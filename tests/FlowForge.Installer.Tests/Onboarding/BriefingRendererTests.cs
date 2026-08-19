using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FlowForge.Installer.Onboarding;
using Xunit;

namespace FlowForge.Installer.Tests.Onboarding;

/// <summary>
/// FR-010: Tests for the interactive drill-down loop in BriefingRenderer.
/// Verifies that "{number}t" input invokes GetTimelineAsync(id, before:5, after:5).
/// Uses Console.In redirection to simulate interactive input.
/// </summary>
public class BriefingRendererTests
{
    [Fact] // FR-010 — timeline drill-down via "{number}t" input
    public async Task DrillDown_WithTimelineSuffix_CallsGetTimelineAsync()
    {
        // Arrange: mock client that tracks GetTimelineAsync calls
        var mockClient = new TrackingMockEngramClient
        {
            TimelineResult = "Timeline data for observation 42",
        };

        var renderer = new BriefingRenderer(mockClient, interactive: true);

        var data = new BriefingData(
            Project: "team/flowforge",
            Scope: "team",
            Stats: new EngramStats(10, 20, 5, ["ff"], "postgres"),
            RecentActivity: "# Recent\n- Activity",
            Decisions: [new EngramSearchResult(42, "decision", "Use AOT", "Preview", "2026-08-01", "team/ff", "team", 0.9)],
            Patterns: [],
            Blockers: [],
            HasData: true);

        // Simulate user input: "1t" (timeline for item #1), then empty line to exit
        var originalIn = Console.In;
        try
        {
            using var reader = new StringReader("1t\n\n");
            Console.SetIn(reader);

            // Act
            await renderer.RenderAsync(data, "testuser");

            // Assert: GetTimelineAsync was called with the right parameters
            Assert.True(mockClient.GetTimelineAsyncCalled);
            Assert.Equal(42, mockClient.LastTimelineObservationId);
            Assert.Equal(5, mockClient.LastTimelineBefore);
            Assert.Equal(5, mockClient.LastTimelineAfter);
        }
        finally
        {
            Console.SetIn(originalIn);
        }
    }

    [Fact] // FR-010 — plain "{number}" invokes GetObservationAsync (not timeline)
    public async Task DrillDown_WithPlainNumber_CallsGetObservationAsync()
    {
        // Arrange
        var mockClient = new TrackingMockEngramClient
        {
            ObservationResult = new EngramObservation(42, "decision", "Use AOT", "Full content", "team/ff", "team", "aot", "2026-08-01", 1),
        };

        var renderer = new BriefingRenderer(mockClient, interactive: true);

        var data = new BriefingData(
            Project: "team/flowforge",
            Scope: "team",
            Stats: null,
            RecentActivity: null,
            Decisions: [new EngramSearchResult(42, "decision", "Use AOT", "Preview", "2026-08-01", "team/ff", "team", 0.9)],
            Patterns: [],
            Blockers: [],
            HasData: true);

        // Simulate user input: "1" (drill-down for item #1), then empty line to exit
        var originalIn = Console.In;
        try
        {
            using var reader = new StringReader("1\n\n");
            Console.SetIn(reader);

            // Act
            await renderer.RenderAsync(data, "testuser");

            // Assert: GetObservationAsync was called, GetTimelineAsync was NOT called
            Assert.True(mockClient.GetObservationAsyncCalled);
            Assert.Equal(42, mockClient.LastObservationId);
            Assert.False(mockClient.GetTimelineAsyncCalled);
        }
        finally
        {
            Console.SetIn(originalIn);
        }
    }

    [Fact] // FR-010 — invalid input does not call either method
    public async Task DrillDown_WithInvalidInput_CallsNeitherMethod()
    {
        // Arrange
        var mockClient = new TrackingMockEngramClient();

        var renderer = new BriefingRenderer(mockClient, interactive: true);

        var data = new BriefingData(
            Project: "team/flowforge",
            Scope: "team",
            Stats: null,
            RecentActivity: null,
            Decisions: [new EngramSearchResult(42, "decision", "Use AOT", "Preview", "2026-08-01", "team/ff", "team", 0.9)],
            Patterns: [],
            Blockers: [],
            HasData: true);

        // Simulate user input: "99" (out of range), then empty line to exit
        var originalIn = Console.In;
        try
        {
            using var reader = new StringReader("99\n\n");
            Console.SetIn(reader);

            // Act
            await renderer.RenderAsync(data, "testuser");

            // Assert: neither method was called (invalid input)
            Assert.False(mockClient.GetObservationAsyncCalled);
            Assert.False(mockClient.GetTimelineAsyncCalled);
        }
        finally
        {
            Console.SetIn(originalIn);
        }
    }

    /// <summary>
    /// Mock IEngramClient that tracks calls to GetObservationAsync and GetTimelineAsync.
    /// </summary>
    sealed class TrackingMockEngramClient : IEngramClient
    {
        public string? ContextResult { get; set; }
        public EngramStats? StatsResult { get; set; }
        public EngramObservation? ObservationResult { get; set; }
        public string? TimelineResult { get; set; }

        public bool GetObservationAsyncCalled { get; private set; }
        public long LastObservationId { get; private set; }

        public bool GetTimelineAsyncCalled { get; private set; }
        public long LastTimelineObservationId { get; private set; }
        public int LastTimelineBefore { get; private set; }
        public int LastTimelineAfter { get; private set; }

        public Task<bool> HealthCheckAsync(CancellationToken ct = default) => Task.FromResult(true);

        public Task<string?> GetContextAsync(string? project, string? scope, CancellationToken ct = default)
            => Task.FromResult(ContextResult);

        public Task<IReadOnlyList<EngramSearchResult>> SearchAsync(
            string query, string? type, string? project, string? scope, int limit, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<EngramSearchResult>>(Array.Empty<EngramSearchResult>());

        public Task<EngramStats?> GetStatsAsync(CancellationToken ct = default)
            => Task.FromResult(StatsResult);

        public Task<EngramObservation?> GetObservationAsync(long id, CancellationToken ct = default)
        {
            GetObservationAsyncCalled = true;
            LastObservationId = id;
            return Task.FromResult(ObservationResult);
        }

        public Task<string?> GetTimelineAsync(long observationId, int before = 5, int after = 5, CancellationToken ct = default)
        {
            GetTimelineAsyncCalled = true;
            LastTimelineObservationId = observationId;
            LastTimelineBefore = before;
            LastTimelineAfter = after;
            return Task.FromResult(TimelineResult);
        }
    }
}
