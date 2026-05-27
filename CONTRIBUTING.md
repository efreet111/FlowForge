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

## Security

See [`SECURITY.md`](SECURITY.md) for reporting vulnerabilities.
