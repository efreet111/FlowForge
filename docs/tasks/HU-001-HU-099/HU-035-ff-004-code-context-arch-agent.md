---
hu_id: HU-035
title: "Code-Context Arch Agent"
status: done
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

> **AC status (2026-11-06)**: AC-1/AC-3/AC-4 validated at runtime. AC-2 is an outcome
> metric (second-run efficiency) that requires longitudinal usage; it remains open by design.

## Scenarios (SDD Spec)
### Happy Path
- [x] **[Arch agent recalls prior decisions for a module]**
  **GIVEN** a developer runs FlowForge on a codebase where module "auth" was previously architected
  **WHEN** the Arch agent begins designing for module "auth"
  **THEN** it queries mem_decisions_for_module("auth") and incorporates those decisions as constraints
  **🧪 Ref**: FF-001 scenarios for ENG-416/ENG-484

- [x] **[Arch agent warns about contradicting prior decisions]**
  **GIVEN** a developer requests an architecture change that contradicts a stored decision
  **WHEN** the Arch agent detects the conflict
  **THEN** it surfaces a warning: "You're about to contradict decision X"
  **🧪 Ref**: FF-001 scenarios for ENG-416/ENG-484

### Edge Cases
- [x] **[No prior decisions exist]**
  **GIVEN** a developer runs FlowForge on a new module with no prior decisions
  **WHEN** the Arch agent queries memory
  **THEN** it proceeds with standard derivation without errors
  **🧪 Ref**: FF-001 scenarios for ENG-416/ENG-484

- [x] **[Memory unavailable]**
  **GIVEN** the Engram service is unavailable
  **WHEN** the Arch agent attempts to query memory
  **THEN** it logs a warning and proceeds with stateless derivation
  **🧪 Ref**: FF-001 scenarios for ENG-416/ENG-484
  > Validated statically against FR-004 (2026-11-06); no live outage was induced.

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
- **Status**: DONE — runtime validated 2026-11-06 (PM-5 static)

## Definition of Done
- [x] Init recall hook via `mem_decisions_for_module` integrated (FR-001; `mem_recall_for_file` was dropped at CKP-1 — the Arch agent needs decisions, not file-level recall)
- [x] Contradiction warnings implemented (FR-002)
- [x] Decision citation with memory IDs implemented (FR-003)
- [x] Graceful degradation (MCP unavailable, timeout, no module) implemented (FR-004)
- [ ] Tests verify Arch agent uses memory context on second run (PM-2 covers implicit-module recall; second-run efficiency remains an outcome metric)
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
> **PM runtime**: PASS — executed 2026-11-06 with a controlled Arch-agent session against fixtures in project `pm-033-035-validation`:
>
> | PM | Result | Evidence |
> |----|--------|----------|
> | PM-1 | ✅ PASS | `mem_decisions_for_module("skills/forge-arch")` → #4300 + #4301 |
> | PM-2 | ✅ PASS | module resolved from Context Map `target_modules: ["skills/forge-arch"]` |
> | PM-3 | ✅ PASS | contradiction warning cited #4300 vs #4301; CKP-1 blocked |
> | PM-4 | ✅ PASS | `skills/no-such-module` → "No decisions found", proceeded stateless |
> | PM-5 | ✅ PASS (static) | FR-004 graceful-degradation rule; no live outage induced |
>
> **Data-path note**: the MCP `mem_save` signature does not expose `file_path`/`symbol`/`namespace`;
> the runtime fixtures were seeded via the Engram CLI (`engram save --file-path --symbol --namespace`).
> This is the gap tracked by HU-034 (code-aware capture, Part B). The recall hooks themselves work.
>
> **Status rationale**: `draft`/`BLOCKED` was stale. The code is merged, dependencies are Done,
> and the PM runtime gate is satisfied (PM-5 static). AC-2 remains a post-deployment outcome metric.

---

## Technical Debt (if applicable)
- Short-term: mem_search prompt improvements (XS effort) can ship before ENG-416/484
- Long-term: Full code-aware recall wiring depends on ENG-416/ENG-484 (both Done; PM validation pending)
