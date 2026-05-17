# Doctor Diagnostic Specification

## Purpose

The Doctor Diagnostic tool provides a read-only operational health check for the engram-dotnet ecosystem, verifying the database, HTTP server, MCP server connectivity, and background jobs.

## Requirements

### Requirement: CLI Command Execution

The system MUST provide an `engram doctor` command via the CLI.

#### Scenario: All systems healthy
- GIVEN the database, HTTP server, and MCP server are running and accessible
- WHEN the user executes `engram doctor`
- THEN the system MUST output a success message for each component
- AND the system MUST return exit code 0

#### Scenario: One or more systems fail
- GIVEN the database is unreachable
- WHEN the user executes `engram doctor`
- THEN the system MUST output a failure message indicating the database is unreachable
- AND the system MUST return a non-zero exit code

### Requirement: Database Connection Check

The system MUST verify connectivity to the active database (SQLite or PostgreSQL) using a lightweight query.

#### Scenario: Successful connection
- GIVEN a valid connection string to the active store
- WHEN the diagnostic service probes the database
- THEN it MUST execute a `SELECT 1` (or equivalent) query
- AND it MUST report the check as passed if the query returns within the timeout

#### Scenario: Connection timeout
- GIVEN the database is under heavy load or unreachable
- WHEN the diagnostic service probes the database
- THEN the query MUST timeout after a short threshold (e.g., 2000ms)
- AND it MUST report the check as failed

### Requirement: MCP Server Diagnostic

The system MUST expose a `mem_doctor` tool via MCP that returns the health status in structured JSON.

#### Scenario: MCP client requests diagnostic
- GIVEN the MCP server is running
- WHEN a client invokes the `mem_doctor` tool
- THEN the system MUST run the diagnostic checks
- AND it MUST return a JSON payload with the status of each component (e.g., `{"database": "healthy", "http_server": "unreachable"}`)

### Requirement: Non-Destructive Execution

The system MUST NOT mutate any data or state during the diagnostic checks.

#### Scenario: Diagnostic execution
- GIVEN an existing set of observations in the database
- WHEN the user executes `engram doctor`
- THEN no existing data MUST be altered or deleted
- AND no new operational data (other than ephemeral logs) MUST be created
