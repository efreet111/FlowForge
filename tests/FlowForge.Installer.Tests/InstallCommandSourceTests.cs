using System.IO;
using Xunit;

namespace FlowForge.Installer.Tests;

public class InstallCommandSourceTests
{
    [Fact]
    public void FR_004_BannerAndConnectingPrintedBeforeManifest()
    {
        var content = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "src", "FlowForge.Installer", "Commands", "InstallCommand.cs"));
        var bannerIndex = content.IndexOf("FlowForge Stack Installer");
        var connectIndex = content.IndexOf("Conectando a GitHub");
        var manifestIndex = content.IndexOf("ctx.Manifest.FetchAsync");

        Assert.True(bannerIndex >= 0, "Banner text is present");
        Assert.True(connectIndex >= 0, "Connecting hint is present");
        Assert.True(manifestIndex >= 0, "Manifest fetch call exists");
        Assert.True(connectIndex < manifestIndex, "Connecting message occurs before manifest fetch");
    }

    [Fact]
    public void HU042_Rework_InstallContext7McpAsync_AcceptsResolvedParameters()
    {
        // ISSUE-1 fix: InstallContext7McpAsync must accept engramMode and engramSyncUrl
        // as parameters (source of truth from wizard) instead of re-deriving from env/config
        var content = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "src", "FlowForge.Installer", "Commands", "InstallCommand.cs"));
        
        // Verify method signature accepts parameters
        Assert.Contains("async Task InstallContext7McpAsync(string engramMode, string? engramSyncUrl)", content);
        
        // Verify the method uses the parameters as primary source of truth
        Assert.Contains("var isNonLocal = engramMode != \"local\" || !string.IsNullOrWhiteSpace(engramSyncUrl);", content);
        
        // Verify the call site passes the resolved values
        Assert.Contains("await InstallContext7McpAsync(engramMode, engramSyncUrl);", content);
    }

    [Fact]
    public void HU042_Rework_DetectSyncMode_ChecksEngramProfile()
    {
        // ISSUE-2 fix: DetectSyncMode must check ENGRAM_PROFILE env var
        var content = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "src", "FlowForge.Installer", "Commands", "InstallCommand.cs"));
        
        // Verify DetectSyncMode checks ENGRAM_PROFILE
        var detectMethodStart = content.IndexOf("string DetectSyncMode()");
        Assert.True(detectMethodStart >= 0, "DetectSyncMode method exists");
        
        var detectMethodEnd = content.IndexOf("    }", detectMethodStart + 100);
        var detectMethodBody = content.Substring(detectMethodStart, detectMethodEnd - detectMethodStart);
        
        Assert.Contains("ENGRAM_PROFILE", detectMethodBody);
        Assert.Contains("fromProfile", detectMethodBody);
    }

    [Fact]
    public void HU042_Rework_RunCommandAsync_HasTimeoutAndParallelDrain()
    {
        // Minor fixes: timeout (T-01) and parallel stdout/stderr drain
        var content = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "src", "FlowForge.Installer", "Commands", "InstallCommand.cs"));
        
        // Verify timeout is set
        Assert.Contains("CancellationTokenSource(TimeSpan.FromSeconds(10))", content);
        
        // Verify parallel drain (stdout and stderr read concurrently)
        Assert.Contains("var stdoutTask = process.StandardOutput.ReadToEndAsync();", content);
        Assert.Contains("var stderrTask = process.StandardError.ReadToEndAsync();", content);
        
        // Verify timeout handling
        Assert.Contains("OperationCanceledException", content);
        Assert.Contains("process.Kill", content);
    }

    [Fact]
    public void HU042_Rework_HeadlessSyncMode_NoHardErrorOnMissingUrl()
    {
        // ISSUE-2 related: headless mode should warn (not error) when sync URL is missing,
        // allowing context7 install to proceed
        var content = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "src", "FlowForge.Installer", "Commands", "InstallCommand.cs"));
        
        // Verify the warning message exists (instead of hard error)
        Assert.Contains("Sync mode sin URL configurada", content);
        
        // Verify no Environment.Exit(1) in the sync URL resolution block
        // (the old code had: Environment.Exit(1); return;)
        var headlessBlock = content.IndexOf("if (isHeadless)");
        Assert.True(headlessBlock >= 0, "Headless block exists");
        
        var urlResolutionBlock = content.IndexOf("if (engramMode == \"sync\")", headlessBlock);
        Assert.True(urlResolutionBlock >= 0, "URL resolution block exists");
        
        // Check that Environment.Exit(1) is NOT in the URL resolution section
        var urlResolutionEnd = content.IndexOf("        }\n        else", urlResolutionBlock);
        var urlResolutionBody = content.Substring(urlResolutionBlock, urlResolutionEnd - urlResolutionBlock);
        Assert.DoesNotContain("Environment.Exit(1)", urlResolutionBody);
    }

    [Fact]
    public void HU042_Rework_CtxLog_DecisionLogging()
    {
        // Minor fix: decisions logged to ctx.Log (NFR-004), not just AnsiConsole
        var content = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "src", "FlowForge.Installer", "Commands", "InstallCommand.cs"));
        
        // Verify ctx.Log calls in InstallContext7McpAsync
        var methodStart = content.IndexOf("async Task InstallContext7McpAsync(string engramMode, string? engramSyncUrl)");
        Assert.True(methodStart >= 0, "InstallContext7McpAsync method exists");
        
        var methodEnd = content.IndexOf("    }\n\n    ///", methodStart + 100);
        var methodBody = content.Substring(methodStart, methodEnd - methodStart);
        
        Assert.Contains("ctx.Log.Info", methodBody);
        Assert.Contains("ctx.Log.Warn", methodBody);
    }
}
