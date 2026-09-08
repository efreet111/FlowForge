namespace FlowForge.Installer.Onboarding;

/// <summary>
/// Aggregated onboarding data for a project.
/// Contains recent sessions, decisions, and patterns retrieved from engram.
/// </summary>
public sealed record OnboardingData(
    string Project,
    string User,
    DateTime GeneratedAt,
    IReadOnlyList<Session> RecentSessions,
    IReadOnlyList<Observation> Decisions,
    IReadOnlyList<Observation> Patterns
);

/// <summary>
/// A recent coding session from `engram context`.
/// </summary>
public sealed record Session(
    string Title,
    string Content,
    DateTime Timestamp
);

/// <summary>
/// A single observation (decision or pattern) from `engram search`.
/// </summary>
public sealed record Observation(
    string Id,
    string Title,
    string Type,
    string Content,
    DateTime Timestamp
);

/// <summary>
/// Raw output from the three engram subprocess invocations.
/// </summary>
public sealed record EngramRawOutput(
    string ContextOutput,
    string DecisionOutput,
    string PatternOutput
);
