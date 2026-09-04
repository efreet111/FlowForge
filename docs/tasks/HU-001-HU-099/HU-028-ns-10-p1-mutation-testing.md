---
hu_id: HU-028
title: "Mutation Testing Gate (NS-10 P1)"
status: draft
category: improvement
flowforge_slug: "ns-10-p1-mutation-testing"
---

# HU-028 — Mutation Testing Gate (NS-10 P1)

## User Story
As a FlowForge developer, I want a mutation testing gate that introduces synthetic code defects to validate test suite robustness, so that we can measure and improve our tests' effectiveness in catching real bugs before production.

## Acceptance Criteria (business-level)
- [ ] AC-1: Mutation testing tools are integrated into the CI/CD pipeline for all supported languages (.NET, JavaScript/TypeScript, Python)
- [ ] AC-2: Mutation testing runs in Stage 1 informational mode (PASS_DEGRADADO, never blocking)
- [ ] AC-3: Mutation testing runs in Stage 2 with 80% threshold and ≥5 survivors rule that triggers REWORK
- [ ] AC-4: Baseline measurement is captured before Stage 2 enforcement begins
- [ ] AC-5: Mental mutation checklist is available as fallback when tooling is unavailable

## Scenarios (SDD Spec)
### Happy Path
- [ ] **[Mutation Testing Stage 1 - Informational]**
  **GIVEN** a developer opens a pull request with code changes
  **WHEN** the mutation testing gate executes in informational mode
  **THEN** the pipeline completes with PASS_DEGRADADO status and mutation score is reported as a comment
  **🧪 Ref**: NS-10 P1, Stryker .NET / StrykerJS / mutmut

- [ ] **[Mutation Testing Stage 2 - Enforced]**
  **GIVEN** a baseline mutation score has been established and Stage 2 is activated
  **WHEN** a pull request mutation score falls below 80% OR has ≥5 survivors
  **THEN** the pipeline blocks with REWORK status and suggests specific mutants to address
  **🧪 Ref**: NS-10 P1

- [ ] **[Baseline Capture]**
  **GIVEN** P0 baseline has been implemented and P1 is being configured
  **WHEN** the team runs the initial mutation test baseline
  **THEN** the baseline score is recorded and stored for future comparison
  **🧪 Ref**: NS-10 P1, ADR-015

### Edge Cases
- [ ] **[Fallback Mental Checklist]**
  **GIVEN** mutation testing tools are unavailable for a specific language runtime
  **WHEN** the gate is invoked
  **THEN** the mental mutation checklist is presented as an alternative and pipeline continues with PASS_DEGRADADO
  **🧪 Ref**: NS-10 P1

- [ ] **[Degraded Mode - Tool Failure]**
  **GIVEN** mutation testing tools crash or timeout during execution
  **WHEN** the gate processes the error
  **THEN** pipeline continues with PASS_DEGRADADO and an incident is logged
  **🧪 Ref**: NS-10 P1

## Context / Notes
From the spec - NS-10 has TWO phases:
- P0 (DONE): Assertion validation + coverage gate — already shipped in PR #22 (2026-08-07)
- P1 (PENDING): Mutation Testing — future phase after P0 baseline is established

P1 Mutation Testing Details:
- Adds mutation testing as add-on gate using Stryker .NET, StrykerJS, mutmut (Python)
- Stage 1: informational only (PASS_DEGRADADO, never blocking)
- Stage 2: 80% threshold with ≥5 survivors → REWORK (after baseline measurement)
- Fallback: mental mutation checklist
- Effort: 6-8 days for P1 (17 tasks)
- Dependencies: Requires P0 baseline data before implementing P1
- Priority: P2
- ADR-015 exists for this feature

## Owner & Timeline
- **Owner**: @equipo-FlowForge
- **Target milestone**: v0.7.0
- **Dependencies**: NS-10 P0 baseline (already implemented)

## Definition of Done
- [ ] Mutation testing tools integrated for .NET (Stryker .NET)
- [ ] Mutation testing tools integrated for JavaScript/TypeScript (StrykerJS)
- [ ] Mutation testing tools integrated for Python (mutmut)
- [ ] Stage 1 informational mode operational
- [ ] Baseline mutation score captured and stored
- [ ] Stage 2 enforcement configured with 80% threshold and ≥5 survivors rule
- [ ] Mental mutation checklist fallback documented and accessible
- [ ] CI/CD pipeline updated with mutation testing gate
- [ ] Documentation updated (ADR-015 already exists, confirm alignment)

## FlowForge
> This section is managed by FlowForge agents. Do not edit manually.
- HU status lifecycle: `draft` → `in-progress` → `done`
- Status is updated by forge-memory at `/flow-close`

---

## Technical Debt (if applicable)
- Mutation testing can be resource-intensive; consider parallelization for large codebases
- Tool versions should be pinned to ensure reproducible mutation results
