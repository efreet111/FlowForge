using System.Diagnostics;
using System.IO;
using FlowForge.Installer.Infrastructure;
using Xunit;

namespace FlowForge.Installer.Tests.Infrastructure;

/// <summary>
/// Tests del refresh de cache en FlowForgeRepoLocator
/// (rework hotfix-opencode-commands-missing: cache stale nunca recibía git pull).
/// Los tests basados en git se ejecutan solo si git está disponible (CI ubuntu/windows: sí).
/// </summary>
public class FlowForgeRepoLocatorTests
{
    static string CreateTempDir() =>
        Path.Combine(Path.GetTempPath(), $"ff-locator-{Guid.NewGuid():N}");

    static bool GitAvailable()
    {
        try
        {
            using var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            proc.Start();
            proc.WaitForExit(TimeSpan.FromSeconds(30));
            return proc.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    static void Git(string arguments, string workingDir)
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
    }

    static void CommitFile(string repoDir, string relativePath, string content, string message)
    {
        var full = Path.Combine(repoDir, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, content);
        Git("add -A", repoDir);
        Git($"-c user.name=test -c user.email=test@example.com commit -m \"{message}\"", repoDir);
    }

    static (string? Repo, string? Workspace) ClearRepoEnvVars()
    {
        var repo = Environment.GetEnvironmentVariable("FLOWFORGE_REPO");
        var ws = Environment.GetEnvironmentVariable("GITHUB_WORKSPACE");
        Environment.SetEnvironmentVariable("FLOWFORGE_REPO", null);
        Environment.SetEnvironmentVariable("GITHUB_WORKSPACE", null);
        return (repo, ws);
    }

    static void RestoreRepoEnvVars(string? repo, string? workspace)
    {
        Environment.SetEnvironmentVariable("FLOWFORGE_REPO", repo);
        Environment.SetEnvironmentVariable("GITHUB_WORKSPACE", workspace);
    }

    static void DeleteDir(string dir)
    {
        try
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, true);
        }
        catch
        {
            // best effort cleanup — no romper el test por un temp dir colgado
        }
    }

    [Fact]
    public void EnsureAvailable_StaleCache_PullsUpstreamCommits()
    {
        if (!GitAvailable()) return;

        var root = CreateTempDir();
        var (envRepo, envWs) = ClearRepoEnvVars();
        try
        {
            // 1. Work repo con el contenido inicial del cache
            var work = Path.Combine(root, "work");
            Directory.CreateDirectory(work);
            Git("-c init.defaultBranch=main init", work);
            CommitFile(work, "AGENTS.md", "# FlowForge\n", "add AGENTS.md");
            CommitFile(work, Path.Combine("ide", "opencode", "config", "agent-models.json"), "{}\n", "add agent-models.json");

            // 2. Bare remote + push
            var remote = Path.Combine(root, "remote.git");
            Git($"init --bare -b main \"{remote}\"", root);
            Git($"remote add origin \"{remote}\"", work);
            Git("push origin main", work);

            // 3. Clonar al cache (stale: antes de que lleguen los commands)
            var cache = Path.Combine(root, "cache");
            Git($"clone \"{remote}\" \"{cache}\"", root);
            Assert.True(File.Exists(Path.Combine(cache, "ide", "opencode", "config", "agent-models.json")));
            Assert.False(File.Exists(Path.Combine(cache, "ide", "opencode", "commands", "flow-start.md")));

            // 4. Nuevos commits upstream (los 7 commands de OpenCode)
            var commands = new[] { "flow-start", "flow-plan", "flow-dev", "flow-verify", "flow-rework", "flow-close", "flow-status" };
            foreach (var name in commands)
                CommitFile(work, Path.Combine("ide", "opencode", "commands", $"{name}.md"), $"---\ndescription: {name}\nagent: flowforge\n---\n", $"add {name}");
            Git("push origin main", work);

            // 5. EnsureAvailable debe refrescar el cache
            var logFile = Path.Combine(root, "install.log");
            var locator = new FlowForgeRepoLocator(new InstallerLogger(logFile), cache);
            var ok = locator.EnsureAvailable(out var repoPath);

            Assert.True(ok);
            Assert.Equal(cache, repoPath);
            Assert.True(File.Exists(Path.Combine(cache, "ide", "opencode", "commands", "flow-start.md")),
                "el cache debe quedar actualizado con los commits upstream");

            var logText = File.ReadAllText(logFile);
            Assert.Contains("cache refreshed:", logText);
        }
        finally
        {
            RestoreRepoEnvVars(envRepo, envWs);
            DeleteDir(root);
        }
    }

    [Fact]
    public void EnsureAvailable_RefreshFails_ContinuesWithExistingCache()
    {
        if (!GitAvailable()) return;

        var root = CreateTempDir();
        var (envRepo, envWs) = ClearRepoEnvVars();
        try
        {
            // Cache git válido pero con remote inexistente → pull/fetch fallan (simula offline)
            var cache = Path.Combine(root, "cache");
            Directory.CreateDirectory(cache);
            Git("-c init.defaultBranch=main init", cache);
            CommitFile(cache, "AGENTS.md", "# FlowForge\n", "add AGENTS.md");
            CommitFile(cache, Path.Combine("ide", "opencode", "config", "agent-models.json"), "{}\n", "add agent-models.json");
            Git($"remote add origin \"{Path.Combine(root, "no-network.git")}\"", cache);

            var logFile = Path.Combine(root, "install.log");
            var locator = new FlowForgeRepoLocator(new InstallerLogger(logFile), cache);
            var ok = locator.EnsureAvailable(out var repoPath);

            Assert.True(ok, "EnsureAvailable no debe fallar cuando el refresh no puede ejecutarse (offline)");
            Assert.Equal(cache, repoPath);
            Assert.True(File.Exists(Path.Combine(cache, "AGENTS.md")), "el cache existente debe seguir disponible");

            var logText = File.ReadAllText(logFile);
            Assert.Contains("continuing with existing cache", logText);
        }
        finally
        {
            RestoreRepoEnvVars(envRepo, envWs);
            DeleteDir(root);
        }
    }

    [Fact]
    public void EnsureAvailable_EnvRepoPath_DoesNotRefresh()
    {
        var root = CreateTempDir();
        var (envRepo, envWs) = ClearRepoEnvVars();
        try
        {
            // Repo de desarrollo (FLOWFORGE_REPO): NO es git, y si el refresh se ejecutara
            // (aunque sea para loguear) quedaría evidencia en el log.
            var devRepo = Path.Combine(root, "dev-repo");
            Directory.CreateDirectory(Path.Combine(devRepo, "ide", "opencode", "config"));
            File.WriteAllText(Path.Combine(devRepo, "AGENTS.md"), "# FlowForge dev repo\n");
            File.WriteAllText(Path.Combine(devRepo, "ide", "opencode", "config", "agent-models.json"), "{}\n");

            Environment.SetEnvironmentVariable("FLOWFORGE_REPO", devRepo);

            var cache = Path.Combine(root, "cache");
            var logFile = Path.Combine(root, "install.log");
            var locator = new FlowForgeRepoLocator(new InstallerLogger(logFile), cache);
            var ok = locator.EnsureAvailable(out var repoPath);

            Assert.True(ok);
            Assert.Equal(devRepo, repoPath);
            Assert.False(Directory.Exists(cache), "el cache no debe crearse cuando FLOWFORGE_REPO está definido");
            Assert.False(File.Exists(logFile), "repos de desarrollo no deben recibir refresh (ningún log emitido)");
        }
        finally
        {
            RestoreRepoEnvVars(envRepo, envWs);
            DeleteDir(root);
        }
    }

    [Fact]
    public void EnsureAvailable_NonGitCache_SkipsRefreshAndReturnsTrue()
    {
        var root = CreateTempDir();
        var (envRepo, envWs) = ClearRepoEnvVars();
        try
        {
            // Cache válido en contenido pero sin .git: el refresh se salta y no bloquea.
            var cache = Path.Combine(root, "cache");
            Directory.CreateDirectory(Path.Combine(cache, "ide", "opencode", "config"));
            File.WriteAllText(Path.Combine(cache, "AGENTS.md"), "# FlowForge\n");
            File.WriteAllText(Path.Combine(cache, "ide", "opencode", "config", "agent-models.json"), "{}\n");

            var logFile = Path.Combine(root, "install.log");
            var locator = new FlowForgeRepoLocator(new InstallerLogger(logFile), cache);
            var ok = locator.EnsureAvailable(out var repoPath);

            Assert.True(ok);
            Assert.Equal(cache, repoPath);

            var logText = File.ReadAllText(logFile);
            Assert.Contains("skipping refresh", logText);
        }
        finally
        {
            RestoreRepoEnvVars(envRepo, envWs);
            DeleteDir(root);
        }
    }
}
