# Cross-memory (epics) and fallback strategy

> **Version**: 1.0  
> **Topic**: User stories and graceful degradation

FlowForge assumes knowledge accumulates over time. New user stories usually depend on a larger epic or prior work. This document defines how **Phase 0 (Discovery)** maps cross-memory and how the system behaves when **engram-dotnet** is unavailable.

---

## 1. Phase 0: context discovery (epic mapping)

Before the Arch Agent writes a line of spec (Phase 1), the orchestrator runs **Phase 0**.

### Discovery Agent (`forge-discovery`)

Invoked by the orchestrator during `/flow-start`, not as a standalone slash command.

- **Mission**: Read the human request and search project memory (MCP or local files) for related context, constraints, and past epics.
- **Model routing**: Fast, cheap models (Haiku, Flash, GPT-4o-mini) — read/classify only, not heavy architecture.

### Cross-memory mapping process

For a new story (e.g. “Add Google login”):

1. Extract keywords (`login`, `auth`, `oauth`).
2. Query memory (engram or local fallback).
3. If prior engrams exist (e.g. JWT story), capture IDs/references.
4. Produce a **Context Map** injected into the Arch Agent prompt.

*Example Phase 0 output for Arch:*

> “Arch Agent: this story belongs to **Epic: Identity v2**. Memory shows engram #104 required **HttpOnly** cookies. Include that in `spec.md`.”

---

## 2. Fallback strategy (graceful degradation)

The ideal path assumes **engram-dotnet** running locally with MCP. If a team cannot run .NET or the server is down, use **graceful degradation**.

### Local file protocol

On MCP failure (timeout, connection refused), agents switch to **file-based memory**:

1. **Location**: `./.engram/local_memory/obs-<timestamp>.md`
2. **Shape**: YAML frontmatter + markdown body (mirrors DB metadata)
3. **Search**: `grep` / ripgrep over `./.engram/local_memory/*.md` instead of `mem_search`

```markdown
---
title: "Fixed N+1 on user list"
type: "bugfix"
topic_key: "performance/user-list"
date: "2026-05-19"
---

## What
Added Include() in EF Core to eager-load roles.
```

FlowForge remains fully usable as a methodology without the Engram server — only persistent semantic search is degraded.

---

## 3. Offline-first lifecycle

Understanding the complete save lifecycle prevents the most common failure mode:
**closing the IDE does not trigger any save in Engram.**

### The full flow

```
Agent completes work
      │
      ▼
Agent emits ## Memory Signal in handoff (forge-arch / forge-dev only)
      │
      ▼
Orchestrator reads signal → applies 3-step curation
      │
      ├─ mem_save succeeds → SQLite local (ENGRAM_DATA_DIR)
      │         │
      │         ▼
      │   sync_mutations queue
      │         │
      │         ▼
      │   push to server when healthy (ENGRAM_SERVER_URL)
      │
      └─ MCP not responding → .engram/local_memory/obs-<timestamp>.md
                                    │
                                    ▼
                              forge-memory ingests at next /flow-close
                              (Smart Curation protocol in forge-memory skill)
```

### Closing the IDE ≠ automatic save

Cursor, OpenCode, Antigravity, and VS Code do not have hooks that call
`mem_session_summary` when the window closes. The agent must call it explicitly
before ending the session. This is enforced in the Memory Curation Protocol:
`forge-memory` calls `mem_session_summary` as a mandatory step in `/flow-close`.

If a session ends without `/flow-close`, knowledge is only in the `.ai-work/`
artifact files and any mid-session local fallback files. Run `/flow-close` in
the next session to synthesize and persist the summary.

### Enroll after first save

The SyncManager uses an enrollment list to know which projects to push.
After the first `mem_save` for a new project, the project must be enrolled:

```bash
# Via CLI (when server is healthy)
engram sync enroll --project <project-name>

# Or via HTTP
POST http://<ENGRAM_SERVER_URL>/sync/enroll/<project-name>
```

Without enrollment, local mutations accumulate in the queue but are never pushed.
This does not affect local reads/writes — only the sync to the remote server.

---

## References

- [`02-memory-strategy.md`](02-memory-strategy.md)
- [`forge-discovery/SKILL.md`](../skills/forge-discovery/SKILL.md)
- [`12-engram-tool-reference.md`](12-engram-tool-reference.md)
