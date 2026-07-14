# Changelog

All notable changes to the FlowForge methodology and IDE packs are documented here.

Format based on [Keep a Changelog](https://keepachangelog.com/). Versioning follows [SemVer](https://semver.org/).

## [Unreleased]

### Added
- **ADR-008**: matriz canónica IDE × ruta global × ruta proyecto × detección (`docs/decisions/ADR-008-ide-installer-path-matrix.md`).
- **ADR-007 — FlowDoc v2.0 migration** (absorbs branch `FlowDocsv2Adoption`): migrate project templates from FlowDoc v1.1 → v2.0. HU template expanded with GWT scenarios (Happy Path, Edge Cases, Error Cases), Owner & Timeline, Technical Debt, and 🧪 Ref test links. `flowdoc-ciclo.md` removed from artifact table (deprecated upstream in v2.0). Range-binned folder structure adopted (`docs/tasks/HU-001-HU-099/`). `.flowforge.json` version pin moved from concatenated string (`flowdoc@1.1`) to split keys (`docs_framework` + `docs_framework_version` + `upstream`). Adoption levels reduced from L1–L4 to L1–L3 (L4 split into L3 + post-cycle review). See [`docs/decisions/ADR-007-flowdocs-v2-absorption.md`](docs/decisions/ADR-007-flowdocs-v2-absorption.md) and [`docs/decisions/ADR-004-flowdoc-integration.md`](docs/decisions/ADR-004-flowdoc-integration.md).

### Fixed
- **Stack Installer**: `--yes` ahora salta todos los prompts de Spectre.Console y usa defaults (ambos componentes, IDEs auto-detectados, modo sync si `ENGRAM_SERVER_URL` está definido). Corrige `System.NotSupportedException` en entornos no-interactivos (CI/CD, Docker, scripts).
- **EngramModule**: symlink automático `libe_sqlite3.so` → `libsqlite3.so` del sistema cuando la librería nativa no viene en el release de engram-dotnet. Soporte para `libe_sqlite3.so` (Linux) y `e_sqlite3.dll` (Windows).
- **EngramModule**: `ENGRAM_SERVER_URL` ahora incluido en la configuración MCP generada cuando `syncEnabled=true`.
- **GitHubReleasesClient**: `DownloadNativeSqliteLibAsync` descarga la librería nativa SQLite desde el release de engram-dotnet.
- **FlowForgeModule**: mensaje de error multiplataforma cuando falta git (pacman/apt/brew/winget).
- **install.sh/install.ps1**: agregan flag `--yes` al wizard de instalación.
- **Multi-IDE packs** (`fix-ide-installer-packs`): Copilot y Kilo instalan en `~/.copilot/agents/` y `~/.config/kilo/agents/`; OpenCode en `~/.config/opencode/agents/` + `commands/` (limpieza legacy); Antigravity en `~/.gemini/antigravity/`; `flowforge init` crea `.github/`, `.opencode/`, `.kilo/`, `.cursor/`, `.agents/`; `flowforge doctor` reporta extensiones VS Code y rutas globales/proyecto; paridad `install.sh`/`install.ps1` (best-effort Copilot+Kilo, OpenCode sin JSON legacy en Windows).
- **ADR-005**: documenta los 5 bugs del ecosistema de instalación (cross-repo FlowForge + engram-dotnet).
- feat: fix-installer — Arregla instalador que se queda "pegado": timeouts configurables, barra de progreso en descargas, detección headless fiable, feedback temprano, comando `flowforge doctor`, corrección de ownership post-sudo y documentación de troubleshooting.

### Changed
- Nombre del asset nativo Linux: `e_sqlite3.so` → `libe_sqlite3.so` (respeta el prefijo `lib` de SQLitePCLRaw).

## [v0.1.0-alpha.2] - 2026-06-23

### Added
- ENG-301 Stack Installer (C# .NET 10 AOT, linux-x64 + win-x64)
- Bootstrap scripts (`install/install.sh`, `install/install.ps1`) with SHA-256 verification
- Remote `manifest.yaml` for runtime version compatibility checks
- `--verbose` global flag with `Verbosity.FormatError` gating stack traces
- Pre-install warning for existing `forge-*` agent files (FU-5)
- `ide/install.sh` for Cursor/OpenCode/VS Code/Antigravity skill installation
- Installer 5-command CLI: `install`, `update`, `uninstall`, `config`, `status`

### Fixed (5 hotfix cycles during alpha)
- `release.yml`: use OS-specific runners for AOT publish (matrix ubuntu-latest + windows-latest) — cross-OS AOT compilation not supported
- `release.yml`: use `shell: bash` on Windows runner to avoid PowerShell parse errors
- `install.sh`: handle pre-release version lookup (use `/releases` list, not `/releases/latest`)
- `install.sh`: move `FETCH` variable outside conditional (was unbound with `--version` flag)
- `Program.cs`: register `InstallerContext` in CAF DI container (was missing, caused NRE in all commands)
- `Program.cs`: pre-validate commands before CAF dispatch to prevent NRE stack trace leaks on invalid commands
- `Verbosity.cs`: properly register `--verbose` flag via CAF `ConfigureGlobalOptions` + manual args filter (was causing NRE on `--verbose --help`)
- `ide/install.sh`: move `DEST` env var before python command (bash ignored it after the command)

## [0.5.0] - 2026-06-21

### Added

- **CKP-1 Open Questions gate** — `forge-arch` now requires all open questions in `spec.md` to be tagged `[BLOCKER]`, `[OPTIONAL]`, or `[FOLLOW-UP]` (new section 5). The orchestrator's CKP-1 handling now distinguishes three cases: no questions (approve freely), BLOCKERs present (halt, present questions explicitly, reject "adelante" without answers), or only OPTIONAL/FOLLOW-UP (note assumptions, approve). `forge-plan` adds a mechanical pre-flight guard: if any `[BLOCKER]` row remains in `spec.md`, it stops and refuses to write `plan.md`. No new checkpoint added — CKP-1 strengthened in place.
- **FlowDoc integration** (`flow-init`, `templates/project/`, ADR-004): `flow-init.sh` and `flow-init.ps1` scaffold a new project with FlowDoc v1.1-compatible `docs/` structure (PRD, DEVELOPMENT, HU tasks, ADR/RFC templates, `.flowforge.json`). `GLOSSARY.md` + `GLOSSARY.es.md` added as terminology reference. See [`docs/decisions/ADR-004-flowdoc-integration.md`](docs/decisions/ADR-004-flowdoc-integration.md) and [`docs/20-flowdoc-ecosystem.md`](docs/20-flowdoc-ecosystem.md).
- **Pattern Search step in `forge-discovery`** (item 21): mandatory codebase cloning check before any non-trivial design. Agents must search for existing implementations of the same architectural shape and document findings in a `## Reusable Patterns Found` section of the Context Map. See [`docs/decisions/ADR-003-pattern-search-mandate.md`](docs/decisions/ADR-003-pattern-search-mandate.md).
- **Orchestrator Memory Curation Protocol** (item 20): IDE-agnostic protocol for proactive knowledge persistence during FlowForge sessions. `forge-arch` and `forge-dev` emit a lightweight `## Memory Signal` (3 fields); the orchestrator applies a 3-step curation process using cross-phase context (`revision_cycle`, `rework_count`). `mem_session_summary` is now mandatory at `/flow-close`. Fallback to `.engram/local_memory/` when MCP is unavailable. See [`docs/decisions/ADR-001-memory-curation-protocol.md`](docs/decisions/ADR-001-memory-curation-protocol.md).
- **CI — OpenCode Smoke + Layer 1 structural linting** (items 1 + 22): `.github/workflows/opencode-smoke.yml` validates `opencode.flowforge.json` (JSON syntax, 7 subagents, skill paths, no placeholders), SKILL.md completeness (≥3 sections per core skill), AGENTS.md cross-references, and `flow-init.sh` smoke on every push/PR to `main`.
- **GitHub contributor experience** (item 1): issue templates (bug report, feature request, blank issues disabled), PR template with 4-item quality-gate checklist.
- `docs/decisions/` directory and ADR workflow established (`ADR-001` through `ADR-004`).
- `docs/20-flowdoc-ecosystem.md`: adopter guide for the combined FlowForge × FlowDoc stack.
- `GLOSSARY.md` + `GLOSSARY.es.md`: full terminology reference (EN + ES).

### Changed

- `docs/08-test-plan.md`: full English translation; phase guide uses `/flow-start` … `/flow-close` and `.ai-work/` paths.
- `docs/03-engram-dotnet-gaps.md`: rewritten as "implemented features / reference only" — removed "open gap" and "blocking" language.
- `docs/I18N.md`: items 08 and 03 marked Done (EN).
- Cursor agent files (`forge-arch.md`, `forge-plan.md`): synced with CKP-1 BLOCKER gate logic from skills.
- `CONTRIBUTING.md`: references issue templates, PR template, and CI smoke workflow.

### Fixed

- OpenCode bundle: avoid invalid `{file:...}` placeholder text inside JSON string values (can crash OpenCode config loader).
- OpenCode bundle: remove hardcoded local skill paths via repo-path placeholder patched at install time.
- CI `opencode-smoke.yml`: fix escaped `\$skill` in grep (was causing silent false-green on skill path validation); fix agent count (filter by `mode == "subagent"`); fix broken relative link in `CONTRIBUTING.md`.

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
