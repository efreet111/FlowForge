# FlowForge — OpenCode

Orquestador primario: agente `flowforge` en `opencode.flowforge.json`.

## Subagentes

`forge-discovery`, `forge-arch`, `forge-plan`, `forge-dev`, `forge-verify`, `forge-memory` (mode: subagent, hidden).

## Reglas de orquestación

Cargá siempre el bloque de paridad compartido (mismo contrato que Cursor/Antigravity/VS Code):

`{file:./flowforge/shared/workflow-orchestrator-parity.md}` (tras `install.ps1` / `install.sh`)

Tras recibir handoff de forge-arch o forge-dev: leer `## Memory Signal` y aplicar
el Memory Curation Protocol (sección en el bloque de paridad compartido).

## Comandos de flujo

`/flow-start`, `/flow-plan`, `/flow-dev`, `/flow-verify`, `/flow-close`, `/flow-status` — el orquestador delega; no implementa producto inline.

## Skills

Los subagentes cargan `skills/forge-*/SKILL.md` vía referencias `{file:ruta}` en `opencode.json`. Ajustá rutas absolutas a tu clone de FlowForge. **No uses el texto literal `{file:...}` en valores JSON** — OpenCode lo interpreta como ruta de archivo.
