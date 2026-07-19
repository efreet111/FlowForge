---
description: Read-only flow status — shows current phase, active feature, checkpoint, and open rework tickets.
---
# /flow-status — Flow inspection (read-only)

1. List folders under `.ai-work/` — resolve active `feature-slug`.
2. For the active feature, report:
   - **Phase**: which artifact exists (context-map → spec → plan → verify-report → summary).
   - **Checkpoint**: last CKP reached and its status (pending / approved / rejected).
   - **Rework**: read `rework_ticket.md` frontmatter if present — `status`, `cycle_count`, `severity`.
   - **Plan progress**: count `[x]` vs `[ ]` in `plan.md`.
3. Do NOT modify any file. Do NOT delegate. This is an orchestrator-inline read-only command.

See `ide/shared/workflow-orchestrator-parity.md`.
