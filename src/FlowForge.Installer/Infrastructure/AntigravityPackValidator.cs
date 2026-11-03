namespace FlowForge.Installer.Infrastructure;

/// <summary>
/// Validates the Antigravity customization contract:
/// rules need a valid <c>trigger:</c>, legacy workflows keep a single-line
/// <c>description:</c>, and the <c>/flow-*</c> commands + subagents are installed
/// as skills/agents (legacy workflows are dead since 2026-10-19 — ADR-019 Amendment).
/// </summary>
public static class AntigravityPackValidator
{
    public record ValidationIssue(string File, string Rule, string Remediation);

    // Legacy Cursor key. Rules carrying only this key are silently discarded by
    // Antigravity, so we detect it to emit a migration hint.
    const string LegacyCursorFrontmatterKey = "alwaysApply";

    public static readonly string[] AllowedTriggers =
        ["always_on", "model_decision", "glob", "manual"];

    public static readonly string[] CommandSkillNames =
        ["flow-start", "flow-plan", "flow-dev", "flow-verify", "flow-close", "flow-status", "flow-rework"];

    public static readonly string[] SubagentNames =
        ["flowforge", "forge-discovery", "forge-arch", "forge-plan", "forge-dev", "forge-verify", "forge-memory"];

    /// <summary>
    /// Validates every <c>flow-*.md</c> under <paramref name="workflowsDir"/>.
    /// These are the legacy workflow sources used to GENERATE skills, so only the
    /// single-line <c>description:</c> is required.
    /// </summary>
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

    /// <summary>
    /// Validates installed rules: frontmatter present, <c>trigger:</c> with an
    /// allowed value, and a non-empty <c>description:</c> for <c>model_decision</c>.
    /// Missing/invalid trigger makes Antigravity discard the rule silently.
    /// </summary>
    public static IReadOnlyList<ValidationIssue> ValidateRules(string rulesDir)
    {
        var issues = new List<ValidationIssue>();
        if (!Directory.Exists(rulesDir))
        {
            issues.Add(new ValidationIssue(
                rulesDir,
                "directorio rules ausente",
                "Ejecutá `flowforge install` para reinstalar las reglas del pack."));
            return issues;
        }

        foreach (var file in Directory.GetFiles(rulesDir, "*.md"))
        {
            var frontmatter = ReadFrontmatter(file);
            var name = Path.GetFileName(file);

            if (frontmatter == null)
            {
                issues.Add(new ValidationIssue(
                    name,
                    "falta frontmatter YAML (--- ... ---)",
                    "Agregá frontmatter con `trigger: always_on` (o el trigger que corresponda)."));
                continue;
            }

            if (!frontmatter.TryGetValue("trigger", out var trigger) || string.IsNullOrWhiteSpace(trigger))
            {
                issues.Add(new ValidationIssue(
                    name,
                    "falta trigger: en frontmatter (la regla se descarta en silencio)",
                    $"Reemplazá la clave inerte de Cursor ({LegacyCursorFrontmatterKey}) por "
                    + $"`trigger:` con un valor válido ({string.Join(", ", AllowedTriggers)}). "
                    + "Ejecutá `flowforge install`."));
                continue;
            }

            if (!AllowedTriggers.Contains(trigger))
            {
                issues.Add(new ValidationIssue(
                    name,
                    $"trigger inválido: '{trigger}'",
                    $"Usá uno de: {string.Join(", ", AllowedTriggers)}. "
                    + "Antigravity descarta reglas con trigger inválido."));
                continue;
            }

            if (trigger.Equals("model_decision", StringComparison.Ordinal)
                && (!frontmatter.TryGetValue("description", out var description)
                    || string.IsNullOrWhiteSpace(description)))
            {
                issues.Add(new ValidationIssue(
                    name,
                    "trigger model_decision requiere description no vacío",
                    "Agregá `description: <una línea>` al frontmatter."));
            }
        }

        return issues;
    }

    /// <summary>True when the rule carries a valid <c>trigger:</c> (ADR-019 Amendment).</summary>
    public static bool RuleHasValidTrigger(string rulePath)
    {
        if (!File.Exists(rulePath))
            return false;

        var frontmatter = ReadFrontmatter(rulePath);
        if (frontmatter == null)
            return false;

        if (!frontmatter.TryGetValue("trigger", out var trigger) || string.IsNullOrWhiteSpace(trigger))
            return false;

        if (!AllowedTriggers.Contains(trigger))
            return false;

        return !trigger.Equals("model_decision", StringComparison.Ordinal)
               || (frontmatter.TryGetValue("description", out var description)
                   && !string.IsNullOrWhiteSpace(description));
    }

    public static bool HasForgeDiscoverySkill(string skillsDir) =>
        File.Exists(Path.Combine(skillsDir, "forge-discovery", "SKILL.md"));

    /// <summary>True when all 7 <c>/flow-*</c> command skills are installed.</summary>
    public static bool HasCommandSkills(string skillsDir) =>
        CommandSkillNames.All(name => File.Exists(Path.Combine(skillsDir, name, "SKILL.md")));

    /// <summary>True when all 7 native Antigravity subagents are installed.</summary>
    public static bool HasSubagents(string agentsDir) =>
        SubagentNames.All(name => File.Exists(Path.Combine(agentsDir, name + ".md")));

    public static bool WorkflowHasValidFrontmatter(string filePath)
    {
        if (!File.Exists(filePath))
            return false;

        var frontmatter = ReadFrontmatter(filePath);
        return frontmatter != null
               && frontmatter.TryGetValue("description", out var description)
               && !string.IsNullOrWhiteSpace(description);
    }

    public static bool LegacyPackDetected()
    {
        var legacyAgents = Path.Combine(PathHelper.AntigravityLegacyDir, "AGENTS.md");
        return File.Exists(legacyAgents);
    }

    /// <summary>
    /// True when a dead workflow dir still contains <c>flow-*.md</c>. With no
    /// argument it checks both <c>config/workflows</c> and <c>config/global_workflows</c>.
    /// </summary>
    public static bool LegacyWorkflowsDirDetected(string? workflowsDir = null)
    {
        if (workflowsDir != null)
            return DirectoryHasFlowFiles(workflowsDir);

        return DirectoryHasFlowFiles(PathHelper.AntigravityLegacyWorkflowsDir)
               || DirectoryHasFlowFiles(PathHelper.AntigravityWorkflows);
    }

    static bool DirectoryHasFlowFiles(string dir) =>
        Directory.Exists(dir) && Directory.GetFiles(dir, "flow-*.md").Length > 0;

    /// <summary>
    /// Minimal single-block YAML frontmatter reader (flat <c>key: value</c> pairs).
    /// Returns null when the file has no closed frontmatter block.
    /// </summary>
    static Dictionary<string, string>? ReadFrontmatter(string filePath)
    {
        string[] lines;
        try
        {
            lines = File.ReadAllLines(filePath);
        }
        catch
        {
            return null;
        }

        if (lines.Length < 2 || lines[0].Trim().TrimStart('\uFEFF') != "---")
            return null;

        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var i = 1; i < lines.Length; i++)
        {
            var trimmed = lines[i].Trim();
            if (trimmed == "---")
                return result;
            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
                continue;

            var separator = trimmed.IndexOf(':');
            if (separator <= 0)
                continue;

            var key = trimmed[..separator].Trim();
            var value = trimmed[(separator + 1)..].Trim().Trim('"', '\'');
            if (value is ">" or "|" || value.StartsWith('>') || value.StartsWith('|'))
                continue; // multi-line blocks are not valid single-line values here

            result.TryAdd(key, value);
        }

        return null; // never closed
    }
}
