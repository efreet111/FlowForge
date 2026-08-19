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
        var decisionsTask = SafeSearchAsync("", "decision", project, scope, limit, ct);
        var patternsTask = SafeSearchAsync(
            "convention OR naming OR style OR workflow",
            "pattern", project, scope, limit, ct);
        var blockersTask = SafeSearchAsync(
            "blocker OR gotcha OR issue OR bug OR workaround",
            null, project, scope, limit, ct); // search across all types (bugfix/manual)
        var statsTask = SafeGetStatsAsync(ct);

        await Task.WhenAll(contextTask, decisionsTask, patternsTask, blockersTask, statsTask)
            .ConfigureAwait(false);

        var recentActivity = await contextTask.ConfigureAwait(false);
        var decisions = await decisionsTask.ConfigureAwait(false);
        var patterns = await patternsTask.ConfigureAwait(false);
        var blockers = await blockersTask.ConfigureAwait(false);
        var stats = await statsTask.ConfigureAwait(false);

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
}
