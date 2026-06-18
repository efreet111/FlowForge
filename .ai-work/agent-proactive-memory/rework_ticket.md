---
cycle_count: 1
max_cycles: 3
status: resolved
severity: P1
source: verify
feature_slug: agent-proactive-memory
---

# Rework ticket — agent-proactive-memory

> Created: 2026-05-30  
> Status: resolved  
> Resolved: 2026-05-30  
> Source: forge-verify static audit

## Resolution summary

- Added "Session close protocol (mandatory)" to `skills/forge-memory/SKILL.md`
- Propagated to `ide/cursor/agents/forge-memory.md` and `ide/vscode/agents/forge-memory.agent.md`
- Added NFR-004 mem_search timeout line to `skills/forge-orchestrator/SKILL.md` STEP 3

## 1. Failure Reason

**FR-004 not fully implemented.** The spec requires `forge-memory` to call `mem_session_summary` mandatorily at `/flow-close` (with local fallback on MCP failure). The implementation added this obligation to the orchestrator and shared parity docs, but **`skills/forge-memory/SKILL.md` and `ide/cursor/agents/forge-memory.md` were not updated** (plan incorrectly stated "forge-memory no cambia").

Without explicit instructions in the memory agent, the original failure mode persists: `/flow-close` may complete without `mem_session_summary`.

Classified as **spec deviation**, not false green.

## 2. Affected Files

- `skills/forge-memory/SKILL.md`
- `ide/cursor/agents/forge-memory.md`
- (Optional) `ide/vscode/agents/forge-memory.agent.md` if it mirrors the skill
- (Optional) `skills/forge-orchestrator/SKILL.md` — add NFR-004 dedup timeout line in STEP 3

## 3. Correction Instruction

### Required (F-001)

Add a **"Session close protocol (mandatory)"** section to `skills/forge-memory/SKILL.md` after the PM-* gate section (or in Advanced Memory Tools), containing:

1. Before reporting CKP-4 complete, MUST call `mem_session_summary` with:
   - Goal, Discoveries, Accomplished, Next Steps, Relevant Files
   - project scoped to active feature (e.g. `team/flowforge`)
2. If MCP fails → write `.engram/local_memory/obs-<YYYYMMDD>-session-close.md` with same content + YAML frontmatter (`type: session_summary`).
3. Ingest any pending `.engram/local_memory/*.md` from the session (Smart Curation) before or as part of close.

Propagate the same section to `ide/cursor/agents/forge-memory.md` (compiled agent).

### Optional (F-002)

In `skills/forge-orchestrator/SKILL.md` STEP 3, add:
`If mem_search times out → skip dedup check and proceed to mem_save (NFR-004).`

### Do NOT

- Re-expand scope to other agents beyond forge-memory + optional orchestrator wording
- Change the Memory Signal design already verified as PASS

## 4. Close criteria

- [x] `skills/forge-memory/SKILL.md` explicitly requires `mem_session_summary` at close
- [x] `ide/cursor/agents/forge-memory.md` propagated
- [x] `ide/vscode/agents/forge-memory.agent.md` propagated
- [x] NFR-004 dedup timeout line in orchestrator STEP 3
- [ ] Re-run verify → PASS on FR-004 (pending confirm in verify-report)
