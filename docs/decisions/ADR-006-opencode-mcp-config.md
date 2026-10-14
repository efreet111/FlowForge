---
adr: 006
title: "OpenCode MCP: usar transport 'local' y habilitar explícitamente"
date: 2026-06-29
status: accepted
authors:
  - victor
---

# ADR-006: OpenCode MCP — usar transport 'local' y `enabled: true`

## Context
Durante el desarrollo y pruebas del feature `fix-installer` se detectó que OpenCode no se iniciaba correctamente cuando la entrada MCP generada usaba `type: "stdio"` o no incluía un campo `enabled: true`. Esto provocaba fallos silenciosos al arrancar agentes OpenCode en instalaciones locales.

## Decision
Normalizar la entrada MCP para OpenCode de la siguiente forma:

- `type: "local"`
- `enabled: true`
- Incluir en la documentación del instalador que la configuración MCP para OpenCode debe contener `enabled: true` y ejemplos de `opencode.flowforge.json`.

## Consequences
- OpenCode detectará y cargará agentes locales de forma fiable en instalaciones producidas por el instalador.
- Esta decisión requiere que el instalador actualice la plantilla MCP generada y que las pruebas CI verifiquen la validez de la entrada OpenCode.
- Los usuarios que tenían configuraciones personalizadas deberán migrar si usan transportes no compatibles.

## Implementation notes
- Cambios realizados en `McpOpenCodeEntry` y `MergeOpenCodeMcp` para emitir `type: "local"` y `enabled: true`.
- Ver `src/FlowForge.Installer/...` y tests de integración.

## Follow-up
- Añadir pruebas unitarias/CI que validen `opencode.flowforge.json` y la presencia de `enabled: true`.
- Documentar en `README.md` y `README.es.md` la configuración mínima requerida para OpenCode.
- Aclarar que FlowForge instala markdown agents en `~/.config/opencode/agents/` (y `.opencode/agents/` / `.kilo/agents/` por proyecto) y que `opencode.flowforge.json` es un helper legacy, no la ruta principal.

## Update (2026-10-14) — context7 requirement for non-local profiles

**IMPORTANT**: When installing FlowForge with a **non-local Engram profile** (sync/remote mode), OpenCode requires the `context7` MCP server as a prerequisite for remote MCP connections to work.

### Why context7?
OpenCode v2 requires context7 as a transport layer for remote MCP servers. Without it, remote MCP tools will fail with:
```
No tool named "tools.*" is currently available.
```

### Installation
The installer automatically detects non-local profiles and adds context7. This applies to **both installers**:
- C# installer: `flowforge install --ide opencode`
- Bash installer: `bash ide/install.sh`

**Profile detection logic** (in priority order):
1. `ENGRAM_SERVER_URL` environment variable is set
2. `ENGRAM_PROFILE` environment variable is not `"local"` (e.g., `sync`)
3. `~/.engram/config.json` contains a `sync` configuration

If any condition is met AND OpenCode is selected as an IDE, context7 is automatically installed with:
```bash
opencode mcp add context7 --url https://mcp.context7.com/mcp --global
```

**Idempotency**: The installer checks `opencode mcp list` before adding to prevent duplicate entries.

**Graceful failure**: If installation fails, the installer logs a warning and continues. Manual fix instructions are provided in the troubleshooting output.

See [HU-042](docs/tasks/HU-001-HU-099/HU-042-context7-mcp-auto-install.md) for implementation details.

**User-facing documentation**: See [POST-INSTALL.md §2](POST-INSTALL.md#2-decide-your-sync-mode) for troubleshooting steps and manual installation instructions.

