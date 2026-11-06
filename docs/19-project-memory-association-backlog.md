# Backlog — Project memory association on first save

> **Status:** Proposed (not implemented) — last reviewed 2026-11-06
>
> **Priority:** P2 — UX / adoption (reduces duplicate `team/*` projects and accidental team sync)
>
> **Review basis (2026-11-06):** live Engram MCP catalog (34 tools), local engram-dotnet checkout `100a9be` (`src/Engram.Mcp/EngramTools.cs`) and FlowForge `.flowforge.json` / config model.
>
> **Related:** [engram-dotnet](https://github.com/efreet111/engram-dotnet) · [`06-engram-sync-convention.md`](06-engram-sync-convention.md) · [`12-engram-tool-reference.md`](12-engram-tool-reference.md)

---

## Problem

When an agent saves memory for a **new project** (no observations yet under that name), the user may:

- Have **similar projects** already in Engram (local or server)
- Want to **link** to an existing canonical project instead of creating a duplicate namespace
- Want **`scope: personal`** (private, not shared with the team) even if the repo exists on the team server
- Use the **shared PostgreSQL server** but **not enroll / not sync** this work to `team/{project}`

As of 2026-11-06 the save still proceeds; the user only gets a passive text warning (if similar names exist).

---

## Current behavior in engram-dotnet (re-verified 2026-11-06)

> Verified against the live MCP catalog (34 tools) and `src/Engram.Mcp/EngramTools.cs`.
> Items that could not be re-checked are explicitly marked **not re-verified**.

### 1. Similar-project hint on `mem_save` (passive only)

In `EngramTools.MemSave`, when the normalized project has **no observations yet**, the store lists existing project names and runs `ProjectDetector.FindSimilar`. If a match exists, the **save completes** and the response appends a warning string:

```text
⚠️ Project "flowforge" has no memories. Similar project found: "team/flowforge" (42 memories). Consider using that name instead.
```

Source: `src/Engram.Mcp/EngramTools.cs` (similar-project block after normalize, re-read 2026-11-06).

**Gaps vs desired feature:**

| Expected | Current (2026-11-06) |
|----------|----------------------|
| Ask before save | No — save is not blocked |
| Offer explicit choices (link / new / cancel) | No — text suggestion only |
| Structured payload for UI/agent | No — plain string in tool result |
| “Associate as reference” vs “rename to existing” | Only “consider using that name” |
| Verify detected project before writing | Yes — `mem_current_project` (detection only, no gate) |
| Preview similar projects without writing | No — `mem_save` has no `dry_run` / `confirm` |
| Repair name drift after the fact | Yes — `mem_project_redirects` + `mem_merge_projects` |

### 2. Project verification: `mem_current_project` (available, not a gate)

`mem_current_project` is a read-only MCP tool. It serializes `ProjectDetector.DetectProjectFull(cwd)` as JSON with `project`, `project_source`, `project_id`, `project_path`, `cwd`, `available_projects` and an optional `warning`.

- It lets the agent **confirm which project the current workspace resolves to** (and see ambiguity warnings) *before* the first `mem_save`.
- It does **not** list similar project names, does **not** propose a scope, and does **not** block or alter a save. It is a verification step, not the interactive “link / new / personal / save-once” gate described below.

### 3. Drift and merge: `mem_project_redirects` + `mem_merge_projects` (available)

- `mem_project_redirects` (read-only) lists project-name migrations (`from` → `to`, migrated at).
- `mem_merge_projects(from, to)` is destructive: it moves observations, sessions and prompts from one or more source names into a canonical project.
- Both are **post-hoc remediation** for drift; neither participates in the first-save decision.

### 4. Team vs personal scope

| `scope` | Stored project key (when `ENGRAM_USER` set) | Visibility / sync |
|---------|---------------------------------------------|-------------------|
| `team` | `team/{project}` | Shared; sync push when project **enrolled** |
| `personal` | `{user}/{project}` | Private per user; isolated on server under user namespace |

- Default if omitted: **`AutoClassifyScope(type)`** — most knowledge types (`decision`, `discovery`, `bugfix`, …) default to **`team`**. Ephemeral types (`tool_use`, `command`, …) default to **`personal`**.
- Agent must pass `scope` explicitly to force personal on a `discovery`/`decision` save.

Docs: [`06-engram-sync-convention.md`](06-engram-sync-convention.md), engram README memory JSON example.

> **Not re-verified (2026-11-06):** the exact namespacing helper (`ResolveNamespacedProject`) and enrollment/sync-push internals. The `scope` parameter and `AutoClassifyScope` behaviour are still present in `mem_save`.

### 5. Post-hoc merge (CLI, not at save time)

```bash
engram projects list
engram projects consolidate          # interactive merge similar names
engram projects consolidate --all    # batch
```

Uses the same `ProjectDetector.FindSimilar` algorithm. Operator-driven; not integrated into MCP save flow.

### 6. Roadmap upstream (engram-dotnet)

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
┌──────────────────────────────────────────────────────┐
│  mem_current_project / mem_search / FindSimilar      │
│  (mem_project_redirects for known name migrations)   │
└───────────────────┬──────────────────────────────────┘
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

**Building blocks available today (2026-11-06):** `mem_current_project` covers detection/verification and `mem_search` covers name lookup; `mem_merge_projects` + `mem_project_redirects` repair drift after the fact. Still missing: the interactive gate itself, a read-only candidate preview, and `engram.scope` persistence in `.flowforge.json`.

### Proposed engram-dotnet enhancements (optional, cleaner MCP)

| Option | Description | Breaking? | Current status (2026-11-06) |
|--------|-------------|-----------|-----------------------------|
| **A. `mem_resolve_project`** | Read-only tool: returns `{ detected, similar[], suggested_scope }` | No | **Not implemented** — absent from the 34-tool MCP catalog and from `EngramTools.cs` |
| **B. `mem_save` dry_run** | `confirm=false` returns candidates without write | No | **Not implemented** — `mem_save` exposes only `title`, `content`, `type`, `session_id`, `project`, `scope`, `topic_key`, `sync_project` |
| **C. Structured warnings** | JSON envelope with `similar_projects[]` (ROADMAP Phase 3 partial) | Minor | **Partially verified** — `mem_save` still appends plain-text warnings; no structured `similar_projects[]` observed. Roadmap Phase 3 envelope: **not re-verified** |
| **D. `mem_link_project`** | Alias local slug → canonical project id | New table / metadata | **Not implemented** — no such tool in the catalog |

**Recommendation:** start with **FlowForge skill + orchestrator gate** (no engram change); add **A + B** in engram if agents ignore text warnings. `mem_current_project` + `mem_merge_projects` are useful today but do not replace the gate.

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

| Layer | Owner | Effort (est.) | Current state (2026-11-06) |
|-------|-------|----------------|----------------------------|
| FlowForge orchestrator + memory skill gate | FlowForge | 2–4 h | Not started — no first-save gate exists |
| `.flowforge.json` schema fields `engram.project`, `engram.scope` | FlowForge (item **6**) | 1 h | `engram.project` present (`"flowforge"` in `.flowforge.json`); `engram.scope` **still missing** (config model reads `project` only) |
| `mem_resolve_project` MCP tool | engram-dotnet | 2–3 h | Not implemented |
| Docs EN in engram (save + scope + consolidate) | engram-dotnet | 1 h | Not re-verified |

**Already available (no work needed):** `mem_current_project` (project detection/verification), `mem_project_redirects` and `mem_merge_projects` (drift inspection and repair).

---

## References

- engram-dotnet: `ProjectDetector.FindSimilar`, `EngramTools.MemSave`, `AutoClassifyScope`
- engram-dotnet MCP tools (2026-11-06 catalog): `mem_current_project`, `mem_project_redirects`, `mem_merge_projects`, `mem_search`, `mem_save`
- FlowForge: [`02-memory-strategy.md`](02-memory-strategy.md), [`06-engram-sync-convention.md`](06-engram-sync-convention.md)
- engram ROADMAP Phase 3: project auto-detection + response envelope (**not re-verified**)
