# FlowForge — IDE Integration Files

This directory contains ready-to-use IDE integration files for Cursor, Antigravity, and VS Code.

## Quick Install

### Cursor
```bash
cp ide/cursor/rules/*.mdc ~/.cursor/rules/
cp ide/cursor/agents/*.md  ~/.cursor/agents/
```

### Antigravity (per-project)
```bash
mkdir -p .agents/rules .agents/workflows
cp ide/antigravity/rules/*.md .agents/rules/
cp ide/antigravity/workflows/*.md .agents/workflows/
cp ide/antigravity/AGENTS.md .
```

### VS Code / Copilot
```bash
mkdir -p .vscode
cp ide/vscode/copilot-instructions.md .vscode/
```

## What Each IDE Gets

| File | Cursor | Antigravity | VS Code |
|------|--------|-------------|---------|
| Workflow orchestrator | `rules/workflow.mdc` | `rules/workflow.md` | Embedded in copilot-instructions |
| Model assignments | `rules/model-assignments.mdc` | `rules/model-assignments.md` | Embedded |
| Git safety | `rules/git-sin-push.mdc` | `rules/git-sin-push.md` | Embedded |
| Agent instructions | `agents/forge-*.md` (6 files) | Referenced via workflow.md | Embedded |
| Slash commands | Via rules | `workflows/flow-*.md` (4 files) | N/A |

## Full Methodology

The install files are compilations. The complete methodology with all 31 specialized skills
lives in `skills/` and is documented in `docs/14-flowforge-complete-reference.md`.

For generating a consolidated rules file with all specialized skills, use the
Generador de Reglas (planned feature — TODO).
