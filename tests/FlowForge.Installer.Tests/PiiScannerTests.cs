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

    [Fact]
    public void Detects_home_path_inside_json_strings()
    {
        var scanner = new PiiScanner();
        var json = "{\"pii_test\": \"/home/victor/secret\"}";
        var (clean, hits) = scanner.Scan(json);
        Assert.False(clean);
        Assert.Contains(hits, hit => hit.Value.Contains("/home/victor/", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("/home/runner/work/project")]
    [InlineData("/home/testuser/docs")]
    [InlineData("/home/user/tmp")]
    [InlineData("/home/username/data")]
    [InlineData("/home/example/config")]
    public void Ignores_placeholder_usernames_in_json(string path)
    {
        var scanner = new PiiScanner();
        var json = $"{{\"path\":\"{path}\"}}";
        var (clean, hits) = scanner.Scan(json);
        Assert.True(clean);
        Assert.Empty(hits);
    }
}
