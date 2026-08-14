# FF-004: Code-Context Query for Arch Agent

**Status**: 🔴 Blocked  
**Priority**: P1  
**Effort**: M (2-3 days, after engram work)  
**Origin**: FF-IDEA-004  

---

## Problem Statement

The Arch agent in FlowForge designs systems and makes architectural decisions. Today, it has no code-aware memory — it can't ask "what have we decided about this module before?" The Arch agent's recommendations are stateless across sessions. If you run FlowForge twice on the same codebase, the Arch agent re-derives the same architectural decisions instead of building on prior decisions. That's wasteful and inconsistent.

## Proposed Solution

Wire `mem_recall_for_file` and `mem_decisions_for_module` into the Arch agent's context-loading phase. When the Arch agent starts a new flow:

1. Identify the target module(s) from the user's request
2. Query `mem_decisions_for_module(module)` for each
3. Use those decisions as constraints in the new architecture design
4. Optionally, surface "you're about to contradict decision X" warnings

## Success Criteria

- Second FlowForge run on the same codebase is faster (less re-derivation)
- Architecture decisions are consistent across runs unless explicitly revisited
- The Arch agent can explain "I made this decision because of memory Y"

## Dependencies

### engram-dotnet (HARD BLOCKERS)

| ENG | Feature | Status | Impact |
|-----|---------|--------|--------|
| **ENG-416** | Schema evolution (add `file_path`, `symbol`, `namespace` fields) | Ready, not started | **HARD BLOCKER** |
| **ENG-484** | Code-context query tools (`mem_recall_for_file`, `mem_decisions_for_module`) | Idea (P2) | **HARD BLOCKER** |

### FlowForge (no blockers)

- Arch agent's existing `mem_search` integration
- Context Map from forge-discovery (already identifies target modules)

## Effort Breakdown

| Component | Effort | Notes |
|-----------|--------|-------|
| ENG-416 (schema evolution) | L (1 week) | Migration strategy, backward compatibility |
| ENG-484 (code-context tools) | L (1 week) | New MCP tools, query logic |
| FF-004 (Arch agent wiring) | M (2-3 days) | Module detection, recall integration, contradiction warnings |
| **Total** | **XL (2-3 weeks)** | Cross-project coordination required |

**Note**: FF-004 shares the same engram dependencies as FF-001. If we implement ENG-416/484 for FF-001, FF-004 becomes M effort.

## Open Questions

1. How to identify "target modules" from the user's request? (LLM extraction? regex on file paths? manual specification?)
2. Should the Arch agent automatically recall decisions, or only when explicitly asked?
3. How to handle contradictions (Arch agent proposes X, but memory says Y)?
   - Option A: Warn user, let them decide
   - Option B: Auto-reject proposal, require justification
   - Option C: Surface both, let Arch agent reason about it
4. Should we cache recalled decisions in the Context Map to avoid repeated queries?

## Implementation Plan (after engram dependencies are ready)

### Phase 1: Module Detection (S)

```python
# Pseudocode
def detect_target_modules(user_request, context_map):
    # Extract file paths mentioned in request
    file_paths = extract_file_paths(user_request)
    
    # Map file paths to modules
    modules = set()
    for path in file_paths:
        module = path_to_module(path)  # e.g., "src/FlowForge.Installer/Update/" → "Update"
        modules.add(module)
    
    # Also include modules from Context Map
    modules.update(context_map.target_modules)
    
    return modules
```

### Phase 2: Memory Recall (M)

```python
# Pseudocode
def recall_architecture_context(modules):
    decisions = []
    for module in modules:
        module_decisions = engram_client.mem_decisions_for_module(module)
        decisions.extend(module_decisions)
    
    # Rank by relevance (recency, frequency of reference)
    decisions.sort(key=lambda d: d.relevance_score, reverse=True)
    
    return decisions[:20]  # Top 20 decisions
```

### Phase 3: Arch Agent Integration (M)

```python
# Pseudocode
class ForgeArchAgent:
    def execute(self, user_request, context_map):
        # Detect target modules
        modules = detect_target_modules(user_request, context_map)
        
        # Recall architecture context
        decisions = recall_architecture_context(modules)
        
        # Inject into prompt
        prompt = f"""
        You are the Arch agent. Design the architecture for: {user_request}
        
        ## Prior Decisions (from memory)
        {format_decisions(decisions)}
        
        ## Constraints
        - Do not contradict prior decisions unless explicitly justified
        - If you propose a decision that contradicts a prior one, explain why
        
        ## Context Map
        {context_map.to_markdown()}
        """
        
        # Generate spec
        spec = llm.generate(prompt)
        
        return spec
```

### Phase 4: Contradiction Detection (S)

```python
# Pseudocode
def detect_contradictions(proposed_decisions, prior_decisions):
    contradictions = []
    for proposed in proposed_decisions:
        for prior in prior_decisions:
            if contradicts(proposed, prior):
                contradictions.append({
                    "proposed": proposed,
                    "prior": prior,
                    "severity": "high" if prior.significance == "high" else "medium"
                })
    
    return contradictions
```

## Risk Assessment

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| Module detection is inaccurate | Medium | Medium | Allow manual override, show detected modules to user |
| Too many decisions recalled (context overflow) | Medium | Medium | Limit to top 20, rank by relevance |
| Contradiction detection has false positives | High | Low | Show contradictions as warnings, not errors |
| Arch agent ignores recalled decisions | Low | High | Enforce contradiction check, require justification |

## Recommended Path

1. **Blocked on engram-dotnet** — same dependencies as FF-001
2. **Implement FF-001 first** — if we do ENG-416/484 for FF-001, FF-004 is a natural extension
3. **Alternative (short-term)**: Improve Arch agent's `mem_search` usage — prompt it to search for "decisions about {module}" explicitly. This is effort **XS** and improves precision without engram changes.

## Related Features

- **FF-001** (Dev agent code-aware) — same engram dependencies, can share implementation
- **FF-002** (Decision extraction) — benefits Arch agent by populating decision memories
- **FF-005** (Contradiction detection) — Arch agent could use contradiction detection internally

---

**Next step**: Blocked on engram-dotnet ENG-416. Re-evaluate when ENG-416 status changes to "In Progress".

**Short-term alternative**: Improve Arch agent's `mem_search` prompts (XS effort, no blockers).
