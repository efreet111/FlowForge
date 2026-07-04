# AGENTS — FlowForge (Antigravity)

Orquestación del flujo FlowForge de 6 fases con 5 checkpoints CKP-0→CKP-4.

## Reglas activas

- `.agents/rules/workflow.md` — Orquestador + checkpoints + rework intake
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

Skills en `skills/` — cada agente carga on-demand (seguridad, SOLID, etc.).

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
| `/flow-plan` | (delegar forge-plan vía workflow.md) |
| `/flow-dev` | `flow-dev.md` |
| `/flow-verify` | `flow-verify.md` |
| `/flow-close` | `flow-close.md` |
| Bug / rework | `flow-rework.md` |

## Layout por contexto

### Instalación global (`flowforge install` o `ide/install.sh`)

- `~/.gemini/antigravity/AGENTS.md` — archivo raíz del orquestador (flowforge + hooks)
- `~/.gemini/antigravity/rules/*.md` — reglas/estados (workflow, model-assignments, git-sin-push, etc.)
- `~/.gemini/antigravity/workflows/*.md` — implementaciones de `/flow-*`
- `~/.gemini/antigravity/mcp_config.json` — configuración MCP para Engram (tipo cursor + mcpServers)

### Instalación en proyecto (`flowforge init <ruta>` o `install.sh <ruta>`)

- `{repo}/AGENTS.md` — orquestador del proyecto en la raíz del repositorio
- `{repo}/.agents/rules/` — reglas locales (`workflow.md`, `model-assignments.md`, `git-sin-push.md`)
- `{repo}/.agents/workflows/` — workflows `/flow-start`, `/flow-plan`, `/flow-rework`, etc.

FlowForge mantiene la paridad entre la instalación global y cada repo. Para mayor detalle, consultá [`docs/decisions/ADR-008-ide-installer-path-matrix.md`](../../docs/decisions/ADR-008-ide-installer-path-matrix.md).

## Documentación

[FlowForge](https://github.com/efreet111/FlowForge)
