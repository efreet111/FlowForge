using System.Diagnostics;
using Spectre.Console;

namespace FlowForge.Installer.Infrastructure;

/// <summary>
/// Resuelve la ruta al repo FlowForge (local, env, o cache tras git clone).
/// Necesario cuando el binario AOT se instala sin el repo junto al ejecutable.
/// </summary>
public sealed class FlowForgeRepoLocator(InstallerLogger log, string? cachePath = null)
{
    const string RepoUrl = "https://github.com/efreet111/FlowForge.git";

    readonly string _cachePath = cachePath ?? DefaultCachePath;

    public string? Locate()
    {
        var envRepo = Environment.GetEnvironmentVariable("FLOWFORGE_REPO")
                      ?? Environment.GetEnvironmentVariable("GITHUB_WORKSPACE");
        if (envRepo != null && Directory.Exists(envRepo) && RepoHasOpenCodeTemplates(envRepo))
            return envRepo;

        var dir = AppContext.BaseDirectory;
        for (int i = 0; i < 5; i++)
        {
            var agents = Path.Combine(dir, "AGENTS.md");
            if (File.Exists(agents) && File.ReadAllText(agents).Contains("FlowForge", StringComparison.Ordinal)
                && RepoHasOpenCodeTemplates(dir))
                return dir;

            var parent = Directory.GetParent(dir)?.FullName;
            if (parent == null) break;
            dir = parent;
        }

        var cache = CachePath;
        if (File.Exists(Path.Combine(cache, "AGENTS.md")) && RepoHasOpenCodeTemplates(cache))
            return cache;

        return null;
    }

    static bool RepoHasOpenCodeTemplates(string repo) =>
        File.Exists(Path.Combine(repo, "ide", "opencode", "config", "agent-models.json"));

    /// <summary>Localiza el repo o clona a ~/.flowforge/cache/FlowForge.</summary>
    public bool EnsureAvailable(out string? repoPath)
    {
        repoPath = Locate();
        if (repoPath != null)
        {
            // Solo el cache gestionado (~/.flowforge/cache/FlowForge) recibe pull automático.
            // Repos de desarrollo (FLOWFORGE_REPO / GITHUB_WORKSPACE / walk local) nunca.
            if (IsCachePath(repoPath))
                RefreshCache(repoPath);
            return true;
        }

        return TryClone(out repoPath);
    }

    static string DefaultCachePath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".flowforge", "cache", "FlowForge");

    string CachePath => _cachePath;

    bool IsCachePath(string path) => string.Equals(
        Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
        Path.GetFullPath(CachePath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
        StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Refresca el cache con `git pull --ff-only` (fallback: fetch origin main + reset --hard origin/main).
    /// Non-blocking: si el refresh falla (offline, repo corrupto) se continúa con el cache existente.
    /// </summary>
    void RefreshCache(string cache)
    {
        if (!File.Exists(Path.Combine(cache, ".git", "HEAD")))
        {
            log.Warn("FlowForgeRepoLocator: cache is not a git checkout, skipping refresh");
            return;
        }

        if (!IsGitAvailable())
        {
            log.Warn("FlowForgeRepoLocator: git unavailable, skipping cache refresh (using existing cache)");
            return;
        }

        var oldSha = GetHeadSha(cache);

        var pullExit = RunGit("pull --ff-only", cache);
        if (pullExit != 0)
        {
            // Pull divergió o el clone es shallow: fetch + hard reset al remote como fallback.
            if (RunGit("fetch origin main", cache) == 0)
                pullExit = RunGit("reset --hard origin/main", cache);
        }

        if (pullExit != 0)
        {
            log.Warn($"FlowForgeRepoLocator: cache refresh failed (exit {pullExit}), continuing with existing cache");
            return;
        }

        var newSha = GetHeadSha(cache);
        if (oldSha != null && newSha != null && !string.Equals(oldSha, newSha, StringComparison.Ordinal))
            log.Info($"FlowForgeRepoLocator: cache refreshed: {oldSha} → {newSha}");
        else
            log.Info("FlowForgeRepoLocator: cache is up to date");
    }

    bool TryClone(out string? repoPath)
    {
        repoPath = null;
        var cache = CachePath;

        if (!IsGitAvailable())
        {
            log.Error("git no encontrado en PATH — requerido para descargar skills IDE");
            return false;
        }

        try
        {
            if (Directory.Exists(cache) && !File.Exists(Path.Combine(cache, "AGENTS.md")))
            {
                Directory.Delete(cache, true);
            }
            else if (Directory.Exists(cache) && !RepoHasOpenCodeTemplates(cache))
            {
                log.Info("FlowForgeRepoLocator: cache stale (missing OpenCode templates), refreshing");
                Directory.Delete(cache, true);
            }

            if (!Directory.Exists(cache))
            {
                AnsiConsole.MarkupLine("[grey]Descargando FlowForge desde GitHub (git clone)...[/]");
                log.Info($"FlowForgeRepoLocator: cloning to {cache}");

                var parent = Path.GetDirectoryName(cache)!;
                Directory.CreateDirectory(parent);

                var exitCode = RunGit($"clone --depth 1 {RepoUrl} \"{cache}\"");
                if (exitCode != 0)
                {
                    log.Error($"git clone falló (exit {exitCode})");
                    return false;
                }
            }

            if (File.Exists(Path.Combine(cache, "AGENTS.md")))
            {
                repoPath = cache;
                AnsiConsole.MarkupLine($"  [green]✓[/] Repo listo en [grey]{cache}[/]");
                return true;
            }
        }
        catch (Exception ex)
        {
            log.Error($"FlowForgeRepoLocator: {ex.Message}");
        }

        return false;
    }

    static string? GetHeadSha(string workingDir)
    {
        try
        {
            using var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "rev-parse HEAD",
                    WorkingDirectory = workingDir,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            proc.Start();
            var sha = proc.StandardOutput.ReadToEnd().Trim();
            proc.StandardError.ReadToEnd();
            proc.WaitForExit(TimeSpan.FromSeconds(30));
            return proc.ExitCode == 0 ? sha : null;
        }
        catch
        {
            return null;
        }
    }

    static bool IsGitAvailable()
    {
        try { return RunGit("--version") == 0; }
        catch { return false; }
    }

    static int RunGit(string arguments, string? workingDir = null)
    {
        try
        {
            using var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = arguments,
                    WorkingDirectory = workingDir ?? string.Empty,
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
