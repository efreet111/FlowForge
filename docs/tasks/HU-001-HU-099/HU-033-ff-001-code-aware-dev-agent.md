---
hu_id: HU-033
title: "Code-Aware Dev Agent"
status: done
category: feature
flowforge_slug: "hu-033-code-aware-dev-agent"
---

# HU-033 — Code-Aware Dev Agent

## User Story
As a development team, I want the Dev agent to have access to our project's code-context memory, so that it produces code consistent with our existing architectural decisions, team conventions, and design patterns without re-deriving the same decisions repeatedly.

## Acceptance Criteria (business-level)
- [x] AC-1: Dev agent loads relevant project context at initialization before generating code
- [x] AC-2: Dev agent surfaces relevant memories before editing existing files
- [x] AC-3: Dev agent offers to capture code changes as memories after successful edits
- [x] AC-4: Dev agent queries symbol-level context when modifying classes or functions
- [ ] AC-5: Team reports measurable reduction in time spent correcting Dev agent output
- [ ] AC-6: Team trusts the Dev agent with our codebase

## Scenarios (SDD Spec)
### Happy Path
- [x] **Agent initializes with project context**
  **GIVEN** a Dev agent session is started for module `auth`
  **WHEN** the agent queries `mem_recall_for_module("auth")`
  **THEN** it loads existing decisions about auth architecture, conventions, and patterns
  **🧪 Ref**: ENG-484

- [x] **Agent surfaces memories before file edit**
  **GIVEN** a Dev agent is about to edit `src/services/UserService.ts`
  **WHEN** it queries `mem_recall_for_file("src/services/UserService.ts")`
  **THEN** relevant memories about the file's design decisions are surfaced
  **🧪 Ref**: ENG-484

- [x] **Agent captures change after edit**
  **GIVEN** a Dev agent successfully edits `src/services/UserService.ts`
  **WHEN** the edit is applied and validated
  **THEN** the agent offers to capture the change as a memory for future reference
  **🧪 Ref**: ENG-483

- [x] **Agent recalls symbol context**
  **GIVEN** a Dev agent is about to modify the `UserService` class
  **WHEN** it queries `mem_recall_for_symbol("UserService")`
  **THEN** relevant memories about the class design are surfaced
  **🧪 Ref**: ENG-484

### Edge Cases
- [x] **No memories found for file/module**
  **GIVEN** no prior memories exist for a target file
  **WHEN** `mem_recall_for_file` is called
  **THEN** the agent proceeds with standard behavior without error
  **🧪 Ref**: ENG-484

- [ ] **Partial context loaded**
  **GIVEN** some memories exist but are outdated
  **WHEN** context is loaded
  **THEN** the agent indicates confidence level and proceeds cautiously
  **🧪 Ref**: ENG-416
  > Not executed in the 2026-11-06 runtime validation; requires a deliberately stale fixture.

## Context / Notes
A Dev agent without project memory is essentially a smarter autocomplete—it can generate syntactically correct code but lacks awareness of architectural decisions, team conventions, and design patterns that have already been established. This leads to:

- Repeated re-derivation of the same architectural decisions
- Inconsistent code that contradicts existing patterns
- Wasted human time correcting agent output
- Lack of trust in the Dev agent's recommendations

**Short-term workaround**: Implement FF-003 (Onboarding) and FF-002 Parte A (decision extraction) to deliver value without engram-dotnet dependencies.

## Owner & Timeline
- **Owner**: @equipo-FlowForge
- **Target milestone**: v0.8.0 (DEPENDE de engram-dotnet)
- **Dependencies**:
  - ENG-416 (schema evolution — add file_path, symbol, namespace fields): ✅ Done
  - ENG-484 (code-context query tools): ✅ Done
  - ENG-483 (code-aware memory capture): ✅ Done
- **Effort**: XL (2-3 weeks including engram work)
- **Priority**: P1
- **Status**: DONE — runtime validated 2026-11-06

## Definition of Done
- [x] Dev agent initializes with `mem_recall_for_module` context
- [x] Pre-edit hook queries `mem_recall_for_file` and surfaces memories
- [x] Post-edit hook offers `mem_capture` for code changes
- [x] Symbol-level recall via `mem_recall_for_symbol` integrated
- [ ] Team validates consistency with existing design decisions
- [ ] Measurable reduction in correction time demonstrated
- [ ] User trust survey shows improvement

## FlowForge
> This section is managed by FlowForge agents. Do not edit manually.
- HU status lifecycle: `draft` → `in-progress` → `done`
- Status is updated by forge-memory at `/flow-close`

---

## Verification & PM status

> **Implementation**: `ec8abea` (`feat(HU-033): add code-aware memory hooks to forge-dev`).
> **Verify**: PASS — `.ai-work/hu-033-code-aware-dev-agent/verify-report.md` (static wiring, 12/12 spec items, 14/14 plan items).
> **PM runtime**: PASS — executed 2026-11-06 with a controlled Dev-agent session against fixtures in project `pm-033-035-validation`:
>
> | PM | Result | Evidence |
> |----|--------|----------|
> | PM-1 | ✅ PASS | `mem_recall_for_module("skills/forge-dev")` → #4298 |
> | PM-2 | ✅ PASS | `mem_recall_for_file("skills/forge-dev/SKILL.md")` → #4298 |
> | PM-3 | ✅ PASS | `code-capture` Memory Signal emitted + offer text; no direct `mem_save` |
> | PM-4 | ✅ PASS | `mem_recall_for_symbol("InstallCommand")` → #4299 |
> | PM-5 | ✅ PASS | `no-such-module` returned descriptive empty result, no error |
>
> **AC status**: AC-1..AC-4 validated at runtime. AC-5/AC-6 are post-deployment outcome
> metrics (correction time, team trust) and remain open by design; they are not release blockers.
>
> **Data-path note**: the MCP `mem_save` signature does not expose `file_path`/`symbol`/`namespace`;
> the runtime fixtures were seeded via the Engram CLI (`engram save --file-path --symbol --namespace`).
> This is the gap tracked by HU-034 (code-aware capture, Part B). The recall hooks themselves work.

---

## Technical Debt (if applicable)
- Temporary coupling to engram-dotnet schema (ENG-416) will require migration work if schema changes
- FF-002 Parte A decision extraction may need updates once FF-001 memory tools are available
