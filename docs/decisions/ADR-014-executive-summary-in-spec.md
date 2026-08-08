# ADR-014 — Mandatory Executive Summary in spec.md

> **Status**: Accepted  
> **Date**: 2026-08-06  
> **Feature**: `executive-summary-in-spec` (NS-09)  
> **Deciders**: Engineering (FlowForge methodology team)  
> **Links**: [`NS-09`](../backlog/NS-09-executive-summary-in-spec.md) · [`skills/forge-arch/SKILL.md`](../../skills/forge-arch/SKILL.md)  
> **Supersedes**: Initial proposal for conversational spec mode (rejected as over-engineering)

---

## Context

Users reported that `forge-arch` generates `spec.md` files exceeding 300 lines, requiring significant time to understand before making modifications or approving at CKP-1.

**User feedback**:
- _"Existen specs de más de 300 líneas detalladas"_
- _"Se tiene que invertir mucho tiempo en entender toda la spec"_

**Initial proposal (rejected)**: Conversational mode with 3 pauses, orchestrator-mediated multi-turn loop, WIP state management, resume capability. Analysis revealed this was over-engineering based on an incorrect assumption that users wanted active participation in spec construction.

**Corrected understanding**: Users want to **understand specs faster**, not participate in building them. The problem is **readability**, not **construction**.

---

## Decision drivers

- **Time efficiency**: Users should understand a spec in 2-3 minutes, not 15-20
- **Simplicity**: Solution should be minimal, not require orchestrator changes or state management
- **Immediate value**: Fix should work for all specs, not just complex ones
- **No side effects**: Solution should not add complexity to workflows, IDE parity, or agent behavior
- **CKP-1 compatibility**: Solution should enhance CKP-1 review, not replace it

---

## Options considered

### Option A — Conversational mode (REJECTED)

forge-arch pauses after 3 sections (Objective → FRs → NFR+PM+OQ), orchestrator mediates multi-turn loop, WIP state management with `_spec_wip.md`, resume capability.

**Pros**:
- User participates in construction
- Early correction of misunderstandings

**Cons**:
- Does NOT reduce spec length (300+ lines remain)
- High complexity: orchestrator changes, state management, resume, IDE parity
- +15% tokens, +2-3 min per spec
- Over-engineering: assumes users want participation, but they want readability
- CKP-1 already provides correction mechanism (OQ-* [BLOCKER], revision cycles)

**Decision**: ❌ **Rejected** — over-engineering. The problem is readability, not construction participation.

### Option B — Executive summary (✅ Accepted)

forge-arch always generates an executive summary (15-20 lines) as the first section of `spec.md`. No orchestrator changes, no state management, no resume.

**Pros**:
- Solves 80% of the problem (time-to-understand)
- Minimal implementation (1-2 days)
- No orchestrator changes
- No state management
- No IDE parity issues
- Works for all specs, regardless of length
- CKP-1 review enhanced (read summary first, then decide)

**Cons**:
- No user participation during construction
- Summary may not capture everything the user needs
- forge-arch must be good at summarizing

**Decision**: ✅ **Accepted** — minimal, high-value, no side effects.

### Option C — Spec splitting (NOT EVALUATED)

Split spec.md into multiple files (summary.md, fr.md, nfr.md, pm.md, oq.md).

**Pros**:
- Users read only what they need
- Each file is digestible

**Cons**:
- More files to manage
- forge-verify and forge-plan must search multiple files
- Significant paradigm change
- More implementation effort than Option B

**Decision**: ❌ **Not evaluated** — Option B is simpler and sufficient.

---

## Decision

**Option B: Mandatory executive summary in spec.md**

### Rationale

1. **Readability, not construction**: Users want to understand specs faster, not participate in building them. A summary achieves this without adding complexity.

2. **Minimal implementation**: Only `skills/forge-arch/SKILL.md` needs changes. No orchestrator, no state, no resume, no IDE parity.

3. **Immediate value**: Every spec gets a summary, regardless of length. Users can read 15-20 lines instead of 300+.

4. **CKP-1 enhancement**: Users can approve/reject based on summary without reading full spec. If they have questions, they dig deeper. This is how academic papers work (abstract → conclusions → full read if needed).

5. **No side effects**: Solution doesn't add complexity to workflows, doesn't increase token usage significantly, doesn't require new artifacts.

### Implementation approach

1. **Summary format** (15-20 lines):
   - **Objective**: 1 line — what the feature solves
   - **Scope**: 3 lines — what's in/out
   - **FRs table**: 6 lines — ID, name, description (no GWT scenarios)
   - **Key decisions**: 3 lines — architectural decisions made
   - **Risks**: 2 lines — identified risks and mitigations

2. **Placement**: As section 0 (before Objective), titled "## 0. Executive Summary"

3. **Always generated**: Regardless of spec length, summary is always present.

4. **Accuracy**: Summary must accurately reflect the full spec content. forge-arch generates summary after writing all sections.

---

## Consequences

### Positive

- **Time-to-understand reduced**: 2-3 minutes instead of 15-20
- **CKP-1 enhanced**: Users can approve/reject based on summary
- **Minimal implementation**: 1-2 days, no orchestrator changes
- **No side effects**: No state management, no resume, no IDE parity issues
- **Immediate value**: Every spec benefits, regardless of length

### Negative

- **No construction participation**: Users don't participate in building the spec (but this wasn't the real problem)
- **Summary quality depends on forge-arch**: If forge-arch is bad at summarizing, summary may be inaccurate (mitigated by testing)
- **Slight increase in spec length**: Summary adds 15-20 lines to every spec (negligible impact)

### Neutral

- **Spec format unchanged**: Summary is an addition, not a change to existing sections
- **CKP-1 unchanged**: Summary enhances CKP-1 but doesn't change its mechanics
- **Backward compatible**: Existing specs without summaries continue to work

---

## Implementation notes

### Files to modify

- `skills/forge-arch/SKILL.md` — add executive summary as required section 0
- `CHANGELOG.md` — add entry for NS-09

### Summary template

```markdown
## 0. Executive Summary

**Objective**: [1 line — what the feature solves]

**Scope**:
- In: [3-5 bullet points]
- Out: [2-3 bullet points]

**Functional Requirements**:
| ID | Name | Description |
|----|------|-------------|
| FR-001 | [name] | [1-line description] |
| FR-002 | [name] | [1-line description] |
| ... | ... | ... |

**Key Decisions**: [2-3 architectural decisions made]

**Risks**: [2-3 identified risks with mitigations]
```

---

## Validation plan

Once implemented, validate with:

1. **Summary presence**: Every new spec.md has an executive summary at the top
2. **Time-to-understand**: Users report faster spec review
3. **Summary accuracy**: Summary accurately reflects the full spec content
4. **CKP-1 experience**: Users approve/reject based on summary without reading full spec

**Success criteria**:
- Every spec has a summary
- Time-to-understand reduced by 80%
- Users report improved CKP-1 experience

---

## Future considerations

- **v2**: If users still report issues, consider Option C (spec splitting)
- **v2**: Configurable summary length in `.flowforge.json`
- **v3**: AI-powered summary refinement (summarize only changed sections)

---

## Why conversational mode was rejected

Initial analysis incorrectly assumed users wanted **active participation** in spec construction. User feedback actually indicated:
- Specs are too long to read
- Time investment is too high
- No mention of wanting to participate in building

Conversational mode would have:
- Added 5.5-6.5 days of implementation effort
- Required orchestrator changes, state management, resume, IDE parity
- NOT reduced spec length (300+ lines remain)
- NOT reduced time-to-understand significantly (only divided reading time)

Executive summary solves the actual problem (readability) with minimal effort and no side effects.

---

## References

- User feedback: Conversation 2026-08-06
- Related: NS-09 (executive-summary-in-spec backlog item)
- Affected skill: `skills/forge-arch/SKILL.md`
- Similar pattern: Academic papers (abstract → conclusions → full read)
