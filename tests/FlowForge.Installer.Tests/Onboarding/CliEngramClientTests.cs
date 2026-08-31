using System.Threading.Tasks;
using FlowForge.Installer.Onboarding;
using Xunit;

namespace FlowForge.Installer.Tests.Onboarding;

public class CliEngramClientTests
{
    [Fact] // FR-013
    public async Task HealthCheck_BinaryExists_ReturnsTrue()
    {
        // Use a file that exists on the system
        var tempFile = Path.GetTempFileName();
        try
        {
            var client = new CliEngramClient(tempFile);
            var result = await client.HealthCheckAsync();
            Assert.True(result);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact] // FR-013
    public async Task HealthCheck_BinaryMissing_ReturnsFalse()
    {
        var client = new CliEngramClient("/nonexistent/engram/binary");
        var result = await client.HealthCheckAsync();
        Assert.False(result);
    }

    [Fact] // FR-013
    public void ParseSearchResults_ValidInput_ParsesCorrectly()
    {
        var output = """
        [i] #97 (decision) — Use AOT compilation
        [i] #112 (decision) — Adopt Spectre.Console for rendering
        #32 (pattern) — EN linked to observations
        """;

        var results = CliEngramClient.ParseSearchResults(output);

        Assert.Equal(3, results.Count);
        Assert.Equal(97, results[0].Id);
        Assert.Equal("decision", results[0].Type);
        Assert.Equal("Use AOT compilation", results[0].Title);
        Assert.Equal(112, results[1].Id);
        Assert.Equal(32, results[2].Id);
        Assert.Equal("pattern", results[2].Type);
    }

    [Fact] // FR-013
    public void ParseSearchResults_EmptyOutput_ReturnsEmpty()
    {
        var results = CliEngramClient.ParseSearchResults("");
        Assert.Empty(results);
    }

    [Fact] // FR-013
    public void ParseSearchResults_NoMemoriesFound_ReturnsEmpty()
    {
        var output = "No memories found for query 'nonexistent'";
        var results = CliEngramClient.ParseSearchResults(output);
        Assert.Empty(results);
    }

    [Fact] // FR-008
    public void ParseStats_ValidInput_ParsesCorrectly()
    {
        var output = """
        Sessions: 45
        Observations: 65
        Prompts: 23
        Projects: flowforge, other
        Backend: sqlite
        """;

        var stats = CliEngramClient.ParseStats(output);

        Assert.NotNull(stats);
        Assert.Equal(45, stats!.TotalSessions);
        Assert.Equal(65, stats.TotalObservations);
        Assert.Equal(23, stats.TotalPrompts);
        Assert.Equal(2, stats.Projects.Count);
        Assert.Contains("flowforge", stats.Projects);
        Assert.Contains("other", stats.Projects);
        Assert.Equal("sqlite", stats.Backend);
    }

    [Fact] // FR-010
    public void ParseObservation_ValidInput_ParsesCorrectly()
    {
        var output = """
        ID: 97
        Type: decision
        Title: Use AOT compilation
        Project: team/flowforge
        Scope: team
        Topic: aot
        Created: 2026-08-01T10:00:00Z
        Session: 1
        Content: Full content of the decision.
        Multiple lines of content.
        """;

        var obs = CliEngramClient.ParseObservation(output, 97);

        Assert.NotNull(obs);
        Assert.Equal(97, obs!.Id);
        Assert.Equal("decision", obs.Type);
        Assert.Equal("Use AOT compilation", obs.Title);
        Assert.Equal("team/flowforge", obs.Project);
        Assert.Equal("team", obs.Scope);
        Assert.Equal("aot", obs.TopicKey);
        Assert.Contains("Full content of the decision.", obs.Content);
        Assert.Contains("Multiple lines of content.", obs.Content);
    }

    [Fact] // FR-010
    public void ParseObservation_EmptyOutput_ReturnsNull()
    {
        var obs = CliEngramClient.ParseObservation("", 1);
        Assert.Null(obs);
    }
}
