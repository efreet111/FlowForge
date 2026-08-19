using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FlowForge.Installer.Onboarding;
using Xunit;

namespace FlowForge.Installer.Tests.Onboarding;

/// <summary>
/// Integration tests for the onboarding flow.
/// Tests the end-to-end flow with mock HTTP server / mock client.
/// </summary>
public class OnboardCommandIntegrationTests
{
    [Fact] // PM-1 — Happy path
    public async Task HappyPath_AllSectionsPopulated_BriefingRendered()
    {
        var mock = new MockEngramClient
        {
            ContextResult = "# Recent activity\n- Session 1",
            SearchResults = new()
            {
                ["decision"] = [new EngramSearchResult(1, "decision", "Use AOT", "Preview", "2026-08-01", "team/ff", "team", 0.9)],
                ["pattern"] = [new EngramSearchResult(2, "pattern", "Naming", "Preview", "2026-08-01", "team/ff", "team", 0.8)],
            },
            StatsResult = new EngramStats(10, 20, 5, ["ff"], "postgres"),
        };

        var aggregator = new BriefingAggregator(mock);
        var data = await aggregator.AggregateAsync("team/flowforge", "team", 10);

        Assert.True(data.HasData);
        Assert.NotNull(data.RecentActivity);
        Assert.Single(data.Decisions);
        Assert.Single(data.Patterns);
    }

    [Fact] // PM-2 — Pre-check failure (simulated)
    public void PreCheckFailure_EngarmBinaryMissing_DetectedEarly()
    {
        // Verify that a missing binary is detected
        var client = new CliEngramClient("/nonexistent/engram");
        var healthy = client.HealthCheckAsync().Result;
        Assert.False(healthy);
    }

    [Fact] // PM-3 — Empty memory path
    public async Task EmptyMemory_HasDataFalse_TriggersGuidance()
    {
        var mock = new MockEngramClient
        {
            ContextResult = null,
            SearchResults = new(),
            StatsResult = null,
        };

        var aggregator = new BriefingAggregator(mock);
        var data = await aggregator.AggregateAsync("team/empty", "team", 10);

        Assert.False(data.HasData);
    }

    [Fact] // PM-4 — Export to ONBOARDING.md (atomic write, team scope only)
    public async Task Export_TeamScope_WritesAtomically()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"ff-export-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var data = new BriefingData(
                Project: "team/flowforge",
                Scope: "team",
                Stats: new EngramStats(10, 20, 5, ["ff"], "postgres"),
                RecentActivity: "# Recent\n- Activity",
                Decisions: [new EngramSearchResult(1, "decision", "AOT", "Preview", "2026-08-01", "team/ff", "team", 0.9)],
                Patterns: [],
                Blockers: [],
                HasData: true);

            var outputPath = Path.Combine(tempDir, "ONBOARDING.md");
            var exporter = new MarkdownExporter();
            var result = await exporter.ExportAsync(data, outputPath, "testuser");

            Assert.True(result);
            Assert.True(File.Exists(outputPath));

            var content = File.ReadAllText(outputPath);
            Assert.Contains("Generated:", content);
            Assert.Contains("team/flowforge", content);
            Assert.Contains("Recent Activity", content);
            Assert.Contains("Key Architectural Decisions", content);
            Assert.Contains("AOT", content);

            // Verify no .tmp file remains
            Assert.False(File.Exists(outputPath + ".tmp"));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact] // NFR-005 — Personal scope export blocked
    public async Task Export_PersonalScope_ReturnsFalse()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"ff-export-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var data = new BriefingData(
                Project: "user/flowforge",
                Scope: "personal",
                Stats: null,
                RecentActivity: "Some activity",
                Decisions: [],
                Patterns: [],
                Blockers: [],
                HasData: true);

            var outputPath = Path.Combine(tempDir, "ONBOARDING.md");
            var exporter = new MarkdownExporter();
            var result = await exporter.ExportAsync(data, outputPath, "testuser");

            Assert.False(result); // Personal scope should be blocked
            Assert.False(File.Exists(outputPath)); // No file written
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact] // NFR-005/T6 — Default export filters out personal-scope items
    public async Task Export_DefaultScope_FiltersOutPersonalItems()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"ff-export-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            // Simulate wide-read data with mixed scopes
            var data = new BriefingData(
                Project: "team/flowforge",
                Scope: null, // default scope (wide-read)
                Stats: new EngramStats(10, 20, 5, ["ff"], "postgres"),
                RecentActivity: "# Recent\n- Activity",
                Decisions: [
                    new EngramSearchResult(1, "decision", "Team Decision", "Preview", "2026-08-01", "team/ff", "team", 0.9),
                    new EngramSearchResult(2, "decision", "Personal Decision", "Preview", "2026-08-01", "user/ff", "personal", 0.8),
                ],
                Patterns: [
                    new EngramSearchResult(3, "pattern", "Team Pattern", "Preview", "2026-08-01", "team/ff", "team", 0.7),
                    new EngramSearchResult(4, "pattern", "Personal Pattern", "Preview", "2026-08-01", "user/ff", "personal", 0.6),
                ],
                Blockers: [
                    new EngramSearchResult(5, "bugfix", "Team Bug", "Preview", "2026-08-01", "team/ff", "team", 0.5),
                    new EngramSearchResult(6, "manual", "Personal Note", "Preview", "2026-08-01", "user/ff", "personal", 0.4),
                ],
                HasData: true);

            var outputPath = Path.Combine(tempDir, "ONBOARDING.md");
            var exporter = new MarkdownExporter();
            var result = await exporter.ExportAsync(data, outputPath, "testuser");

            Assert.True(result);
            Assert.True(File.Exists(outputPath));

            var content = File.ReadAllText(outputPath);
            // Team-scope items should be present
            Assert.Contains("Team Decision", content);
            Assert.Contains("Team Pattern", content);
            Assert.Contains("Team Bug", content);
            // Personal-scope items must NOT be present
            Assert.DoesNotContain("Personal Decision", content);
            Assert.DoesNotContain("Personal Pattern", content);
            Assert.DoesNotContain("Personal Note", content);
            // Scope header should say "team"
            Assert.Contains("Scope: team", content);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    // ── Mock IEngramClient ────────────────────────────────────────────────────

    sealed class MockEngramClient : IEngramClient
    {
        public string? ContextResult { get; set; }
        public Dictionary<string, IReadOnlyList<EngramSearchResult>> SearchResults { get; set; } = new();
        public EngramStats? StatsResult { get; set; }

        public Task<bool> HealthCheckAsync(CancellationToken ct = default) => Task.FromResult(true);

        public Task<string?> GetContextAsync(string? project, string? scope, CancellationToken ct = default)
            => Task.FromResult(ContextResult);

        public Task<IReadOnlyList<EngramSearchResult>> SearchAsync(
            string query, string? type, string? project, string? scope, int limit, CancellationToken ct = default)
        {
            if (type != null && SearchResults.TryGetValue(type, out var results))
                return Task.FromResult(results);
            return Task.FromResult<IReadOnlyList<EngramSearchResult>>(Array.Empty<EngramSearchResult>());
        }

        public Task<EngramStats?> GetStatsAsync(CancellationToken ct = default)
            => Task.FromResult(StatsResult);

        public Task<EngramObservation?> GetObservationAsync(long id, CancellationToken ct = default)
            => Task.FromResult<EngramObservation?>(null);

        public Task<string?> GetTimelineAsync(long observationId, int before, int after, CancellationToken ct = default)
            => Task.FromResult<string?>(null);
    }
}
