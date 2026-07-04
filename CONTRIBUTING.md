# Contributing to FlowForge

Thank you for improving FlowForge. This repository contains **methodology**, **skills**, and **IDE integration** packs (Cursor, OpenCode, Antigravity, VS Code).

## Getting started

- Read [`QUICKSTART.md`](QUICKSTART.md) first.
- Full system reference (checkpoints, phases, test cases): [`docs/14-flowforge-complete-reference.md`](docs/14-flowforge-complete-reference.md).
- IDE files: [`ide/README.md`](ide/README.md).
- Spanish overview: [`README.es.md`](README.es.md).

## What we accept

- **Docs**: fixes, clarifications, examples, troubleshooting.
- **Installers**: robustness for `ide/install.ps1` and `ide/install.sh` (detection, backups, `-ProjectPath`).
- **IDE integrations**: `ide/cursor/`, `ide/opencode/`, `ide/antigravity/`, `ide/vscode/`.
- **Skills**: improvements to `skills/**/SKILL.md` (keep CKP semantics and orchestrator parity consistent).

## IDE pack paths

FlowForge installs each IDE bundle into the directories listed below. Matching docs and CI must keep this table in sync with [`docs/decisions/ADR-008-ide-installer-path-matrix.md`](docs/decisions/ADR-008-ide-installer-path-matrix.md).

| IDE | Global | Project |
|-----|--------|---------|
| Cursor | `~/.cursor/agents/`, `~/.cursor/rules/`, `~/.cursor/commands/` | `.cursor/agents/`, `.cursor/rules/`, `.cursor/commands/` |
| OpenCode | `~/.config/opencode/agents/`, `~/.config/opencode/commands/` | `.opencode/agents/` + `.kilo/agents/` |
| GitHub Copilot | `~/.copilot/agents/*.agent.md`, `~/.copilot/instructions/flowforge.instructions.md` | `.github/agents/*.agent.md`, `.github/copilot-instructions.md` |
| Kilo Code | `~/.config/kilo/agents/*.md` | `.kilo/agents/*.md` |
| Antigravity | `~/.gemini/antigravity/` (`AGENTS.md`, `rules/`, `workflows/`) | `.agents/rules/`, `.agents/workflows/`, `AGENTS.md` |

## Issues

Please include:

- **OS**: Windows / macOS / Linux (WSL if applicable).
- **IDE**: Cursor / OpenCode / Antigravity / VS Code.
- **Install mode**: remote (`raw.githubusercontent.com`) vs local clone.
- **Logs**: full installer output and exact reproduction steps.

## Pull requests

### Principles

- **Small, reviewable PRs** — one concern per PR.
- **No contradictions**: if you change checkpoints, align `README.md`, `QUICKSTART.md`, and `docs/14-*`.
- **Do not break onboarding** — entry points are `README.md` and `QUICKSTART.md`.

### Conventions

- **Language**: public docs are **English**; Spanish entry is [`README.es.md`](README.es.md). Track progress in [`docs/I18N.md`](docs/I18N.md).
- **Paths**: Antigravity standard is `.agents/` (not `.agent/`).
- **Artifacts**: `.ai-work/{feature-slug}/`, `verify-report.md` (not `cert-report.md`).

## Issue Templates

When reporting issues, **please use our templates**:

- **Bug Report** — for bugs: include OS, IDE, install mode, reproduction steps, expected vs actual behavior
- **Feature Request** — for enhancements: describe the problem, proposed solution, alternatives

Blank issues are disabled. Use a template to ensure maintainers get the context they need.

## PR Template

All pull requests should use our [PR template](.github/PULL_REQUEST_TEMPLATE.md):

- Description (2-5 sentences)
- Related issue (if any)
- **Checklist** — all items must be checked before merge:
  - Principles respected (small PRs, no contradictions)
  - Conventions followed (English for docs, `.agents/` for Antigravity)
  - Tests included (npm test / dotnet test passes)
  - PM manual tests (PM-* from spec.md marked if applicable)
- Breaking changes (yes/no)

## CI Smoke Test

Every PR runs the **OpenCode Smoke** workflow (`.github/workflows/opencode-smoke.yml`):

- Validates `opencode.flowforge.json` is valid JSON
- Checks all 7 subagents exist
- Verifies each skill path resolves to a real file
- Ensures no unresolved placeholders remain

The CI completes in < 2 minutes.

## Security

See [`SECURITY.md`](SECURITY.md) for reporting vulnerabilities.
