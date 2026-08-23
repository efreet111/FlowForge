# Session Summary: ff-003-onboarding-flow

**Feature**: `flowforge onboard` — First-day onboarding briefing from engram memories
**Duration**: 2026-08-14 to 2026-08-23
**Phases**: 0 (discovery) -> 1 (arch) -> 2 (plan) -> 3 (dev + verify) -> 4 (close)
**Rework cycles**: 2 (below emergency brake threshold of 3)
**Status**: COMPLETE

---

## Implementation Summary

| Metric | Value |
|--------|-------|
| Files created | 9 source + 5 test + 1 doc |
| Files modified | 2 (Program.cs, InstallerConfig.cs) |
| Total tests | 51 onboarding tests + 155 full suite |
| Manual tests | 4/4 passing (PM-1 to PM-4) |
| Verdict | PASS (after 2 rework cycles) |
| Bugs fixed during manual testing | 3 (stats snake_case, session_id type, ambiguity detection) |

---

## Key Decisions

### AD-1: FF-003 vs ENG-485 boundary
FF-003 owns CLI orchestration + project detection + briefing rendering.
Engram owns memory aggregation/ranking.
**Rationale**: Prevents duplicated ranking logic across repos.

### AD-2: HTTP-first with CLI-subprocess fallback
Primary path uses engram sync server HTTP API (/search, /context, /stats) for structured JSON.
Fallback to engram CLI subprocess with text parsing when server unavailable.
**Rationale**: HTTP is more reliable and structured; CLI is a safety net.

### AD-3: CLI command (not Orchestrator flow) for v1
flowforge onboard is a CLI command, not a /flow-onboard Orchestrator skill.
**Rationale**: Simpler for v1; /flow-onboard is a follow-up (OQ-4).

### AD-4: User identity from config, --user display-only
Identity for API calls comes from ~/.engram/config.json (sync.user -> ENGRAM_USER).
--user flag is display-only (briefing header), never used for filtering or authentication.
**Rationale**: Security - prevents identity spoofing.

---

## Reusable Patterns Extracted

| Pattern | Source | Reused for |
|---------|--------|------------|
| DoctorCommand check-list | Commands/DoctorCommand.cs | Pre-flight checks (FR-002) |
| ConfigStore atomic read | Infrastructure/ConfigStore.cs | Read sync config (FR-013, FR-014) |
| EngramProcessChecker Process | Update/EngramProcessChecker.cs | CLI subprocess invocation (FR-013) |
| InstallerJsonContext source-gen | Models/InstallerConfig.cs | HTTP response deserialization (NFR-002) |
| Atomic write (temp+rename) | ConfigStore.Save() | ONBOARDING.md export (FR-011) |
| CAF command registration | Program.cs | Register onboard (FR-001) |
| ProjectDetector / mem_current_project | engram-dotnet | Project detection without reimplementing (FR-003) |
| FormatContextAsync / mem_context | engram-dotnet | Recent-activity briefing aggregate (FR-004) |

---

## Lessons Learned

### Rework Cycle 1 (6 issues)
1. **FR-002**: CheckConfigAsync was a no-op - ConfigStore.Load() returns defaults, not exceptions. **Fix**: Validate sync.user is non-empty.
2. **FR-014/FR-015/T1**: --user flag was misused for personal-scope namespace. **Fix**: Resolve identity from config first, pass to resolver.
3. **NFR-005/T6**: Default --output exported personal memories. **Fix**: Force scope=team when output set without scope.
4. **FR-007**: Blockers search used type=null (all types). **Fix**: Use type=bugfix + type=manual (two parallel searches).
5. **FR-010/T5**: mem_timeline drill-down not wired. **Fix**: {number}t syntax in renderer calls GetTimelineAsync.
6. **NFR-001**: FLOWFORGE_API_TIMEOUT_SECONDS ignored. **Fix**: Read env var in command, not hardcoded.

### Rework Cycle 2 (3 missing tests)
1. **FR-002**: Config pre-check exit 2 - no test existed. **Fix**: Added 6 tests.
2. **NFR-001**: Env-var timeout - not tested. **Fix**: Added 4 tests.
3. **FR-010**: Timeline drill-down - not tested. **Fix**: Added 3 tests.

### Key Insight
**Verify "test added" claims by actually running the tests.** The verify agent caught that rework cycle 1 claimed tests were added but they were not actually present. This is the exact pattern the Sentinel Judge exists to catch.

### Bugs Fixed During Manual Testing
1. **Stats showing 0**: Server returns snake_case (total_sessions), DTO expected camelCase (totalSessions). **Fix**: Added [JsonPropertyName] attributes.
2. **Missing sections**: session_id type mismatch - server returns string, DTO expected long?. **Fix**: Changed DTO to string?.
3. **Ambiguity detection**: DetectFromGit returned directory name instead of null when no .git found. **Fix**: Return null to allow child project detection.

---

## Security and Compliance

### STRIDE Mitigations
- **T1 (Identity spoofing)**: --user is display-only; identity from config.
- **T2 (MITM)**: Recommend HTTPS; treat memory content as untrusted.
- **T3 (Terminal injection)**: Escape memory text with Markup.Escape().
- **T4 (Malicious markdown)**: Content is truncated data, never executed.
- **T5 (No audit trail)**: Log generation (timestamp, project, user).
- **T6 (Leak personal memory)**: Default scope=team for export; personal scope excluded.
- **T7 (Full-content secrets)**: Drill-down is opt-in; previews truncated to 300 chars.
- **T8 (Server-down)**: Bounded timeouts; fallback to local SQLite CLI.
- **T9 (Malicious binary)**: Subprocess uses PathHelper.EngramBinary, not user input.

### AOT Compatibility
- Source-gen JSON (OnboardingJsonContext with CamelCase naming policy)
- No reflection
- Only BCL + already-used packages (Spectre.Console, ConsoleAppFramework)
- Zero new AOT/trimming warnings from new code

### ADR-017 Compliance
- installer-baseline.md updated with full command reference
- Regression tests: existing commands unaffected
- Additive change (no modification to existing commands)

---

## Future Work

### AI Agent Awareness (OQ-4 from spec)
**What**: Make AI agents aware of flowforge onboard command
**Why**: Agents do not know this command exists; new users will not discover it unless they read the docs
**How**:
1. Update AGENTS.md with onboarding detection logic
2. Create skills/forge-onboarding/SKILL.md
3. Optional: Create /flow-onboard workflow

**Priority**: P1 (important for team adoption)
**Effort**: 1-2 hours
**Status**: Not started (follow-up after FF-003 closure)

**Detection signals for AI agents**:
- User asks "How do I get started?"
- User has not worked on this project in >7 days
- User is switching projects
- No ONBOARDING.md exists in the repo

---

## Files Created/Modified

### Source Files
- src/FlowForge.Installer/Onboarding/EngramModels.cs - DTOs + source-gen JSON context
- src/FlowForge.Installer/Onboarding/IEngramClient.cs - Interface for memory retrieval
- src/FlowForge.Installer/Onboarding/HttpEngramClient.cs - HTTP API client (primary)
- src/FlowForge.Installer/Onboarding/CliEngramClient.cs - CLI subprocess fallback
- src/FlowForge.Installer/Onboarding/ProjectResolver.cs - Project detection + namespacing
- src/FlowForge.Installer/Onboarding/BriefingAggregator.cs - Orchestrates mem_context/search/stats
- src/FlowForge.Installer/Onboarding/BriefingRenderer.cs - Spectre.Console rendering
- src/FlowForge.Installer/Onboarding/MarkdownExporter.cs - Atomic ONBOARDING.md export
- src/FlowForge.Installer/Commands/OnboardCommand.cs - Main command with pre-checks

### Test Files
- tests/.../Onboarding/HttpEngramClientTests.cs - 10 tests
- tests/.../Onboarding/CliEngramClientTests.cs - 8 tests
- tests/.../Onboarding/ProjectResolverTests.cs - 8 tests
- tests/.../Onboarding/BriefingAggregatorTests.cs - 7 tests
- tests/.../Onboarding/BriefingRendererTests.cs - 3 tests
- tests/.../Onboarding/OnboardCommandIntegrationTests.cs - 5 tests
- tests/.../Onboarding/OnboardCommandPreCheckTests.cs - 6 tests

### Documentation
- docs/installer-baseline.md - Full command reference for flowforge onboard

### Modified Files
- src/FlowForge.Installer/Program.cs - Registered onboard command
- src/FlowForge.Installer/FlowForge.Installer.csproj - Added InternalsVisibleTo for tests

---

## Memory Signal

- **type**: decision
- **significance**: high
- **summary**: FF-003 complete: 51 tests pass, 4/4 manual tests verified, verdict PASS after 2 rework cycles. Key decisions: FF-003 vs ENG-485 boundary, HTTP-first with CLI fallback, CLI command for v1, user identity from config. 8 reusable patterns extracted. Lessons: config validation must check field values, --user is display-only, --output must filter to team scope, blockers search uses type=bugfix/manual, mem_timeline drill-down must be wired, env-var timeout must be read. Security: Markup.Escape(), team scope default, --user display-only. AOT-safe, ADR-017 compliant. Future work: AI agent awareness (OQ-4) - update AGENTS.md + create forge-onboarding skill.
