using System.Text.Json.Serialization;

namespace FlowForge.Installer.Models;

public sealed class InstallerConfig
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "0.1.0";

    [JsonPropertyName("channel")]
    public string Channel { get; set; } = "stable";

    [JsonPropertyName("auto_update")]
    public bool AutoUpdate { get; set; } = false;

    [JsonPropertyName("flowdoc")]
    public FlowDocConfig FlowDoc { get; set; } = new();

    [JsonPropertyName("components")]
    public ComponentsConfig Components { get; set; } = new();
}

public sealed class FlowDocConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;
}

public sealed class ComponentsConfig
{
    [JsonPropertyName("engram_dotnet")]
    public ComponentEntry? EngramDotnet { get; set; }

    [JsonPropertyName("flowforge")]
    public FlowForgeComponentEntry? FlowForge { get; set; }
}

public sealed class ComponentEntry
{
    [JsonPropertyName("installed")]
    public bool Installed { get; set; }

    [JsonPropertyName("version")]
    public string Version { get; set; } = "";

    [JsonPropertyName("binary")]
    public string Binary { get; set; } = "";

    [JsonPropertyName("registered_at")]
    public string RegisteredAt { get; set; } = "";
}

public sealed class FlowForgeComponentEntry
{
    [JsonPropertyName("installed")]
    public bool Installed { get; set; }

    [JsonPropertyName("version")]
    public string Version { get; set; } = "";

    [JsonPropertyName("ides")]
    public List<string> Ides { get; set; } = [];
}

[JsonSerializable(typeof(InstallerConfig))]
[JsonSerializable(typeof(FlowDocConfig))]
[JsonSerializable(typeof(ComponentsConfig))]
[JsonSerializable(typeof(ComponentEntry))]
[JsonSerializable(typeof(FlowForgeComponentEntry))]
public partial class InstallerJsonContext : JsonSerializerContext { }

// ── MCP config models (AOT-safe) ──────────────────────────────────────────────

public sealed class McpCursorConfig
{
    [JsonPropertyName("mcpServers")]
    public Dictionary<string, McpStdioEntry> McpServers { get; set; } = [];
}

public sealed class McpStdioEntry
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "stdio";

    [JsonPropertyName("command")]
    public string Command { get; set; } = "";

    [JsonPropertyName("args")]
    public List<string> Args { get; set; } = [];

    [JsonPropertyName("env")]
    public Dictionary<string, string> Env { get; set; } = [];
}

public sealed class McpOpenCodeConfig
{
    [JsonPropertyName("mcp")]
    public Dictionary<string, McpOpenCodeEntry> Mcp { get; set; } = [];
}

public sealed class McpOpenCodeEntry
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "stdio";

    [JsonPropertyName("command")]
    public List<string> Command { get; set; } = [];

    [JsonPropertyName("environment")]
    public Dictionary<string, string> Environment { get; set; } = [];
}

[JsonSerializable(typeof(McpCursorConfig))]
[JsonSerializable(typeof(McpStdioEntry))]
[JsonSerializable(typeof(McpOpenCodeConfig))]
[JsonSerializable(typeof(McpOpenCodeEntry))]
[JsonSerializable(typeof(Dictionary<string, McpStdioEntry>))]
[JsonSerializable(typeof(Dictionary<string, McpOpenCodeEntry>))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(Dictionary<string, object>))]
[JsonSerializable(typeof(List<string>))]
public partial class McpJsonContext : JsonSerializerContext { }
