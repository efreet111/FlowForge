# Completed Changes

Index of all completed and archived changes in the engram-dotnet project.

---

## Archive

| Change | Date Completed | Status | Location |
|--------|---------------|--------|----------|
| Doctor Diagnostic | 2026-05-17 | ✅ Archived | `archive/doctor-diagnostic/` |
| Backend Config File | — | 🔄 In Progress | `backend-config-file/` |

---

## Change Summaries

### Doctor Diagnostic (2026-05-17)

**Commit**: `dc9e5d1 feat: add doctor diagnostic health check`

**Purpose**: Read-only operational health check for the engram-dotnet ecosystem.

**Deliverables**:
- `Engram.Diagnostics` class library with `IDiagnosticService`
- CLI command: `engram doctor`
- MCP tool: `mem_doctor`
- Health checks: Database (2s timeout), HTTP Server (`/health`), MCP Server (config verification)
- 26 passing tests (19 unit + 7 integration)

**Architecture**:
- New `Engram.Diagnostics` project (clean separation from `Engram.Store`)
- Parallel execution of all 3 health checks via `Task.WhenAll()`
- Non-destructive: no mutations during diagnostic execution

**Files**: See `archive/doctor-diagnostic/` for full specification, design, tasks, and verification report.

---

## Archiving Process

When a change is completed:

1. Move the change folder from `changes/<change-name>/` to `changes/archive/<change-name>/`
2. Add entry to this `COMPLETED.md` index
3. Ensure all specs are synchronized (delta specs merged if applicable)
4. Verify commit is pushed to main
5. Update this document with the summary
