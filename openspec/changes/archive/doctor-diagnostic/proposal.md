# Proposal: Doctor Diagnostic

## Intent

Ensure the engram-dotnet ecosystem (DB, MCP, Server, Tests) is healthy and properly configured. Provide a fast, reliable CLI diagnostic tool (`engram doctor`) to validate the operational status, giving developers confidence before executing the methodology.

## Scope

### In Scope
- Create `engram doctor` CLI command.
- Diagnostics for SQLite/Postgres DB connection and schema version.
- Diagnostics for MCP server connectivity and tool registration.
- Diagnostics for HTTP server health check.
- Verification of background jobs (Memory Janitor configuration).

### Out of Scope
- Automatic fixing of detected issues (read-only diagnostic).
- Execution of the full test suite (xUnit) from the doctor command.
- Changes to the core storage logic.

## Capabilities

### New Capabilities
- `doctor-diagnostic`: Read-only operational diagnostics tool to check system health.

### Modified Capabilities
- None

## Approach

Implement a new `DoctorCommand` in `Engram.Cli`. Create a `DiagnosticService` that runs independent checks (DB connection, HTTP ping, MCP stdio probe) and aggregates results into a clear console output (pass/fail/warn). 

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `src/Engram.Cli/` | Modified | Add `doctor` command and options. |
| `src/Engram.Diagnostics/` | New | Create `DiagnosticService` and individual checks. |
| `src/Engram.Mcp/` | Modified | Expose `mem_doctor` tool via MCP. |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| False negatives in DB check | Low | Use simple `SELECT 1` queries with short timeouts. |
| MCP stdio contention | Medium | Avoid locking stdio during the MCP diagnostic check. |

## Rollback Plan

Remove the `doctor` command from the CLI registry and delete the `Engram.Diagnostics` namespace/project. No data is mutated, so rollback is fully safe.

## Dependencies

- None (uses existing infrastructure).

## Success Criteria

- [ ] `engram doctor` command returns exit code 0 when all systems are green.
- [ ] Explicit error messages are shown when DB or HTTP server are unreachable.
- [ ] `mem_doctor` MCP tool returns structured JSON of the health status.
