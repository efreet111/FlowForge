# Backlog — Project memory association on first save

> **Status:** Proposed (not implemented)  
> **Priority:** P2 — UX / adoption (reduces duplicate `team/*` projects and accidental team sync)  
> **Related:** [engram-dotnet](https://github.com/efreet111/engram-dotnet) · [`06-engram-sync-convention.md`](06-engram-sync-convention.md) · [`12-engram-tool-reference.md`](12-engram-tool-reference.md)

---

## Problem

When an agent saves memory for a **new project** (no observations yet under that name), the user may:

- Have **similar projects** already in Engram (local or server)
- Want to **link** to an existing canonical project instead of creating a duplicate namespace
- Want **`scope: personal`** (private, not shared with the team) even if the repo exists on the team server
- Use the **shared PostgreSQL server** but **not enroll / not sync** this work to `team/{project}`

Today the save still proceeds; the user only gets a passive text warning (if similar names exist).

---

## Current behavior in engram-dotnet (verified)

### 1. Similar-project hint on `mem_save` (passive only)

In `EngramTools.MemSave`, when the normalized project has **no observations yet**, the store lists existing project names and runs `ProjectDetector.FindSimilar`. If a match exists, the **save completes** and the response appends a warning string:

```text
⚠️ Project "flowforge" has no memories. Similar project found: "team/flowforge" (42 memories). Consider using that name instead.
```

Source: `src/Engram.Mcp/EngramTools.cs` (similar-project block after normalize).

**Gaps vs desired feature:**

| Expected | Current |
|----------|---------|
| Ask before save | No — save is not blocked |
| Offer explicit choices (link / new / cancel) | No — text suggestion only |
| Structured payload for UI/agent | Plain string in tool result |
| “Associate as reference” vs “rename to existing” | Only “consider using that name” |

### 2. Team vs personal scope

| `scope` | Stored project key (when `ENGRAM_USER` set) | Visibility / sync |
|---------|---------------------------------------------|-------------------|
| `team` | `team/{project}` | Shared; sync push when project **enrolled** |
| `personal` | `{user}/{project}` | Private per user; isolated on server under user namespace |

- Default if omitted: **`AutoClassifyScope(type)`** — most knowledge types (`decision`, `discovery`, `bugfix`, …) default to **`team`**. Ephemeral types (`tool_use`, `command`, …) default to **`personal`**.
- Agent must pass `scope` explicitly to force personal on a `discovery`/`decision` save.

Docs: [`06-engram-sync-convention.md`](06-engram-sync-convention.md), engram README memory JSON example.

### 3. Post-hoc merge (CLI, not at save time)

```bash
engram projects list
engram projects consolidate          # interactive merge similar names
engram projects consolidate --all    # batch
```

Uses the same `ProjectDetector.FindSimilar` algorithm. Operator-driven; not integrated into MCP save flow.

### 4. Roadmap upstream (engram-dotnet)

Phase 3 (breaking, planned): auto-detect project via `DetectProjectFull`, remove `project` from write tools, **project envelope** in every response (`project`, `project_source`, `warning`). See engram `docs/ROADMAP.md`.

That improves detection but does **not** replace an interactive “associate or stay independent?” gate.

---

## Proposed feature (FlowForge + engram)

### User story

> As a developer opening a **new repo**, when the agent is about to call `mem_save` for the first time in this workspace, I want to **choose** whether memories belong to an **existing project**, a **new team project**, or **personal-only** work — so I do not pollute team memory or create duplicate project names.

### Proposed flow (FlowForge layer — CKP-style gate)

Trigger: first `mem_save` in session **or** first save when `ListProjectNames` has no match for detected project.

```
Agent detects new project (git remote / folder / ENGRAM_PROJECT)
        │
        ▼
┌───────────────────────────────────────────┐
│  mem_search / projects list / FindSimilar │
└───────────────────┬───────────────────────┘
                    ▼
        Present to HUMAN (or orchestrator CKP):
        ┌─────────────────────────────────────┐
        │ A) Link to existing: team/flowforge │
        │ B) New team project: team/my-app    │
        │ C) Personal only: scope=personal    │
        │ D) Save once without default       │
        │    (do not ask again this session)  │
        └─────────────────────────────────────┘
                    ▼
              mem_save with chosen project + scope
```

**Rules:**

- **Personal (C):** memories stay under `{user}/{project}`; user can use server sync without sharing with team (no `team/` prefix).
- **Team (A/B):** `team/{slug}`; only syncs if project enrolled — user may keep local-only SQLite without enroll.
- **Reference link (A):** use canonical project name for all future saves in this workspace; optional `.flowforge.json` → `"engram": { "project": "flowforge", "scope": "team" }`.

### Proposed engram-dotnet enhancements (optional, cleaner MCP)

| Option | Description | Breaking? |
|--------|-------------|-------------|
| **A. `mem_resolve_project`** | Read-only tool: returns `{ detected, similar[], suggested_scope }` | No |
| **B. `mem_save` dry_run** | `confirm=false` returns candidates without write | No |
| **C. Structured warnings** | JSON envelope with `similar_projects[]` (ROADMAP Phase 3 partial) | Minor |
| **D. `mem_link_project`** | Alias local slug → canonical project id | New table / metadata |

**Recommendation:** start with **FlowForge skill + orchestrator gate** (no engram change); add **A + B** in engram if agents ignore text warnings.

---

## Acceptance criteria (when implemented)

- [ ] New workspace + first save → human sees **similar projects** (if any) and **team vs personal** choice
- [ ] Choosing **personal** → subsequent saves use `scope=personal` without team visibility
- [ ] Choosing **existing team project** → no duplicate `team/foo` vs `team/flowforge` drift
- [ ] Choice persisted in `.flowforge.json` or `.engram.json` for the repo
- [ ] Documented in QUICKSTART + `forge-memory` / `forge-discovery` skills
- [ ] Works with MCP unavailable (fallback: grep `.engram/local_memory/`)

---

## Split of work

| Layer | Owner | Effort (est.) |
|-------|-------|----------------|
| FlowForge orchestrator + memory skill gate | FlowForge | 2–4 h |
| `.flowforge.json` schema fields `engram.project`, `engram.scope` | FlowForge (item **6**) | 1 h |
| `mem_resolve_project` MCP tool | engram-dotnet | 2–3 h |
| Docs EN in engram (save + scope + consolidate) | engram-dotnet | 1 h |

---

## References

- engram-dotnet: `ProjectDetector.FindSimilar`, `EngramTools.MemSave`, `AutoClassifyScope`
- FlowForge: [`02-memory-strategy.md`](02-memory-strategy.md), [`06-engram-sync-convention.md`](06-engram-sync-convention.md)
- engram ROADMAP Phase 3: project auto-detection + response envelope
