# Design: Doctor Diagnostic

## Technical Approach

The Doctor Diagnostic tool will be implemented as a new core service (`IDiagnosticService`) that orchestrates independent health checks for the engram-dotnet ecosystem. To keep dependencies clean, this service will be placed in a new `Engram.Diagnostics` project, which will be referenced by `Engram.Cli` (for the `doctor` command) and `Engram.Mcp` (for the `mem_doctor` tool).

## Architecture Decisions

### Decision: Placement of Diagnostic Logic

**Choice**: Create a new `Engram.Diagnostics` project.
**Alternatives considered**: Add logic directly to `Engram.Cli` or `Engram.Store`.
**Rationale**: Diagnostic logic needs to be shared between the CLI and the MCP server. Putting it in `Engram.Store` pollutes the data access layer with HTTP client logic (for pinging the server). A dedicated project keeps responsibilities separated and aligns with the existing architecture.

### Decision: Database Check Strategy

**Choice**: Execute a lightweight `SELECT 1` with a strict 2-second timeout.
**Alternatives considered**: Full schema validation or ORM integrity checks.
**Rationale**: The goal is a fast operational check. A quick query verifies both network connectivity and basic database availability without locking.

### Decision: MCP Stdio Contention Avoidance

**Choice**: The diagnostic will verify MCP configuration and basic bindings instead of trying to spawn a new stdio client.
**Alternatives considered**: Launch a subprocess to ping the MCP server.
**Rationale**: Launching another stdio client while the main server is holding the streams leads to deadlocks.

## Data Flow

    CLI (`engram doctor`) OR MCP (`mem_doctor`)
         │
         ▼
    DiagnosticService ───► IStore (Database `SELECT 1`)
         │
         ├───► HttpClient (Ping `Engram.Server` /health)
         │
         └───► McpHealthCheck (Verify MCP config/process)

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `src/Engram.Diagnostics/Engram.Diagnostics.csproj` | Create | New class library for diagnostics. |
| `src/Engram.Diagnostics/IDiagnosticService.cs` | Create | Interface defining `RunDiagnosticsAsync()`. |
| `src/Engram.Diagnostics/DiagnosticService.cs` | Create | Implementation of the checks. |
| `src/Engram.Diagnostics/Models/DiagnosticResult.cs` | Create | DTO for health status. |
| `src/Engram.Cli/Program.cs` | Modify | Register `engram doctor` command. |
| `src/Engram.Mcp/EngramTools.cs` | Modify | Expose `mem_doctor` tool. |

## Interfaces / Contracts

```csharp
namespace Engram.Diagnostics.Models
{
    public class DiagnosticResult
    {
        public bool IsHealthy { get; set; }
        public Dictionary<string, ComponentHealth> Components { get; set; } = new();
    }

    public class ComponentHealth
    {
        public bool IsHealthy { get; set; }
        public string Message { get; set; } = string.Empty;
        public long LatencyMs { get; set; }
    }
}

namespace Engram.Diagnostics
{
    public interface IDiagnosticService
    {
        Task<DiagnosticResult> RunDiagnosticsAsync(CancellationToken cancellationToken = default);
    }
}
```

## Testing Strategy

| Layer | What to Test | Approach |
|-------|-------------|----------|
| Unit | `DiagnosticService` | Mock `IStore` and `HttpMessageHandler` to simulate success, timeout, and failure scenarios. |
| Integration | `engram doctor` command | Run the CLI command against a real test SQLite DB and verify exit code 0. |

## Migration / Rollout

No migration required.

## Open Questions

- [ ] Does `Engram.Server` already expose a `/health` endpoint? If not, we'll need to add a standard ASP.NET Core Health Check endpoint.
