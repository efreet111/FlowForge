# FlowForge — Memory Strategy (Two Levels)

> **Version**: 0.2
> **Last updated**: 2026-05-27
> **Depends on**: engram-dotnet (persistence engine)

---

## 1. The problem

Agent memory has a fundamental issue: **everything feels important in the moment**.

An agent debugging a rare error for two hours may save 20 observations about it. A week later the bug is fixed and those 20 observations are noise — but they still consume space and pollute search.

Without cleanup, after three months of development you typically have:

- 2000+ observations
- ~40% noise (temporary debugging, failed experiments, ephemeral commands)
- FTS5 returning irrelevant hits
- Agents wasting tokens on junk context

---

## 2. The solution: two memory levels

```
┌──────────────────────────────────────────────────────────────┐
│                   MEMORY STRATIFICATION                      │
├──────────────────────────────────────────────────────────────┤
│                                                              │
│  LEVEL 1: OPERATIONAL (automatic, ephemeral)                 │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ • Contents: sessions, prompts, intermediate outputs,   │  │
│  │   debugging, tool_use, file_change, command            │  │
│  │ • Where: engram-dotnet DB (SQLite/Postgres)            │  │
│  │ • TTL: 30–90 days by type (auto-prune)                 │  │
│  │ • Writer: automatic (agent calls mem_save)             │  │
│  │ • Reader: agent for immediate context                 │  │
│  │ • Purpose: "what were we doing yesterday?"             │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                              │
│  LEVEL 2: STRUCTURED (deliberate, permanent)                 │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ • Contents: architecture decisions, RFCs, specs,       │  │
│  │   lessons learned, patterns, conventions               │  │
│  │ • Where: versioned .md in repo + DB metadata           │  │
│  │   (FTS5 + bidirectional link)                          │  │
│  │ • TTL: permanent (removed via PR like any code)        │  │
│  │ • Writer: Memory Agent (Phase 4) + human              │  │
│  │ • Reader: agent and human as project "constitution"    │  │
│  │ • Purpose: "why did we decide this?"                   │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

---

## 3. Flow between levels

```
AGENT WORKING
       │
       ▼
┌──────────────────┐
│  mem_save()      │  ← Agent saves automatically
│  → Level 1 (DB)  │    (tool_use, discoveries, etc.)
└──────┬───────────┘
       │
       ▼
┌──────────────────────────────────────────────────────┐
│                  MEMORY AGENT (Phase 4)               │
│                                                       │
│  1. Take observations from the current cycle          │
│  2. Decide which deserve Level 2:                     │
│     - Architecture decision? → .md                    │
│     - Reusable pattern? → .md                         │
│     - Transient debugging? → stays L1                 │
│     - Known error with fix? → L1 + maybe L2           │
│  3. For promotions to L2:                             │
│     a. Generate structured .md under docs/decisions/  │
│     b. Store metadata in DB with link to .md          │
│     c. Update CLAUDE.md / AGENTS.md when relevant     │
│                                                       │
└──────────────────────────────────────────────────────┘
       │
       ▼
┌──────────────────────────────────────────────────────┐
│                  MEMORY JANITOR (background)          │
│                                                       │
│  Daily (deterministic cron, $0 LLM):                   │
│  - Delete L1 observations past TTL                    │
│  - Soft-delete: SET deleted_at = NOW()                │
│  - Log: "Pruned 42 observations"                      │
│                                                       │
│  Weekly (Haiku batch, cheap):                         │
│  - Scan observations nearing expiry                   │
│  - Ask: "any worth L2 before delete?"                 │
│  - If yes: generate .md and update observation        │
│  - If no: let expire                                  │
│                                                       │
└──────────────────────────────────────────────────────┘
```

---

## 4. TTL by observation type

| Type | Suggested TTL | Behavior |
|------|---------------|----------|
| `tool_use` | 30 days | Auto-expire |
| `file_change` | 30 days | Auto-expire |
| `command` | 30 days | Auto-expire |
| `bugfix` | 90 days | Expires unless promoted to L2 |
| `pattern` | 90 days | Expires unless promoted to L2 |
| `learning` | 60 days | Expires unless promoted to L2 |
| `discovery` | 60 days | Expires unless promoted to L2 |
| `decision` | Never | Native L2 |
| `architecture` | Never | Native L2 |
| `session_summary` | Never | Never expires |

**Rules**:

- Observations with `topic_key` do not expire (deliberately structured knowledge)
- When promoted to Level 2, TTL is renewed or the L1 row is removed
- TTL is configurable via `ENGRAM_TTL_{TYPE}` environment variables

---

## 5. Promotion schema to Level 2

When an observation is promoted, a canonical `.md` file is created in the repo:

```
docs/decisions/
├── YYYY-MM-DD-short-title.md
├── index.md
└── templates/
    └── decision.md
```

### Example Level 2 document

```markdown
# ADR-001: Replace sessions with JWT

**Date**: 2026-05-13
**Type**: Architecture Decision Record
**Status**: Accepted

## Context
We needed to scale auth across multiple instances.
In-memory sessions did not work with round-robin.

## Decision
Use JWT with refresh tokens. Access token 15 min,
refresh token 7 days.

## Consequences
- +: Stateless, horizontal scale
- +: No shared Redis required
- -: Token revocation is harder
- -: JWT payload must not hold sensitive data

## References
- engram-dotnet observation: #42
- Related spec.md: /specs/auth-service.md
```

### Bidirectional link

The DB observation stores:

```
md_path: "docs/decisions/2026-05-13-replace-sessions-with-jwt.md"
```

The `.md` stores:

```
observation_id: 42
```

This enables:

- From DB → find the `.md`
- From `.md` → find the source observation
- FTS5 in DB surfaces related structured docs

---

## 6. Memory Janitor

**Not an agent. Two background processes:**

### 6.1 Daily prune (deterministic, $0)

```bash
#!/bin/bash
# Memory Janitor — daily prune
engram retention prune --type tool_use --older-than 30d --apply
engram retention prune --type file_change --older-than 30d --apply
engram retention prune --type command --older-than 30d --apply
engram retention prune --type bugfix --older-than 90d --apply
engram retention prune --type pattern --older-than 90d --apply
engram retention prune --type learning --older-than 60d --apply
engram retention prune --type discovery --older-than 60d --apply
```

### 6.2 Weekly promotion (Haiku batch, cheap)

```bash
#!/bin/bash
# Memory Janitor — weekly promotion scan
engram retention scan --for-promotion
# Scan observations past ~75% TTL
# For each, ask (via Haiku):
#   "Architecture decision, reusable pattern, or lesson learned?"
# If yes: generate .md and set observation.md_path
```

---

## 7. engram-dotnet integration

The two-level strategy requires engram-dotnet features:

| Feature | Status | Priority |
|---------|--------|----------|
| TTL per type (env vars) | ✅ Proposal written, not implemented | High — Phase 1 |
| `PruneOldObservationsAsync()` | ❌ Missing | High — Phase 1 |
| `mem_retention_prune` (MCP) | ❌ Missing | High — Phase 1 |
| `mem_retention_stats` (MCP) | ❌ Missing | High — Phase 1 |
| `md_path` on observation | ❌ Missing | High — Phase 2 |
| `mem_promote_to_md` (MCP) | ❌ Missing | High — Phase 2 |
| `.md` template engine | ❌ Missing | Medium — Phase 2 |
| `mem_verify_artifact` (MCP) | ❌ Missing | High — Phase 3 |
| `mem_traceability` (MCP) | ❌ Missing | High — Phase 3 |

See [03-engram-dotnet-gaps.md](03-engram-dotnet-gaps.md) for technical detail.
