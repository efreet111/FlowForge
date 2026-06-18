# /flow-rework — Bug report → ticket → dev

When the human reports a failure, regression, or "esto no cumple":

1. **Do not** patch product code in the orchestrator thread.
2. Resolve `feature-slug` (active folder under `.ai-work/`).
3. Create or update `.ai-work/{feature-slug}/rework_ticket.md`:
   - Expected vs Actual
   - Steps to reproduce
   - Evidence, environment, severity
   - YAML frontmatter: `cycle_count` (increment after each failed fix cycle)
4. Delegate to **forge-dev** with the ticket as top priority.
5. After dev: optionally **forge-verify** if audit was the source.

If `cycle_count >= 3` → CKP-3 escalate to human.

See `ide/shared/workflow-orchestrator-parity.md`.
