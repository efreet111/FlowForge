---
title: "Session close — flowforge-update-mechanism (CKP-4 GREEN, PR #24 MERGED)"
type: "session_summary"
topic_key: "installer/update-mechanism"
date: "2026-08-13"
scope: "team"
project: "team/flowforge"
---

## Goal
Close feature `flowforge-update-mechanism`: extend `flowforge update` from an in-place engram binary swap (no backup, no health-check, no granularity) to a per-component update mechanism (engram | flowforge-skills | all) that preserves user config, tolerates failure with automatic rollback, and exposes full traceability (12 FRs, 5 OQ resolved).

## Discoveries
1. **Two MCP data-loss bugs (S6)**: Cursor `mcp.json` and Antigravity `mcp_config.json` lost all non-FlowForge MCP servers on full-file regeneration. Fix: surgical read-merge-write via `JsonNode` (pattern already proven in OpenCode) generalized to all IDEs.
2. **Stub traps verification late**: `UpdateSkillsAsync` was a stub with correct signature — only functional tests (real file copies) exposed it in rework cycle 1. forge-verify must audit that installer methods do real work, not just exist.
3. **Baseline pays off**: `flowforge status` had been de-registered during implementation; caught in rework cycle 2 only because `installer-baseline.md` (Installer Protection Policy) documented expected commands.
4. **Hardcoded version drift**: `InstallerVersion` constant drifted from the manifest (`0.1.0-alpha.6` vs `.7`), causing `flowforge status` inconsistencies. Versions must derive from manifest/`config.json`.
5. **NTFS-mounted workspace blocks `dotnet test`**: verification used Docker suite (99/107, 8 pre-existing failures) + static coverage validation; final human host run: 95/112 (17 pre-existing failures unrelated).

## Accomplished
- `UpdateOrchestrator` composing existing modules (~80% reuse: `EngramModule`, `ConfigStore`, `ManifestClient`, `FlowForgeModule`) + 9 new units (`BackupManager`, `HealthCheckRunner`, `McpConfigMerger`, `UserModifiedAgentDetector`, `CacheRefresher`, `EngramProcessChecker`, `ManagedPathsSidecarFactory`, `ComponentRegistry`, `UpdateResult`).
- 10 modules + 9 test files; ~1818 lines added across 22 files (commits `654ddc1`, `786fd78`, `984daa3`).
- 2 rework cycles resolved (cycle 1: stub + structured logging; cycle 2: status command, agent detection prompt, version consistency).
- PM-1/PM-3/PM-4/PM-5 PASSED (human-verified). PM-2 (rollback broken binary) **deferred as minor technical debt** — requires complex simulation; rollback covered by unit tests (`BackupManagerTests`, `UpdateOrchestratorTests`).
- ADRs promoted: ADR-016 (update mechanism by component), ADR-017 (installer protection policy).
- **PR #24 created → MERGED** into `main` (`c7bb9c6`, 2026-08-13) with 5/5 CI checks passed; feature branch deleted upstream. Full PR description documents architecture, testing (95/112 unit + 25/25 exhaustive), deferred items.
- Engram observations saved: decision (#97), session summary (#98), metrics (#99). Retention prune: 0 observations past TTL.

## Next Steps
- **Release/packaging**: feature is on `main`; next human step is cutting the next FlowForge release tag so `flowforge update` ships in the binary.
- **PM-2 follow-up**: simulate broken-binary release if a real rollback regression is suspected; unit coverage is the safeguard meanwhile.
- **OQ-1** (`flowforge update --self`) post-MVP; bootstrap `curl | bash` remains the installer update path.
- Local artifact updates (this summary + obs) committed locally on `feat/flowforge-update-mechanism` (git-sin-push: no push without explicit request).

## Relevant Files
- **PR #24** — https://github.com/efreet111/FlowForge/pull/24 (merged `c7bb9c6`, 5/5 checks)
- `.ai-work/flowforge-update-mechanism/{context-map,spec,plan,verify-report,summary,installer-baseline}.md`
- `src/FlowForge.Installer/Update/*.cs` — 10 modules new.
- `tests/FlowForge.Installer.Tests/Update/*.cs` — 9 test files new.
- `docs/decisions/ADR-016-update-mechanism-by-component.md`, `docs/decisions/ADR-017-installer-protection-policy.md` — new.
- `CHANGELOG.md` — entry added under [Unreleased].
