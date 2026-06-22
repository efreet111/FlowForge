# FlowForge — OpenCode

Orquestador primario: agente `flowforge` en `opencode.flowforge.json`.

## Subagentes

`forge-discovery`, `forge-arch`, `forge-plan`, `forge-dev`, `forge-verify`, `forge-memory` (mode: subagent, hidden).

## Reglas de orquestación

Cargá siempre el bloque de paridad compartido (mismo contrato que Cursor/Antigravity/VS Code):

`{file:./flowforge/shared/workflow-orchestrator-parity.md}` (tras `install.ps1` / `install.sh`)

Tras recibir handoff de forge-arch o forge-dev: leer `## Memory Signal` y aplicar
el Memory Curation Protocol (sección en el bloque de paridad compartido).

### Verdicts de forge-verify (4 estados)

Después de `/flow-verify`, leer `verify-report.md` bajo `.ai-work/{slug}/` y ramificar:
- **PASS** → ir a `/flow-close`
- **PASS_DEGRADADO** → no cerrar; pedir al humano que ejecute los tests
- **PENDING** → pausar; pedir instrucción al humano
- **REWORK** → si `status: "open"` en `rework_ticket.md` → `/flow-dev`; CKP-3 si `cycle_count ≥ 3`

### CKP-1 BLOCKER gate

Al mostrar `spec.md`, escanear Section 5. Si hay preguntas `[BLOCKER]`, no aceptar "ok"/"adelante" como aprobación — listar los BLOCKERs y esperar respuesta explícita.

## Comandos de flujo

`/flow-start`, `/flow-plan`, `/flow-dev`, `/flow-verify`, `/flow-close`, `/flow-status` — el orquestador delega; no implementa producto inline.

## Skills

Los subagentes cargan `skills/forge-*/SKILL.md` vía referencias `{file:ruta}` en `opencode.json`. Ajustá rutas absolutas a tu clone de FlowForge. **No uses el texto literal `{file:...}` en valores JSON** — OpenCode lo interpreta como ruta de archivo.
