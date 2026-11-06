# Case 1 (Item 2) — Task CRUD validation

> **Improvement plan item 2** · Reference: [`docs/14-flowforge-complete-reference.md`](../../docs/14-flowforge-complete-reference.md) Case 1  
> **Original executable run:** Cursor + FlowForge **v0.4.x** · Demo app: `flowforge-demo-task-manager` (local) · **2026-05-27**
> **Documentation revalidation:** FlowForge **v0.5.0** · **2026-11-06**

## Result: **PASS** (v0.4.x executable run; phases 0–3 + automated layer)

| # | Criterion (item 2 / docs/14) | Status | Evidence |
|---|------------------------------|--------|----------|
| 1 | Discovery → `context-map.md` | ✅ | [`context-map.md`](context-map.md) — CKP-0 clear |
| 2 | Arch → `spec.md` (RF, RNF, GWT, PM-*, matrix) | ✅ | [`spec.md`](spec.md) |
| 3 | CKP-1 human approves spec | ✅ | Run completed in demo session |
| 4 | Plan → `plan.md` checklist | ✅ | [`plan.md`](plan.md) — dev marked items |
| 5 | CKP-2 human green-lights plan | ✅ | Run completed |
| 6 | Dev → code + tests (Ralph loop) | ✅ | 20/20 tests (`npm test` vitest) |
| 7 | Verify → `verify-report.md` PASS | ✅ | [`verify-report.md`](verify-report.md) |
| 8 | CKP-3 rework cycles = 0 | ✅ | No open `rework_ticket.md` for CRUD scope |
| 9 | PM-* manual tests | ✅ | PM-1–4 executed; PM-4 = automated suite green |
| 10 | Memory → `summary.md` | ✅ | [`summary.md`](summary.md) |
| 11 | CKP-4 deploy decision | 🟡 | Human gate — deploy optional for demo |

**Rework note:** Dashboard sync bug tracked separately (`bug-ticket-dashboard-sync.md` in demo) — out of CRUD RF scope; verify PASS still valid for API.

---

## v0.5.0 documentation revalidation (2026-11-06)

**Scope:** the committed artifacts and methodology, **not** a fresh executable run. The demo app
(`flowforge-demo-task-manager`) is not vendored in this repository, so `npm test` was not
re-executed here.

| Check | Result | Note |
|-------|--------|------|
| Artifact set complete | ✅ | `context-map.md`, `spec.md`, `plan.md`, `verify-report.md`, `summary.md` |
| Phase traceability | ✅ | Discovery → spec → plan → verify → summary intact |
| PM-* evidence | ✅ | PM-1..PM-4 recorded in `spec.md` |
| Current orchestrator name | ⚠️ Historical | Artifacts use the v0.4 flow; current orchestrator is `flowforge` |
| Current requirement IDs | ⚠️ Historical | Artifacts use v0.4 `RF/RNF`; current template uses `FR/NFR` |
| `revision_cycle.md` | ➖ N/A | Happy path; no CKP-1/CKP-2 rejection |
| Retention dry-run gate | ➖ N/A | Demo close predates HU-048; no prune executed |
| Executable re-run | ❌ Pending | Requires the external demo app |

**Verdict:** the artifacts remain valid evidence of the **v0.4.x** run. They are **not** a fresh
v0.5.0 executable PASS. Re-run the demo app under v0.5.0 (and refresh this file) before using it
as a hard release gate.

---

## How to repeat (any IDE)

1. Empty or minimal Node+TS repo  
2. `/flow-start Task CRUD — REST endpoints…`  
3. Approve spec → `/flow-plan` → approve plan  
4. `/flow-dev` → `npm test` green  
5. `/flow-verify` → PASS  
6. Run PM-* → mark `[x]` in spec  
7. `/flow-close` → CKP-4  

**OpenCode (Linux):** same steps after `bash ide/install.sh` — item **1** smoke is separate; this case validates the **methodology**, not the IDE pack.

---

## Commands used to confirm

```bash
cd flowforge-demo-task-manager
npm test   # 20 passed (2026-05-27)
```

---

## Gaps / follow-ups

| Gap | Owner | Priority |
|-----|-------|----------|
| Item **1** — OpenCode bundle smoke on Linux | You | P0 when on Linux |
| Publish demo repo | Optional | Low — `examples/` + docs/18 suffice |
| Re-run Case 1 on OpenCode to combine items 1+2 | Optional | After Linux session |
