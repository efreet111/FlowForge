# FlowForge — IDE Integration Files

This directory contains ready-to-use IDE integration files for **OpenCode, Cursor, Antigravity, and VS Code**.

> Para onboarding en español: [`../QUICKSTART.md`](../QUICKSTART.md) y [`../docs/14-flowforge-complete-reference.md`](../docs/14-flowforge-complete-reference.md).

## Paridad entre IDEs (v0.4)

Fuente compartida del orquestador (todos los IDEs deben alinearse a esto):

```text
ide/shared/workflow-orchestrator-parity.md
```

| Regla | Cursor | Antigravity | VS Code | OpenCode |
|-------|--------|-------------|---------|----------|
| Orquestador no codea producto | `cursor/rules/workflow.mdc` | `antigravity/rules/workflow.md` | `vscode/agents/forge-orchestrator.agent.md` | `opencode` + shared file |
| Rework intake → `rework_ticket.md` | ✅ | ✅ + `workflows/flow-rework.md` | ✅ handoff Fix Rework | ✅ en prompt |
| PM-* gate en cierre | ✅ | ✅ | ✅ | ✅ (skills + parity) |
| `verify-report.md` (no cert-report) | ✅ | ✅ | ✅ | ✅ |
| `.ai-work/{slug}/` kebab-case | ✅ | ✅ | ✅ | ✅ |
| Dev marca checklist plan | skills + agents compilados | skills on-demand | `.github/agents` | `{file:skills}` |

**Regenerar agentes Cursor** tras cambiar skills:

```bash
python ide/cursor/compile-agents-from-skills.py
```

## Quick Install (recomendado)

### Windows (PowerShell)
```powershell
cd ide
.\install.ps1

# Por proyecto (Antigravity + .cursor + .github/agents):
.\install.ps1 -ProjectPath "E:\Proyectos\mi-app"
```

### Linux / macOS
```bash
cd ide
bash install.sh

# Por proyecto:
bash install.sh /path/to/mi-app
```

El instalador:

1. Copia `ide/shared/` → `~/.flowforge/shared/` (paridad orquestador)
2. Recompila agentes Cursor desde `skills/` (si hay Python)
3. Instala Cursor (`~/.cursor`), OpenCode (`~/.config/opencode/flowforge/`), VS Code (`~/.vscode`)
4. Con ruta de proyecto: `.agents/`, `.cursor/`, `.github/agents/`, `.flowforge/shared/`

### Manual (si preferís)

**OpenCode:** mergeá `agent{}` de `opencode.flowforge.json` en `~/.config/opencode/opencode.json`. El bundle en `flowforge/` usa `{file:./flowforge/shared/workflow-orchestrator-parity.md}`.

**Cursor:**
```bash
cp ide/cursor/rules/*.mdc ~/.cursor/rules/
cp ide/cursor/agents/*.md  ~/.cursor/agents/
cp ide/cursor/commands/*.md ~/.cursor/commands/
```

**Antigravity (por proyecto):** ver `install.sh <proyecto>` o copiar `ide/antigravity/` a `.agents/`.

**VS Code:** `install.sh <proyecto>` copia a `.github/agents/` y `.vscode/copilot-instructions.md`.

## What Each IDE Gets

| Component | OpenCode | Cursor | Antigravity | VS Code |
|-----------|----------|--------|-------------|---------|
| Workflow orchestrator | `opencode.flowforge.json` (subagents) | `rules/workflow.mdc` | `rules/workflow.md` | Embedded in copilot-instructions |
| Model assignments | Inline in agent config | `rules/model-assignments.mdc` | `rules/model-assignments.md` | Embedded |
| Git safety | Via `permission.bash` in opencode.json | `rules/git-sin-push.mdc` | `rules/git-sin-push.md` | Embedded |
| Agent instructions | `{file:...}` references to `skills/*/SKILL.md` | `agents/forge-*.md` (6 files) | Referenced via workflow.md | Embedded |
| Shared parity doc | `../shared/workflow-orchestrator-parity.md` | copy ref in workflow | in copilot + orchestrator | `{file:../shared/...}` |
| Slash commands | Via rules / `.cursor/commands` | `workflows/flow-*.md` (6) | handoffs + chat | AGENTS.md |
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
