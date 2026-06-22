# /flow-verify — Audit implementation

1. Delegate to **forge-verify** with paths to `spec.md`, `plan.md`, and codebase.
2. forge-verify **always** writes `.ai-work/{feature-slug}/verify-report.md`.
3. Read `verify-report.md` and branch on verdict:
   - **PASS** → proceed to `/flow-close`.
   - **PASS_DEGRADADO** → do NOT close. Ask human to run tests manually first.
   - **PENDING** → pause. Ask human how to proceed (runtime unavailable).
   - **REWORK** → read `rework_ticket.md`. If `status: "open"` → delegate **forge-dev** again. If `status: "resolved"` → treat as PASS.
4. CKP-3 🔴: if `cycle_count >= 3` in rework ticket → escalate; no 4th automatic attempt.

Verify does not evaluate PM-* (human runs those before `/flow-close`).

Orchestrator does not write verify-report inline.
