# Installer Baseline Documentation

> **Purpose**: Document existing installer behavior BEFORE update mechanism changes.
> This serves as the regression reference for the Installer Protection Policy.

## Commands

### `flowforge` (no args → status)
- **Behavior**: Shows installed component versions and update availability
- **Side effects**: None (read-only)
- **Files read**: `~/.engram/config.json`
- **References**: ADR-001

### `flowforge install`
- **Behavior**: Interactive wizard that installs engram-dotnet + FlowForge skills for selected IDEs
- **Flags**: `--yes`, `--provider`, `--force-free`, `--dry-run`, `--json-only`, `--allow-symlink`, `--no-sudo`, `--server-url`
- **Side effects**:
  - Downloads engram binary to `~/.local/bin/engram` (Linux) or `%LOCALAPPDATA%\Programs\FlowForge\engram.exe` (Windows)
  - Downloads native SQLite library (`libe_sqlite3.so` / `e_sqlite3.dll`)
  - Creates MCP config for detected IDEs (Cursor, OpenCode, Antigravity)
  - Copies skills/agents/commands to IDE destinations
  - Creates sidecar at `~/.config/opencode/.flowforge-managed.json`
  - Writes `~/.engram/config.json` with component versions
- **References**: ADR-001, ADR-002, ADR-008

### `flowforge update [--check] [-y]`
- **Behavior**: Checks for and applies engram-dotnet updates
- **Flags**: `--check` (verify only), `-y/--yes` (confirm without prompt)
- **Side effects**:
  - Downloads new engram binary (with SHA-256 verification)
  - Overwrites existing binary
  - Updates version in `config.json`
- **References**: ADR-001

### `flowforge doctor`
- **Behavior**: Validates installation health (binary exists, config valid, paths correct)
- **Side effects**: None (read-only)
- **Files read**: `~/.engram/config.json`, binary paths
- **References**: ADR-001

### `flowforge uninstall`
- **Behavior**: Removes installed components
- **Side effects**: Deletes binaries, config, backup directories
- **Files affected**: `~/.local/bin/engram`, `~/.local/bin/flowforge`, `~/.engram/`

### `flowforge config set <key> <value>`
- **Behavior**: Modifies installer configuration
- **Side effects**: Updates `~/.engram/config.json`
- **References**: ADR-010

### `flowforge init [path]`
- **Behavior**: Initializes FlowForge in a project directory
- **Side effects**: Creates `.agents/`, `.opencode/`, `.cursor/`, etc. in target project
- **References**: ADR-008

## Key Infrastructure Components

| Component | File | Responsibility |
|-----------|------|---------------|
| `ConfigStore` | `Infrastructure/ConfigStore.cs` | Atomic read-modify-write of `config.json` |
| `PathHelper` | `Infrastructure/PathHelper.cs` | Cross-platform path resolution |
| `InstallerLogger` | `Infrastructure/InstallerLogger.cs` | Structured logging to `install.log` |
| `GitHubReleasesClient` | `Infrastructure/GitHubReleasesClient.cs` | Binary download + SHA-256 verification |
| `ManifestClient` | `Infrastructure/ManifestClient.cs` | Remote manifest fetch + compatibility checks |
| `FlowForgeRepoLocator` | `Infrastructure/FlowForgeRepoLocator.cs` | Git cache management for skills |
| `EngramModule` | `Modules/EngramModule.cs` | Engram binary install/update + MCP config |
| `FlowForgeModule` | `Modules/FlowForgeModule.cs` | Skills/agents installation per IDE |

## Data Flow

```
config.json (source of truth)
├── version, channel, auto_update
├── sync (mode, remote_url, user, data_dir)
├── components
│   ├── engram_dotnet (installed, version, binary, registered_at)
│   └── flowforge (installed, version, ides[])
└── flowdoc (enabled)
```

## ADR References

- **ADR-001**: Installer architecture (single binary, cross-platform)
- **ADR-002**: MCP config strategy (per-IDE surgical merge)
- **ADR-008**: IDE installer path matrix (sidecar locations)
- **ADR-010**: Sync config persistence (config.json as source of truth)
