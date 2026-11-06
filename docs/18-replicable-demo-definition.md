# FlowForge — Replicable Demo Definition (Release Gate)

> **Status**: Active release gate
>
> **Last reviewed**: 2026-11-06
>
> **Version**: 1.1
>
> **Applies to**: FlowForge v0.5.0 (`VERSION.md`)

## Goal

A **replicable demo** is minimal, reproducible evidence that FlowForge works in a real environment and that a third party can repeat the flow without author “telepathy.”

**A separate public demo repository is optional.** Replication is satisfied when the step-by-step runbook in this doc plus [`QUICKSTART.md`](../QUICKSTART.md) and [`ide/README.md`](../ide/README.md) are sufficient. The validated artifact set to compare against lives in [`examples/crud-tareas/`](../examples/crud-tareas/README.md).

---

## Recommended shapes

### Option A: separate demo repo (optional)

Example: `flowforge-demo-task-manager` (local only is fine).

- Version artifacts next to application code
- Keeps the methodology repo clean

### Option B: `examples/` inside FlowForge (implemented)

Single URL, but mixes methodology and sample product. The canonical reference set is [`examples/crud-tareas/`](../examples/crud-tareas/README.md) — a completed Task CRUD run validated on 2026-05-27 under v0.4.x (see [`CASE-1-VALIDATION.md`](../examples/crud-tareas/CASE-1-VALIDATION.md)). A fresh v0.5.0 executable re-run is still pending.

---

## Sample project scope

**Name:** Task Manager API

**Stack (low friction):**

- Node.js + TypeScript
- SQLite
- `vitest` + `supertest`

**V1 scope:**

- No auth (see [Case 2 in `docs/14`](14-flowforge-complete-reference.md))
- No UI
- Simple CRUD + validation

---

## Required artifacts (in the target project)

Under `.ai-work/{feature-slug}/` (kebab-case, no `FLOW-` prefix):

| File | Owner |
|------|--------|
| `context-map.md` | forge-discovery |
| `spec.md` | forge-arch (includes PM-* manual tests) |
| `plan.md` | forge-plan (checklist; dev marks `[x]`) |
| `verify-report.md` | forge-verify (not `cert-report.md`) |
| `rework_ticket.md` | verify → dev (if needed) |
| `revision_cycle.md` | orchestrator (`flowforge`) — CKP-1/CKP-2 rejections (max 3) |
| `summary.md` | forge-memory (after PM-* complete) |

**Canonical reference set:** [`examples/crud-tareas/`](../examples/crud-tareas/README.md) contains a completed run with `context-map.md`, `spec.md`, `plan.md`, `verify-report.md`, and `summary.md`. Use it to compare shapes — do not edit it in place (see its README).

---

## Definition of Done (replicable)

1. **Clean clone**: third party can run project tests successfully (`npm test` or equivalent).
2. **No hidden steps**: prerequisites documented.
3. **Traceable FlowForge**: `spec.md` and `plan.md` complete and consistent with code.
4. **Auditable verify**: `verify-report.md` with PASS (or documented reworks).
5. **IDE runbook**: steps below executed on at least one IDE (Cursor recommended first).
6. **Artifact parity**: produced `.ai-work/{slug}/` files match the shape of [`examples/crud-tareas/`](../examples/crud-tareas/README.md).

---

## Minimum runbook (any IDE)

On a clean machine:

1. Install FlowForge — stack installer (`install/install.sh` / `install/install.ps1`, includes the `flowforge` CLI and engram) or IDE agents only (`bash ide/install.sh` or `.\ide\install.ps1`; use `-ProjectPath` for a project bundle). See [`QUICKSTART.md`](../QUICKSTART.md) §1.
2. Prepare the target repo: `flowforge init <path>` scaffolds `.flowforge.json`, `AGENTS.md`, `docs/`, and `.ai-work/` (use `--no-flowdoc` if the project has no product doc layer). Open that repo in your IDE (empty or minimal app).
3. Optional briefing: `flowforge onboard --project <name>` summarizes recent decisions, patterns, and blockers (see [`forge-onboarding`](../skills/forge-onboarding/SKILL.md)).
4. `/flow-start Task CRUD — …` (describe the feature).
5. Approve **CKP-1** (`spec.md`).
6. `/flow-plan` → approve **CKP-2** (`plan.md`).
7. `/flow-dev` → tests green; dev marks plan checklist.
8. `/flow-verify` → **PASS** in `verify-report.md`.
9. Run **PM-*** from `spec.md`; mark `[x]`.
10. `/flow-close` → **CKP-4** deploy decision.

**Check phase at any time:** `/flow-status` (reads `.ai-work/` only).

**Bug during testing:** report in chat or run `/flow-rework` → orchestrator creates `rework_ticket.md` → `/flow-dev` — orchestrator must **not** patch code inline.

**Expected outcome:** artifacts under `.ai-work/{slug}/`, verify PASS, tests green, PM-* done before close. Compare with [`examples/crud-tareas/`](../examples/crud-tareas/README.md).

---

## Anti-criteria (does not count)

- “Works on my machine” with no instructions
- Requires external services without documented alternatives in v1
- No automated tests
- `spec.md` / `plan.md` contradict the code
- Orchestrator fixes bugs inline instead of routing through `/flow-rework` → `forge-dev`

---

## Related docs

- [`QUICKSTART.md`](../QUICKSTART.md) — 5-minute start (English) · [`QUICKSTART.es.md`](../QUICKSTART.es.md) (Spanish)
- [`README.es.md`](../README.es.md) — Spanish overview
- [`ide/README.md`](../ide/README.md) — IDE installer and path matrix
- [`ide/shared/workflow-orchestrator-parity.md`](../ide/shared/workflow-orchestrator-parity.md) — cross-IDE contract
- [`examples/crud-tareas/`](../examples/crud-tareas/README.md) — canonical completed artifact set
- [`docs/14-flowforge-complete-reference.md`](14-flowforge-complete-reference.md) — Test Case 1 (Task CRUD) and Test Case 2 (security)
- [`VERSION.md`](../VERSION.md) — current release version
