---
hu_id: HU-040
title: "MIGRATION: OpenCode v2.0.29 plugin API V1→V2 breaking change"
status: done
category: maintenance
flowforge_slug: "migrate-plugins-v1-to-v2"
priority: P0
severity: critical
created_date: 2026-10-14
---

# HU-040 — MIGRATION: OpenCode v2.0.29 plugin API V1→V2 breaking change

## User Story

As a FlowForge operator,
I want all OpenCode plugins to load and function correctly after the v2.0.29 upgrade,
so that FlowForge continues to operate with full functionality (memory persistence, background agents, model variants).

## Problem Description

OpenCode v2.0.29 introduced a **breaking change** in the plugin API: migrating from V1 to V2. The three local plugins in `~/.config/opencode/plugins/` and two external plugins fail to load with the error:

```
Plugin must export a default definition with an id and an effect or setup function.
SchemaError: Expected object at ["default"]
SchemaError: Missing key at ["default"]
```

### Affected Plugins

| Plugin | File | Error |
|--------|------|-------|
| `background-agents` | `plugins/background-agents.ts` | `SchemaError: Expected object at ["default"]` |
| `engram` | `plugins/engram.ts` | `SchemaError: Missing key at ["default"]` |
| `model-variants` | `plugins/model-variants.ts` | `SchemaError: Expected object at ["default"]` |
| `opencode-subagent-statusline` | npm external | `NpmInstallFailedError` |
| `opencode-sdd-engram-manage` | npm external | `NpmInstallFailedError` |

### Functional Impact

| Plugin | Functionality Lost |
|--------|-------------------|
| `background-agents` | `delegate()`, `delegation_list()`, `delegation_read()` — async delegation system |
| `engram` | Memory persistence: `mem_save()`, `mem_search()`, session capture, system prompt injection |
| `model-variants` | Model variant cache generation |
| External plugins | Status line, SDD engram management |

**Note:** The MCP Engram server continues to run (connected on port 7437), but the plugin that bridges OpenCode to it does not load. Tools `mem_*` are unavailable to the agent.

## Root Cause

OpenCode v2.0.29 requires plugins to use the **V2 Plugin API** instead of the legacy V1 format.

### V1 Format (BROKEN)

```typescript
import type { Plugin } from "@opencode-ai/plugin"

export const PluginName: Plugin = async (ctx) => {
  return {
    tool: { ... },
    "experimental.chat.system.transform": async (_input, output) => { ... },
    "experimental.session.compacting": async (input, output) => { ... },
  }
}
export default PluginName
```

### V2 Format (REQUIRED)

```typescript
import { Plugin } from "@opencode/plugin"

export default Plugin.define({
  id: "plugin-name",
  async setup(ctx) {
    // Tools via ctx.tool.transform()
    // Hooks via ctx.session.hook() and domain-specific methods
    return () => {
      // cleanup optional
    }
  }
})
```

### Hook Migration Reference

| V1 Hook (Legacy) | V2 Equivalent | Domain |
|------------------|---------------|--------|
| `experimental.chat.system.transform` | `ctx.session.hook("context", ...)` | Session |
| `experimental.session.compacting` | `ctx.session.hook("compaction", ...)` | Session |
| `tool.execute.after` | `ctx.tool.hook("after", ...)` | Tool |
| `event` (async iterable) | `ctx.event.subscribe({ signal })` | Event |

### Tool Migration Reference

```typescript
// V1
return {
  tool: {
    myTool: tool({ ... })
  }
}

// V2
await ctx.tool.transform((editor) => {
  editor.add({
    name: "myTool",
    description: "...",
    input: { type: "object", properties: {...} },
    async execute(input, context) {
      return { content: "..." }
    }
  })
})
```

## Acceptance Criteria

- [x] AC-1: `engram.ts` loads successfully in OpenCode v2.0.29
- [x] AC-2: `background-agents.ts` loads successfully in OpenCode v2.0.29
- [x] AC-3: `model-variants.ts` loads successfully in OpenCode v2.0.29
- [x] AC-4: `opencode plugin list` shows all 3 plugins as loaded
- [x] AC-5: Memory instructions are injected into system prompt (verify with test prompt)
- [x] AC-6: Session events (`session.created`, `session.deleted`) are captured by engram plugin
- [x] AC-7: Tool hooks (`tool.execute.after`) function correctly
- [x] AC-8: External plugins (`opencode-subagent-statusline`, `opencode-sdd-engram-manage`) migrated to V2 via HU-041
  ✅ **RESOLVED:** Both plugins migrated to V2 format and load successfully (HU-041)

## Scenarios (GWT)

### Happy Path

- [x] **Plugin loads without errors**
  **GIVEN** OpenCode v2.0.29 is running
  **WHEN** user starts a new session
  **THEN** all 3 local plugins load successfully (no SchemaError in logs)

- [x] **Engram memory integration works**
  **GIVEN** a session is active
  **WHEN** user prompts FlowForge to remember something
  **THEN** `mem_save` is called and the information persists across sessions

- [x] **System prompt contains memory instructions**
  **GIVEN** a new session starts
  **WHEN** the system prompt is built
  **THEN** the Engram memory protocol instructions are injected

### Edge Cases

- [ ] **Plugin fails gracefully**
  **GIVEN** a plugin has a runtime error during setup
  **WHEN** the error is caught
  **THEN** OpenCode continues to operate without crashing

- [ ] **Version compatibility**
  **GIVEN** OpenCode is downgraded to v2.0.28
  **WHEN** plugins are loaded
  **THEN** V1 plugins still work (backwards compatible if possible)

## Files to Migrate

| File | Complexity | Priority |
|------|------------|----------|
| `~/.config/opencode/plugins/model-variants.ts` | Low (no hooks, passive) | 1st |
| `~/.config/opencode/plugins/engram.ts` | High (hooks + events + tools) | 2nd |
| `~/.config/opencode/plugins/background-agents.ts` | High (tools + hooks + delegation) | 3rd |

## Technical Notes

### API Version Mismatch

| Location | `@opencode-ai/plugin` version |
|----------|-------------------------------|
| Global (`~/.config/opencode/`) | `^1.18.22` |
| Project FlowForge (`.opencode/`) | `1.17.15` |

The plugin files import from `@opencode-ai/plugin` but the new API uses `@opencode/plugin` (note: no `-ai-`). This import path change is part of the V2 migration.

### References

- [OpenCode V2 Plugins Docs](https://opencode.ai/v2/docs/build/plugins)
- [Migración V1 → V2](https://opencode.ai/v2/docs/build/plugins/migrate-v1)
- [Hooks Reference](https://opencode.ai/v2/docs/build/plugins#hooks)
- [Session Hooks](https://opencode.ai/v2/docs/build/plugins#session-hooks)
- Audit report: `reportes/2026-10-14_Analisis-Plugins-OpenCode-v2.0.29.md`

## Owner & Timeline

- **Owner**: Unassigned
- **Priority**: P0 (critical — FlowForge non-functional without memory)
- **Target milestone**: Immediate (no workaround exists)
- **Dependencies**: None (blocking all FlowForge operations)

## Resolution (2026-10-14)

**Completed:**
- ✅ Plugins migrated to V2: `engram.ts`, `background-agents.ts`, `model-variants.ts`
- ✅ MCP config fixed: added `servers` nesting level (V2 format requirement)
- ✅ All 3 plugins load successfully (`opencode plugin list` verified)
- ✅ MCP engram + obsidian both connected
- ✅ Memory tools functional (tested with `engram save` CLI)

**Pending (AC-8):**
- External npm plugins installed but NOT loading — they use V1 plugin format (not V2)
- `opencode-subagent-statusline` exports V1 format: `async () => { event: ... }`
- `opencode-sdd-engram-manage` likely same issue
- **See HU-041** for migration of these external plugins

## Definition of Done

- [x] All 3 local plugins migrated to V2 format and load without errors
- [x] `opencode plugin list` confirms all plugins loaded
- [x] Functional test: new session shows memory instructions in system context
- [x] Functional test: `mem_save` call succeeds and persists
- [ ] External plugins reinstalled (AC-8 - pending V1→V2 migration of npm plugins)
- [ ] CHANGELOG entry added in FlowForge project

## FlowForge

> This section is managed by FlowForge agents. Do not edit manually.

To implement this HU:

```bash
/flow-start migrate-plugins-v1-to-v2
# HU file: HU-040-migrate-plugins-v1-to-v2.md
```

- `flowforge_slug`: `migrate-plugins-v1-to-v2`
- HU status lifecycle: `draft` → `in-progress` → `done`
