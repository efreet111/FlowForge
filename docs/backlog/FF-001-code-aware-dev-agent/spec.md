# FF-001: Code-Aware Memory Tools in Dev Agent

**Status**: 🔴 Blocked  
**Priority**: P1  
**Effort**: XL (2-3 weeks including engram work)  
**Origin**: FF-IDEA-001  

---

## Problem Statement

The Dev agent in FlowForge generates code without code-context memory. It re-derives the same architectural decisions, forgets team conventions, and produces inconsistent code over time. A coding agent without project memory is a smarter autocomplete, not a real collaborator.

## Proposed Solution

Wire code-aware memory tools (`mem_recall_for_file`, `mem_recall_for_module`, `mem_recall_for_symbol`) into the Dev agent:

1. **Dev agent initialization**: Query `mem_recall_for_module(target_module)` to load relevant context
2. **Pre-edit hook**: Before editing a file, query `mem_recall_for_file(path)` and surface relevant memories
3. **Post-edit hook**: After editing, offer to capture the change as a memory (if it represents a decision or convention)
4. **Symbol-level recall**: When modifying a class/function, query `mem_recall_for_symbol(symbol_name)`

## Success Criteria

- Dev agent session produces code consistent with team's existing design decisions
- Time spent by developer correcting agent output drops measurably
- "I trust the Dev agent with my codebase" is a common user sentiment

## Dependencies

### engram-dotnet (HARD BLOCKERS)

| ENG | Feature | Status | Impact |
|-----|---------|--------|--------|
| **ENG-416** | Schema evolution (add `file_path`, `symbol`, `namespace` fields) | Ready, not started | **HARD BLOCKER** — without schema, no code metadata to query |
| **ENG-484** | Code-context query tools (`mem_recall_for_file`, etc.) | Idea (P2) | **HARD BLOCKER** — tools don't exist yet |
| **ENG-483** | Code-aware memory capture (`engram watch <file>`) | Idea (P2) | Soft blocker — without automatic capture, memories won't have code metadata |

### FlowForge (no blockers)

- Dev agent's existing MCP tool integration points
- Documentation of `mem_recall_for_*` semantics

## Effort Breakdown

| Component | Effort | Notes |
|-----------|--------|-------|
| ENG-416 (schema evolution) | L (1 week) | Migration strategy, backward compatibility |
| ENG-484 (code-context tools) | L (1 week) | New MCP tools, query logic |
| FF-001 (Dev agent wiring) | M (2-3 days) | Hooks, prompts, UX |
| **Total** | **XL (2-3 weeks)** | Cross-project coordination required |

## Open Questions

1. Should memory recall be automatic (agent always queries) or explicit (user triggers)?
2. How to handle memory conflicts (Dev agent retrieves a decision, but a more recent memory contradicts it)?
3. Latency budget: how much time can the agent spend on memory recall before the user notices?
4. Should we implement a fallback to `mem_search` with keywords if code-aware tools are unavailable?

## Alternatives Considered

### Alternative A: Use `mem_search` with file path as keyword

**Pros**: Implementable today, no engram changes required  
**Cons**: Imprecise, noisy, doesn't understand code structure  
**Verdict**: Rejected — too imprecise for code-aware workflows

### Alternative B: Implement code-aware memory in FlowForge (not engram)

**Pros**: No cross-project dependency  
**Cons**: Duplicates functionality, breaks "engram is the memory layer" architecture  
**Verdict**: Rejected — violates separation of concerns

## Risk Assessment

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| Schema migration breaks existing engram installations | Medium | High | Versioned migrations, backward compatibility |
| Code-aware queries are too slow (latency) | Medium | Medium | Caching, async recall, lazy loading |
| Low adoption (users don't trust agent with code memory) | Low | High | Transparent recall (show what was recalled), opt-in |

## Recommended Path

1. **Short-term**: Implement FF-003 (Onboarding) and FF-002 Parte A (decision extraction) — these deliver value without engram dependencies
2. **Medium-term**: Prioritize ENG-416 (schema evolution) in engram-dotnet
3. **Long-term**: Implement ENG-484 + FF-001 once schema is ready

## Related Features

- **FF-002** (Code-aware capture) — needs ENG-483, same schema blocker
- **FF-004** (Code-context for Arch agent) — same engram dependencies
- **FF-003** (Onboarding) — can be implemented independently

---

**Next step**: Blocked on engram-dotnet ENG-416. Re-evaluate when ENG-416 status changes to "In Progress".
