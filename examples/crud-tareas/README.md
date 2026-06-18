# Example: Task CRUD flow (`crud-tareas`)

Sample artifacts from a **completed** FlowForge run (Task Manager API, Node.js + TypeScript + SQLite).

They illustrate what you should see under `.ai-work/{feature-slug}/` after discovery → spec → plan → dev → verify.

| File | Phase | Agent |
|------|-------|--------|
| [`context-map.md`](context-map.md) | 0 — Discovery | forge-discovery |
| [`spec.md`](spec.md) | 1 — Intent | forge-arch |
| [`plan.md`](plan.md) | 2 — Plan | forge-plan |
| [`verify-report.md`](verify-report.md) | 3 — Verify | forge-verify |
| [`summary.md`](summary.md) | 4 — Close | forge-memory |
| [`CASE-1-VALIDATION.md`](CASE-1-VALIDATION.md) | Item 2 checklist | Human / release gate |

**Status:** Case 1 **validated** (2026-05-27) — 20/20 tests, verify PASS, PM-* complete. See validation doc.

**How to use**

1. Copy this folder into your project: `.ai-work/crud-tareas/` (or compare side by side).
2. Run your own feature with `/flow-start` — do not treat these files as templates to edit in place in FlowForge repo.
3. Product error messages in the sample spec are in Spanish (demo choice); methodology docs are English.

**Reproduce the flow:** [`QUICKSTART.md`](../../QUICKSTART.md) · [`docs/14-flowforge-complete-reference.md`](../../docs/14-flowforge-complete-reference.md) (Test case 1).
