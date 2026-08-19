---
cycle_count: 1
max_cycles: 3
status: "resolved"
severity: P2
---
# Rework ticket — ff-003-onboarding-flow

## 1. Failure Reason

**Spec deviation (deterministic rules)** — not a false green and not a runtime crash. 32/32
onboarding tests pass, but the test suite does not cover four deterministic spec rules that the
implementation violates, plus two partial gaps:

1. **FR-002** — the "engram config" pre-check is a no-op. `CheckConfigAsync` (`OnboardCommand.cs:192-211`)
   calls `ConfigStore.Load()`, which returns defaults (never throws) when `~/.engram/config.json`
   is missing/corrupt, then unconditionally returns `true`. The required `engram config — ✗ FAIL`
   + exit 2 path is dead code.
2. **FR-014 / FR-015 / T1** — the `--user` flag is passed into `ProjectResolver.Resolve()` and used
   as the personal-scope namespace (`{user}/{project}`), violating "`--user` is display-only".
   Additionally, `--scope personal` without `--user` resolves to `team/{project}` instead of
   `{configUser}/{project}`.
3. **NFR-005 / T6** — with `--output` and no `--scope` (the default), the aggregator runs an engram
   wide-read (team + personal) and `MarkdownExporter` writes it unfiltered. Personal memories can
   leak into `ONBOARDING.md`.
4. **FR-007** — blockers search passes `type=null` (all types) instead of `type=bugfix` / `type=manual`,
   surfacing non-blocker observations as blockers.

## 2. Affected Files

- `src/FlowForge.Installer/Commands/OnboardCommand.cs`
- `src/FlowForge.Installer/Onboarding/ProjectResolver.cs`
- `src/FlowForge.Installer/Onboarding/BriefingAggregator.cs`
- `src/FlowForge.Installer/Onboarding/MarkdownExporter.cs`
- `src/FlowForge.Installer/Onboarding/BriefingRenderer.cs`

## 3. Correction Instruction

1. **FR-002** — Make `CheckConfigAsync` detect a missing/unparseable config file explicitly
   (e.g. `File.Exists(PathHelper.ConfigFile)` and a successful parse — do not rely on `Load()`
   defaults), returning `(false, "Config unreadable. Run `flowforge install` or `flowforge config`.")`.
   Add a unit test asserting exit 2 when config is absent/corrupt.
2. **FR-014/FR-015** — Do NOT pass the `--user` flag to `ProjectResolver.Resolve()`. Resolve identity
   first (`ResolveUserIdentity(config)`) and pass that identity for personal-scope namespacing.
   `--user` must remain display-only (header text). Fix `ApplyNamespace` so personal scope uses the
   resolved identity even when `--user` is null. Add tests: `--scope personal` → `{identity}/flowforge`;
   `--user X --scope personal` → namespace still `{identity}/flowforge`.
3. **NFR-005/T6** — Normalize the export scope to `team` by default, or filter `BriefingData`
   (`Decisions`/`Patterns`/`Blockers`/`RecentActivity`) to team scope before exporting. Personal-scope
   items must never reach the markdown file. Add a test: default `--output` excludes personal-scope
   entries.
4. **FR-007** — Restore the type filter: search `type=bugfix` and `type=manual` (two searches, or a
   single search with post-hoc type filtering). Update `BriefingAggregatorTests` to assert the type
   filter is applied (not null).
5. **FR-010/T5 (partial)** — Wire `mem_timeline` into the drill-down loop (e.g. accept `1t` to show
   `GetTimelineAsync`). Add a generation log line (timestamp, project, user) via `_ctx.Log` to
   satisfy the T5 audit-trail mitigation.

## 4. Close Criteria

- [x] Config pre-check fails with exit 2 on missing/corrupt `config.json` (test added).
- [x] `--user` flag no longer influences memory queries or namespacing; personal scope uses resolved identity.
- [x] Default `--output` never writes personal-scope memories (test added).
- [x] Blockers search filters to `bugfix`/`manual` types.
- [x] `mem_timeline` drill-down wired; generation audit log present.
- [x] All 38 onboarding tests green (was 32, +6 new); full suite 142 pass / 8 pre-existing (unchanged).
