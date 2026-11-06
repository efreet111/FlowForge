---
hu_id: HU-035
title: "Code-Context Arch Agent"
status: in-progress
category: feature
flowforge_slug: "hu-035-code-context-arch-agent"
---

# HU-035 — Code-Context Arch Agent

## User Story
As a FlowForge developer, I want the Arch agent to recall prior architectural decisions from memory before designing new solutions, so that I don't waste time re-deriving the same decisions and can maintain consistency across sessions.

## Acceptance Criteria (business-level)
- [x] AC-1: Arch agent queries memory for prior decisions on the target module before generating recommendations
- [ ] AC-2: Second FlowForge run on the same codebase produces fewer re-derivation steps
- [x] AC-3: Arch agent can explain "I made this decision because of memory Y"
- [x] AC-4: "You're about to contradict decision X" warnings surface when applicable

## Scenarios (SDD Spec)
### Happy Path
- [ ] **[Arch agent recalls prior decisions for a module]**
  **GIVEN** a developer runs FlowForge on a codebase where module "auth" was previously architected
  **WHEN** the Arch agent begins designing for module "auth"
  **THEN** it queries mem_decisions_for_module("auth") and incorporates those decisions as constraints
  **🧪 Ref**: FF-001 scenarios for ENG-416/ENG-484

- [ ] **[Arch agent warns about contradicting prior decisions]**
  **GIVEN** a developer requests an architecture change that contradicts a stored decision
  **WHEN** the Arch agent detects the conflict
  **THEN** it surfaces a warning: "You're about to contradict decision X"
  **🧪 Ref**: FF-001 scenarios for ENG-416/ENG-484

### Edge Cases
- [ ] **[No prior decisions exist]**
  **GIVEN** a developer runs FlowForge on a new module with no prior decisions
  **WHEN** the Arch agent queries memory
  **THEN** it proceeds with standard derivation without errors
  **🧪 Ref**: FF-001 scenarios for ENG-416/ENG-484

- [ ] **[Memory unavailable]**
  **GIVEN** the Engram service is unavailable
  **WHEN** the Arch agent attempts to query memory
  **THEN** it logs a warning and proceeds with stateless derivation
  **🧪 Ref**: FF-001 scenarios for ENG-416/ENG-484

## Context / Notes
Problem: Arch agent designs systems without code-aware memory. It can't ask "what have we decided about this module before?" The Arch agent's recommendations are stateless across sessions. If you run FlowForge twice on the same codebase, the Arch agent re-derives the same architectural decisions instead of building on prior decisions.

Proposed Solution: Wire mem_recall_for_file and mem_decisions_for_module into the Arch agent's context-loading phase:
1. Identify target module(s) from user's request
2. Query mem_decisions_for_module(module) for each
3. Use those decisions as constraints in the new architecture design
4. Optionally, surface "you're about to contradict decision X" warnings

Short-term alternative (XS effort, no blockers): Improve Arch agent's mem_search prompts to search for "decisions about {module}" explicitly.

Note: FF-004 shares same engram dependencies as FF-001. If ENG-416/484 are implemented for FF-001, FF-004 becomes M effort (2-3 days).

## Owner & Timeline
- **Owner**: @equipo-FlowForge
- **Target milestone**: v0.8.0 (DEPENDE de engram-dotnet)
- **Dependencies**: ENG-416 (schema evolution) ✅ Done, ENG-484 (code-context tools) ✅ Done

## Definition of Done
- [x] Init recall hook via `mem_decisions_for_module` integrated (FR-001; `mem_recall_for_file` was dropped at CKP-1 — the Arch agent needs decisions, not file-level recall)
- [x] Contradiction warnings implemented (FR-002)
- [x] Decision citation with memory IDs implemented (FR-003)
- [x] Graceful degradation (MCP unavailable, timeout, no module) implemented (FR-004)
- [ ] Tests verify Arch agent uses memory context on second run
- [x] Documentation updated for Arch agent memory integration (SKILL.md v1.1.0 + changelog)

## FlowForge
> This section is managed by FlowForge agents. Do not edit manually.
- HU status lifecycle: `draft` → `in-progress` → `done`
- Status is updated by forge-memory at `/flow-close`

---

## Verification & PM status

> **Implementation**: `19ed2af` (`feat(HU-035): add code-aware memory hooks to forge-arch`).
> **Verify**: PASS — `.ai-work/hu-035-code-context-arch-agent/verify-report.md` (4/4 FRs, 12/12 plan items).
> **DoD reconciliation**: the original DoD mentioned `mem_recall_for_file`, but the approved
> spec FR-001 uses `mem_decisions_for_module`; the Arch agent needs prior decisions, not
> file-level recall. The DoD was reconciled to the verified spec on 2026-11-06.
> **PM runtime**: pending — PM-1..PM-5 require a real Arch-agent session; see `spec.md` §4.
> **Status rationale**: `draft`/`BLOCKED` was stale. The code is merged and the dependencies
> (ENG-416/484) are Done; only the human runtime validation remains.

---

## Technical Debt (if applicable)
- Short-term: mem_search prompt improvements (XS effort) can ship before ENG-416/484
- Long-term: Full code-aware recall wiring depends on ENG-416/ENG-484 (both Done; PM validation pending)
