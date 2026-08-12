using System.Net;
using System.Net.Http;
using FlowForge.Installer.Infrastructure;
using FlowForge.Installer.Models;
using Xunit;

namespace FlowForge.Installer.Tests.Regression;

/// <summary>
/// [PM-REGRESSION] Baseline tests that verify existing installer behavior
/// is preserved after update mechanism changes. These tests MUST pass before
/// and after any update-related code changes.
/// </summary>
public class InstallerBaselineTests
{
    // ── ConfigStore round-trip ──────────────────────────────────────────────

    [Fact]
    public void ConfigStore_LoadSave_RoundTrip_PreservesAllFields()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"flowforge-baseline-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var configFile = Path.Combine(tempDir, "config.json");
            var logFile = Path.Combine(tempDir, "install.log");
            var log = new InstallerLogger(logFile);
            var store = new ConfigStore(configFile, log);

            // Write a config with all fields populated
            var original = new InstallerConfig
            {
                Version = "1.2.3",
                Channel = "nightly",
                AutoUpdate = true,
                FlowDoc = new FlowDocConfig { Enabled = false },
                Sync = new SyncConfig
                {
                    Mode = "sync",
                    RemoteUrl = "https://example.com",
                    User = "testuser",
                    DataDir = "/data",
                    ConnectedAt = "2024-01-01T00:00:00Z"
                },
                Components = new ComponentsConfig
                {
                    EngramDotnet = new ComponentEntry
                    {
                        Installed = true,
                        Version = "0.5.0",
                        Binary = "/usr/local/bin/engram",
                        RegisteredAt = "2024-01-01T00:00:00Z"
                    },
                    FlowForge = new FlowForgeComponentEntry
                    {
                        Installed = true,
                        Version = "0.1.0",
                        Ides = ["cursor", "opencode"]
                    }
                }
            };

            store.Save(original);
            var loaded = store.Load();

            Assert.Equal(original.Version, loaded.Version);
            Assert.Equal(original.Channel, loaded.Channel);
            Assert.Equal(original.AutoUpdate, loaded.AutoUpdate);
            Assert.Equal(original.FlowDoc.Enabled, loaded.FlowDoc.Enabled);
            Assert.NotNull(loaded.Sync);
            Assert.Equal(original.Sync.Mode, loaded.Sync!.Mode);
            Assert.Equal(original.Sync.RemoteUrl, loaded.Sync.RemoteUrl);
            Assert.Equal(original.Sync.User, loaded.Sync.User);
            Assert.Equal(original.Sync.DataDir, loaded.Sync.DataDir);
            Assert.NotNull(loaded.Components.EngramDotnet);
            Assert.Equal(original.Components.EngramDotnet!.Version, loaded.Components.EngramDotnet!.Version);
            Assert.NotNull(loaded.Components.FlowForge);
            Assert.Equal(original.Components.FlowForge!.Version, loaded.Components.FlowForge!.Version);
            Assert.Equal(original.Components.FlowForge.Ides, loaded.Components.FlowForge.Ides);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ConfigStore_Load_NonExistentFile_ReturnsDefaults()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"flowforge-baseline-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var configFile = Path.Combine(tempDir, "nonexistent.json");
            var log = new InstallerLogger(Path.Combine(tempDir, "install.log"));
            var store = new ConfigStore(configFile, log);

            var config = store.Load();

            Assert.NotNull(config);
            Assert.Equal("0.1.0", config.Version);
            Assert.Equal("stable", config.Channel);
            Assert.False(config.AutoUpdate);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ConfigStore_Update_AtomicReadWrite_Works()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"flowforge-baseline-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var configFile = Path.Combine(tempDir, "config.json");
            var log = new InstallerLogger(Path.Combine(tempDir, "install.log"));
            var store = new ConfigStore(configFile, log);

            // Initial save
            store.Save(new InstallerConfig { Channel = "stable" });

            // Atomic update
            var updated = store.Update(cfg => cfg.Channel = "nightly");

            Assert.Equal("nightly", updated.Channel);

            // Verify persisted
            var reloaded = store.Load();
            Assert.Equal("nightly", reloaded.Channel);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    // ── PathHelper ──────────────────────────────────────────────────────────

    [Fact]
    public void PathHelper_ReturnsValidPaths_OnCurrentPlatform()
    {
        // Core paths must not be null or empty
        Assert.False(string.IsNullOrEmpty(PathHelper.HomeDir));
        Assert.False(string.IsNullOrEmpty(PathHelper.EngramDir));
        Assert.False(string.IsNullOrEmpty(PathHelper.ConfigFile));
        Assert.False(string.IsNullOrEmpty(PathHelper.LogFile));
        Assert.False(string.IsNullOrEmpty(PathHelper.EngramBinary));
        Assert.False(string.IsNullOrEmpty(PathHelper.InstallerBinary));
        Assert.False(string.IsNullOrEmpty(PathHelper.InstallerBinDir));
        Assert.False(string.IsNullOrEmpty(PathHelper.FlowForgeBackupDir));
        Assert.False(string.IsNullOrEmpty(PathHelper.OpenCodeSidecarPath));
    }

    [Fact]
    public void PathHelper_EngramDir_IsUnderHome()
    {
        Assert.StartsWith(PathHelper.HomeDir, PathHelper.EngramDir);
    }

    [Fact]
    public void PathHelper_ConfigFile_IsUnderEngramDir()
    {
        Assert.StartsWith(PathHelper.EngramDir, PathHelper.ConfigFile);
        Assert.EndsWith("config.json", PathHelper.ConfigFile);
    }

    // ── ManifestClient compatibility checks ─────────────────────────────────

    [Fact]
    public void ManifestClient_CheckEngramCompatibility_CompatibleVersion_ReturnsNull()
    {
        var manifest = RemoteManifest.Default;
        var result = ManifestClient.CheckEngramCompatibility(manifest, "0.5.0");
        Assert.Null(result);
    }

    [Fact]
    public void ManifestClient_CheckEngramCompatibility_IncompatibleVersion_ReturnsError()
    {
        var manifest = new RemoteManifest { RequiresEngramDotnet = ">=1.0.0" };
        var result = ManifestClient.CheckEngramCompatibility(manifest, "0.5.0");
        Assert.NotNull(result);
        Assert.Contains("no es compatible", result);
    }

    [Fact]
    public void ManifestClient_CheckEngramCompatibility_EmptyVersion_ReturnsNull()
    {
        var manifest = RemoteManifest.Default;
        var result = ManifestClient.CheckEngramCompatibility(manifest, "");
        Assert.Null(result);
    }

    // ── GitHubReleasesClient timeout handling ───────────────────────────────

    [Fact]
    public async Task GitHubReleasesClient_GetLatestVersion_HandlesCancellation()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"flowforge-baseline-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var log = new InstallerLogger(Path.Combine(tempDir, "install.log"));
            using var http = new HttpClient(new SlowHandler());
            var client = new GitHubReleasesClient(http, log, downloadTimeoutSeconds: 2);

            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
            // GitHubReleasesClient catches exceptions internally and returns null.
            // The key invariant is: it must NOT hang indefinitely.
            var result = await client.GetLatestVersionAsync("efreet111/FlowForge", "stable", cts.Token);
            // Either returns null (error caught) or throws — both are acceptable.
            // The important thing is it completes within a reasonable time.
            Assert.Null(result);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    // ── InstallerLogger ─────────────────────────────────────────────────────

    [Fact]
    public void InstallerLogger_WritesAllLevels()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"flowforge-baseline-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var logFile = Path.Combine(tempDir, "test.log");
            var logger = new InstallerLogger(logFile);

            logger.Info("info message");
            logger.Warn("warn message");
            logger.Error("error message");

            var content = File.ReadAllText(logFile);
            Assert.Contains("[INFO]", content);
            Assert.Contains("[WARN]", content);
            Assert.Contains("[ERROR]", content);
            Assert.Contains("info message", content);
            Assert.Contains("warn message", content);
            Assert.Contains("error message", content);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    sealed class SlowHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
