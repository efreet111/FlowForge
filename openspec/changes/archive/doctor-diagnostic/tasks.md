# Tasks: Doctor Diagnostic

## Phase 1: Foundation / Infrastructure

- [ ] 1.1 Create the `Engram.Diagnostics` project (`dotnet new classlib` in `src/`) and add it to the solution.
- [ ] 1.2 Create `src/Engram.Diagnostics/Models/DiagnosticResult.cs` containing `DiagnosticResult` and `ComponentHealth` DTOs.
- [ ] 1.3 Create `src/Engram.Diagnostics/IDiagnosticService.cs` defining `Task<DiagnosticResult> RunDiagnosticsAsync(CancellationToken ct)`.

## Phase 2: Core Implementation

- [ ] 2.1 Create `src/Engram.Diagnostics/DiagnosticService.cs` implementing `IDiagnosticService`.
- [ ] 2.2 Implement Database Check in `DiagnosticService.cs` using `IStore` (execute a lightweight `SELECT` query with a 2-second timeout).
- [ ] 2.3 Implement HTTP Server Check in `DiagnosticService.cs` using `HttpClient` to ping the existing `/health` endpoint.
- [ ] 2.4 Implement MCP Server Check in `DiagnosticService.cs` verifying process/socket binding without causing stdio contention.

## Phase 3: Integration / Wiring

- [ ] 3.1 Modify `src/Engram.Cli/Engram.Cli.csproj` and `src/Engram.Mcp/Engram.Mcp.csproj` to add project references to `Engram.Diagnostics`.
- [ ] 3.2 Modify `src/Engram.Cli/Program.cs` to register the `engram doctor` command and configure `IDiagnosticService` in the dependency injection container.
- [ ] 3.3 Modify `src/Engram.Mcp/EngramTools.cs` to register and expose the `mem_doctor` tool, returning the serialized `DiagnosticResult`.

## Phase 4: Testing / Verification

- [ ] 4.1 Create `tests/Engram.Diagnostics.Tests` project and add it to the solution.
- [ ] 4.2 Write unit tests for `DiagnosticService` (mocking `IStore` for success/timeout scenarios).
- [ ] 4.3 Write unit tests for `DiagnosticService` (mocking `HttpMessageHandler` for HTTP `/health` success/failure).
- [ ] 4.4 Add an integration test verifying the CLI `engram doctor` command returns exit code 0 when all components are mocked/healthy.
