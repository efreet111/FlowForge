---
hu_id: HU-034
title: "Code-Aware Capture with file metadata"
status: draft
category: feature
flowforge_slug: "hu-034-code-aware-capture"
---

# HU-034 — Code-Aware Capture with file metadata

## User Story
As a FlowForge Memory agent, I want to capture decisions with code-aware metadata (file_path, symbol, namespace), so that future queries can trace decisions to the exact code locations they affect.

## Acceptance Criteria (business-level)
- [ ] AC-1: When a decision is captured, it includes `file_path` indicating which file the decision applies to
- [ ] AC-2: When a decision is captured, it includes `symbol` indicating which class/function the decision affects
- [ ] AC-3: When a decision is captured, it includes `namespace` indicating which module the decision belongs to
- [ ] AC-4: Code-aware metadata is captured using engram-dotnet tools (ENG-483 engram watch)
- [ ] AC-5: Schema evolution (ENG-416) is applied to support the new metadata fields

## Scenarios (SDD Spec)
### Happy Path
- [ ] **[Capture decision with file_path]**
  **GIVEN** a decision extracted from plan.md at CKP-2
  **WHEN** code-aware capture is triggered
  **THEN** the decision is saved with `file_path` metadata pointing to the relevant source file
  **🧪 Ref**: FF-002-007

- [ ] **[Capture decision with symbol]**
  **GIVEN** a decision extracted from plan.md at CKP-2
  **WHEN** code-aware capture is triggered
  **THEN** the decision is saved with `symbol` metadata indicating the affected class or function
  **🧪 Ref**: FF-002-008

- [ ] **[Capture decision with namespace]**
  **GIVEN** a decision extracted from plan.md at CKP-2
  **WHEN** code-aware capture is triggered
  **THEN** the decision is saved with `namespace` metadata indicating the module
  **🧪 Ref**: FF-002-009

- [ ] **[Complete metadata capture]**
  **GIVEN** a decision with identified code references in plan.md
  **WHEN** code-aware capture is triggered
  **THEN** the decision is saved with all three metadata fields (file_path, symbol, namespace) populated
  **🧪 Ref**: FF-002-010

### Edge Cases
- [ ] **[Partial code awareness]**
  **GIVEN** a decision with only a file reference but no clear symbol or namespace
  **WHEN** code-aware capture is triggered
  **THEN** the decision is saved with available metadata; missing fields are left null
  **🧪 Ref**: FF-002-011

- [ ] **[No code reference in decision]**
  **GIVEN** a decision that does not reference any specific code location
  **WHEN** code-aware capture is triggered
  **THEN** the decision is saved without code-aware metadata (null file_path, symbol, namespace)
  **🧪 Ref**: FF-002-012

- [ ] **[Code-aware tool unavailable]**
  **GIVEN** ENG-483 tools are not available in engram-dotnet
  **WHEN** code-aware capture is triggered
  **THEN** the capture falls back to text-only extraction (Parte A behavior) with a warning logged
  **🧪 Ref**: FF-002-013

## Context / Notes
**Problem Statement**: Parte A (HU-030) captures decisions as text only. Parte B extends this to include code-aware metadata so decisions can be traced to exact file locations, symbols, and namespaces.

**Relationship to Parte A**:
- Parte A (HU-030): Text extraction from plan.md — no code awareness required
- Parte B (this HU): Code-aware capture with file metadata — blocked by ENG-416 and ENG-483

**Dependencies**:
- ENG-416: Schema evolution - Ready, not started (HARD BLOCKER)
- ENG-483: Code-aware memory capture (engram watch <file>) - Idea (P2) (HARD BLOCKER)
- HU-030 must be implemented first (no blockers)

**Open Questions**:
1. How does the Plan agent identify which code files a decision relates to?
2. Should symbol resolution use static analysis or heuristics from plan.md references?
3. How to handle decisions spanning multiple files/symbols?

## Owner & Timeline
- **Owner**: @equipo-FlowForge
- **Target milestone**: v0.8.0
- **Dependencies**: ENG-416 (schema evolution), ENG-483 (code-aware capture), HU-030 (Parte A)
- **Effort**: L (2-3 weeks) for Parte B alone
- **Priority**: P2
- **Status**: BLOCKED (waiting on ENG-416, ENG-483)

## Definition of Done
- [ ] ENG-416 schema evolution is completed and deployed
- [ ] ENG-483 code-aware capture tools are implemented in engram-dotnet
- [ ] Decisions captured include file_path metadata when applicable
- [ ] Decisions captured include symbol metadata when applicable
- [ ] Decisions captured include namespace metadata when applicable
- [ ] Fallback to text-only capture when code-aware tools unavailable
- [ ] Tests verify metadata capture for all three fields
- [ ] Integration tests verify end-to-end flow from plan.md to engram

## FlowForge
> This section is managed by FlowForge agents. Do not edit manually.
- HU status lifecycle: `draft` → `in-progress` → `done`
- Status is updated by forge-memory at `/flow-close`

---

## Technical Debt (if applicable)
- Parte B is blocked until ENG-416 and ENG-483 are completed in engram-dotnet
- Multiple code references in a single decision may require multiple memory entries (one per file/symbol)
