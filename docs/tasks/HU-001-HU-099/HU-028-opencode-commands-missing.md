---
hu_id: HU-028
title: "HOTFIX: flowforge init does not create OpenCode commands"
status: hotfix
flowforge_slug: "hotfix-opencode-commands-missing"
priority: P0
severity: critical
---

# HU-028 — Fix: flowforge init does not create OpenCode commands

## User Story

**As a** FlowForge user with OpenCode IDE,
**I want** `flowforge init` to create slash commands in `.opencode/commands/`,
**so that** I can use `/flow-start`, `/flow-plan`, `/flow-dev`, etc. in OpenCode TUI.

---

## Problem Description

After running `flowforge init .`, the project has:
- ✅ `.opencode/agents/` — 8 agent files (flowforge, forge-arch, etc.)
- ❌ `.opencode/commands/` — empty (no slash commands)

The installer creates commands for Cursor (`ide/cursor/commands/`) but not for OpenCode.

### Root Cause

The FlowForge repo has:
- `ide/cursor/commands/` — 7 command files (flow-start.md, flow-plan.md, etc.)
- `ide/opencode/commands/` — **does not exist**

The installer copies from `ide/{ide}/commands/` but OpenCode has no commands defined.

### Impact

Users cannot use FlowForge slash commands in OpenCode. They must manually create the commands or use the agents directly.

---

## Acceptance Criteria (business-level)

- [ ] AC-1: `flowforge init` creates `.opencode/commands/flow-start.md` with correct OpenCode format
- [ ] AC-2: All 7 FlowForge commands are available: `flow-start`, `flow-plan`, `flow-dev`, `flow-verify`, `flow-rework`, `flow-close`, `flow-status`
- [ ] AC-3: Commands use OpenCode format (frontmatter YAML with `description`, `agent`, `template`)
- [ ] AC-4: Commands reference the `flowforge` agent (defined in `.opencode/agents/flowforge.md`)
- [ ] AC-5: Documentation updated (OpenCode quickstart mentions slash commands)

---

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

---

## Command Format (OpenCode)

OpenCode commands use markdown. **YAML frontmatter is optional** (only needed for metadata).

### Minimal format (no frontmatter):
```markdown
Review $ARGUMENTS for bugs and missing tests.
```

### Complete format (with frontmatter):
```markdown
---
agent: flowforge
---
Iniciar feature FlowForge: $ARGUMENTS

El orquestador delega a `forge-discovery` → `forge-arch`. 
Artefactos: `.ai-work/{feature-slug}/spec.md`. 
CKP-1: aprobación humana antes de `/flow-plan`.
```

### Key differences from Cursor commands:
- **Frontmatter optional**: Only needed for `agent`, `model`, `subagent` metadata
- **Template syntax**: `$ARGUMENTS`, `$1`, `$2`, etc. for parameters
- **Shell output**: `!`command`` to inject bash output
- **File references**: `@path/to/file.md` to include file content (but `@` is not expanded in stored templates)

### OpenCode v2 documentation (official):
> "Add YAML frontmatter when the command needs metadata. The trimmed Markdown body is always the prompt template."

### Fields (all optional except `template` for JSON):
| Field | Required | Behavior |
|-------|----------|----------|
| `template` | JSON only | Prompt template (markdown gets it from body) |
| `description` | No | Text shown in command lists |
| `agent` | No | Agent selected when command runs |
| `model` | No | Model override |
| `subagent` | No | `true` runs in background child session |

---

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

---

## Workaround (Applied)

### Solution 1: JSON configuration (global, immediate)
Commands added to `~/.config/opencode/opencode.json` in the `commands` section:

```json
"commands": {
  "flow-start": {
    "template": "Iniciar feature FlowForge: $ARGUMENTS...",
    "description": "Iniciar feature FlowForge (Discovery → Spec)",
    "agent": "flowforge"
  },
  // ... 6 more commands
}
```

**Pros:** Immediate availability, works globally
**Cons:** Not project-specific, requires JSON editing

### Solution 2: Markdown files (per-project)
Commands created in `.opencode/commands/*.md` with minimal frontmatter:

```markdown
---
agent: flowforge
---
Iniciar feature FlowForge: $ARGUMENTS
...
```

**Pros:** Project-specific, easier to edit
**Cons:** May require OpenCode restart to detect

### Recommendation
Use **both** approaches for maximum compatibility:
- JSON config for immediate global availability
- Markdown files for project-specific customization

---

## Resolution Steps

1. **Create `ide/opencode/commands/`** in the FlowForge repo with 7 command files
2. **Update `InitCommand.cs`** to copy commands for OpenCode (parity with Cursor)
3. **Add tests** to verify commands are created with correct format
4. **Update documentation** (`docs/opencode-installer.md`, `ide/README.md`)
5. **Recompile and publish** new release

---

## Context / Notes

### Forensic Analysis: When did it break?

**Commit `cdd5c79`** (chore(ide): parity v0.4 + installers):
- ✅ Created `ide/cursor/commands/` (7 files)
- ✅ Created `ide/antigravity/workflows/` (6 files)
- ❌ **Did NOT create `ide/opencode/commands/`**

**Commit `3f7b07b`** (28 Jun 2026) — feat(installer): OpenCode agents as markdown files + MCP merge:
- ✅ Created `ide/opencode/agents/` (8 files)
- ✅ Added copy logic: `CopyGlob(Path.Combine(ffRepo, "ide", "opencode", "commands"), ...)`
- ❌ **Did NOT create source files `ide/opencode/commands/*.md`**
- Commit message claims: *"OpenCode auto-loads agents from ~/.config/opencode/agents/*.md and commands from ~/.config/opencode/commands/*.md"*
- But only implemented agents, forgot commands

**15 subsequent commits** touched `ide/opencode/` but none created `ide/opencode/commands/`

### Root Cause

**Incomplete implementation in commit `3f7b07b`**:
- Developer added copy logic for OpenCode commands
- Assumed `ide/opencode/commands/*.md` already existed
- But they were never created (missing from commit `cdd5c79`)
- Installer tries to copy non-existent files → empty directory

### OpenCode vs Cursor format

- Cursor: Plain markdown with code blocks
- OpenCode: YAML frontmatter + template syntax (`$ARGUMENTS`, `!`command``, `@file`)

**Shared content:** The command logic is the same (delegate to `flowforge` agent), but the format differs.

---

## Owner & Timeline

- **Owner**: Unassigned
- **Priority**: P1 (blocks user experience)
- **Target milestone**: Next release
- **Dependencies**: None (self-contained fix)

---

## Definition of Done

- [ ] `ide/opencode/commands/` created with 7 command files
- [ ] `InitCommand.cs` copies commands for OpenCode
- [ ] Unit tests passing
- [ ] Manual test: `flowforge init` creates commands in a fresh project
- [ ] Documentation updated
- [ ] New release published

---

## FlowForge

> This section is managed by FlowForge agents. Do not edit manually.

To implement this HU:

```bash
# Start the feature cycle
/flow-start fix-opencode-commands-missing
# HU file: HU-028-opencode-commands-missing.md
```

- `flowforge_slug`: `fix-opencode-commands-missing`
- `.ai-work/fix-opencode-commands-missing/` contains: context-map.md, spec.md, plan.md (pending)
- HU status lifecycle: `draft` → `in-progress` → `done`
- Status is updated by forge-memory at `/flow-close`
