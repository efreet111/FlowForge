using System.Text.RegularExpressions;

namespace FlowForge.Installer.Modules.OpenCode;

public sealed class PiiScanner
{
    static readonly Regex[] Patterns =
    {
        new(@"/home/[a-z]+/", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"@local\.dev", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"\bOPENCODIGO_API_KEY\b", RegexOptions.Compiled),
        new(@"\bDEEPSEEK_API_KEY\b", RegexOptions.Compiled),
        new(@"\bMINIMAX_API_KEY\b", RegexOptions.Compiled),
    };

    public (bool Clean, List<PiiHit> Hits) Scan(string input)
    {
        var hits = new List<PiiHit>();
        if (string.IsNullOrEmpty(input))
            return (true, hits);

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
