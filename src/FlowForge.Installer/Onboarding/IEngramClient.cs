namespace FlowForge.Installer.Onboarding;

/// <summary>
/// Abstraction for engram memory retrieval. Two implementations:
/// - HttpEngramClient: HTTP API (primary, JSON structured)
/// - CliEngramClient: CLI subprocess (fallback, text parsing)
/// </summary>
public interface IEngramClient
{
    /// <summary>Check if the client is available (server reachable / binary exists).</summary>
    Task<bool> HealthCheckAsync(CancellationToken ct = default);

    /// <summary>Get recent context (sessions + observations + prompts).</summary>
    /// <returns>Markdown-formatted context string, or null if no data.</returns>
    Task<string?> GetContextAsync(string? project, string? scope, CancellationToken ct = default);

    /// <summary>Search memories by query and type.</summary>
    /// <returns>List of search results (id, type, title, preview, createdAt, project).</returns>
    Task<IReadOnlyList<EngramSearchResult>> SearchAsync(
        string query,
        string? type = null,
        string? project = null,
        string? scope = null,
        int limit = 10,
        CancellationToken ct = default);

    /// <summary>Get project stats (sessions, observations, prompts, projects).</summary>
    Task<EngramStats?> GetStatsAsync(CancellationToken ct = default);

    /// <summary>Get full observation content by ID.</summary>
    Task<EngramObservation?> GetObservationAsync(long id, CancellationToken ct = default);

    /// <summary>Get timeline around an observation (drill-down).</summary>
    Task<string?> GetTimelineAsync(long observationId, int before = 5, int after = 5, CancellationToken ct = default);
}
