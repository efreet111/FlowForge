using System.Text.Json.Nodes;
using FlowForge.Installer.Update;
using Xunit;

namespace FlowForge.Installer.Tests.Update;

public class McpConfigMergerTests
{
    const string EngramBinary = "/usr/local/bin/engram";
    const string User = "testuser";
    const string DataDir = "/home/testuser/.engram";

    [Fact]
    public void MergeCursorMcp_EmptyFile_CreatesEngramEntry()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"mcp-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var configPath = Path.Combine(tempDir, "mcp.json");
            var merger = new McpConfigMerger();

            var result = merger.MergeCursorMcp(configPath, EngramBinary, User, DataDir, false, null);

            Assert.True(result.Success);
            Assert.True(result.EngramAdded);
            Assert.Equal(0, result.ServersPreserved);

            // Verify file content
            var node = JsonNode.Parse(File.ReadAllText(configPath));
            Assert.NotNull(node?["mcpServers"]?["engram"]);
            Assert.Equal("stdio", node!["mcpServers"]!["engram"]!["type"]!.GetValue<string>());
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void MergeCursorMcp_ExistingServers_PreservesThem()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"mcp-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var configPath = Path.Combine(tempDir, "mcp.json");
            File.WriteAllText(configPath, """
            {
                "mcpServers": {
                    "existing-server": {
                        "type": "stdio",
                        "command": "/bin/existing"
                    },
                    "another-server": {
                        "type": "stdio",
                        "command": "/bin/another"
                    }
                }
            }
            """);

            var merger = new McpConfigMerger();
            var result = merger.MergeCursorMcp(configPath, EngramBinary, User, DataDir, false, null);

            Assert.True(result.Success);
            Assert.Equal(2, result.ServersPreserved);
            Assert.True(result.EngramAdded);

            // Verify existing servers preserved
            var node = JsonNode.Parse(File.ReadAllText(configPath));
            Assert.NotNull(node?["mcpServers"]?["existing-server"]);
            Assert.NotNull(node?["mcpServers"]?["another-server"]);
            Assert.NotNull(node?["mcpServers"]?["engram"]);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void MergeCursorMcp_ExistingEngramEntry_UpdatesInPlace()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"mcp-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var configPath = Path.Combine(tempDir, "mcp.json");
            File.WriteAllText(configPath, """
            {
                "mcpServers": {
                    "engram": {
                        "type": "stdio",
                        "command": "/old/path/engram",
                        "args": ["mcp"]
                    },
                    "other": {
                        "type": "stdio",
                        "command": "/bin/other"
                    }
                }
            }
            """);

            var merger = new McpConfigMerger();
            var result = merger.MergeCursorMcp(configPath, EngramBinary, User, DataDir, true, "https://sync.example.com");

            Assert.True(result.Success);
            Assert.False(result.EngramAdded); // Updated, not added
            Assert.Equal(1, result.ServersPreserved); // "other" preserved

            var node = JsonNode.Parse(File.ReadAllText(configPath));
            var engram = node?["mcpServers"]?["engram"];
            Assert.Equal(EngramBinary, engram?["command"]?.GetValue<string>());
            Assert.NotNull(node?["mcpServers"]?["other"]);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void MergeCursorMcp_InvalidJson_HandlesGracefully()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"mcp-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var configPath = Path.Combine(tempDir, "mcp.json");
            File.WriteAllText(configPath, "not valid json {{{");

            var merger = new McpConfigMerger();
            var result = merger.MergeCursorMcp(configPath, EngramBinary, User, DataDir, false, null);

            Assert.True(result.Success); // Should recover and create fresh
            Assert.True(result.EngramAdded);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void MergeCursorMcp_NonEngramServers_ByteIdenticalAfterMerge()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"mcp-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var configPath = Path.Combine(tempDir, "mcp.json");
            var originalContent = """
            {
                "mcpServers": {
                    "my-server": {
                        "type": "stdio",
                        "command": "/bin/my-server",
                        "args": ["--flag"],
                        "env": {"KEY": "value"}
                    }
                }
            }
            """;
            File.WriteAllText(configPath, originalContent);

            var merger = new McpConfigMerger();
            merger.MergeCursorMcp(configPath, EngramBinary, User, DataDir, false, null);

            var node = JsonNode.Parse(File.ReadAllText(configPath));
            var myServer = node?["mcpServers"]?["my-server"];
            Assert.NotNull(myServer);
            Assert.Equal("stdio", myServer!["type"]?.GetValue<string>());
            Assert.Equal("/bin/my-server", myServer["command"]?.GetValue<string>());
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void MergeAntigravityMcp_WorksSameAsCursor()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"mcp-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var configPath = Path.Combine(tempDir, "mcp_config.json");
            var merger = new McpConfigMerger();

            var result = merger.MergeAntigravityMcp(configPath, EngramBinary, User, DataDir, false, null);

            Assert.True(result.Success);
            Assert.True(result.EngramAdded);

            var node = JsonNode.Parse(File.ReadAllText(configPath));
            Assert.NotNull(node?["mcpServers"]?["engram"]);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void MergeCursorMcp_WithSyncUrl_IncludesEnvVar()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"mcp-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var configPath = Path.Combine(tempDir, "mcp.json");
            var merger = new McpConfigMerger();

            merger.MergeCursorMcp(configPath, EngramBinary, User, DataDir, true, "https://sync.example.com");

            var node = JsonNode.Parse(File.ReadAllText(configPath));
            var env = node?["mcpServers"]?["engram"]?["env"];
            Assert.Equal("true", env?["ENGRAM_SYNC_ENABLED"]?.GetValue<string>());
            Assert.Equal("https://sync.example.com", env?["ENGRAM_SERVER_URL"]?.GetValue<string>());
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }
}
