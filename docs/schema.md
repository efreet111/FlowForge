# Database Schema — N/A

> **Status**: Not applicable
> **Last reviewed**: 2026-11-06

FlowForge itself has **no database**. Persistent memory is owned by `engram-dotnet`
(SQLite locally, PostgreSQL for team sync). The relevant contracts are documented in:

- [`docs/06-engram-sync-convention.md`](06-engram-sync-convention.md) — project/scope/sync rules
- [`docs/12-engram-tool-reference.md`](12-engram-tool-reference.md) — MCP tool catalog
- [`docs/team/engram-dotnet-dependencies-report.md`](team/engram-dotnet-dependencies-report.md) — cross-project contract

The code-aware columns (`file_path`, `symbol`, `namespace`) and their migration are owned by
`engram-dotnet` (ENG-416). Do not document them here as FlowForge tables; link to the Engram
contract instead.
