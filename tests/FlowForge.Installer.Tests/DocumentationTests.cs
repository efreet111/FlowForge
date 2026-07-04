using System.IO;
using Xunit;

namespace FlowForge.Installer.Tests;

public class DocumentationTests
{
    [Fact]
    public void FR_008_ReadmeIncludesTroubleshootingHints()
    {
        var content = File.ReadAllText(Path.Combine("..", "..", "README.md"));
        Assert.Contains("## Troubleshooting", content);
        Assert.Contains("SQLite Error 14", content);
        Assert.Contains("flowforge doctor", content);
        Assert.Contains("FLOWFORGE_API_TIMEOUT_SECONDS", content);
    }

    [Fact]
    public void FR_008_ReadmeEsIncludesTroubleshootingHints()
    {
        var content = File.ReadAllText(Path.Combine("..", "..", "README.es.md"));
        Assert.Contains("## Solución de problemas", content);
        Assert.Contains("SQLite Error 14", content);
        Assert.Contains("flowforge doctor", content);
        Assert.Contains("FLOWFORGE_YES", content);
    }
}
