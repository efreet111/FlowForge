# FlowForge — IDE Integration Files

This directory contains ready-to-use IDE integration files for **OpenCode, Cursor, Antigravity, and VS Code**.

## Quick Install

### OpenCode (nativo)
```bash
# Mergear los agentes FlowForge en tu opencode.json existente
# Copiá las entradas de 'agent' de ide/opencode/opencode.flowforge.json
# en tu ~/.config/opencode/opencode.json (dentro del objeto "agent")

# O usarlo como referencia para crear los subagentes manualmente.
# Los prompts cargan los archivos SKILL.md directamente desde el repo FlowForge.
```

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

| Component | OpenCode | Cursor | Antigravity | VS Code |
|-----------|----------|--------|-------------|---------|
| Workflow orchestrator | `opencode.flowforge.json` (subagents) | `rules/workflow.mdc` | `rules/workflow.md` | Embedded in copilot-instructions |
| Model assignments | Inline in agent config | `rules/model-assignments.mdc` | `rules/model-assignments.md` | Embedded |
| Git safety | Via `permission.bash` in opencode.json | `rules/git-sin-push.mdc` | `rules/git-sin-push.md` | Embedded |
| Agent instructions | `{file:...}` references to `skills/*/SKILL.md` | `agents/forge-*.md` (6 files) | Referenced via workflow.md | Embedded |
| Slash commands | Via AGENTS.md or custom commands | Via rules | `workflows/flow-*.md` (4 files) | N/A |
| MCP integration | Native engram MCP | N/A | N/A | N/A |

## OpenCode — Detalle

FlowForge en OpenCode usa **6 subagentes** (mode: "subagent", hidden: true) más 1 opcional (teacher):

| Subagent | Modelo recomendado | Skills que carga |
|----------|-------------------|-----------------|
| `forge-discovery` | deepseek-v4-flash | core + security + compliance + cost |
| `forge-arch` | deepseek-v4-pro | core + security + performance + a11y + domain |
| `forge-plan` | qwen3.5-plus | core + security + patterns + migrations + rollback |
| `forge-dev` | qwen3.5-plus | core + security + solid + testing + performance + refactor |
| `forge-verify` | deepseek-v4-pro | core + security + complexity + performance + a11y |
| `forge-memory` | deepseek-v4-flash | core + metrics + changelog + knowledge |
| `forge-teacher` | deepseek-v4-flash | teacher (toggleable) |

Los modelos son los disponibles en tu proveedor `opencode-go/`. Ajustalos a los que tengas contratados.

## Full Methodology

The install files are compilations. The complete methodology with all **31 skills**
lives in `skills/` and is documented in `docs/14-flowforge-complete-reference.md`.

For generating a consolidated rules file with all specialized skills, use the
Generador de Reglas (planned feature — TODO).
