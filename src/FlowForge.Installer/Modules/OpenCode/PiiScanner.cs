using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace FlowForge.Installer.Modules.OpenCode;

public sealed class PiiScanner
{
    static readonly Regex[] Patterns =
    {
        // Real PII: absolute home paths like /home/victor/... (NOT relative ~/ paths,
        // which are generic placeholders used legitimately in docs/instructions).
        new(@"[=:\s]\s*/home/[A-Za-z0-9_.-]{3,}/", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"@local\.dev", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"OPENCODIGO_API_KEY\s*[:=]\s*[""'']?[A-Za-z0-9\-_]{20,}", RegexOptions.Compiled),
        new(@"DEEPSEEK_API_KEY\s*[:=]\s*[""'']?[A-Za-z0-9\-_]{20,}", RegexOptions.Compiled),
        new(@"MINIMAX_API_KEY\s*[:=]\s*[""'']?[A-Za-z0-9\-_]{20,}", RegexOptions.Compiled),
    };

    static readonly Regex HomePathRegex = new(@"/home/(?<user>[A-Za-z0-9_.-]{3,})/", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    static readonly HashSet<string> PlaceholderUsernames = new(StringComparer.OrdinalIgnoreCase)
    {
        "runner",
        "testuser",
        "user",
        "username",
        "example",
    };

    public (bool Clean, List<PiiHit> Hits) Scan(string input)
    {
        var hits = new List<PiiHit>();
        if (string.IsNullOrEmpty(input))
            return (true, hits);

        try
        {
            return ScanJsonValues(input);
        }
        catch (JsonException)
        {
            foreach (var pattern in Patterns)
            {
                foreach (Match match in pattern.Matches(input))
                {
                    if (!match.Success)
                        continue;

                    hits.Add(new PiiHit(pattern.ToString(), match.Value, match.Index));
                }
            }

            return (hits.Count == 0, hits);
        }
    }

    public (bool Clean, List<PiiHit> Hits) ScanJsonValues(string jsonText)
    {
        var hits = new List<PiiHit>();
        var node = JsonNode.Parse(jsonText);
        ScanJsonNode(node, hits);
        return (hits.Count == 0, hits);
    }

    public (bool Clean, List<PiiHit> Hits) ScanGenerated(string input, string home)
    {
        if (string.IsNullOrEmpty(input))
            return (true, new List<PiiHit>());

        var normalized = input;
        if (!string.IsNullOrWhiteSpace(home))
            normalized = normalized.Replace(home.TrimEnd('/'), "$HOME", StringComparison.Ordinal);

        return Scan(normalized);
    }

    public void EnsureClean(string input, string context)
    {
        var (clean, hits) = Scan(input);
        if (clean)
            return;

        throw new PiiDetectedException(context, hits);
    }

    static void ScanJsonNode(JsonNode? node, List<PiiHit> hits)
    {
        if (node is null)
            return;

        switch (node)
        {
            case JsonValue value when value.TryGetValue(out string? str) && !string.IsNullOrEmpty(str):
                ScanJsonStringValue(str, hits);
                break;
            case JsonObject obj:
                foreach (var child in obj)
                    ScanJsonNode(child.Value, hits);
                break;
            case JsonArray array:
                foreach (var child in array)
                    ScanJsonNode(child, hits);
                break;
        }
    }

    static void ScanJsonStringValue(string value, List<PiiHit> hits)
    {
        if (string.IsNullOrEmpty(value))
            return;

        foreach (var pattern in Patterns)
        {
            foreach (Match match in pattern.Matches(value))
            {
                if (!match.Success)
                    continue;

                hits.Add(new PiiHit(pattern.ToString(), match.Value, match.Index));
            }
        }

        foreach (Match match in HomePathRegex.Matches(value))
        {
            if (!match.Success)
                continue;

            var user = match.Groups["user"].Value;
            if (string.IsNullOrEmpty(user) || PlaceholderUsernames.Contains(user))
                continue;

            hits.Add(new PiiHit("JSON string home path", match.Value, match.Index));
        }
    }
}

public sealed class PiiHit
{
    public PiiHit(string pattern, string value, int index)
    {
        Pattern = pattern;
        Value = value;
        Index = index;
    }

    public string Pattern { get; }
    public string Value { get; }
    public int Index { get; }
}

public sealed class PiiDetectedException : Exception
{
    public PiiDetectedException(string context, IReadOnlyList<PiiHit> hits)
        : base(FormatMessage(context, hits))
    {
        Hits = hits;
    }

    public IReadOnlyList<PiiHit> Hits { get; }

    static string FormatMessage(string context, IReadOnlyList<PiiHit> hits)
    {
        var builder = new System.Text.StringBuilder();
        builder.Append("PII detectada en ");
        builder.Append(context);
        builder.Append(": ");
        builder.AppendJoin(" | ", hits.Select(h => $"{h.Pattern} -> \"{h.Value}\" at {h.Index}"));
        builder.Append(". Usa placeholders ($HOME / $USER) y limpia los templates.");
        return builder.ToString();
    }
}
