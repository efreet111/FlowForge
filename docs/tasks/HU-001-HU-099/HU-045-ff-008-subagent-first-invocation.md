<!-- 
  Adapted from FlowDoc v2.0 (github.com/crhistianmdz/FlowDocs) — MIT License
  Modified by FlowForge: additions documented in ADR-004.
-->

---
hu_id: HU-045
title: "Subagent-first invocation en FlowForge Orchestrator"
status: draft
flowforge_slug: ""
---

# HU-045 — Subagent-first invocation en FlowForge Orchestrator

## User Story

**As a** FlowForge user,
**I want** the orchestrator to always delegate work to specialized subagents instead of loading skills inline,
**so that** each phase runs with isolated context, optimal model selection, and consistent behavior across all installations.

---

## Problem Statement

Currently, when the orchestrator (reginald/flowforge) encounters a FlowForge phase (Discovery, Arch, Plan, Dev, Verify, Memory), it may load the corresponding skill inline instead of launching a subagent. This happens because:

1. Skills in `~/.config/opencode/skills/forge-*/` have `description` fields
2. OpenCode V2 auto-advertises skills with descriptions to the model
3. The model chooses skill loading (more efficient) over subagent creation
4. Result: Inline execution instead of isolated subagent sessions

### Consequences

- Subagents defined in `opencode.json` with `mode: subagent` are underutilized
- Each phase may run with different context than intended
- Model routing per phase is ineffective
- Inconsistent behavior across installations

---

## Acceptance Criteria (business-level)

- [ ] AC-1: When orchestrator needs to execute a FlowForge phase, it **always** uses the `subagent` tool to launch the specialized agent
- [ ] AC-2: Skills remain available for direct invocation when users explicitly request them (e.g., "load the forge-arch skill")
- [ ] AC-3: The configuration works consistently across all FlowForge installations (no machine-specific manual changes required)
- [ ] AC-4: Installation script (`install-skills.sh`) applies the necessary configuration automatically

---

## Scenarios (SDD Spec)

### Happy Path

- [ ] **Orchestrator delegates to subagent on /flow-start**
  **GIVEN** User invokes `/flow-start nueva-feature`
  **WHEN** Orchestrator needs to run Discovery phase
  **THEN** Orchestrator uses `subagent` tool to launch `@forge-discovery`
  **🧪 Ref**: Integration test → "subagent-first on flow-start"

- [ ] **Orchestrator delegates to subagent on /flow-plan**
  **GIVEN** User invokes `/flow-plan`
  **WHEN** Orchestrator needs to run Plan phase
  **THEN** Orchestrator uses `subagent` tool to launch `@forge-plan`
  **🧪 Ref**: Integration test → "subagent-first on flow-plan"

- [ ] **User can still invoke skills directly**
  **GIVEN** User explicitly asks to "load the forge-arch skill"
  **WHEN** User requests skill loading
  **THEN** Orchestrator loads the skill inline as requested
  **🧪 Ref**: Integration test → "direct skill invocation still works"

### Edge Cases

- [ ] **Subagent unavailable — use skill as fallback**
  **GIVEN** Subagent is not available in the environment
  **WHEN** Orchestrator attempts to launch subagent
  **THEN** Orchestrator falls back to `skill` tool with user notification
  **🧪 Ref**: Integration test → "graceful degradation uses skill as fallback"

- [ ] **Skills without autoinvoke config — backward compatibility**
  **GIVEN** A skill exists without `metadata.opencode/autoinvoke` field
  **WHEN** Orchestrator runs in environment without HU-045 changes
  **THEN** Orchestrator still works (uses skills inline as fallback)
  **🧪 Ref**: Integration test → "backward compatibility preserved"

### Error Cases

- [ ] **Subagent permission denied**
  **GIVEN** Orchestrator lacks `subagent` permission
  **WHEN** Orchestrator attempts to launch subagent
  **THEN** OpenCode returns permission error, orchestrator logs and falls back to skill loading with user notification
  **🧪 Ref**: Integration test → "permission error handled gracefully"

---

## Technical Solution

### Opción implementada: Instrucciones claras de prioridad

**NO se bloquean skills** — se instruye al orquestador a usar subagentes PRIMERO.

1. **`autoinvoke: false`** en skills forge-*
   - Skills no se auto-anuncian al modelo
   
2. **Sección "Delegación: Subagentes PRIMERO"** en `forge-orchestrator/SKILL.md`
   - Mapa de fases → subagentes
   - Ejemplos de uso correcto/incorrecto
   - Anti-pattern documentado

3. **`skills[]`** preservado en agentes
   - Usuarios pueden invocar skills directamente si desean
   - Backward compatible

### Por qué no permisos de denegación

- Skills pueden ser útiles para usuarios que quieren uso directo
- Bloquear skills rompe flujos de trabajo válidos
- Instrucciones claras + prioridad es más flexible
   - Model can always use subagent tool

### Files to Modify

| File | Change |
|------|--------|
| `skills/forge-*/SKILL.md` | Add `metadata.opencode/autoinvoke: false` to all forge-* skills |
| `install-skills.sh` | Insert `autoinvoke: false` during installation |

### Implementation Detail

```yaml
# Before
---
name: forge-arch
description: Phase 1 (Intent)...
---

# After
---
name: forge-arch
description: Phase 1 (Intent)...
metadata:
  opencode/autoinvoke: false
---
```

### Installation Script Integration

```bash
# In install-skills.sh, add:
for skill_dir in ~/.config/opencode/skills/forge-*; do
  if [ -f "$skill_dir/SKILL.md" ]; then
    # Ensure metadata.opencode/autoinvoke: false exists
    if ! grep -q "autoinvoke: false" "$skill_dir/SKILL.md"; then
      # Insert after first --- line
      sed -i '/^---$/a metadata:\n  opencode/autoinvoke: false' "$skill_dir/SKILL.md"
    fi
  fi
done
```

---

## Context / Notes

- **Related to**: HU-021 (adopt-flowdocs-skills), HU-027 (agent-quality-improvement)
- **OpenCode V2 docs**: Skills with `description` are auto-advertised unless `metadata.opencode/autoinvoke: false`
- **Reference**: `~/.config/opencode/docs/opencode-agents-skills-config.md` — Previous learning about skills vs subagents

> ⚠️ **Why Option C over Option A or B?**
> - Option A (remove skills[]): Breaks direct-skill workflows for users who want single-model local usage
> - Option B (modify orchestrator only): Fragile — depends on model following instructions exactly
> - Option C: Combines configuration + permissions + instructions = robust

---

## Owner & Timeline

- **Owner**: @kaito
- **Target milestone**: v1.3.0
- **Dependencies**: None (self-contained HU)

---

## Definition of Done

- [ ] HU created and merged
- [ ] All forge-* skills have `autoinvoke: false`
- [ ] `install-skills.sh` applies configuration during installation
- [ ] Integration tests pass for all scenarios
- [ ] Documentation updated (AGENTS.md, README)

---

## FlowForge

> This section is managed by FlowForge agents. Do not edit manually.

To implement this HU:

```bash
# Start the feature cycle
/flow-start subagent-first-invocation
# HU file: HU-045-subagent-first-invocation.md (range-binned)
```

- `flowforge_slug` is set by forge-arch when `.ai-work/{slug}/` is created
- HU status lifecycle: `draft` → `in-progress` → `done`
- Status is updated by forge-memory at `/flow-close`

---

## Technical Debt (if applicable)

None anticipated.
