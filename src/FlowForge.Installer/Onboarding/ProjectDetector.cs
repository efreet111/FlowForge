using System.Diagnostics;
using System.Text.Json.Nodes;

namespace FlowForge.Installer.Onboarding;

/// <summary>
/// Deterministic project detection chain (FR-002).
/// First non-empty wins:
/// 1. --project flag (explicit override)
/// 2. .flowforge.json → engram.project
/// 3. .flowforge.json → project (top-level)
/// 4. git remote basename
/// 5. basename(CWD)
/// </summary>
public static class ProjectDetector
{
    /// <summary>
    /// Detect the project name using the detection chain.
    /// </summary>
    public static string Detect(string? explicitProject = null)
    {
        // 1. Explicit --project flag
        if (!string.IsNullOrWhiteSpace(explicitProject))
            return explicitProject.Trim();

        var cwd = Directory.GetCurrentDirectory();

        // 2 & 3. .flowforge.json
        var flowforgeJson = Path.Combine(cwd, ".flowforge.json");
        if (File.Exists(flowforgeJson))
        {
            try
            {
                var json = File.ReadAllText(flowforgeJson);
                var node = JsonNode.Parse(json);

                // 2. engram.project
                var engramProject = node?["engram"]?["project"]?.GetValue<string>();
                if (!string.IsNullOrWhiteSpace(engramProject))
                    return engramProject.Trim();

                // 3. top-level project
                var topProject = node?["project"]?.GetValue<string>();
                if (!string.IsNullOrWhiteSpace(topProject))
                    return topProject.Trim();
            }
            catch
            {
                // Malformed JSON — fall through to next detection step
            }
        }

        // 4. git remote basename
        var gitRemote = TryGetGitRemoteBasename(cwd);
        if (!string.IsNullOrWhiteSpace(gitRemote))
            return gitRemote;

        // 5. basename(CWD)
        return Path.GetFileName(cwd) ?? "unknown";
    }

    /// <summary>
    /// Returns true if .flowforge.json exists in the given directory.
    /// </summary>
    public static bool IsFlowForgeProject(string directory)
    {
        return File.Exists(Path.Combine(directory, ".flowforge.json"));
    }

    /// <summary>
    /// Extract basename from git remote URL.
    /// Handles both HTTPS (https://github.com/org/repo.git) and SSH (git@github.com:org/repo.git).
    /// </summary>
    public static string? TryGetGitRemoteBasename(string cwd)
    {
        try
        {
            using var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    ArgumentList = { "remote", "get-url", "origin" },
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            proc.Start();
            var output = proc.StandardOutput.ReadToEnd().Trim();
            proc.WaitForExit(5_000);

            if (proc.ExitCode != 0 || string.IsNullOrWhiteSpace(output))
                return null;

            return ParseGitRemoteBasename(output);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Parse the basename from a git remote URL.
    /// Pure function for testability.
    /// </summary>
    public static string ParseGitRemoteBasename(string remoteUrl)
    {
        var url = remoteUrl.Trim();

        // SSH format: git@github.com:org/repo.git
        if (url.Contains(':') && !url.Contains("://"))
        {
            var lastColon = url.LastIndexOf(':');
            url = url[(lastColon + 1)..];
        }

        // Extract basename from path (works for both SSH remainder and HTTPS)
        var lastSlash = url.LastIndexOf('/');
        if (lastSlash >= 0)
            url = url[(lastSlash + 1)..];

        // Strip .git suffix
        if (url.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
            url = url[..^4];

        return url;
    }
}
