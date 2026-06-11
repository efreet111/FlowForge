# Architecture Decision Records (ADRs) — FlowForge Guide

> This guide explains what ADRs are, when to create one, how to write one,
> and how they fit into the FlowForge workflow. If you are new to this practice,
> start here before reading any ADR in this folder.

---

## What is an ADR?

An **Architecture Decision Record** is a short document that captures an important
architectural or design decision, together with its context and consequences.

The goal is simple: **six months from now, someone (including future you) should be
able to read this file and understand not just what was decided, but why — and what
alternatives were considered and rejected.**

Code tells you *what* the system does. An ADR tells you *why* it was built that way.

---

## When to write an ADR

Write an ADR when:

- You are choosing between multiple valid approaches with real trade-offs
- The decision affects more than one component, agent, or IDE
- The decision is likely to confuse a new contributor who reads the code
- The decision was contested — there were revisions, rejections, or debate
- You are establishing a new pattern or convention that others will follow

Do **not** write an ADR for:

- Routine implementation choices ("I used a for loop instead of map")
- Single-file, low-impact decisions
- Decisions that are obvious from reading the code

**In FlowForge terms:** if `forge-arch` produced a spec with `revision_cycle >= 1`,
that is a signal that an ADR might be worth writing. Contested decisions are the
prime candidates.

---

## Relationship to other FlowForge artifacts

```
CHANGELOG.md         ← "What changed" — user-facing, one line summary
docs/04-roadmap.md   ← "What is planned / why it was added to the backlog"
.ai-work/{slug}/spec.md  ← "What to build" — FR/NFR/PM acceptance tests
.ai-work/{slug}/plan.md  ← "How to build it" — file checklist, contracts
docs/decisions/ADR-NNN-*.md  ← "Why this approach" — decision + trade-offs
```

Each artifact serves a different reader:

| Reader | Wants to know | Goes to |
|--------|---------------|---------|
| User / adopter | What's new in this version | CHANGELOG |
| Contributor / new teammate | Where is this feature in the backlog | Roadmap |
| Dev agent running `/flow-dev` | What exactly to implement | spec + plan |
| Tech lead reviewing architecture | Why this approach and not others | ADR |
| Future self debugging a strange design | Why does it work this way | ADR |

---

## The ADR lifecycle

```
1. Triggered during planning (forge-arch or /flow-plan)
   → Decision has trade-offs worth recording

2. Written as "Proposed" or "Accepted"
   → If still under discussion: Proposed
   → If approved (CKP-1 or CKP-2 passed): Accepted

3. Referenced from spec.md, plan.md, CHANGELOG, roadmap
   → The ADR number (ADR-NNN) is the stable reference

4. Superseded when a future decision changes the approach
   → Do NOT delete; mark as "Superseded by ADR-NNN"
   → The history of decisions is as valuable as the current one
```

**Status values:**

| Status | Meaning |
|--------|---------|
| `Proposed` | Under discussion; not yet approved |
| `Accepted` | Decision approved and in effect |
| `Deprecated` | Still in effect but being phased out |
| `Superseded by ADR-NNN` | Replaced by a newer decision |

---

## Naming convention

```
ADR-001-kebab-case-title.md
ADR-002-another-decision.md
```

- Always zero-padded 3 digits: `001`, `002`, `012`, `100`
- Kebab-case title: short (3–6 words), describes the decision topic
- Never rename an existing ADR — the number is the stable reference

---

## ADR template

Copy this template when creating a new ADR:

```markdown
# ADR-NNN — Short Decision Title

> **Status**: Proposed | Accepted | Deprecated | Superseded by ADR-NNN
> **Date**: YYYY-MM-DD
> **Feature**: `feature-slug` (if applicable)
> **Links**: [spec](...) · [plan](...) · [related ADR](...)

---

## Context

Describe the situation that led to this decision. What problem were you solving?
What constraints existed? What triggered the need for a decision?

Keep this factual — no opinions yet.

---

## Decision drivers

List the criteria that mattered when evaluating options:

- Must be X
- Should avoid Y
- Acceptable to lose Z

---

## Options considered

### Option A — Short name

Brief description of the approach.

**Pros**: ...
**Cons**: ...
**Rejected because**: ...

---

### Option B — Short name (repeat for each option)

...

---

## Decision

State the chosen option in one clear sentence.

Describe the chosen design in enough detail that someone can implement it
without reading the options section. Include key contracts, formats, or
interfaces if relevant.

---

## Consequences

**Positive:**
- ...

**Negative / accepted:**
- ...
```

---

## Example: reading an existing ADR

Open [`ADR-001-memory-curation-protocol.md`](ADR-001-memory-curation-protocol.md) for a **methodology** ADR (memory curation).

Open [`ADR-002-scaffold-doc-policy.md`](ADR-002-scaffold-doc-policy.md) for a **scaffolding** ADR (`flow-init` / project template: what goes in `AGENTS.md`, `docs/DEVELOPMENT.md`, XML `///` policy).

Notice the structure:
1. **Context** explains the problem (offline-first failure report; agents not saving)
2. **Decision drivers** lists the constraints (IDE-agnostic, accept partial loss, etc.)
3. **Options considered** shows 3 alternatives and why 2 were rejected
4. **Decision** describes the chosen approach in enough detail to implement
5. **Consequences** is honest about what was accepted as a trade-off

This is the standard you should aim for. A future reader should be able to:
- Understand the problem in 30 seconds (Context)
- Trust that alternatives were genuinely evaluated (Options)
- Implement the decision without guessing (Decision section)
- Know what to watch out for (Consequences)

---

## FlowForge workflow integration

In the FlowForge flow, ADRs are created at **Phase 2 (Plan)** — after the spec is
approved (CKP-1) and before development starts.

```
/flow-start  → forge-discovery + forge-arch → spec.md (CKP-1)
                                                   ↓
/flow-plan   → forge-plan → plan.md + ADR (if needed) (CKP-2)
                                                   ↓
/flow-dev    → forge-dev (ADR is the "why" reference during implementation)
```

When `forge-memory` closes a session (`/flow-close`), it may use
`mem_promote_to_md` to create or sync ADRs from Engram observations — but
manual ADRs (like this one) are equally valid and preferred for architectural
decisions made during planning.

---

## Quick reference card

| Question | Answer |
|----------|--------|
| Where do ADRs live? | `docs/decisions/ADR-NNN-*.md` |
| Who writes them? | The planning agent (`forge-plan`) or the human during `/flow-plan` |
| When in the flow? | After CKP-1 (spec approved), before `/flow-dev` |
| What if the decision changes? | New ADR supersedes the old one; old one stays with `Superseded` status |
| Do I need one for every feature? | No — only for contested or architecture-level decisions |
| Can I reference them in code comments? | Yes: `// See ADR-001 for why this approach was chosen` |
