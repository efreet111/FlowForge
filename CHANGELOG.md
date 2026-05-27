# Changelog

All notable changes to the FlowForge methodology and IDE packs are documented here.

Format based on [Keep a Changelog](https://keepachangelog.com/). Versioning follows [SemVer](https://semver.org/).

## [0.4.1] - 2026-05-27

### Changed

- English: `docs/05`, `docs/09`, `docs/project-context`, `docs/15` (executive sections)
- `skills/forge-teacher` translated to English

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
