# FF-005: Memory Curation — Contradiction Detection

**Status**: 🔴 Blocked  
**Priority**: P3  
**Effort**: L (1-2 days, after engram work)  
**Origin**: FF-IDEA-005  

---

## Problem Statement

As memory stores grow, contradictions become inevitable. Two memories that say "we use REST" and "we use gRPC" can both be true at different times, but the agent needs to know which is current. Without contradiction detection, the memory store becomes a source of confusion.

## Proposed Solution

A `mem_check_contradictions` tool (and corresponding FlowForge Memory Curation step):

- Run periodically (cron) or on demand
- Use embedding similarity + heuristics to find potentially-conflicting memories
- Surface them to the user with options: keep both, mark one as superseded, merge, or ignore
- For detected supersedence (one memory makes another obsolete), offer auto-marking with a confidence threshold

## Success Criteria

- A user with 500 memories runs `mem_check_contradictions` and gets a useful list of 5-10 conflicts
- Resolving each is a 30-second decision
- The agent's retrieval quality improves measurably after curation

## Dependencies

### engram-dotnet (HARD BLOCKERS)

| ENG | Feature | Status | Impact |
|-----|---------|--------|--------|
| **ENG-412** | Memory taxonomy & lifecycle (types: Decision, Insight, Transient) | Ready, not started | **HARD BLOCKER** — need type-aware conflict detection |
| **ENG-414** | Contradiccion temporal y supersedencia | Ready, depends on ENG-412 | **HARD BLOCKER** — core logic |
| **ENG-416** | Schema evolution (versioning, temporal fields) | Ready, not started | Soft blocker — helps with temporal contradictions |
| **ENG-418** | Busqueda hibrida (vector + FTS5 + metadata) | Ready, depends on embeddings | Soft blocker — embedding similarity improves detection |

### FlowForge (no blockers)

- Memory Curation Protocol (ADR-001) — conceptual parent
- forge-memory agent — integration point

## Effort Breakdown

| Component | Effort | Notes |
|-----------|--------|-------|
| ENG-412 (taxonomy) | M (1 week) | Define types, lifecycle rules |
| ENG-414 (contradiction logic) | L (1-2 weeks) | Heuristics, temporal supersedence |
| ENG-418 (hybrid search) | L (1-2 weeks) | Embeddings, similarity scoring |
| FF-005 (FlowForge integration) | M (2-3 days) | Cron job, UI for resolution |
| **Total** | **XL (4-6 weeks)** | Heavy engram work |

**Note**: This is the most engram-heavy feature. If engram team prioritizes ENG-412/414/418, FF-005 becomes M effort.

## Open Questions

1. What's the false-positive budget? Too many false positives = noise; too few = missing real conflicts.
2. Auto-resolve or always require human judgment?
   - Option A: Auto-resolve low-confidence contradictions, require human for high-confidence
   - Option B: Always require human (safer, but slower)
3. How to handle "soft" contradictions (memories that are related but not directly conflicting)?
4. Should we use LLM-as-Judge for contradiction detection (expensive but accurate) or heuristics (cheap but less accurate)?
5. How to prioritize which contradictions to resolve first? (by recency? by significance? by frequency of reference?)

## Implementation Plan (after engram dependencies are ready)

### Phase 1: Contradiction Detection (L)

```python
# Pseudocode
def detect_contradictions(memories):
    contradictions = []
    
    # Group by topic
    by_topic = group_by_topic(memories)
    
    for topic, topic_memories in by_topic.items():
        # Check for direct contradictions (same topic, conflicting content)
        for i, mem1 in enumerate(topic_memories):
            for mem2 in topic_memories[i+1:]:
                if contradicts(mem1, mem2):
                    contradictions.append({
                        "mem1": mem1,
                        "mem2": mem2,
                        "type": "direct",
                        "confidence": calculate_confidence(mem1, mem2)
                    })
        
        # Check for temporal supersedence (newer memory makes older obsolete)
        sorted_by_date = sort_by_date(topic_memories)
        for i, mem in enumerate(sorted_by_date[:-1]):
            newer = sorted_by_date[i+1]
            if supersedes(newer, mem):
                contradictions.append({
                    "mem1": mem,
                    "mem2": newer,
                    "type": "supersedence",
                    "confidence": calculate_supersedence_confidence(mem, newer)
                })
    
    return contradictions
```

### Phase 2: Resolution UI (M)

```
╭──────────────────────────────────────────────────────────╮
│  Memory Contradictions Detected: 7                       │
╰──────────────────────────────────────────────────────────╯

## Contradiction 1 (High Confidence)
- Memory A: "We use REST for all APIs" (2026-07-15, type: decision)
- Memory B: "We use gRPC for internal services" (2026-08-10, type: decision)

Options:
[1] Keep both (they're not actually contradictory)
[2] Mark A as superseded by B
[3] Mark B as superseded by A
[4] Merge into a new memory
[5] Ignore (don't show again)

Your choice: _
```

### Phase 3: Cron Job (S)

```bash
# Run weekly
0 0 * * 0 flowforge memory-curation --check-contradictions
```

### Phase 4: Integration with forge-memory (S)

- Add contradiction check to Memory Curation Protocol
- After session close, run `mem_check_contradictions`
- If contradictions found, prompt user to resolve

## Risk Assessment

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| High false-positive rate | High | Medium | Tune heuristics, allow user to adjust sensitivity |
| LLM-as-Judge is too expensive | Medium | Low | Use heuristics first, LLM only for ambiguous cases |
| Users ignore contradiction warnings | Medium | Medium | Make resolution mandatory before session close |
| Contradiction detection is slow (large memory stores) | Medium | Medium | Batch processing, async detection |

## Recommended Path

1. **Blocked on engram-dotnet** — heavy dependencies (ENG-412/414/416/418)
2. **Low priority** — only matters when memory store is large (500+ memories)
3. **Alternative (short-term)**: Use `mem_relations` with type `conflicts_with` to manually model contradictions. This is effort **S** and provides basic contradiction tracking without automatic detection.

## Related Features

- **FF-001** (Dev agent code-aware) — could benefit from contradiction detection (avoid recalling contradictory decisions)
- **FF-004** (Arch agent code-context) — same benefit
- **FF-003** (Onboarding) — could warn about contradictory decisions during onboarding

---

**Next step**: Blocked on engram-dotnet ENG-412/414. Re-evaluate when those are "In Progress".

**Short-term alternative**: Use `mem_relations` with `conflicts_with` for manual contradiction tracking (S effort, no blockers).
