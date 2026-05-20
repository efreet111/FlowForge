/# Tasks: Backend Config File

## Phase 1: Foundation (Configuration Models)

- [ ] 1.1 Create `src/Engram.Store/Config/EngramOptions.cs` and define `EngramOptions`, `StoreOptions`, `ServerOptions`, and `ArtifactStoreOptions` classes with default values.

## Phase 2: Core Implementation (Refactoring Store Services)

- [ ] 2.1 Modify `src/Engram.Store/StoreConfig.cs` to remove direct `Environment.GetEnvironmentVariable` calls. Refactor to inject `IOptions<EngramOptions>` or bind manually from `IConfiguration`.
- [ ] 2.2 Modify `src/Engram.Store/RetentionConfig.cs` to read TTL configurations from `EngramOptions.TtlRules` dictionary instead of looping over environment variables.

## Phase 3: Integration (Wiring up the CLI and Server)

- [ ] 3.1 Modify `src/Engram.Cli/Program.cs` to implement the `IConfigurationBuilder` logic. Implement the upward directory traversal to find `.engram.json` up to the `.git` root.
- [ ] 3.2 Modify `src/Engram.Cli/Program.cs` to load the global `~/.config/engram/config.json`, the local `.engram.json`, and finally `AddEnvironmentVariables("ENGRAM_")`.
- [ ] 3.3 Modify `src/Engram.Cli/Program.cs` to bind the built configuration to `EngramOptions` and register it in the dependency injection container (`AddOptions()`, `Configure()`).
- [ ] 3.4 Modify `src/Engram.Server/Program.cs` to ensure the ASP.NET Core `WebApplicationBuilder` respects the same configuration hierarchy.

## Phase 4: Testing & Verification

- [ ] 4.1 Create `tests/Engram.Store.Tests/ConfigResolutionTests.cs` to verify the priority hierarchy: Environment Variables > Local Config > Global Config.
- [ ] 4.2 Write a unit test in `ConfigResolutionTests.cs` to verify the directory traversal logic correctly finds `.engram.json` in a parent directory.
- [ ] 4.3 Update `tests/Engram.Diagnostics.Tests/CliIntegrationTests.cs` (or equivalent) to ensure the CLI boots correctly without setting any `ENGRAM_` environment variables if a local config file is mocked.
