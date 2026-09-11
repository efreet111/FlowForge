---
hu_id: HU-030
title: "Decision Extraction from plan.md"
status: in-progress
category: feature
flowforge_slug: "hu-030-decision-extraction"
---

# HU-030 — Decision Extraction from plan.md

## User Story
As a FlowForge Memory agent, I want to extract architectural decisions from plan.md when finalized at CKP-2, so that decisions are captured as engram memories with traceability instead of being trapped in markdown.

## Acceptance Criteria (business-level)
- [ ] AC-1: Each `[DECISION]` row in plan.md is extracted and saved as an engram memory with type `decision`
- [ ] AC-2: Each `[CONVENTION]` mention in plan.md is extracted and saved as an engram memory with type `convention`
- [ ] AC-3: Capability matrix entries are extracted and saved as engram memories with type `capability`
- [ ] AC-4: All captures are tagged with the plan's identifier (feature-slug) for traceability
- [ ] AC-5: Capture is opt-in and handles failures gracefully without blocking plan finalization

## Scenarios (SDD Spec)
### Happy Path
- [ ] **[Extract DECISION rows to engram]**
  **GIVEN** a plan.md with finalized `[DECISION]` rows at CKP-2
  **WHEN** the Plan phase completes and capture is triggered
  **THEN** each `[DECISION]` row is saved as an engram memory with type `decision`, tagged with the feature-slug
  **🧪 Ref**: FF-002-001

- [ ] **[Extract CONVENTION mentions to engram]**
  **GIVEN** a plan.md with `[CONVENTION]` mentions at CKP-2
  **WHEN** the Plan phase completes and capture is triggered
  **THEN** each `[CONVENTION]` mention is saved as an engram memory with type `convention`, tagged with the feature-slug
  **🧪 Ref**: FF-002-002

- [ ] **[Extract capability matrix to engram]**
  **GIVEN** a plan.md with capability matrix entries at CKP-2
  **WHEN** the Plan phase completes and capture is triggered
  **THEN** each capability is saved as an engram memory with type `capability`, tagged with the feature-slug
  **🧪 Ref**: FF-002-003

### Edge Cases
- [ ] **[Capture failure handling]**
  **GIVEN** an engram capture failure during extraction
  **WHEN** the capture is triggered
  **THEN** the error is logged and the plan finalization proceeds without blocking
  **🧪 Ref**: FF-002-004

- [ ] **[Empty decision set]**
  **GIVEN** a plan.md with no `[DECISION]` rows, `[CONVENTION]` mentions, or capability matrix
  **WHEN** the capture is triggered
  **THEN** no engram memories are created and no error is raised
  **🧪 Ref**: FF-002-005

- [ ] **[Selective capture]**
  **GIVEN** a plan.md where only some sections contain extractable content
  **WHEN** the capture is triggered
  **THEN** only the sections with `[DECISION]`, `[CONVENTION]`, or capability matrix are processed
  **🧪 Ref**: FF-002-006

## Context / Notes
**Problem Statement**: When FlowForge's Plan phase produces plan.md with architectural decisions, those decisions are trapped in markdown and don't get captured as engram memories for future discoverability and traceability.

**Parte A vs Parte B distinction**:
- **Parte A (this HU)**: Text extraction from plan.md — no code awareness required, no file metadata
- **Parte B (future)**: Code-aware capture with file metadata (file_path, symbol, namespace) — blocked by ENG-416 and ENG-483

**Dependencies for Parte A**: FlowForge Memory agent (exists), plan artifact format (exists), mem_save tool (exists)

**Open Questions for Parte A**:
1. Capture in Plan agent or post-Plan hook?
2. Handle capture failures gracefully?
3. Selective capture?
4. Tag with feature-slug?

## Owner & Timeline
- **Owner**: @equipo-FlowForge
- **Target milestone**: v0.7.0
- **Dependencies**: None (no blockers for Parte A)
- **Effort**: S (1 day)
- **Priority**: P2
- **Status**: Ready (Parte A implementable today)

## Definition of Done
- [ ] Plan agent or post-Plan hook extracts `[DECISION]` rows and saves to engram
- [ ] Plan agent or post-Plan hook extracts `[CONVENTION]` mentions and saves to engram
- [ ] Plan agent or post-Plan hook extracts capability matrix entries and saves to engram
- [ ] All captures tagged with feature-slug for traceability
- [ ] Capture failures are handled gracefully (logged, not blocking)
- [ ] Tests verify extraction behavior for all three content types
- [ ] Documentation updated to reflect decision capture flow

## FlowForge
> This section is managed by FlowForge agents. Do not edit manually.
- HU status lifecycle: `draft` → `in-progress` → `done`
- Status is updated by forge-memory at `/flow-close`

---

## Technical Debt (if applicable)
- Parte A extraction is text-based only; no file_path, symbol, or namespace metadata captured (deferred to Parte B via ENG-416, ENG-483)
