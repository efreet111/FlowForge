# Contradiction Resolution Workflow

> **Scope**: HU-036 Rework v2. This document defines the resolution workflow for detected memory contradictions.
> **When to use**: When `mem_check_contradictions` returns contradictions, or when user requests contradiction check.

---

## Resolution Options Overview

| Option | When to Use | mem_relations action | mem_update action |
|--------|-------------|---------------------|-------------------|
| **Keep Both** | Both memories valid in different contexts | Tag `conflicts_with` | Update relation with `resolution: kept_both` |
| **Mark Supersedence** | Newer memory intentionally replaces older | Create `supersedes` B→A | Archive `conflicts_with` |
| **Merge** | Both have partial truths to combine | Create new memory C | Archive A and B with `superseded_by: C` |
| **Ignore** | Not a real contradiction | Tag `conflicts_with` | Update relation with `resolution: ignored` |

---

## Option 1: Keep Both

### When to Use
- The memories are both factually correct but apply to different contexts
- Examples:
  - "We use REST for external APIs" vs "We use gRPC for internal services"
  - "Team A uses Kubernetes" vs "Team B uses Docker Swarm"
  - "Q1 we prioritized performance" vs "Q2 we prioritized security"

### Prompt Template

```markdown
## Keep Both Resolution

**Memory A**: {summaryA}
**Memory B**: {summaryB}

These appear contradictory, but both may be true in different contexts.

Please provide a brief justification for keeping both:
(e.g., "They apply to different teams/services/timeframes")

> Justification: _
```

### Implementation

```bash
# Tag the conflicts_with relation with resolution
mem_update(
  id: "<relation-id>",
  resolution: "kept_both",
  justification: "<user-provided justification>"
)

# Optional: Add context tags to memories for clarity
mem_update(id: "<memoryA-id>", tags: ["context:external-api"])
mem_update(id: "<memoryB-id>", tags: ["context:internal-service"])
```

### Memory Signal (after resolution)

```markdown
## Memory Signal
- type: decision
- significance: medium
- summary: "Kept both memories for {topic} — they apply to different contexts: {justification}"
- topics: [contradiction-resolution, hu-036, keep-both]
```

---

## Option 2: Mark Supersedence

### When to Use
- The newer memory intentionally replaces the older one
- The older memory is outdated and should be archived
- Examples:
  - "We planned to use PostgreSQL" → "We decided to use MongoDB"
  - "Auth: sessions" → "Auth: JWT with refresh tokens"
  - "Architecture: monolith" → "Architecture: microservices"

### Prompt Template

```markdown
## Mark Supersedence Resolution

**Older Memory A** (id: {id}): {summaryA}
**Newer Memory B** (id: {id}): {summaryB}

Mark B as superseding A?

> Confirm: Y/N _
```

### Implementation

```bash
# Step 1: Create supersedes relation B → A
mem_relations(
  --from "<memoryB-id>",
  --to "<memoryA-id>",
  --type supersedes
)

# Step 2: Archive the conflicts_with relation
mem_update(
  id: "<conflicts-with-relation-id>",
  archived: true
)

# Step 3: Optionally update older memory's topic_key
mem_update(
  id: "<memoryA-id>",
  topic_key: "<original-topic>/superseded"
)
```

### Memory Signal (after resolution)

```markdown
## Memory Signal
- type: decision
- significance: high
- summary: "Memory {memoryB-id} supersedes {memoryA-id} for {topic}"
- topics: [contradiction-resolution, hu-036, supersedence]
```

---

## Option 3: Merge

### When to Use
- Both memories contain partial truths that should be combined
- The merged content is more accurate than either alone
- Examples:
  - "We use REST" + "We also support WebSocket" → "We use REST for sync, WebSocket for real-time"
  - "DB: PostgreSQL" + "DB: Redis cache" → "PostgreSQL primary, Redis cache layer"

### Prompt Template

```markdown
## Merge Resolution

**Memory A** (id: {id}): {summaryA}
**Memory B** (id: {id}): {summaryB}

Please provide the merged content for the new memory:

---
**New Memory C**:
Title: _
Content: _
Topic Key: {topicKey}
---
```

### Implementation

```bash
# Step 1: Save new merged memory
mem_save(
  title: "<merged-title>",
  type: "decision",
  content: "<merged-content>",
  topic_key: "<topicKey>",
  scope: "team"
)

# Returns: { id: "<memoryC-id>", ... }

# Step 2: Archive original memories with superseder reference
mem_update(id: "<memoryA-id>", superseded_by: "<memoryC-id>")
mem_update(id: "<memoryB-id>", superseded_by: "<memoryC-id>")

# Step 3: Archive conflicts_with relation
mem_update(id: "<conflicts-with-relation-id>", archived: true)
```

### Memory Signal (after resolution)

```markdown
## Memory Signal
- type: decision
- significance: high
- summary: "Merged {memoryA-id} and {memoryB-id} into {memoryC-id} for {topic}"
- topics: [contradiction-resolution, hu-036, merge]
```

---

## Option 4: Ignore

### When to Use
- The apparent contradiction is not actually contradictory
- Different contexts, scopes, or timeframes make both valid
- The user determines it is a false positive

### Prompt Template

```markdown
## Ignore Resolution

**Memory A** (id: {id}): {summaryA}
**Memory B** (id: {id}): {summaryB}

Why is this not a real contradiction?

> Justification: _
```

### Implementation

```bash
# Tag the conflicts_with relation as ignored
mem_update(
  id: "<relation-id>",
  resolution: "ignored",
  justification: "<user-provided justification>"
)
```

### Memory Signal (after resolution)

```markdown
## Memory Signal
- type: note
- significance: low
- summary: "Ignored apparent contradiction between {memoryA-id} and {memoryB-id}: {justification}"
- topics: [contradiction-resolution, hu-036, ignore]
```

---

## Deferral Flow

If the user chooses to defer resolution:

```bash
# Do not modify any relations or memories
# Log the deferral for follow-up

mem_save(
  title: "Deferred contradiction: {memoryA-id} ↔ {memoryB-id}",
  type: "task",
  content: "Contradiction on topic {topicKey} deferred for later resolution.\n\nMemory A: {summaryA}\nMemory B: {summaryB}\nReason for deferral: {user-stated-reason}",
  topic_key: "contradiction/hu-036/deferred",
  scope: "team"
)
```

---

## Bulk Resolution (When >5 Contradictions)

When many contradictions are found:

1. **Sort by topic** — group contradictions by `topicKey`
2. **Prioritize high-impact** — contradictions in active topic_keys first
3. **Batch prompts** — offer 3-5 at a time rather than one-by-one
4. **Track progress** — log each resolution as it completes

```markdown
## Bulk Resolution: {n} Contradictions Found

**Topic breakdown**:
- {topicKey1}: {count1} contradictions
- {topicKey2}: {count2} contradictions
- ...

**Resolution order**: {topicKey1} first (most active)

**Progress**: 0/{n} resolved

Shall we start with {topicKey1}?
```

---

## Quality Checklist (Before Closing Resolution)

- [ ] Both memories still accessible in store (archived ≠ deleted)
- [ ] `conflicts_with` relation archived (not deleted)
- [ ] New `supersedes` or resolution tag applied
- [ ] Justification provided for `kept_both` and `ignore`
- [ ] Memory signal saved for audit trail
- [ ] User confirmed resolution choice
