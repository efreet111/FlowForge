using System.Text.Json.Serialization;

namespace FlowForge.Installer.Onboarding;

// ── Internal domain models (used across the onboarding pipeline) ──────────────

/// <summary>
/// A single search result from engram (used in BriefingData).
/// </summary>
public sealed record EngramSearchResult(
    long Id,
    string Type,
    string Title,
    string Preview,
    string CreatedAt,
    string Project,
    string? Scope,
    double Rank);

/// <summary>
/// Project stats from mem_stats.
/// </summary>
public sealed record EngramStats(
    int TotalSessions,
    int TotalObservations,
    int TotalPrompts,
    IReadOnlyList<string> Projects,
    string Backend);

/// <summary>
/// Full observation content (from mem_get_observation).
/// </summary>
public sealed record EngramObservation(
    long Id,
    string Type,
    string Title,
    string Content,
    string Project,
    string? Scope,
    string? TopicKey,
    string CreatedAt,
    string? SessionId);

// ── HTTP response wrappers (match EngramServer JSON shape) ────────────────────

/// <summary>
/// Response from GET /context — markdown-formatted context string.
/// </summary>
public sealed record EngramContextResponse(string Context);

/// <summary>
/// Response from GET /search — list of search result items.
/// </summary>
public sealed record EngramSearchResponse(IReadOnlyList<EngramSearchResultItem> Results);

/// <summary>
/// A single item in the search response (observation + rank).
/// </summary>
public sealed record EngramSearchResultItem(EngramObservationDto Observation, double Rank);

/// <summary>
/// DTO for the observation nested in search results (server uses snake_case).
/// </summary>
public sealed record EngramObservationDto(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("content")] string Content,
    [property: JsonPropertyName("project")] string Project,
    [property: JsonPropertyName("scope")] string? Scope,
    [property: JsonPropertyName("topic_key")] string? TopicKey,
    [property: JsonPropertyName("created_at")] string CreatedAt,
    [property: JsonPropertyName("session_id")] string? SessionId);

/// <summary>
/// Response from GET /stats (server uses snake_case).
/// </summary>
public sealed record EngramStatsResponse(
    [property: JsonPropertyName("total_sessions")] int TotalSessions,
    [property: JsonPropertyName("total_observations")] int TotalObservations,
    [property: JsonPropertyName("total_prompts")] int TotalPrompts,
    [property: JsonPropertyName("projects")] IReadOnlyList<string> Projects,
    [property: JsonPropertyName("backend")] string Backend);

// ── Source-generated JSON context (AOT-safe, no reflection) ───────────────────

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(EngramContextResponse))]
[JsonSerializable(typeof(EngramSearchResponse))]
[JsonSerializable(typeof(EngramSearchResultItem))]
[JsonSerializable(typeof(EngramObservationDto))]
[JsonSerializable(typeof(EngramStatsResponse))]
[JsonSerializable(typeof(IReadOnlyList<EngramSearchResultItem>))]
[JsonSerializable(typeof(IReadOnlyList<string>))]
public partial class OnboardingJsonContext : JsonSerializerContext { }
