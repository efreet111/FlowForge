---
cycle_count: 2
max_cycles: 3
status: "resolved"
severity: P2
---
# Rework ticket — ff-003-onboarding-flow (cycle 2)

## 1. Failure Reason

**False Green close (test coverage, not a code defect).** The cycle-1 rework correctly fixed all 6
issues in **code** (verified line-by-line), and 38/38 onboarding tests pass. But the rework ticket's
close criteria were marked `[x]` inaccurately:

- Close criterion #1 states *"Config pre-check fails with exit 2 on missing/corrupt config.json
  **(test added)**"* — **no test was added.** The deterministic FR-002 exit-2 rule (the #1
  HIGH-severity issue from the original audit) still has **zero unit-test coverage**.
- The +6 new tests map to fixes #2 (FR-014 ×2), #3 (NFR-005 ×1), #4 (FR-007 ×2), and #6 (timeout
  propagation ×1). Fix #1 (FR-002) and fix #5 (FR-010 timeline drill-down) have **no** tests, and
  fix #6's env-var read (`GetApiTimeoutSeconds`) is untested (only generic HttpClient timeout
  propagation is covered).

This is a False Green because a deterministic spec rule required to be tested (spec §2 FR-002
Scenario A/B, capability-matrix "deterministic") remains unguarded against regression.

## 2. Affected Files

- `tests/FlowForge.Installer.Tests/Onboarding/OnboardCommandIntegrationTests.cs` (or a new test file, e.g. `OnboardCommandPreCheckTests.cs`)
- `src/FlowForge.Installer/Commands/OnboardCommand.cs` (only to expose a testable seam for `CheckConfigAsync` / `GetApiTimeoutSeconds` — do **not** change behavior)
- `src/FlowForge.Installer/Onboarding/BriefingRenderer.cs` (only if needed to make the timeline drill-down testable)

## 3. Correction Instruction

1. **FR-002 (required).** Add a unit test asserting the pre-check fails (exit 2) for each of:
   - missing `config.json` (`PathHelper.ConfigFile` does not exist),
   - corrupt/unparseable `config.json`,
   - `config.json` present but `sync.user` empty/whitespace,
   - engram binary missing (FR-002 Scenario A).
   To make this testable without global state, add an internal seam (e.g. inject the config file
   path and/or `ConfigStore` into `OnboardCommand` or extract `CheckConfigAsync` into a testable
   internal helper). `InternalsVisibleTo` is already configured in the csproj.
2. **NFR-001 (required).** Make `GetApiTimeoutSeconds()` `internal` and add a test:
   - `FLOWFORGE_API_TIMEOUT_SECONDS=5` → returns 5,
   - absent / non-numeric / `<=0` → returns 30.
3. **FR-010 (required).** Add a test that the `{number}t` drill-down path invokes
   `GetTimelineAsync(id, before:5, after:5)` (extract the timeline fetch into a testable method or
   assert through a mock `IEngramClient`), and that plain `{number}` invokes `GetObservationAsync`.

## 4. Close Criteria

- [x] FR-002 pre-check test added and green (missing/corrupt/empty-user config → exit 2; missing binary → exit 2).
- [x] NFR-001 env-var timeout test added and green.
- [x] FR-010 timeline drill-down test added and green.
- [x] All onboarding tests green (>= 41, was 38 + 3 new minimum). → **51/51 pass** (38 existing + 13 new).
- [x] Full suite still exactly 8 pre-existing failures (unrelated to FF-003). → **155 pass / 8 fail / 163 total**.
- [x] Close criteria reflect only what actually shipped (no false "test added" claims).
