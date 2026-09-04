---
hu_id: HU-035
title: "Code-Context Arch Agent"
status: draft
category: feature
flowforge_slug: "ff-004-code-context-arch-agent"
---

# HU-035 — Code-Context Arch Agent

## User Story
As a FlowForge developer, I want the Arch agent to recall prior architectural decisions from memory before designing new solutions, so that I don't waste time re-deriving the same decisions and can maintain consistency across sessions.

## Acceptance Criteria (business-level)
- [ ] AC-1: Arch agent queries memory for prior decisions on the target module before generating recommendations
- [ ] AC-2: Second FlowForge run on the same codebase produces fewer re-derivation steps
- [ ] AC-3: Arch agent can explain "I made this decision because of memory Y"
- [ ] AC-4: "You're about to contradict decision X" warnings surface when applicable

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
- **Dependencies**: ENG-416 (schema evolution), ENG-484 (code-context tools)

## Definition of Done
- [ ] mem_recall_for_file integrated into Arch agent context-loading
- [ ] mem_decisions_for_module integrated into Arch agent context-loading
- [ ] Contradiction warnings implemented
- [ ] Short-term mem_search prompt improvements deployed
- [ ] Tests verify Arch agent uses memory context on second run
- [ ] Documentation updated for Arch agent memory integration

## FlowForge
> This section is managed by FlowForge agents. Do not edit manually.
- HU status lifecycle: `draft` → `in-progress` → `done`
- Status is updated by forge-memory at `/flow-close`

---

## Technical Debt (if applicable)
- Short-term: mem_search prompt improvements (XS effort) can ship before ENG-416/484
- Long-term: Full mem_recall_for_file / mem_decisions_for_module wiring depends on ENG-416/ENG-484
