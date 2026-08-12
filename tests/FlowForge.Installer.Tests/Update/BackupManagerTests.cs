using FlowForge.Installer.Infrastructure;
using FlowForge.Installer.Update;
using Xunit;

namespace FlowForge.Installer.Tests.Update;

public class BackupManagerTests
{
    static (BackupManager manager, string tempDir, string backupDir) CreateManager()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"flowforge-backup-{Guid.NewGuid():N}");
        var backupDir = Path.Combine(tempDir, "backups");
        Directory.CreateDirectory(tempDir);
        var log = new InstallerLogger(Path.Combine(tempDir, "install.log"));
        var manager = new BackupManager(log, backupDir);
        return (manager, tempDir, backupDir);
    }

    [Fact]
    public void CreateBackup_File_CreatesBackupInTimestampedDir()
    {
        var (manager, tempDir, backupDir) = CreateManager();
        try
        {
            var sourceFile = Path.Combine(tempDir, "test.txt");
            File.WriteAllText(sourceFile, "hello");

            var backupPath = manager.CreateBackup(sourceFile, "test-component");

            Assert.True(Directory.Exists(backupPath));
            var files = Directory.GetFiles(backupPath, "*", SearchOption.AllDirectories);
            Assert.Single(files);
            Assert.Equal("hello", File.ReadAllText(files[0]));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void CreateBackup_Directory_CopiesAllFiles()
    {
        var (manager, tempDir, backupDir) = CreateManager();
        try
        {
            var sourceDir = Path.Combine(tempDir, "source");
            Directory.CreateDirectory(sourceDir);
            File.WriteAllText(Path.Combine(sourceDir, "a.txt"), "aaa");
            File.WriteAllText(Path.Combine(sourceDir, "b.txt"), "bbb");

            var backupPath = manager.CreateBackup(sourceDir, "test-component");

            Assert.True(Directory.Exists(backupPath));
            var files = Directory.GetFiles(backupPath, "*", SearchOption.AllDirectories);
            Assert.Equal(2, files.Length);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void CreateBackup_NonExistentSource_Throws()
    {
        var (manager, tempDir, _) = CreateManager();
        try
        {
            Assert.Throws<FileNotFoundException>(
                () => manager.CreateBackup("/nonexistent/path", "test"));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void TryRestoreLatest_File_RestoresContent()
    {
        var (manager, tempDir, backupDir) = CreateManager();
        try
        {
            var sourceFile = Path.Combine(tempDir, "original.txt");
            File.WriteAllText(sourceFile, "original content");
            manager.CreateBackup(sourceFile, "mycomp");

            // Overwrite original
            File.WriteAllText(sourceFile, "modified content");
            Assert.Equal("modified content", File.ReadAllText(sourceFile));

            // Restore
            var restored = manager.TryRestoreLatest("mycomp", sourceFile);
            Assert.True(restored);
            Assert.Equal("original content", File.ReadAllText(sourceFile));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void TryRestoreLatest_NoBackup_ReturnsFalse()
    {
        var (manager, tempDir, _) = CreateManager();
        try
        {
            var target = Path.Combine(tempDir, "target.txt");
            Assert.False(manager.TryRestoreLatest("nonexistent", target));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void PruneOldBackups_ExceedsMax_RemovesOldest()
    {
        var (manager, tempDir, backupDir) = CreateManager();
        try
        {
            var sourceFile = Path.Combine(tempDir, "test.txt");
            File.WriteAllText(sourceFile, "data");

            // Create 6 backups (max is 5)
            for (int i = 0; i < 6; i++)
            {
                manager.CreateBackup(sourceFile, "prune-test");
                Thread.Sleep(5); // Ensure unique millisecond timestamps
            }

            var remaining = manager.ListBackups("prune-test");
            Assert.Equal(BackupManager.MaxBackupsPerComponent, remaining.Count);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ListBackups_ReturnsSortedDesc()
    {
        var (manager, tempDir, backupDir) = CreateManager();
        try
        {
            var sourceFile = Path.Combine(tempDir, "test.txt");
            File.WriteAllText(sourceFile, "data");

            manager.CreateBackup(sourceFile, "sort-test");
            Thread.Sleep(5);
            manager.CreateBackup(sourceFile, "sort-test");
            Thread.Sleep(5);
            manager.CreateBackup(sourceFile, "sort-test");

            var backups = manager.ListBackups("sort-test");
            Assert.Equal(3, backups.Count);

            // Verify descending order (most recent first)
            for (int i = 0; i < backups.Count - 1; i++)
            {
                Assert.True(
                    string.Compare(Path.GetFileName(backups[i]), Path.GetFileName(backups[i + 1]),
                        StringComparison.Ordinal) >= 0,
                    $"Backups not sorted desc: {backups[i]} should come before {backups[i + 1]}");
            }
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ListBackups_Empty_ReturnsEmptyList()
    {
        var (manager, tempDir, _) = CreateManager();
        try
        {
            var backups = manager.ListBackups("nonexistent");
            Assert.Empty(backups);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }
}
