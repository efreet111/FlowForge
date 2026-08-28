# Plan: Onboarding Flow (`flowforge onboard`)

- Feature slug: `ff-003-onboarding-flow`
- Spec: `.ai-work/ff-003-onboarding-flow/spec.md`
- Status: complete (forge-dev, Phase 3 done)
- Generated: 2026-08-18

---

## 1. Impact and dependencies

### 1.1 Components changed

| Component | Change type | Rationale |
|-----------|-------------|-----------|
| `src/FlowForge.Installer/Program.cs` | MODIFY | Register `onboard` command + add to `knownCommands` |
| `src/FlowForge.Installer/Commands/OnboardCommand.cs` | NEW | Main command implementation |
| `src/FlowForge.Installer/Onboarding/` | NEW (directory) | Onboarding-specific services |
| `src/FlowForge.Installer/Onboarding/IEngramClient.cs` | NEW | Abstraction for memory retrieval |
| `src/FlowForge.Installer/Onboarding/HttpEngramClient.cs` | NEW | HTTP API implementation (primary) |
| `src/FlowForge.Installer/Onboarding/CliEngramClient.cs` | NEW | CLI subprocess fallback |
| `src/FlowForge.Installer/Onboarding/EngramModels.cs` | NEW | DTOs for HTTP responses (AOT-safe) |
| `src/FlowForge.Installer/Onboarding/ProjectResolver.cs` | NEW | Project detection + namespacing |
| `src/FlowForge.Installer/Onboarding/BriefingAggregator.cs` | NEW | Orchestrates mem_context/search/stats |
| `src/FlowForge.Installer/Onboarding/BriefingRenderer.cs` | NEW | Spectre.Console rendering |
| `src/FlowForge.Installer/Onboarding/MarkdownExporter.cs` | NEW | ONBOARDING.md atomic write |

### 1.2 Dependencies

| Dependency | Type | Source | AOT-safe? |
|------------|------|--------|-----------|
| `System.Net.Http.Json` | BCL | Already used | ✅ |
| `System.Text.Json` (source-gen) | BCL | Already used | ✅ |
| `System.Diagnostics.Process` | BCL | Already used (EngramProcessChecker) | ✅ |
| `Spectre.Console` | NuGet | Already used | ✅ |
| `ConsoleAppFramework` | NuGet | Already used | ✅ |
| **No new packages** | — | — | — |

### 1.3 Reused patterns (from context-map.md §8)

| Pattern | Source file | Reused for |
|---------|-------------|------------|
| DoctorCommand check-list | `Commands/DoctorCommand.cs` | Pre-flight checks (FR-002) |
| ConfigStore atomic read | `Infrastructure/ConfigStore.cs` | Read sync config (FR-013, FR-014) |
| EngramProcessChecker Process | `Update/EngramProcessChecker.cs` | CLI subprocess invocation (FR-013) |
| InstallerJsonContext source-gen | `Models/InstallerConfig.cs` | HTTP response deserialization (NFR-002) |
| Atomic write (temp+rename) | `ConfigStore.Save()` | ONBOARDING.md export (FR-011) |
| CAF command registration | `Program.cs` | Register `onboard` (FR-001) |

---

## 2. File changes (Proposed Changes)

### 2.1 New files

- **[NEW]** `src/FlowForge.Installer/Commands/OnboardCommand.cs` — Main command with pre-checks, project resolution, briefing orchestration, interactive drill-down, and export. Follows `DoctorCommand` pattern for exit codes (0/1/2).

- **[NEW]** `src/FlowForge.Installer/Onboarding/IEngramClient.cs` — Interface abstracting memory retrieval. Methods: `GetContextAsync(project, scope)`, `SearchAsync(query, type, project, scope, limit)`, `GetStatsAsync()`, `GetObservationAsync(id)`, `GetTimelineAsync(id, before, after)`, `HealthCheckAsync()`.

- **[NEW]** `src/FlowForge.Installer/Onboarding/HttpEngramClient.cs` — HTTP API implementation using `HttpClient` + source-gen JSON. Calls `/context`, `/search`, `/stats`, `/observations/{id}`, `/timeline`, `/health`. Sets `X-Engram-User` header from config.

- **[NEW]** `src/FlowForge.Installer/Onboarding/CliEngramClient.cs` — CLI subprocess fallback using `System.Diagnostics.Process`. Invokes `engram context/search/stats` and parses stable text format. Used when HTTP fails or `sync.mode=local`.

- **[NEW]** `src/FlowForge.Installer/Onboarding/EngramModels.cs` — DTOs for HTTP responses: `EngramContextResponse`, `EngramSearchResponse`, `EngramStatsResponse`, `EngramObservation`, `EngramTimelineResponse`. All registered in `OnboardingJsonContext` (source-gen).

- **[NEW]** `src/FlowForge.Installer/Onboarding/ProjectResolver.cs` — Resolves project name via `.flowforge.json` → `engram.project`, with `--project` override. Applies namespacing: `team/{project}` for team scope, `{user}/{project}` for personal. Handles ambiguity (returns `available_projects` list).

- **[NEW]** `src/FlowForge.Installer/Onboarding/BriefingAggregator.cs` — Orchestrates `IEngramClient` calls: `mem_context` (recent activity), `mem_search(type=decision)` (top N decisions), `mem_search(type=pattern)` (conventions/patterns), keyword search for blockers (`type=bugfix/manual`), `mem_stats` (header). Returns structured `BriefingData` record.

- **[NEW]** `src/FlowForge.Installer/Onboarding/BriefingRenderer.cs` — Renders `BriefingData` using Spectre.Console: stats header, recent activity table, decisions list, patterns list, blockers list. Escapes memory text (`Markup.Escape`) to prevent injection (NFR-004). Supports interactive drill-down with `SelectionPrompt`.

- **[NEW]** `src/FlowForge.Installer/Onboarding/MarkdownExporter.cs` — Writes `ONBOARDING.md` atomically (temp file + rename). Includes generated timestamp header. Filters to team scope only (NFR-005). Warns if `--scope personal` combined with `--output`.

### 2.2 Modified files

- **[MODIFY]** `src/FlowForge.Installer/Program.cs` — Add `app.Add<OnboardCommand>("onboard");` after line 81. Add `"onboard"` to `knownCommands` array at line 90.

- **[MODIFY]** `src/FlowForge.Installer/Models/InstallerConfig.cs` — Add `OnboardingJsonContext` source-gen context for onboarding DTOs (or create in `EngramModels.cs`).

---

## 3. Contracts and schemas

### 3.1 IEngramClient interface

```csharp
namespace FlowForge.Installer.Onboarding;

/// <summary>
/// Abstraction for engram memory retrieval. Two implementations:
/// - HttpEngramClient: HTTP API (primary, JSON structured)
/// - CliEngramClient: CLI subprocess (fallback, text parsing)
/// </summary>
public interface IEngramClient
{
    /// <summary>Check if the client is available (server reachable / binary exists).</summary>
    Task<bool> HealthCheckAsync(CancellationToken ct = default);

    /// <summary>Get recent context (sessions + observations + prompts).</summary>
    /// <returns>Markdown-formatted context string, or null if no data.</returns>
    Task<string?> GetContextAsync(string? project, string? scope, CancellationToken ct = default);

    /// <summary>Search memories by query and type.</summary>
    /// <returns>List of search results (id, type, title, preview, createdAt, project).</returns>
    Task<IReadOnlyList<EngramSearchResult>> SearchAsync(
        string query,
        string? type = null,
        string? project = null,
        string? scope = null,
        int limit = 10,
        CancellationToken ct = default);

    /// <summary>Get project stats (sessions, observations, prompts, projects).</summary>
    Task<EngramStats?> GetStatsAsync(CancellationToken ct = default);

    /// <summary>Get full observation content by ID.</summary>
    Task<EngramObservation?> GetObservationAsync(long id, CancellationToken ct = default);

    /// <summary>Get timeline around an observation (drill-down).</summary>
    Task<string?> GetTimelineAsync(long observationId, int before = 5, int after = 5, CancellationToken ct = default);
}
```

### 3.2 DTO models (AOT-safe, source-gen)

```csharp
// src/FlowForge.Installer/Onboarding/EngramModels.cs

public sealed record EngramSearchResult(
    long Id,
    string Type,
    string Title,
    string Preview,      // truncated to 300 chars
    string CreatedAt,
    string Project,
    string? Scope,
    double Rank);

public sealed record EngramStats(
    int TotalSessions,
    int TotalObservations,
    int TotalPrompts,
    IReadOnlyList<string> Projects,
    string Backend);

public sealed record EngramObservation(
    long Id,
    string Type,
    string Title,
    string Content,      // full content
    string Project,
    string? Scope,
    string? TopicKey,
    string CreatedAt,
    long? SessionId);

// HTTP response wrappers (match EngramServer JSON shape)
public sealed record EngramContextResponse(string Context);

public sealed record EngramSearchResponse(IReadOnlyList<EngramSearchResultItem> Results);
public sealed record EngramSearchResultItem(EngramObservationDto Observation, double Rank);
public sealed record EngramObservationDto(
    long Id, string Type, string Title, string Content,
    string Project, string? Scope, string? TopicKey, string CreatedAt, long? SessionId);

public sealed record EngramStatsResponse(
    int TotalSessions, int TotalObservations, int TotalPrompts,
    IReadOnlyList<string> Projects, string Backend);

// Source-gen context
[JsonSerializable(typeof(EngramContextResponse))]
[JsonSerializable(typeof(EngramSearchResponse))]
[JsonSerializable(typeof(EngramSearchResultItem))]
[JsonSerializable(typeof(EngramObservationDto))]
[JsonSerializable(typeof(EngramStatsResponse))]
[JsonSerializable(typeof(IReadOnlyList<EngramSearchResultItem>))]
public partial class OnboardingJsonContext : JsonSerializerContext { }
```

### 3.3 BriefingData record (internal model)

```csharp
// src/FlowForge.Installer/Onboarding/BriefingAggregator.cs

public sealed record BriefingData(
    string Project,
    string? Scope,
    EngramStats? Stats,
    string? RecentActivity,           // markdown from mem_context
    IReadOnlyList<EngramSearchResult> Decisions,
    IReadOnlyList<EngramSearchResult> Patterns,
    IReadOnlyList<EngramSearchResult> Blockers,
    bool HasData);                    // false if all sections empty
```

### 3.4 OnboardCommand signature

```csharp
// src/FlowForge.Installer/Commands/OnboardCommand.cs

public sealed class OnboardCommand(InstallerContext ctx)
{
    [Command("")]
    public async Task<int> RunAsync(
        string? project = null,         // --project <name>
        string? user = null,            // --user <handle> (display-only)
        string? scope = null,           // --scope team|personal (default: team for export)
        string? output = null,          // --output <path> (ONBOARDING.md)
        int limit = 10,                 // --limit <n> (clamped 1..20)
        bool noInteractive = false)     // --no-interactive (CI-safe)
    {
        // Returns: 0 success, 1 runtime error, 2 pre-check failure
    }
}
```

### 3.5 ProjectResolver contract

```csharp
// src/FlowForge.Installer/Onboarding/ProjectResolver.cs

public sealed record ProjectResolution(
    string ProjectName,          // e.g. "flowforge"
    string NamespacedProject,    // e.g. "team/flowforge"
    string? Source,              // "flowforge.json" | "git" | "cli-arg"
    bool IsAmbiguous,
    IReadOnlyList<string>? AvailableProjects);

public static class ProjectResolver
{
    public static ProjectResolution Resolve(
        string? cliProjectOverride,
        string? scope,
        string? user,
        string workingDir);
}
```

---

## 4. Implementation checklist

### Phase 1: Foundation (DTOs + interfaces) — no dependencies

- [x] **T-001** [S] Create `EngramModels.cs` with all DTOs and `OnboardingJsonContext` source-gen.
  - File: `src/FlowForge.Installer/Onboarding/EngramModels.cs`
  - Verify: compiles with `PublishAot=true`, no reflection warnings.
  - Trace: NFR-002 (AOT compatibility).

- [x] **T-002** [S] Create `IEngramClient.cs` interface.
  - File: `src/FlowForge.Installer/Onboarding/IEngramClient.cs`
  - Verify: interface compiles, all methods return `Task<T>`.
  - Trace: FR-013 (integration path abstraction).

### Phase 2: HTTP client (primary path) — depends on T-001, T-002

- [x] **T-003** [M] Implement `HttpEngramClient.cs`.
  - File: `src/FlowForge.Installer/Onboarding/HttpEngramClient.cs`
  - Constructor: `HttpEngramClient(HttpClient http, string remoteUrl, string engramUser)`.
  - Methods: call `/context`, `/search`, `/stats`, `/observations/{id}`, `/timeline`, `/health`.
  - Set `X-Engram-User` header on all requests.
  - Deserialize with `OnboardingJsonContext` (source-gen).
  - Handle timeouts (configurable via `FLOWFORGE_API_TIMEOUT_SECONDS`).
  - Verify: unit test with mock `HttpMessageHandler`.
  - Trace: FR-013 (Scenario A), NFR-001 (performance < 5s), NFR-002 (AOT).

- [x] **T-004** [S] Add unit tests for `HttpEngramClient`.
  - File: `tests/FlowForge.Installer.Tests/Onboarding/HttpEngramClientTests.cs`
  - Test cases: health check success/failure, search with type filter, stats parsing, timeout handling.
  - Trace: FR-013, NFR-001.

### Phase 3: CLI fallback — depends on T-001, T-002

- [x] **T-005** [M] Implement `CliEngramClient.cs`.
  - File: `src/FlowForge.Installer/Onboarding/CliEngramClient.cs`
  - Constructor: `CliEngramClient(string engramBinaryPath)`.
  - Use `System.Diagnostics.Process` with `RedirectStandardOutput = true`.
  - Parse stable text format: `[i] #id (type) — title` for search results.
  - Invoke `engram context [project] --scope`, `engram search <query> --type --project --scope --limit`, `engram stats`.
  - Verify: unit test with mocked Process (or integration test if engram binary available).
  - Trace: FR-013 (Scenario B), NFR-001 (performance < 2s local).

- [x] **T-006** [S] Add unit tests for `CliEngramClient`.
  - File: `tests/FlowForge.Installer.Tests/Onboarding/CliEngramClientTests.cs`
  - Test cases: parse search result text, handle empty output, timeout handling.
  - Trace: FR-013.

### Phase 4: Project resolution — depends on T-001

- [x] **T-007** [S] Implement `ProjectResolver.cs`.
  - File: `src/FlowForge.Installer/Onboarding/ProjectResolver.cs`
  - Logic: read `.flowforge.json` → `engram.project`, apply `--project` override, namespace to `team/{project}` or `{user}/{project}`.
  - Return `ProjectResolution` with `IsAmbiguous` flag if multiple projects detected (future: integrate with `mem_current_project` if needed).
  - Verify: unit test with mock `.flowforge.json`.
  - Trace: FR-003 (project detection), FR-015 (namespacing).

- [x] **T-008** [S] Add unit tests for `ProjectResolver`.
  - File: `tests/FlowForge.Installer.Tests/Onboarding/ProjectResolverTests.cs`
  - Test cases: CLI override, flowforge.json fallback, team namespacing, personal namespacing.
  - Trace: FR-003, FR-015.

### Phase 5: Briefing aggregation — depends on T-002, T-007

- [x] **T-009** [M] Implement `BriefingAggregator.cs`.
  - File: `src/FlowForge.Installer/Onboarding/BriefingAggregator.cs`
  - Constructor: `BriefingAggregator(IEngramClient client)`.
  - Method: `Task<BriefingData> AggregateAsync(string project, string? scope, int limit, CancellationToken ct)`.
  - Calls: `GetContextAsync(project, scope)`, `SearchAsync("", type="decision", project, scope, limit)`, `SearchAsync("convention OR naming OR style OR workflow", type="pattern", project, scope, limit)`, `SearchAsync("blocker OR gotcha OR issue OR bug OR workaround", type="bugfix|manual", project, scope, limit)`, `GetStatsAsync()`.
  - Set `HasData = false` if all sections empty (triggers FR-012 empty-memory path).
  - Verify: unit test with mock `IEngramClient`.
  - Trace: FR-004 (mem_context), FR-005 (decisions), FR-006 (patterns), FR-007 (blockers), FR-008 (stats), FR-012 (empty-memory).

- [x] **T-010** [S] Add unit tests for `BriefingAggregator`.
  - File: `tests/FlowForge.Installer.Tests/Onboarding/BriefingAggregatorTests.cs`
  - Test cases: all sections populated, some sections empty, all sections empty (HasData=false), limit clamping.
  - Trace: FR-004 to FR-008, FR-012.

### Phase 6: Rendering + export — depends on T-009

- [x] **T-011** [L] Implement `BriefingRenderer.cs`.
  - File: `src/FlowForge.Installer/Onboarding/BriefingRenderer.cs`
  - Method: `void Render(BriefingData data, string? displayUser)`.
  - Sections: stats header (if available), recent activity (markdown from `mem_context`), key decisions (table with id/type/title/preview), conventions/patterns (table), blockers (table).
  - Escape all memory text with `Markup.Escape()` before rendering (NFR-004).
  - Truncate previews to 300 chars (already done in DTO, but verify).
  - Interactive drill-down: prompt "type a number to drill down, Enter to exit", call `GetObservationAsync(id)` + optional `GetTimelineAsync(id)`.
  - If `--no-interactive`, skip drill-down prompt.
  - Verify: manual test (PM-1, PM-4).
  - Trace: FR-009 (interactive briefing), FR-010 (drill-down), NFR-004 (security: escape markup).

- [x] **T-012** [S] Implement `MarkdownExporter.cs`.
  - File: `src/FlowForge.Installer/Onboarding/MarkdownExporter.cs`
  - Method: `Task ExportAsync(BriefingData data, string outputPath, string? displayUser, CancellationToken ct)`.
  - Write atomically: temp file + rename (follow `ConfigStore.Save()` pattern).
  - Include header: `> Generated: <UTC timestamp>`.
  - Filter to team scope only (warn if `--scope personal` combined with `--output`).
  - Sections: Recent Activity, Key Decisions, Conventions/Patterns, Next Steps.
  - Verify: manual test (PM-4), unit test for atomic write.
  - Trace: FR-011 (export), NFR-005 (privacy: team scope default), NFR-006 (atomic write).

### Phase 7: Command integration — depends on all above

- [x] **T-013** [M] Implement `OnboardCommand.cs`.
  - File: `src/FlowForge.Installer/Commands/OnboardCommand.cs`
  - Follow `DoctorCommand` pattern for structure.
  - Pre-flight checks (FR-002):
    1. engram binary present (`PathHelper.EngramBinary` exists) → exit 2 with hint if missing.
    2. config readable (`ConfigStore.Load()` succeeds, `sync.user` or `ENGRAM_USER` resolvable) → exit 2 with hint if missing.
    3. (optional) server reachable (`IEngramClient.HealthCheckAsync()`) → fallback to CLI if unreachable.
  - Resolve project via `ProjectResolver.Resolve(...)`.
  - If ambiguous and interactive, prompt for selection; if `--no-interactive`, exit 1.
  - Select client: HTTP if `sync.mode=sync` and health check passes, else CLI fallback.
  - Resolve user identity: `sync.user` → `ENGRAM_USER` → `Environment.UserName` (FR-014).
  - Aggregate briefing via `BriefingAggregator`.
  - If `!HasData`, print empty-memory guidance (FR-012) and exit 0.
  - Render briefing via `BriefingRenderer`.
  - If `--output`, export via `MarkdownExporter`.
  - Return exit code 0 on success, 1 on runtime error, 2 on pre-check failure.
  - Verify: manual tests (PM-1 to PM-4).
  - Trace: FR-001 to FR-015, NFR-007 (exit codes).

- [x] **T-014** [S] Register `onboard` command in `Program.cs`.
  - File: `src/FlowForge.Installer/Program.cs`
  - Add `app.Add<OnboardCommand>("onboard");` after line 81.
  - Add `"onboard"` to `knownCommands` array at line 90.
  - Verify: `flowforge onboard --help` shows usage, `flowforge --help` lists `onboard`.
  - Trace: FR-001 (command registration), NFR-003 (ADR-017: composition, not replacement).

### Phase 8: Testing + validation — depends on all above

- [x] **T-015** [M] Write integration tests for end-to-end flow.
  - File: `tests/FlowForge.Installer.Tests/Onboarding/OnboardCommandIntegrationTests.cs`
  - Test cases:
    - Happy path with mock HTTP server (all sections populated).
    - Pre-check failure (engram binary missing) → exit 2.
    - Empty memory path → guidance message.
    - Ambiguous project + interactive → prompt.
    - Ambiguous project + `--no-interactive` → exit 1.
    - Export to ONBOARDING.md → atomic write, team scope only.
  - Trace: PM-1 to PM-4 (manual test coverage).

- [x] **T-016** [S] Update `installer-baseline.md` (ADR-017 compliance).
  - File: `docs/installer-baseline.md` (or create if not exists).
  - Document: `flowforge onboard` command, all flags (`--project`, `--user`, `--scope`, `--output`, `--limit`, `--no-interactive`), exit codes (0/1/2), side effects (none — read-only).
  - Verify: baseline matches implementation.
  - Trace: NFR-003 (ADR-017).

- [x] **T-017** [S] Run regression tests for existing commands.
  - Commands: `flowforge install --yes`, `flowforge status`, `flowforge doctor`, `flowforge uninstall`.
  - Verify: all commands still work after adding `onboard`.
  - Trace: NFR-003 (ADR-017: no regression).

---

## 5. Implementation order (dependency graph)

```
T-001 (DTOs) ──────────────┐
                            ├─→ T-003 (HTTP client) ──→ T-004 (HTTP tests)
T-002 (IEngramClient) ─────┤
                            ├─→ T-005 (CLI client) ───→ T-006 (CLI tests)
                            │
                            └─→ T-007 (ProjectResolver) → T-008 (Resolver tests)
                                    
T-002 + T-007 ────────────────→ T-009 (BriefingAggregator) → T-010 (Aggregator tests)

T-009 ─────────────────────────→ T-011 (BriefingRenderer)
                            ├─→ T-012 (MarkdownExporter)

T-003 + T-005 + T-009 + T-011 + T-012 → T-013 (OnboardCommand)

T-013 ─────────────────────────→ T-014 (Program.cs registration)

T-014 ─────────────────────────→ T-015 (Integration tests)
                            ├─→ T-016 (installer-baseline.md)
                            └─→ T-017 (Regression tests)
```

**Critical path**: T-001 → T-002 → T-003 → T-009 → T-011 → T-013 → T-014 → T-015

---

## 6. Effort estimates

| Task | Effort | Rationale |
|------|--------|-----------|
| T-001 (DTOs) | S | Straightforward records + source-gen context |
| T-002 (IEngramClient) | S | Interface definition only |
| T-003 (HttpEngramClient) | M | HTTP calls + JSON parsing + error handling |
| T-004 (HTTP tests) | S | Mock HttpMessageHandler, standard patterns |
| T-005 (CliEngramClient) | M | Process invocation + text parsing |
| T-006 (CLI tests) | S | Mock Process or integration test |
| T-007 (ProjectResolver) | S | Simple resolution logic |
| T-008 (Resolver tests) | S | Unit tests with mock files |
| T-009 (BriefingAggregator) | M | Orchestration of multiple client calls |
| T-010 (Aggregator tests) | S | Mock IEngramClient |
| T-011 (BriefingRenderer) | L | Spectre rendering + interactive drill-down |
| T-012 (MarkdownExporter) | S | Atomic write pattern exists |
| T-013 (OnboardCommand) | M | Integration of all components + pre-checks |
| T-014 (Program.cs) | S | One-liner registration |
| T-015 (Integration tests) | M | End-to-end test scenarios |
| T-016 (installer-baseline.md) | S | Documentation update |
| T-017 (Regression tests) | S | Run existing tests |

**Total effort**: 9S + 6M + 1L + 1S = **10S + 6M + 1L**

**Estimated duration**: 3-4 days (assuming 1 developer, 6h/day effective coding time).

---

## 7. Test strategy

### 7.1 Unit tests

| Component | Test file | Coverage target |
|-----------|-----------|-----------------|
| `EngramModels` | `EngramModelsTests.cs` | JSON serialization/deserialization round-trip |
| `HttpEngramClient` | `HttpEngramClientTests.cs` | All methods with mock HTTP, timeout handling |
| `CliEngramClient` | `CliEngramClientTests.cs` | Text parsing, empty output, timeout |
| `ProjectResolver` | `ProjectResolverTests.cs` | CLI override, flowforge.json, namespacing |
| `BriefingAggregator` | `BriefingAggregatorTests.cs` | All sections, empty data, limit clamping |
| `MarkdownExporter` | `MarkdownExporterTests.cs` | Atomic write, team scope filter |

### 7.2 Integration tests

| Scenario | Test file | Trace |
|----------|-----------|-------|
| Happy path (team sync) | `OnboardCommandIntegrationTests.cs` | PM-1 |
| Pre-check failure (no engram) | `OnboardCommandIntegrationTests.cs` | PM-2 |
| Ambiguous / no memories | `OnboardCommandIntegrationTests.cs` | PM-3 |
| Export + drill-down | `OnboardCommandIntegrationTests.cs` | PM-4 |

### 7.3 Manual tests (from spec.md §4)

| ID | Case | Steps | Expected | [x] |
|----|------|-------|----------|-----|
| PM-1 | Happy path (team sync) | `flowforge onboard --project flowforge` in FlowForge repo | Briefing shows Recent Activity, Key Decisions, Conventions/Patterns from `team/flowforge`; exit 0 | [ ] |
| PM-2 | Error path (no engram) | Rename `~/.local/bin/engram`, run `flowforge onboard`, restore | Pre-check "engram binary ✗ FAIL" with hint; exit 2 | [ ] |
| PM-3 | Edge case (ambiguous/no memories) | `cd` into dir with ≥2 child git repos or empty project | Ambiguity prompt or empty-memory guidance; no crash; exit 0/1 | [ ] |
| PM-4 | Export + drill-down | `flowforge onboard --output ONBOARDING.md --project flowforge`, then drill-down | `ONBOARDING.md` written atomically with timestamp; drill-down shows full content | [ ] |

---

## 8. Risk mitigations

| Risk | Mitigation (from context-map.md §10) | Task trace |
|------|--------------------------------------|------------|
| **R1**: Duplication with ENG-485 | FF-003 owns CLI orchestration + detection + rendering; delegates aggregation to engram primitives. No ranking logic in installer. | T-009 (aggregator calls engram, no ranking) |
| **R2**: `mem_timeline` misuse | `mem_timeline` used ONLY for drill-down (FR-010). Recent activity uses `mem_context` (FR-004). | T-009 (aggregator), T-011 (renderer drill-down) |
| **R3**: `type:convention` nonexistent | Search uses `type=pattern` + keywords ("convention", "naming", "style", "workflow"). Unit test guards against `type=convention`. | T-009 (aggregator), T-010 (tests) |
| **R4**: Namespacing `team/{project}` | `ProjectResolver` applies namespace based on scope. Unit tests verify `team/flowforge` resolution. | T-007 (resolver), T-008 (tests) |
| **R5**: CLI without `--json` | HTTP-first path (structured JSON). CLI fallback parses stable text format. Integration tests cover both paths. | T-003 (HTTP), T-005 (CLI), T-015 (integration) |
| **R6**: No memories (new team) | `BriefingAggregator.HasData = false` triggers FR-012 empty-memory guidance. | T-009 (aggregator), T-013 (command) |
| **R7**: Server not running | Health check fails → fallback to CLI subprocess. Print "usando memorias locales". | T-003 (HTTP), T-005 (CLI), T-013 (command) |
| **R8**: Too many memories | `--limit` clamped to 1..20 (default 10). Briefing shows top N only. | T-009 (aggregator), T-013 (command) |
| **R9**: AOT incompatibility | Only BCL + already-used packages. Source-gen JSON contexts. No reflection. | T-001 (DTOs), T-003 (HTTP client) |
| **R10**: ADR-017 regression | `installer-baseline.md` updated. Regression tests for install/status/doctor/uninstall. | T-014 (registration), T-016 (baseline), T-017 (regression) |
| **R11**: ENG-416 schema evolution | Types are strings (no CHECK constraint). Search by type works today. Monitor future schema changes. | No specific task — monitoring only |

---

## 9. Security architecture

### 9.1 STRIDE mitigations (from spec.md §3.1)

| Threat | Mitigation | Task trace |
|--------|------------|------------|
| **T1**: Identity spoofing | `--user` is display-only. `X-Engram-User` from config (`sync.user`), never from flag. | T-013 (command), FR-014 |
| **T2**: MITM on sync server | Recommend HTTPS `remote_url`. Treat all memory content as untrusted. | T-003 (HTTP client), NFR-004 |
| **T3**: Terminal injection | Escape memory text with `Markup.Escape()` before Spectre rendering. | T-011 (renderer), NFR-004 |
| **T4**: Malicious markdown in export | Content is truncated data, never executed. Generated-timestamp header marks provenance. | T-012 (exporter), FR-011 |
| **T5**: No audit trail | Log generation (timestamp, project, user) to installer log. | T-013 (command) |
| **T6**: Leak of personal memory into VCS | Default `scope=team` for export. Personal scope excluded. Warn before writing into git repo. | T-012 (exporter), NFR-005 |
| **T7**: Full-content secrets via drill-down | Drill-down is opt-in. Default previews truncated to 300 chars. No bulk full-content dump. | T-011 (renderer), FR-010 |
| **T8**: Server-down / slow query | Bounded timeouts. Fallback to local SQLite CLI. `--limit` clamp bounds output size. | T-003 (HTTP), T-005 (CLI), T-013 (command) |
| **T9**: Malicious binary path | Subprocess invokes `PathHelper.EngramBinary` (installer-managed), never from user input. | T-005 (CLI client), FR-013 |

### 9.2 Security checklist

- [x] **[SEC]** All memory text escaped with `Markup.Escape()` before Spectre rendering (T-011).
- [x] **[SEC]** `--user` flag is display-only; identity from config (T-013).
- [x] **[SEC]** `--output` defaults to `scope=team`; personal scope excluded (T-012).
- [x] **[SEC]** Drill-down is opt-in; no bulk full-content dump (T-011).
- [x] **[SEC]** Subprocess uses `PathHelper.EngramBinary`, not user input (T-005).
- [x] **[SEC]** HTTP timeouts bounded (configurable, default 30s) (T-003).

---

## 10. Deployment & rollback

### 10.1 Risk level

**LOW** — Additive command, no breaking changes to existing commands. No schema changes. No API contract changes.

### 10.2 Deployment strategy

**Rolling update** — New command is additive. No downtime. No feature flag needed (command is opt-in; users must explicitly run `flowforge onboard`).

### 10.3 Rollback plan

If issues arise post-deployment:

1. Revert the commit that added `OnboardCommand.cs` and modified `Program.cs`.
2. Rebuild and redeploy the installer binary.
3. Verify existing commands (`install`, `status`, `doctor`, `uninstall`) still work.

**Rollback criteria**: Any regression in existing commands (detected by T-017 regression tests).

**Estimated recovery time**: < 5 minutes (rebuild + redeploy).

**Data loss on rollback**: NO — `onboard` is read-only; no data written except optional `ONBOARDING.md` (user-controlled).

---

## 11. Open questions (from spec.md §5)

| ID | Tag | Assumption | Impact on plan |
|----|-----|------------|----------------|
| OQ-1 | [OPTIONAL] | v1 has no dependency on ENG-485; delegation is a follow-up (AD-1). | No impact — plan implements FF-003 independently. |
| OQ-2 | [OPTIONAL] | "Known Blockers / Gotchas" section via keyword search; omitted if empty (FR-007). | T-009 (aggregator) includes blocker keyword search. |
| OQ-3 | [OPTIONAL] | Interactive by default; export only via `--output` (FR-009/FR-011). | T-013 (command) implements both modes. |
| OQ-4 | [FOLLOW-UP] | Out of v1 scope (NG-1); does not change v1 design. | No impact. |

**No BLOCKER questions** — plan can proceed.

---

## 12. Memory signal

- type: decision
- significance: high
- summary: "Plan generated for FF-003: 17 tasks (10S + 6M + 1L), ~3-4 days effort. AOT-safe (source-gen JSON, no reflection, only BCL + already-used packages). HTTP-first with CLI-subprocess fallback. Reuses 6+ existing patterns (DoctorCommand, ConfigStore, EngramProcessChecker, InstallerJsonContext, atomic write, CAF routing). Includes test strategy covering PM-1 to PM-4 manual tests. Security: escape memory content (NFR-004), team scope default for export (NFR-005). Rollback: LOW risk, rolling update, < 5 min recovery."
