namespace FlowForge.Installer.Onboarding;

/// <summary>
/// Aggregated briefing data from engram memories.
/// </summary>
public sealed record BriefingData(
    string Project,
    string? Scope,
    EngramStats? Stats,
    string? RecentActivity,           // markdown from mem_context
    IReadOnlyList<EngramSearchResult> Decisions,
    IReadOnlyList<EngramSearchResult> Patterns,
    IReadOnlyList<EngramSearchResult> Blockers,
    bool HasData);                    // false if all sections empty

/// <summary>
/// Orchestrates IEngramClient calls to aggregate briefing data.
/// Calls mem_context (recent activity), mem_search (decisions, patterns, blockers), mem_stats.
/// </summary>
public sealed class BriefingAggregator
{
    readonly IEngramClient _client;

    public BriefingAggregator(IEngramClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    /// <summary>
    /// Aggregates all briefing data for the given project.
    /// </summary>
    /// <param name="project">Namespaced project name (e.g. "team/flowforge").</param>
    /// <param name="scope">"team" or "personal".</param>
    /// <param name="limit">Max items per section (clamped 1..20).</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<BriefingData> AggregateAsync(
        string project,
        string? scope,
        int limit,
        CancellationToken ct = default)
    {
        // Clamp limit to 1..20
        limit = Math.Clamp(limit, 1, 20);

        // Run all queries in parallel for performance
        var contextTask = SafeGetContextAsync(project, scope, ct);
        // FR-005: Use type name as query (server requires non-empty q, FTS5 doesn't support "*" wildcard reliably)
        var decisionsTask = SafeSearchAsync("decision", "decision", project, scope, limit, ct);
        // FR-006: Use type name for patterns (conventions are stored as type=pattern)
        var patternsTask = SafeSearchAsync("pattern", "pattern", project, scope, limit, ct);
        // FR-007: Blockers search must use type=bugfix and type=manual (not null/all types)
        var blockersBugfixTask = SafeSearchAsync("bugfix", "bugfix", project, scope, limit, ct);
        var blockersManualTask = SafeSearchAsync("manual", "manual", project, scope, limit, ct);
        var statsTask = SafeGetStatsAsync(ct);

        await Task.WhenAll(contextTask, decisionsTask, patternsTask, blockersBugfixTask, blockersManualTask, statsTask)
            .ConfigureAwait(false);

        var recentActivity = await contextTask.ConfigureAwait(false);
        var decisions = await decisionsTask.ConfigureAwait(false);
        var patterns = await patternsTask.ConfigureAwait(false);
        var blockersBugfix = await blockersBugfixTask.ConfigureAwait(false);
        var blockersManual = await blockersManualTask.ConfigureAwait(false);
        var stats = await statsTask.ConfigureAwait(false);

        // Combine bugfix + manual blockers, deduplicating by ID
        var blockers = CombineBlockers(blockersBugfix, blockersManual);

        var hasData = !string.IsNullOrWhiteSpace(recentActivity)
            || decisions.Count > 0
            || patterns.Count > 0
            || blockers.Count > 0;

        return new BriefingData(
            Project: project,
            Scope: scope,
            Stats: stats,
            RecentActivity: recentActivity,
            Decisions: decisions,
            Patterns: patterns,
            Blockers: blockers,
            HasData: hasData);
    }

    // ── Safe wrappers (degrade gracefully on failure) ─────────────────────────

    async Task<string?> SafeGetContextAsync(string project, string? scope, CancellationToken ct)
    {
        try { return await _client.GetContextAsync(project, scope, ct).ConfigureAwait(false); }
        catch { return null; }
    }

    async Task<IReadOnlyList<EngramSearchResult>> SafeSearchAsync(
        string query, string? type, string project, string? scope, int limit, CancellationToken ct)
    {
        try { return await _client.SearchAsync(query, type, project, scope, limit, ct).ConfigureAwait(false); }
        catch { return Array.Empty<EngramSearchResult>(); }
    }

    async Task<EngramStats?> SafeGetStatsAsync(CancellationToken ct)
    {
        try { return await _client.GetStatsAsync(ct).ConfigureAwait(false); }
        catch { return null; }
    }

    /// <summary>
    /// Combines bugfix and manual blocker results, deduplicating by ID.
    /// </summary>
    static IReadOnlyList<EngramSearchResult> CombineBlockers(
        IReadOnlyList<EngramSearchResult> bugfix,
        IReadOnlyList<EngramSearchResult> manual)
    {
        if (bugfix.Count == 0) return manual;
        if (manual.Count == 0) return bugfix;

        var seen = new HashSet<long>();
        var combined = new List<EngramSearchResult>();
        foreach (var r in bugfix)
        {
            if (seen.Add(r.Id)) combined.Add(r);
        }
        foreach (var r in manual)
        {
            if (seen.Add(r.Id)) combined.Add(r);
        }
        return combined;
    }
}
