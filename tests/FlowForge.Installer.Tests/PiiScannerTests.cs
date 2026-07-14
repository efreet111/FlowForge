using FlowForge.Installer.Modules.OpenCode;
using Xunit;

namespace FlowForge.Installer.Tests;

public class PiiScannerTests
{
    [Fact]
    public void Recognizes_env_var_names_as_safe()
    {
        var scanner = new PiiScanner();
        var json = "\"env\": [\"OPENCODIGO_API_KEY\"]";
        var (clean, hits) = scanner.Scan(json);
        Assert.True(clean);
        Assert.Empty(hits);
    }

    [Fact]
    public void Detects_key_value_pairs_as_pii()
    {
        var scanner = new PiiScanner();
        var line = "OPENCODIGO_API_KEY=sk-abcdefghijklmnopqrst";
        var (clean, hits) = scanner.Scan(line);
        Assert.False(clean);
        Assert.NotEmpty(hits);
    }
}
