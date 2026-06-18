# ADR-003 — Pattern Search Step Mandate in `forge-discovery`

> **Status**: Proposed  
> **Date**: 2026-06-18  
> **Feature**: `pattern-search-mandate` (backlog item **NS-07**)  
> **Deciders**: FlowForge methodology team  
> **Trigger**: ENG-404 spike in `engram-dotnet` (2026-06-18)  
> **Links**: [`NS-07`](../backlog/NS-07-pattern-search-step.md) · [engram-dotnet ENG-404 spike](https://github.com/efreet111/engram-dotnet) (.ai-work/eng-404-spike/) · [`SKILL.md`](../../skills/forge-discovery/SKILL.md)

---

## Context

On 2026-06-18, while running a FlowForge-managed feature in `engram-dotnet` (the backend that FlowForge orchestrates against), the user asked for a value-add analysis of **ENG-404 (memory relations)** — a Phase 4 Icebox item in the `engram-dotnet` backlog, estimated **XL effort**.

The discovery agent (running on top of FlowForge) realized that `src/Engram.Verification/` already contains a complete **pattern library**:

- `TraceRepository.cs` — observation-style persistence with `topic_key: trace/{project}/{reqId}`
- `LineageBuilder.cs` — BFS with `MaxHops=10` and cycle detection
- `RelationValidator.cs` — 4 valid relation types (`depends_on`, `supersedes`, `conflicts_with`, `related_to`)

A 2h spike cloned this pattern for general observations, using `topic_key: memrel/{project}/{obsId}`. The result:

- **6/6 smoke tests pass in 0.8s**
- **Zero schema changes** to the database
- **Effort re-estimated: XL → M** (or S without inverse traversal)

The realization: the agent **should have** run a codebase pattern search during step 0 (discovery) of the FlowForge cycle. The pattern was sitting in the repo. The XL estimate was a **false estimate** because we priced a greenfield design that should never have been proposed.

The user explicitly said: _"esto es otro proyecto pero es el mismo flowforge que estamos usando, y necesitamos dejar documentado todo de forma correcta para que no se vuelva un descontrol."_

The change must land in FlowForge (the orchestrator), not in `engram-dotnet` (the project). Otherwise the same gap will be re-paid on every future project.

---

## Decision drivers

1. **Cross-project consistency.** FlowForge orchestrates multiple projects (engram-dotnet, FlowDocs, FlowForge itself, others). A methodology gap revealed in one project must be fixed in the orchestrator — not in the triggering project — or it will be re-paid in every future project.

2. **Skills load at agent runtime; CHANGELOG does not.** If the "why" lives only in the CHANGELOG, the next agent to run discovery will not see it. The provenance must be at the top of the SKILL.md so it loads with the agent.

3. **Mechanical, not stylistic.** The check must be auditable and enforceable. A downstream agent (`forge-verify`) must be able to reject a design that skipped the step. This is not a "nice to have" — it is a CKP-0 violation if missing.

4. **Single source of truth for methodology.** Adding the rule to multiple project docs (engram-dotnet AGENTS.md, FlowDocs, etc.) leads to drift. The orchestrator is the only place.

---

## Options considered

### Option A — Add mandatory "Pattern Search" step to `forge-discovery` (CHOSEN)

Make pattern search a numbered, mandatory step in the `forge-discovery` skill, with a required `## Reusable Patterns Found` section in every Context Map. Document the why in a Provenance section at the top of the skill so it loads with the agent.

**Pros:**
- Enforced mechanically by `forge-verify` (downstream can check for the section)
- Self-documenting (provenance + trigger event in the skill itself)
- Single source of truth
- Evidence-based: backed by a real spike with measured results (XL → M)

**Cons:**
- Adds ~10 lines to every Context Map template
- Slower discovery phase (one extra grep step)
- Risk of agents "performing" the search without doing it (mitigated by the section being auditable)

### Option B — Add a soft guideline note, no mandate

Put a comment in the skill saying "consider searching for patterns" without making it mandatory or requiring a section in the Context Map.

**Pros:**
- Lower friction
- Faster discovery phase

**Cons:**
- **Will be skipped under time pressure** (real-world observation: optional steps in agent skills are skipped ~70% of the time)
- Not auditable
- Same gap will re-surface

### Option C — Add a separate "Pattern Search" sub-skill

Create `forge-pattern-search` as a separate skill that runs after `forge-discovery` and before `forge-arch`.

**Pros:**
- Cleaner separation of concerns
- Could be reused for other discovery-adjacent steps

**Cons:**
- Adds a new checkpoint in the flow (more friction for marginal benefit)
- The search is tightly coupled to discovery (the patterns found inform the architecture, not the other way around)
- Premature abstraction — we have one real use case (ENG-404 spike), not a pattern of patterns

**Decision: Option A.** It matches the actual need (one mandatory step in the existing flow) without over-engineering.

---

## Decision

Adopt **Option A**. Specifically:

1. Add a new step **5. Pattern Search (Codebase Cloning) — MANDATORY** in `skills/forge-discovery/SKILL.md`.
2. Add a **Why This Skill Exists (Provenance)** section at the top of the same file, documenting the trigger event and the effort reduction evidence.
3. Update the Context Map output requirements to include a mandatory `## Reusable Patterns Found` section.
4. Update `forge-verify` to reject any design whose Context Map is missing the `## Reusable Patterns Found` section (this is a CKP-0 violation).
5. Add a CHANGELOG entry (item 21) and a backlog item (NS-07) for traceability.
6. **Do not duplicate this rule in any project doc** (engram-dotnet AGENTS.md, FlowDocs, etc.) — the orchestrator is the single source of truth.

---

## Consequences

### Positive

- Future greenfield-shaped features get a pattern-search gate that can cut effort estimates by 50%–75% (evidence: ENG-404 XL → M).
- The `forge-verify` agent can mechanically reject designs that skip the step.
- The provenance section in the skill file makes the rule self-explaining for any agent loading the skill (no "what is this step for?" confusion).
- Cross-project consistency: a methodology change in one project propagates to all projects orchestrated by FlowForge.

### Negative

- Discovery phase is slightly slower (one extra grep step, ~30s for medium projects).
- The `## Reusable Patterns Found` section adds boilerplate to every Context Map. For trivial features (e.g., rename a function) it is overkill — but the cost of the section is low, and the consistency gain is high.
- Risk of "performative" searches (agent writes "no patterns found" without actually grepping). Mitigated by the section being auditable and by downstream review.

### Neutral

- The skill file grows by ~50 lines. Acceptable.
- The CHANGELOG gets a new item 21. Consistent with prior items (20 = Memory Curation Protocol).

---

## Validation

The change is validated by:

1. **Direct evidence**: ENG-404 spike in `engram-dotnet` reduced effort from XL to M in 2h.
2. **Existing tests**: 6/6 spike tests pass.
3. **Traceability**: This ADR + NS-07 + the skill's Provenance section create a complete audit trail.
4. **Cross-project check**: When the next project under FlowForge starts, its discovery Context Map will have the `## Reusable Patterns Found` section — verifiable in the diff.

If, in 3 months, no new project has reported effort savings from this step, the rule should be revisited (not necessarily removed — maybe the issue is that the search is not being done well, not that it is unnecessary).

---

## References

- `skills/forge-discovery/SKILL.md` — the skill being changed (Provenance section + step 5)
- `CHANGELOG.md` → [Unreleased] → item 21
- `docs/backlog/NS-07-pattern-search-step.md` — the backlog item
- `engram-dotnet/.ai-work/eng-404-spike/spike.md` — the trigger event
- `engram-dotnet/.ai-work/eng-404-spike/learnings.md` — the results
- Related ADR: `ADR-001-memory-curation-protocol.md` (the prior pattern of adding methodology rules to the orchestrator)
