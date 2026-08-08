---
title: "Metrics — test-quality-gates: 0 reworks, PM-* validation deferred as accepted tech debt"
type: "metrics"
topic_key: "metrics/tech-debt"
date: "2026-08-07"
scope: "team"
project: "team/flowforge"
---

## What
Feature `test-quality-gates` (NS-10) shipped via PR #22 (`775c678`, 2026-08-07) with 0 rework cycles and no SOLID/code-complexity concerns (methodology/docs feature — no product code, so test-coverage delta is N/A). The accepted technical debt is **PM-1..PM-5 manual validation, deferred**: requires a real project context, coverage tools (coverlet/istanbul/`--cov`), and crafted test scenarios that do not exist in the FlowForge repo.

## Why
Closing with the merge as deliverable while deferring manual validation keeps momentum on the roadmap (assertion validation + coverage gate shipped to all IDE parity files). The debt is bounded and documented: 5 manual tests, tracked in `spec.md` §4 and `summary.md` §5, with a clear prerequisite checklist for execution.

## Where
- `.ai-work/test-quality-gates/spec.md` §4 — deferral note + PM table.
- `.ai-work/test-quality-gates/summary.md` §4/§5 — metrics and open items.
- `docs/decisions/ADR-015-mutation-testing.md` — P0 shipped, validation pending.

## Learned
- **Tech debt to clear before the next feature that uses forge-verify gates**: run PM-1..PM-5 against a fixture host project; flip `[x]` in spec.md; update ADR-015 status to "fully validated".
- **Cycle time**: CKP timestamps were not recorded for this cycle → `cycle_time: unknown` (orchestrator should persist `metrics/timestamp/ckp*` per checkpoint for automatic metrics).
- **Trend note**: methodology/docs features have N/A test coverage — track docs-completeness (FR/NFR traceability, parity files updated) instead of code coverage for such cycles.
