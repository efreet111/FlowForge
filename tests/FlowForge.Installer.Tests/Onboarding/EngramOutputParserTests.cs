using FlowForge.Installer.Onboarding;
using Xunit;

namespace FlowForge.Installer.Tests.Onboarding;

/// <summary>
/// Unit tests for EngramOutputParser (NFR-005).
/// Pure function parser — testable with fixture strings, no binary required.
/// Tests cover the real engram Go 1.20+ / .NET 1.3+ output format:
///   [N] #ID (type) — title
///       **What**: ...
///       YYYY-MM-DD HH:MM:SS | project: NAME | scope: SCOPE
/// </summary>
public class EngramOutputParserTests
{
    // ── ParseContext ─────────────────────────────────────────────────────────

    [Fact]
    public void FR_003_ParseContext_WithBanner_ExtractsSessions()
    {
        // Given engram context output with Go "Update available" banner (legacy format)
        var fixture = GetFixture("context-output-with-banner.txt");

        // When parsing
        var sessions = EngramOutputParser.ParseContext(fixture);

        // Then sessions are extracted, banner is stripped
        Assert.Equal(2, sessions.Count);
        Assert.Equal("Initial project setup", sessions[0].Title);
        Assert.Equal(new DateTime(2024, 1, 15), sessions[0].Timestamp);
        Assert.Contains("FlowForge methodology", sessions[0].Content);

        Assert.Equal("ADR-001 AOT compatibility", sessions[1].Title);
        Assert.Equal(new DateTime(2024, 1, 14), sessions[1].Timestamp);
    }

    [Fact]
    public void FR_003_ParseContext_CurrentFormat_ExtractsSessions()
    {
        // Given engram context output in the current format (Recent Sessions + Recent Observations)
        var input = """
            ## Memory from Previous Sessions

            ### Recent Sessions
            - **flowforge** (2026-09-08 16:05:12) [3 observations]
            - **other-project** (2026-09-07 10:00:00) [1 observation]

            ### Recent Observations
            - [pattern] **FTS5 query sanitization**: Wrapped each search term in quotes
            - [decision] **JWT auth middleware**: Replaced sessions with tokens
            """;

        // When parsing
        var sessions = EngramOutputParser.ParseContext(input);

        // Then sessions are extracted from the Recent Sessions section
        Assert.Equal(2, sessions.Count);
        Assert.Equal("flowforge", sessions[0].Title);
        Assert.Equal(new DateTime(2026, 9, 8, 16, 5, 12), sessions[0].Timestamp);
        Assert.Equal("other-project", sessions[1].Title);

        // And observation content is enriched into the first session
        Assert.Contains("FTS5 query sanitization", sessions[0].Content);
        Assert.Contains("JWT auth middleware", sessions[0].Content);
    }

    [Fact]
    public void FR_003_ParseContext_EmptyInput_ReturnsEmpty()
    {
        var sessions = EngramOutputParser.ParseContext("");
        Assert.Empty(sessions);
    }

    [Fact]
    public void FR_003_ParseContext_NullInput_ReturnsEmpty()
    {
        var sessions = EngramOutputParser.ParseContext(null!);
        Assert.Empty(sessions);
    }

    [Fact]
    public void FR_003_ParseContext_BlankLinesOnly_ReturnsEmpty()
    {
        var sessions = EngramOutputParser.ParseContext("\n\n\n");
        Assert.Empty(sessions);
    }

    [Fact]
    public void FR_003_ParseContext_BannerOnly_ReturnsEmpty()
    {
        var input = "Update available! Current version: 1.16.0 Latest version: 1.17.0\nPlease update: https://engram.dev/docs/update\n";
        var sessions = EngramOutputParser.ParseContext(input);
        Assert.Empty(sessions);
    }

    // ── ParseSearchResults ───────────────────────────────────────────────────

    [Fact]
    public void FR_003_ParseSearchResults_Decisions_ExtractsObservations()
    {
        // Given engram search output in the real format (with [N] #ID header and footer dates)
        var fixture = GetFixture("search-output-decisions.txt");

        // When parsing
        var observations = EngramOutputParser.ParseSearchResults(fixture, "decision");

        // Then observations are extracted
        Assert.Equal(3, observations.Count);

        Assert.Equal("1", observations[0].Id);
        Assert.Equal("Switched to AOT compilation", observations[0].Title);
        Assert.Equal("decision", observations[0].Type);
        Assert.Equal(new DateTime(2024, 1, 15, 10, 30, 0), observations[0].Timestamp);
        Assert.Contains("AOT", observations[0].Content);

        Assert.Equal("2", observations[1].Id);
        Assert.Equal("ConsoleAppFramework for CLI routing", observations[1].Title);
        Assert.Equal(new DateTime(2024, 1, 14, 9, 15, 0), observations[1].Timestamp);

        Assert.Equal("3", observations[2].Id);
        Assert.Equal("Spectre.Console for visual output", observations[2].Title);
        Assert.Equal(new DateTime(2024, 1, 13, 14, 45, 0), observations[2].Timestamp);
    }

    [Fact]
    public void FR_003_ParseSearchResults_GoWithBanner_StripsBannerAndParses()
    {
        // Given engram Go output WITH "Update available" banner
        var fixture = GetFixture("search-output-go-with-banner.txt");

        // When parsing
        var observations = EngramOutputParser.ParseSearchResults(fixture, "decision");

        // Then banner is stripped and observations are parsed
        Assert.Equal(2, observations.Count);

        Assert.Equal("3767", observations[0].Id);
        Assert.Equal("ADR-001: Memory Curation Protocol", observations[0].Title); // **bold** stripped
        Assert.Equal(new DateTime(2026, 9, 8, 16, 27, 50), observations[0].Timestamp);
        Assert.Contains("memory curation protocol", observations[0].Content, StringComparison.OrdinalIgnoreCase);

        Assert.Equal("3768", observations[1].Id);
        Assert.Equal("FTS5 query sanitization", observations[1].Title);
        Assert.Equal(new DateTime(2026, 9, 8, 15, 10, 22), observations[1].Timestamp);
    }

    [Fact]
    public void FR_003_ParseSearchResults_GoNoBanner_ParsesCorrectly()
    {
        // Given engram Go output WITHOUT banner
        var fixture = GetFixture("search-output-go-no-banner.txt");

        // When parsing
        var observations = EngramOutputParser.ParseSearchResults(fixture, "pattern");

        // Then observations are parsed correctly
        Assert.Equal(2, observations.Count);
        Assert.Equal("3767", observations[0].Id);
        Assert.Equal("ADR-001: Memory Curation Protocol", observations[0].Title);
        Assert.Equal("3768", observations[1].Id);
        Assert.Equal("FTS5 query sanitization", observations[1].Title);
    }

    [Fact]
    public void FR_003_ParseSearchResults_DotNet_ParsesCorrectly()
    {
        // Given engram .NET output
        var fixture = GetFixture("search-output-dotnet.txt");

        // When parsing
        var observations = EngramOutputParser.ParseSearchResults(fixture, "decision");

        // Then observations are parsed correctly
        Assert.Equal(2, observations.Count);

        Assert.Equal("101", observations[0].Id);
        Assert.Equal("JWT auth middleware", observations[0].Title);
        Assert.Equal(new DateTime(2026, 9, 7, 11, 0, 0), observations[0].Timestamp);
        Assert.Contains("jsonwebtoken", observations[0].Content);

        Assert.Equal("102", observations[1].Id);
        Assert.Equal("Database connection pooling", observations[1].Title);
        Assert.Equal(new DateTime(2026, 9, 6, 9, 30, 0), observations[1].Timestamp);
    }

    [Fact]
    public void FR_003_ParseSearchResults_BoldTitle_StripsMarkdown()
    {
        // Given output with **bold** title
        var input = """
            Found 1 memories:

            [1] #42 (decision) — **My Bold Title**
                **What**: Something
                **Why**: Because
            2026-01-01 12:00:00 | project: test | scope: project
            """;

        // When parsing
        var observations = EngramOutputParser.ParseSearchResults(input, "decision");

        // Then bold markers are stripped from title
        Assert.Single(observations);
        Assert.Equal("My Bold Title", observations[0].Title);
    }

    [Fact]
    public void FR_003_ParseSearchResults_NoMemories_ReturnsEmpty()
    {
        // Given "No memories found" output
        var fixture = GetFixture("search-output-empty.txt");

        // When parsing
        var observations = EngramOutputParser.ParseSearchResults(fixture, "decision");

        // Then empty list
        Assert.Empty(observations);
    }

    [Fact]
    public void FR_003_ParseSearchResults_EmptyInput_ReturnsEmpty()
    {
        var observations = EngramOutputParser.ParseSearchResults("", "pattern");
        Assert.Empty(observations);
    }

    [Fact]
    public void FR_003_ParseSearchResults_NullInput_ReturnsEmpty()
    {
        var observations = EngramOutputParser.ParseSearchResults(null!, "pattern");
        Assert.Empty(observations);
    }

    [Fact]
    public void FR_003_ParseSearchResults_BannerTolerated()
    {
        // Given output with Go banner prepended (real format)
        var input = """
            Update available: 1.16.0 -> 1.20.0
            Please update: https://engram.dev/docs/update

            Found 1 memories:

            [1] #1 (decision) — Test decision
                **What**: Test content
            2024-01-15 10:00:00 | project: test | scope: project
            """;

        var observations = EngramOutputParser.ParseSearchResults(input, "decision");

        Assert.Single(observations);
        Assert.Equal("Test decision", observations[0].Title);
        Assert.Equal(new DateTime(2024, 1, 15, 10, 0, 0), observations[0].Timestamp);
    }

    [Fact]
    public void FR_003_ParseSearchResults_TypeParameterPreserved()
    {
        var input = """
            [1] #1 (pattern) — Test pattern
                **What**: Something
            2024-01-15 10:00:00 | project: test | scope: project
            """;

        var observations = EngramOutputParser.ParseSearchResults(input, "pattern");

        Assert.Single(observations);
        Assert.Equal("pattern", observations[0].Type);
    }

    [Fact]
    public void FR_003_ParseSearchResults_ContentFields_AccumulatedCorrectly()
    {
        // Given output with **What**, **Why**, **Where**, **Learned** fields
        var input = """
            [1] #1 (decision) — Test
                **What**: What content
                **Why**: Why content
                **Where**: Where content
                **Learned**: Learned content
            2024-01-15 10:00:00 | project: test | scope: project
            """;

        var observations = EngramOutputParser.ParseSearchResults(input, "decision");

        Assert.Single(observations);
        Assert.Contains("**What**: What content", observations[0].Content);
        Assert.Contains("**Why**: Why content", observations[0].Content);
        Assert.Contains("**Where**: Where content", observations[0].Content);
        Assert.Contains("**Learned**: Learned content", observations[0].Content);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static string GetFixture(string name)
    {
        // Try multiple paths to find the fixture
        var candidates = new[]
        {
            Path.Combine("Onboarding", "Fixtures", name),
            Path.Combine("tests", "FlowForge.Installer.Tests", "Onboarding", "Fixtures", name),
        };

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
                return File.ReadAllText(candidate);
        }

        // Try from the test assembly directory
        var assemblyDir = AppDomain.CurrentDomain.BaseDirectory;
        var fullPath = Path.Combine(assemblyDir, "..", "..", "..", "Onboarding", "Fixtures", name);
        if (File.Exists(fullPath))
            return File.ReadAllText(fullPath);

        throw new FileNotFoundException($"Fixture not found: {name}. Searched in: {string.Join(", ", candidates)}");
    }
}
