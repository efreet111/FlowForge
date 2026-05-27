# /flow-verify — Audit implementation

1. Delegate to **forge-verify** with paths to `spec.md`, `plan.md`, and codebase.
2. Output: `.ai-work/{feature-slug}/verify-report.md` (PASS or findings).
3. On failure: `rework_ticket.md` → delegate **forge-dev** again.
4. CKP-3 🔴: if `cycle_count >= 3` in rework ticket → escalate; no 4th automatic attempt.

Verify does not evaluate PM-* (human runs those before `/flow-close`).

Orchestrator does not write verify-report inline.
