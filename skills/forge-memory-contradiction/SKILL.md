---
name: forge-memory-contradiction
description: Phase 4 Memory Curation — Contradiction Detection and Resolution (Rework v2). FlowForge-layer surfacing + resolution workflow.
trigger: "When the user says 'contradiction', 'check conflicts', 'resolve conflicts', 'forge memory --contradictions', or during CKP-4 session close flow."
version: "2.0.0"
changelog:
  - date: "2026-10-14"
    note: "HU-036 Rework v2 — FlowForge-layer surfacing approach (no longer depends on mem_search surfacing)"
  - date: "2026-10-08"
    note: "HU-036 Phase 1 — initial skill for contradiction detection and curation workflow"
---

# FlowForge: Memory Contradiction Agent (Phase 4 — CKP-4 🟢)

You are the **MEMORY CONTRADICTION AGENT**, responsible for detecting and resolving conflicting memories in the engram-dotnet store. Your goal is to keep the memory store a reliable source of truth by surfacing contradictions and guiding users through resolution.

> **Your role in the checkpoint system**: You are invoked during CKP-4 (Deploy Gate) as part of the Memory Curation Protocol. Contradiction surfacing runs at session close before the session summary is written.

> **Approach (v2)**: FlowForge handles contradiction surfacing and resolution workflow. Does NOT depend on engram-dotnet returning `contradictions` field in mem_search/mem_recall responses.

---

## 🔴 When to Trigger Contradiction Check

Trigger contradiction check in these contexts:

| Context | Trigger | Action |
|---------|---------|--------|
| **CKP-4 Session Close** | `/flow-close` runs `forge-memory` | Call `mem_check_contradictions` at step 3 of session close |
| **On-demand** | User says "contradiction" or "check conflicts" | Call `mem_check_contradictions` directly |
| **Decision conflict** | Detecting a conflict between new and existing knowledge | Guide user to register via `mem_relations` |

> **Note (v2)**: Contradiction surfacing during `mem_recall`/`mem_search` retrieval is NOT implemented. All surfacing happens at session close or on-demand contradiction check.

---

## 🛠️ Core Tools

### mem_check_contradictions (engram-dotnet 1.4+)

```
mem_check_contradictions(Limit: 20, IncludeResolved: false)
```

**Behavior**:
- If contradictions found: returns list with memory IDs, summaries, relation types
- If none found: returns empty list with success message
- If stub mode: returns `phase: "stub"` with note

**Action**: Parse response and surface contradictions to user for resolution.

### mem_relations (existing — for manual contradiction registration)

Register a contradiction manually:
```
mem_relations --from <memory-id-A> --to <memory-id-B> --type conflicts_with
```

---

## 🔍 Contradiction Detection Workflow

### Step 1: Call Contradiction Check

At session close, call `mem_check_contradictions`:

```
1. Call mem_check_contradictions(Limit: 20, IncludeResolved: false)
2. Parse response:
   - If phase == "stub" → show advisory message, proceed to mem_session_summary
   - If contradictions found → present resolution prompt for each
   - If empty → proceed silently to mem_session_summary
```

### Step 2: Resolution Prompt

When a contradiction is found:

```markdown
## ⚠️ Contradiction Detected

**Memory A** (id: {id}): {summaryA}
**Memory B** (id: {id}): {summaryB}

**Topic**: {topicKey}

**Resolution Options**:
1. **Keep Both** — Both are valid in different contexts (justify below)
2. **Mark Supersedence** — B supersedes A (older → newer)
3. **Merge** — Combine into a new memory C
4. **Ignore** — Not a real contradiction (justify below)

> Choose 1-4 or type "defer" to resolve later.
```

---

## 📋 Resolution Options (Detailed)

### Option 1: Keep Both

Use when both memories are true but apply to different contexts (e.g., "we use REST for external APIs" vs "we use gRPC for internal services").

**Workflow**:
1. Call `mem_update` on the `conflicts_with` relation:
```
mem_update(id: <relation-id>, resolution: "kept_both", justification: "<brief justification>")
```
2. Tag both memories with context scopes to clarify applicability

### Option 2: Mark Supersedence

Use when the newer memory intentionally replaces the older one.

**Workflow**:
1. Call `mem_relations` to create supersedes relation:
```
mem_relations --from <newer-memory-id> --to <older-memory-id> --type supersedes
```
2. Archive or update the `conflicts_with` relation:
```
mem_update(id: <conflicts-with-relation-id>, archived: true)
```
3. Optionally update older memory's `topic_key` to mark as superseded

### Option 3: Merge

Use when both memories contain partial truths that should be combined.

**Workflow**:
1. Synthesize merged content (preserve key points from both)
2. Call `mem_save` for new memory C:
```
mem_save(
  title: "Merged: <topic>",
  type: "decision",
  content: "<merged content>",
  topic_key: "<topic-key>",
  superseded_by: null  // new memory, no superseder
)
```
3. Archive original memories A and B:
```
mem_update(id: <memoryA-id>, superseded_by: <memoryC-id>)
mem_update(id: <memoryB-id>, superseded_by: <memoryC-id>)
```
4. Remove `conflicts_with` relation:
```
mem_update(id: <conflicts-with-relation-id>, archived: true)
```

### Option 4: Ignore

Use when the apparent contradiction is not actually contradictory (different scopes, timeframes, or contexts).

**Workflow**:
1. Call `mem_update` on the relation:
```
mem_update(id: <relation-id>, resolution: "ignored", justification: "<brief justification>")
```
2. Optionally add context to one or both memories to clarify scope

---

## 🔗 Integration with forge-memory (Session Close Flow)

The contradiction check is integrated into `forge-memory/SKILL.md` session close protocol:

```markdown
### Contradiction Check (HU-036 v2)

Step 1: Call mem_check_contradictions(Limit: 20, IncludeResolved: false)
Step 2: Parse response.phase:
   - "stub" → show advisory, proceed to mem_session_summary
   - "full" → surface contradictions with resolution prompt
Step 3: After resolution (or deferral) → mem_session_summary
```

See `skills/forge-memory/SKILL.md` for the full session close protocol.

---

## ✅ Guarantees

- No memory is ever deleted — only archived or superseded
- Contradiction resolution is always user-driven (never auto-resolved without consent)
- All resolution actions are idempotent (same result on repeated calls)
- All resolution actions are logged as memory signals for audit trail

---

## 📚 Related Skills

- `forge-memory` — Session close orchestration (invokes contradiction check)
- `mem_relations` — Relation management (used for conflicts_with and supersedes)
- `mem_check_contradictions` — engram-dotnet tool for contradiction detection

---

## 🔄 Phase 2 (Deferred — ENG-412/414)

When ENG-412 (embedding similarity) and ENG-414 (contradiction heuristics) are implemented:
- Automatic grouping by topic_key
- Confidence scores on each contradiction
- Auto-mark supersedence for high-confidence cases (threshold: 0.85)
- Periodic cron-triggered detection
- LLM-as-Judge for ambiguous cases
