<!-- 
  Adapted from FlowDoc v2.0 (github.com/crhistianmdz/FlowDocs) — MIT License
  Modified by FlowForge: additions documented in ADR-004.
-->

---
hu_id: HU-001
title: "Project Onboarding"
status: done
flowforge_slug: "project-onboarding"
---

# HU-001 — Project Onboarding

## User Story

**As a** new team member,
**I want** to understand the project structure, tools, and conventions,
**so that** I can start contributing effectively.

---

## Acceptance Criteria (business-level)

These criteria define when this feature is "done" from a **business perspective**.
**Do not include technical details here** — those belong in spec.md.

- [ ] AC-1: Team member can run the project locally following README instructions
- [ ] AC-2: Team member understands the folder structure and its purpose
- [ ] AC-3: Team member knows how to run tests and linters

---

## Scenarios (SDD Spec)

Each scenario describes verifiable behavior. Use Given/When/Then format.
**🧪 Ref**: link to test file (completed during implementation).

### Happy Path

- [ ] **Onboarding complete**
  **GIVEN** a new team member joins the project
  **WHEN** they follow the README and ONBOARDING.md
  **THEN** they can run the application locally
  **🧪 Ref**: `README.md` → "Verify: runs without errors"

### Edge Cases

- [ ] **Missing dependencies**
  **GIVEN** a new team member with incomplete toolchain
  **WHEN** they try to run the project
  **THEN** they see clear error messages explaining what's missing
  **🧪 Ref**: `docs/troubleshooting.md` → "Verify: error messages are actionable"

### Error Cases

- [ ] **Unsupported platform**
  **GIVEN** a new team member on an unsupported OS
  **WHEN** they try to run the setup scripts
  **THEN** they see a clear message listing supported platforms
  **🧪 Ref**: `docs/troubleshooting.md` → "Verify: supported platforms documented"

---

## Context / Notes

This HU was completed as part of initial project setup. All acceptance criteria were met and verified by the tech lead.

---

## Owner & Timeline

- **Owner**: @techlead
- **Target milestone**: v1.0
- **Dependencies**: None

---

## Definition of Done

- [x] Code reviewed and merged
- [x] Unit tests passing (coverage ≥ threshold)
- [x] Manual tests (PM-*) executed by developer
- [x] ADR created if architecture decision was made
- [x] Documentation updated (if applicable)

---

## FlowForge

> This section is managed by FlowForge agents. Do not edit manually.

To implement this HU:

```bash
# Start the feature cycle
/flow-start HU-001-project-onboarding
```

- `flowforge_slug` is set by forge-arch when `.ai-work/{slug}/` is created
- HU status lifecycle: `draft` → `in-progress` → `done`
- Status is updated by forge-memory at `/flow-close`

---

## Technical Debt (if applicable)

None — onboarding documentation is complete.
