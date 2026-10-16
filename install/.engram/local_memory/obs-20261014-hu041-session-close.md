---
title: "HU-041: migrate-npm-plugins-v1-to-v2 session close"
type: "session_summary"
topic_key: "session/hu-041"
date: "2026-10-14"
scope: "team"
project: "FlowForge"
---

## What

Migración V1→V2 de 2 plugins npm (subagent-statusline y sdd-engram-manage) completada. Ambos plugins cargan correctamente según `opencode plugin list`.

## Why

HU-040 definió el patrón V2 para 3 plugins locales; HU-041 completó la migración de los 2 plugins npm restantes a local TypeScript.

## Where

- `~/.config/opencode/plugins/subagent-statusline.ts` (~1200 líneas)
- `~/.config/opencode/plugins/sdd-engram-manage.ts` (~4000 líneas)
- `~/.config/opencode/package.json` (deps npm removidas)

## Learned

1. **CLI Plugin API** (`@opencode/plugin/tui`) es el equivalente V2 para TUI plugins — provee `ctx.ui.dialog`, `ctx.keymap.layer()`, `ctx.slots.register()`, `ctx.kv.get/set`
2. **Patrón api→ctx rename** aplica universalmente a todos los plugins V1
3. **SolidJS compiled JSX globals** funcionan sin cambios en V2 (runtime globals)
4. **BLOCKER resuelto**: OQ-[BLOCKER] sobre TUI API v2 desconocido fue resuelto encontrando `@opencode/plugin/tui`

## PM Status

| PM | Resultado |
|----|-----------|
| PM-1 | ✅ PASSED |
| PM-2..PM-5 | ⏳ Pending (runtime) |

## Technical Decisions Captured

1. CLI Plugin API provee todas las APIs TUI necesarias
2. api→ctx rename es el patrón de migración universal
3. SolidJS compiled JSX globals no requieren cambios

---
*Session close — engram MCP no disponible, guardado en buffer local*
