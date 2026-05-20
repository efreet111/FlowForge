# Proposal: Backend Config File

## Intent

Replace the rigid environment-variable-only configuration of `engram-dotnet` with a structured configuration system. This allows projects to dictate their own memory rules (like custom TTLs and backend choices) via a project-local file, fulfilling the "Directives as Code" principle.

## Scope

### In Scope
- Support for a project-local `.engram.json` configuration file.
- Support for a global developer fallback `~/.config/engram/config.json`.
- Migration of existing environment variable usage (e.g., `ENGRAM_TTL_TOOL_USE`) to the new config schema.
- Support for configuring the backend store type (SQLite, PostgreSQL, HTTP) and connection strings.

### Out of Scope
- Hot-reloading of configuration at runtime for the MCP server.
- Web UI for managing configurations.
- Support for YAML configurations in this initial iteration (sticking to native JSON).

## Capabilities

### New Capabilities
- `configuration-management`: Allows configuring Engram settings via structured JSON files (local and global) instead of just environment variables.

### Modified Capabilities
- None

## Approach

Implement a "Hybrid Configuration" model using `Microsoft.Extensions.Configuration`. The system will load settings in this priority order (highest to lowest):
1. Environment Variables (for legacy support and quick overrides).
2. Project-Local `.engram.json` (found in the current working directory or Git root).
3. Global User `~/.config/engram/config.json`.
4. Hardcoded defaults.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `src/Engram.Store/` | Modified | Update initialization to consume strongly-typed options instead of env vars. |
| `src/Engram.Cli/` | Modified | Update startup sequence to build the configuration root. |
| `src/Engram.Mcp/` | Modified | Update startup sequence to inject the configuration into the server. |
| `src/Engram.Server/` | Modified | Update API to read configurations from the new system. |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Path resolution errors for local config | Medium | Traverse up the directory tree until `.git` is found, or default to current directory. |
| Breaking existing environments | Low | Maintain environment variables as the highest priority override during migration. |

## Rollback Plan

Revert the startup builder configuration to rely strictly on `AddEnvironmentVariables()` and remove the `.engram.json` parsing logic. Since this doesn't alter database schemas, rollback is safe.

## Dependencies

- `Microsoft.Extensions.Configuration.Json`
- `Microsoft.Extensions.Options`

## Success Criteria

- [ ] A project can define `.engram.json` with a specific SQLite path and the CLI respects it.
- [ ] A global `~/.config/engram/config.json` can define default TTLs.
- [ ] `engram-dotnet` starts up correctly without any environment variables being set.
