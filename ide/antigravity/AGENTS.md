# AGENTS — FlowForge (Antigravity)

Orquestación del flujo FlowForge de 6 fases con 5 checkpoints CKP-0→CKP-4.

## Reglas activas

- `.agents/rules/workflow.md` — Orquestador + checkpoints + rework intake (`alwaysApply: true`)
- `.agents/rules/model-assignments.md` — Modelos por agente
- `.agents/rules/git-sin-push.md` — No push sin pedir

Paridad compartida (todos los IDEs): `ide/shared/workflow-orchestrator-parity.md` en el repo FlowForge.

## Agentes (7 roles)

| Agente | Fase | Cuándo |
|--------|------|--------|
| forge-discovery | 0 | Nueva feature: memorias, CVEs, compliance |
| forge-arch | 1 | spec.md + PM-* |
| forge-plan | 2 | plan.md + checklist |
| forge-dev | 3 | Código + tests; marca checklist; prioridad rework_ticket |
| forge-verify | 3b | verify-report.md o rework_ticket |
| forge-memory | 4 | Cierre; bloquea si PM-* pendientes |

Skills en `.agents/skills/forge-*/SKILL.md` (symlinks o copia desde `skills/forge-*` del repo FlowForge). **No** uses `skills.json` — no es mecanismo soportado (ADR-011).

## Artefactos

```
.ai-work/{feature-slug}/
├── context-map.md
├── spec.md
├── plan.md
├── verify-report.md
├── rework_ticket.md
└── summary.md
```

## Workflows

| Comando | Archivo |
|---------|---------|
| `/flow-start` | `.agents/workflows/flow-start.md` |
| `/flow-plan` | `.agents/workflows/flow-plan.md` |
| `/flow-dev` | `.agents/workflows/flow-dev.md` |
| `/flow-verify` | `.agents/workflows/flow-verify.md` |
| `/flow-close` | `.agents/workflows/flow-close.md` |
| `/flow-status` | `.agents/workflows/flow-status.md` |
| Bug / rework | `.agents/workflows/flow-rework.md` |

Los workflows **requieren frontmatter YAML** con `description:` en **una sola línea** (sin `>` multilínea); sin eso el parser de `/` de Antigravity puede devolver 0 resultados.

## Layout por contexto

### Instalación global (`flowforge install`, `ide/install.sh` o `ide/install.ps1`)

- **Linux:** `~/.gemini/config/` — `AGENTS.md`, `rules/`, `global_workflows/`, `skills/forge-*/`
- **Windows:** `%USERPROFILE%\.gemini\config\` — mismo inventario
- Espejo workspace: `config/.agents/{rules,workflows,skills}/`
- Always-on: `~/.gemini/GEMINI.md` (copia de `rules/workflow.md`)
- MCP Engram: `config/mcp_config.json` — escrito por `flowforge install` (C#), no por scripts shell v1

**Legacy rechazado:** `~/.gemini/antigravity/` — Antigravity 2.0 no escanea esa ruta. El instalador la limpia al reinstalar.

### Instalación en proyecto (`flowforge init <ruta>`, `install.sh <ruta>` o `install.ps1 -ProjectPath`)

- `{repo}/.agents/AGENTS.md` — orquestador FlowForge
- `{repo}/AGENTS.md` — solo si no existía otro en la raíz
- `{repo}/.agents/rules/` — reglas locales
- `{repo}/.agents/workflows/` — workflows `/flow-*` con frontmatter
- `{repo}/.agents/skills/forge-*/` — skills metodología (incl. `forge-discovery`)

## Post-install

1. **Reiniciá Antigravity** (reload IDE) tras `flowforge install` o reinstalar — los `/flow-*` no aparecen hasta que el IDE reescanea `config/global_workflows/`.
2. Si `/` no lista comandos `flow-*`: reload primero; luego `flowforge doctor` o `scripts/validate-antigravity-pack.sh` en el repo.
3. Para MCP/Engram completo usá `flowforge install` (no solo `ide/install.ps1`).

Ver [`docs/decisions/ADR-011-opencode-antigravity-customizations.md`](../../docs/decisions/ADR-011-opencode-antigravity-customizations.md) y [`ADR-008`](../../docs/decisions/ADR-008-ide-installer-path-matrix.md).

## Documentación

[FlowForge](https://github.com/efreet111/FlowForge)
