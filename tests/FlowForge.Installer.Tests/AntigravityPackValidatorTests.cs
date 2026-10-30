using FlowForge.Installer.Infrastructure;
using Xunit;

namespace FlowForge.Installer.Tests;

/// <summary>
/// ADR-019 Amendment (2026-10-29): Antigravity rules require a valid `trigger:`
/// (`alwaysApply` is inert), and `/flow-*` + subagents ship as skills/agents.
/// </summary>
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
    public void FR_001_GoldenPackRulesHaveValidTrigger()
    {
        var repoRoot = FindRepoRoot();
        var rulesDir = Path.Combine(repoRoot, "ide", "antigravity", "rules");

        Assert.Empty(AntigravityPackValidator.ValidateRules(rulesDir));
        Assert.True(AntigravityPackValidator.RuleHasValidTrigger(Path.Combine(rulesDir, "workflow.md")));
        Assert.True(AntigravityPackValidator.RuleHasValidTrigger(Path.Combine(rulesDir, "git-sin-push.md")));
        Assert.True(AntigravityPackValidator.RuleHasValidTrigger(Path.Combine(rulesDir, "pr-workflow.md")));
        Assert.True(AntigravityPackValidator.RuleHasValidTrigger(Path.Combine(rulesDir, "model-assignments.md")));
    }

    [Fact]
    public void FR_001_GoldenPackHasSevenSubagents()
    {
        var repoRoot = FindRepoRoot();
        var agentsDir = Path.Combine(repoRoot, "ide", "antigravity", "agents");
        Assert.True(AntigravityPackValidator.HasSubagents(agentsDir));
        Assert.Equal(7, Directory.GetFiles(agentsDir, "*.md").Length);
    }

    [Fact]
    public void FR_002_LegacyAlwaysApplyIsRejected()
    {
        var tempDir = NewTempDir("ff-rules-test-");
        try
        {
            var rule = Path.Combine(tempDir, "workflow.md");
            File.WriteAllText(rule, "---\nalwaysApply: true\ndescription: legacy rule\n---\n\n# Rule\n");

            Assert.False(AntigravityPackValidator.RuleHasValidTrigger(rule));

            var issues = AntigravityPackValidator.ValidateRules(tempDir);
            var issue = Assert.Single(issues);
            Assert.Contains("trigger:", issue.Rule, StringComparison.Ordinal);
            Assert.Contains("trigger:", issue.Remediation, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Theory]
    [InlineData("always_on")]
    [InlineData("model_decision")]
    [InlineData("glob")]
    [InlineData("manual")]
    public void FR_002_AllowedTriggersAreAccepted(string trigger)
    {
        var tempDir = NewTempDir("ff-rules-test-");
        try
        {
            var rule = Path.Combine(tempDir, "rule.md");
            File.WriteAllText(rule,
                $"---\ntrigger: {trigger}\ndescription: single-line description\n---\n\n# Rule\n");

            Assert.True(AntigravityPackValidator.RuleHasValidTrigger(rule));
            Assert.Empty(AntigravityPackValidator.ValidateRules(tempDir));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void FR_002_UnknownTriggerIsRejected()
    {
        var tempDir = NewTempDir("ff-rules-test-");
        try
        {
            var rule = Path.Combine(tempDir, "rule.md");
            File.WriteAllText(rule, "---\ntrigger: sometimes\ndescription: nope\n---\n");

            Assert.False(AntigravityPackValidator.RuleHasValidTrigger(rule));
            var issue = Assert.Single(AntigravityPackValidator.ValidateRules(tempDir));
            Assert.Contains("sometimes", issue.Rule, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void FR_002_ModelDecisionWithoutDescriptionIsRejected()
    {
        var tempDir = NewTempDir("ff-rules-test-");
        try
        {
            var rule = Path.Combine(tempDir, "model-assignments.md");
            File.WriteAllText(rule, "---\ntrigger: model_decision\n---\n\n# Models\n");

            Assert.False(AntigravityPackValidator.RuleHasValidTrigger(rule));
            var issue = Assert.Single(AntigravityPackValidator.ValidateRules(tempDir));
            Assert.Contains("description", issue.Rule, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void FR_002_RuleWithoutFrontmatterIsRejected()
    {
        var tempDir = NewTempDir("ff-rules-test-");
        try
        {
            var rule = Path.Combine(tempDir, "rule.md");
            File.WriteAllText(rule, "# No frontmatter\n");

            Assert.False(AntigravityPackValidator.RuleHasValidTrigger(rule));
            Assert.Single(AntigravityPackValidator.ValidateRules(tempDir));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void FR_010_WorkflowWithoutFrontmatterFailsValidation()
    {
        var tempDir = NewTempDir("ff-antigravity-test-");
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
        var tempDir = NewTempDir("ff-skills-test-");
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
    public void FR_012_HasCommandSkillsRequiresAllSeven()
    {
        var tempDir = NewTempDir("ff-command-skills-");
        try
        {
            var expected = AntigravityPackValidator.CommandSkillNames;
            Assert.Equal(7, expected.Length);

            for (var i = 0; i < expected.Length; i++)
            {
                Assert.False(AntigravityPackValidator.HasCommandSkills(tempDir));
                var dir = Path.Combine(tempDir, expected[i]);
                Directory.CreateDirectory(dir);
                File.WriteAllText(Path.Combine(dir, "SKILL.md"), $"---\nname: {expected[i]}\n---\n");
            }

            Assert.True(AntigravityPackValidator.HasCommandSkills(tempDir));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void FR_012_HasSubagentsRequiresAllSeven()
    {
        var tempDir = NewTempDir("ff-subagents-");
        try
        {
            var expected = AntigravityPackValidator.SubagentNames;
            Assert.Equal(7, expected.Length);

            for (var i = 0; i < expected.Length; i++)
            {
                Assert.False(AntigravityPackValidator.HasSubagents(tempDir));
                File.WriteAllText(
                    Path.Combine(tempDir, expected[i] + ".md"),
                    $"---\nname: {expected[i]}\ndescription: test\n---\n");
            }

            Assert.True(AntigravityPackValidator.HasSubagents(tempDir));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void FR_016_LegacyWorkflowsDirDetectedWhenObsoletePathHasFlowFiles()
    {
        var tempConfig = NewTempDir("ff-legacy-wf-");
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

    [Fact]
    public void FR_016_LegacyWorkflowsDirDetectedCoversGlobalWorkflows()
    {
        var tempConfig = NewTempDir("ff-legacy-global-wf-");
        var globalDir = Path.Combine(tempConfig, "global_workflows");
        Directory.CreateDirectory(globalDir);
        try
        {
            Assert.False(AntigravityPackValidator.LegacyWorkflowsDirDetected(globalDir));
            File.WriteAllText(Path.Combine(globalDir, "flow-close.md"), "---\ndescription: test\n---\n");
            Assert.True(AntigravityPackValidator.LegacyWorkflowsDirDetected(globalDir));
        }
        finally
        {
            Directory.Delete(tempConfig, recursive: true);
        }
    }

    static string NewTempDir(string prefix)
    {
        var dir = Path.Combine(Path.GetTempPath(), prefix + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
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
