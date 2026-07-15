using FlowForge.Installer.Infrastructure;
using Xunit;

namespace FlowForge.Installer.Tests;

public class AntigravityPackValidatorTests
{
    [Fact]
    public void FR_001_GoldenPackWorkflowsHaveValidFrontmatter()
    {
        var repoRoot = FindRepoRoot();
        var workflowsDir = Path.Combine(repoRoot, "ide", "antigravity", "workflows");
        var issues = AntigravityPackValidator.ValidateWorkflows(workflowsDir);
        Assert.Empty(issues);
        Assert.Equal(7, Directory.GetFiles(workflowsDir, "flow-*.md").Length);
    }

    [Fact]
    public void FR_003_WorkflowRuleHasAlwaysApply()
    {
        var repoRoot = FindRepoRoot();
        var rulePath = Path.Combine(repoRoot, "ide", "antigravity", "rules", "workflow.md");
        Assert.True(AntigravityPackValidator.WorkflowRuleHasAlwaysApply(rulePath));
    }

    [Fact]
    public void FR_010_WorkflowWithoutFrontmatterFailsValidation()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "ff-antigravity-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var badFile = Path.Combine(tempDir, "flow-bad.md");
            File.WriteAllText(badFile, "# /flow-bad\n\nNo frontmatter.\n");
            Assert.False(AntigravityPackValidator.WorkflowHasValidFrontmatter(badFile));

            var issues = AntigravityPackValidator.ValidateWorkflows(tempDir);
            Assert.Single(issues);
            Assert.Contains("description:", issues[0].Rule, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void FR_011_HasForgeDiscoverySkillDetectsPresenceAndAbsence()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "ff-skills-test-" + Guid.NewGuid().ToString("N"));
        var skillDir = Path.Combine(tempDir, "forge-discovery");
        Directory.CreateDirectory(skillDir);
        try
        {
            Assert.False(AntigravityPackValidator.HasForgeDiscoverySkill(tempDir));
            File.WriteAllText(Path.Combine(skillDir, "SKILL.md"), "# forge-discovery\n");
            Assert.True(AntigravityPackValidator.HasForgeDiscoverySkill(tempDir));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void FR_016_LegacyWorkflowsDirDetectedWhenObsoletePathHasFlowFiles()
    {
        var tempConfig = Path.Combine(Path.GetTempPath(), "ff-legacy-wf-" + Guid.NewGuid().ToString("N"));
        var legacyDir = Path.Combine(tempConfig, "workflows");
        Directory.CreateDirectory(legacyDir);
        try
        {
            Assert.False(AntigravityPackValidator.LegacyWorkflowsDirDetected(legacyDir));
            File.WriteAllText(Path.Combine(legacyDir, "flow-start.md"), "---\ndescription: test\n---\n");
            Assert.True(AntigravityPackValidator.LegacyWorkflowsDirDetected(legacyDir));
        }
        finally
        {
            Directory.Delete(tempConfig, recursive: true);
        }
    }

    static string FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            if (File.Exists(Path.Combine(dir, "AGENTS.md"))
                && Directory.Exists(Path.Combine(dir, "ide", "antigravity", "workflows")))
            {
                return dir;
            }

            dir = Directory.GetParent(dir)?.FullName ?? string.Empty;
        }

        throw new InvalidOperationException("Could not locate FlowForge repo root for golden pack tests.");
    }
}
