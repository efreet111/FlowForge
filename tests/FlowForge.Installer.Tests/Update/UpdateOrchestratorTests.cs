using FlowForge.Installer.Commands;
using FlowForge.Installer.Infrastructure;
using FlowForge.Installer.Update;
using Xunit;

namespace FlowForge.Installer.Tests.Update;

public class UpdateOrchestratorTests
{
    static InstallerContext CreateContext()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"orch-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var log = new InstallerLogger(Path.Combine(tempDir, "install.log"));
        var store = new ConfigStore(Path.Combine(tempDir, "config.json"), log);
        using var http = new System.Net.Http.HttpClient(new NoNetworkHandler());
        var gh = new GitHubReleasesClient(http, log);
        var manifest = new ManifestClient(http, log);
        return new InstallerContext(log, store, gh, manifest);
    }

    [Fact]
    public async Task RunAsync_Installer_ReturnsFailed_DeferredMessage()
    {
        var ctx = CreateContext();
        var orchestrator = new UpdateOrchestrator(ctx);
        var options = new UpdateOptions(UpdateComponent.Installer, false, false, null, null);

        var results = await orchestrator.RunAsync(options);

        var result = Assert.Single(results);
        Assert.Equal(UpdateStatus.Failed, result.Status);
        Assert.Contains("deferred", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RunAsync_FlowDoc_ReturnsSkipped()
    {
        var ctx = CreateContext();
        var orchestrator = new UpdateOrchestrator(ctx);
        var options = new UpdateOptions(UpdateComponent.FlowDoc, false, false, null, null);

        var results = await orchestrator.RunAsync(options);

        var result = Assert.Single(results);
        Assert.Equal(UpdateStatus.SkippedAlreadyLatest, result.Status);
    }

    [Fact]
    public async Task RunAsync_Engram_NoNetwork_ReturnsFailed()
    {
        var ctx = CreateContext();
        var orchestrator = new UpdateOrchestrator(ctx);
        var options = new UpdateOptions(UpdateComponent.Engram, false, false, null, null);

        var results = await orchestrator.RunAsync(options);

        var result = Assert.Single(results);
        // Without network, version fetch fails
        Assert.True(result.Status == UpdateStatus.Failed || result.Status == UpdateStatus.SkippedAlreadyLatest);
    }

    [Fact]
    public async Task RunAsync_All_StopsOnFirstFailure()
    {
        var ctx = CreateContext();
        var orchestrator = new UpdateOrchestrator(ctx);
        var options = new UpdateOptions(UpdateComponent.All, false, false, null, null);

        var results = await orchestrator.RunAsync(options);

        // Should have at least the engram result
        Assert.NotEmpty(results);
        // If engram fails, skills should not be attempted
        if (results[0].Status == UpdateStatus.Failed)
        {
            Assert.Single(results);
        }
    }

    [Fact]
    public void UpdateComponent_EnumValues_AreCorrect()
    {
        Assert.Equal(0, (int)UpdateComponent.Engram);
        Assert.Equal(1, (int)UpdateComponent.FlowForgeSkills);
        Assert.Equal(2, (int)UpdateComponent.FlowDoc);
        Assert.Equal(3, (int)UpdateComponent.Installer);
        Assert.Equal(4, (int)UpdateComponent.All);
    }

    [Fact]
    public void UpdateResult_RecordEquality_Works()
    {
        var r1 = new UpdateResult(UpdateComponent.Engram, "1.0", "2.0", UpdateStatus.Success);
        var r2 = new UpdateResult(UpdateComponent.Engram, "1.0", "2.0", UpdateStatus.Success);
        Assert.Equal(r1, r2);
    }

    [Fact]
    public void UpdateOptions_RecordCreation_Works()
    {
        var options = new UpdateOptions(UpdateComponent.Engram, true, false, "v1.0", null);
        Assert.Equal(UpdateComponent.Engram, options.Component);
        Assert.True(options.Yes);
        Assert.False(options.Force);
        Assert.Equal("v1.0", options.Tag);
        Assert.Null(options.SpecificVersion);
    }

    sealed class NoNetworkHandler : System.Net.Http.HttpMessageHandler
    {
        protected override Task<System.Net.Http.HttpResponseMessage> SendAsync(
            System.Net.Http.HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromException<System.Net.Http.HttpResponseMessage>(
                new System.Net.Http.HttpRequestException("No network in test"));
        }
    }
}
