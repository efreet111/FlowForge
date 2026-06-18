# FlowForge — engram-dotnet Feature Reference

> **Version**: 0.4 (implemented feature reference — all items **completed**)
> **Last updated**: 2026-06-01
> **Repository**: [efreet111/engram-dotnet](https://github.com/efreet111/engram-dotnet)

> **Status summary:** All features from the original gap analysis (TTL/pruning, Level-2 promotion, verification tools, doctor diagnostic, and offline-first sync) are **fully implemented** in engram-dotnet. This doc serves as architecture context and MCP tool mapping — not as a backlog.

---

## Overview: Implemented Features

All major features originally identified in the gap analysis have been implemented and tested. Use [`docs/12-engram-tool-reference.md`](12-engram-tool-reference.md) for the complete MCP tool catalog.

### Technical Stack

| Layer | Technology |
|-------|------------|
| Language | C# / .NET 10 LTS |
| HTTP | ASP.NET Core Minimal API (Kestrel) |
| Database | SQLite via Microsoft.Data.Sqlite + FTS5 **or** PostgreSQL via Npgsql + tsvector |
| MCP | ModelContextProtocol NuGet (Microsoft official) |
| CLI | System.CommandLine |
| Auth | Microsoft.AspNetCore.Authentication.JwtBearer (optional) |
| Tests | xUnit + WebApplicationFactory + Testcontainers.PostgreSql |

### Project Structure

```
engram-dotnet/
├── src/
│   ├── Engram.Store/        ← Core engine: IStore + 3 implementations
│   ├── Engram.Server/       ← HTTP REST API (ASP.NET Core)
│   ├── Engram.Mcp/          ← MCP Server (stdio transport, 15 tools)
│   ├── Engram.Sync/         ← Git-friendly sync (gzip + JSONL)
│   └── Engram.Cli/          ← CLI entry point + DI wiring
├── tests/
│   ├── Engram.Store.Tests/       ← 110 tests
│   ├── Engram.Postgres.Tests/    ← 26 tests
│   ├── Engram.Server.Tests/      ← 19 tests
│   ├── Engram.Mcp.Tests/         ← 34 tests
│   └── Engram.HttpStore.Tests/   ← 30 tests
└── config/                      ← MCP configs for Cursor, VS Code
```

### Fully Implemented Features

| Feature | Status | Reference |
|---------|--------|-----------|
| IStore with 22 methods (Strategy Pattern) | ✅ Complete | SqliteStore, PostgresStore, HttpStore |
| Hash deduplication (15 min window) | ✅ Complete | `DeduplicateByHashAsync` |
| topic_key upsert (evolving topics) | ✅ Complete | `SaveObservationAsync` with topic_key |
| Sessions with start/end/summary | ✅ Complete | Session management API |
| FTS5 search (title, content, type, project, topic_key) | ✅ Complete | Full-text search with ranking |
| Multi-user namespacing (X-Engram-User) | ✅ Complete | Server middleware |
| JSON Export/Import | ✅ Complete | CLI + API |
| Obsidian Export | ✅ Complete | `mem_obsidian_export` |
| CLI commands | ✅ Complete | search, save, context, stats, export, import |
| Schema extensions (expires_at, review_after, embedding) | ✅ Complete | Full columns implemented |
| TTL + Pruning | ✅ Complete | `PruneOldObservationsAsync`, retention config |
| Level-2 Promotion (.md files) | ✅ Complete | `mem_promote_to_md`, bidirectional links |
| Verification Tools | ✅ Complete | `mem_verify_artifact`, `mem_traceability` |
| Retry Logic | ✅ Complete | Exponential backoff in HttpStore |
| Doctor Diagnostic | ✅ Complete | `mem_doctor` for diagnostics |
| Offline-First Sync | ✅ Complete | PR #14 archived |

---

## Feature Reference by Category

### Retention & Pruning

See [`docs/12-engram-tool-reference.md`](12-engram-tool-reference.md) for full MCP tool documentation.

| Tool | Description | Implementation |
|------|-------------|---------------|
| `mem_retention_stats` | Distribution by observation age | Retention analytics |
| `mem_retention_prune` | Manual or batch pruning | TTL-based cleanup |
| `engram retention check` | CLI retention stats | Statistics command |
| `engram retention prune` | CLI pruning | Prune command |
| Memory Janitor cron | Daily automatic prune | Background scheduling |

### Level-2 .md Promotion

| Tool | Description | Implementation |
|------|-------------|---------------|
| `mem_promote_to_md` | Observation → .md with frontmatter | Automated file generation |
| `mem_sync_md_to_repo` | Batch sync with dry-run | Bi-directional sync |
| `docs/decisions/index.md` | Auto-generated ADR index | Template-driven |

### Verification & Traceability

| Tool | Description | Implementation |
|------|-------------|---------------|
| `mem_verify_artifact` | Spec/plan compliance check | Cycle tracking |
| `mem_traceability` | RF/RNF → code coverage | Source validity |
| `mem_trace_source` | Origin tracking for requirements | Lineage tree |
| `mem_lineage` | BFS lineage with cycle detection | Max 10 hops |

### MCP Tools (Full Reference)

All 15 MCP tools are documented in [`docs/12-engram-tool-reference.md`](12-engram-tool-reference.md):

1. `mem_save` — Save observations
2. `mem_get_observation` — Retrieve by ID
3. `mem_search` — Full-text search
4. `mem_context` — Session history
5. `mem_session_summary` — Session closure
6. `mem_judge` — Resolve conflicts
7. `mem_compare` — Semantic relations
8. `mem_doctor` — Diagnostics
9. `mem_obsidian_export` — Export to Obsidian
10. `mem_retention_stats` — Retention analytics
11. `mem_retention_prune` — Prune old data
12. `mem_promote_to_md` — Promote to .md
13. `mem_verify_artifact` — Verify compliance
14. `mem_traceability` — Trace requirements
15. `mem_lineage` — Track lineage

---

## What engram-dotnet Does NOT Handle

| Feature | Responsibility | Why Not engram-dotnet |
|---------|---------------|----------------------|
| AI Orchestrator | FlowForge | JSON config + routing logic, not persistence |
| Model Routing | FlowForge + MCP | Dispatcher in agent or MCP proxy |
| Workflow Runner (Makefile/bash) | FlowForge | Orchestrates phases with artifacts |
| Human Checkpoints | FlowForge | Human processes, not tools |
| spec.md / plan.md / rework_ticket.md format | FlowForge | File formats, not engram responsibility |

---

## Implementation History (Reference Only)

### Completed: Verification Tools (13 tasks)

- `mem_verify_artifact`: spec.md + diff → compliance report with cycle tracking
- `mem_traceability`: RF/RNF matrix with source validity
- SpecParser: bilingual EN/ES with canonical section detection
- CycleTracker: persistence with topic_key and escalation

### Completed: Memory Level 2 — .md Promotion (21 tasks)

- `mem_promote_to_md`: observation → .md with YAML frontmatter
- `mem_sync_md_to_repo`: batch sync with dry-run
- Bidirectional links: observation.md_path ↔ .md frontmatter
- CLI: `engram promote --id <n>` or `--sync --dry-run`

### Completed: TTL + Pruning (8 tasks)

1. Implemented `PruneOldObservationsAsync` in IStore + SqliteStore + PostgresStore
2. Added `RetentionConfig.cs` for TTL env var parsing
3. Implemented `GetRetentionStatsAsync` (age buckets, inactive projects)
4. Added HTTP endpoint: `GET /retention/stats`
5. Added CLI: `engram retention check`, `engram retention prune`
6. Added MCP tools: `mem_retention_stats`, `mem_retention_prune`
7. Implemented HttpStore proxy for new methods
8. Memory Janitor v1: daily cron prune script

### Completed: Traceability (18 tasks)

- `mem_trace_source`: RF/RNF origin with Source/Author/Rationale
- `mem_lineage`: BFS lineage tree with cycle detection (max 10 hops)
- TraceRepository: persistence with topic_key `trace/{project}/{rf-id}`
- SpecParser extended: `## Traceability` section in spec.md

---

## Related Documentation

| Document | Purpose |
|----------|---------|
| [`docs/12-engram-tool-reference.md`](12-engram-tool-reference.md) | Complete MCP tool catalog |
| [`docs/04-roadmap.md`](04-roadmap.md) | Feature roadmap and status |
| [`docs/I18N.md`](I18N.md) | Internationalization tracker |
| [`QUICKSTART.md`](../QUICKSTART.md) | Getting started guide |