---
hu_id: HU-038
title: "HOTFIX: OpenCode slash commands missing"
status: in-progress
category: hotfix
flowforge_slug: "hotfix-opencode-commands-missing"
priority: P0
severity: critical
created_date: 2026-09-16
---

# HU-038 — HOTFIX: OpenCode slash commands missing

## User Story

As a FlowForge user with OpenCode IDE,
I want `flowforge init` to create slash commands in `.opencode/commands/`,
so that I can use `/flow-start`, `/flow-plan`, `/flow-dev`, etc. in OpenCode TUI.

## Problem Description

After running `flowforge init .`, the project has:
- ✅ `.opencode/agents/` — 8 agent files (flowforge, forge-arch, etc.)
- ❌ `.opencode/commands/` — empty (no slash commands)

The installer creates commands for Cursor (`ide/cursor/commands/`) but not for OpenCode.

### Root Cause

Commit `cdd5c79` (Jun 2026) created commands for Cursor and Antigravity, but forgot OpenCode. Commit `3f7b07b` (28 Jun 2026) added copy logic for OpenCode but assumed the source files already existed (they didn't).

### Impact

- **Cursor**: ✅ Works (has `ide/cursor/commands/`)
- **Antigravity**: ✅ Works (uses `ide/antigravity/workflows/`)
- **OpenCode**: ❌ Broken (missing `ide/opencode/commands/`)
- **VS Code**: N/A (uses `.agent.md` frontmatter, not commands)

## Acceptance Criteria (business-level)

- [ ] AC-1: `flowforge init` creates `.opencode/commands/flow-start.md` with correct OpenCode format
- [ ] AC-2: All 7 FlowForge commands are available: `flow-start`, `flow-plan`, `flow-dev`, `flow-verify`, `flow-rework`, `flow-close`, `flow-status`
- [ ] AC-3: Commands use OpenCode format (frontmatter YAML with `description`, `agent`, `template`)
- [ ] AC-4: Commands reference the `flowforge` agent (defined in `.opencode/agents/flowforge.md`)
- [ ] AC-5: Documentation updated (OpenCode quickstart mentions slash commands)

## Scenarios (SDD Spec)

### Happy Path

- [ ] **Commands are created on init**
  **GIVEN** a project without `.opencode/commands/`
  **WHEN** `flowforge init .` runs
  **THEN** 7 command files are created in `.opencode/commands/`
  **🧪 Ref**: `tests/FlowForge.Installer.Tests/Init/InitCommandTests.cs`

- [ ] **Commands have correct format**
  **GIVEN** `.opencode/commands/flow-start.md` exists
  **WHEN** the file is read
  **THEN** it has frontmatter with `description` and `agent: flowforge`
  **AND** the template uses `$ARGUMENTS` for feature description

### Edge Cases

- [ ] **Existing commands are preserved**
  **GIVEN** `.opencode/commands/custom.md` exists (user-created)
  **WHEN** `flowforge init .` runs
  **THEN** `custom.md` is not modified or deleted
  **AND** FlowForge commands are added alongside it

- [ ] **Re-run does not overwrite**
  **GIVEN** `.opencode/commands/flow-start.md` already exists
  **WHEN** `flowforge init .` runs again
  **THEN** the user is prompted: Skip / Backup+Overwrite / Overwrite
  **AND** default is Skip (preserve existing)

## Command Format (OpenCode)

OpenCode commands use markdown with YAML frontmatter:

```markdown
---
description: Iniciar feature FlowForge (Discovery → Spec)
agent: flowforge
---
Iniciar feature FlowForge: $ARGUMENTS

El orquestador delega a `forge-discovery` → `forge-arch`. 
Artefactos: `.ai-work/{feature-slug}/spec.md`. 
CKP-1: aprobación humana antes de `/flow-plan`.
```

Key differences from Cursor commands:
- **Frontmatter required**: `description` (mandatory), `agent`, `model`, `subtask`
- **Template syntax**: `$ARGUMENTS`, `$1`, `$2`, etc. for parameters
- **Shell output**: `!`command`` to inject bash output
- **File references**: `@path/to/file.md` to include file content

## Files to Create

| File | Description | Agent |
|------|-------------|-------|
| `flow-start.md` | Iniciar feature (Discovery → Spec) | `flowforge` |
| `flow-plan.md` | Generar plan desde spec.md | `flowforge` |
| `flow-dev.md` | Ejecutar plan con TDD | `flowforge` |
| `flow-verify.md` | Auditoría LLM-as-Judge | `flowforge` |
| `flow-rework.md` | Ejecutar rework tickets | `flowforge` |
| `flow-close.md` | Cerrar sesión y extraer conocimiento | `flowforge` |
| `flow-status.md` | Ver estado actual del workflow | `flowforge` |

## Workaround (Applied)

Commands were manually created in `.opencode/commands/` following the OpenCode format. See commit history for the exact files.

## Resolution Steps

1. **Create `ide/opencode/commands/`** in the FlowForge repo with 7 command files
2. **Update `InitCommand.cs`** to copy commands for OpenCode (parity with Cursor)
3. **Add tests** to verify commands are created with correct format
4. **Update documentation** (`docs/opencode-installer.md`, `ide/README.md`)
5. **Recompile and publish** new release

## Context / Notes

**Origin:** FlowForge was initially designed for Cursor (which uses `.cursor/commands/`). OpenCode support was added later but commands were overlooked.

**OpenCode vs Cursor format:**
- Cursor: Plain markdown with code blocks
- OpenCode: YAML frontmatter + template syntax

**Shared content:** The command logic is the same (delegate to `flowforge` agent), but the format differs.

## Owner & Timeline

- **Owner**: Unassigned
- **Priority**: P1 (blocks user experience)
- **Target milestone**: Next release
- **Dependencies**: None (self-contained fix)

## Definition of Done

- [ ] `ide/opencode/commands/` created with 7 command files
- [ ] `InitCommand.cs` copies commands for OpenCode
- [ ] Unit tests passing
- [ ] Manual test: `flowforge init` creates commands in a fresh project
- [ ] Documentation updated
- [ ] New release published

## FlowForge

> This section is managed by FlowForge agents. Do not edit manually.

To implement this HU:

```bash
# Start the feature cycle
/flow-start hotfix-opencode-commands-missing
# HU file: HU-038-opencode-commands-missing.md
```

- `flowforge_slug`: `hotfix-opencode-commands-missing`
- `.ai-work/hotfix-opencode-commands-missing/` contains: context-map.md, spec.md, plan.md
- HU status lifecycle: `draft` → `in-progress` → `done`
- Status is updated by forge-memory at `/flow-close`
