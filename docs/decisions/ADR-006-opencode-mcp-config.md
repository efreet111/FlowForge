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

