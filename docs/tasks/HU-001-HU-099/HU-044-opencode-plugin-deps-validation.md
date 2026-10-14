---
hu_id: HU-044
title: "FEATURE: Auto-install OpenCode plugin npm dependencies during FlowForge installation"
status: in-progress
category: feature
flowforge_slug: "opencode-plugin-deps-validation"
priority: P1
severity: high
created_date: 2026-10-14
updated_date: 2026-10-14
---

# HU-044 — FEATURE: Auto-install OpenCode plugin npm dependencies during FlowForge installation

## User Story

As a FlowForge operator,
I want FlowForge installation to automatically install all OpenCode plugin npm dependencies in the correct location,
so that plugins work out-of-the-box without manual configuration.

## Problem Description

When FlowForge installs OpenCode plugins locally, the plugins fail to load because their npm dependencies are not installed in the correct location.

### Symptoms

```
[WARN] failed to load plugin: background-agents.ts
  cause="Cannot find package 'unique-names-generator'"

[WARN] failed to load plugin: sdd-engram-manage.ts
  cause="Cannot find package 'solid-js'"
```

### Plugin Status

| Plugin | Status | Missing Dependency |
|--------|--------|-------------------|
| engram.ts | ✅ WORKING | — |
| model-variants.ts | ✅ WORKING | — |
| subagent-statusline.ts | ✅ WORKING | — |
| background-agents.ts | ❌ FAILING | `unique-names-generator` |
| sdd-engram-manage.ts | ❌ FAILING | `solid-js`, `@opentui/keymap/addons` |

## Root Cause Analysis

### Discovery: OpenCode Uses Bun Runtime

OpenCode v2 is bundled as a native binary that uses **Bun** as its JavaScript runtime, not Node.js.

```bash
$ strings ~/.opencode/bin/opencode | grep -i bun
BUN_1.2
```

### Module Resolution: Bun vs Node.js

**Bun's module resolution** differs from Node.js:

| Runtime | Resolution Strategy |
|---------|-------------------|
| Node.js | `node_modules` in global paths (`~/.node_modules`, `/usr/lib/node`) |
| **Bun** | `node_modules` in the **same directory as the file** and **ancestors** |

### Plugin Path Structure

```
~/.config/opencode/
├── opencode.json
├── package.json          ← FlowForge dependencies (V1 SDK)
├── node_modules/          ← ✅ CORRECT: Bun resolves from here
│   ├── solid-js/
│   ├── unique-names-generator/
│   └── @opentui/
└── plugins/
    ├── background-agents.ts     → Bun looks for node_modules in:
    │                               1. plugins/node_modules/     (doesn't exist)
    │                               2. config/opencode/node_modules/  ← FOUND ✅
    │                               3. ~/.config/node_modules/
    │                               4. ~/node_modules/
    ├── engram.ts
    ├── model-variants.ts
    ├── sdd-engram-manage.ts
    └── subagent-statusline.ts
```

### Why Old Approaches Failed

| Attempt | Location | Why It Failed |
|---------|----------|--------------|
| Install in `~/.opencode/node_modules/` | `~/.opencode/` | Bun doesn't search here from `~/.config/opencode/plugins/` |
| Install in `~/.local/share/opencode/bin/node_modules/` | OpenCode binary dir | Binary is in `~/.opencode/bin/`, plugins are in `~/.config/opencode/plugins/` |
| Install in `~/.config/opencode/oldnode_modules/` | Legacy location | Bun doesn't search `oldnode_modules/` |

### Correct Solution

**Install dependencies in `~/.config/opencode/node_modules/`**:

```bash
cd ~/.config/opencode
npm install solid-js unique-names-generator @opentui/keymap
```

After this, all 5 plugins load successfully:
```
✅ background-agents.ts
✅ engram.ts
✅ model-variants.ts
✅ sdd-engram-manage.ts
✅ subagent-statusline.ts
```

## Requirements

### FR-001: Auto-Install Plugin Dependencies

During `flowforge install --ide opencode`:
1. Detect plugin directory: `~/.config/opencode/plugins/`
2. Extract npm dependencies from all `.ts` files in plugins dir
3. Install dependencies in `~/.config/opencode/node_modules/`
4. Restart OpenCode service to reload plugins

### FR-002: Dependency Extraction

```bash
# Extract all npm imports from plugins
grep -oE "from ['\"]@[^'\"]+['\"]" ~/.config/opencode/plugins/*.ts | \
  sed 's/from //g; s/["'\'']//g' | sed 's/^@//' | \
  grep '/' | sort -u
```

Required packages for FlowForge plugins:
- `solid-js` (for `sdd-engram-manage.ts`)
- `unique-names-generator` (for `background-agents.ts`)
- `@opentui/keymap` (for `sdd-engram-manage.ts`)

### FR-003: Validation After Installation

After installation, verify plugins load:
```bash
grep "failed to load plugin" ~/.local/share/opencode/log/opencode.log
# Should return empty (no failures)
```

## Implementation Tasks

- [x] Root cause identified (Bun module resolution)
- [x] Solution verified (install in `~/.config/opencode/node_modules/`)
- [ ] Integrate into `flowforge install` command
- [ ] Add validation step to verify plugin load
- [ ] Document in POST-INSTALL.md
- [ ] Test with clean install

## Files to Modify

| File | Change |
|------|--------|
| `src/FlowForge.Installer/Commands/InstallCommand.cs` | Add npm install for plugin deps |
| `ide/install.sh` | Add npm install for plugin deps |
| `POST-INSTALL.md` | Document plugin dependency requirement |

## Key Insight for Installer

> **Bun resolves modules from the plugin file's directory upward.**
> 
> Plugin at: `~/.config/opencode/plugins/background-agents.ts`
> 
> Bun looks for `node_modules` in:
> 1. `~/.config/opencode/plugins/node_modules/` ❌
> 2. `~/.config/opencode/node_modules/` ✅ **Install here**
> 3. `~/.config/node_modules/` ❌
> 4. `~/.node_modules/` ❌
> 5. `~/node_modules/` ❌

## Related

- HU-040 (OpenCode v2 migration) — discovered the plugin loading issue
- HU-041 (NPM plugins V1→V2 migration) — related to plugin migration
- HU-042 (context7 MCP auto-install) — similar auto-install pattern
- HU-043 (OpenCode v2 config migration) — related to V2 compliance

## References

- [OpenCode v2 Plugins Documentation](https://opencode.ai/v2/docs/build/plugins)
- [OpenCode v2 Plugin Migration Guide](https://opencode.ai/v2/docs/build/plugins/migrate-v1)
- Log evidence: `~/.local/share/opencode/log/opencode.log`
- Runtime detection: `strings ~/.opencode/bin/opencode | grep BUN`
