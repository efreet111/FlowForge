<!-- 
  Adapted from FlowDoc v2.0 (github.com/crhistianmdz/FlowDocs) — MIT License
  Modified by FlowForge: additions documented in ADR-004.
-->

---
hu_id: HU-024
title: "Forzar lectura de AGENTS.md al inicio + version tracking de skills"
status: in-progress
flowforge_slug: "forge-startup-update-tracking-log-skill"
---

# HU-024 — Forzar lectura de AGENTS.md al inicio + version tracking de skills

## User Story

**As a** FlowForge development team,
**I want** that the orchestrator reads AGENTS.md at the start of every session and that skills have version tracking,
**so that** behavior rules are always enforced and skill changes are traceable.

---

## Acceptance Criteria (business-level)

These criteria define when this feature is "done" from a **business perspective**.
**Do not include technical details here** — those belong in spec.md.

- [ ] AC-1: forge-orchestrator SKILL.md specifies "read AGENTS.md first" rule
- [ ] AC-2: All SKILL.md files have version in frontmatter
- [ ] AC-3: All SKILL.md files have changelog section in frontmatter
- [ ] AC-4: Session start always verifies state from local files, not just memory

---

## Scenarios (SDD Spec)

Each scenario describes verifiable behavior. Use Given/When/Then format.
**🧪 Ref**: link to test file (completed during implementation).

### Happy Path

- [ ] **[Orchestrator reads AGENTS.md first]**
  **GIVEN** a new session starts
  **WHEN** the user sends the first message
  **THEN** forge-orchestrator reads AGENTS.md before responding
  **🧪 Ref**: `skills/forge-orchestrator/SKILL.md`

- [ ] **[Skill version tracking]**
  **GIVEN** a skill is modified
  **WHEN** the change is committed
  **THEN** version is incremented and changelog entry added in frontmatter
  **🧪 Ref**: `.agents/skills/*/SKILL.md`

### Edge Cases

- [ ] **[No memory available]**
  **GIVEN** Engram memory is unavailable
  **WHEN** session starts
  **THEN** orchestrator reads AGENTS.md and local files only
  **🧪 Ref**: `skills/forge-orchestrator/SKILL.md`

---

## Context / Notes

This HU addresses a discipline issue where agents go straight to memory without verifying local state first. The solution is to make reading AGENTS.md a mandatory first step.

---

## Owner & Timeline

- **Owner**: @flowforge-team
- **Target milestone**: Q3 2026
- **Dependencies**: None

---

## Definition of Done

- [ ] forge-orchestrator SKILL.md updated with first-step rule
- [ ] All skills have version + changelog in frontmatter
- [ ] No HU needed for this change (self-referential)

---

## FlowForge

> This section is managed by FlowForge agents. Do not edit manually.

To implement this HU:

```bash
# Start the feature cycle
/flow-start forge-startup-update-tracking-log-skill
# HU file: HU-024-forge-startup-update-tracking-log-skill.md (range-binned)
```

- `flowforge_slug` is set by forge-arch when `.ai-work/{slug}/` is created
- HU status lifecycle: `draft` → `in-progress` → `done`
- Status is updated by forge-memory at `/flow-close`

---

## Technical Debt (if applicable)

None

(End of file - total 119 lines)
