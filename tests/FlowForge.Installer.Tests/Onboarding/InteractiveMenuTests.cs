using FlowForge.Installer.Onboarding;
using Xunit;

namespace FlowForge.Installer.Tests.Onboarding;

/// <summary>
/// Unit tests for InteractiveMenu (FR-009).
/// Note: Full interactive testing requires manual PM tests (PM-1 through PM-12).
/// These tests verify the data model and construction.
/// </summary>
public class InteractiveMenuTests
{
    [Fact]
    public void FR_009_MenuConstructsWithData()
    {
        // Given valid onboarding data
        var data = CreateSampleData();

        // When creating the menu
        var menu = new InteractiveMenu(data);

        // Then it should be created without error
        Assert.NotNull(menu);
    }

    [Fact]
    public void FR_009_MenuConstructsWithOutputPath()
    {
        var data = CreateSampleData();
        var menu = new InteractiveMenu(data, "/tmp/test.md");
        Assert.NotNull(menu);
    }

    [Fact]
    public void FR_009_MenuConstructsWithEmptyData()
    {
        // Given empty data (edge case for empty lists)
        var data = new OnboardingData(
            "empty-project", "user", DateTime.Now,
            RecentSessions: [],
            Decisions: [],
            Patterns: []
        );

        // When creating the menu
        var menu = new InteractiveMenu(data);

        // Then it should be created without error
        Assert.NotNull(menu);
    }

    [Fact]
    public void FR_009_OnboardingData_HasCorrectCounts()
    {
        var data = CreateSampleData();

        Assert.Equal(2, data.RecentSessions.Count);
        Assert.Equal(2, data.Decisions.Count);
        Assert.Single(data.Patterns);
    }

    [Fact]
    public void FR_009_OnboardingData_ProjectAndUser()
    {
        var data = CreateSampleData();

        Assert.Equal("test-project", data.Project);
        Assert.Equal("test-user", data.User);
    }

    [Fact]
    public void FR_009_Observation_HasAllFields()
    {
        var obs = new Observation("42", "Test Decision", "decision", "Full content", new DateTime(2024, 1, 15));

        Assert.Equal("42", obs.Id);
        Assert.Equal("Test Decision", obs.Title);
        Assert.Equal("decision", obs.Type);
        Assert.Equal("Full content", obs.Content);
        Assert.Equal(new DateTime(2024, 1, 15), obs.Timestamp);
    }

    [Fact]
    public void FR_009_Session_HasAllFields()
    {
        var session = new Session("Test Session", "Session content", new DateTime(2024, 1, 15));

        Assert.Equal("Test Session", session.Title);
        Assert.Equal("Session content", session.Content);
        Assert.Equal(new DateTime(2024, 1, 15), session.Timestamp);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static OnboardingData CreateSampleData()
    {
        return new OnboardingData(
            Project: "test-project",
            User: "test-user",
            GeneratedAt: new DateTime(2024, 1, 15, 10, 0, 0),
            RecentSessions:
            [
                new Session("Session One", "Content 1", new DateTime(2024, 1, 15)),
                new Session("Session Two", "Content 2", new DateTime(2024, 1, 14)),
            ],
            Decisions:
            [
                new Observation("1", "Decision A", "decision", "Content A", new DateTime(2024, 1, 15)),
                new Observation("2", "Decision B", "decision", "Content B", new DateTime(2024, 1, 14)),
            ],
            Patterns:
            [
                new Observation("1", "Pattern X", "pattern", "Content X", new DateTime(2024, 1, 15)),
            ]
        );
    }
}
