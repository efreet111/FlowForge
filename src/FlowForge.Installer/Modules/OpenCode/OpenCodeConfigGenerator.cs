using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using FlowForge.Installer.Infrastructure;

namespace FlowForge.Installer.Modules.OpenCode;

public sealed class OpenCodeConfigGenerator
{
    readonly string _repoPath;
    readonly bool _forceFree;
    readonly bool _dryRun;
    readonly bool _allowSymlink;

    readonly PiiScanner _piiScanner = new();
    readonly AtomicWriter _atomicWriter = new();

    public OpenCodeConfigGenerator(string repoPath, bool forceFree = false, bool dryRun = false, bool allowSymlink = false)
    {
        _repoPath = repoPath;
        _forceFree = forceFree;
        _dryRun = dryRun;
        _allowSymlink = allowSymlink;
    }

    public GenerateResult GenerateOrMerge(
        string targetConfigPath,
        string templatesDir,
        string agentModelsPath,
        string managedPathsPath,
        string sidecarPath)
    {
        var warnings = new List<string>();
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var user = Environment.UserName;

        var manifest = JsonSerializer.Deserialize<AgentModelsManifest>(
            File.ReadAllText(agentModelsPath))
            ?? throw new InvalidOperationException("agent-models.json is invalid.");

        var templateText = File.ReadAllText(Path.Combine(templatesDir, "opencode.json.tpl"));
        templateText = templateText
            .Replace("$FLOWFORGE_REPO", _repoPath)
            .Replace("$HOME", home)
            .Replace("$USER", user)
            .Replace("$FLOWFORGE_ENGRAM_BIN", PathHelper.EngramBinary);

        var templateNode = JsonNode.Parse(templateText) ?? new JsonObject();
        InjectAgentModels(templateNode, manifest);

        var managedPaths = ReadManagedPaths(managedPathsPath);
        var existingNode = LoadExistingConfig(targetConfigPath);

        var paidProviderDetected = existingNode?["provider"]?["opencode-go"] is not null && !_forceFree;
        if (paidProviderDetected)
            warnings.Add("Detected paid provider 'opencode-go'. Free-Zen not applied unless --force-free.");

        var merged = MergeManagedPaths(existingNode, templateNode, managedPaths, paidProviderDetected, warnings);

        var serialized = JsonSerializer.Serialize(merged, new JsonSerializerOptions { WriteIndented = true });
        var pii = _piiScanner.ScanGenerated(serialized, home);
        if (!pii.Clean)
            throw new PiiDetectedException("OpenCode config", pii.Hits);

        if (!_dryRun)
        {
            _atomicWriter.Write(targetConfigPath, serialized, _allowSymlink);
            Directory.CreateDirectory(Path.GetDirectoryName(sidecarPath) ?? string.Empty);
            File.WriteAllText(sidecarPath, JsonSerializer.Serialize(managedPaths, new JsonSerializerOptions { WriteIndented = true }));
        }

        return new GenerateResult(targetConfigPath, managedPaths, paidProviderDetected, warnings);
    }

    static string[] ReadManagedPaths(string managedPathsPath)
    {
        if (!File.Exists(managedPathsPath))
            return Array.Empty<string>();

        var text = File.ReadAllText(managedPathsPath);
        return JsonSerializer.Deserialize<string[]>(text) ?? Array.Empty<string>();
    }

    static JsonNode LoadExistingConfig(string targetConfigPath)
    {
        if (!File.Exists(targetConfigPath))
            return new JsonObject();

        var text = File.ReadAllText(targetConfigPath);
        if (targetConfigPath.EndsWith(".jsonc", StringComparison.OrdinalIgnoreCase))
            text = Regex.Replace(text, @"\/\/.*$", "", RegexOptions.Multiline);

        return JsonNode.Parse(text) ?? new JsonObject();
    }

    static void InjectAgentModels(JsonNode templateNode, AgentModelsManifest manifest)
    {
        if (templateNode["agent"] is not JsonObject agents)
            return;

        foreach (var (name, entry) in manifest.Agents)
        {
            if (agents[name] is JsonObject agent)
                agent["model"] = $"opencode-zen/{entry.Model}";
        }
    }

    static JsonNode MergeManagedPaths(JsonNode existing, JsonNode template, string[] managedPaths, bool paidProviderDetected, List<string> warnings)
    {
        var target = existing is JsonObject obj ? obj : new JsonObject();

        foreach (var path in managedPaths)
        {
            var templateValue = GetNodeAtPath(template, path);
            if (templateValue is null)
                continue;

            if (paidProviderDetected && IsPaidPath(path, target, warnings))
                continue;

            SetValueAtPath(target, path, DeepCopy(templateValue));
        }

        var engramNode = GetNodeAtPath(template, "mcp.engram");
        if (engramNode is not null)
            SetValueAtPath(target, "mcp.engram", DeepCopy(engramNode));

        return target;
    }

    static JsonNode DeepCopy(JsonNode node) =>
        JsonNode.Parse(node.ToJsonString()) ?? new JsonObject();

    static JsonNode? GetNodeAtPath(JsonNode root, string path)
    {
        var current = root;
        foreach (var segment in path.Split('.'))
        {
            if (current is null)
                return null;

            if (current[segment] is JsonNode next)
                current = next;
            else
                return null;
        }

        return current;
    }

    static bool IsPaidPath(string path, JsonNode target, List<string> warnings)
    {
        if (path == "provider.opencode-go")
        {
            warnings.Add("Preserving custom paid provider 'opencode-go'.");
            return true;
        }

        if (path == "agent.flowforge")
        {
            var model = GetNodeAtPath(target, "agent.flowforge.model")?.GetValue<string>();
            if (!string.IsNullOrWhiteSpace(model) && model.StartsWith("opencode-go/", StringComparison.OrdinalIgnoreCase))
            {
                warnings.Add("Preserving paid flowforge model.");
                return true;
            }
        }

        return false;
    }

    static void SetValueAtPath(JsonNode root, string path, JsonNode value)
    {
        var segments = path.Split('.');
        var current = root;

        for (var i = 0; i < segments.Length - 1; i++)
        {
            var segment = segments[i];
            if (current[segment] is not JsonObject child)
            {
                child = new JsonObject();
                if (current is JsonObject obj)
                    obj[segment] = child;
            }

            current = child;
        }

        if (current is JsonObject parent)
            parent[segments[^1]] = value;
    }

    public sealed record GenerateResult(string OutputPath, string[] ManagedPaths, bool PaidProviderDetected, List<string> Warnings);

    public sealed record AgentModelsManifest(
        [property: JsonPropertyName("agents")] Dictionary<string, AgentEntry> Agents,
        [property: JsonPropertyName("provider")] ProviderDef Provider);

    public sealed record AgentEntry(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("fallback")] string Fallback,
        [property: JsonPropertyName("mode")] string Mode,
        [property: JsonPropertyName("purpose")] string Purpose);

    public sealed record ProviderDef(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("api")] string Api,
        [property: JsonPropertyName("npm")] string Npm,
        [property: JsonPropertyName("models")] string[] Models);
}
