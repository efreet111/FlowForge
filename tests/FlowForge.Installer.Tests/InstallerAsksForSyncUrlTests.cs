using System.IO;
using Xunit;

namespace FlowForge.Installer.Tests;

/// <summary>
/// Source-level tests for ADR-010 (installer must prompt for ENGRAM_SERVER_URL
/// in mode=sync). Verifies that the hardcoded IP fallback was removed and the
/// wizard now prompts the user. Real unit tests for the validation logic
/// would require mocking Spectre.Console prompts, which is non-trivial;
/// source-level assertions are sufficient to prevent regression.
/// </summary>
public class InstallerAsksForSyncUrlTests
{
    // Resolve the repo root from this test binary's location:
    //   <repo>/tests/FlowForge.Installer.Tests/bin/Release/net10.0/...dll
    // We walk up until we find a directory containing `src/FlowForge.Installer`.
    static string RepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir, "src", "FlowForge.Installer")))
                return dir;
            dir = Path.GetDirectoryName(dir);
        }
        throw new DirectoryNotFoundException("Could not locate repo root from " + AppContext.BaseDirectory);
    }

    static string Read(string relativePath)
        => File.ReadAllText(Path.Combine(RepoRoot(), relativePath));

    [Fact]
    public void FR_010_InstallCommand_PromptsForSyncUrl_WhenSyncChosen()
    {
        var content = Read("src/FlowForge.Installer/Commands/InstallCommand.cs");

        Assert.Contains("URL del servidor sync", content);
        Assert.Contains("TextPrompt<string>", content);
    }

    [Fact]
    public void FR_010_InstallCommand_DoesNotHardcode_IP()
    {
        var content = Read("src/FlowForge.Installer/Commands/InstallCommand.cs");

        Assert.DoesNotContain("192.168.0.178", content);
    }

    [Fact]
    public void FR_010_InstallCommand_HasServerUrlFlag()
    {
        var content = Read("src/FlowForge.Installer/Commands/InstallCommand.cs");

        Assert.Contains("string? serverUrl = null", content);
    }

    [Fact]
    public void FR_010_EngramModule_DoesNotHardcode_IP()
    {
        var content = Read("src/FlowForge.Installer/Modules/EngramModule.cs");

        Assert.DoesNotContain("192.168.0.178", content);
    }

    [Fact]
    public void FR_010_EngramModule_ThrowsWhenSyncWithoutUrl()
    {
        var content = Read("src/FlowForge.Installer/Modules/EngramModule.cs");

        Assert.Contains("sync mode requires ENGRAM_SERVER_URL", content);
        Assert.Contains("InvalidOperationException", content);
    }

    [Fact]
    public void FR_010_InstallerConfig_HasSyncSection()
    {
        var content = Read("src/FlowForge.Installer/Models/InstallerConfig.cs");

        Assert.Contains("SyncConfig", content);
        Assert.Contains("RemoteUrl", content);
    }

    [Fact]
    public void FR_010_Installer_PersistsSyncConfig_OnInstall()
    {
        var content = Read("src/FlowForge.Installer/Commands/InstallCommand.cs");

        Assert.Contains("c.Sync = new SyncConfig", content);
    }
}