using System;
using System.IO;
using System.Text.Json;
using FlowForge.Installer.Modules.OpenCode;
using Xunit;

namespace FlowForge.Installer.Tests.Modules.OpenCode;

public class OpenCodeConfigGeneratorTests
{
    [Fact]
    public void GenerateOrMerge_creates_full_config_sections()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        try
        {
            Directory.CreateDirectory(tempRoot);

            var templatesDir = Path.Combine(tempRoot, "templates");
            Directory.CreateDirectory(templatesDir);

            var templatePath = Path.Combine(templatesDir, "opencode.json.tpl");
            File.WriteAllText(templatePath, TemplateContent);

            var manifestPath = Path.Combine(templatesDir, "agent-models.json");
            File.WriteAllText(manifestPath, ManifestContent);

            var managedPathsPath = Path.Combine(templatesDir, "managed-paths.json");
            File.WriteAllText(managedPathsPath, ManagedPathsContent);

            var targetConfig = Path.Combine(tempRoot, "opencode.json");
            var sidecarPath = Path.Combine(tempRoot, ".flowforge-managed.json");

            var generator = new OpenCodeConfigGenerator(tempRoot);
            generator.GenerateOrMerge(targetConfig, templatesDir, manifestPath, managedPathsPath, sidecarPath);

            var json = JsonDocument.Parse(File.ReadAllText(targetConfig));
            var root = json.RootElement;

            Assert.True(root.TryGetProperty("$schema", out var schema));
            Assert.Equal("https://opencode.ai/config.json", schema.GetString());

            Assert.True(root.TryGetProperty("instructions", out var instructions));
            Assert.Equal("./flowforge/AGENTS.md", instructions[0].GetString());

            Assert.True(root.TryGetProperty("agent", out var agents));
            Assert.True(agents.TryGetProperty("flowforge", out var flowforge));
            Assert.Equal("opencode-zen/big-pickle", flowforge.GetProperty("model").GetString());

            Assert.True(root.TryGetProperty("provider", out var provider));
            Assert.True(provider.TryGetProperty("opencode-zen", out var zen));
            Assert.Equal("https://opencode.ai/zen/v1", zen.GetProperty("api").GetString());

            Assert.True(root.TryGetProperty("permission", out var permission));
            Assert.True(permission.TryGetProperty("bash", out _));
            Assert.True(permission.TryGetProperty("read", out _));

            Assert.True(root.TryGetProperty("mcp", out var mcp));
            Assert.True(mcp.TryGetProperty("engram", out var engram));
            Assert.True(engram.GetProperty("enabled").GetBoolean());
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                try
                {
                    Directory.Delete(tempRoot, recursive: true);
                }
                catch { }
            }
        }
    }

    static string TemplateContent => """
    {
      "$schema": "https://opencode.ai/config.json",
      "instructions": [
        "./flowforge/AGENTS.md"
      ],
      "agent": {
        "flowforge": {
          "description": "FlowForge Orchestrator",
          "mode": "primary",
          "model": "__FLOWFORGE_MODEL__",
          "permission": {
            "edit": "allow",
            "write": "allow",
            "read": "allow",
            "bash": "allow",
            "task": "allow"
          },
          "prompt": "{file:./agents/flowforge.md}"
        }
      },
      "provider": {
        "opencode-zen": {
          "api": "https://opencode.ai/zen/v1",
          "npm": "@ai-sdk/openai-compatible",
          "models": [
            "big-pickle"
          ]
        }
      },
      "permission": {
        "bash": {
          "*": "allow"
        },
        "read": {
          "*": "allow"
        }
      },
      "mcp": {
        "engram": {
          "type": "local",
          "enabled": true
        }
      }
    }
    """;

    static string ManifestContent => """
    {
      "agents": {
        "flowforge": {
          "model": "big-pickle",
          "fallback": "big-pickle",
          "mode": "primary",
          "purpose": "Test"
        }
      },
      "provider": {
        "id": "opencode-zen",
        "api": "https://opencode.ai/zen/v1",
        "npm": "@ai-sdk/openai-compatible",
        "models": [
          "big-pickle"
        ]
      }
    }
    """;

    static string ManagedPathsContent => """
    [
      "$schema",
      "instructions",
      "agent.flowforge",
      "provider.opencode-zen",
      "permission.bash",
      "permission.read",
      "mcp.engram"
    ]
    """;
}
