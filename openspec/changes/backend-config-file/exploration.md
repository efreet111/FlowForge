## Exploration: Backend Config File

### Current State
Currently, `engram-dotnet` relies heavily on environment variables (e.g., `ENGRAM_TTL_TOOL_USE`) and default command-line arguments to configure its behavior. While ASP.NET Core supports `appsettings.json`, a generic approach is not ideal for a CLI/MCP tool that needs to operate on a per-project basis with specific rules (like TTLs, promotion paths, and store types).

### Affected Areas
- `src/Engram.Store/` — Configuration initialization.
- `src/Engram.Cli/` — Reading config on startup.
- `src/Engram.Mcp/` — MCP server startup context.
- `src/Engram.Server/` — HTTP API configuration.

### Approaches

1. **Project-Local Config (`.engram.yaml` or `engram.json`)**
   - **Description**: A configuration file placed in the root of the user's project (e.g., alongside `.git`).
   - **Pros**: Version-controlled ("Directives as Code"); allows different projects to have different backends (SQLite vs Postgres) and TTL rules.
   - **Cons**: Requires walking up the directory tree to find the file; YAML requires an external parser in C# (like YamlDotNet).
   - **Effort**: Medium.

2. **Global User Config (`~/.config/engram/config.json`)**
   - **Description**: A single global configuration file for the developer's machine.
   - **Pros**: Easy to implement; one place to manage settings.
   - **Cons**: Doesn't support per-project overrides (a key feature for EngramFlow).
   - **Effort**: Low.

3. **Hybrid Configuration (Global + Local Override)**
   - **Description**: Read from `~/.config/engram/config.yaml` for base settings, and override with `.engram.yaml` in the current working directory.
   - **Pros**: Maximum flexibility. Sensible defaults globally, specific rules per project.
   - **Cons**: Highest complexity to merge configurations.
   - **Effort**: High.

### Recommendation
**Approach 3 (Hybrid Configuration)** using JSON (or YAML if preferred, though JSON is native to `Microsoft.Extensions.Configuration`). A project-local `.engram.json` is critical for EngramFlow to support "Directives as Code" where a project dictates its own memory TTLs and storage backend.

### Risks
- Configuration merge conflicts (global vs local vs env vars).
- Caching configurations in long-running MCP servers if the local file changes.

### Ready for Proposal
Yes. Ready to propose the structure of the config file and the resolution hierarchy.
