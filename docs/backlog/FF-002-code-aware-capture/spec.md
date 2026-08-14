# FF-002: Code-Aware Memory Capture in CKP-2 (Plan Phase)

**Status**: 🟡 Partially Ready (Parte A) / 🔴 Blocked (Parte B)  
**Priority**: P2  
**Effort**: S (Parte A) / L (Parte B)  
**Origin**: FF-IDEA-002  

---

## Problem Statement

When FlowForge's Plan phase produces a `plan.md` with architectural decisions, those decisions live in the artifact file but don't necessarily get captured as engram memories. They're "decision-like content" trapped in markdown.

## Proposed Solution

### Parte A: Extract decisions from plan.md (READY — no engram changes)

When `plan.md` is finalized in CKP-2, FlowForge's Memory agent extracts:
- Each `[DECISION]` row in the plan → engram memory (type: `decision`)
- Each `[CONVENTION]` mention → engram memory (type: `convention`)
- The capability matrix → engram memory (type: `capability`)

The capture is opt-in (user can disable) and memories are tagged with the plan's identifier for traceability.

### Parte B: Code-aware capture with file metadata (BLOCKED)

When decisions are captured, they include code metadata:
- `file_path`: which file the decision applies to
- `symbol`: which class/function the decision affects
- `namespace`: which module the decision belongs to

This requires code-aware tools in engram-dotnet (ENG-483, ENG-484).

## Success Criteria

### Parte A
- After a FlowForge session, engram memory store contains N new decisions (N = number of `[DECISION]` rows in final plan)
- A future Dev agent session on the same project retrieves these decisions via `mem_search`
- The team reports (qualitative) that "FlowForge now remembers our decisions across sessions"

### Parte B
- Decisions are queryable by file path (`mem_recall_for_file`)
- Decisions are queryable by module (`mem_decisions_for_module`)
- Code-aware recall is precise (low false positives)

## Dependencies

### Parte A (no blockers)

- FlowForge Memory agent (already exists, may need extension)
- Plan artifact format (already exists)
- `mem_save` tool in engram-dotnet (already exists)

### Parte B (HARD BLOCKERS)

| ENG | Feature | Status | Impact |
|-----|---------|--------|--------|
| **ENG-416** | Schema evolution (add `file_path`, `symbol`, `namespace` fields) | Ready, not started | **HARD BLOCKER** |
| **ENG-483** | Code-aware memory capture (`engram watch <file>`) | Idea (P2) | **HARD BLOCKER** |

## Effort Breakdown

### Parte A (Ready)

| Component | Effort | Notes |
|-----------|--------|-------|
| Parse plan.md for `[DECISION]`, `[CONVENTION]` | S (2-4 hours) | Regex or markdown parser |
| Generate `mem_save` calls | S (2-4 hours) | Map plan sections to memory types |
| Integrate into forge-memory agent | S (2-4 hours) | Hook into session close |
| **Total Parte A** | **S (1 day)** | Implementable today |

### Parte B (Blocked)

| Component | Effort | Notes |
|-----------|--------|-------|
| ENG-416 (schema evolution) | L (1 week) | Migration strategy |
| ENG-483 (code-aware capture) | L (1 week) | File watching, metadata extraction |
| FF-002 Parte B (wiring) | M (2-3 days) | Integration with code-aware tools |
| **Total Parte B** | **L (2-3 weeks)** | Cross-project coordination |

## Open Questions

### Parte A

1. Should the capture be in the Plan agent itself, or in a post-Plan hook (forge-memory)?
2. How to handle capture failures gracefully (if engram is down, don't block the plan)?
3. What if the user wants to selectively capture only some decisions?
4. Should we tag memories with the plan's `feature-slug` for traceability?

### Parte B

1. How to automatically detect which file/symbol a decision applies to? (LLM extraction? regex? manual tagging?)
2. Should we support retroactive tagging (add code metadata to existing decisions)?

## Implementation Plan (Parte A)

### Phase 1: Parser (S)

```python
# Pseudocode
def extract_decisions_from_plan(plan_md):
    decisions = []
    for line in plan_md.lines:
        if line.startswith("- [DECISION]"):
            decisions.append({
                "content": line.replace("- [DECISION]", "").strip(),
                "type": "decision",
                "source": "plan.md"
            })
    return decisions
```

### Phase 2: Memory Generation (S)

```python
def generate_mem_save_calls(decisions, feature_slug):
    for decision in decisions:
        yield {
            "tool": "mem_save",
            "params": {
                "what": decision["content"],
                "why": f"Architectural decision from {feature_slug}",
                "type": decision["type"],
                "topics": [feature_slug, "architecture", "decision"]
            }
        }
```

### Phase 3: Integration (S)

- Add to `forge-memory` agent's session close workflow
- After generating `summary.md`, call `extract_decisions_from_plan`
- For each decision, call `mem_save`
- Log count: "Captured N decisions from plan.md"

## Risk Assessment

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| Plan format changes (no `[DECISION]` markers) | Low | Medium | Document required format, validate in forge-plan |
| Too many decisions captured (noise) | Medium | Low | Opt-in capture, user can disable |
| engram is down during capture | Low | Low | Graceful degradation (log error, don't block) |

## Recommended Path

1. **Implement Parte A now** — delivers value (S effort, no blockers)
2. **Monitor usage** — if users find decisions useful, prioritize ENG-416/483 for Parte B
3. **Parte B** — implement after engram schema evolution is ready

## Related Features

- **FF-001** (Dev agent code-aware) — benefits from Parte B (code metadata)
- **FF-004** (Arch agent code-context) — benefits from Parte B
- **FF-003** (Onboarding) — can surface captured decisions to new team members

---

**Next step**: Implement Parte A. Create `/flow-start ff-002-parte-a-decision-extraction` when ready.
