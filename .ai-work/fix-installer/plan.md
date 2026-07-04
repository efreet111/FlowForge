---
feature_slug: fix-installer
spec_version: "1.0"
plan_version: "1.0"
date: 2026-06-29
total_tasks: 16
phases: 4
estimated_cycles: 2
---

# Plan: Fix FlowForge Installer "Stuck" Issues

## 1. Impact and Dependencies

### What Changes

| Component | Change Type | Description |
|-----------|-------------|-------------|
| `install/install.sh` | MODIFY | Add progress visibility, FLOWFORGE_YES support, chown fix, `--diagnose` flag |
| `GitHubReleasesClient.cs` | MODIFY | Add configurable HTTP timeouts (30s API / 300s download) |
| `EngramModule.cs` | MODIFY | Catch timeout exceptions, format actionable errors with AnsiConsole |
| `InstallCommand.cs` | MODIFY | Move banner to top of RunAsync, add immediate startup feedback |
| `Program.cs` | MODIFY | Wire timeout configuration, register DoctorCommand |
| `PathHelper.cs` | MODIFY | Add sudo ownership helper paths |
| `DoctorCommand.cs` | NEW | Diagnostic command implementation |
| README.md / docs/ | MODIFY | Update troubleshooting, add doctor command docs |

### Dependencies

- **Upstream**: engram-dotnet v1.2.1 (already released with `libe_sqlite3.so`)
- **No breaking changes**: All modifications are backward-compatible
- **Environment variables**: `FLOWFORGE_API_TIMEOUT_SECONDS`, `FLOWFORGE_DOWNLOAD_TIMEOUT_SECONDS`, `FLOWFORGE_YES`

### Risk Assessment

| Risk | Mitigation |
|------|------------|
| Timeout too aggressive | Use generous defaults (30s API / 300s download), overridable via env vars |
| TTY detection regression | Test both `[ -t 0 ]` AND `$FLOWFORGE_YES` explicitly |
| chown fails silently | Verify with `ls -la` post-install, warn on mismatch |

---

## 2. File Changes (Proposed Changes)

### [NEW] `src/FlowForge.Installer/Commands/DoctorCommand.cs`

Responsibility: Implement `flowforge doctor` diagnostic command with status table.

Entry point registration in `Program.cs`:
```csharp
app.Add<DoctorCommand>("doctor");
```

### [NEW] `--diagnose` flag in `install/install.sh`

Responsibility: Pre-install health check (curl/wget availability, PATH writability, GitHub reachability) without installing.

### [MODIFY] `install/install.sh`

Exact changes:
1. **FR-002 (progress)**: Change line 56 `curl -fsSL` → `curl -fSL --progress-bar` OR add `echo "Descargando flowforge..."` before silent fetch
2. **FR-002 (wget fallback)**: Add spinner message for wget that prints every 5 seconds
3. **FR-003 (headless)**: Add `$FLOWFORGE_YES` check before `[ -t 0 ]` test (lines 127-133)
4. **FR-007 (ownership)**: Add sudo detection with `$SUDO_USER` + `chown` post-install
5. **OQ-4 (optional)**: Add `--diagnose` flag handling in arg parser (lines 18-24)

### [MODIFY] `src/FlowForge.Installer/Infrastructure/GitHubReleasesClient.cs`

Exact changes:
1. **FR-001 (timeout)**: Add constructor/env-var-based timeout configuration
2. **FR-001 (timeout)**: Set `HttpClient.Timeout` from `FLOWFORGE_API_TIMEOUT_SECONDS` (default 30s)
3. **FR-001 (download timeout)**: Accept timeout parameter for `DownloadEngramAsync` (default 300s)
4. **FR-001 (cleanup)**: On timeout, delete partial download file
5. **FR-005 (error)**: Wrap `GetLatestVersionAsync` in try/catch for `TaskCanceledException` → throw clear `TimeoutException`

### [MODIFY] `src/FlowForge.Installer/Modules/EngramModule.cs`

Exact changes:
1. **FR-005 (actionable error)**: In `InstallAsync`, catch timeout from `GetLatestVersionAsync` and format with AnsiConsole.MarkupLine (red + manual URL)
2. **FR-005 (error message)**: Line 30-34 → Add "GitHub no responde" / "Verificá tu conexión" + URL to engram-dotnet releases
3. Ensure exit with code 1 on GitHub failure (not hang)

### [MODIFY] `src/FlowForge.Installer/Commands/InstallCommand.cs`

Exact changes:
1. **FR-004 (startup feedback)**: Move banner print (line 42) to VERY TOP of `RunAsync`, before any async call
2. **FR-004 (liveness)**: Add "Connecting to GitHub..." message immediately after banner
3. **FR-007 (ownership warning)**: Add post-install ownership check + warning if files owned by root

### [MODIFY] `src/FlowForge.Installer/Program.cs`

Exact changes:
1. Register `DoctorCommand` in CAF routing:
   ```csharp
   app.Add<DoctorCommand>("doctor");
   ```
2. Update `HttpClient` timeout initialization to use env vars (line 31)

### [MODIFY] `README.md`

Exact changes:
1. Add troubleshooting section with MCP diagnostic steps
2. Document `flowforge doctor` usage
3. Document ownership fix: `sudo chown -R $USER:$USER ~/.engram ~/.local/bin/engram ...`
4. Add env var documentation: `FLOWFORGE_API_TIMEOUT_SECONDS`, `FLOWFORGE_DOWNLOAD_TIMEOUT_SECONDS`

### [MODIFY] `README.es.md`

Same changes as README.md but in Spanish.

### [MODIFY] `docs/` (select files)

Files to review and update:
- `docs/decisions/ADR-005-installer-headless-native-libs.md` — append verification results
- Any install/MCP guides that need timeout/doctor references

---

## 3. Contracts and Schemas

### HTTP Timeout Configuration

```csharp
// In Program.cs - HttpClient initialization
var apiTimeoutSeconds = int.TryParse(
    Environment.GetEnvironmentVariable("FLOWFORGE_API_TIMEOUT_SECONDS"), 
    out var apiSecs) ? apiSecs : 30;

var downloadTimeoutSeconds = int.TryParse(
    Environment.GetEnvironmentVariable("FLOWFORGE_DOWNLOAD_TIMEOUT_SECONDS"), 
    out var dlSecs) ? dlSecs : 300;

var http = new HttpClient { 
    Timeout = TimeSpan.FromSeconds(apiTimeoutSeconds) 
};
```

### GitHubReleasesClient Constructor Signature

```csharp
public sealed class GitHubReleasesClient(
    HttpClient http, 
    InstallerLogger log,
    int downloadTimeoutSeconds = 300)
```

### DoctorCommand Check Results

```csharp
public readonly record struct DoctorCheck(
    string Name,
    bool Passed,
    string? Hint = null
);
```

Checks to implement:
| Check | Implementation |
|-------|----------------|
| flowforge binary | `File.Exists(PathHelper.InstallerBinary)` |
| GitHub reachable | `HttpClient.GetAsync("https://api.github.com")` with short timeout |
| MCP configured | Check `~/.cursor/mcp.json` or `~/.config/opencode/opencode.json(c)` exists and contains "engram" |
| engram binary | `File.Exists(PathHelper.EngramBinary)` |
| engram PATH | `Environment.GetEnvironmentVariable("PATH")` contains `EngramBinDir` |

Exit codes for doctor:
- `0` = all checks pass
- `1` = hard error (exception)
- `2` = partial failure (some checks fail)

### Ownership Fix Logic

```bash
# In install.sh
if [ -n "$SUDO_USER" ]; then
    TARGET_USER="$SUDO_USER"
    chown -R "$TARGET_USER:$TARGET_USER" "$INSTALL_DIR/flowforge" 2>/dev/null || true
    chown -R "$TARGET_USER:$TARGET_USER" "$HOME/.local/bin/engram" 2>/dev/null || true
    chown -R "$TARGET_USER:$TARGET_USER" "$HOME/.engram" 2>/dev/null || true
    echo "  [INFO] Ajustando permisos para usuario: $TARGET_USER"
fi
```

### Headless Detection Logic

```bash
# In install.sh - combine both checks
if [ -n "$FLOWFORGE_YES" ] || [ ! -t 0 ]; then
    # Headless mode
    "$INSTALL_DIR/flowforge" install --yes
else
    # Interactive mode
    "$INSTALL_DIR/flowforge" install
fi
```

---

## 4. Implementation Checklist

### Fase A: Fixes Críticos (FR-001, FR-002, FR-003, FR-007)

**A1.1** [MODIFY] `install/install.sh` — FR-003 headless mode
- [x] Add `FLOWFORGE_YES` env var check before TTY detection
- [x] Update wizard launch logic (lines 125-133) to honor `FLOWFORGE_YES`
- Contract: If `FLOWFORGE_YES=1`, always pass `--yes` regardless of TTY
- Done when: Script runs with `FLOWFORGE_YES=1 | bash` without interactive prompts

**A1.2** [MODIFY] `install/install.sh` — FR-002 download progress
- [x] Change `curl -fsSL` to `curl -fSL --progress-bar` for visible progress
- [x] Add spinner loop for wget fallback (print message every 5 seconds)
- Contract: User sees download progress (curl) or activity indicator (wget)
- Done when: Download shows progress bar or periodic messages

**A1.3** [MODIFY] `install/install.sh` — FR-007 ownership fix
- [x] Detect `$SUDO_USER` after platform detection
- [x] Add `chown` commands for `~/.engram`, `~/.local/bin/engram`, `~/.local/bin/libe_sqlite3.so`, `~/.local/bin/flowforge`
- [x] Add warning message if ownership mismatch detected
- Contract: Files installed via sudo are owned by original user
- Done when: Post-install `ls -la` shows correct ownership

**A2.1** [MODIFY] `src/FlowForge.Installer/Infrastructure/GitHubReleasesClient.cs` — FR-001 timeouts
- [x] Add `downloadTimeoutSeconds` parameter to constructor
- [x] Read `FLOWFORGE_API_TIMEOUT_SECONDS` env var for API calls (default 30s)
- [x] Read `FLOWFORGE_DOWNLOAD_TIMEOUT_SECONDS` env var for downloads (default 300s)
- [x] Implement timeout cleanup: delete partial file on `TaskCanceledException`
- Contract: Operations timeout with clear exceptions, partial files cleaned
- Done when: Unit test shows timeout after specified seconds

**A2.2** [MODIFY] `src/FlowForge.Installer/Modules/EngramModule.cs` — FR-005 error handling
- [x] Wrap `GetLatestVersionAsync` call in try/catch for `TimeoutException`
- [x] Format error with AnsiConsole: "[red]GitHub no responde[/]", hint with URL
- [x] Ensure `Environment.Exit(1)` on failure (no hang)
- Contract: Timeout shows actionable Spanish error, exits cleanly
- Done when: Blocking GitHub API shows error within 35s and exits

---

### Fase B: UX y Feedback (FR-004, FR-005 refinement)

**B1.1** [MODIFY] `src/FlowForge.Installer/Commands/InstallCommand.cs` — FR-004 startup feedback
- [x] Move `AnsiConsole.Write(new Rule(...))` to first line of `RunAsync`
- [x] Add "Connecting to GitHub..." message immediately after banner
- [x] Ensure first output appears within 5 seconds
- Contract: User sees "FlowForge Stack Installer v{X}" within 5s of launch
- Done when: Manual test shows banner within 5 seconds

**B1.2** [MODIFY] `src/FlowForge.Installer/Commands/InstallCommand.cs` — FR-007 ownership warning
- [x] Add post-install check for root-owned files
- [x] If root ownership detected, print warning with chown command
- Contract: User sees warning if installation ran as root
- Done when: `sudo flowforge install` shows ownership warning at end

---

### Fase C: Nuevo Comando Doctor (FR-006)

**C1.1** [NEW] `src/FlowForge.Installer/Commands/DoctorCommand.cs`
- [x] Create class `DoctorCommand(InstallerContext ctx)`
- [x] Implement checks: flowforge binary, GitHub API, MCP config, engram binary, PATH
- [x] Render status table with Spectre.Console (✓ green / ✗ red)
- [x] Print hints for failed checks
- [x] Return exit code 0 (all pass), 1 (error), 2 (partial)
- Contract: `flowforge doctor` shows complete health status
- Done when: Command shows table with 5 checks, correct exit code

**C1.2** [MODIFY] `src/FlowForge.Installer/Program.cs` — Register doctor
- [x] Add `app.Add<DoctorCommand>("doctor");` in command registration section
- Contract: `flowforge doctor` invokes DoctorCommand
- Done when: Command runs without "unknown command" error

---

### Fase D: Documentación (FR-008)

**D1.1** [MODIFY] `README.md` — Update install section
- [x] Document `FLOWFORGE_API_TIMEOUT_SECONDS` env var
- [x] Document `FLOWFORGE_DOWNLOAD_TIMEOUT_SECONDS` env var
- [x] Document `FLOWFORGE_YES` for headless installs
- [x] Add troubleshooting subsection for MCP issues

**D1.2** [MODIFY] `README.md` — Add doctor command documentation
- [x] Add `flowforge doctor` usage example
- [x] Explain exit codes (0, 1, 2)
- [x] Add sample output showing checks

**D1.3** [MODIFY] `README.md` — Add ownership fix documentation
- [x] Document post-install `chown` command for sudo installs
- [x] Explain SQLite Error 14 scenario and fix
- Contract: Users can resolve ownership issues via README
- Done when: README contains copy-pasteable chown command

**D1.4** [MODIFY] `README.es.md` — Mirror all README changes in Spanish
- [x] Translate all new sections
- Contract: Spanish docs match English docs
- Done when: README.es.md has equivalent troubleshooting sections

**D1.5** [MODIFY] `docs/` — Update relevant guides
- [x] Review `docs/decisions/ADR-005-installer-headless-native-libs.md` for accuracy
- [x] Update any install guides with doctor/timeout references
- Done when: Docs reference `flowforge doctor` where relevant

---

## 5. Optional Enhancements (from OQ-3, OQ-4)

### OQ-3: Doctor vs Status
Decision: ✅ Confirmed as new `doctor` command (follows npm/brew conventions)

### OQ-4: `--diagnose` flag in install.sh
- [x] Add `--diagnose` to arg parser in `install/install.sh`
- [x] Implement checks: curl/wget available, PATH writable, GitHub reachable
- [x] Print report without installing
- Contract: `curl ... | bash -s -- --diagnose` shows pre-install health
- Done when: Diagnose-only run completes without installing

---

## 6. Release Checklist

For version bump to `v0.1.0-alpha.10`:

| Step | Task | File(s) |
|------|------|---------|
| 1 | Update version constant in `StatusCommand.cs` | `src/FlowForge.Installer/Commands/StatusCommand.cs` |
| 2 | Update version in `InstallCommand.cs` banner | `src/FlowForge.Installer/Commands/InstallCommand.cs` |
| 3 | Update `CHANGELOG.md` with fixes | `CHANGELOG.md` |
| 4 | Build and test AOT binary | `dotnet publish -c Release -r linux-x64` |
| 5 | Create GitHub Release with assets | GitHub web UI |
| 6 | Attach `flowforge-linux-x64` + `.sha256` checksum | Release assets |
| 7 | Update install script URL reference | Verify `install/install.sh` points to correct release |

---

## 7. Verification Mapping

Maps spec.md PM-* items to implementation tasks:

| PM ID | Test Case | Covered By |
|-------|-----------|------------|
| PM-1 | Happy path install | A1.2 (progress), A1.1 (headless) |
| PM-2 | Timeout error handling | A2.1 (timeouts), A2.2 (error format) |
| PM-3 | FLOWFORGE_YES headless | A1.1 (env var check) |
| PM-4 | Doctor command output | C1.1 (doctor implementation) |
| PM-5 | Visual startup feedback | B1.1 (banner at top) |

---

## 8. Files to Read for Context

Before implementing, review:
- `spec.md` (this folder) — full requirements
- `install/install.sh` — bash bootstrap to modify
- `src/FlowForge.Installer/Program.cs` — command registration
- `src/FlowForge.Installer/Commands/StatusCommand.cs` — table rendering pattern
- `src/FlowForge.Installer/Infrastructure/PathHelper.cs` — path constants

---

> **Assumption Note**: OQ-3 assumes new `doctor` command (not extending `status`). OQ-4 assumes adding `--diagnose` to install.sh. Both marked [OPTIONAL] in spec; implement if time permits.
