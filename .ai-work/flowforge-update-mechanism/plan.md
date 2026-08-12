# Plan: Update Mechanism por Componente (`flowforge update --component`)

> **Spec**: `.ai-work/flowforge-update-mechanism/spec.md`
> **Context map**: `.ai-work/flowforge-update-mechanism/context-map.md`
> **Feature slug**: `flowforge-update-mechanism`

---

## 1. Impact and Dependencies

### Existing components that change

| Component | Change type | Reason |
|-----------|------------|--------|
| `UpdateCommand.cs` | MODIFY | Add `--component`, `--force`, `--tag` flags; delegate to `UpdateOrchestrator` |
| `EngramModule.UpdateAsync` | MODIFY | Add backup-before-swap + health-check + rollback (currently bare download) |
| `FlowForgeModule.Install` | MODIFY | Extract reusable `UpdateSkillsAsync` method with SHA-256 diff detection |
| `InstallerConfig.cs` / `ComponentsConfig` | MODIFY | Add `FlowDoc` and `Installer` component entries; add `LastCacheRefresh` timestamp |
| `PathHelper.cs` | MODIFY | Add sidecar paths for Cursor, Antigravity, VS Code, Kilo |
| `StatusCommand.cs` | MODIFY | Show per-component versions (add FlowDoc + Installer columns) |
| `ManifestClient.cs` | MODIFY | Add cross-component compatibility check (not just engram) |
| `FlowForgeRepoLocator.cs` | MODIFY | Add `RefreshCacheAsync` (git pull) method |
| `EngramModule.WriteMcpJson` (Cursor/Antigravity) | MODIFY | Replace overwrite with surgical merge (fixes 2 data-loss bugs from S6) |

### New components to create

| Component | Responsibility |
|-----------|---------------|
| `UpdateOrchestrator` | Top-level orchestrator: component selection, topological ordering, error propagation |
| `BackupManager` | Create/restore/prune backups in `~/.flowforge-backups/` (max 5 per component) |
| `HealthCheckRunner` | Post-update verification (binary version, MCP parse, doctor subset) |
| `McpConfigMerger` | Surgical JSON merge for Cursor `mcp.json` + Antigravity `mcp_config.json` (pattern from `MergeOpenCodeMcp`) |
| `UserModifiedAgentDetector` | SHA-256 comparison between installed and source agents; returns `ModifiedFileReport` |
| `EngramProcessChecker` | Detect running `engram` processes before binary swap |
| `ManagedPathsSidecar` (generalized) | Extend from OpenCode-only to all IDE destinations |
| `ComponentRegistry` | Read/write per-component version tracking in `config.json` |

### Dependencies (no new NuGet packages)

All patterns already exist in the codebase. Zero new dependencies — AOT-safe.

### Reuse matrix (~80% code reuse)

| Existing pattern | Reused in |
|-----------------|-----------|
| `EngramModule.UpdateAsync` (download + swap) | `UpdateOrchestrator.UpdateEngramAsync` (wraps with backup + health-check) |
| `OpenCodeConfigGenerator.MergeManagedPaths` (surgical JSON merge) | `McpConfigMerger` (same `JsonNode` pattern for Cursor/Antigravity) |
| `ManagedPathsSidecar` (OpenCode) | Generalized sidecar for Cursor, Antigravity, VS Code, Kilo |
| `ConfigStore.Update` (atomic read-modify-write) | `ComponentRegistry` (same pattern for per-component versions) |
| `ManifestClient.CheckEngramCompatibility` | Extended to `CheckCrossComponentCompatibility` |
| `FlowForgeRepoLocator.EnsureAvailable` | Add `RefreshCacheAsync` (git pull) |
| `FlowForgeModule.BackupDirectory` | `BackupManager` (formalize with retention cap) |
| `AntigravityPackValidator` | `HealthCheckRunner` (composes validator + binary check + MCP parse) |
| `GitHubReleasesClient.DownloadAndVerifyAsync` | Reused as-is (SHA-256 mandatory already implemented) |
| `AtomicWriter` | Reused for all config writes |

---

## 2. File Changes (Proposed Changes)

### New files

- [NEW] `src/FlowForge.Installer/Update/UpdateOrchestrator.cs` — Top-level orchestrator; composes all modules; handles `--component` routing, topological ordering, error propagation
- [NEW] `src/FlowForge.Installer/Update/BackupManager.cs` — Create/restore/prune backups; retention cap at 5 per component; path: `~/.flowforge-backups/{component}-{timestamp}/`
- [NEW] `src/FlowForge.Installer/Update/HealthCheckRunner.cs` — Post-update verification: binary `--version`, MCP config parse, `doctor --strict` subset
- [NEW] `src/FlowForge.Installer/Update/McpConfigMerger.cs` — Surgical JSON merge for Cursor `mcp.json` and Antigravity `mcp_config.json` (preserves existing servers)
- [NEW] `src/FlowForge.Installer/Update/UserModifiedAgentDetector.cs` — SHA-256 comparison between installed and repo source; returns `ModifiedFileReport`
- [NEW] `src/FlowForge.Installer/Update/EngramProcessChecker.cs` — Detect running `engram` processes (cross-platform); returns PID list
- [NEW] `src/FlowForge.Installer/Update/ComponentRegistry.cs` — Per-component version tracking; reads/writes `config.json` `components` section
- [NEW] `src/FlowForge.Installer/Update/CacheRefresher.cs` — `git pull` on cache; fallback to fresh clone on failure
- [NEW] `src/FlowForge.Installer/Update/ManagedPathsSidecarFactory.cs` — Creates sidecar instances for each IDE destination (generalizes OpenCode-only pattern)
- [NEW] `src/FlowForge.Installer/Update/UpdateResult.cs` — Result DTO: component, old version, new version, status (success/skipped/failed/rolled-back), SHA-256 pre/post
- [NEW] `tests/FlowForge.Installer.Tests/Update/BackupManagerTests.cs` — Unit tests for backup create/restore/prune
- [NEW] `tests/FlowForge.Installer.Tests/Update/McpConfigMergerTests.cs` — Unit tests for surgical merge (preserves existing servers)
- [NEW] `tests/FlowForge.Installer.Tests/Update/UserModifiedAgentDetectorTests.cs` — Unit tests for SHA-256 comparison
- [NEW] `tests/FlowForge.Installer.Tests/Update/HealthCheckRunnerTests.cs` — Unit tests for health-check logic
- [NEW] `tests/FlowForge.Installer.Tests/Update/UpdateOrchestratorTests.cs` — Integration tests for orchestration flow
- [NEW] `tests/FlowForge.Installer.Tests/Update/EngramProcessCheckerTests.cs` — Unit tests for process detection
- [NEW] `tests/FlowForge.Installer.Tests/Update/ComponentRegistryTests.cs` — Unit tests for version tracking
- [NEW] `tests/FlowForge.Installer.Tests/Update/CacheRefresherTests.cs` — Unit tests for git pull/clone logic
- [NEW] `tests/FlowForge.Installer.Tests/Regression/InstallerBaselineTests.cs` — Regression tests for existing commands (Installer Protection Policy)

### Modified files

- [MODIFY] `src/FlowForge.Installer/Commands/UpdateCommand.cs` — Add `--component`, `--force`, `--tag` flags; replace inline logic with `UpdateOrchestrator` delegation
- [MODIFY] `src/FlowForge.Installer/Modules/EngramModule.cs` — Extract `DownloadToTempPathAsync` (download without overwrite); add `HealthCheckBinaryAsync` method; keep `InstallAsync` unchanged
- [MODIFY] `src/FlowForge.Installer/Modules/FlowForgeModule.cs` — Extract `UpdateSkillsForIdeAsync` (reusable from update path); add SHA-256 diff detection; keep `Install` unchanged
- [MODIFY] `src/FlowForge.Installer/Models/InstallerConfig.cs` — Add `FlowDocEntry` and `InstallerEntry` to `ComponentsConfig`; add `LastCacheRefresh` field
- [MODIFY] `src/FlowForge.Installer/Infrastructure/PathHelper.cs` — Add sidecar paths: `CursorSidecarPath`, `AntigravitySidecarPath`, `VsCodeSidecarPath`, `KiloSidecarPath`
- [MODIFY] `src/FlowForge.Installer/Infrastructure/FlowForgeRepoLocator.cs` — Add `RefreshCacheAsync` method (git pull + fallback clone)
- [MODIFY] `src/FlowForge.Installer/Infrastructure/ManifestClient.cs` — Add `CheckCrossComponentCompatibility` method
- [MODIFY] `src/FlowForge.Installer/Commands/StatusCommand.cs` — Show per-component versions including FlowDoc and Installer
- [MODIFY] `src/FlowForge.Installer/FlowForge.Installer.csproj` — No changes expected (all in same project)

---

## 3. Contracts and Schemas

### 3.1 UpdateOrchestrator

```csharp
namespace FlowForge.Installer.Update;

public enum UpdateComponent
{
    Engram,
    FlowForgeSkills,
    FlowDoc,
    Installer, // OUT v1 (OQ-1) — reserved enum value
    All
}

public enum UpdateStatus
{
    Success,
    SkippedAlreadyLatest,
    SkippedUserChoice,
    Failed,
    RolledBack
}

public sealed record UpdateResult(
    UpdateComponent Component,
    string OldVersion,
    string NewVersion,
    UpdateStatus Status,
    string? ErrorMessage = null,
    string? Sha256Pre = null,
    string? Sha256Post = null
);

public sealed record UpdateOptions(
    UpdateComponent Component,
    bool Yes,
    bool Force,
    string? Tag,           // git tag for skills pinning (FR-004/SEC-003)
    string? SpecificVersion // explicit version override
);

public sealed class UpdateOrchestrator
{
    public UpdateOrchestrator(InstallerContext ctx) { ... }

    /// <summary>
    /// Main entry point. Routes to component-specific update methods.
    /// Topological order for All: Engram → MCP configs → FlowForgeSkills → FlowDoc.
    /// Stops on first failure (no partial updates).
    /// </summary>
    public async Task<IReadOnlyList<UpdateResult>> RunAsync(UpdateOptions options, CancellationToken ct = default);
}
```

### 3.2 BackupManager

```csharp
public sealed class BackupManager
{
    const int MaxBackupsPerComponent = 5;

    public BackupManager(InstallerLogger log) { ... }

    /// <summary>Creates backup of file/directory. Returns backup path.</summary>
    public string CreateBackup(string sourcePath, string componentName);

    /// <summary>Restores the most recent backup for a component.</summary>
    public bool TryRestoreLatest(string componentName, string targetPath);

    /// <summary>Prunes old backups, keeping only the N most recent.</summary>
    public void PruneOldBackups(string componentName);

    /// <summary>Lists all backups for a component, ordered by timestamp desc.</summary>
    public IReadOnlyList<string> ListBackups(string componentName);
}
```

### 3.3 McpConfigMerger

```csharp
/// <summary>
/// Surgical JSON merge for MCP config files.
/// Pattern: read existing → parse as JsonNode → replace only "engram" entry → write back.
/// Preserves ALL other servers byte-for-byte.
/// </summary>
public sealed class McpConfigMerger
{
    /// <summary>
    /// Merge engram MCP entry into Cursor mcp.json.
    /// Format: {"mcpServers": {"engram": {"type":"stdio","command":"...","args":["mcp"],"env":{...}}}}
    /// </summary>
    public McpMergeResult MergeCursorMcp(string mcpJsonPath, string engramBinaryPath,
        string user, string dataDir, bool syncEnabled, string? syncUrl);

    /// <summary>
    /// Merge engram MCP entry into Antigravity mcp_config.json.
    /// Same mcpServers format as Cursor.
    /// </summary>
    public McpMergeResult MergeAntigravityMcp(string mcpConfigPath, string engramBinaryPath,
        string user, string dataDir, bool syncEnabled, string? syncUrl);
}

public sealed record McpMergeResult(
    bool Success,
    int ServersPreserved,  // count of non-engram servers preserved
    bool EngramAdded,       // true if engram entry was added (vs updated)
    string? Error
);
```

### 3.4 UserModifiedAgentDetector

```csharp
public sealed record ModifiedFileReport(
    string FilePath,
    string InstalledSha256,
    string SourceSha256,
    bool IsModified
);

public sealed class UserModifiedAgentDetector
{
    /// <summary>
    /// Compares SHA-256 of installed agent files against source repo files.
    /// Returns list of reports — caller decides skip/backup/overwrite.
    /// </summary>
    public IReadOnlyList<ModifiedFileReport> DetectModifications(
        string installedDir, string sourceDir, string filePattern);
}
```

### 3.5 EngramProcessChecker

```csharp
public sealed record EngramProcessInfo(int Pid, string ProcessName);

public sealed class EngramProcessChecker
{
    /// <summary>
    /// Detects running engram processes (MCP servers).
    /// Cross-platform: uses `pgrep` on Linux/macOS, `GetProcessesByName` on Windows.
    /// </summary>
    public IReadOnlyList<EngramProcessInfo> DetectRunningProcesses();
}
```

### 3.6 HealthCheckRunner

```csharp
public sealed record HealthCheckResult(
    string CheckName,
    bool Passed,
    string? Detail
);

public sealed class HealthCheckRunner
{
    /// <summary>Binary health-check: run `engram --version`, verify exit code + version string.</summary>
    public Task<HealthCheckResult> CheckBinaryAsync(string binaryPath, string expectedVersion);

    /// <summary>MCP config parse check: validate JSON is parseable and contains engram entry.</summary>
    public HealthCheckResult CheckMcpConfig(string mcpConfigPath);

    /// <summary>Run all post-update checks for a component.</summary>
    public async Task<IReadOnlyList<HealthCheckResult>> RunAllAsync(UpdateComponent component, string version);
}
```

### 3.7 ComponentRegistry

```csharp
public sealed class ComponentRegistry
{
    public ComponentRegistry(ConfigStore store) { ... }

    /// <summary>Get installed version for a component (null if not installed).</summary>
    public string? GetVersion(UpdateComponent component);

    /// <summary>Set version for a component (atomic write via ConfigStore).</summary>
    public void SetVersion(UpdateComponent component, string version);

    /// <summary>Check if a component is at the target version (idempotency check).</summary>
    public bool IsAtVersion(UpdateComponent component, string targetVersion);

    /// <summary>Get all component versions for status display.</summary>
    public Dictionary<string, string?> GetAllVersions();
}
```

### 3.8 CacheRefresher

```csharp
public sealed class CacheRefresher
{
    /// <summary>
    /// Refresh the FlowForge cache via `git pull`.
    /// If pull fails (corrupt repo), falls back to fresh clone.
    /// Returns the cache path on success, null on failure.
    /// </summary>
    public string? RefreshCache(InstallerLogger log);
}
```

### 3.9 InstallerConfig model changes

```csharp
// ADD to ComponentsConfig:
[JsonPropertyName("flowdoc")]
public FlowDocEntry? FlowDoc { get; set; }

[JsonPropertyName("installer")]
public InstallerEntry? Installer { get; set; }

// NEW types:
public sealed class FlowDocEntry
{
    [JsonPropertyName("installed")]
    public bool Installed { get; set; }

    [JsonPropertyName("version")]
    public string Version { get; set; } = "";

    [JsonPropertyName("project_path")]
    public string ProjectPath { get; set; } = "";
}

public sealed class InstallerEntry
{
    [JsonPropertyName("installed")]
    public bool Installed { get; set; }

    [JsonPropertyName("version")]
    public string Version { get; set; } = "";

    [JsonPropertyName("binary")]
    public string Binary { get; set; } = "";
}

// ADD to InstallerConfig:
[JsonPropertyName("last_cache_refresh")]
public string? LastCacheRefresh { get; set; }
```

### 3.10 PathHelper additions

```csharp
// Sidecar paths per IDE (generalize ManagedPathsSidecar pattern):
public static string CursorSidecarPath =>
    Path.Combine(HomeDir, ".cursor", ".flowforge-managed.json");

public static string AntigravitySidecarPath =>
    Path.Combine(HomeDir, ".gemini", "config", ".flowforge-managed.json");

public static string VsCodeSidecarPath =>
    Path.Combine(HomeDir, ".copilot", ".flowforge-managed.json");

public static string KiloSidecarPath =>
    Path.Combine(HomeDir, ".config", "kilo", ".flowforge-managed.json");
```

---

## 4. Security Architecture

### Secure-by-design patterns applied

| Principle | Implementation |
|-----------|---------------|
| **Least Privilege** | Updater runs as current user; no sudo required; `chmod 755` enforced on binaries (RNF-SEC-008) |
| **Defense in Depth** | SHA-256 mandatory on all downloads (RNF-SEC-002) + health-check post-swap + backup rollback |
| **Fail Securely** | Download timeout = hard stop (no partial data); health-check failure = no overwrite; errors never leak tokens |
| **Never Trust Input** | MCP merge validates JSON structure before write; component names validated against enum allowlist |
| **Secure by Default** | `--yes` defaults to backup+overwrite (non-destructive); `--force` required for overwrite without backup |

### OWASP ASVS items covered

- **[SEC] V5 Input Validation**: Component name validated against `UpdateComponent` enum; version strings parsed with `Version.TryParse`; file paths validated before write
- **[SEC] V2 Authentication**: No auth required (local tool); GitHub API uses public endpoints
- **[SEC] V4 Access Control**: Updater only writes to user-owned directories (`~/.engram/`, `~/.flowforge-backups/`, IDE config dirs)
- **[SEC] V1 Cryptography**: SHA-256 mandatory for all binary downloads; `engram.db` and `local_memory/` are NEVER read or written by updater code (RNF-SEC-005)
- **[SEC] RNF-SEC-AOT**: No reflection; all JSON via source-gen (`InstallerJsonContext`, `McpJsonContext`); no hardcoded tokens

### Security-critical tasks (marked `[SEC]` in checklist)

- SHA-256 verification mandatory on all downloads (already implemented in `GitHubReleasesClient`)
- Binary permissions check post-copy (`chmod 755`, reject setuid/setgid)
- MCP merge must NOT log environment variable values (debug logs sanitize env vars)
- `engram.db`, `-wal`, `-shm`, `local_memory/` must be read-only for updater (code audit)
- PII scanner runs on all generated config files before write

---

## 5. Implementation Checklist

### Phase 0: Installer Protection Policy (baseline + regression tests)

> **Goal**: Document and test the existing installer behavior BEFORE any changes.

- [x] **0.1** [REGRESSION] Create `tests/FlowForge.Installer.Tests/Regression/InstallerBaselineTests.cs` with tests for existing commands:
  - `flowforge status` runs without error
  - `flowforge doctor` runs without error
  - `ConfigStore.Load` + `Save` round-trip preserves all fields
  - `PathHelper` returns valid paths on current platform
  - `GitHubReleasesClient.GetLatestVersionAsync` handles timeout correctly
  - `ManifestClient.CheckEngramCompatibility` validates version constraints
  - Size: **M**
- [x] **0.2** Create `.ai-work/flowforge-update-mechanism/installer-baseline.md` documenting:
  - All commands: `install`, `update`, `uninstall`, `config`, `status`, `doctor`, `init`
  - Flags and expected behavior per command
  - Side effects (files created, configs modified)
  - References: ADR-001, ADR-002, ADR-008, ADR-010
  - Size: **S**
- [x] **0.3** [REGRESSION] Run existing test suite (`dotnet test`) and verify all green before proceeding
  - Size: **S**

### Phase 1: Foundation (models, paths, registry)

- [x] **1.1** [MODEL] Extend `InstallerConfig.cs`:
  - Add `FlowDocEntry` and `InstallerEntry` classes
  - Add `FlowDoc` and `Installer` properties to `ComponentsConfig`
  - Add `LastCacheRefresh` to `InstallerConfig`
  - Add `[JsonSerializable]` attributes for new types in `InstallerJsonContext`
  - Size: **S**
- [x] **1.2** [PATH] Extend `PathHelper.cs`:
  - Add `CursorSidecarPath`, `AntigravitySidecarPath`, `VsCodeSidecarPath`, `KiloSidecarPath`
  - Size: **S**
- [x] **1.3** [NEW] Create `src/FlowForge.Installer/Update/UpdateResult.cs`:
  - Define `UpdateComponent` enum, `UpdateStatus` enum, `UpdateResult` record, `UpdateOptions` record
  - Size: **S**
- [x] **1.4** [NEW] Create `src/FlowForge.Installer/Update/ComponentRegistry.cs`:
  - Implement `GetVersion`, `SetVersion`, `IsAtVersion`, `GetAllVersions`
  - Uses `ConfigStore` for atomic writes
  - Size: **M**
- [x] **1.5** [TEST] Create `tests/FlowForge.Installer.Tests/Update/ComponentRegistryTests.cs`:
  - Test version get/set round-trip
  - Test idempotency check (`IsAtVersion`)
  - Test all-versions retrieval
  - Size: **S**

### Phase 2: Backup + Rollback

- [x] **2.1** [NEW] Create `src/FlowForge.Installer/Update/BackupManager.cs`:
  - `CreateBackup(sourcePath, componentName)` → copies to `~/.flowforge-backups/{component}-{timestamp}/`
  - `TryRestoreLatest(componentName, targetPath)` → restores most recent backup
  - `PruneOldBackups(componentName)` → keeps max 5, prunes oldest
  - `ListBackups(componentName)` → returns sorted list
  - Size: **M**
- [x] **2.2** [TEST] Create `tests/FlowForge.Installer.Tests/Update/BackupManagerTests.cs`:
  - Test backup creation (file + directory)
  - Test restore from backup
  - Test retention cap (create 6 backups → verify only 5 remain)
  - Test restore when no backup exists (returns false)
  - Size: **M**

### Phase 3: Health-Check

- [x] **3.1** [NEW] Create `src/FlowForge.Installer/Update/HealthCheckRunner.cs`:
  - `CheckBinaryAsync(binaryPath, expectedVersion)` → runs `{binary} --version`, validates exit code + version string
  - `CheckMcpConfig(mcpConfigPath)` → validates JSON parseable + contains engram entry
  - `RunAllAsync(component, version)` → runs all relevant checks
  - Size: **M**
- [x] **3.2** [TEST] Create `tests/FlowForge.Installer.Tests/Update/HealthCheckRunnerTests.cs`:
  - Test binary health-check with valid/invalid binary
  - Test MCP config parse check with valid/invalid JSON
  - Test health-check timeout handling
  - Size: **M**

### Phase 4: MCP Config Merge (fix data-loss bugs)

- [x] **4.1** [SEC] [NEW] Create `src/FlowForge.Installer/Update/McpConfigMerger.cs`:
  - `MergeCursorMcp(mcpJsonPath, ...)` → surgical merge using `JsonNode` (same pattern as `MergeOpenCodeMcp`)
  - `MergeAntigravityMcp(mcpConfigPath, ...)` → same pattern for Antigravity
  - Preserves ALL existing servers byte-for-byte
  - Debug log of diff WITHOUT env var values
  - Size: **L**
- [x] **4.2** [TEST] Create `tests/FlowForge.Installer.Tests/Update/McpConfigMergerTests.cs`:
  - Test merge into empty file (creates engram entry)
  - Test merge with existing servers (preserves them)
  - Test merge with existing engram entry (updates in place)
  - Test invalid JSON handling (graceful failure)
  - Test that non-engram servers are byte-identical after merge
  - Size: **L**

### Phase 5: User-Modified Agent Detection

- [x] **5.1** [NEW] Create `src/FlowForge.Installer/Update/UserModifiedAgentDetector.cs`:
  - `DetectModifications(installedDir, sourceDir, filePattern)` → SHA-256 comparison
  - Returns `ModifiedFileReport` list
  - Size: **M**
- [x] **5.2** [TEST] Create `tests/FlowForge.Installer.Tests/Update/UserModifiedAgentDetectorTests.cs`:
  - Test unmodified file (same hash → `IsModified = false`)
  - Test modified file (different hash → `IsModified = true`)
  - Test missing installed file (new file → `IsModified = false`)
  - Test missing source file (deleted upstream → report)
  - Size: **M**

### Phase 6: Engram Process Check

- [x] **6.1** [NEW] Create `src/FlowForge.Installer/Update/EngramProcessChecker.cs`:
  - `DetectRunningProcesses()` → cross-platform process detection
  - Linux/macOS: `pgrep -f engram` or `/proc` scan
  - Windows: `Process.GetProcessesByName("engram")`
  - Size: **M**
- [x] **6.2** [TEST] Create `tests/FlowForge.Installer.Tests/Update/EngramProcessCheckerTests.cs`:
  - Test detection when no engram running (empty list)
  - Test detection returns PID + process name
  - Size: **S**

### Phase 7: Cache Refresh

- [x] **7.1** [NEW] Create `src/FlowForge.Installer/Update/CacheRefresher.cs`:
  - `RefreshCache(log)` → `git pull` on `~/.flowforge/cache/FlowForge`
  - On pull failure: delete cache + `git clone --depth 1` fresh
  - On clone failure: return null (abort)
  - Size: **M**
- [x] **7.2** [MODIFY] `src/FlowForge.Installer/Infrastructure/FlowForgeRepoLocator.cs`:
  - Extract `RunGit` to be reusable (or make `CacheRefresher` use its own process runner)
  - Size: **S**
- [x] **7.3** [TEST] Create `tests/FlowForge.Installer.Tests/Update/CacheRefresherTests.cs`:
  - Test successful pull (mock git)
  - Test fallback to fresh clone on pull failure
  - Test abort on clone failure
  - Size: **M**

### Phase 8: Sidecar Generalization

- [x] **8.1** [NEW] Create `src/FlowForge.Installer/Update/ManagedPathsSidecarFactory.cs`:
  - Factory that creates `ManagedPathsSidecar`-like instances for each IDE
  - Cursor: `~/.cursor/.flowforge-managed.json`
  - Antigravity: `~/.gemini/config/.flowforge-managed.json`
  - VS Code: `~/.copilot/.flowforge-managed.json`
  - Kilo: `~/.config/kilo/.flowforge-managed.json`
  - Size: **M**
- [x] **8.2** [MODIFY] `src/FlowForge.Installer/Modules/OpenCode/ManagedPathsSidecar.cs`:
  - Make constructor accept a custom path (currently hardcoded to `PathHelper.OpenCodeSidecarPath`)
  - Keep backward compatibility (default constructor uses OpenCode path)
  - Size: **S**

### Phase 9: UpdateOrchestrator (main integration)

- [x] **9.1** [NEW] Create `src/FlowForge.Installer/Update/UpdateOrchestrator.cs`:
  - Constructor takes `InstallerContext`
  - `RunAsync(UpdateOptions, CancellationToken)` → main entry point
  - Routes by component: `Engram` → `UpdateEngramAsync`, `FlowForgeSkills` → `UpdateSkillsAsync`, etc.
  - `All` → topological order: Engram → MCP merge → Skills → FlowDoc
  - Stops on first failure; returns `IReadOnlyList<UpdateResult>`
  - Size: **XL**
- [x] **9.2** [INTERNAL] Implement `UpdateEngramAsync` in `UpdateOrchestrator`:
  1. Check idempotency (`ComponentRegistry.IsAtVersion`)
  2. Check engram processes (`EngramProcessChecker`)
  3. Fetch latest version (`GitHubReleasesClient.GetLatestVersionAsync`)
  4. Validate compatibility (`ManifestClient.CheckCrossComponentCompatibility`)
  5. Create backup (`BackupManager.CreateBackup`)
  6. Download to temp path (`GitHubReleasesClient` → new `DownloadEngramToTempAsync`)
  7. Health-check temp binary (`HealthCheckRunner.CheckBinaryAsync`)
  8. If health-check passes: atomic move temp → target
  9. If health-check fails: delete temp, keep original (no rollback needed)
  10. Update version tracking (`ComponentRegistry.SetVersion`)
  11. Run MCP merge (`McpConfigMerger`)
  12. Prune old backups (`BackupManager.PruneOldBackups`)
  13. Log to `install.log` (NFR-LOG-001)
  - Size: (included in 9.1)
- [x] **9.3** [INTERNAL] Implement `UpdateSkillsAsync` in `UpdateOrchestrator`:
  1. Refresh cache (`CacheRefresher.RefreshCache`)
  2. Detect installed IDEs
  3. For each IDE: detect modified agents (`UserModifiedAgentDetector`)
  4. If modified: prompt user (Skip/Backup+Overwrite/Overwrite) or auto-backup if `--yes`
  5. Copy skills/agents from cache to IDE destinations
  6. Update/create sidecar per IDE (`ManagedPathsSidecarFactory`)
  7. Update version tracking
  - Size: (included in 9.1)
- [x] **9.4** [TEST] Create `tests/FlowForge.Installer.Tests/Update/UpdateOrchestratorTests.cs`:
  - Test single-component update (engram)
  - Test all-components update (topological order)
  - Test idempotency (already at latest → skipped)
  - Test failure propagation (engram fails → skills not attempted)
  - Test rollback on health-check failure
  - Size: **XL**

### Phase 10: Command integration

- [x] **10.1** [MODIFY] `src/FlowForge.Installer/Commands/UpdateCommand.cs`:
  - Add `--component` flag (string, parsed to `UpdateComponent` enum)
  - Add `--force` flag (bool)
  - Add `--tag` flag (string, for skills pinning)
  - Replace inline update logic with `UpdateOrchestrator.RunAsync`
  - Keep `--check` flag working (delegate to `ComponentRegistry.GetAllVersions` + `GitHubReleasesClient.GetLatestVersionAsync`)
  - Size: **L**
- [x] **10.2** [MODIFY] `src/FlowForge.Installer/Modules/EngramModule.cs`:
  - Add `DownloadEngramToTempAsync(version, tempPath)` method (download without overwriting target)
  - Add `HealthCheckBinaryAsync(binaryPath, expectedVersion)` method
  - Keep existing `UpdateAsync` for backward compat (called from `install` path)
  - Size: **M**
- [x] **10.3** [MODIFY] `src/FlowForge.Installer/Modules/FlowForgeModule.cs`:
  - Extract `UpdateSkillsForIdeAsync(ide, home, ffRepo, detector)` from existing `Install` logic
  - Add SHA-256 diff detection before overwrite
  - Keep existing `Install` unchanged
  - Size: **L**
- [x] **10.4** [MODIFY] `src/FlowForge.Installer/Commands/StatusCommand.cs`:
  - Add FlowDoc and Installer rows to status table
  - Use `ComponentRegistry.GetAllVersions()` for data
  - Size: **S**
- [x] **10.5** [MODIFY] `src/FlowForge.Installer/Infrastructure/ManifestClient.cs`:
  - Add `CheckCrossComponentCompatibility(manifest, installedComponents)` method
  - Validates ALL installed components against manifest constraints
  - Size: **M**

### Phase 11: Cross-cutting concerns

- [x] **11.1** [SEC] Audit: verify `engram.db`, `-wal`, `-shm`, `local_memory/` are NEVER written by updater code
  - Grep for write operations to `~/.engram/` paths in `Update/` directory
  - Add unit test that asserts no writes to protected paths
  - Size: **S**
- [x] **11.2** [SEC] Verify binary permissions after copy:
  - After `File.Move` to target, check `UnixFileMode` is `755` (not setuid/setgid)
  - If unexpected permissions: `chmod 755` + warning
  - Size: **S**
- [x] **11.3** [LOG] Structured logging for update operations (NFR-LOG-001):
  - Each update generates: timestamp, component, old version, new version, SHA-256 pre/post, result
  - Extend `InstallerLogger` with `UpdateOperation` method (or use existing `Info` with structured message)
  - Size: **S**
- [x] **11.4** [SEC] MCP merge debug log sanitization (RNF-SEC-006):
  - Debug log of MCP diff must NOT include env var values
  - Mask API keys/tokens in log output
  - Size: **S**
- [x] **11.5** [NFR] Backup retention enforcement (NFR-REL-002):
  - After each backup creation, call `BackupManager.PruneOldBackups`
  - Verify max 5 backups per component
  - Size: **S** (covered in 2.1)

### Phase 12: Final regression + integration

- [x] **12.1** [REGRESSION] Re-run `InstallerBaselineTests` to verify no regression
  - Size: **S**
- [ ] **12.2** [REGRESSION] Manual test PM-1: Happy path update all components
  - Size: **S**
- [ ] **12.3** [REGRESSION] Manual test PM-2: Rollback on broken binary
  - Size: **S**
- [ ] **12.4** [REGRESSION] Manual test PM-3: MCP merge preserves existing servers
  - Size: **S**
- [ ] **12.5** [REGRESSION] Manual test PM-4: User-modified agent detection
  - Size: **S**
- [ ] **12.6** [REGRESSION] Manual test PM-5: Cache git refresh
  - Size: **S**
- [x] **12.7** Run full test suite (`dotnet test`) — all green required
  - Size: **S**

---

## 6. Effort Estimation Summary

| Phase | Tasks | T-shirt | Estimated effort |
|-------|-------|---------|-----------------|
| 0. Installer Protection | 0.1–0.3 | S + M + S | ~2h |
| 1. Foundation | 1.1–1.5 | S + S + S + M + S | ~2h |
| 2. Backup + Rollback | 2.1–2.2 | M + M | ~3h |
| 3. Health-Check | 3.1–3.2 | M + M | ~2h |
| 4. MCP Config Merge | 4.1–4.2 | L + L | ~4h |
| 5. Agent Detection | 5.1–5.2 | M + M | ~2h |
| 6. Process Check | 6.1–6.2 | M + S | ~1.5h |
| 7. Cache Refresh | 7.1–7.3 | M + S + M | ~2h |
| 8. Sidecar Generalization | 8.1–8.2 | M + S | ~2h |
| 9. UpdateOrchestrator | 9.1–9.4 | XL + XL | ~6h |
| 10. Command Integration | 10.1–10.5 | L + M + L + S + M | ~4h |
| 11. Cross-cutting | 11.1–11.5 | 5×S | ~2h |
| 12. Final Regression | 12.1–12.7 | 7×S | ~2h |
| **TOTAL** | | | **~34.5h** |

### T-shirt sizing guide

| Size | Meaning | Time range |
|------|---------|-----------|
| **S** | Simple change, clear pattern, < 50 LOC | 0.5–1h |
| **M** | Moderate complexity, some design needed, 50–150 LOC | 1–2h |
| **L** | Complex, multiple integration points, 150–300 LOC | 2–3h |
| **XL** | Architectural, cross-cutting, 300+ LOC | 3–5h |

---

## 7. Acceptance Criteria

### Global

- [x] All existing tests pass (`dotnet test` green)
- [x] All new tests pass
- [x] No regression in `flowforge install`, `flowforge status`, `flowforge doctor`, `flowforge uninstall`
- [ ] `flowforge update --component engram` works end-to-end with backup + health-check + rollback
- [ ] `flowforge update --component flowforge-skills` updates skills for detected IDEs
- [ ] `flowforge update --component all` follows topological order
- [ ] MCP merge preserves existing servers (PM-3)
- [ ] User-modified agents detected and handled (PM-4)
- [ ] Cache git refresh works (PM-5)
- [ ] Rollback works on broken binary (PM-2)
- [x] Idempotent: re-running same update is a no-op (FR-007)
- [x] `install.log` has structured entries per update (NFR-LOG-001)
- [x] SHA-256 mandatory on all downloads (RNF-SEC-002)
- [x] Binary permissions verified post-copy (RNF-SEC-008)
- [x] `engram.db` and `local_memory/` never touched by updater (RNF-SEC-005)
- [x] Backup retention capped at 5 per component (NFR-REL-002)
- [x] AOT-safe: no reflection, all JSON via source-gen

### Per-FR acceptance

| FR | Acceptance criteria |
|----|-------------------|
| FR-001 | `--component` flag accepts: engram, flowforge-skills, all. Invalid component → error message. |
| FR-002 | Backup created before swap; health-check runs on temp binary; rollback restores original on failure. |
| FR-003 | Cursor `mcp.json` and Antigravity `mcp_config.json` merged surgically; existing servers preserved byte-for-byte. |
| FR-004 | `git pull` on cache before skills copy; fallback to fresh clone on failure; abort if clone fails. |
| FR-005 | Post-update: `engram --version` matches expected; MCP configs parse as valid JSON. |
| FR-006 | SHA-256 diff detects modified agents; offers Skip/Backup+Overwrite/Overwrite; `--yes` defaults to Backup+Overwrite. |
| FR-007 | Re-running update with same version → no-op; no duplicates; no corruption. |
| FR-008 | `config.json` has per-component version entries; `flowforge status` shows all. |
| FR-009 | OUT (diferido) — no implementation. |
| FR-010 | Skills update works for Cursor, OpenCode, Antigravity, VS Code Copilot, Kilo. |
| FR-011 | Sidecar created/updated for all IDE destinations; user custom files not touched. |
| FR-012 | Running engram processes detected; warning shown; abort unless `--force`. |

---

## 8. Dependency Graph (task ordering)

```
Phase 0 (baseline) ──────────────────────────────────────────────┐
                                                                  │
Phase 1 (foundation) ──→ Phase 2 (backup) ──→ Phase 9 (orchestrator)
                   ──→ Phase 3 (health)  ──┘         ↑
                   ──→ Phase 4 (MCP merge) ──────────┤
                   ──→ Phase 5 (agent detect) ───────┤
                   ──→ Phase 6 (process check) ──────┤
                   ──→ Phase 7 (cache refresh) ──────┤
                   ──→ Phase 8 (sidecar) ────────────┘
                                                     │
Phase 10 (command integration) ←─────────────────────┘
         │
Phase 11 (cross-cutting)
         │
Phase 12 (final regression)
```

**Critical path**: Phase 0 → Phase 1 → Phase 9 → Phase 10 → Phase 12

---

## 9. Risk Mitigations

| Risk | Mitigation | Task |
|------|-----------|------|
| Regression in existing commands | Phase 0 baseline tests; Phase 12 re-run | 0.1, 0.3, 12.1 |
| MCP merge data loss | Surgical merge pattern from OpenCode; unit tests with existing servers | 4.1, 4.2 |
| Rollback untested | BackupManager tests; PM-2 manual test | 2.2, 12.3 |
| Cache git stale/corrupt | CacheRefresher with fallback to fresh clone | 7.1 |
| AOT breakage | No new reflection; all JSON source-gen | 11.1 audit |
| Cross-platform process check | EngramProcessChecker with platform-specific implementations | 6.1 |
