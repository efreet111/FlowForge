using FlowForge.Installer.Infrastructure;
using FlowForge.Installer.Models;
using Xunit;

namespace FlowForge.Installer.Tests.Infrastructure;

/// <summary>
/// ADR-002 / ADR-016: when the remote manifest raises <c>requires.installer</c>, the
/// block message must tell the user a command that actually exists. v1 has no
/// <c>update --self</c> (ADR-016 OQ-1), so the message points to the bootstrap.
/// </summary>
public class ManifestClientTests
{
    [Fact]
    public void CheckInstallerCompatibility_OldInstaller_SuggestsExistingBootstrapCommand()
    {
        var manifest = new RemoteManifest
        {
            IsRemote = true,
            RequiresInstaller = ">=0.1.0-alpha.15",
        };

        var error = ManifestClient.CheckInstallerCompatibility(manifest, "0.1.0-alpha.13");

        Assert.NotNull(error);
        Assert.Contains("install/install.sh", error, StringComparison.Ordinal);
        Assert.DoesNotContain("--self", error, StringComparison.Ordinal);
    }

    [Fact]
    public void CheckInstallerCompatibility_SameOrNewerInstaller_ReturnsNull()
    {
        var manifest = new RemoteManifest
        {
            IsRemote = true,
            RequiresInstaller = ">=0.1.0-alpha.15",
        };

        Assert.Null(ManifestClient.CheckInstallerCompatibility(manifest, "0.1.0-alpha.15"));
        Assert.Null(ManifestClient.CheckInstallerCompatibility(manifest, "0.1.0-alpha.16"));
        Assert.Null(ManifestClient.CheckInstallerCompatibility(manifest, "0.1.0-beta.1"));
        Assert.Null(ManifestClient.CheckInstallerCompatibility(manifest, "0.1.0"));
    }

    [Fact]
    public void CheckInstallerCompatibility_ComparesPrereleaseNumerically()
    {
        // alpha.9 < alpha.10 (comparación numérica, no lexicográfica).
        var manifest = new RemoteManifest
        {
            IsRemote = true,
            RequiresInstaller = ">=0.1.0-alpha.10",
        };

        Assert.NotNull(ManifestClient.CheckInstallerCompatibility(manifest, "0.1.0-alpha.9"));
        Assert.Null(ManifestClient.CheckInstallerCompatibility(manifest, "0.1.0-alpha.10"));
    }

    [Fact]
    public void CheckInstallerCompatibility_PrereleaseIsLowerThanRelease()
    {
        var manifest = new RemoteManifest { IsRemote = true, RequiresInstaller = ">=0.2.0" };
        Assert.NotNull(ManifestClient.CheckInstallerCompatibility(manifest, "0.2.0-rc.1"));
    }

    [Fact]
    public void CheckEngramCompatibility_OldPrerelease_IsBlocked()
    {
        var manifest = new RemoteManifest { IsRemote = true, RequiresEngramDotnet = ">=0.3.0" };

        Assert.NotNull(ManifestClient.CheckEngramCompatibility(manifest, "0.3.0-beta.2"));
        Assert.Null(ManifestClient.CheckEngramCompatibility(manifest, "0.3.0"));
        Assert.Null(ManifestClient.CheckEngramCompatibility(manifest, "0.4.1"));
    }

    [Fact]
    public void CheckInstallerCompatibility_LocalManifest_SkipsCheck()
    {
        var manifest = RemoteManifest.Default; // IsRemote = false

        Assert.Null(ManifestClient.CheckInstallerCompatibility(manifest, "0.1.0-alpha.1"));
    }
}
