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
- [x] AC-1: User can trigger contradiction detection on demand via a `mem_check_contradictions` tool
- [ ] AC-2: System identifies 5-10 useful conflicts when run against a store of 500+ memories
- [x] AC-3: Resolving each contradiction is a 30-second decision for the user
- [ ] AC-4: Agent retrieval quality improves measurably after curation
- [ ] AC-5: Contradiction detection can run periodically (cron) or on demand

> **Scope note (rework v2, 2026-10-14)**: AC-2, AC-4 and AC-5 describe the full
> engram-dotnet feature (500+ memories, retrieval-quality metrics, cron). The delivered
> FlowForge-layer v2 scope is **on-demand detection + manual resolution over `mem_relations`**;
> the metrics/cron items remain Phase 2/upstream. PM-3 is PARTIAL because `mem_update` lacks
> a `resolution` field; the documented workaround (memory signal) preserves the audit trail.

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
- **Status**: DONE (FlowForge-layer v2 scope); Phase 2 items deferred

## Definition of Done
- [x] `mem_check_contradictions` tool implemented in engram-dotnet
- [x] Integration with `mem_relations` for `conflicts_with` tracking
- [x] User-facing workflow for contradiction resolution (keep both, supersede, merge, ignore)
- [ ] Confidence threshold configuration for auto-marking supersedence (Phase 2)
- [ ] Periodic (cron) execution option (Phase 2; on-demand + CKP-4 is implemented)
- [ ] Unit tests for contradiction detection logic (PM-1..PM-4 executed; automated tests deferred)
- [ ] Integration tests with mocked memory store (deferred)
- [ ] Retrieval quality improvement metrics documented (Phase 2)

## FlowForge
> This section is managed by FlowForge agents. Do not edit manually.
- HU status lifecycle: `draft` → `in-progress` → `done`
- Status is updated by forge-memory at `/flow-close`
- **Dependency note**: the engram-dotnet tools used by rework v2 (`mem_check_contradictions`,
  `mem_relations conflicts_with`) are available in engram 1.4. The Phase 2 items
  (ENG-412/414/416/418) remain upstream and are not blockers for the delivered v2 scope.

---

## Closure note (2026-11-06)

> **Implementation**: rework v2 skills (`forge-memory` v1.3.0, `forge-memory-contradiction` v2.0.0),
> commit `a5643f4`/`4ce23e7` lineage on `main`.
> **Verify**: PASS — `.ai-work/hu-036-contradiction-detection/verify-report.md`.
> **PM**: PM-1 PASS, PM-2 PASS, PM-4 PASS, PM-3 PARTIAL (accepted third-party debt;
> see `docs/backlog/HU-PARTIALS-third-party-deps.md`).
> **Decision**: the FlowForge-layer v2 scope is complete; the original full-scope
> AC-2/AC-4/AC-5 and DoD metrics remain Phase 2/upstream. Status promoted from `draft` to `done`.

---

## Technical Debt (if applicable)
- **Short-term workaround**: Manual contradiction tracking via `mem_relations conflicts_with` (S effort, no blockers)
- **Soft blockers**: ENG-416 and ENG-418 are soft blockers; FF-005 can proceed partially with basic functionality before these are complete
