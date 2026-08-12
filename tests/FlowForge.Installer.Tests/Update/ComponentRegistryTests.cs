using FlowForge.Installer.Infrastructure;
using FlowForge.Installer.Update;
using Xunit;

namespace FlowForge.Installer.Tests.Update;

public class ComponentRegistryTests
{
    static (ComponentRegistry registry, string tempDir) CreateRegistry()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"flowforge-reg-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var configFile = Path.Combine(tempDir, "config.json");
        var log = new InstallerLogger(Path.Combine(tempDir, "install.log"));
        var store = new ConfigStore(configFile, log);
        var registry = new ComponentRegistry(store);
        return (registry, tempDir);
    }

    [Fact]
    public void GetVersion_NotInstalled_ReturnsNull()
    {
        var (registry, tempDir) = CreateRegistry();
        try
        {
            Assert.Null(registry.GetVersion(UpdateComponent.Engram));
            Assert.Null(registry.GetVersion(UpdateComponent.FlowForgeSkills));
            Assert.Null(registry.GetVersion(UpdateComponent.FlowDoc));
            Assert.Null(registry.GetVersion(UpdateComponent.Installer));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void SetVersion_ThenGetVersion_RoundTrip()
    {
        var (registry, tempDir) = CreateRegistry();
        try
        {
            registry.SetVersion(UpdateComponent.Engram, "1.0.0");
            Assert.Equal("1.0.0", registry.GetVersion(UpdateComponent.Engram));

            registry.SetVersion(UpdateComponent.FlowForgeSkills, "0.2.0");
            Assert.Equal("0.2.0", registry.GetVersion(UpdateComponent.FlowForgeSkills));

            registry.SetVersion(UpdateComponent.FlowDoc, "2.0");
            Assert.Equal("2.0", registry.GetVersion(UpdateComponent.FlowDoc));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void IsAtVersion_Match_ReturnsTrue()
    {
        var (registry, tempDir) = CreateRegistry();
        try
        {
            registry.SetVersion(UpdateComponent.Engram, "1.0.0");
            Assert.True(registry.IsAtVersion(UpdateComponent.Engram, "1.0.0"));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void IsAtVersion_Mismatch_ReturnsFalse()
    {
        var (registry, tempDir) = CreateRegistry();
        try
        {
            registry.SetVersion(UpdateComponent.Engram, "1.0.0");
            Assert.False(registry.IsAtVersion(UpdateComponent.Engram, "2.0.0"));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void IsAtVersion_NotInstalled_ReturnsFalse()
    {
        var (registry, tempDir) = CreateRegistry();
        try
        {
            Assert.False(registry.IsAtVersion(UpdateComponent.Engram, "1.0.0"));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void GetAllVersions_ReturnsAllComponents()
    {
        var (registry, tempDir) = CreateRegistry();
        try
        {
            registry.SetVersion(UpdateComponent.Engram, "1.0.0");
            registry.SetVersion(UpdateComponent.FlowForgeSkills, "0.2.0");

            var versions = registry.GetAllVersions();

            Assert.Equal(4, versions.Count);
            Assert.Equal("1.0.0", versions["engram"]);
            Assert.Equal("0.2.0", versions["flowforge-skills"]);
            Assert.Null(versions["flowdoc"]);
            Assert.Null(versions["installer"]);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void SetVersion_Overwrite_UpdatesCorrectly()
    {
        var (registry, tempDir) = CreateRegistry();
        try
        {
            registry.SetVersion(UpdateComponent.Engram, "1.0.0");
            registry.SetVersion(UpdateComponent.Engram, "2.0.0");
            Assert.Equal("2.0.0", registry.GetVersion(UpdateComponent.Engram));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }
}
