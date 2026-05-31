# Changelog

All notable changes to the FlowForge methodology and IDE packs are documented here.

Format based on [Keep a Changelog](https://keepachangelog.com/). Versioning follows [SemVer](https://semver.org/).

## [Unreleased]

### Added

- **Orchestrator Memory Curation Protocol** (item 20): IDE-agnostic protocol for
  proactive knowledge persistence during FlowForge sessions. `forge-arch` and
  `forge-dev` emit a lightweight `## Memory Signal` (3 fields) in their handoff;
  the orchestrator applies a 3-step curation process (type → friction → dedup via
  `mem_search`) using cross-phase context (`revision_cycle`, `rework_count`) that
  subagents lack. `mem_session_summary` is now mandatory (not optional) at
  `/flow-close`. Fallback to `.engram/local_memory/` when MCP is unavailable.
  See [`docs/decisions/ADR-001-memory-curation-protocol.md`](docs/decisions/ADR-001-memory-curation-protocol.md).
- `docs/decisions/` directory and ADR format established for architectural decisions.

### Changed

- Docs: clarify `/flow-*` (human commands) vs `forge-*` (agent names) in QUICKSTART, `docs/06`, `docs/14`, `docs/08` (partial modernize), and `docs/10`.
- `docs/08-test-plan.md`: phase guide uses `/flow-start` … `/flow-close` and `.ai-work/` paths instead of legacy `@forge-*` + `openspec/changes/`.

### Fixed

- OpenCode bundle: avoid invalid `{file:...}` placeholder text inside JSON string values (can crash OpenCode config loader).
- OpenCode bundle: remove hardcoded local skill paths by introducing a repo-path placeholder and patching it during install.
- Install docs: clarify OpenCode merge guidance (merge `agent{}` only; keep existing `mcp`/`permission`; configure provider/API keys for `opencode-go/*` models).

## [0.4.1] - 2026-05-27

### Changed

- English: `docs/05`, `docs/09`, `docs/project-context`, `docs/15` (executive sections)
- `skills/forge-teacher` translated to English

## [0.4.1] - 2026-05-27

### Changed

- English docs: `04-roadmap`, `07-core-skills`, `10-memory-mapping-fallback`, `13-edge-cases-and-risks`
- `docs/15` Part 1: English headers; orchestrator table EN; legacy catalog note
- `forge-memory/metrics` skill: English cycle-time section
- `docs/03` header: reflects implemented engram features

## [0.4.0] - 2026-05-27

### Added

- IDE parity v0.4: shared `workflow-orchestrator-parity.md`, installers (`install.ps1` / `install.sh`)
- Cursor commands: `/flow-plan`, `/flow-rework`, and full `flow-*` set
- OpenCode bundle under `~/.config/opencode/flowforge/`
- `QUICKSTART.es.md`, `README.es.md` (Spanish entry points)
- `examples/crud-tareas/` sample artifacts from a completed CRUD flow
- `VERSION.md` for methodology release tracking

### Changed

- Public docs: English for README, QUICKSTART, CONTRIBUTING, SECURITY, and core `docs/` (01, 02, 06, 11, 14, 16, 18)
- Core skills (`forge-orchestrator`, `forge-arch`, `forge-plan`, `forge-dev`) translated to English
- Orchestrator contract: coordinates only; bugs → `rework_ticket.md` → **forge-dev** (no inline product patches)
- Dev agent marks `plan.md` checklist; PM-* remain human gates

### Fixed

- Comparison table in `docs/05`: 7 agents, 5 checkpoints (CKP-0→4), `verify-report.md`
- Legacy naming: `cert-report` → `verify-report`; `.ai-work/{slug}/` paths

## [0.3.0] - 2026-05-21

- 31 skills (7 core + 23 specialized + teacher)
- CKP-0 through CKP-4 normalized across methodology docs
- IDE integration scaffolding (Cursor, Antigravity, VS Code)
