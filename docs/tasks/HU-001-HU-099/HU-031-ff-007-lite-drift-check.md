---
hu_id: HU-031
title: "Drift Health Check - Lite Version"
status: draft
category: feature
flowforge_slug: "ff-007-lite-drift-check"
---

# HU-031 — Drift Health Check (Lite Version)

## User Story
As a developer or team lead, I want the system to detect when actual code structure drifts from the documented plan, so that I can keep plan.md accurate and prevent architectural decay.

## Acceptance Criteria (business-level)
- [ ] AC-1: forge-verify detects files in code that are not mentioned in any plan.md task
- [ ] AC-2: forge-verify flags plan.md tasks not marked [x] after a configurable threshold (default: 14 days)
- [ ] AC-3: forge-verify prompts the user when drift is detected, offering to update plan or fix code
- [ ] AC-4: Drift detection runs automatically during the verify phase with no additional flags needed

## Scenarios (SDD Spec)
### Happy Path
- [ ] **[Drift detection on new untracked file]**
  **GIVEN** a new file `src/utils/helper.ts` exists in the codebase
  **WHEN** forge-verify runs and `src/utils/helper.ts` is not referenced in plan.md
  **THEN** a warning is raised: "Code drift detected: src/utils/helper.ts not found in plan.md"
  **🧪 Ref**: TBD

- [ ] **[Stale task detection]**
  **GIVEN** a task `- [ ] Implement auth middleware` exists in plan.md and was created/modified 15 days ago
  **WHEN** forge-verify runs with default threshold of 14 days
  **THEN** a warning is raised: "Stale task detected: 'Implement auth middleware' not completed after 14 days"
  **🧪 Ref**: TBD

- [ ] **[No drift scenario]**
  **GIVEN** all code files are referenced in plan.md and all tasks are marked [x] or are recent
  **WHEN** forge-verify runs
  **THEN** no drift warnings are produced
  **🧪 Ref**: TBD

### Edge Cases
- [ ] **[Intentionally deferred task]**
  **GIVEN** a task is marked with a deferral marker (e.g., `deferred: v0.8.0`)
  **WHEN** forge-verify runs
  **THEN** the stale task warning is suppressed for that task
  **🧪 Ref**: TBD

- [ ] **[In-progress task not flagged]**
  **GIVEN** a task `- [ ] Implement auth middleware` exists in plan.md and is marked `- [x] Implement auth middleware` but has a related PR open
  **WHEN** forge-verify runs
  **THEN** the task is not flagged as stale since it is in progress or completed
  **🧪 Ref**: TBD

- [ ] **[Multiple drift sources]**
  **GIVEN** 3 new files exist and 2 tasks are stale
  **WHEN** forge-verify runs
  **THEN** all 5 issues are reported in a single grouped summary
  **🧪 Ref**: TBD

## Context / Notes
**Problem Statement**: Over time, actual code drifts from plan.md. New devs add features not in plan; refactors change architecture; decisions get lost. Plan becomes historical artifact instead of living document.

**Lite vs Full Version**:
- **Lite** (this HU): Integrated into forge-verify. Compares current code structure against plan's expected structure, flags stale tasks, flags untracked files, prompts user on drift.
- **Full**: Standalone command `flowforge drift` with LLM-as-Judge comparison of spec vs implementation plus drift report.

**Open Questions from spec**:
- Threshold for "old" tasks? → Default to 14 days (configurable)
- Distinguish "not started" vs "in progress"? → Yes, check for `[x]` completion marker
- Handle intentionally deferred tasks? → Yes, support `deferred: <version>` marker
- Alert on every check or only when threshold exceeded? → Alert every check but only when threshold exceeded per task

**Dependencies**: plan.md artifact format (exists), spec.md format (exists), forge-verify agent (exists, can extend)
**Effort**: M (1 day) for lite version
**Priority**: P3

## Owner & Timeline
- **Owner**: @equipo-FlowForge
- **Target milestone**: v0.7.0
- **Dependencies**: None (no blockers for lite version)

## Definition of Done
- [ ] forge-verify detects untracked files not in plan.md
- [ ] forge-verify flags tasks stale after N days (default: 14, configurable)
- [ ] forge-verify supports deferred task marker to suppress stale warnings
- [ ] Drift summary is presented to user with prompt to update plan or fix code
- [ ] Configuration option for threshold days added to .flowforge.json
- [ ] Unit tests cover drift detection logic
- [ ] Documentation updated in forge-verify skill

## FlowForge
> This section is managed by FlowForge agents. Do not edit manually.
- HU status lifecycle: `draft` → `in-progress` → `done`
- Status is updated by forge-memory at `/flow-close`

---

## Technical Debt (if applicable)
- Threshold configuration may need validation (positive integer, reasonable max)
- Consider adding `flowforge drift` full command in future iteration (FF-007-full)
