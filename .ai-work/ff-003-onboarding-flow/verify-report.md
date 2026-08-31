# Verify Report — ff-003-onboarding-flow (Final, cycle 2)

- Feature slug: `ff-003-onboarding-flow`
- Verdict: **PASS** ✅
- Agent: forge-verify (Phase 3b, Sentinel Judge)
- Date: 2026-08-19
- Commit audited: `f0e2a74` (test(installer): add 13 missing tests for ff-003 rework cycle 2)
- Rework cycles consumed: 2 (below CKP-3 emergency-brake threshold of 3)

---

## 1. Verdict

**PASS.** All 13 cycle-2 tests exist and pass, all 51 onboarding tests are green, and every
FR / NFR / STRIDE requirement is implemented and covered. The cycle-2 rework resolved the prior
False-Green by adding the three missing test areas (FR-002 config pre-check, NFR-001 env-var
timeout, FR-010 timeline drill-down). Production changes were limited to internal testability
seams with **no behavior change**. The 8 remaining full-suite failures are identical pre-existing
issues unrelated to FF-003.

The feature is ready for `/flow-close` (Phase 4), pending the human PM-* manual tests below.

---

## 2. Cycle-2 rework verification (the 3 previously-missing areas)

| Area | New tests | Test file | Result |
|------|-----------|-----------|--------|
| FR-002 — config pre-check (exit 2) | 6 | `OnboardCommandPreCheckTests.cs` | ✅ 6/6 green |
| NFR-001 — `FLOWFORGE_API_TIMEOUT_SECONDS` | 4 | `HttpEngramClientTests.cs` | ✅ 4/4 green |
| FR-010 — timeline drill-down (`{number}t`) | 3 | `BriefingRendererTests.cs` | ✅ 3/3 green |

**Production changes (commit `f0e2a74`) — internal seams only, zero behavior change:**

1. `OnboardCommand.CheckConfig(string configFile, ConfigStore store)` — extracted from
   `CheckConfigAsync()` as `internal static`. Same logic, same hints, same `File.Exists` /
   `store.Load()` / `sync.user` validation. `CheckConfigAsync()` now delegates with
   `PathHelper.ConfigFile` and `_ctx.Store` (identical to prior behavior).
2. `OnboardCommand.GetApiTimeoutSeconds()` — visibility changed `static` → `internal static`.
   Logic unchanged (`int.TryParse` + `> 0` else default 30).

No new packages, no reflection, no AOT-unsafe changes. `InternalsVisibleTo("FlowForge.Installer.Tests")`
was already configured (verified in `FlowForge.Installer.csproj:29`).

---

## 3. Final test results

| Suite | Result |
|-------|--------|
| Onboarding tests (7 files) | ✅ **51/51 pass** (38 existing + 13 new) |
| Full test run | **155 pass / 8 fail / 163 total** |

**New-test assertion/oracle validation** (expected values matched against spec, not
implementation-derived):

- `GetApiTimeoutSeconds`: absent/invalid/`<=0` → **30** (spec NFR-001 "default 30 s"); valid `5` → **5**. ✅
- Drill-down timeline: `GetTimelineAsync(id, before:5, after:5)` (spec FR-010 Scenario B "before=5, after=5"). ✅
- Config pre-check: missing/corrupt/empty/whitespace/null-sync → `Passed=false` (exit 2 path; spec FR-002 + NFR-007). ✅
- Limit clamp: `Math.Clamp(limit, 1, 20)` asserted (spec deterministic rule "clamped to 1..20"). ✅
- Preview truncation: 300 chars in `HttpEngramClient.TruncatePreview` (spec "truncated to 300 chars"). ✅

**8 pre-existing failures — confirmed identical & unrelated to FF-003** (none of their files were
touched by `4208639`, `0f5ad19`, or `f0e2a74`):

1. `ScriptTests.FR_002_CurlUsesProgressBar` — `bin/install/install.sh` not copied to test output.
2. `ScriptTests.FR_002_WgetSpinnerMessage` — same.
3. `ScriptTests.FR_003_DiagnoseFlagSupported` — same.
4. `ScriptTests.FR_003_HeadlessDetectionHonorsFlowforgeYes` — same.
5. `DocumentationTests.FR_008_ReadmeIncludesTroubleshootingHints` — README not copied to test output.
6. `DocumentationTests.FR_008_ReadmeEsIncludesTroubleshootingHints` — same.
7. `InstallCommandSourceTests.FR_004_BannerAndConnectingPrintedBeforeManifest` — banner ordering assertion.
8. `GitHubReleasesClientTests.FR_001_GetLatestVersion_TimesOut` — flaky network-dependent timeout.

---

## 4. Traceability matrix (FR → implementation file)

| FR | Requirement | Implementation file | Code | Test |
|----|-------------|---------------------|------|------|
| FR-001 | Register `onboard` + knownCommands | `Program.cs:84,93` | ✅ | ✅ |
| FR-002 | Pre-flight checks → exit 2 | `OnboardCommand.cs:173-241` | ✅ | ✅ **6 tests (new)** |
| FR-003 | Project detection + ambiguity | `ProjectResolver.cs:32-95`, `OnboardCommand.cs:45-67` | ✅ | ✅ |
| FR-004 | Recent activity via `mem_context` | `BriefingAggregator.cs:46`, `HttpEngramClient.cs:49`, `CliEngramClient.cs:34` | ✅ | ✅ |
| FR-005 | Decisions via `type=decision` | `BriefingAggregator.cs:47` | ✅ | ✅ |
| FR-006 | Conventions via `type=pattern` + keywords | `BriefingAggregator.cs:48-50` | ✅ | ✅ (guard test) |
| FR-007 | Blockers keyword search over `bugfix`/`manual` | `BriefingAggregator.cs:52-57,113-131` | ✅ | ✅ (2 tests) |
| FR-008 | Stats header from `mem_stats` | `BriefingAggregator.cs:58`, `BriefingRenderer.cs:38-42` | ✅ | ✅ |
| FR-009 | Interactive briefing (Spectre) | `BriefingRenderer.cs:25-89`, `OnboardCommand.cs:133-135` | ✅ | ✅ |
| FR-010 | Drill-down via `mem_get_observation` + `mem_timeline` | `BriefingRenderer.cs:117-204` | ✅ | ✅ **3 tests (new)** |
| FR-011 | ONBOARDING.md export (atomic, team scope) | `MarkdownExporter.cs:21-133`, `OnboardCommand.cs:141-156` | ✅ | ✅ |
| FR-012 | Empty-memory guidance | `OnboardCommand.cs:116-130` | ✅ | ✅ |
| FR-013 | HTTP-first → CLI fallback | `OnboardCommand.cs:71-101`, `HttpEngramClient.cs`, `CliEngramClient.cs` | ✅ | ✅ |
| FR-014 | Identity from config; `--user` display-only | `OnboardCommand.cs:37-45,245-255` | ✅ | ✅ |
| FR-015 | `team/{project}` namespacing | `ProjectResolver.cs:101-108` | ✅ | ✅ |

---

## 5. NFR compliance matrix

| NFR | Requirement | Code | Test |
|-----|-------------|------|------|
| NFR-001 | Perf < 5s/2s; configurable timeout | ✅ (`GetApiTimeoutSeconds` reads env; default 30s) | ✅ **4 tests (new)** |
| NFR-002 | AOT (source-gen JSON, no reflection) | ✅ (`OnboardingJsonContext`, `FlowForgeProjectJsonContext`, `[GeneratedRegex]`, no new packages) | ✅ |
| NFR-003 | ADR-017 (baseline + regression + composition) | ✅ (`installer-baseline.md` updated; additive command) | ✅ |
| NFR-004 | Security (escape content, no injection) | ✅ (`Markup.Escape` on all memory text) | ✅ |
| NFR-005 | Privacy (team scope default for export) | ✅ (`effectiveScope=team`, `FilterToTeamScope`) | ✅ |
| NFR-006 | Resilience (atomic write, timeouts, audit-trail) | ✅ (atomic write + timeouts + `_ctx.Log.Info` T5) | ✅ |
| NFR-007 | Deterministic exit codes 0/1/2 | ✅ | ✅ (FR-002 exit-2 path now covered) |

---

## 6. STRIDE mitigation verification

| Threat | Mitigation | Status |
|--------|-----------|--------|
| T1 — Identity spoofing | `--user` display-only; identity from config | ✅ PASS |
| T2 — MITM | Content untrusted; no creds to disk | ✅ PASS |
| T3 — Terminal injection | `Markup.Escape()` before Spectre | ✅ PASS |
| T4 — Malicious markdown | Truncated; provenance header | ✅ PASS |
| T5 — No audit trail | `_ctx.Log.Info(...)` generation log | ✅ PASS |
| T6 — Leak into VCS | `effectiveScope=team`; personal filtered | ✅ PASS |
| T7 — Secrets via drill-down | Opt-in; 300-char previews | ✅ PASS |
| T8 — Server down / slow query | Bounded timeouts; CLI fallback; limit clamp | ✅ PASS |
| T9 — Malicious binary path | `PathHelper.EngramBinary`, not user input | ✅ PASS |

---

## 7. Line-by-line & static inspection (Step Zero)

- No debug prints, no `TODO`/`FIXME`/`HACK` markers, no empty blocks, no missing returns in any
  `src/FlowForge.Installer/Onboarding/*` file or `OnboardCommand.cs`.
- All `AnsiConsole.WriteLine` calls are legitimate rendering output (not stray debug).
- No secrets or credentials written to disk (T2). No reflection-based serialization.
- Cyclomatic complexity of all onboarding methods < 20 (dense parsers in `CliEngramClient` remain
  under threshold; `BriefingRenderer.DrillDownLoopAsync` has a bounded while-loop, not nested
  conditionals).

---

## 8. Summary of cycle-2 verification

The prior REWORK (cycle 2) was a **test-coverage False Green**, not a code defect. This cycle
added 13 tests (6 FR-002, 4 NFR-001, 3 FR-010) and exposed two minimal internal seams
(`CheckConfig`, `GetApiTimeoutSeconds`) with no behavior change. All close criteria in
`rework_ticket.md` (cycle 2, status `resolved`) are now accurately satisfied — no "test added"
claims remain unbacked. Total rework cycles: 2 of 3 (no CKP-3 emergency brake).

---

## Pending Manual Tests

The developer must run **PM-1 → PM-4** from `spec.md` §4 before `/flow-close`. These are Layer-B
human validations (happy path, no-engram error, ambiguity/empty-memory, export+drill-down) and are
**not** evaluated by this verdict.

---

## 🔍 Manual Verification Steps

1. `flowforge onboard --help` — shows all 6 flags; `flowforge --help` lists `onboard`.
2. Rename `~/.local/bin/engram` → run `flowforge onboard` → "engram binary — ✗ FAIL" + exit 2.
3. Temporarily corrupt/remove `~/.engram/config.json` → run `flowforge onboard` → "engram config — ✗ FAIL" + exit 2.
4. `flowforge onboard --scope personal` (no `--user`) → must resolve `{configUser}/flowforge`, not `team/flowforge`.
5. `flowforge onboard --output ONBOARDING.md` (no `--scope`) → inspect file: no personal-scope entries.
6. Interactive briefing → type `1` (full content) and `1t` (timeline).
7. `FLOWFORGE_API_TIMEOUT_SECONDS=5 flowforge onboard` → HTTP client honors 5s timeout (validates env-var path live).

---

## Memory Signal

- type: decision
- significance: high
- summary: "FF-003 final verification after rework cycle 2: all 51 onboarding tests pass (38 + 13 new covering FR-002 exit-2 pre-check, NFR-001 env-var timeout, FR-010 timeline drill-down). All FR-001..FR-015, NFR-001..NFR-007, STRIDE T1-T9 verified. Production changes limited to internal seams (no behavior change). 8 pre-existing full-suite failures unchanged/unrelated. Verdict: PASS. Ready for /flow-close (Phase 4). Total rework cycles: 2 (below emergency brake threshold of 3)."
