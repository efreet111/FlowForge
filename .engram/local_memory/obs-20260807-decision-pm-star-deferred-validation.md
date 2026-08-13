---
title: "PM-* manual tests for forge-verify gates deferred — require real project context, coverage tools, and test scenarios"
type: "decision"
topic_key: "verify/pm-star-validation"
date: "2026-08-07"
scope: "team"
project: "team/flowforge"
---

## What
PM-1..PM-5 of `test-quality-gates` (NS-10) were marked `[ ]` and **deferred as technical debt** with the note "Deferred - requires development context". Feature closed with PR #22 merge as deliverable. The PM-* tests validate: (PM-1) assertion validation flags a wrong expected value, (PM-2) coverage gate issues REWORK for <80% coverage on ≥5 lines, (PM-3) PASS_DEGRADADO for <80% on <5 lines, (PM-4) mental-checklist fallback when coverage tools are unavailable, (PM-5) coverlet integration on a .NET project.

## Why
The FlowForge methodology repo contains no host project to exercise the gates against. Each PM-* test needs: (a) a real spec + code with a git diff, (b) tests with deliberately wrong expectations / partial coverage, and (c) coverage tools installed on PATH (coverlet/istanbul/`--cov`). Building that harness is additional development work outside this cycle's deliverable. The human decided to close with the merge and track PM-* as technical debt rather than block — a CKP-4 human decision (deploy gate), not a false close.

## Where
- `.ai-work/test-quality-gates/spec.md` §4 (deferral note).
- `.ai-work/test-quality-gates/summary.md` §2 and §5 (deferred status + open items).
- `docs/decisions/ADR-015-mutation-testing.md` (validation plan, status: P0 shipped, validation deferred).

## Learned
1. **When a feature changes verification tooling rather than product code, plan PM-* execution against a fixture/demo host project from the start** — or explicitly defer and track as technical debt, as done here.
2. **A deferred PM-* must stay visible**: keep the `[ ]` checkbox (honest — not executed), annotate the deferral with date + reason, and surface it in the summary's open items so future sessions don't treat the feature as fully validated.
3. **Prerequisite checklist for running deferred PM-***: host project with a spec, real git diff (≥5 modified lines for REWORK path), crafted wrong-expected-value tests, coverage tools on PATH, and a way to remove tools from PATH to exercise the fallback.
