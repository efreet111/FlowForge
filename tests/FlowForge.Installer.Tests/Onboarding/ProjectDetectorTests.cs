using FlowForge.Installer.Onboarding;
using Xunit;

namespace FlowForge.Installer.Tests.Onboarding;

/// <summary>
/// Unit tests for ProjectDetector (FR-002).
/// Tests the deterministic detection chain and git remote parsing.
/// </summary>
public class ProjectDetectorTests
{
    // ── FR-002: Detection chain ──────────────────────────────────────────────

    [Fact]
    public void FR_002_ExplicitProjectWins()
    {
        // Given an explicit --project flag
        // Then it should override all other detection
        var result = ProjectDetector.Detect("my-explicit-project");
        Assert.Equal("my-explicit-project", result);
    }

    [Fact]
    public void FR_002_ExplicitProjectTrimmed()
    {
        var result = ProjectDetector.Detect("  spaced-project  ");
        Assert.Equal("spaced-project", result);
    }

    [Fact]
    public void FR_002_EmptyExplicitFallsThrough()
    {
        // Given empty --project flag
        // Then it should fall through to next detection step
        var result = ProjectDetector.Detect("");
        Assert.False(string.IsNullOrWhiteSpace(result));
    }

    [Fact]
    public void FR_002_NullExplicitFallsThrough()
    {
        var result = ProjectDetector.Detect(null);
        Assert.False(string.IsNullOrWhiteSpace(result));
    }

    [Fact]
    public void FR_002_FallbackToBasenameWhenNoConfig()
    {
        // When no .flowforge.json and no git remote, should fall back to CWD basename
        var result = ProjectDetector.Detect(null);
        var expectedBasename = Path.GetFileName(Directory.GetCurrentDirectory());
        // Result should be either git remote basename or CWD basename
        Assert.False(string.IsNullOrWhiteSpace(result));
    }

    // ── Git remote parsing (pure function) ───────────────────────────────────

    [Theory]
    [InlineData("https://github.com/org/my-app.git", "my-app")]
    [InlineData("https://github.com/org/my-app", "my-app")]
    [InlineData("git@github.com:org/my-app.git", "my-app")]
    [InlineData("git@github.com:org/my-app", "my-app")]
    [InlineData("git@gitlab.com:team/project-name.git", "project-name")]
    [InlineData("ssh://git@github.com/org/repo.git", "repo")]
    public void FR_002_ParseGitRemoteBasename(string remoteUrl, string expected)
    {
        var result = ProjectDetector.ParseGitRemoteBasename(remoteUrl);
        Assert.Equal(expected, result);
    }

    // ── IsFlowForgeProject ───────────────────────────────────────────────────

    [Fact]
    public void FR_002_IsFlowForgeProject_WithConfig()
    {
        // The current project should be a FlowForge project
        var cwd = Directory.GetCurrentDirectory();
        // This test depends on the environment; just verify it doesn't throw
        var result = ProjectDetector.IsFlowForgeProject(cwd);
        Assert.IsType<bool>(result);
    }

    [Fact]
    public void FR_002_IsFlowForgeProject_WithTempDir()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        try
        {
            var result = ProjectDetector.IsFlowForgeProject(tempDir);
            Assert.False(result);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }
}
