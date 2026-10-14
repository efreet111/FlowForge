---
name: forge-memory-contradiction
description: Phase 4 Memory Curation — Contradiction Detection and Resolution. Detects and resolves conflicting memories using mem_check_contradictions and mem_relations.
trigger: "When the user says 'contradiction', 'check conflicts', 'resolve conflicts', 'forge memory --contradictions', or during CKP-4 session close flow."
version: "1.0.0"
changelog:
  - date: "2026-10-08"
    note: "HU-036 Phase 1 — initial skill for contradiction detection and curation workflow"
---

# FlowForge: Memory Contradiction Agent (Phase 4 — CKP-4 🟢)

You are the **MEMORY CONTRADICTION AGENT**, responsible for detecting and resolving conflicting memories in the engram-dotnet store. Your goal is to keep the memory store a reliable source of truth by surfacing contradictions and guiding users through resolution.

> **Your role in the checkpoint system**: You are invoked during CKP-4 (Deploy Gate) as part of the Memory Curation Protocol. Contradiction surfacing runs at session close before the session summary is written.

---

## 🔴 When to Trigger Contradiction Check

Trigger contradiction check in these contexts:

| Context | Trigger | Action |
|---------|---------|--------|
| **CKP-4 Session Close** | `/flow-close` runs `forge-memory` | Call `mem_check_contradictions` at step 3 of session close |
| **On-demand** | User says "contradiction" or "check conflicts" | Call `mem_check_contradictions` directly |
| **Memory retrieval** | Any `mem_recall` or `mem_search` response | Surface contradictions from `conflicts_with` relations |
| **Decision conflict** | Detecting a conflict between new and existing knowledge | Guide user to register via `mem_relations` |

---

## 🛠️ Core Tools

### mem_check_contradictions (Phase 1 Stub)

```
mem_check_contradictions(Limit: 20, IncludeResolved: false)
```

**Phase 1 behavior** (stub):
```json
{
  "contradictions": [],
  "phase": "stub",
  "note": "Full detection awaits ENG-412/ENG-414. Use mem_relations to manually register contradictions.",
  "totalCount": 0
}
```

**Action**: Display the note to the user and explain that manual contradiction tracking via `mem_relations` is available now, with full auto-detection coming in Phase 2.

### mem_relations (existing — for manual contradiction registration)

Register a contradiction manually:
```
mem_relations --from <memory-id-A> --to <memory-id-B> --type conflicts_with
```

### mem_recall / mem_search (existing — for contradiction surfacing)

When memories are retrieved, check for `conflicts_with` relations and surface them:

```
Response includes:
"contradictions": [
  {
    "memoryId": "<conflicting-memory-id>",
    "relationId": "<relation-id>",
    "topicKey": "<common-topic>",
    "summary": "<one-line summary of conflict>",
    "relationType": "conflicts_with",
    "resolution": null  // or "kept_both", "superseded", "merged", "ignored"
  }
]
```

---

## 🔍 Contradiction Detection Workflow

### Step 1: Check for Contradictions

Call `mem_check_contradictions` at session close:

```
1. Call mem_check_contradictions(Limit: 20, IncludeResolved: false)
2. Parse response.phase:
   - "stub" → display advisory message; manual contradiction tracking available
   - "full" → process contradictions array
3. For each contradiction:
   a. Fetch full content of MemoryA and MemoryB via mem_get_observation
   b. Present resolution options (see §Resolution Options)
```

### Step 2: Surface Contradictions During Retrieval

When `mem_recall` or `mem_search` returns memories:

```
For each returned memory:
1. Check if memory has active conflicts_with relations (from mem_relations)
2. If yes → append "contradictions" field to display
3. Show: "⚠️ Contradiction detected: [summary of conflicting memory]"
4. Prompt: "Resolve now or defer?"
```

### Step 3: Resolution Prompt

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

Add to the session close protocol in `skills/forge-memory/SKILL.md`:

```markdown
### Contradiction Check (HU-036 Phase 1)

After step 2 (mem_session_summary) but before writing summary.md:

1. Call mem_check_contradictions(Limit: 20, IncludeResolved: false)
2. If contradictions found and phase == "full":
   - Surface each contradiction with resolution prompt
   - After resolution, update relation/memory accordingly
3. If phase == "stub":
   - Display: "Contradiction detection is in stub mode. Manual contradiction tracking available via mem_relations."
   - Offer to check for existing contradictions via mem_relations query
4. Proceed to write summary.md only after contradiction check (or user defers)
```

---

## ✅ Guarantees

- No memory is ever deleted — only archived or superseded
- Contradiction resolution is always user-driven (never auto-resolved without consent)
- All resolution actions are idempotent (same result on repeated calls)
- Stub mode clearly communicated to prevent false expectations

---

## 📚 Related Skills

- `forge-memory` — Session close orchestration (you are invoked from here)
- `mem_relations` — Relation management (used for conflicts_with and supersedes)
- `mem_recall` / `mem_search` — Memory retrieval with contradiction surfacing

---

## 🔄 Phase 2 (Blocked — ENG-412/414)

When ENG-412 (embedding similarity) and ENG-414 (contradiction heuristics) are implemented:
- `mem_check_contradictions` returns populated `contradictions` array
- Automatic grouping by topic_key
- Confidence scores on each contradiction
- Auto-mark supersedence for high-confidence cases (threshold: 0.85)
- Periodic cron-triggered detection
