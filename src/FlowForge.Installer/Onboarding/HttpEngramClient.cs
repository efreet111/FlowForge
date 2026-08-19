using System.Net.Http.Headers;
using System.Text.Json;

namespace FlowForge.Installer.Onboarding;

/// <summary>
/// HTTP API implementation of IEngramClient.
/// Calls /context, /search, /stats, /observations/{id}, /timeline, /health.
/// Sets X-Engram-User header on all requests.
/// Deserializes with OnboardingJsonContext (source-gen, AOT-safe).
/// </summary>
public sealed class HttpEngramClient : IEngramClient, IDisposable
{
    readonly HttpClient _http;
    readonly string _remoteUrl;
    readonly string _engramUser;
    readonly bool _ownsHttpClient;

    /// <summary>
    /// Creates a new HTTP engram client.
    /// </summary>
    /// <param name="http">HttpClient instance (caller-owned or client-owned).</param>
    /// <param name="remoteUrl">Base URL of the engram sync server (e.g. https://engram.example.com).</param>
    /// <param name="engramUser">User identity for X-Engram-User header.</param>
    /// <param name="ownsHttpClient">If true, disposes the HttpClient on Dispose.</param>
    public HttpEngramClient(HttpClient http, string remoteUrl, string engramUser, bool ownsHttpClient = false)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _remoteUrl = remoteUrl?.TrimEnd('/') ?? throw new ArgumentNullException(nameof(remoteUrl));
        _engramUser = engramUser ?? throw new ArgumentNullException(nameof(engramUser));
        _ownsHttpClient = ownsHttpClient;
    }

    public async Task<bool> HealthCheckAsync(CancellationToken ct = default)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, $"{_remoteUrl}/health");
            SetHeaders(req);
            using var resp = await _http.SendAsync(req, ct).ConfigureAwait(false);
            return resp.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<string?> GetContextAsync(string? project, string? scope, CancellationToken ct = default)
    {
        var url = $"{_remoteUrl}/context";
        var queryParts = new List<string>();
        if (!string.IsNullOrEmpty(project)) queryParts.Add($"project={Uri.EscapeDataString(project)}");
        if (!string.IsNullOrEmpty(scope)) queryParts.Add($"scope={Uri.EscapeDataString(scope)}");
        if (queryParts.Count > 0) url += "?" + string.Join("&", queryParts);

        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        SetHeaders(req);
        using var resp = await _http.SendAsync(req, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        var ctx = JsonSerializer.Deserialize(json, OnboardingJsonContext.Default.EngramContextResponse);
        return ctx?.Context;
    }

    public async Task<IReadOnlyList<EngramSearchResult>> SearchAsync(
        string query,
        string? type = null,
        string? project = null,
        string? scope = null,
        int limit = 10,
        CancellationToken ct = default)
    {
        var url = $"{_remoteUrl}/search";
        var queryParts = new List<string>();
        if (!string.IsNullOrEmpty(query)) queryParts.Add($"q={Uri.EscapeDataString(query)}");
        if (!string.IsNullOrEmpty(type)) queryParts.Add($"type={Uri.EscapeDataString(type)}");
        if (!string.IsNullOrEmpty(project)) queryParts.Add($"project={Uri.EscapeDataString(project)}");
        if (!string.IsNullOrEmpty(scope)) queryParts.Add($"scope={Uri.EscapeDataString(scope)}");
        queryParts.Add($"limit={limit}");
        url += "?" + string.Join("&", queryParts);

        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        SetHeaders(req);
        using var resp = await _http.SendAsync(req, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        var searchResp = JsonSerializer.Deserialize(json, OnboardingJsonContext.Default.EngramSearchResponse);
        if (searchResp?.Results is null) return Array.Empty<EngramSearchResult>();

        return searchResp.Results.Select(r => new EngramSearchResult(
            r.Observation.Id,
            r.Observation.Type,
            r.Observation.Title,
            TruncatePreview(r.Observation.Content, 300),
            r.Observation.CreatedAt,
            r.Observation.Project,
            r.Observation.Scope,
            r.Rank
        )).ToList();
    }

    public async Task<EngramStats?> GetStatsAsync(CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, $"{_remoteUrl}/stats");
        SetHeaders(req);
        using var resp = await _http.SendAsync(req, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        var statsResp = JsonSerializer.Deserialize(json, OnboardingJsonContext.Default.EngramStatsResponse);
        if (statsResp is null) return null;

        return new EngramStats(
            statsResp.TotalSessions,
            statsResp.TotalObservations,
            statsResp.TotalPrompts,
            statsResp.Projects,
            statsResp.Backend);
    }

    public async Task<EngramObservation?> GetObservationAsync(long id, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, $"{_remoteUrl}/observations/{id}");
        SetHeaders(req);
        using var resp = await _http.SendAsync(req, ct).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode) return null;

        var json = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        var dto = JsonSerializer.Deserialize(json, OnboardingJsonContext.Default.EngramObservationDto);
        if (dto is null) return null;

        return new EngramObservation(
            dto.Id, dto.Type, dto.Title, dto.Content,
            dto.Project, dto.Scope, dto.TopicKey, dto.CreatedAt, dto.SessionId);
    }

    public async Task<string?> GetTimelineAsync(long observationId, int before = 5, int after = 5, CancellationToken ct = default)
    {
        var url = $"{_remoteUrl}/timeline?observation_id={observationId}&before={before}&after={after}";
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        SetHeaders(req);
        using var resp = await _http.SendAsync(req, ct).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode) return null;

        return await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
    }

    void SetHeaders(HttpRequestMessage req)
    {
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (!string.IsNullOrEmpty(_engramUser))
        {
            req.Headers.TryAddWithoutValidation("X-Engram-User", _engramUser);
        }
    }

    static string TruncatePreview(string content, int maxLen)
    {
        if (string.IsNullOrEmpty(content) || content.Length <= maxLen) return content;
        return content[..maxLen];
    }

    public void Dispose()
    {
        if (_ownsHttpClient)
            _http.Dispose();
    }
}
