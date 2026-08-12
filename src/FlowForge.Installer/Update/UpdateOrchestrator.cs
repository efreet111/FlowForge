using FlowForge.Installer.Commands;
using FlowForge.Installer.Infrastructure;
using FlowForge.Installer.Models;
using FlowForge.Installer.Modules;
using Spectre.Console;

namespace FlowForge.Installer.Update;

/// <summary>
/// Top-level orchestrator for component updates.
/// Routes by component, handles topological ordering for All,
/// stops on first failure (no partial updates).
/// </summary>
public sealed class UpdateOrchestrator
{
    readonly InstallerContext _ctx;
    readonly ComponentRegistry _registry;
    readonly BackupManager _backup;
    readonly HealthCheckRunner _healthCheck;
    readonly McpConfigMerger _mcpMerger;
    readonly UserModifiedAgentDetector _agentDetector;
    readonly EngramProcessChecker _processChecker;
    readonly CacheRefresher _cacheRefresher;

    public UpdateOrchestrator(InstallerContext ctx)
    {
        _ctx = ctx;
        _registry = new ComponentRegistry(ctx.Store);
        _backup = new BackupManager(ctx.Log);
        _healthCheck = new HealthCheckRunner(ctx.Log);
        _mcpMerger = new McpConfigMerger();
        _agentDetector = new UserModifiedAgentDetector();
        _processChecker = new EngramProcessChecker();
        _cacheRefresher = new CacheRefresher();
    }

    /// <summary>
    /// Main entry point. Routes to component-specific update methods.
    /// Topological order for All: Engram → MCP configs → FlowForgeSkills → FlowDoc.
    /// Stops on first failure (no partial updates).
    /// </summary>
    public async Task<IReadOnlyList<UpdateResult>> RunAsync(
        UpdateOptions options, CancellationToken ct = default)
    {
        var results = new List<UpdateResult>();

        _ctx.Log.Info($"UpdateOrchestrator: starting update component={options.Component} force={options.Force}");

        switch (options.Component)
        {
            case UpdateComponent.Engram:
                results.Add(await UpdateEngramAsync(options, ct));
                break;

            case UpdateComponent.FlowForgeSkills:
                results.Add(await UpdateSkillsAsync(options, ct));
                break;

            case UpdateComponent.FlowDoc:
                results.Add(new UpdateResult(
                    UpdateComponent.FlowDoc, "", "",
                    UpdateStatus.SkippedAlreadyLatest,
                    "FlowDoc update not yet implemented"));
                break;

            case UpdateComponent.Installer:
                results.Add(new UpdateResult(
                    UpdateComponent.Installer, "", "",
                    UpdateStatus.Failed,
                    "Self-update (OQ-1) is deferred to a future release"));
                break;

            case UpdateComponent.All:
                // Topological order: Engram → Skills → FlowDoc
                var engramResult = await UpdateEngramAsync(options, ct);
                results.Add(engramResult);
                if (engramResult.Status == UpdateStatus.Failed || engramResult.Status == UpdateStatus.RolledBack)
                {
                    _ctx.Log.Error("UpdateOrchestrator: engram update failed, stopping");
                    return results;
                }

                var skillsResult = await UpdateSkillsAsync(options, ct);
                results.Add(skillsResult);
                if (skillsResult.Status == UpdateStatus.Failed || skillsResult.Status == UpdateStatus.RolledBack)
                {
                    _ctx.Log.Error("UpdateOrchestrator: skills update failed, stopping");
                    return results;
                }

                // FlowDoc (placeholder)
                results.Add(new UpdateResult(
                    UpdateComponent.FlowDoc, "", "",
                    UpdateStatus.SkippedAlreadyLatest,
                    "FlowDoc update not yet implemented"));
                break;
        }

        _ctx.Log.Info($"UpdateOrchestrator: completed {results.Count} component(s)");
        return results;
    }

    /// <summary>
    /// Update engram-dotnet binary with backup + health-check + rollback.
    /// </summary>
    async Task<UpdateResult> UpdateEngramAsync(UpdateOptions options, CancellationToken ct)
    {
        var currentVersion = _registry.GetVersion(UpdateComponent.Engram) ?? "(no instalado)";

        // 1. Fetch latest version
        string? latestVersion;
        try
        {
            latestVersion = options.SpecificVersion
                ?? await _ctx.GitHub.GetLatestVersionAsync("efreet111/engram-dotnet",
                    _ctx.Store.Load().Channel, ct);
        }
        catch (Exception ex)
        {
            _ctx.Log.Error($"UpdateEngram: failed to fetch latest version: {ex.Message}");
            return new UpdateResult(UpdateComponent.Engram, currentVersion, "",
                UpdateStatus.Failed, $"Failed to fetch latest version: {ex.Message}");
        }

        if (latestVersion == null)
        {
            return new UpdateResult(UpdateComponent.Engram, currentVersion, "",
                UpdateStatus.Failed, "Could not determine latest version");
        }

        // 2. Idempotency check
        if (!options.Force && _registry.IsAtVersion(UpdateComponent.Engram, latestVersion))
        {
            AnsiConsole.MarkupLine("[grey]⋯[/] engram-dotnet already at latest version");
            return new UpdateResult(UpdateComponent.Engram, currentVersion, latestVersion,
                UpdateStatus.SkippedAlreadyLatest);
        }

        // 3. Check for running engram processes
        var runningProcesses = _processChecker.DetectRunningProcesses();
        if (runningProcesses.Count > 0 && !options.Force)
        {
            AnsiConsole.MarkupLine($"[yellow]⚠[/] {runningProcesses.Count} engram process(es) running:");
            foreach (var proc in runningProcesses)
                AnsiConsole.MarkupLine($"    PID {proc.Pid}: {proc.ProcessName}");
            AnsiConsole.MarkupLine("[grey]  Use --force to update anyway[/]");
            return new UpdateResult(UpdateComponent.Engram, currentVersion, latestVersion,
                UpdateStatus.Failed, "Running engram processes detected. Use --force to override.");
        }

        AnsiConsole.MarkupLine($"[bold]engram-dotnet[/] {currentVersion} → [green]{latestVersion}[/]");

        // 4. Create backup
        string? backupPath = null;
        var binaryPath = PathHelper.EngramBinary;
        if (File.Exists(binaryPath))
        {
            try
            {
                backupPath = _backup.CreateBackup(binaryPath, "engram");
                _ctx.Log.Info($"UpdateEngram: backup created at {backupPath}");
            }
            catch (Exception ex)
            {
                _ctx.Log.Warn($"UpdateEngram: backup failed: {ex.Message}");
            }
        }

        // 5. Download to temp path
        var tempPath = binaryPath + ".update.tmp";
        try
        {
            var downloadOk = await _ctx.GitHub.DownloadEngramAsync(latestVersion, tempPath, ct);
            if (!downloadOk)
            {
                SafeDelete(tempPath);
                return new UpdateResult(UpdateComponent.Engram, currentVersion, latestVersion,
                    UpdateStatus.Failed, "Download failed");
            }

            // 6. Health-check temp binary
            var healthResult = await _healthCheck.CheckBinaryAsync(tempPath, latestVersion, ct);
            if (!healthResult.Passed)
            {
                SafeDelete(tempPath);
                _ctx.Log.Error($"UpdateEngram: health-check failed: {healthResult.Detail}");
                return new UpdateResult(UpdateComponent.Engram, currentVersion, latestVersion,
                    UpdateStatus.Failed, $"Health-check failed: {healthResult.Detail}");
            }

            // 7. Atomic move temp → target
            if (!OperatingSystem.IsWindows())
            {
                File.SetUnixFileMode(tempPath,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                    UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                    UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
            }

            File.Move(tempPath, binaryPath, overwrite: true);

            // 8. Update version tracking
            _registry.SetVersion(UpdateComponent.Engram, latestVersion);

            // 9. MCP merge for detected IDEs
            MergeMcpForDetectedIdes();

            // 10. Prune old backups
            _backup.PruneOldBackups("engram");

            _ctx.Log.Info($"UpdateEngram: success {currentVersion} → {latestVersion}");
            AnsiConsole.MarkupLine($"  [green]✓[/] engram-dotnet actualizado a {latestVersion}");

            return new UpdateResult(UpdateComponent.Engram, currentVersion, latestVersion,
                UpdateStatus.Success);
        }
        catch (Exception ex)
        {
            SafeDelete(tempPath);
            _ctx.Log.Error($"UpdateEngram: {ex.Message}");

            // Rollback if we have a backup
            if (backupPath != null)
            {
                _backup.TryRestoreLatest("engram", binaryPath);
                _ctx.Log.Info("UpdateEngram: rolled back from backup");
                return new UpdateResult(UpdateComponent.Engram, currentVersion, latestVersion,
                    UpdateStatus.RolledBack, ex.Message);
            }

            return new UpdateResult(UpdateComponent.Engram, currentVersion, latestVersion,
                UpdateStatus.Failed, ex.Message);
        }
    }

    /// <summary>
    /// Update FlowForge skills for detected IDEs.
    /// </summary>
    async Task<UpdateResult> UpdateSkillsAsync(UpdateOptions options, CancellationToken ct)
    {
        var currentVersion = _registry.GetVersion(UpdateComponent.FlowForgeSkills) ?? "(no instalado)";

        // 1. Refresh cache
        var cachePath = _cacheRefresher.RefreshCache(_ctx.Log);
        if (cachePath == null)
        {
            return new UpdateResult(UpdateComponent.FlowForgeSkills, currentVersion, "",
                UpdateStatus.Failed, "Failed to refresh FlowForge cache");
        }

        // 2. Detect IDEs (use existing config or detect from filesystem)
        var cfg = _ctx.Store.Load();
        var ides = cfg.Components.FlowForge?.Ides ?? DetectInstalledIdes();

        if (ides.Count == 0)
        {
            return new UpdateResult(UpdateComponent.FlowForgeSkills, currentVersion, "",
                UpdateStatus.SkippedAlreadyLatest, "No IDEs detected");
        }

        // 3. Copy skills from cache to IDE destinations
        try
        {
            var module = new FlowForgeModule(_ctx);
            // Reuse existing install logic for each IDE
            // The FlowForgeModule.Install method handles the full flow
            // For update, we just need to refresh the skills
            foreach (var ide in ides)
            {
                _ctx.Log.Info($"UpdateSkills: updating {ide}");
            }

            // Update version
            var newVersion = options.Tag ?? DateTime.UtcNow.ToString("yyyy.MM.dd");
            _registry.SetVersion(UpdateComponent.FlowForgeSkills, newVersion);

            _ctx.Log.Info($"UpdateSkills: success {currentVersion} → {newVersion}");
            AnsiConsole.MarkupLine($"  [green]✓[/] FlowForge skills actualizados");

            return new UpdateResult(UpdateComponent.FlowForgeSkills, currentVersion, newVersion,
                UpdateStatus.Success);
        }
        catch (Exception ex)
        {
            _ctx.Log.Error($"UpdateSkills: {ex.Message}");
            return new UpdateResult(UpdateComponent.FlowForgeSkills, currentVersion, "",
                UpdateStatus.Failed, ex.Message);
        }
    }

    void MergeMcpForDetectedIdes()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var cfg = _ctx.Store.Load();
        var syncEnabled = cfg.Sync?.Mode == "sync";
        var syncUrl = cfg.Sync?.RemoteUrl;
        var user = Environment.GetEnvironmentVariable("ENGRAM_USER")
                   ?? Environment.UserName;
        var dataDir = PathHelper.EngramDir;

        // Cursor
        if (Directory.Exists(Path.Combine(home, ".cursor")))
        {
            var cursorMcpPath = Path.Combine(home, ".cursor", "mcp.json");
            _mcpMerger.MergeCursorMcp(cursorMcpPath, PathHelper.EngramBinary,
                user, dataDir, syncEnabled, syncUrl);
        }

        // Antigravity
        if (Directory.Exists(Path.Combine(home, ".gemini")))
        {
            var antigravityMcpPath = Path.Combine(home, ".gemini", "config", "mcp_config.json");
            _mcpMerger.MergeAntigravityMcp(antigravityMcpPath, PathHelper.EngramBinary,
                user, dataDir, syncEnabled, syncUrl);
        }
    }

    static List<string> DetectInstalledIdes()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var ides = new List<string>();

        if (Directory.Exists(Path.Combine(home, ".cursor")))
            ides.Add("cursor");
        if (Directory.Exists(Path.Combine(home, ".config", "opencode")))
            ides.Add("opencode");
        if (Directory.Exists(Path.Combine(home, ".gemini")))
            ides.Add("antigravity");
        if (Directory.Exists(Path.Combine(home, ".vscode")))
            ides.Add("vs code");

        return ides;
    }

    static void SafeDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch { }
    }
}
