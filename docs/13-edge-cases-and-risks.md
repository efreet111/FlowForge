# FlowForge — Edge cases, risks, and open questions

> **Type**: Risk register and brainstorming  
> **Status**: Living document  
> **Version**: 0.4.0

---

## 1. Open technical questions

**Concurrent writes to backup `.md` files:** If two agents write the same backup file at once, should we rely on the filesystem alone or add a light lock/queue?

---

## 2. Potential future capabilities

**Context poisoning guardrail:** An agent might plan from a stale engram (e.g. deprecated library). A lightweight validation phase could check whether a memory item still matches the current codebase before Phase 2 uses it.

**Conflict resolution agent:** Two agents on branches touching the same engram namespace need a watcher: *“Agent B is changing the same interface you are refactoring.”*

**Cost observability:** Console or web dashboard: USD per phase/epic — critical for SMB budgeting with cheap Asian model tiers.

---

## 3. Real-world failure modes

**Intent drift:** Human approves `plan.md` (CKP-2), but Dev improvises in Phase 3 and never returns to Plan.  
*Mitigation:* periodic drift check — compare code to `plan.md` every N commits.

**Multi-user memory latency:** Colombia push vs Spain pull — if engram sync lags, agents work on stale memory.  
*Mitigation:* solid offline-first sync; surface sync health in `mem_doctor`.

**Telephone game across phases:** Nuance from Phase 0 (cheap model) may dilute by Phase 3.  
*Mitigation:* artifacts on disk (`spec.md`, `plan.md`) as source of truth, not chat alone.

**Parallel Phase 4 writes:** Two Memory agents closing at once → filesystem conflicts.  
*Mitigation:* sequential write queue in engram-dotnet (incubator idea).

---

## 4. Use cases that may break the flow

**Mass refactors:** “Replace all JWT with OAuth” may fight old engrams still suggesting JWT. May need epic-level memory pruning or explicit “ignore prior auth engrams.”

**Onboarding without FlowForge:** Devs coding outside checkpoints → memory lineage breaks.  
*Mitigation:* team policy + optional git hooks reminding `/flow-start`.

**Weak models + complex rules:** Very cheap models may ignore orchestrator flow.  
*Mitigation:* use capable model for orchestrator; cheap models only on discovery/memory.

---

## 5. Open product question

Should `forge-orchestrator` block CKP-3 escalation if `forge-verify` reports invalid data lineage (not just failed tests)?

See incubator items in [`04-roadmap.md`](04-roadmap.md).
