using System.Text.Json;
using System.Text.Json.Nodes;

namespace FlowForge.Installer.Update;

/// <summary>
/// Result of an MCP config merge operation.
/// </summary>
public sealed record McpMergeResult(
    bool Success,
    int ServersPreserved,
    bool EngramAdded,
    string? Error
);

/// <summary>
/// Surgical JSON merge for MCP config files.
/// Pattern: read existing → parse as JsonNode → replace only "engram" entry → write back.
/// Preserves ALL other servers byte-for-byte.
/// </summary>
public sealed class McpConfigMerger
{
    /// <summary>
    /// Merge engram MCP entry into Cursor mcp.json.
    /// Format: {"mcpServers": {"engram": {"type":"stdio","command":"...","args":["mcp"],"env":{...}}}}
    /// </summary>
    public McpMergeResult MergeCursorMcp(
        string mcpJsonPath, string engramBinaryPath,
        string user, string dataDir, bool syncEnabled, string? syncUrl)
    {
        return MergeMcpServersFormat(mcpJsonPath, engramBinaryPath, user, dataDir, syncEnabled, syncUrl);
    }

    /// <summary>
    /// Merge engram MCP entry into Antigravity mcp_config.json.
    /// Same mcpServers format as Cursor.
    /// </summary>
    public McpMergeResult MergeAntigravityMcp(
        string mcpConfigPath, string engramBinaryPath,
        string user, string dataDir, bool syncEnabled, string? syncUrl)
    {
        return MergeMcpServersFormat(mcpConfigPath, engramBinaryPath, user, dataDir, syncEnabled, syncUrl);
    }

    static McpMergeResult MergeMcpServersFormat(
        string configPath, string engramBinaryPath,
        string user, string dataDir, bool syncEnabled, string? syncUrl)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);

            // Read existing config (or start fresh)
            var existingText = File.Exists(configPath)
                ? File.ReadAllText(configPath)
                : "{}";

            JsonNode node;
            try
            {
                node = JsonNode.Parse(existingText) ?? new JsonObject();
            }
            catch
            {
                node = new JsonObject();
            }

            // Get or create mcpServers section
            JsonObject mcpServers;
            if (node["mcpServers"] is JsonObject existingServers)
            {
                mcpServers = existingServers;
            }
            else
            {
                mcpServers = new JsonObject();
                node["mcpServers"] = mcpServers;
            }

            // Count preserved servers (non-engram)
            var engramAdded = !mcpServers.ContainsKey("engram");
            var serversPreserved = mcpServers.Count(kvp =>
                !string.Equals(kvp.Key, "engram", StringComparison.OrdinalIgnoreCase));

            // Build engram entry
            var env = new JsonObject();
            env["ENGRAM_DATA_DIR"] = dataDir;
            env["ENGRAM_USER"] = user;
            env["ENGRAM_SYNC_ENABLED"] = syncEnabled.ToString().ToLower();
            if (syncEnabled && !string.IsNullOrWhiteSpace(syncUrl))
                env["ENGRAM_SERVER_URL"] = syncUrl;

            var engramEntry = new JsonObject
            {
                ["type"] = "stdio",
                ["command"] = engramBinaryPath,
                ["args"] = new JsonArray("mcp"),
                ["env"] = env
            };

            // Surgical replacement: only the engram key
            mcpServers["engram"] = engramEntry;

            // Write back with indentation
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = node.ToJsonString(options);
            File.WriteAllText(configPath, json + Environment.NewLine);

            return new McpMergeResult(true, serversPreserved, engramAdded, null);
        }
        catch (Exception ex)
        {
            return new McpMergeResult(false, 0, false, ex.Message);
        }
    }
}
