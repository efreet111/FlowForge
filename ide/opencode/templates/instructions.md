# FlowForge — OpenCode

Orquestador primario: agente markdown `flowforge` en `agents/flowforge.md` más la configuración MCP en `opencode.json` (tipo `local`).

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

## FlowDoc Integration

Esta configuración sigue [FlowDoc v2.0](https://github.com/crhistianmdz/FlowDocs) como capa documental del proyecto FlowForge. La versión está pineada en `.flowforge.json` (`docs_framework: "flowdoc"` + `docs_framework_version: "2.0"`).

**Referencias clave en el repo FlowForge:**

- **Adopter Guide:** [`docs/20-flowdoc-ecosystem.md`](../../docs/20-flowdoc-ecosystem.md) — artifact boundaries, adoption levels L1-L4, mapeo CKP↔FlowDoc
- **Decisión de integración:** [`docs/decisions/ADR-004-flowdoc-integration.md`](../../docs/decisions/ADR-004-flowdoc-integration.md) — qué se toma de FlowDoc y qué no

**Reglas críticas de FlowDoc para OpenCode agents:**

1. `openspec/` está **prohibido** en proyectos FlowForge + FlowDoc. Usar siempre `.ai-work/{slug}/` para features en progreso.
2. El `AGENTS.md` del proyecto (raíz) debe ser corto (40-60 líneas) y referenciar `docs/`, no duplicar skills/CKPs.
3. Los HUs en `docs/tasks/HU-*.md` se enlazan a `.ai-work/{slug}/` vía `flowforge_slug` frontmatter.

## Skills (importante — no están en el pack OpenCode)

FlowForge tiene **dos capas** en el repo:

| Capa | Carpeta | Qué es |
|------|---------|--------|
| **Skills** (metodología) | `skills/forge-*/SKILL.md` | Fuente de verdad: checkpoints, Ralph loop, PM-*, etc. |
| **Agents IDE** | `ide/opencode/agents/*.md` | Empaquetado OpenCode: orquestador + subagentes |

El instalador **solo copia agents + commands**. **No** crea `.opencode/skills/` ni copia `skills/` a `$HOME/.config/opencode/`.

Los agents dicen al modelo que cargue skills **por ruta relativa** al repo:

```text
skills/forge-dev/SKILL.md
```

Eso funciona si OpenCode arranca en la **raíz del clone FlowForge**. En otros proyectos, los skills no están disponibles salvo rutas absolutas en `opencode.json` o abrir el repo FlowForge como workspace.

Comparación con otros IDEs:

| IDE | Cómo accede a `skills/` |
|-----|-------------------------|
| **Cursor** | Embebidos en `ide/cursor/agents/` (compilados) |
| **Antigravity** | Symlinks en `~/.gemini/config/skills/forge-*` |
| **OpenCode** | Solo referencia textual; archivo en `{repo}/skills/` |

Documentación completa: [`docs/decisions/ADR-009-opencode-antigravity-customizations.md`](../../docs/decisions/ADR-009-opencode-antigravity-customizations.md).

### Referencias MCP / file (legacy)

Los subagentes pueden usar `{file:ruta}` en `opencode.json` para rutas absolutas al clone. Ajustá rutas a tu máquina. **No uses el texto literal `{file:...}` como string suelto** — OpenCode lo interpreta como ruta de archivo.

## Estructura de agentes

### Instalación global

- `$HOME/.config/opencode/agents/` — Markdown de los agentes FlowForge
- `$HOME/.config/opencode/commands/` — Comandos opcionales (ya no se mezclan desde `flowforge/`)
- `opencode.json` / `opencode.jsonc` (tipo `local`) — contiene `mcp.engram` generado por FlowForge o `ide/install.sh`; conserva tus bloques `mcp`, `permission`, `provider`.
- `$HOME/.config/opencode/flowforge/` + `opencode.flowforge.json` se mantienen solo para migraciones y no forman parte de la instalación primaria.

### Instalación por proyecto

- `$HOME/.opencode/agents/` — copia local de los agentes
- `$HOME/.opencode/commands/` — comandos `/flow-*` del proyecto
- `$HOME/.kilo/agents/` — duplicado inmediato de `.opencode/agents/` para compatibilidad con Kilo Code

### Nota legacy

`opencode.flowforge.json` ya no es la ruta principal. FlowForge escribe directamente en `$HOME/.config/opencode/agents/` y en `opencode.json` (tipo `local`). Conservá el archivo legacy solo para migraciones históricas y evita merges que reemplacen los bloques `mcp`/`permission`.
