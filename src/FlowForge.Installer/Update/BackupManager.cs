using FlowForge.Installer.Infrastructure;

namespace FlowForge.Installer.Update;

/// <summary>
/// Creates, restores, and prunes backups in ~/.flowforge-backups/.
/// Retention cap: max 5 backups per component.
/// </summary>
public sealed class BackupManager
{
    public const int MaxBackupsPerComponent = 5;

    readonly InstallerLogger _log;
    readonly string _backupRoot;

    public BackupManager(InstallerLogger log)
        : this(log, PathHelper.FlowForgeBackupDir) { }

    public BackupManager(InstallerLogger log, string backupRoot)
    {
        _log = log;
        _backupRoot = backupRoot;
    }

    /// <summary>
    /// Creates backup of file or directory. Returns backup path.
    /// Path: ~/.flowforge-backups/{component}-{timestamp}/
    /// </summary>
    public string CreateBackup(string sourcePath, string componentName)
    {
        if (!File.Exists(sourcePath) && !Directory.Exists(sourcePath))
            throw new FileNotFoundException($"Source not found: {sourcePath}");

        Directory.CreateDirectory(_backupRoot);

        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff");
        var backupDir = Path.Combine(_backupRoot, $"{componentName}-{timestamp}");
        Directory.CreateDirectory(backupDir);

        if (File.Exists(sourcePath))
        {
            var destFile = Path.Combine(backupDir, Path.GetFileName(sourcePath));
            File.Copy(sourcePath, destFile, overwrite: true);
            _log.Info($"BackupManager: file backed up → {backupDir}");
        }
        else
        {
            CopyDirectoryRecursive(sourcePath, backupDir);
            _log.Info($"BackupManager: directory backed up → {backupDir}");
        }

        PruneOldBackups(componentName);
        return backupDir;
    }

    /// <summary>
    /// Restores the most recent backup for a component.
    /// Returns true if restored, false if no backup exists.
    /// </summary>
    public bool TryRestoreLatest(string componentName, string targetPath)
    {
        var backups = ListBackups(componentName);
        if (backups.Count == 0)
        {
            _log.Warn($"BackupManager: no backups found for {componentName}");
            return false;
        }

        var latest = backups[0]; // Sorted desc, first is most recent

        try
        {
            if (File.Exists(targetPath))
            {
                // Target is a file — find the file in the backup
                var backupFiles = Directory.GetFiles(latest, "*", SearchOption.AllDirectories);
                if (backupFiles.Length == 0)
                {
                    _log.Warn($"BackupManager: backup {latest} is empty");
                    return false;
                }

                File.Copy(backupFiles[0], targetPath, overwrite: true);
            }
            else
            {
                // Target is a directory — restore all files
                CopyDirectoryRecursive(latest, targetPath);
            }

            _log.Info($"BackupManager: restored {componentName} from {latest}");
            return true;
        }
        catch (Exception ex)
        {
            _log.Error($"BackupManager: restore failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Prunes old backups, keeping only the N most recent.
    /// </summary>
    public void PruneOldBackups(string componentName)
    {
        var backups = ListBackups(componentName);
        if (backups.Count <= MaxBackupsPerComponent)
            return;

        var toDelete = backups.Skip(MaxBackupsPerComponent);
        foreach (var old in toDelete)
        {
            try
            {
                Directory.Delete(old, recursive: true);
                _log.Info($"BackupManager: pruned old backup {old}");
            }
            catch (Exception ex)
            {
                _log.Warn($"BackupManager: failed to prune {old}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Lists all backups for a component, ordered by timestamp desc (most recent first).
    /// </summary>
    public IReadOnlyList<string> ListBackups(string componentName)
    {
        if (!Directory.Exists(_backupRoot))
            return [];

        return Directory.GetDirectories(_backupRoot, $"{componentName}-*")
            .OrderByDescending(d => Path.GetFileName(d))
            .ToList();
    }

    static void CopyDirectoryRecursive(string source, string destination)
    {
        Directory.CreateDirectory(destination);

        foreach (var dir in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, dir);
            Directory.CreateDirectory(Path.Combine(destination, relative));
        }

        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, file);
            var destFile = Path.Combine(destination, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);
            File.Copy(file, destFile, overwrite: true);
        }
    }
}
