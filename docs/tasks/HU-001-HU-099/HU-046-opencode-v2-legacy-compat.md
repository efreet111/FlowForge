---
hu_id: HU-046
title: "FIX: OpenCode V2 legacy format compatibility"
status: done
flowforge_slug: "opencode-v2-legacy-compat"
priority: P1
severity: medium
created_date: 2026-10-15
completed_date: 2026-10-15
---

# HU-046 — FIX: OpenCode V2 legacy format compatibility

## User Story

As a FlowForge maintainer,
I want all FlowForge configuration files to be fully V2 compliant with OpenCode's current documentation,
so that the installer generates correct configuration that works with OpenCode v2.

## Problem Description

After reviewing [OpenCode V2 documentation](https://opencode.ai/v2/docs/config/) and [V2 Agents guide](https://opencode.ai/v2/docs/agents/), two files use legacy V1 format:

### 1. Agentes Markdown — Usan `permission:` (V1)

**Archivo:** `ide/opencode/templates/agents/*.md.tpl`

**Problema:** Usan formato legacy `permission:` (objeto simple)

```yaml
# ❌ Legacy V1
permission:
  bash: allow
  read: allow
  edit: deny
```

**Debería ser (V2):**

```yaml
# ✅ V2
permissions:
  - action: bash
    resource: "*"
    effect: allow
  - action: read
    resource: "*"
    effect: allow
  - action: edit
    resource: "*"
    effect: deny
```

### 2. Schema — Usa formato V1

**Archivo:** `ide/opencode/templates/config.schema.json`

**Problema:** El schema define `agent`, `provider`, `permission` (singular) cuando V2 usa `agents`, `providers`, `permissions[]`.

## Requirements

### FR-001: Actualizar agentes markdown a formato V2

Todos los archivos `ide/opencode/templates/agents/*.md.tpl` deben:

1. Cambiar `permission:` → `permissions:`
2. Convertir de objeto simple a array de rules:
   ```yaml
   permissions:
     - action: <tool>
       resource: <path or *>
       effect: <allow|deny|ask>
   ```
3. Mantener funcionalidad equivalente

### FR-002: Actualizar config.schema.json a formato V2

El schema debe:
1. Cambiar `agent` → `agents` (object)
2. Cambiar `provider` → `providers` (object)
3. Cambiar `permission` → `permissions` (array)
4. Agregar nuevos campos V2: `default_agent`, `plugins`, `mcp.servers`, `skills`, etc.

## Files to Modify

| File | Change |
|------|--------|
| `ide/opencode/templates/agents/flowforge.md.tpl` | `permission:` → `permissions[]` |
| `ide/opencode/templates/agents/forge-discovery.md.tpl` | `permission:` → `permissions[]` |
| `ide/opencode/templates/agents/forge-arch.md.tpl` | `permission:` → `permissions[]` |
| `ide/opencode/templates/agents/forge-plan.md.tpl` | `permission:` → `permissions[]` |
| `ide/opencode/templates/agents/forge-dev.md.tpl` | `permission:` → `permissions[]` |
| `ide/opencode/templates/agents/forge-verify.md.tpl` | `permission:` → `permissions[]` |
| `ide/opencode/templates/agents/forge-memory.md.tpl` | `permission:` → `permissions[]` |
| `ide/opencode/templates/agents/forge-teacher.md.tpl` | `permission:` → `permissions[]` |
| `ide/opencode/templates/config.schema.json` | Rewrite to V2 format |

## Reference: OpenCode V2 Format

### Agentes (Markdown)

```md
---
description: Agent description
mode: subagent
model: provider/model-id
permissions:
  - action: edit
    resource: "*"
    effect: deny
  - action: shell
    resource: "*"
    effect: allow
---

System prompt here.
```

### Agentes (JSON config)

```jsonc
{
  "agents": {
    "reviewer": {
      "description": "Reviews code",
      "mode": "subagent",
      "system": "Review only.",
      "permissions": [
        { "action": "edit", "resource": "*", "effect": "deny" }
      ]
    }
  }
}
```

### Permissions (JSON)

```jsonc
{
  "permissions": [
    { "action": "shell", "resource": "*", "effect": "allow" },
    { "action": "shell", "resource": "git push *", "effect": "ask" }
  ]
}
```

### MCP (JSON)

```jsonc
{
  "mcp": {
    "servers": {
      "engram": {
        "type": "local",
        "command": ["engram", "mcp"]
      }
    }
  }
}
```

## Acceptance Criteria

| ID | Criteria | Verification |
|----|----------|--------------|
| AC-1 | Todos los `.md.tpl` usan `permissions:` array | Sin `permission:` objeto |
| AC-2 | Schema `config.schema.json` usa formato V2 | Valida contra schema V2 |
| AC-3 | Instalación genera config V2 compliant | `opencode.json` pasa validación |

## Technical Notes

### Conversión de permissions

**Legacy (V1):**
```yaml
permission:
  edit: allow
  bash: allow
  read: allow
```

**V2:**
```yaml
permissions:
  - action: edit
    resource: "*"
    effect: allow
  - action: bash
    resource: "*"
    effect: allow
  - action: read
    resource: "*"
    effect: allow
```

### Agentes afectados

| Agent | edit | bash | read | write | skill | subagent |
|-------|------|------|------|-------|-------|---------|
| flowforge | allow | allow | allow | allow | allow | allow |
| forge-discovery | deny | allow | allow | deny | allow | - |
| forge-arch | allow | allow | allow | - | allow | - |
| forge-plan | allow | allow | allow | - | allow | - |
| forge-dev | allow | allow | allow | - | allow | - |
| forge-verify | allow | allow | allow | - | allow | - |
| forge-memory | allow | allow | allow | - | allow | - |
| forge-teacher | deny | deny | allow | deny | allow | - |

---

## FlowForge

> This section is managed by FlowForge agents. Do not edit manually.

To implement this HU:

```bash
/flow-start opencode-v2-legacy-compat
```

