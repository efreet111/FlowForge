using System.Text.RegularExpressions;

namespace FlowForge.Installer.Onboarding;

/// <summary>
/// Pure function parser for engram CLI text output (NFR-005).
/// Tolerates: Go "Update available" banner, "Found N memories" header,
/// "No memories found" message, blank lines, and truncated content (…).
/// </summary>
public static partial class EngramOutputParser
{
    // ── Regex patterns (compiled for performance) ────────────────────────────

    // Go binary update banner
    [GeneratedRegex(@"^Update available.*$", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
    private static partial Regex UpdateBannerRegex();

    // "Please update:" line that follows the banner
    [GeneratedRegex(@"^Please update:.*$", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
    private static partial Regex PleaseUpdateRegex();

    // "Found N memories" header
    [GeneratedRegex(@"^Found \d+ memories?\.?$", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
    private static partial Regex FoundMemoriesRegex();

    // "No memories found for: ..." message
    [GeneratedRegex(@"^No memories found.*$", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
    private static partial Regex NoMemoriesRegex();

    // Session header: "Session N: "title" (YYYY-MM-DD)" or similar
    [GeneratedRegex(@"^\s*Session\s+\d+:\s*""(.+?)""\s*\((\d{4}-\d{2}-\d{2})\)", RegexOptions.IgnoreCase)]
    private static partial Regex SessionHeaderRegex();

    // Observation header (engram Go 1.20+ / .NET 1.3+): "[N] #ID (type) — title"
    // Title may be wrapped in **bold** markers.
    [GeneratedRegex(@"^\[(\d+)\]\s+#(\d+)\s+\((\w+)\)\s*[—\u2014\-]+\s*(.+)$", RegexOptions.IgnoreCase)]
    private static partial Regex ObservationHeaderRegex();

    // Alternative observation header — same format, tolerant of minor whitespace variations
    [GeneratedRegex(@"^\[\s*(\d+)\s*\]\s+#(\d+)\s+\((\w+)\)\s*[—\u2014\-]+\s*(.+)$", RegexOptions.IgnoreCase)]
    private static partial Regex ObservationAltHeaderRegex();

    // Footer line: "YYYY-MM-DD HH:MM:SS | project: NAME | scope: SCOPE"
    [GeneratedRegex(@"^\s*(\d{4}-\d{2}-\d{2}\s+\d{2}:\d{2}:\d{2})\s*\|\s*project:\s*(\S+)\s*\|\s*scope:\s*(\S+)", RegexOptions.IgnoreCase)]
    private static partial Regex FooterLineRegex();

    // Context session line: "- **project** (YYYY-MM-DD HH:MM:SS) [N observations]"
    [GeneratedRegex(@"^\s*-\s+\*\*(.+?)\*\*\s+\((\d{4}-\d{2}-\d{2}\s+\d{2}:\d{2}:\d{2})\)\s+\[(\d+)\s+observations?\]", RegexOptions.IgnoreCase)]
    private static partial Regex ContextSessionRegex();

    // Context observation line: "- [type] **title**: content..."
    [GeneratedRegex(@"^\s*-\s+\[(\w+)\]\s+\*\*(.+?)\*\*:\s*(.*)", RegexOptions.IgnoreCase)]
    private static partial Regex ContextObservationRegex();

    // Timestamp pattern for standalone dates
    [GeneratedRegex(@"(\d{4}-\d{2}-\d{2})")]
    private static partial Regex DateRegex();

    // Bold markdown stripping: **text**
    [GeneratedRegex(@"\*\*(.+?)\*\*")]
    private static partial Regex BoldMarkdownRegex();

    /// <summary>
    /// Parse `engram context &lt;project&gt;` output into sessions.
    /// Supports two formats:
    ///   Legacy:  Session N: "title" (YYYY-MM-DD) + Content: lines
    ///   Current: "## Recent Sessions" with "- **project** (datetime) [N obs]" lines
    ///            and "## Recent Observations" with "- [type] **title**: content" lines
    /// Tolerates: blank lines, "Update available" banner, truncation (…).
    /// </summary>
    public static IReadOnlyList<Session> ParseContext(string output)
    {
        if (string.IsNullOrWhiteSpace(output))
            return [];

        var cleaned = StripNoise(output);
        var lines = cleaned.Split('\n');
        var sessions = new List<Session>();

        // Try current format first (Recent Sessions / Recent Observations)
        var currentSessions = ParseContextCurrentFormat(lines);
        if (currentSessions.Count > 0)
            return currentSessions;

        // Fallback to legacy format
        return ParseContextLegacyFormat(lines);
    }

    /// <summary>
    /// Parse the current engram context format with "Recent Sessions" and "Recent Observations" sections.
    /// </summary>
    private static List<Session> ParseContextCurrentFormat(string[] lines)
    {
        var sessions = new List<Session>();
        var observationContents = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        string? currentSection = null;

        // First pass: identify sections and collect data
        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd('\r');

            // Detect section headers
            if (line.Contains("Recent Sessions", StringComparison.OrdinalIgnoreCase))
            {
                currentSection = "sessions";
                continue;
            }
            if (line.Contains("Recent Observations", StringComparison.OrdinalIgnoreCase))
            {
                currentSection = "observations";
                continue;
            }
            if (line.StartsWith("##", StringComparison.Ordinal))
            {
                currentSection = null;
                continue;
            }

            if (currentSection == "sessions")
            {
                var match = ContextSessionRegex().Match(line);
                if (match.Success)
                {
                    var title = match.Groups[1].Value.Trim();
                    var dateStr = match.Groups[2].Value.Trim();
                    var timestamp = ParseDate(dateStr) ?? DateTime.MinValue;
                    sessions.Add(new Session(title, "", timestamp));
                }
            }
            else if (currentSection == "observations")
            {
                var match = ContextObservationRegex().Match(line);
                if (match.Success)
                {
                    var title = StripBold(match.Groups[2].Value.Trim());
                    var content = match.Groups[3].Value.Trim();
                    if (!observationContents.ContainsKey(title))
                        observationContents[title] = [];
                    // Include the title as part of the content for enrichment
                    var fullContent = string.IsNullOrWhiteSpace(content) ? title : $"{title}: {content}";
                    observationContents[title].Add(fullContent);
                }
            }
        }

        // Enrich sessions with observation content if available
        if (observationContents.Count > 0 && sessions.Count > 0)
        {
            var allContent = string.Join("\n", observationContents.Values.SelectMany(v => v));
            if (!string.IsNullOrWhiteSpace(allContent))
            {
                // Attach observation content to the first session (or distribute)
                var enriched = new List<Session>();
                for (int i = 0; i < sessions.Count; i++)
                {
                    var s = sessions[i];
                    var content = i == 0 ? allContent : s.Content;
                    enriched.Add(new Session(s.Title, content, s.Timestamp));
                }
                return enriched;
            }
        }

        return sessions;
    }

    /// <summary>
    /// Parse the legacy context format: Session N: "title" (date) + Content: lines.
    /// </summary>
    private static List<Session> ParseContextLegacyFormat(string[] lines)
    {
        var sessions = new List<Session>();
        string? currentTitle = null;
        string? currentDate = null;
        var contentLines = new List<string>();

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd('\r');

            var match = SessionHeaderRegex().Match(line);
            if (match.Success)
            {
                FlushSession(sessions, currentTitle, currentDate, contentLines);
                currentTitle = match.Groups[1].Value.Trim();
                currentDate = match.Groups[2].Value.Trim();
                contentLines.Clear();
                continue;
            }

            if (line.TrimStart().StartsWith("Content:", StringComparison.OrdinalIgnoreCase))
            {
                var afterContent = line.TrimStart()["Content:".Length..].Trim();
                if (!string.IsNullOrWhiteSpace(afterContent))
                    contentLines.Add(afterContent);
                continue;
            }

            if (currentTitle != null && !string.IsNullOrWhiteSpace(line))
            {
                contentLines.Add(line.Trim());
            }
        }

        FlushSession(sessions, currentTitle, currentDate, contentLines);
        return sessions;
    }

    /// <summary>
    /// Parse `engram search ... --type &lt;type&gt;` output into observations.
    /// Supports the real engram Go 1.20+ / .NET 1.3+ format:
    ///   [N] #ID (type) — title
    ///       **What**: ...
    ///   **Why**: ...
    ///   **Where**: ...
    ///   **Learned**: ...
    ///       YYYY-MM-DD HH:MM:SS | project: NAME | scope: SCOPE
    /// Tolerates: "Found N memories:" header, "No memories found" message.
    /// </summary>
    public static IReadOnlyList<Observation> ParseSearchResults(string output, string type)
    {
        if (string.IsNullOrWhiteSpace(output))
            return [];

        // Check for "No memories found"
        if (NoMemoriesRegex().IsMatch(output))
            return [];

        var cleaned = StripNoise(output);
        var lines = cleaned.Split('\n');
        var observations = new List<Observation>();

        string? currentId = null;
        string? currentTitle = null;
        string? currentDate = null;
        var contentLines = new List<string>();

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd('\r');

            // Check for footer line first (date extraction)
            var footerMatch = FooterLineRegex().Match(line);
            if (footerMatch.Success)
            {
                currentDate = footerMatch.Groups[1].Value.Trim();
                continue;
            }

            // Try primary observation header format: [N] #ID (type) — title
            var match = ObservationHeaderRegex().Match(line);
            if (!match.Success)
            {
                // Try alternative format (same pattern, tolerant whitespace)
                match = ObservationAltHeaderRegex().Match(line);
            }

            if (match.Success)
            {
                // Flush previous observation
                FlushObservation(observations, currentId, currentTitle, type, currentDate, contentLines);

                currentId = match.Groups[2].Value.Trim();
                currentTitle = StripBold(match.Groups[4].Value.Trim());
                currentDate = null; // Will be set when footer line is encountered
                contentLines.Clear();
                continue;
            }

            // Accumulate content lines (skip blank lines)
            if (currentId != null && !string.IsNullOrWhiteSpace(line))
            {
                contentLines.Add(line.Trim());
            }
        }

        // Flush last observation
        FlushObservation(observations, currentId, currentTitle, type, currentDate, contentLines);

        return observations;
    }

    /// <summary>
    /// Strip noise lines (banners, headers) from raw engram output.
    /// </summary>
    private static string StripNoise(string output)
    {
        var result = UpdateBannerRegex().Replace(output, "");
        result = PleaseUpdateRegex().Replace(result, "");
        result = FoundMemoriesRegex().Replace(result, "");
        result = NoMemoriesRegex().Replace(result, "");
        return result;
    }

    private static void FlushSession(
        List<Session> sessions,
        string? title,
        string? dateStr,
        List<string> contentLines)
    {
        if (title == null) return;

        var timestamp = ParseDate(dateStr) ?? DateTime.MinValue;
        var content = string.Join("\n", contentLines).Trim();
        sessions.Add(new Session(title, content, timestamp));
    }

    private static void FlushObservation(
        List<Observation> observations,
        string? id,
        string? title,
        string type,
        string? dateStr,
        List<string> contentLines)
    {
        if (id == null || title == null) return;

        var timestamp = ParseDate(dateStr) ?? DateTime.MinValue;
        var content = string.Join("\n", contentLines).Trim();
        observations.Add(new Observation(id, title, type, content, timestamp));
    }

    private static DateTime? ParseDate(string? dateStr)
    {
        if (string.IsNullOrWhiteSpace(dateStr))
            return null;

        if (DateTime.TryParse(dateStr, out var dt))
            return dt;

        return null;
    }

    /// <summary>
    /// Strip **bold** markdown markers from a string.
    /// "**title**" → "title"; "plain" → "plain".
    /// Also handles edge cases where ** is not properly paired.
    /// </summary>
    private static string StripBold(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        // First pass: replace **...** with just the content
        text = BoldMarkdownRegex().Replace(text, "$1");

        // Second pass: replace any remaining orphaned ** with nothing
        // This handles edge cases like "Phase **3.2:** text" where the **
        // didn't form a proper markdown bold pair
        text = text.Replace("**", "");

        return text.Trim();
    }
}
