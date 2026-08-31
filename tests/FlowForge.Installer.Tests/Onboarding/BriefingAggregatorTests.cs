using System.Threading;
using System.Threading.Tasks;
using FlowForge.Installer.Onboarding;
using Xunit;

namespace FlowForge.Installer.Tests.Onboarding;

public class BriefingAggregatorTests
{
    [Fact] // FR-004, FR-005, FR-006, FR-007, FR-008
    public async Task AllSectionsPopulated_HasDataTrue()
    {
        var mock = new MockEngramClient
        {
            ContextResult = "# Recent activity\n- Session 1\n- Session 2",
            SearchResults = new Dictionary<string, IReadOnlyList<EngramSearchResult>>
            {
                ["decision"] = [new EngramSearchResult(1, "decision", "Use AOT", "Preview", "2026-08-01", "team/ff", "team", 0.9)],
                ["pattern"] = [new EngramSearchResult(2, "pattern", "Naming convention", "Preview", "2026-08-01", "team/ff", "team", 0.8)],
            },
            StatsResult = new EngramStats(45, 65, 23, ["flowforge"], "postgres"),
        };

        var aggregator = new BriefingAggregator(mock);
        var data = await aggregator.AggregateAsync("team/flowforge", "team", 10);

        Assert.True(data.HasData);
        Assert.NotNull(data.RecentActivity);
        Assert.Single(data.Decisions);
        Assert.Single(data.Patterns);
        Assert.NotNull(data.Stats);
    }

    [Fact] // FR-012
    public async Task AllSectionsEmpty_HasDataFalse()
    {
        var mock = new MockEngramClient
        {
            ContextResult = null,
            SearchResults = new Dictionary<string, IReadOnlyList<EngramSearchResult>>(),
            StatsResult = null,
        };

        var aggregator = new BriefingAggregator(mock);
        var data = await aggregator.AggregateAsync("team/empty", "team", 10);

        Assert.False(data.HasData);
        Assert.Null(data.RecentActivity);
        Assert.Empty(data.Decisions);
        Assert.Empty(data.Patterns);
        Assert.Empty(data.Blockers);
    }

    [Fact] // FR-008
    public async Task StatsFailure_DegradesGracefully()
    {
        var mock = new MockEngramClient
        {
            ContextResult = "Some activity",
            StatsResult = null, // Simulate stats failure
            ShouldThrowOnStats = true,
        };

        var aggregator = new BriefingAggregator(mock);
        var data = await aggregator.AggregateAsync("team/ff", "team", 10);

        Assert.True(data.HasData); // Still has data from context
        Assert.Null(data.Stats); // Stats degraded gracefully
    }

    [Fact] // FR-005, FR-006
    public async Task LimitClamped_Between1And20()
    {
        var mock = new MockEngramClient();
        var aggregator = new BriefingAggregator(mock);

        // Limit 0 should be clamped to 1
        await aggregator.AggregateAsync("team/ff", "team", 0);
        Assert.Equal(1, mock.LastLimit);

        // Limit 100 should be clamped to 20
        await aggregator.AggregateAsync("team/ff", "team", 100);
        Assert.Equal(20, mock.LastLimit);
    }

    [Fact] // FR-006 — Guard: never uses type=convention
    public async Task PatternSearch_UsesTypePattern_NotConvention()
    {
        var mock = new MockEngramClient();
        var aggregator = new BriefingAggregator(mock);
        await aggregator.AggregateAsync("team/ff", "team", 10);

        // Verify that pattern search was called with type="pattern", not "convention"
        Assert.Contains(mock.SearchCalls, c => c.Type == "pattern");
        Assert.DoesNotContain(mock.SearchCalls, c => c.Type == "convention");
    }

    [Fact] // FR-007 — Blockers search must use type=bugfix or type=manual (NOT null)
    public async Task BlockersSearch_UsesTypeBugfixAndManual_NotNull()
    {
        var mock = new MockEngramClient();
        var aggregator = new BriefingAggregator(mock);
        await aggregator.AggregateAsync("team/ff", "team", 10);

        // FR-007: Blockers must search with type="bugfix" and type="manual"
        Assert.Contains(mock.SearchCalls, c => c.Type == "bugfix");
        Assert.Contains(mock.SearchCalls, c => c.Type == "manual");
        // Must NOT use null (all types) for blockers
        var blockerCalls = mock.SearchCalls.Where(c =>
            c.Query.Contains("blocker") || c.Query.Contains("bug") || c.Query.Contains("gotcha"));
        Assert.All(blockerCalls, c => Assert.NotNull(c.Type));
        Assert.DoesNotContain(mock.SearchCalls, c =>
            (c.Query.Contains("blocker") || c.Query.Contains("bug")) && c.Type == null);
    }

    [Fact] // FR-007 — Blockers combines bugfix + manual results
    public async Task BlockersCombined_DeduplicatesById()
    {
        var bugfixResult = new EngramSearchResult(10, "bugfix", "Bug A", "Preview", "2026-08-01", "team/ff", "team", 0.9);
        var manualResult = new EngramSearchResult(10, "manual", "Bug A (manual)", "Preview", "2026-08-01", "team/ff", "team", 0.8);
        var manualResult2 = new EngramSearchResult(20, "manual", "Manual B", "Preview", "2026-08-01", "team/ff", "team", 0.7);

        var mock = new MockEngramClient
        {
            SearchResults = new Dictionary<string, IReadOnlyList<EngramSearchResult>>
            {
                ["bugfix"] = [bugfixResult],
                ["manual"] = [manualResult, manualResult2],
            },
        };

        var aggregator = new BriefingAggregator(mock);
        var data = await aggregator.AggregateAsync("team/ff", "team", 10);

        // Should have 2 blockers (ID 10 deduplicated, ID 20 from manual)
        Assert.Equal(2, data.Blockers.Count);
        Assert.Contains(data.Blockers, b => b.Id == 10);
        Assert.Contains(data.Blockers, b => b.Id == 20);
    }

    // ── Mock IEngramClient ────────────────────────────────────────────────────

    sealed class MockEngramClient : IEngramClient
    {
        public string? ContextResult { get; set; }
        public Dictionary<string, IReadOnlyList<EngramSearchResult>> SearchResults { get; set; } = new();
        public EngramStats? StatsResult { get; set; }
        public bool ShouldThrowOnStats { get; set; }
        public int LastLimit { get; private set; }
        public List<(string Query, string? Type)> SearchCalls { get; } = new();

        public Task<bool> HealthCheckAsync(CancellationToken ct = default) => Task.FromResult(true);

        public Task<string?> GetContextAsync(string? project, string? scope, CancellationToken ct = default)
            => Task.FromResult(ContextResult);

        public Task<IReadOnlyList<EngramSearchResult>> SearchAsync(
            string query, string? type, string? project, string? scope, int limit, CancellationToken ct = default)
        {
            LastLimit = limit;
            SearchCalls.Add((query, type));
            if (type != null && SearchResults.TryGetValue(type, out var results))
                return Task.FromResult(results);
            return Task.FromResult<IReadOnlyList<EngramSearchResult>>(Array.Empty<EngramSearchResult>());
        }

        public Task<EngramStats?> GetStatsAsync(CancellationToken ct = default)
        {
            if (ShouldThrowOnStats) throw new Exception("DB locked");
            return Task.FromResult(StatsResult);
        }

        public Task<EngramObservation?> GetObservationAsync(long id, CancellationToken ct = default)
            => Task.FromResult<EngramObservation?>(null);

        public Task<string?> GetTimelineAsync(long observationId, int before, int after, CancellationToken ct = default)
            => Task.FromResult<string?>(null);
    }
}
