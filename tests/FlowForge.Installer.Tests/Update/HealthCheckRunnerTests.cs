using FlowForge.Installer.Infrastructure;
using FlowForge.Installer.Update;
using Xunit;

namespace FlowForge.Installer.Tests.Update;

public class HealthCheckRunnerTests
{
    [Fact]
    public async Task CheckBinary_NonExistentBinary_ReturnsFailed()
    {
        var log = new InstallerLogger(Path.Combine(Path.GetTempPath(), $"hc-{Guid.NewGuid():N}.log"));
        var runner = new HealthCheckRunner(log);

        var result = await runner.CheckBinaryAsync("/nonexistent/binary", "1.0.0");

        Assert.False(result.Passed);
        Assert.Contains("not found", result.Detail);
    }

    [Fact]
    public void CheckMcpConfig_ValidJson_ReturnsPassed()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"hc-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var configPath = Path.Combine(tempDir, "mcp.json");
            File.WriteAllText(configPath, """{"mcpServers":{"engram":{"type":"stdio","command":"engram"}}}""");

            var log = new InstallerLogger(Path.Combine(tempDir, "install.log"));
            var runner = new HealthCheckRunner(log);
            var result = runner.CheckMcpConfig(configPath);

            Assert.True(result.Passed);
            Assert.Contains("engram entry", result.Detail);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void CheckMcpConfig_InvalidJson_ReturnsFailed()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"hc-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var configPath = Path.Combine(tempDir, "mcp.json");
            File.WriteAllText(configPath, "not json at all {{{");

            var log = new InstallerLogger(Path.Combine(tempDir, "install.log"));
            var runner = new HealthCheckRunner(log);
            var result = runner.CheckMcpConfig(configPath);

            Assert.False(result.Passed);
            Assert.Contains("Invalid JSON", result.Detail);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void CheckMcpConfig_NonExistentFile_ReturnsFailed()
    {
        var log = new InstallerLogger(Path.Combine(Path.GetTempPath(), $"hc-{Guid.NewGuid():N}.log"));
        var runner = new HealthCheckRunner(log);

        var result = runner.CheckMcpConfig("/nonexistent/mcp.json");

        Assert.False(result.Passed);
        Assert.Contains("not found", result.Detail);
    }

    [Fact]
    public void CheckMcpConfig_ValidJsonWithoutEngram_ReturnsPassedWithNote()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"hc-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var configPath = Path.Combine(tempDir, "mcp.json");
            File.WriteAllText(configPath, """{"mcpServers":{"other":{"type":"stdio"}}}""");

            var log = new InstallerLogger(Path.Combine(tempDir, "install.log"));
            var runner = new HealthCheckRunner(log);
            var result = runner.CheckMcpConfig(configPath);

            Assert.True(result.Passed);
            Assert.Contains("no engram entry", result.Detail);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }
}
