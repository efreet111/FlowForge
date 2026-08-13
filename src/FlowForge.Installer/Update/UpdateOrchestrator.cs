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
            _ctx.Log.UpdateOperation("engram", currentVersion, "", null, null, "failed");
            return new UpdateResult(UpdateComponent.Engram, currentVersion, "",
                UpdateStatus.Failed, $"Failed to fetch latest version: {ex.Message}");
        }

        if (latestVersion == null)
        {
            _ctx.Log.UpdateOperation("engram", currentVersion, "", null, null, "failed");
            return new UpdateResult(UpdateComponent.Engram, currentVersion, "",
                UpdateStatus.Failed, "Could not determine latest version");
        }

        // 2. Idempotency check
        if (!options.Force && _registry.IsAtVersion(UpdateComponent.Engram, latestVersion))
        {
            AnsiConsole.MarkupLine("[grey]⋯[/] engram-dotnet already at latest version");
            _ctx.Log.UpdateOperation("engram", currentVersion, latestVersion, null, null, "skipped-already-latest");
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
            _ctx.Log.UpdateOperation("engram", currentVersion, latestVersion, null, null, "failed");
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
                _ctx.Log.UpdateOperation("engram", currentVersion, latestVersion, null, null, "failed");
                return new UpdateResult(UpdateComponent.Engram, currentVersion, latestVersion,
                    UpdateStatus.Failed, "Download failed");
            }

            // 6. Health-check temp binary
            var healthResult = await _healthCheck.CheckBinaryAsync(tempPath, latestVersion, ct);
            if (!healthResult.Passed)
            {
                SafeDelete(tempPath);
                _ctx.Log.Error($"UpdateEngram: health-check failed: {healthResult.Detail}");
                _ctx.Log.UpdateOperation("engram", currentVersion, latestVersion, null, null, "failed");
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

            _ctx.Log.UpdateOperation("engram", currentVersion, latestVersion, null, null, "success");
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
                _ctx.Log.UpdateOperation("engram", currentVersion, latestVersion, null, null, "rolled-back");
                _ctx.Log.Info("UpdateEngram: rolled back from backup");
                return new UpdateResult(UpdateComponent.Engram, currentVersion, latestVersion,
                    UpdateStatus.RolledBack, ex.Message);
            }

            _ctx.Log.UpdateOperation("engram", currentVersion, latestVersion, null, null, "failed");
            return new UpdateResult(UpdateComponent.Engram, currentVersion, latestVersion,
                UpdateStatus.Failed, ex.Message);
        }
    }

    /// <summary>
    /// Update FlowForge skills for detected IDEs.
    /// Copies files from cache to each IDE destination, runs UserModifiedAgentDetector
    /// before overwriting (FR-006), and writes sidecar per IDE (FR-011).
    /// </summary>
    async Task<UpdateResult> UpdateSkillsAsync(UpdateOptions options, CancellationToken ct)
    {
        var currentVersion = _registry.GetVersion(UpdateComponent.FlowForgeSkills) ?? "(no instalado)";
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        // 1. Refresh cache
        var cachePath = _cacheRefresher.RefreshCache(_ctx.Log);
        if (cachePath == null)
        {
            _ctx.Log.UpdateOperation("flowforge-skills", currentVersion, "", null, null, "failed");
            return new UpdateResult(UpdateComponent.FlowForgeSkills, currentVersion, "",
                UpdateStatus.Failed, "Failed to refresh FlowForge cache");
        }

        // 2. Detect IDEs (use existing config or detect from filesystem)
        var cfg = _ctx.Store.Load();
        var ides = cfg.Components.FlowForge?.Ides ?? DetectInstalledIdes();

        if (ides.Count == 0)
        {
            _ctx.Log.UpdateOperation("flowforge-skills", currentVersion, "", null, null, "skipped");
            return new UpdateResult(UpdateComponent.FlowForgeSkills, currentVersion, "",
                UpdateStatus.SkippedAlreadyLatest, "No IDEs detected");
        }

        // 3. For each IDE: detect modifications, copy files, write sidecar
        try
        {
            var allManagedPaths = new Dictionary<string, IReadOnlyList<string>>();

            foreach (var ide in ides)
            {
                _ctx.Log.Info($"UpdateSkills: processing {ide}");

                // 3a. Run UserModifiedAgentDetector before overwriting (FR-006)
                var agentDirs = GetAgentDirsForIde(ide, home, cachePath);
                foreach (var (installedDir, sourceDir, pattern) in agentDirs)
                {
                    _ctx.Log.Info($"UpdateSkills: checking for modified agents in {ide} ({installedDir} vs {sourceDir})...");
                    var reports = _agentDetector.DetectModifications(installedDir, sourceDir, pattern);
                    var modifiedFiles = reports.Where(r => r.IsModified).ToList();
                    _ctx.Log.Info($"UpdateSkills: found {modifiedFiles.Count} modified file(s) in {ide}");

                    if (modifiedFiles.Count > 0)
                    {
                        // Show user-visible feedback for each modified file
                        foreach (var mf in modifiedFiles)
                        {
                            AnsiConsole.MarkupLine($"  [yellow]⚠[/] {mf.FilePath} fue modificado por el usuario");
                        }

                        if (options.Force)
                        {
                            // --force: overwrite without backup
                            _ctx.Log.Info($"UpdateSkills: --force flag, overwriting {modifiedFiles.Count} modified file(s) without backup");
                            AnsiConsole.MarkupLine($"  [grey]⋯ --force: sobrescribiendo sin backup[/]");
                        }
                        else if (options.Yes)
                        {
                            // --yes: auto-backup + overwrite
                            _ctx.Log.Info($"UpdateSkills: --yes flag, auto-backup + overwrite {modifiedFiles.Count} modified file(s)");
                            var backupPath = BackupModifiedFiles(modifiedFiles, installedDir, ide);
                            if (backupPath != null)
                                AnsiConsole.MarkupLine($"  [green]✓[/] Backup creado en {backupPath}");
                        }
                        else
                        {
                            // Interactive: prompt user for action
                            var choice = AnsiConsole.Prompt(
                                new Spectre.Console.SelectionPrompt<string>()
                                    .Title($"[yellow]{modifiedFiles.Count} archivo(s) modificado(s). ¿Qué hacer?[/]")
                                    .AddChoices(new[] {
                                        "[B]ackup + overwrite (recomendado)",
                                        "[O]verwrite sin backup",
                                        "[S]kip (no sobrescribir)"
                                    }));

                            if (choice.StartsWith("[S]"))
                            {
                                // Skip: don't copy files for this IDE
                                _ctx.Log.Info($"UpdateSkills: user chose Skip for {ide}");
                                AnsiConsole.MarkupLine($"  [yellow]⊘[/] {ide} → omitido (archivos del usuario preservados)");
                                continue; // Skip to next IDE
                            }
                            else if (choice.StartsWith("[O]"))
                            {
                                // Overwrite without backup
                                _ctx.Log.Info($"UpdateSkills: user chose Overwrite without backup for {ide}");
                                AnsiConsole.MarkupLine($"  [grey]⋯[/] Sobrescribiendo sin backup[/]");
                            }
                            else
                            {
                                // Backup + overwrite (default/recommended)
                                _ctx.Log.Info($"UpdateSkills: user chose Backup + Overwrite for {ide}");
                                var backupPath = BackupModifiedFiles(modifiedFiles, installedDir, ide);
                                if (backupPath != null)
                                    AnsiConsole.MarkupLine($"  [green]✓[/] Backup creado en {backupPath}");
                            }
                        }
                    }
                }

                // 3b. Copy files from cache to IDE destination
                var managedPaths = FlowForgeModule.CopySkillsForIde(ide, home, cachePath);
                allManagedPaths[ide] = managedPaths;
                _ctx.Log.Info($"UpdateSkills: copied {managedPaths.Count} file(s) for {ide}");

                // 3c. Write sidecar per IDE (FR-011)
                try
                {
                    ManagedPathsSidecarFactory.WriteSidecar(ide, managedPaths);
                    _ctx.Log.Info($"UpdateSkills: sidecar written for {ide}");
                }
                catch (Exception ex)
                {
                    _ctx.Log.Warn($"UpdateSkills: sidecar write failed for {ide}: {ex.Message}");
                }

                AnsiConsole.MarkupLine($"  [green]✓[/] {ide} → skills actualizados");
            }

            // 4. Update version
            var newVersion = options.Tag ?? DateTime.UtcNow.ToString("yyyy.MM.dd");
            _registry.SetVersion(UpdateComponent.FlowForgeSkills, newVersion);

            var totalFiles = allManagedPaths.Values.Sum(v => v.Count);
            _ctx.Log.UpdateOperation("flowforge-skills", currentVersion, newVersion, null, null, "success");
            _ctx.Log.Info($"UpdateSkills: success {currentVersion} → {newVersion} ({totalFiles} files across {ides.Count} IDEs)");
            AnsiConsole.MarkupLine($"  [green]✓[/] FlowForge skills actualizados");

            return new UpdateResult(UpdateComponent.FlowForgeSkills, currentVersion, newVersion,
                UpdateStatus.Success);
        }
        catch (Exception ex)
        {
            _ctx.Log.Error($"UpdateSkills: {ex.Message}");
            _ctx.Log.UpdateOperation("flowforge-skills", currentVersion, "", null, null, "failed");
            return new UpdateResult(UpdateComponent.FlowForgeSkills, currentVersion, "",
                UpdateStatus.Failed, ex.Message);
        }
    }

    /// <summary>
    /// Returns (installedDir, sourceDir, pattern) tuples for agent detection per IDE.
    /// </summary>
    static List<(string InstalledDir, string SourceDir, string Pattern)> GetAgentDirsForIde(
        string ide, string home, string cachePath)
    {
        var dirs = new List<(string, string, string)>();
        var ideLower = ide.ToLowerInvariant();

        switch (ideLower)
        {
            case "cursor":
                dirs.Add((
                    Path.Combine(home, ".cursor", "agents"),
                    Path.Combine(cachePath, "ide", "cursor", "agents"),
                    "forge-*.md"));
                break;
            case "opencode":
                dirs.Add((
                    Path.Combine(home, ".config", "opencode", "agents"),
                    Path.Combine(cachePath, "ide", "opencode", "agents"),
                    "*.md"));
                break;
            case "antigravity":
                dirs.Add((
                    Path.Combine(home, ".gemini", "config", "rules"),
                    Path.Combine(cachePath, "ide", "antigravity", "rules"),
                    "*.md"));
                break;
            case "vs code" or "vscode" or "copilot":
                dirs.Add((
                    Path.Combine(home, ".copilot", "agents"),
                    Path.Combine(cachePath, "ide", "vscode", "agents"),
                    "*.agent.md"));
                break;
            case "kilo":
                dirs.Add((
                    Path.Combine(home, ".config", "kilo", "agents"),
                    Path.Combine(cachePath, "ide", "opencode", "agents"),
                    "*.md"));
                break;
        }

        return dirs;
    }

    /// <summary>
    /// Backs up user-modified files before overwriting.
    /// Copies each modified file to a timestamped backup directory.
    /// Returns the backup directory path, or null on failure.
    /// </summary>
    string? BackupModifiedFiles(List<ModifiedFileReport> modifiedFiles, string installedDir, string ide)
    {
        try
        {
            var backupDir = Path.Combine(PathHelper.FlowForgeBackupDir, $"skills-{ide}-{DateTime.UtcNow:yyyyMMdd-HHmmss}");
            Directory.CreateDirectory(backupDir);

            var backedUpCount = 0;
            foreach (var report in modifiedFiles)
            {
                // report.FilePath is relative to installedDir
                var sourceFile = Path.Combine(installedDir, report.FilePath);
                if (!File.Exists(sourceFile))
                {
                    _ctx.Log.Warn($"UpdateSkills: modified file not found for backup: {sourceFile}");
                    continue;
                }

                // Preserve relative directory structure in backup
                var destFile = Path.Combine(backupDir, report.FilePath);
                var destDir = Path.GetDirectoryName(destFile);
                if (destDir != null)
                    Directory.CreateDirectory(destDir);

                File.Copy(sourceFile, destFile, overwrite: true);
                backedUpCount++;
                _ctx.Log.Info($"UpdateSkills: backed up {report.FilePath} → {destFile}");
            }

            _ctx.Log.Info($"UpdateSkills: backup complete — {backedUpCount} file(s) → {backupDir}");
            return backupDir;
        }
        catch (Exception ex)
        {
            _ctx.Log.Warn($"UpdateSkills: backup of modified files failed: {ex.Message}");
            return null;
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
