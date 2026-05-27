# FlowForge — Replicable Demo Definition (Release Gate)

## Goal

A **replicable demo** is minimal, reproducible evidence that FlowForge works in a real environment and that a third party can repeat the flow without author “telepathy.”

**A separate public demo repository is optional.** Replication is satisfied when the step-by-step runbook in this doc plus [`QUICKSTART.md`](../QUICKSTART.md) and [`ide/README.md`](../ide/README.md) are sufficient.

---

## Recommended shapes

### Option A: separate demo repo (optional)

Example: `flowforge-demo-task-manager` (local only is fine).

- Version artifacts next to application code
- Keeps the methodology repo clean

### Option B: `examples/` inside FlowForge (future)

Single URL, but mixes methodology and sample product.

---

## Sample project scope

**Name:** Task Manager API

**Stack (low friction):**

- Node.js + TypeScript
- SQLite
- `vitest` + `supertest`

**V1 scope:**

- No auth (Case 2 in docs/14)
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
| `summary.md` | forge-memory (after PM-* complete) |

---

## Definition of Done (replicable)

1. **Clean clone**: third party can run project tests successfully (`npm test` or equivalent).
2. **No hidden steps**: prerequisites documented.
3. **Traceable FlowForge**: `spec.md` and `plan.md` complete and consistent with code.
4. **Auditable verify**: `verify-report.md` with PASS (or documented reworks).
5. **IDE runbook**: steps below executed on at least one IDE (Cursor recommended first).

---

## Minimum runbook (any IDE)

On a clean machine:

1. Install FlowForge (`bash ide/install.sh` or `.\ide\install.ps1`; use `-ProjectPath` for project bundle).
2. Open an empty or minimal app repo in your IDE.
3. `/flow-start Task CRUD — …` (describe the feature).
4. Approve **CKP-1** (`spec.md`).
5. `/flow-plan` → approve **CKP-2** (`plan.md`).
6. `/flow-dev` → tests green; dev marks plan checklist.
7. `/flow-verify` → **PASS** in `verify-report.md`.
8. Run **PM-*** from `spec.md`; mark `[x]`.
9. `/flow-close` → **CKP-4** deploy decision.

**Bug during testing:** report in chat → orchestrator creates `rework_ticket.md` → `/flow-dev` (or “reporté un error”) — orchestrator must **not** patch code inline.

**Expected outcome:** artifacts under `.ai-work/{slug}/`, verify PASS, tests green, PM-* done before close.

---

## Anti-criteria (does not count)

- “Works on my machine” with no instructions
- Requires external services without documented alternatives in v1
- No automated tests
- `spec.md` / `plan.md` contradict the code
- Orchestrator fixes bugs inline instead of delegating to forge-dev

---

## Related docs

- [`QUICKSTART.md`](../QUICKSTART.md) — 5-minute start (English) · [`QUICKSTART.es.md`](../QUICKSTART.es.md) (Spanish)
- [`README.es.md`](../README.es.md) — Spanish overview
- [`ide/shared/workflow-orchestrator-parity.md`](../ide/shared/workflow-orchestrator-parity.md) — cross-IDE contract
