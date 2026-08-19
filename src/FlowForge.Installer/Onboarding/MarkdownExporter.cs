using System.Text;

namespace FlowForge.Installer.Onboarding;

/// <summary>
/// Writes ONBOARDING.md atomically (temp file + rename).
/// Filters to team scope only (NFR-005).
/// Warns if --scope personal combined with --output.
/// </summary>
public sealed class MarkdownExporter
{
    /// <summary>
    /// Exports the briefing data to a markdown file.
    /// </summary>
    /// <param name="data">Aggregated briefing data.</param>
    /// <param name="outputPath">Target file path.</param>
    /// <param name="displayUser">Display-only user name (for header).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if export succeeded, false if warned and skipped.</returns>
    public Task<bool> ExportAsync(BriefingData data, string outputPath, string? displayUser, CancellationToken ct = default)
    {
        // Warn if personal scope combined with output
        if (string.Equals(data.Scope, "personal", StringComparison.OrdinalIgnoreCase))
        {
            // Personal scope should not be exported (NFR-005)
            return Task.FromResult(false);
        }

        var sb = new StringBuilder();

        // Header with generated timestamp
        sb.AppendLine($"> Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        if (!string.IsNullOrEmpty(displayUser))
            sb.AppendLine($"> For: {displayUser}");
        sb.AppendLine($"> Project: {data.Project}");
        if (!string.IsNullOrEmpty(data.Scope))
            sb.AppendLine($"> Scope: {data.Scope}");
        sb.AppendLine();

        // Stats
        if (data.Stats is not null)
        {
            sb.AppendLine("## Memory Stats");
            sb.AppendLine();
            sb.AppendLine($"- Sessions: {data.Stats.TotalSessions}");
            sb.AppendLine($"- Observations: {data.Stats.TotalObservations}");
            sb.AppendLine($"- Prompts: {data.Stats.TotalPrompts}");
            if (data.Stats.Projects.Count > 0)
                sb.AppendLine($"- Projects: {string.Join(", ", data.Stats.Projects)}");
            sb.AppendLine();
        }

        // Recent Activity
        if (!string.IsNullOrWhiteSpace(data.RecentActivity))
        {
            sb.AppendLine("## Recent Activity");
            sb.AppendLine();
            sb.AppendLine(data.RecentActivity);
            sb.AppendLine();
        }

        // Key Decisions
        if (data.Decisions.Count > 0)
        {
            sb.AppendLine("## Key Architectural Decisions");
            sb.AppendLine();
            foreach (var d in data.Decisions)
            {
                sb.AppendLine($"- **#{d.Id}** [{d.Type}] {d.Title}");
                if (!string.IsNullOrWhiteSpace(d.Preview))
                    sb.AppendLine($"  {d.Preview}");
            }
            sb.AppendLine();
        }

        // Conventions / Patterns
        if (data.Patterns.Count > 0)
        {
            sb.AppendLine("## Conventions & Patterns");
            sb.AppendLine();
            foreach (var p in data.Patterns)
            {
                sb.AppendLine($"- **#{p.Id}** [{p.Type}] {p.Title}");
                if (!string.IsNullOrWhiteSpace(p.Preview))
                    sb.AppendLine($"  {p.Preview}");
            }
            sb.AppendLine();
        }

        // Blockers / Gotchas
        if (data.Blockers.Count > 0)
        {
            sb.AppendLine("## Known Blockers / Gotchas");
            sb.AppendLine();
            foreach (var b in data.Blockers)
            {
                sb.AppendLine($"- **#{b.Id}** [{b.Type}] {b.Title}");
                if (!string.IsNullOrWhiteSpace(b.Preview))
                    sb.AppendLine($"  {b.Preview}");
            }
            sb.AppendLine();
        }

        // Next Steps
        sb.AppendLine("## Next Steps");
        sb.AppendLine();
        sb.AppendLine("- Review the decisions above to understand the project's architecture.");
        sb.AppendLine("- Follow the conventions/patterns when writing new code.");
        sb.AppendLine("- Check the blockers section for known issues to avoid.");
        sb.AppendLine();

        // Atomic write (temp file + rename)
        var content = sb.ToString();
        var dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var tmpFile = outputPath + ".tmp";
        try
        {
            File.WriteAllText(tmpFile, content, Encoding.UTF8);
            File.Move(tmpFile, outputPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(tmpFile))
                File.Delete(tmpFile);
        }

        return Task.FromResult(true);
    }
}
