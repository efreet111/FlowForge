# AGENTS — FlowForge (Antigravity)

Orquestación del flujo FlowForge de 6 fases con 5 checkpoints CKP-0→CKP-4.

## Reglas activas

Toda regla **requiere frontmatter YAML con `trigger:`** válido (`always_on`, `model_decision`, `glob`, `manual`). Sin `trigger:` (o con la clave inerte que usaba Cursor) Antigravity la descarta en silencio. Para `model_decision` el `description` es obligatorio.

- `.agents/rules/workflow.md` — Orquestador + checkpoints + rework intake (`trigger: always_on`)
- `.agents/rules/git-sin-push.md` — No push sin pedir (`trigger: always_on`)
- `.agents/rules/pr-workflow.md` — PR obligatorio a `main` (`trigger: always_on`)
- `.agents/rules/model-assignments.md` — Modelos por agente (`trigger: model_decision`)

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

**Agentes (subagentes nativos):** los 7 roles se instalan como subagentes en `.agents/agents/*.md` (workspace) y `~/.gemini/config/agents/*.md` (global). Sólo `flowforge` es `mainAgent: true`; el resto son `subagent: true` con `model: inherit`. Cada uno declara `name`, `description`, `subagent: true` y **no** declara `tools:` (un tool mal mapeado cuelga el subagente).

Skills en `.agents/skills/<name>/SKILL.md` (symlinks o copia desde `skills/<name>` del repo FlowForge), incluida `flowforge`. **No** uses `skills.json` — no es mecanismo soportado (ADR-011).

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

## Skills / slash commands

Los workflows legacy (`.agents/workflows/*.md`, `~/.gemini/config/global_workflows/*.md`, `~/.gemini/config/workflows/*.md`) **están deprecados desde el 2026-10-19**: Antigravity ya no los indexa ni los ejecuta con `/`. Los `/flow-*` se entregan como **skills**, generadas desde `ide/antigravity/workflows/*.md`.

| Comando | Skill (`.agents/skills/<name>/SKILL.md`) |
|---------|------------------------------------------|
| `/flow-start` | `flow-start` |
| `/flow-plan` | `flow-plan` |
| `/flow-dev` | `flow-dev` |
| `/flow-verify` | `flow-verify` |
| `/flow-close` | `flow-close` |
| `/flow-status` | `flow-status` |
| Bug / rework | `flow-rework` |

Cada `SKILL.md` generada lleva frontmatter YAML con `name:` y `description:` en **una sola línea** (sin `>` multilínea); sin eso el parser de `/` de Antigravity puede devolver 0 resultados. Las skills tienen precedencia sobre workflows legacy homónimos.

## Layout por contexto

### Instalación global (`flowforge install`, `ide/install.sh` o `ide/install.ps1`)

- **Linux:** `~/.gemini/config/` — `AGENTS.md`, `rules/`, `agents/`, `skills/` (incluye `flowforge` y las `flow-*`)
- **Windows:** `%USERPROFILE%\.gemini\config\` — mismo inventario
- Espejo workspace: `config/.agents/{rules,agents,skills}/`
- Always-on: `~/.gemini/GEMINI.md` (cuerpo de `rules/workflow.md`, **sin frontmatter**)
- MCP Engram: `config/mcp_config.json` — escrito por `flowforge install` (C#), no por scripts shell v1

**Legacy rechazado:** `~/.gemini/antigravity/` — Antigravity 2.0 no escanea esa ruta. El instalador la limpia al reinstalar. `config/workflows/` y `config/global_workflows/` también son legacy: se migran a skills y se eliminan.

### Instalación en proyecto (`flowforge init <ruta>`, `install.sh <ruta>` o `install.ps1 -ProjectPath`)

- `{repo}/.agents/AGENTS.md` — orquestador FlowForge
- `{repo}/AGENTS.md` — solo si no existía otro en la raíz
- `{repo}/.agents/rules/` — reglas locales (`trigger:` en frontmatter)
- `{repo}/.agents/agents/` — 7 subagentes nativos
- `{repo}/.agents/skills/` — skills metodología (incl. `flowforge`, `forge-discovery`) y las `flow-*` generadas

## Post-install

1. **Reiniciá Antigravity** (reload IDE) tras `flowforge install` o reinstalar — los `/flow-*` no aparecen hasta que el IDE reescanea `config/skills/`.
2. Si `/` no lista comandos `flow-*`: reload primero; luego `flowforge doctor` o `scripts/validate-antigravity-pack.sh` en el repo.
3. Para MCP/Engram completo usá `flowforge install` (no solo `ide/install.ps1`).

Ver [`docs/decisions/ADR-019-opencode-antigravity-customizations.md`](../../docs/decisions/ADR-019-opencode-antigravity-customizations.md) y [`ADR-008`](../../docs/decisions/ADR-008-ide-installer-path-matrix.md).

## Documentación

[FlowForge](https://github.com/efreet111/FlowForge)
