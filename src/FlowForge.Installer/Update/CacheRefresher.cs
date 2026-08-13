using System.Diagnostics;
using FlowForge.Installer.Infrastructure;

namespace FlowForge.Installer.Update;

/// <summary>
/// Refreshes the FlowForge cache via `git pull`.
/// Falls back to fresh clone on failure.
/// </summary>
public sealed class CacheRefresher
{
    const string RepoUrl = "https://github.com/efreet111/FlowForge.git";

    static string CachePath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".flowforge", "cache", "FlowForge");

    /// <summary>
    /// Refresh the FlowForge cache via `git pull`.
    /// If pull fails (corrupt repo), falls back to fresh clone.
    /// Returns the cache path on success, null on failure.
    /// </summary>
    public string? RefreshCache(InstallerLogger log)
    {
        var cache = CachePath;

        // Try git pull first
        if (Directory.Exists(cache) && File.Exists(Path.Combine(cache, ".git", "HEAD")))
        {
            log.Info($"CacheRefresher: attempting git pull in {cache}");
            var pullExitCode = RunGit("pull --ff-only", cache);
            if (pullExitCode == 0)
            {
                log.Info("CacheRefresher: git pull succeeded");
                return cache;
            }
            log.Warn($"CacheRefresher: git pull failed (exit {pullExitCode}), trying fresh clone");
        }

        // Fallback: delete and fresh clone
        try
        {
            if (Directory.Exists(cache))
            {
                Directory.Delete(cache, recursive: true);
                log.Info($"CacheRefresher: deleted stale cache {cache}");
            }

            var parent = Path.GetDirectoryName(cache)!;
            Directory.CreateDirectory(parent);

            log.Info($"CacheRefresher: cloning {RepoUrl} → {cache}");
            var cloneExitCode = RunGit($"clone --depth 1 {RepoUrl} \"{cache}\"", parent);
            if (cloneExitCode != 0)
            {
                log.Error($"CacheRefresher: git clone failed (exit {cloneExitCode})");
                return null;
            }

            if (File.Exists(Path.Combine(cache, "AGENTS.md")))
            {
                log.Info("CacheRefresher: fresh clone succeeded");
                return cache;
            }

            log.Error("CacheRefresher: clone completed but AGENTS.md not found");
            return null;
        }
        catch (Exception ex)
        {
            log.Error($"CacheRefresher: {ex.Message}");
            return null;
        }
    }

    static int RunGit(string arguments, string workingDir)
    {
        try
        {
            using var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = arguments,
                    WorkingDirectory = workingDir,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            proc.Start();
            proc.StandardOutput.ReadToEnd();
            proc.StandardError.ReadToEnd();
            proc.WaitForExit(TimeSpan.FromSeconds(60));
            return proc.ExitCode;
        }
        catch
        {
            return -1;
        }
    }
}
