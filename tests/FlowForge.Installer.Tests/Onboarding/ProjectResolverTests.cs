using System.IO;
using System.Text.Json;
using FlowForge.Installer.Onboarding;
using Xunit;

namespace FlowForge.Installer.Tests.Onboarding;

public class ProjectResolverTests : IDisposable
{
    readonly string _tempDir;

    public ProjectResolverTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"ff-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { /* best effort */ }
    }

    [Fact] // FR-003
    public void CliOverride_HasHighestPriority()
    {
        var result = ProjectResolver.Resolve("myproject", "team", null, _tempDir);

        Assert.Equal("myproject", result.ProjectName);
        Assert.Equal("team/myproject", result.NamespacedProject);
        Assert.Equal("cli-arg", result.Source);
        Assert.False(result.IsAmbiguous);
    }

    [Fact] // FR-003
    public void FlowForgeJson_Fallback()
    {
        // Create .flowforge.json with engram.project
        var config = new { engram = new { project = "flowforge" } };
        var json = JsonSerializer.Serialize(config);
        File.WriteAllText(Path.Combine(_tempDir, ".flowforge.json"), json);

        var result = ProjectResolver.Resolve(null, "team", null, _tempDir);

        Assert.Equal("flowforge", result.ProjectName);
        Assert.Equal("team/flowforge", result.NamespacedProject);
        Assert.Equal("flowforge.json", result.Source);
    }

    [Fact] // FR-015
    public void TeamScope_AppliesTeamNamespace()
    {
        var result = ProjectResolver.Resolve("flowforge", "team", null, _tempDir);
        Assert.Equal("team/flowforge", result.NamespacedProject);
    }

    [Fact] // FR-015
    public void PersonalScope_AppliesUserNamespace()
    {
        var result = ProjectResolver.Resolve("flowforge", "personal", "victor", _tempDir);
        Assert.Equal("victor/flowforge", result.NamespacedProject);
    }

    [Fact] // FR-015
    public void DefaultScope_AppliesTeamNamespace()
    {
        var result = ProjectResolver.Resolve("flowforge", null, null, _tempDir);
        Assert.Equal("team/flowforge", result.NamespacedProject);
    }

    [Fact] // FR-003
    public void AmbiguousProject_ReturnsAvailableProjects()
    {
        // Create child directories with .git
        var child1 = Path.Combine(_tempDir, "project-a");
        var child2 = Path.Combine(_tempDir, "project-b");
        Directory.CreateDirectory(Path.Combine(child1, ".git"));
        Directory.CreateDirectory(Path.Combine(child2, ".git"));

        var result = ProjectResolver.Resolve(null, "team", null, _tempDir);

        Assert.True(result.IsAmbiguous);
        Assert.NotNull(result.AvailableProjects);
        Assert.Equal(2, result.AvailableProjects!.Count);
        Assert.Contains("project-a", result.AvailableProjects);
        Assert.Contains("project-b", result.AvailableProjects);
    }

    [Fact] // FR-003
    public void SingleChildProject_NotAmbiguous()
    {
        var child = Path.Combine(_tempDir, "single-project");
        Directory.CreateDirectory(Path.Combine(child, ".git"));

        var result = ProjectResolver.Resolve(null, "team", null, _tempDir);

        Assert.False(result.IsAmbiguous);
        Assert.Equal("single-project", result.ProjectName);
    }

    [Fact] // FR-003
    public void NoProjectInfo_FallsBackToDirectoryName()
    {
        var result = ProjectResolver.Resolve(null, "team", null, _tempDir);

        // Falls back to directory name
        Assert.NotNull(result.ProjectName);
        Assert.StartsWith("team/", result.NamespacedProject);
    }
}
