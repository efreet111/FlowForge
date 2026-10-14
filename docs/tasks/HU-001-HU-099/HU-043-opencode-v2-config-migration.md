---
hu_id: HU-043
title: "MIGRATION: OpenCode v2 config format (opencode.json V1→V2)"
status: todo
category: maintenance
flowforge_slug: "opencode-v2-config-migration"
priority: P0
severity: critical
created_date: 2026-10-14
---

# HU-043 — MIGRATION: OpenCode v2 config format (opencode.json V1→V2)

## User Story

As a FlowForge operator,
I want the `opencode.json` configuration to be fully V2 compliant,
so that FlowForge agents load correctly and permissions work as expected in OpenCode v2.

## Problem Description

The `opencode.json` template is a **hybrid V1/V2 format** with critical structural errors that prevent proper V2 compliance. The configuration mixes deprecated V1 field names with some V2 structures.

### Current State (V1/V2 Hybrid — BROKEN)

| Field | Current | V2 Required |
|-------|---------|-------------|
| `agent` | `agent: { ... }` | `agents: { ... }` |
| `permission` | `permission: { edit: "allow" }` | `permissions: [{ action: "edit", resource: "*", effect: "allow" }]` |
| `prompt` | `prompt: "{file:...}"` | `system: "{file:...}"` |
| `tools` | `tools: { edit: true }` | **REMOVE — does not exist in V2** |
| `provider` | `provider: { ... }` | `providers: { ... }` |

### Impact

- Agents may not load correctly
- Permissions are ignored or misapplied
- System prompts may not be injected
- Provider configuration is broken

## Requirements

### FR-001: Rename `agent` → `agents`
All agent definitions must be under the `agents` key, not `agent`.

### FR-002: Convert `permission` objects to `permissions` arrays
V2 uses a structured array format:

```jsonc
"permissions": [
  { "action": "edit", "resource": "*", "effect": "allow" },
  { "action": "shell", "resource": "git push *", "effect": "ask" }
]
```

| Field | Purpose |
|-------|---------|
| `action` | Tool or permission action (`edit`, `shell`, `read`, `subagent`, etc.) |
| `resource` | Path, command, or `*` for all |
| `effect` | `allow`, `ask`, or `deny` |

### FR-003: Rename `prompt` → `system`
All agent-level prompt fields must be renamed to `system`.

### FR-004: Remove `tools` from agent definitions
The `tools` field inside agent definitions does not exist in V2. Tool permissions are controlled exclusively through the `permissions` array.

### FR-005: Rename `provider` → `providers`
Provider definitions must be under `providers`, not `provider`.

### FR-006: Move API keys to environment variables
Any hardcoded API keys must use `{env:VAR_NAME}` format.

### FR-007: Update provider structure to V2 format
V2 providers require a different structure:

```jsonc
"providers": {
  "opencode-go": {
    "id": "opencode-go",
    "name": "OpenCode Go",
    "package": "@ai-sdk/openai-compatible",
    "settings": {
      "baseURL": "https://opencode.ai/zen/go/v1"
    },
    "models": { ... }
  }
}
```

### FR-008: Validate model references
Model references should use `provider/model-id` format, not placeholders like `__FLOWFORGE_MODEL__`.

## Migration Template

### Before (V1/V2 Hybrid — BROKEN)
```json
"agent": {
  "flowforge": {
    "permission": { "edit": "allow" },
    "prompt": "{file:./agents/flowforge.md}",
    "tools": { "edit": true }
  }
}
```

### After (V2 Compliant)
```json
"agents": {
  "flowforge": {
    "permissions": [
      { "action": "edit", "resource": "*", "effect": "allow" }
    ],
    "system": "{file:./agents/flowforge.md}"
  }
}
```

## Files to Modify

| File | Change |
|------|--------|
| `ide/opencode/templates/opencode.json.tpl` | Complete V2 rewrite |
| `ide/opencode/generate-config.sh` | Update field mappings |
| `src/FlowForge.Installer/` | Update C# config generator |
| `install/install.sh` | Ensure V2 output |

## Acceptance Criteria

| ID | Criteria | Verification |
|----|----------|--------------|
| AC-1 | Template `opencode.json.tpl` is V2 compliant | JSON schema validation passes |
| AC-2 | `generate-config.sh` produces V2 output | Generated config validates |
| AC-3 | All agents use `agents` key | No `agent:` found in output |
| AC-4 | All permissions use array format | No `permission: {` in output |
| AC-5 | All prompts renamed to `system` | No `prompt:` in agent definitions |
| AC-6 | No `tools` fields in agent defs | No `tools:` in agent definitions |
| AC-7 | Providers use `providers` key | No `provider:` at root level |
| AC-8 | `flowforge install` produces valid V2 config | OpenCode loads agents correctly |

## Implementation Tasks

- [ ] Rewrite `ide/opencode/templates/opencode.json.tpl` with V2 format
- [ ] Update `ide/opencode/generate-config.sh` field mappings
- [ ] Update C# `OpenCodeConfigGenerator` if applicable
- [ ] Add JSON schema validation test
- [ ] Test generated config with OpenCode v2
- [ ] Update documentation (opencode-installer.md)

## Related

- HU-040 (Plugin V1→V2 migration)
- HU-041 (NPM plugins V1→V2 migration)
- ADR-006 (OpenCode MCP config)

## References

- [OpenCode v2 Configuration](https://opencode.ai/v2/docs/configuration/)
- [OpenCode v2 Agents](https://opencode.ai/v2/docs/agents/)
- Audit Report: `opencode-v2-config-audit-report.md` (Obsidian)
