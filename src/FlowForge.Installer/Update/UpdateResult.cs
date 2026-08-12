namespace FlowForge.Installer.Update;

/// <summary>
/// Components that can be updated independently.
/// </summary>
public enum UpdateComponent
{
    Engram,
    FlowForgeSkills,
    FlowDoc,
    /// <summary>OUT v1 (OQ-1) — reserved enum value, not implemented.</summary>
    Installer,
    All
}

/// <summary>
/// Outcome of an update attempt for a single component.
/// </summary>
public enum UpdateStatus
{
    Success,
    SkippedAlreadyLatest,
    SkippedUserChoice,
    Failed,
    RolledBack
}

/// <summary>
/// Result DTO for a single component update operation.
/// </summary>
public sealed record UpdateResult(
    UpdateComponent Component,
    string OldVersion,
    string NewVersion,
    UpdateStatus Status,
    string? ErrorMessage = null,
    string? Sha256Pre = null,
    string? Sha256Post = null
);

/// <summary>
/// Options passed to the update orchestrator.
/// </summary>
public sealed record UpdateOptions(
    UpdateComponent Component,
    bool Yes,
    bool Force,
    string? Tag,
    string? SpecificVersion
);
