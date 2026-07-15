namespace FlowForge.Installer.Infrastructure;

/// <summary>
/// Validates Antigravity workflow frontmatter and forge-discovery skill presence.
/// </summary>
public static class AntigravityPackValidator
{
    public record ValidationIssue(string File, string Rule, string Remediation);

    /// <summary>Validates all flow-*.md under workflowsDir.</summary>
    public static IReadOnlyList<ValidationIssue> ValidateWorkflows(string workflowsDir)
    {
        var issues = new List<ValidationIssue>();
        if (!Directory.Exists(workflowsDir))
        {
            issues.Add(new ValidationIssue(
                workflowsDir,
                "directorio workflows ausente",
                "Ejecutá `flowforge install` o `ide/install.ps1`."));
            return issues;
        }

        foreach (var file in Directory.GetFiles(workflowsDir, "flow-*.md"))
        {
            if (WorkflowHasValidFrontmatter(file))
                continue;

            issues.Add(new ValidationIssue(
                Path.GetFileName(file),
                "falta description: en frontmatter",
                "Ejecutá `flowforge install` o actualizá FlowForge."));
        }

        return issues;
    }

    public static bool HasForgeDiscoverySkill(string skillsDir) =>
        File.Exists(Path.Combine(skillsDir, "forge-discovery", "SKILL.md"));

    public static bool WorkflowHasValidFrontmatter(string filePath)
    {
        if (!File.Exists(filePath))
            return false;

        var lines = File.ReadAllLines(filePath);
        if (lines.Length < 3 || !lines[0].Trim().Equals("---", StringComparison.Ordinal))
            return false;

        var hasDescription = false;
        var closed = false;
        for (var i = 1; i < lines.Length; i++)
        {
            var trimmed = lines[i].Trim();
            if (trimmed.Equals("---", StringComparison.Ordinal))
            {
                closed = true;
                break;
            }

            if (!trimmed.StartsWith("description:", StringComparison.Ordinal))
                continue;

            var value = lines[i].Split(':', 2);
            if (value.Length < 2)
                continue;

            var desc = value[1].Trim();
            if (!string.IsNullOrEmpty(desc) && !desc.Equals(">", StringComparison.Ordinal))
                hasDescription = true;
        }

        return closed && hasDescription;
    }

    public static bool WorkflowRuleHasAlwaysApply(string rulePath)
    {
        if (!File.Exists(rulePath))
            return false;

        var text = File.ReadAllText(rulePath);
        return text.StartsWith("---", StringComparison.Ordinal)
               && text.Contains("alwaysApply: true", StringComparison.Ordinal);
    }

    public static bool LegacyPackDetected()
    {
        var legacyAgents = Path.Combine(PathHelper.AntigravityLegacyDir, "AGENTS.md");
        return File.Exists(legacyAgents);
    }
}
