using System.IO;
using Xunit;

namespace FlowForge.Installer.Tests;

public class InstallCommandSourceTests
{
    [Fact]
    public void FR_004_BannerAndConnectingPrintedBeforeManifest()
    {
        var content = File.ReadAllText(Path.Combine("..", "..", "src", "FlowForge.Installer", "Commands", "InstallCommand.cs"));
        var bannerIndex = content.IndexOf("FlowForge Stack Installer");
        var connectIndex = content.IndexOf("Conectando a GitHub");
        var manifestIndex = content.IndexOf("ctx.Manifest.FetchAsync");

        Assert.True(bannerIndex >= 0, "Banner text is present");
        Assert.True(connectIndex >= 0, "Connecting hint is present");
        Assert.True(manifestIndex >= 0, "Manifest fetch call exists");
        Assert.True(connectIndex < manifestIndex, "Connecting message occurs before manifest fetch");
    }
}
