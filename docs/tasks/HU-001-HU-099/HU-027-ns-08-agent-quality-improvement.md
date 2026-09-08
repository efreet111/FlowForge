---
hu_id: HU-027
title: "Agent Quality Improvement"
status: draft
category: improvement
flowforge_slug: "hu-027-agent-quality-improvement"
---

# HU-027 — Agent Quality Improvement

## User Story
As the FlowForge team, we want to improve agent quality and consistency across all agent implementations, so that agents provide reliable, traceable, and professional guidance that follows FlowForge conventions.

## Acceptance Criteria (business-level)
- [ ] AC-1: All Spanish text instances in agent instructions are translated to English
- [ ] AC-2: OpenCode agents are brought to parity (80-120 lines) with full protocol implementations
- [ ] AC-3: VS Code agents use FR/NFR notation instead of RF/RNF for traceability
- [ ] AC-4: Missing protocols are added to VS Code agents
- [ ] AC-5: Naming inconsistencies are standardized across all agents
- [ ] AC-6: forge-teacher self-containment violation is fixed
- [ ] AC-7: YAML descriptions are translated to English
- [ ] AC-8: Code duplication is reduced through shared references
- [ ] AC-9: revision_cycle.md template is added
- [ ] AC-10: Error handling is added where missing

## Scenarios (SDD Spec)
### Happy Path
- [ ] **Spanish to English translation completes successfully**
  **GIVEN** agent instruction files containing Spanish text
  **WHEN** the translation phase is executed
  **THEN** all Spanish text is replaced with English equivalents maintaining semantic meaning
  **🧪 Ref**: Phase 1 tasks

- [ ] **OpenCode agents reach target line count**
  **GIVEN** OpenCode agents currently at 19-30 lines (stubs)
  **WHEN** the parity phase is executed
  **THEN** all OpenCode agents contain 80-120 lines with complete protocol implementations
  **🧪 Ref**: Phase 1 tasks

- [ ] **RF/RNF notation migrated to FR/NFR**
  **GIVEN** VS Code forge-arch using RF (Requisito Funcional) / RNF (Requisito No Funcional)
  **WHEN** the notation migration is executed
  **THEN** all references use FR (Functional Requirement) / NFR (Non-Functional Requirement)
  **🧪 Ref**: Phase 1 tasks

- [ ] **Missing protocols added to VS Code agents**
  **GIVEN** VS Code agents with incomplete protocol coverage
  **WHEN** the protocol addition phase is executed
  **THEN** all required protocols are implemented and documented
  **🧪 Ref**: Phase 2 tasks

- [ ] **forge-teacher self-containment fixed**
  **GIVEN** forge-teacher referencing external content that violates self-containment
  **WHEN** the self-containment fix is executed
  **THEN** forge-teacher operates independently without external references
  **🧪 Ref**: Phase 2 tasks

### Edge Cases
- [ ] **Partial translation detection**
  **GIVEN** mixed Spanish/English content after initial translation pass
  **WHEN** validation runs
  **THEN** remaining Spanish instances are flagged for correction

- [ ] **Agent line count boundary handling**
  **GIVEN** agents approaching the 80-120 line target
  **WHEN** additions are made
  **THEN** agents remain within the 80-120 line range without unnecessary bloat

## Context / Notes
**From the spec - key problem**: The FlowForge agent ecosystem has accumulated quality debt across 10+ agents:
- 50+ Spanish text instances scattered across agent instructions
- OpenCode agents are stubs (19-30 lines) lacking full implementations
- VS Code forge-arch uses RF/RNF notation instead of FR/NFR breaking traceability
- Missing protocols in VS Code agents
- Naming inconsistencies across implementations
- forge-teacher violates self-containment principle

**Impact**: Agents provide inconsistent guidance, traceability is broken, and agent quality varies significantly between OpenCode and VS Code implementations.

**Proposed solution phases**:
- Phase 1 (Critical, 1-2 days): Translate Spanish to English, bring OpenCode agents to parity (80-120 lines), fix RF/RNF → FR/NFR
- Phase 2 (High, 2-3 days): Add missing protocols to VS Code agents, standardize naming, fix forge-teacher self-containment
- Phase 3 (Medium, 1-2 days): Translate YAML descriptions, reduce duplication, add revision_cycle.md template, add error handling

**Total effort**: 4-7 days across 10 tasks

## Owner & Timeline
- **Owner**: @equipo-FlowForge
- **Target milestone**: v0.7.0
- **Dependencies**: None (no blockers)

## Definition of Done
- [ ] All Spanish text in agent instructions is translated to English
- [ ] All OpenCode agents contain 80-120 lines with complete implementations
- [ ] All VS Code agents use FR/NFR notation exclusively
- [ ] All missing protocols are implemented in VS Code agents
- [ ] Naming conventions are standardized across all agents
- [ ] forge-teacher operates with full self-containment
- [ ] YAML descriptions are in English
- [ ] Code duplication is reduced via shared references
- [ ] revision_cycle.md template exists and is referenced
- [ ] Error handling is implemented where appropriate
- [ ] All changes pass lint and typecheck (if applicable)

## FlowForge
> This section is managed by FlowForge agents. Do not edit manually.
- HU status lifecycle: `draft` → `in-progress` → `done`
- Status is updated by forge-memory at `/flow-close`

---

## Technical Debt (if applicable)
| Debt Item | Description | Estimated Fix Time |
|-----------|-------------|-------------------|
| Spanish text in agents | 50+ instances across 10+ agents | 1 day |
| Stub agents (OpenCode) | 19-30 lines vs target 80-120 | 1-2 days |
| RF/RNF notation | Wrong notation breaking traceability | 0.5 day |
| Missing protocols | VS Code agents incomplete | 2-3 days |
| Naming inconsistencies | Non-standard naming across agents | 0.5 day |
| forge-teacher self-containment | External reference violation | 0.5 day |
| YAML descriptions | Need English translation | 0.5 day |
| Code duplication | Repeated content across agents | 0.5 day |
| Missing revision_cycle.md | Template not created | 0.25 day |
| Missing error handling | No error handling in some agents | 0.5 day |
