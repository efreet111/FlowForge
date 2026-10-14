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
        using var workspace = new TestWorkspace();
        var generator = new OpenCodeConfigGenerator(workspace.TempRoot);
        generator.GenerateOrMerge(workspace.ConfigPath, workspace.TemplatesDir, workspace.AgentModelsPath, workspace.ManagedPathsPath, workspace.SidecarPath);

        var root = JsonDocument.Parse(File.ReadAllText(workspace.ConfigPath)).RootElement;
        Assert.True(root.TryGetProperty("$schema", out var schema));
        Assert.Equal("https://opencode.ai/config.json", schema.GetString());

        Assert.True(root.TryGetProperty("instructions", out var instructions));
        Assert.Equal("./flowforge/AGENTS.md", instructions[0].GetString());

        Assert.True(root.TryGetProperty("agents", out var agents));
        Assert.True(agents.TryGetProperty("flowforge", out var flowforge));
        Assert.Equal("opencode-zen/big-pickle", flowforge.GetProperty("model").GetString());

        Assert.True(root.TryGetProperty("providers", out var providers));
        Assert.True(providers.TryGetProperty("opencode-zen", out var zen));
        Assert.Equal("https://opencode.ai/zen/v1", zen.GetProperty("settings").GetProperty("baseURL").GetString());

        Assert.True(root.TryGetProperty("permissions", out var permissions));
        Assert.Equal(JsonValueKind.Array, permissions.ValueKind);

        Assert.True(root.TryGetProperty("mcp", out var mcp));
        Assert.True(mcp.TryGetProperty("engram", out var engram));
        Assert.True(engram.GetProperty("enabled").GetBoolean());
    }

    [Theory]
    [InlineData("opencode-zen", "opencode-zen/big-pickle")]
    [InlineData("opencode-go", "opencode-go/qwen3.7-plus")]
    public void GenerateOrMerge_respects_provider_selection(string provider, string expectedModel)
    {
        using var workspace = new TestWorkspace();
        var generator = new OpenCodeConfigGenerator(workspace.TempRoot, provider);
        generator.GenerateOrMerge(workspace.ConfigPath, workspace.TemplatesDir, workspace.AgentModelsPath, workspace.ManagedPathsPath, workspace.SidecarPath);

        var root = JsonDocument.Parse(File.ReadAllText(workspace.ConfigPath)).RootElement;
        var model = root.GetProperty("agents").GetProperty("flowforge").GetProperty("model").GetString();
        Assert.Equal(expectedModel, model);
    }

    sealed class TestWorkspace : IDisposable
    {
        public string TempRoot { get; }
        public string TemplatesDir { get; }
        public string AgentModelsPath { get; }
        public string ManagedPathsPath { get; }
        public string ConfigPath { get; }
        public string SidecarPath { get; }

        public TestWorkspace()
        {
            TempRoot = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(TempRoot);
            TemplatesDir = Path.Combine(TempRoot, "templates");
            Directory.CreateDirectory(TemplatesDir);

            AgentModelsPath = Path.Combine(TemplatesDir, "agent-models.json");
            File.WriteAllText(AgentModelsPath, ManifestContent);

            ManagedPathsPath = Path.Combine(TemplatesDir, "managed-paths.json");
            File.WriteAllText(ManagedPathsPath, ManagedPathsContent);

            File.WriteAllText(Path.Combine(TemplatesDir, "opencode.json.tpl"), TemplateContent);

            ConfigPath = Path.Combine(TempRoot, "opencode.json");
            SidecarPath = Path.Combine(TempRoot, ".flowforge-managed.json");
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(TempRoot))
                    Directory.Delete(TempRoot, recursive: true);
            }
            catch {}
        }
    }

    static string TemplateContent => """
    {
      "$schema": "https://opencode.ai/config.json",
      "default_agent": "flowforge",
      "skills": [
        "./skills"
      ],
      "instructions": [
        "./flowforge/AGENTS.md"
      ],
      "agents": {
        "flowforge": {
          "description": "FlowForge Orchestrator",
          "mode": "primary",
          "model": "__FLOWFORGE_MODEL__",
          "permissions": [
            { "action": "edit", "resource": "*", "effect": "allow" },
            { "action": "shell", "resource": "*", "effect": "allow" },
            { "action": "read", "resource": "*", "effect": "allow" },
            { "action": "subagent", "resource": "*", "effect": "allow" },
            { "action": "skill", "resource": "*", "effect": "allow" }
          ],
          "system": "{file:./agents/flowforge.md}"
        }
      },
      "providers": {
        "opencode-zen": {
          "id": "opencode-zen",
          "name": "OpenCode Zen (Free)",
          "package": "@opencode/ai/providers/openai-compatible",
          "settings": {
            "baseURL": "https://opencode.ai/zen/v1"
          },
          "env": [],
          "models": {
            "big-pickle": {
              "name": "Big Pickle",
              "reasoning": false,
              "structured_output": true,
              "tool_call": true
            }
          }
        },
        "opencode-go": {
          "id": "opencode-go",
          "name": "OpenCode Go (paid)",
          "package": "@opencode/ai/providers/openai-compatible",
          "settings": {
            "baseURL": "https://opencode.ai/zen/go/v1"
          },
          "env": [
            "OPENCODIGO_API_KEY"
          ],
          "description": "OpenCode Go (paid)",
          "models": {
            "qwen3.7-plus": {
              "name": "Qwen3.7 Plus",
              "reasoning": false,
              "structured_output": true,
              "tool_call": true
            },
            "deepseek-v4-flash": {
              "name": "DeepSeek V4 Flash",
              "reasoning": false,
              "structured_output": true,
              "tool_call": true
            }
          }
        }
      },
      "permissions": [
        { "action": "shell", "resource": "*", "effect": "allow" },
        { "action": "read", "resource": "*", "effect": "allow" }
      ],
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
      "providers": {
        "opencode-zen": {
          "id": "opencode-zen",
          "api": "https://opencode.ai/zen/v1",
          "npm": "@ai-sdk/openai-compatible",
          "models": [
            "big-pickle"
          ]
        },
        "opencode-go": {
          "id": "opencode-go",
          "api": "https://opencode.ai/zen/go/v1",
          "npm": "@ai-sdk/openai-compatible",
          "env": [
            "OPENCODIGO_API_KEY"
          ],
          "models": {
            "qwen3.7-plus": {
              "name": "Qwen3.7 Plus",
              "reasoning": false,
              "structured_output": true,
              "tool_call": true
            },
            "deepseek-v4-flash": {
              "name": "DeepSeek V4 Flash",
              "reasoning": false,
              "structured_output": true,
              "tool_call": true
            }
          }
        }
      },
      "agents": {
        "flowforge": {
          "model": {
            "opencode-zen": "big-pickle",
            "opencode-go": "qwen3.7-plus"
          },
          "fallback": {
            "opencode-zen": "big-pickle",
            "opencode-go": "deepseek-v4-flash"
          },
          "mode": "primary",
          "purpose": "FlowForge Orchestrator"
        }
      }
    }
    """;

    static string ManagedPathsContent => """
    [
      "$schema",
      "instructions",
      "agents.flowforge",
      "providers.opencode-zen",
      "providers.opencode-go",
      "permissions",
      "mcp.engram",
      "skills",
      "default_agent"
    ]
    """;
}
