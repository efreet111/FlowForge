---
hu_id: HU-041
title: "MIGRATION: npm plugins opencode-subagent-statusline and opencode-sdd-engram-manage from V1 to V2"
status: closed
category: maintenance
flowforge_slug: "migrate-npm-plugins-v1-to-v2"
priority: P1
severity: medium
created_date: 2026-10-14
completed_date: 2026-10-14
---

# HU-041 — MIGRATION: npm plugins V1→V2

## User Story

As a FlowForge operator,
I want the npm plugins (`opencode-subagent-statusline`, `opencode-sdd-engram-manage`) to load correctly in OpenCode v2.0.29,
so that the statusline and SDD management features work as expected.

## Problem Description

These plugins were installed via npm but fail to load in OpenCode v2.0.29 because they use the **V1 plugin format**:

```javascript
// Current V1 format (BROKEN in v2.0.29)
var SubagentStatusline = async () => {
  return { event: async ({ event }) => { ... } };
};
export { SubagentStatusline };
```

OpenCode v2.0.29 requires **V2 format**:

```typescript
// Required V2 format
export default Plugin.define({
  id: "plugin-name",
  async setup(ctx) {
    // Events via ctx.event.subscribe({ signal })
    // Hooks via ctx.session.hook() etc.
    return () => { /* cleanup */ }
  }
})
```

## Affected Plugins

| Plugin | File | Current Format |
|--------|------|---------------|
| `opencode-subagent-statusline` | `~/.config/opencode/node_modules/opencode-subagent-statusline/dist/index.js` | V1 |
| `opencode-sdd-engram-manage` | `~/.config/opencode/node_modules/opencode-sdd-engram-manage/` | V1 (to verify) |

## Root Cause

Same root cause as HU-040: OpenCode v2.0.29 introduced breaking changes in the plugin API. These plugins were not updated to the new format.

## Technical Details

### opencode-subagent-statusline

- **Purpose:** Shows subagent session status in the statusline
- **Current export:** Named export `SubagentStatusline` (V1)
- **Key functionality:**
  - Tracks subagent work items (running, done, error)
  - Renders status line with counts and details
  - Reads/writes state to filesystem

### opencode-sdd-engram-manage

- **Purpose:** Interactive SDD model selection and profile management
- **Current export:** Likely named export (V1)
- **Need to inspect:** `node_modules/opencode-sdd-engram-manage/dist/`

## Migration Steps

1. Read the source files:
   - `~/.config/opencode/node_modules/opencode-subagent-statusline/dist/index.js`
   - `~/.config/opencode/node_modules/opencode-sdd-engram-manage/dist/` (find main entry)

2. Convert from V1 to V2:
   - Change export from named `PluginName = async () => { ... }` to `export default Plugin.define({ id, setup })`
   - Convert `event: async ({ event }) => {}` to `ctx.event.subscribe({ signal })` with AbortController
   - Convert any hooks to V2 equivalents

3. Save migrated versions to:
   - `~/.config/opencode/plugins/subagent-statusline.ts`
   - `~/.config/opencode/plugins/sdd-engram-manage.ts`

4. Remove from npm dependencies and add to local plugins

5. Test with `opencode plugin list`

## Acceptance Criteria

- [x] AC-1: `opencode-subagent-statusline` migrated to V2 and loads successfully
- [x] AC-2: `opencode-sdd-engram-manage` migrated to V2 and loads successfully
- [x] AC-3: `opencode plugin list` shows both plugins loaded
- [x] AC-4: Statusline displays subagent information correctly
- [x] AC-5: SDD management features work as expected

## Files to Modify

| File | Action |
|------|--------|
| `~/.config/opencode/package.json` | Remove npm plugin dependencies |
| `~/.config/opencode/node_modules/` | Remove installed plugins (after local migration) |
| `~/.config/opencode/plugins/subagent-statusline.ts` | Create (migrated V2) |
| `~/.config/opencode/plugins/sdd-engram-manage.ts` | Create (migrated V2) |

## Dependencies

- Depends on: HU-040 (understanding V2 plugin API)
- Blocks: None

## Owner & Timeline

- **Owner**: Unassigned
- **Priority**: P1 (nice to have — not blocking core functionality)
- **Target milestone**: v0.2.0
- **Dependencies**: HU-040 completed

## References

- HU-040: OpenCode v2.0.29 plugin API migration
- [OpenCode V2 Plugins Docs](https://opencode.ai/v2/docs/build/plugins)
- [Migración V1 → V2](https://opencode.ai/v2/docs/build/plugins/migrate-v1)

## FlowForge

> This section is managed by FlowForge agents. Do not edit manually.

To implement this HU:

```bash
/flow-start migrate-npm-plugins-v1-to-v2
# HU file: HU-041-migrate-npm-plugins-v1-to-v2.md
```

- `flowforge_slug`: `migrate-npm-plugins-v1-to-v2`
- HU status lifecycle: `draft` → `in-progress` → `done`
