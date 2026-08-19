using System.Text.Json;
using System.Text.Json.Serialization;
using FlowForge.Installer.Models;

namespace FlowForge.Installer.Onboarding;

/// <summary>
/// Result of project resolution.
/// </summary>
public sealed record ProjectResolution(
    string ProjectName,          // e.g. "flowforge"
    string NamespacedProject,    // e.g. "team/flowforge"
    string? Source,              // "flowforge.json" | "git" | "cli-arg"
    bool IsAmbiguous,
    IReadOnlyList<string>? AvailableProjects);

/// <summary>
/// Resolves project name via .flowforge.json → engram.project, with --project override.
/// Applies namespacing: team/{project} for team scope, {identity}/{project} for personal.
/// Note: --user flag is display-only (FR-014). Personal-scope namespace uses the resolved
/// identity from config (sync.user → ENGRAM_USER), NOT the --user flag.
/// </summary>
public static class ProjectResolver
{
    /// <summary>
    /// Resolves the project name and namespace.
    /// </summary>
    /// <param name="cliProjectOverride">Value from --project flag (highest priority).</param>
    /// <param name="scope">"team" or "personal" (determines namespace prefix).</param>
    /// <param name="identity">Resolved user identity from config (sync.user → ENGRAM_USER). Used for personal-scope namespacing. NOT the --user flag.</param>
    /// <param name="workingDir">Current working directory for auto-detection.</param>
    public static ProjectResolution Resolve(
        string? cliProjectOverride,
        string? scope,
        string? identity,
        string workingDir)
    {
        // 1. CLI override has highest priority
        if (!string.IsNullOrWhiteSpace(cliProjectOverride))
        {
            var namespaced = ApplyNamespace(cliProjectOverride, scope, identity);
            return new ProjectResolution(cliProjectOverride, namespaced, "cli-arg", false, null);
        }

        // 2. Try .flowforge.json
        var flowforgeJsonPath = Path.Combine(workingDir, ".flowforge.json");
        if (File.Exists(flowforgeJsonPath))
        {
            try
            {
                var json = File.ReadAllText(flowforgeJsonPath);
                var config = JsonSerializer.Deserialize(json, FlowForgeProjectJsonContext.Default.FlowForgeProjectConfig);
                if (!string.IsNullOrWhiteSpace(config?.Engram?.Project))
                {
                    var namespaced = ApplyNamespace(config.Engram.Project, scope, identity);
                    return new ProjectResolution(config.Engram.Project, namespaced, "flowforge.json", false, null);
                }
            }
            catch
            {
                // Ignore parse errors, fall through to git detection
            }
        }

        // 3. Try git remote name detection
        var gitProjectName = DetectFromGit(workingDir);
        if (!string.IsNullOrWhiteSpace(gitProjectName))
        {
            var namespaced = ApplyNamespace(gitProjectName, scope, identity);
            return new ProjectResolution(gitProjectName, namespaced, "git", false, null);
        }

        // 4. Try child directories (ambiguity detection)
        var childProjects = DetectChildProjects(workingDir);
        if (childProjects.Count > 1)
        {
            return new ProjectResolution(
                childProjects[0],
                ApplyNamespace(childProjects[0], scope, identity),
                "ambiguous",
                true,
                childProjects);
        }

        if (childProjects.Count == 1)
        {
            var namespaced = ApplyNamespace(childProjects[0], scope, identity);
            return new ProjectResolution(childProjects[0], namespaced, "git-child", false, null);
        }

        // 5. Fallback: directory name
        var dirName = Path.GetFileName(workingDir) ?? "unknown";
        var ns = ApplyNamespace(dirName, scope, identity);
        return new ProjectResolution(dirName, ns, "directory", false, null);
    }

    /// <summary>
    /// Applies namespace based on scope. For personal scope, uses the resolved identity
    /// (from config), NOT the --user flag (FR-014: --user is display-only).
    /// </summary>
    static string ApplyNamespace(string project, string? scope, string? identity)
    {
        if (string.Equals(scope, "personal", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(identity))
            return $"{identity}/{project}";

        // Default: team scope (also used when personal scope has no identity — falls back to team)
        return $"team/{project}";
    }

    static string? DetectFromGit(string workingDir)
    {
        try
        {
            var gitDir = Path.Combine(workingDir, ".git");
            if (!Directory.Exists(gitDir) && !File.Exists(gitDir)) // gitdir file for submodules
                return null;

            // Try to read the remote origin URL to extract project name
            var configPath = Path.Combine(gitDir, "config");
            if (!File.Exists(configPath))
                return Path.GetFileName(workingDir);

            var content = File.ReadAllText(configPath);
            // Look for url = ... under [remote "origin"]
            var lines = content.Split('\n');
            bool inOrigin = false;
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("[remote \"origin\"]", StringComparison.OrdinalIgnoreCase))
                {
                    inOrigin = true;
                    continue;
                }
                if (trimmed.StartsWith("[", StringComparison.Ordinal))
                {
                    inOrigin = false;
                    continue;
                }
                if (inOrigin && trimmed.StartsWith("url", StringComparison.OrdinalIgnoreCase))
                {
                    var url = trimmed.Split('=', 2)[1].Trim();
                    // Extract repo name from URL (last path segment without .git)
                    var segments = url.Split('/', StringSplitOptions.RemoveEmptyEntries);
                    if (segments.Length > 0)
                    {
                        var name = segments[^1];
                        if (name.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
                            name = name[..^4];
                        return name;
                    }
                }
            }

            // Fallback to directory name
            return Path.GetFileName(workingDir);
        }
        catch
        {
            return null;
        }
    }

    static List<string> DetectChildProjects(string workingDir)
    {
        var projects = new List<string>();
        try
        {
            foreach (var dir in Directory.GetDirectories(workingDir))
            {
                var gitDir = Path.Combine(dir, ".git");
                if (Directory.Exists(gitDir) || File.Exists(gitDir))
                {
                    projects.Add(Path.GetFileName(dir));
                }
            }
        }
        catch
        {
            // Ignore permission errors
        }
        return projects;
    }
}

// ── Source-gen JSON context for .flowforge.json project config ────────────────

internal sealed class FlowForgeProjectConfig
{
    [JsonPropertyName("engram")]
    public FlowForgeEngramConfig? Engram { get; set; }
}

internal sealed class FlowForgeEngramConfig
{
    [JsonPropertyName("project")]
    public string? Project { get; set; }
}

[JsonSerializable(typeof(FlowForgeProjectConfig))]
internal partial class FlowForgeProjectJsonContext : JsonSerializerContext { }
