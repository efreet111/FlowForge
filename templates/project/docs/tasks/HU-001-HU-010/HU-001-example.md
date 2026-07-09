<!-- 
  Adapted from FlowDoc v2.0 (github.com/crhistianmdz/FlowDocs) — MIT License
  Modified by FlowForge: additions documented in ADR-004.
-->

---
hu_id: HU-001
title: "Project Onboarding"
status: draft
flowforge_slug: ""
---

# HU-001 — [Feature name]

## User Story

**As a** [user role],
**I want** [action or capability],
**so that** [benefit or outcome].

---

## Acceptance Criteria (business-level)

These criteria define when this feature is "done" from a **business perspective**.
**Do not include technical details here** — those belong in spec.md.

- [ ] AC-1: [Business outcome 1 — what the user experiences]
- [ ] AC-2: [Business outcome 2]

---

## Scenarios (SDD Spec)

Each scenario describes verifiable behavior. Use Given/When/Then format.
**🧪 Ref**: link to test file (completed during implementation).

### Happy Path

- [ ] **[Main scenario name]**
  **GIVEN** [precondition]
  **WHEN** [action]
  **THEN** [expected result]
  **🧪 Ref**: `tests/...` → "[test name]"

### Edge Cases

- [ ] **[Edge case name]**
  **GIVEN** [precondition]
  **WHEN** [action]
  **THEN** [expected result]
  **🧪 Ref**: `tests/...` → "[test name]"

### Error Cases

- [ ] **[Error name]**
  **GIVEN** [precondition]
  **WHEN** [action]
  **THEN** [expected result]
  **🧪 Ref**: `tests/...` → "[test name]"

---

## Context / Notes

This is a **template example** — replace the content below with your own HU. All checkboxes are unchecked and `flowforge_slug` is empty so adopters can copy-paste without carrying over state from the example.

---

## Owner & Timeline

- **Owner**: @username
- **Target milestone**: [sprint/month/version]
- **Dependencies**: [what this HU needs from others]

---

## Definition of Done

- [ ] Code reviewed and merged
- [ ] Unit tests passing (coverage ≥ threshold)
- [ ] Manual tests (PM-*) executed by developer
- [ ] ADR created if architecture decision was made
- [ ] Documentation updated (if applicable)

---

## FlowForge

> This section is managed by FlowForge agents. Do not edit manually.

To implement this HU:

```bash
# Start the feature cycle
/flow-start HU-001-[feature-slug]
```

- `flowforge_slug` is set by forge-arch when `.ai-work/{slug}/` is created
- HU status lifecycle: `draft` → `in-progress` → `done`
- Status is updated by forge-memory at `/flow-close`

---

## Technical Debt (if applicable)

[Any item left pending, why, and how it will be resolved later]
