using System.IO;
using Xunit;

namespace FlowForge.Installer.Tests;

public class ScriptTests
{
    const string ScriptPath = "install/install.sh";

    string LoadScript() => File.ReadAllText(Path.Combine("..", "..", ScriptPath));

    [Fact]
    public void FR_002_CurlUsesProgressBar()
    {
        var content = LoadScript();
        Assert.Contains("curl -fSL --progress-bar", content);
    }

    [Fact]
    public void FR_002_WgetSpinnerMessage()
    {
        var content = LoadScript();
        Assert.Contains("start_wget_spinner", content);
        Assert.Contains("Descargando flowforge", content);
    }

    [Fact]
    public void FR_003_HeadlessDetectionHonorsFlowforgeYes()
    {
        var content = LoadScript();
        Assert.Contains("if [ -n \"${FLOWFORGE_YES:-}\" ] || ! [ -t 0 ]", content);
    }

    [Fact]
    public void FR_003_DiagnoseFlagSupported()
    {
        var content = LoadScript();
        Assert.Contains("--diagnose", content);
        Assert.Contains("run_diagnose", content);
    }
}
