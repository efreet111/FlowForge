# ADR-018 — FF-003 ↔ ENG-485 Onboarding Boundary (CLI orchestration vs memory aggregation)

> **Status**: **Accepted — applied** (2026-08-23) in `ff-003-onboarding-flow`
> **Date**: 2026-08-23
> **Feature**: `ff-003-onboarding-flow` (`flowforge onboard`)
> **Deciders**: Engineering (FlowForge methodology team) + engram-dotnet team
> **Links**: [Engram obs #115](https://engram/observation/115) · [spec §1.3 AD-1](../../.ai-work/ff-003-onboarding-flow/spec.md) · [ADR-003](ADR-003-pattern-search-mandate.md) · [ADR-017](ADR-017-installer-protection-policy.md) · [ENG-485/HU-055](https://github.com/efreet111/engram-dotnet)

---

## Context

During Phase 0 discovery of FF-003 (`flowforge onboard`), the twin feature **ENG-485/HU-055** was
found in the engram-dotnet backlog: `engram onboard --user <handle>` with `--format markdown|json`,
`--days`, `--output`. Both features would generate an onboarding briefing from engram memories.
Without an explicit boundary, both repos could implement duplicated memory **aggregation/ranking**
logic — the exact anti-pattern ADR-003 was created to prevent.

**Desired behavior**: one owner for memory aggregation/ranking (engram), one owner for CLI UX,
project detection and briefing rendering (FlowForge). No duplicated ranking logic.

---

## Decision drivers

- **Duplication risk**: two repos planning the same onboarding feature (R1 in context-map, prob High).
- **Composition over greenfield**: FF-003 is not greenfield — ≥6 reusable patterns exist in the installer.
- **Delegation of ranking**: relevance ranking is engram's domain (mem_search already returns ranked results).
- **No runtime coupling in v1**: FF-003 must ship without waiting for ENG-485.

---

## Decision

**FF-003 owns**: command registration, project detection, pre-flight checks, briefing rendering
(CLI interactive + ONBOARDING.md), and orchestration of existing engram primitives
(`mem_current_project`, `mem_context`, `mem_search`, `mem_stats`, `mem_get_observation`,
`mem_timeline` for drill-down only).

**FF-003 does NOT own**: memory aggregation/ranking, and does **not** implement `engram onboard`.

**When ENG-485/HU-055 ships**, `flowforge onboard` may delegate to `engram onboard` as an
optimization (follow-up), but **v1 has no runtime dependency on it**.

This boundary is standing policy for any future onboarding/ranking feature touching the installer.

---

## Consequences

### Positive

- No duplicated ranking logic across FlowForge and engram-dotnet.
- FF-003 shipped independently of ENG-485 schedule (v1 no runtime dependency).
- The installer stays a thin orchestrator over engram primitives → lower defect density, ADR-017 compliant.

### Negative / Costs

- If ENG-485 ships with different UX, `flowforge onboard` and `engram onboard` may briefly coexist
  with overlapping surface; reconciliation is a follow-up task (OQ-1 assumption).
- Delegation to `engram onboard` (future) requires a new integration point — not designed in v1.

### Applied evidence

- `.ai-work/ff-003-onboarding-flow/`: FR-001..FR-015 all implemented by orchestrating engram
  primitives only (no ranking logic in installer). Verdict PASS, 51/51 onboarding tests green.