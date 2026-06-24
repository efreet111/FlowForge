using System.Diagnostics;
using Spectre.Console;

namespace FlowForge.Installer.Infrastructure;

/// <summary>
/// Resuelve la ruta al repo FlowForge (local, env, o cache tras git clone).
/// Necesario cuando el binario AOT se instala sin el repo junto al ejecutable.
/// </summary>
public sealed class FlowForgeRepoLocator(InstallerLogger log)
{
    const string RepoUrl = "https://github.com/efreet111/FlowForge.git";

    public string? Locate()
    {
        var envRepo = Environment.GetEnvironmentVariable("FLOWFORGE_REPO");
        if (envRepo != null && Directory.Exists(envRepo))
            return envRepo;

        var dir = AppContext.BaseDirectory;
        for (int i = 0; i < 5; i++)
        {
            var agents = Path.Combine(dir, "AGENTS.md");
            if (File.Exists(agents) && File.ReadAllText(agents).Contains("FlowForge", StringComparison.Ordinal))
                return dir;

            var parent = Directory.GetParent(dir)?.FullName;
            if (parent == null) break;
            dir = parent;
        }

        var cache = CachePath;
        if (File.Exists(Path.Combine(cache, "AGENTS.md")))
            return cache;

        return null;
    }

    /// <summary>Localiza el repo o clona a ~/.flowforge/cache/FlowForge.</summary>
    public bool EnsureAvailable(out string? repoPath)
    {
        repoPath = Locate();
        if (repoPath != null)
            return true;

        return TryClone(out repoPath);
    }

    static string CachePath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".flowforge", "cache", "FlowForge");

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

    static bool IsGitAvailable()
    {
        try { return RunGit("--version") == 0; }
        catch { return false; }
    }

    static int RunGit(string arguments)
    {
        using var proc = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };
        proc.Start();
        proc.StandardOutput.ReadToEnd();
        proc.StandardError.ReadToEnd();
        proc.WaitForExit();
        return proc.ExitCode;
    }
}
