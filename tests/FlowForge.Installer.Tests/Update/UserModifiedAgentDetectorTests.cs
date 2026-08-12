using FlowForge.Installer.Update;
using Xunit;

namespace FlowForge.Installer.Tests.Update;

public class UserModifiedAgentDetectorTests
{
    [Fact]
    public void DetectModifications_UnmodifiedFile_ReturnsNotModified()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"agent-{Guid.NewGuid():N}");
        var installedDir = Path.Combine(tempDir, "installed");
        var sourceDir = Path.Combine(tempDir, "source");
        Directory.CreateDirectory(installedDir);
        Directory.CreateDirectory(sourceDir);
        try
        {
            var content = "agent content here";
            File.WriteAllText(Path.Combine(installedDir, "agent.md"), content);
            File.WriteAllText(Path.Combine(sourceDir, "agent.md"), content);

            var detector = new UserModifiedAgentDetector();
            var reports = detector.DetectModifications(installedDir, sourceDir, "*.md");

            var report = Assert.Single(reports);
            Assert.False(report.IsModified);
            Assert.Equal(report.InstalledSha256, report.SourceSha256);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void DetectModifications_ModifiedFile_ReturnsModified()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"agent-{Guid.NewGuid():N}");
        var installedDir = Path.Combine(tempDir, "installed");
        var sourceDir = Path.Combine(tempDir, "source");
        Directory.CreateDirectory(installedDir);
        Directory.CreateDirectory(sourceDir);
        try
        {
            File.WriteAllText(Path.Combine(installedDir, "agent.md"), "user modified content");
            File.WriteAllText(Path.Combine(sourceDir, "agent.md"), "original source content");

            var detector = new UserModifiedAgentDetector();
            var reports = detector.DetectModifications(installedDir, sourceDir, "*.md");

            var report = Assert.Single(reports);
            Assert.True(report.IsModified);
            Assert.NotEqual(report.InstalledSha256, report.SourceSha256);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void DetectModifications_MissingInstalledFile_ReturnsNotModified()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"agent-{Guid.NewGuid():N}");
        var installedDir = Path.Combine(tempDir, "installed");
        var sourceDir = Path.Combine(tempDir, "source");
        Directory.CreateDirectory(installedDir);
        Directory.CreateDirectory(sourceDir);
        try
        {
            File.WriteAllText(Path.Combine(sourceDir, "new-agent.md"), "new file");

            var detector = new UserModifiedAgentDetector();
            var reports = detector.DetectModifications(installedDir, sourceDir, "*.md");

            var report = Assert.Single(reports);
            Assert.False(report.IsModified); // New file, not modified
            Assert.Empty(report.InstalledSha256);
            Assert.NotEmpty(report.SourceSha256);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void DetectModifications_MissingSourceFile_ReportsIt()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"agent-{Guid.NewGuid():N}");
        var installedDir = Path.Combine(tempDir, "installed");
        var sourceDir = Path.Combine(tempDir, "source");
        Directory.CreateDirectory(installedDir);
        Directory.CreateDirectory(sourceDir);
        try
        {
            File.WriteAllText(Path.Combine(installedDir, "deleted-upstream.md"), "orphan file");

            var detector = new UserModifiedAgentDetector();
            var reports = detector.DetectModifications(installedDir, sourceDir, "*.md");

            var report = Assert.Single(reports);
            Assert.False(report.IsModified);
            Assert.NotEmpty(report.InstalledSha256);
            Assert.Empty(report.SourceSha256);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void DetectModifications_EmptySourceDir_ReturnsEmpty()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"agent-{Guid.NewGuid():N}");
        var installedDir = Path.Combine(tempDir, "installed");
        var sourceDir = Path.Combine(tempDir, "nonexistent");
        Directory.CreateDirectory(installedDir);
        try
        {
            var detector = new UserModifiedAgentDetector();
            var reports = detector.DetectModifications(installedDir, sourceDir, "*.md");
            Assert.Empty(reports);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }
}
