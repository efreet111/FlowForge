# Cross-memory (epics) and fallback strategy

> **Version**: 1.0  
> **Topic**: User stories and graceful degradation

FlowForge assumes knowledge accumulates over time. New user stories usually depend on a larger epic or prior work. This document defines how **Phase 0 (Discovery)** maps cross-memory and how the system behaves when **engram-dotnet** is unavailable.

---

## 1. Phase 0: context discovery (epic mapping)

Before the Arch Agent writes a line of spec (Phase 1), the orchestrator runs **Phase 0**.

### Discovery Agent (`@forge-discovery`)

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

## References

- [`02-memory-strategy.md`](02-memory-strategy.md)
- [`forge-discovery/SKILL.md`](../skills/forge-discovery/SKILL.md)
- [`12-engram-tool-reference.md`](12-engram-tool-reference.md)
