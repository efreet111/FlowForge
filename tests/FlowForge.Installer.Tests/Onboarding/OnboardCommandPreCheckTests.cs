using System.IO;
using FlowForge.Installer.Commands;
using FlowForge.Installer.Infrastructure;
using Xunit;

namespace FlowForge.Installer.Tests.Onboarding;

/// <summary>
/// FR-002: Tests for the config pre-check logic (exit code 2 on missing/corrupt/empty-user config).
/// Uses the internal seam <see cref="OnboardCommand.CheckConfig"/> for deterministic testing
/// without depending on the real ~/.engram/config.json path.
/// </summary>
public class OnboardCommandPreCheckTests
{
    [Fact] // FR-002 Scenario A — config file missing
    public void PreCheck_ConfigFileMissing_ReturnsExitCode2Hint()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"ff-precheck-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var configFile = Path.Combine(tempDir, "config.json");
            // configFile does NOT exist
            var logFile = Path.Combine(tempDir, "install.log");
            using var store = new TestConfigStore(configFile, logFile);

            var (passed, hint) = OnboardCommand.CheckConfig(configFile, store.Store);

            Assert.False(passed);
            Assert.NotNull(hint);
            Assert.Contains("missing", hint!, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact] // FR-002 Scenario B — config file corrupt/unparseable
    public void PreCheck_ConfigFileCorrupt_ReturnsExitCode2Hint()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"ff-precheck-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var configFile = Path.Combine(tempDir, "config.json");
            // Write corrupt JSON — ConfigStore.Load() catches the exception and returns defaults
            // (with null Sync), so sync.user will be empty → exit 2.
            File.WriteAllText(configFile, "{ this is not valid json !!!");
            var logFile = Path.Combine(tempDir, "install.log");
            using var store = new TestConfigStore(configFile, logFile);

            var (passed, hint) = OnboardCommand.CheckConfig(configFile, store.Store);

            Assert.False(passed);
            Assert.NotNull(hint);
            // ConfigStore.Load() swallows the parse error and returns defaults (Sync=null),
            // so CheckConfig sees empty sync.user and reports it.
            Assert.Contains("sync.user", hint!, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact] // FR-002 Scenario C — config file present but sync.user empty/whitespace
    public void PreCheck_SyncUserEmpty_ReturnsExitCode2Hint()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"ff-precheck-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var configFile = Path.Combine(tempDir, "config.json");
            // Valid JSON but sync.user is empty string
            File.WriteAllText(configFile, """{"version":"0.1.0","sync":{"user":"","mode":"local"}}""");
            var logFile = Path.Combine(tempDir, "install.log");
            using var store = new TestConfigStore(configFile, logFile);

            var (passed, hint) = OnboardCommand.CheckConfig(configFile, store.Store);

            Assert.False(passed);
            Assert.NotNull(hint);
            Assert.Contains("sync.user", hint!, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact] // FR-002 — sync.user is whitespace only
    public void PreCheck_SyncUserWhitespace_ReturnsExitCode2Hint()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"ff-precheck-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var configFile = Path.Combine(tempDir, "config.json");
            File.WriteAllText(configFile, """{"version":"0.1.0","sync":{"user":"   ","mode":"local"}}""");
            var logFile = Path.Combine(tempDir, "install.log");
            using var store = new TestConfigStore(configFile, logFile);

            var (passed, hint) = OnboardCommand.CheckConfig(configFile, store.Store);

            Assert.False(passed);
            Assert.NotNull(hint);
            Assert.Contains("sync.user", hint!, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact] // FR-002 — positive case: valid config with sync.user set
    public void PreCheck_ValidConfigWithSyncUser_ReturnsPassed()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"ff-precheck-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var configFile = Path.Combine(tempDir, "config.json");
            File.WriteAllText(configFile, """{"version":"0.1.0","sync":{"user":"alice","mode":"local"}}""");
            var logFile = Path.Combine(tempDir, "install.log");
            using var store = new TestConfigStore(configFile, logFile);

            var (passed, hint) = OnboardCommand.CheckConfig(configFile, store.Store);

            Assert.True(passed);
            Assert.Null(hint);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact] // FR-002 — sync section entirely absent (null)
    public void PreCheck_SyncSectionNull_ReturnsExitCode2Hint()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"ff-precheck-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var configFile = Path.Combine(tempDir, "config.json");
            // Valid JSON but no "sync" key at all → Sync is null → sync.user is null
            File.WriteAllText(configFile, """{"version":"0.1.0"}""");
            var logFile = Path.Combine(tempDir, "install.log");
            using var store = new TestConfigStore(configFile, logFile);

            var (passed, hint) = OnboardCommand.CheckConfig(configFile, store.Store);

            Assert.False(passed);
            Assert.NotNull(hint);
            Assert.Contains("sync.user", hint!, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    /// <summary>
    /// Helper: wraps a ConfigStore + temp log file, implements IDisposable for cleanup.
    /// </summary>
    sealed class TestConfigStore : IDisposable
    {
        public ConfigStore Store { get; }

        public TestConfigStore(string configFile, string logFile)
        {
            Store = new ConfigStore(configFile, new InstallerLogger(logFile));
        }

        public void Dispose() { /* temp dir cleanup is handled by the caller */ }
    }
}
