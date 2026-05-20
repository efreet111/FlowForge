# Design: Backend Config File

## Technical Approach

We will migrate from direct `Environment.GetEnvironmentVariable()` calls to ASP.NET Core's `Microsoft.Extensions.Configuration` system. We will create a strongly typed configuration class (`EngramOptions`) that reflects the structure of `.engram.json`. The CLI and Server entry points will build an `IConfigurationRoot` that cascades globally to locally, and finally falls back to environment variables to ensure backward compatibility and "Directives as Code" compliance.

## Architecture Decisions

### Decision: Configuration Hierarchy Priority

**Choice**: Environment Variables > `.engram.json` (Local) > `~/.config/engram/config.json` (Global)
**Alternatives considered**: Local JSON overriding Environment Variables.
**Rationale**: Environment variables are standard for CI/CD and Docker environments. They must be the ultimate override to ensure deployments can easily inject secrets (like `ENGRAM_PG_CONNECTION` or `ENGRAM_JWT_SECRET`) without needing to write a file to disk.

### Decision: Tree Traversal for Local Config

**Choice**: Traverse upwards from `Environment.CurrentDirectory` until `.engram.json` or a `.git` folder is found.
**Alternatives considered**: Only look in the strict `CurrentDirectory`.
**Rationale**: Developers often run commands from subdirectories within a repository (e.g., `src/Project/`). Engram needs to find the root `.engram.json` of the project.

### Decision: Adopting the IOptions Pattern

**Choice**: Refactor `StoreConfig` and `RetentionConfig` to be bound via `IOptions<EngramOptions>`.
**Alternatives considered**: Pass `IConfiguration` directly to services.
**Rationale**: The Options pattern (`IOptions`) provides strong typing, validation support, and avoids tight coupling between domain services and the configuration provider.

## Data Flow

    [~/.config/engram/config.json] ──┐
                                     │
    [.engram.json (Project Root)] ───┼──> ConfigurationBuilder ──> IConfiguration ──> EngramOptions
                                     │                                                      │
    [Environment Variables] ─────────┘                                                      │
                                                                                            v
                                                                                       DI Container
                                                                                            │
                                                                                     [IStore, CLI]

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `src/Engram.Store/Config/EngramOptions.cs` | Create | The strongly-typed root class mapping to the JSON structure. |
| `src/Engram.Store/StoreConfig.cs` | Modify | Remove `Environment.GetEnvironmentVariable` and rely on `IOptions` injection or manual binding. |
| `src/Engram.Store/RetentionConfig.cs` | Modify | Refactor to read from `EngramOptions.TtlRules` instead of string matching env vars. |
| `src/Engram.Cli/Program.cs` | Modify | Add `ConfigurationBuilder` logic in the entry point and register configuration in DI. |
| `src/Engram.Server/Program.cs` | Modify | Ensure WebApplication builder adds `.engram.json` to its default configuration pipeline. |

## Interfaces / Contracts

```csharp
public class EngramOptions
{
    public StoreOptions Store { get; set; } = new();
    public ServerOptions Server { get; set; } = new();
    public ArtifactStoreOptions ArtifactStore { get; set; } = new();
    public Dictionary<string, string> TtlRules { get; set; } = new();
}

public class ArtifactStoreOptions
{
    // Mode can be: "engram", "openspec", "hybrid", "none"
    public string Mode { get; set; } = "engram";
}

public class StoreOptions
{
    public string Type { get; set; } = "sqlite";
    public string? ConnectionString { get; set; }
}
```

## Testing Strategy

| Layer | What to Test | Approach |
|-------|-------------|----------|
| Unit | Configuration resolution priority | Create `ConfigurationBuilder` in tests with mock env vars and JSON strings to ensure Env > Local > Global. |
| Unit | Directory traversal | Create nested temp directories and verify `.engram.json` is located from a deep subdirectory. |
| Integration | CLI Startup | Ensure `engram doctor` or similar commands boot correctly without any env vars by resolving a `.engram.json` in the test execution directory. |

## Migration / Rollout

No data migration required. Existing users relying on `ENGRAM_` environment variables will experience zero breaking changes, as environment variables remain at the highest priority in the configuration builder.

## Open Questions

- [ ] Should we support hot-reloading (`reloadOnChange: true`) for the MCP server when `.engram.json` changes, or is a server restart acceptable for this iteration?
- [ ] For directory traversal, what is the hard limit on the number of parent directories to check before giving up (to prevent infinite loops in symlink scenarios)?
