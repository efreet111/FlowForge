using FlowForge.Installer.Infrastructure;
using FlowForge.Installer.Update;
using Xunit;

namespace FlowForge.Installer.Tests.Regression;

/// <summary>
/// [SEC] Security audit tests: verify updater code NEVER writes to protected paths.
/// RNF-SEC-005: engram.db, -wal, -shm, local_memory/ must be read-only for updater.
/// </summary>
public class SecurityAuditTests
{
    [Fact]
    public void UpdateOrchestrator_DoesNotReferenceProtectedPaths()
    {
        // Verify that the Update/ directory source files don't contain
        // write operations to engram.db or local_memory paths
        var updateDir = FindUpdateDirectory();
        if (updateDir == null) return; // Skip if not found (CI environment)

        var sourceFiles = Directory.GetFiles(updateDir, "*.cs", SearchOption.AllDirectories);
        foreach (var file in sourceFiles)
        {
            var content = File.ReadAllText(file);
            // These patterns should NOT appear in Update/ source code
            Assert.DoesNotContain("engram.db", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("local_memory", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("-wal", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("-shm", content, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void BackupManager_OnlyWritesToBackupDir()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"sec-{Guid.NewGuid():N}");
        var backupDir = Path.Combine(tempDir, "backups");
        Directory.CreateDirectory(tempDir);
        try
        {
            var log = new InstallerLogger(Path.Combine(tempDir, "install.log"));
            var manager = new BackupManager(log, backupDir);

            var sourceFile = Path.Combine(tempDir, "test.txt");
            File.WriteAllText(sourceFile, "data");

            var backupPath = manager.CreateBackup(sourceFile, "test");

            // Verify backup is under the backup root
            Assert.StartsWith(backupDir, backupPath);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ComponentRegistry_OnlyWritesToConfigFile()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"sec-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var configFile = Path.Combine(tempDir, "config.json");
            var log = new InstallerLogger(Path.Combine(tempDir, "install.log"));
            var store = new ConfigStore(configFile, log);
            var registry = new ComponentRegistry(store);

            registry.SetVersion(UpdateComponent.Engram, "1.0.0");

            // Verify only config.json was created in tempDir
            var files = Directory.GetFiles(tempDir);
            Assert.All(files, f =>
                Assert.True(
                    Path.GetFileName(f) == "config.json" || Path.GetFileName(f) == "install.log",
                    $"Unexpected file: {f}"));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    static string? FindUpdateDirectory()
    {
        // Walk up from test assembly to find src/FlowForge.Installer/Update/
        var dir = AppContext.BaseDirectory;
        for (int i = 0; i < 10; i++)
        {
            var updateDir = Path.Combine(dir, "src", "FlowForge.Installer", "Update");
            if (Directory.Exists(updateDir))
                return updateDir;

            var parent = Directory.GetParent(dir)?.FullName;
            if (parent == null) break;
            dir = parent;
        }
        return null;
    }
}
