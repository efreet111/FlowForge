---
hu_id: HU-036
title: "Contradiction Detection in Memory Store"
status: done
category: feature
flowforge_slug: "hu-036-contradiction-detection"
---

# HU-036 — Contradiction Detection in Memory Store

## User Story
As a FlowForge agent, I want to detect contradictions between stored memories so that the memory store remains a reliable source of truth and does not become a source of confusion as it grows.

## Acceptance Criteria (business-level)
- [ ] AC-1: User can trigger contradiction detection on demand via a `mem_check_contradictions` tool
- [ ] AC-2: System identifies 5-10 useful conflicts when run against a store of 500+ memories
- [ ] AC-3: Resolving each contradiction is a 30-second decision for the user
- [ ] AC-4: Agent retrieval quality improves measurably after curation
- [ ] AC-5: Contradiction detection can run periodically (cron) or on demand

## Scenarios (SDD Spec)
### Happy Path
- [ ] **Detect temporal contradictions**
  **GIVEN** the memory store contains two memories about API protocol ("we use REST" and "we use gRPC")
  **WHEN** `mem_check_contradictions` is executed
  **THEN** the system surfaces both memories with a confidence score and offers resolution options (keep both with timestamps, mark one as superseded, merge, or ignore)
  **🧪 Ref**: ENG-414

- [ ] **Auto-mark supersedence with confidence threshold**
  **GIVEN** contradiction detection finds two memories with high embedding similarity indicating one supersedes the other
  **WHEN** confidence exceeds the configured threshold
  **THEN** the system offers to auto-mark the older memory as superseded
  **🧪 Ref**: ENG-412, ENG-414

- [ ] **Curation improves retrieval quality**
  **GIVEN** a memory store with 500+ memories containing contradictions
  **WHEN** contradictions are resolved through curation
  **THEN** agent retrieval quality metrics improve measurably
  **🧪 Ref**: ENG-418

### Edge Cases
- [ ] **No contradictions found**
  **GIVEN** a coherent memory store with no contradictions
  **WHEN** `mem_check_contradictions` is executed
  **THEN** the system returns an empty conflict list with a success message

- [ ] **Ambiguous contradictions**
  **GIVEN** two memories that could be contradictory but are actually about different contexts
  **WHEN** contradiction detection runs
  **THEN** the system provides context to help the user determine if resolution is needed

- [ ] **Large conflict sets**
  **GIVEN** a memory store with more than 20 potential contradictions
  **WHEN** contradiction detection runs
  **THEN** the system prioritizes and surfaces the highest-confidence conflicts first

## Context / Notes
From the spec:

**Problem**: As memory stores grow, contradictions become inevitable. Two memories that say "we use REST" and "we use gRPC" can both be true at different times, but the agent needs to know which is current. Without contradiction detection, the memory store becomes a source of confusion.

**Proposed Solution**: A `mem_check_contradictions` tool and FlowForge Memory Curation step:
- Run periodically (cron) or on demand
- Use embedding similarity + heuristics to find potentially-conflicting memories
- Surface them to user with options: keep both, mark one as superseded, merge, or ignore
- For detected supersedence, offer auto-marking with confidence threshold

**Success Criteria**: User with 500 memories runs `mem_check_contradictions` and gets 5-10 useful conflicts; resolving each is a 30-second decision; agent's retrieval quality improves measurably after curation.

**Short-term alternative**: Use `mem_relations` with `conflicts_with` for manual contradiction tracking (S effort, no blockers).

## Owner & Timeline
- **Owner**: @equipo-FlowForge
- **Target milestone**: v0.9.0 (DEPENDE de engram-dotnet)
- **Dependencies**:
  - ENG-412 (taxonomy) - Ready, not started
  - ENG-414 (contradiction logic) - Ready, depends on ENG-412
  - ENG-416 (schema evolution) - Ready, not started (soft blocker)
  - ENG-418 (hybrid search) - Ready, depends on embeddings (soft blocker)
- **Effort**: XL (4-6 weeks) / M after engram dependencies
- **Priority**: P3 (only matters when memory store is large - 500+ memories)
- **Status**: BLOCKED

## Definition of Done
- [ ] `mem_check_contradictions` tool implemented in engram-dotnet
- [ ] Integration with `mem_relations` for `conflicts_with` tracking
- [ ] User-facing UI for contradiction resolution (keep both, supersede, merge, ignore)
- [ ] Confidence threshold configuration for auto-marking supersedence
- [ ] Periodic (cron) execution option
- [ ] Unit tests for contradiction detection logic
- [ ] Integration tests with mocked memory store
- [ ] Retrieval quality improvement metrics documented

## FlowForge
> This section is managed by FlowForge agents. Do not edit manually.
- HU status lifecycle: `draft` → `in-progress` → `done`
- Status is updated by forge-memory at `/flow-close`
- **Phase 1/Phase 2 split** (2026-10-08): Phase 1 unblocked — manual contradiction tracking + stub tool
- **Rework v2** (2026-10-14): Full restart from CKP-0 — new FlowForge-layer surfacing approach

### Rework v2 (2026-10-14) — IN PROGRESS
- **Approach**: FlowForge-layer surfacing (does NOT depend on engram-dotnet surfacing in mem_search)
- **Context Map**: `.ai-work/hu-036-contradiction-detection/context-map.md`
- **Spec**: `.ai-work/hu-036-contradiction-detection/spec.md`
- **Plan**: `.ai-work/hu-036-contradiction-detection/plan.md`

#### Tareas Implementadas (Rework v2)
- [x] T-001: `skills/forge-memory/SKILL.md` — updated to v1.3.0, contradiction check section
- [x] T-002: `skills/forge-memory-contradiction/SKILL.md` — refactored to v2.0.0, FlowForge-layer approach
- [x] T-003: `skills/forge-memory-contradiction/contradiction-resolver.md` — updated header for v2
- [ ] T-004: HU-036 documentation updated (en progreso)

### Previous Implementation (Pre-Rework)
#### Phase 1 (2026-10-08) — CKP-4 CLOSED
- [x] T-005: `skills/forge-memory-contradiction/SKILL.md` — contradiction detection + curation skill
- [x] T-006: `skills/forge-memory-contradiction/contradiction-resolver.md` — resolution workflow
- [x] T-007: `skills/forge-memory/SKILL.md` — contradiction surfacing at session close
- [x] T-008: Session close flow — `mem_check_contradictions` call at close
- [x] T-009: HU-036 updated with Phase 1/Phase 2 status
- [x] T-010: `docs/12-engram-tool-reference.md` — `mem_check_contradictions` stub entry
- [x] T-011: PM-1 through PM-4 manual test cases added to spec.md

### Phase 2 (Deferred — blocked on ENG-412/414)
- [BLOCKED] T-012: Embedding-based similarity detection (ENG-412)
- [BLOCKED] T-013: Contradiction heuristics engine (ENG-414)
- [BLOCKED] T-014: Schema evolution for temporal fields (ENG-416)
- [BLOCKED] T-015: Embedding service integration (ENG-418)
- [BLOCKED] T-016: Auto-mark supersedence with confidence threshold
- [BLOCKED] T-017: Cron-triggered periodic detection
- [BLOCKED] T-018: LLM-as-Judge for ambiguous cases

---

## Technical Debt (if applicable)
- **Short-term workaround**: Manual contradiction tracking via `mem_relations conflicts_with` (S effort, no blockers)
- **Soft blockers**: ENG-416 and ENG-418 are soft blockers; FF-005 can proceed partially with basic functionality before these are complete
