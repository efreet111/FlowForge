using System.Diagnostics;
using System.Text.RegularExpressions;

namespace FlowForge.Installer.Onboarding;

/// <summary>
/// CLI subprocess fallback implementation of IEngramClient.
/// Invokes the engram binary as a subprocess and parses its stable text output.
/// Used when HTTP fails or sync.mode=local.
/// </summary>
public sealed partial class CliEngramClient : IEngramClient
{
    readonly string _engramBinaryPath;
    readonly TimeSpan _timeout;

    /// <summary>
    /// Creates a new CLI engram client.
    /// </summary>
    /// <param name="engramBinaryPath">Full path to the engram binary.</param>
    /// <param name="timeout">Timeout for subprocess invocations (default 2s for local).</param>
    public CliEngramClient(string engramBinaryPath, TimeSpan? timeout = null)
    {
        _engramBinaryPath = engramBinaryPath ?? throw new ArgumentNullException(nameof(engramBinaryPath));
        _timeout = timeout ?? TimeSpan.FromSeconds(2);
    }

    public Task<bool> HealthCheckAsync(CancellationToken ct = default)
    {
        // CLI client is "healthy" if the binary exists
        var exists = File.Exists(_engramBinaryPath);
        return Task.FromResult(exists);
    }

    public async Task<string?> GetContextAsync(string? project, string? scope, CancellationToken ct = default)
    {
        var args = new List<string> { "context" };
        if (!string.IsNullOrEmpty(project)) args.Add(project);
        if (!string.IsNullOrEmpty(scope)) { args.Add("--scope"); args.Add(scope); }

        var output = await RunAsync(args, ct).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(output)) return null;
        if (output.Contains("No previous session", StringComparison.OrdinalIgnoreCase)) return null;
        return output;
    }

    public async Task<IReadOnlyList<EngramSearchResult>> SearchAsync(
        string query,
        string? type = null,
        string? project = null,
        string? scope = null,
        int limit = 10,
        CancellationToken ct = default)
    {
        var args = new List<string> { "search" };
        if (!string.IsNullOrEmpty(query)) args.Add(query);
        if (!string.IsNullOrEmpty(type)) { args.Add("--type"); args.Add(type); }
        if (!string.IsNullOrEmpty(project)) { args.Add("--project"); args.Add(project); }
        if (!string.IsNullOrEmpty(scope)) { args.Add("--scope"); args.Add(scope); }
        args.Add("--limit"); args.Add(limit.ToString());

        var output = await RunAsync(args, ct).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(output)) return Array.Empty<EngramSearchResult>();
        if (output.Contains("No memories found", StringComparison.OrdinalIgnoreCase)) return Array.Empty<EngramSearchResult>();

        return ParseSearchResults(output);
    }

    public async Task<EngramStats?> GetStatsAsync(CancellationToken ct = default)
    {
        var output = await RunAsync(["stats"], ct).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(output)) return null;

        return ParseStats(output);
    }

    public async Task<EngramObservation?> GetObservationAsync(long id, CancellationToken ct = default)
    {
        var output = await RunAsync(["get", id.ToString()], ct).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(output)) return null;

        return ParseObservation(output, id);
    }

    public async Task<string?> GetTimelineAsync(long observationId, int before = 5, int after = 5, CancellationToken ct = default)
    {
        var args = new List<string> { "timeline", observationId.ToString(), "--before", before.ToString(), "--after", after.ToString() };
        var output = await RunAsync(args, ct).ConfigureAwait(false);
        return string.IsNullOrWhiteSpace(output) ? null : output;
    }

    async Task<string> RunAsync(IReadOnlyList<string> args, CancellationToken ct)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = _engramBinaryPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };

        foreach (var arg in args)
            process.StartInfo.ArgumentList.Add(arg);

        process.Start();

        var outputTask = process.StandardOutput.ReadToEndAsync(ct);
        var errorTask = process.StandardError.ReadToEndAsync(ct);

        var exited = await WaitForExitAsync(process, _timeout, ct).ConfigureAwait(false);
        if (!exited)
        {
            try { process.Kill(entireProcessTree: true); } catch { /* best effort */ }
            throw new TimeoutException($"engram subprocess timed out after {_timeout.TotalSeconds}s");
        }

        return await outputTask.ConfigureAwait(false);
    }

    static async Task<bool> WaitForExitAsync(Process process, TimeSpan timeout, CancellationToken ct)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeout);
        try
        {
            await process.WaitForExitAsync(cts.Token).ConfigureAwait(false);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    // ── Text parsing for stable engram CLI output format ──────────────────────

    // Parses lines like: [i] #123 (decision) — Title text
    // or: #123 (decision) — Title text
    [GeneratedRegex(@"#(\d+)\s+\((\w+)\)\s*[—\-]\s*(.+)")]
    private static partial Regex SearchResultLineRegex();

    internal static IReadOnlyList<EngramSearchResult> ParseSearchResults(string output)
    {
        var results = new List<EngramSearchResult>();
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var regex = SearchResultLineRegex();

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            // Strip leading [i] marker if present
            if (line.StartsWith("[i]", StringComparison.Ordinal))
                line = line[3..].Trim();

            var match = regex.Match(line);
            if (!match.Success) continue;

            var id = long.Parse(match.Groups[1].Value);
            var type = match.Groups[2].Value;
            var title = match.Groups[3].Value.Trim();

            results.Add(new EngramSearchResult(
                Id: id,
                Type: type,
                Title: title,
                Preview: string.Empty, // CLI format doesn't include preview inline
                CreatedAt: string.Empty,
                Project: string.Empty,
                Scope: null,
                Rank: 0.0));
        }

        return results;
    }

    internal static EngramStats? ParseStats(string output)
    {
        // Parse lines like:
        // Sessions: 45
        // Observations: 65
        // Prompts: 23
        // Projects: flowforge, other
        // Backend: sqlite
        int sessions = 0, observations = 0, prompts = 0;
        var projects = new List<string>();
        string backend = "sqlite";

        foreach (var rawLine in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var line = rawLine.Trim();
            if (line.StartsWith("Sessions:", StringComparison.OrdinalIgnoreCase))
                TryParseInt(line, "Sessions:", out sessions);
            else if (line.StartsWith("Observations:", StringComparison.OrdinalIgnoreCase))
                TryParseInt(line, "Observations:", out observations);
            else if (line.StartsWith("Prompts:", StringComparison.OrdinalIgnoreCase))
                TryParseInt(line, "Prompts:", out prompts);
            else if (line.StartsWith("Projects:", StringComparison.OrdinalIgnoreCase))
            {
                var val = line["Projects:".Length..].Trim();
                projects.AddRange(val.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            }
            else if (line.StartsWith("Backend:", StringComparison.OrdinalIgnoreCase))
                backend = line["Backend:".Length..].Trim();
        }

        return new EngramStats(sessions, observations, prompts, projects, backend);
    }

    static void TryParseInt(string line, string prefix, out int value)
    {
        value = 0;
        var numStr = line[prefix.Length..].Trim();
        int.TryParse(numStr, out value);
    }

    internal static EngramObservation? ParseObservation(string output, long expectedId)
    {
        // Simple parser: look for key: value lines
        string type = "unknown", title = "", content = "", project = "", createdAt = "";
        string? scope = null, topicKey = null;
        string? sessionId = null;
        long id = expectedId;

        var lines = output.Split('\n');
        var contentLines = new List<string>();
        bool inContent = false;

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd();

            if (inContent)
            {
                contentLines.Add(line);
                continue;
            }

            if (line.StartsWith("ID:", StringComparison.OrdinalIgnoreCase))
                long.TryParse(line["ID:".Length..].Trim(), out id);
            else if (line.StartsWith("Type:", StringComparison.OrdinalIgnoreCase))
                type = line["Type:".Length..].Trim();
            else if (line.StartsWith("Title:", StringComparison.OrdinalIgnoreCase))
                title = line["Title:".Length..].Trim();
            else if (line.StartsWith("Project:", StringComparison.OrdinalIgnoreCase))
                project = line["Project:".Length..].Trim();
            else if (line.StartsWith("Scope:", StringComparison.OrdinalIgnoreCase))
                scope = line["Scope:".Length..].Trim();
            else if (line.StartsWith("Topic:", StringComparison.OrdinalIgnoreCase))
                topicKey = line["Topic:".Length..].Trim();
            else if (line.StartsWith("Created:", StringComparison.OrdinalIgnoreCase))
                createdAt = line["Created:".Length..].Trim();
            else if (line.StartsWith("Session:", StringComparison.OrdinalIgnoreCase))
            {
                var sid = line["Session:".Length..].Trim();
                if (!string.IsNullOrEmpty(sid))
                    sessionId = sid;
            }
            else if (line.StartsWith("Content:", StringComparison.OrdinalIgnoreCase))
            {
                inContent = true;
                var firstLine = line["Content:".Length..].Trim();
                if (!string.IsNullOrEmpty(firstLine))
                    contentLines.Add(firstLine);
            }
        }

        content = string.Join("\n", contentLines).Trim();
        if (string.IsNullOrEmpty(title) && string.IsNullOrEmpty(content)) return null;

        return new EngramObservation(id, type, title, content, project, scope, topicKey, createdAt, sessionId);
    }
}
