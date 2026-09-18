using FlowForge.Installer.Onboarding;
using Xunit;

namespace FlowForge.Installer.Tests.Onboarding;

/// <summary>
/// Unit tests for MarkdownExporter (FR-006).
/// Tests the actual ExportAsync API with BriefingData.
/// </summary>
public class MarkdownExporterTests
{
    [Fact]
    public async Task FR_006_ExportAsync_ContainsAllSections()
    {
        // Given complete briefing data
        var data = CreateSampleBriefingData();
        var exporter = new MarkdownExporter();
        var tempFile = Path.Combine(Path.GetTempPath(), $"onboarding-test-{Guid.NewGuid()}.md");

        try
        {
            // When exporting
            var result = await exporter.ExportAsync(data, tempFile, "test-user");

            // Then export succeeded and all sections are present
            Assert.True(result);
            Assert.True(File.Exists(tempFile));

            var content = File.ReadAllText(tempFile);
            Assert.Contains("> Project: test-project", content);
            Assert.Contains("## Recent Activity", content);
            Assert.Contains("## Key Architectural Decisions", content);
            Assert.Contains("## Conventions & Patterns", content);
            Assert.Contains("> Generated:", content);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task FR_006_ExportAsync_ContainsRecentActivity()
    {
        var data = CreateSampleBriefingData();
        var exporter = new MarkdownExporter();
        var tempFile = Path.Combine(Path.GetTempPath(), $"onboarding-test-{Guid.NewGuid()}.md");

        try
        {
            await exporter.ExportAsync(data, tempFile, "test-user");
            var content = File.ReadAllText(tempFile);

            Assert.Contains("Session content here", content);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task FR_006_ExportAsync_ContainsDecisionData()
    {
        var data = CreateSampleBriefingData();
        var exporter = new MarkdownExporter();
        var tempFile = Path.Combine(Path.GetTempPath(), $"onboarding-test-{Guid.NewGuid()}.md");

        try
        {
            await exporter.ExportAsync(data, tempFile, "test-user");
            var content = File.ReadAllText(tempFile);

            Assert.Contains("Decision A", content);
            Assert.Contains("Decision B", content);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task FR_006_ExportAsync_ContainsPatternData()
    {
        var data = CreateSampleBriefingData();
        var exporter = new MarkdownExporter();
        var tempFile = Path.Combine(Path.GetTempPath(), $"onboarding-test-{Guid.NewGuid()}.md");

        try
        {
            await exporter.ExportAsync(data, tempFile, "test-user");
            var content = File.ReadAllText(tempFile);

            Assert.Contains("Pattern X", content);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task FR_006_ExportAsync_EmptyRecentActivity_OmitsSection()
    {
        var data = new BriefingData(
            Project: "proj",
            Scope: "team",
            Stats: null,
            RecentActivity: null,
            Decisions: [new EngramSearchResult(1, "decision", "D1", "preview", "2024-01-01", "proj", null, 1.0)],
            Patterns: [],
            Blockers: [],
            HasData: true
        );

        var exporter = new MarkdownExporter();
        var tempFile = Path.Combine(Path.GetTempPath(), $"onboarding-test-{Guid.NewGuid()}.md");

        try
        {
            await exporter.ExportAsync(data, tempFile, null);
            var content = File.ReadAllText(tempFile);

            // When RecentActivity is null/empty, the section is omitted
            Assert.DoesNotContain("## Recent Activity", content);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task FR_006_ExportAsync_EmptyDecisions_OmitsSection()
    {
        var data = new BriefingData(
            Project: "proj",
            Scope: "team",
            Stats: null,
            RecentActivity: null,
            Decisions: [],
            Patterns: [],
            Blockers: [],
            HasData: false
        );

        var exporter = new MarkdownExporter();
        var tempFile = Path.Combine(Path.GetTempPath(), $"onboarding-test-{Guid.NewGuid()}.md");

        try
        {
            await exporter.ExportAsync(data, tempFile, null);
            var content = File.ReadAllText(tempFile);

            // When decisions are empty, the section is omitted
            Assert.DoesNotContain("## Key Architectural Decisions", content);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task FR_006_ExportAsync_EmptyPatterns_OmitsSection()
    {
        var data = new BriefingData(
            Project: "proj",
            Scope: "team",
            Stats: null,
            RecentActivity: null,
            Decisions: [],
            Patterns: [],
            Blockers: [],
            HasData: false
        );

        var exporter = new MarkdownExporter();
        var tempFile = Path.Combine(Path.GetTempPath(), $"onboarding-test-{Guid.NewGuid()}.md");

        try
        {
            await exporter.ExportAsync(data, tempFile, null);
            var content = File.ReadAllText(tempFile);

            // When patterns are empty, the section is omitted
            Assert.DoesNotContain("## Conventions & Patterns", content);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task FR_006_ExportAsync_ContainsUserAndTimestamp()
    {
        var data = new BriefingData(
            Project: "my-project",
            Scope: "team",
            Stats: null,
            RecentActivity: null,
            Decisions: [],
            Patterns: [],
            Blockers: [],
            HasData: false
        );

        var exporter = new MarkdownExporter();
        var tempFile = Path.Combine(Path.GetTempPath(), $"onboarding-test-{Guid.NewGuid()}.md");

        try
        {
            await exporter.ExportAsync(data, tempFile, "test@example.com");
            var content = File.ReadAllText(tempFile);

            Assert.Contains("> For: test@example.com", content);
            Assert.Contains("> Project: my-project", content);
            // Timestamp is generated at export time (UTC), just check format
            Assert.Contains("> Generated:", content);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task FR_006_ExportAsync_PersonalScope_ReturnsFalse()
    {
        // NFR-005: personal scope must be blocked from export
        var data = new BriefingData(
            Project: "proj",
            Scope: "personal",
            Stats: null,
            RecentActivity: "some activity",
            Decisions: [],
            Patterns: [],
            Blockers: [],
            HasData: true
        );

        var exporter = new MarkdownExporter();
        var tempFile = Path.Combine(Path.GetTempPath(), $"onboarding-test-{Guid.NewGuid()}.md");

        try
        {
            var result = await exporter.ExportAsync(data, tempFile, "test-user");

            // Export should be skipped for personal scope
            Assert.False(result);
            Assert.False(File.Exists(tempFile));
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task FR_006_ExportAsync_WritesFile()
    {
        var data = CreateSampleBriefingData();
        var exporter = new MarkdownExporter();
        var tempFile = Path.Combine(Path.GetTempPath(), $"onboarding-test-{Guid.NewGuid()}.md");

        try
        {
            var result = await exporter.ExportAsync(data, tempFile, "test-user");
            Assert.True(result);
            Assert.True(File.Exists(tempFile));

            var content = File.ReadAllText(tempFile);
            Assert.Contains("> Project: test-project", content);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static BriefingData CreateSampleBriefingData()
    {
        return new BriefingData(
            Project: "test-project",
            Scope: "team",
            Stats: new EngramStats(
                TotalSessions: 10,
                TotalObservations: 20,
                TotalPrompts: 5,
                Projects: ["test-project"],
                Backend: "sqlite"
            ),
            RecentActivity: "Session content here",
            Decisions:
            [
                new EngramSearchResult(1, "decision", "Decision A", "What: Did A", "2024-01-15", "test-project", null, 1.0),
                new EngramSearchResult(2, "decision", "Decision B", "What: Did B", "2024-01-14", "test-project", null, 0.9),
            ],
            Patterns:
            [
                new EngramSearchResult(3, "pattern", "Pattern X", "What: Pattern X", "2024-01-15", "test-project", null, 0.8),
            ],
            Blockers: [],
            HasData: true
        );
    }
}
