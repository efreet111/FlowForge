---
capability_matrix:
  ai_reasoning:
    - UX decision: spinner vs progress bar for binary downloads (trade-off between accurate progress and implementation complexity)
    - UX decision: error message wording and actionable hints for timeout scenarios
    - UX decision: visual layout of `doctor` status table (colors, column order)
  deterministic:
    - HTTP timeout must be enforced at the HttpClient level (not just CancellationToken)
    - Timeout values: API calls = 30s default, binary downloads = 300s default
    - Env var names: FLOWFORGE_API_TIMEOUT_SECONDS, FLOWFORGE_DOWNLOAD_TIMEOUT_SECONDS
    - Exit codes: 0 = success, 1 = error/timeout, 2 = partial failure (doctor only)
    - TTY detection must use both `[ -t 0 ]` AND `$FLOWFORGE_YES` env var check
    - First output must appear within 5 seconds of process launch (measured from Main entry point)
---

# Spec: Fix FlowForge Installer "Stuck" Issues

## 1. Objective and scope

### Objective
Fix the FlowForge installer that "gets stuck" (se queda pegado) during installation, causing users to perceive it as frozen when it's actually waiting on network I/O without feedback.

### Scope (In)
- Add configurable HTTP timeouts to `GitHubReleasesClient`
- Show download progress in `install/install.sh`
- Improve headless/interactive mode detection in bash script
- Add immediate startup feedback in `flowforge install`
- Add actionable error messages on GitHub API failures/timeouts
- Add `flowforge doctor` diagnostic command
- Fix post-install permissions when installer runs as root (`chown` correction)
- Update documentation: README.md (install URL, steps, MCP diagnostic), `docs/` guides

### Scope (Out)
- Windows installer (.ps1) changes — separate future work
- Changing the fundamental installation flow or component architecture
- Adding retry logic with exponential backoff
- Parallel downloads

---

## 2. Functional requirements (FR)

### FR-001: Configurable HTTP timeout in GitHubReleasesClient
`GitHubReleasesClient` must enforce a configurable HTTP timeout on all network operations.

- **Scenario A (API timeout):** Given a slow or unresponsive GitHub API, When `GetLatestVersionAsync` is called, Then the request times out after `FLOWFORGE_API_TIMEOUT_SECONDS` (default: 30s) and throws a clear `TimeoutException`.

- **Scenario B (Download timeout):** Given a slow binary download, When `DownloadEngramAsync` is called, Then the request times out after `FLOWFORGE_DOWNLOAD_TIMEOUT_SECONDS` (default: 300s) and cleans up the partial file.

### FR-002: Download progress visibility in install.sh
The bash bootstrap script must provide visual feedback during binary download.

- **Scenario A (curl progress):** Given `curl` is available, When downloading the flowforge binary, Then `curl --progress-bar` is used (or a "Downloading..." message is printed before silent fetch).

- **Scenario B (wget fallback):** Given only `wget` is available, When downloading, Then a spinner message prints every 5 seconds until completion.

### FR-003: Reliable headless mode detection
The bash script must correctly detect non-interactive environments and honor explicit flags.

- **Scenario A (FLOWFORGE_YES env var):** Given `FLOWFORGE_YES=1` is set in environment, When the script reaches the wizard launch, Then it always passes `--yes` to `flowforge install` regardless of TTY detection.

- **Scenario B (exec </dev/tty edge case):** Given the script was piped via curl, When `exec </dev/tty` succeeds, But `[ -t 0 ]` returns true unexpectedly, Then the script must still check if stdin is readable and has actual input pending before choosing interactive mode.

### FR-004: Immediate startup feedback
The `flowforge install` command must produce visible output within 5 seconds of launch.

- **Scenario A (banner on startup):** Given the user runs `flowforge install`, When the process starts, Then within 5 seconds it prints at minimum: "FlowForge Stack Installer", the version, and "Connecting to GitHub..." (or similar liveness indicator).

- **Scenario B (early error):** Given GitHub is unreachable, When the installer starts, Then it shows the connection error immediately (not after a 100s timeout).

### FR-005: Actionable error on GitHub failure
When `GetLatestVersionAsync` fails or times out, the user sees a clear, actionable error.

- **Scenario A (timeout error):** Given the GitHub API request times out, When the error is displayed, Then the message includes: "GitHub no responde", "Verificá tu conexión", and a manual install URL: `https://github.com/efreet111/FlowForge/releases`.

- **Scenario B (clean exit):** Given any GitHub error occurs, When processing stops, Then the process exits with code 1 (not hanging indefinitely).

### FR-006: Diagnostic command (`flowforge doctor`)
A new `doctor` subcommand checks system health and prints a status table.

- **Scenario A (full check):** Given the user runs `flowforge doctor`, When all checks pass, Then it prints a table with: [✓] flowforge binary exists, [✓] GitHub reachable, [✓] MCP configured, [✓] engram binary exists, [✓] engram PATH registered.

- **Scenario B (failure diagnosis):** Given engram is not installed, When `doctor` runs, Then it marks engram as [✗] and prints a hint: "Run `flowforge install` to install engram-dotnet".

### FR-007: Fix post-install ownership when run as root
When the installer detects it ran as root but the target `$HOME` belongs to another user (via `$SUDO_USER` or heuristic), it must fix file ownership after installation.

- **Scenario A (sudo detection):** Given the installer runs via `sudo` or as root with `SUDO_USER` set, When installation completes, Then all files under `~/.engram`, `~/.local/bin/engram`, `~/.local/bin/flowforge`, `~/.local/bin/libe_sqlite3.so` are `chown`-ed to `$SUDO_USER`.

- **Scenario B (documentation hint):** Given the installer detects ownership mismatch post-install, When printing the summary, Then it prints a warning: "Archivos instalados como root. Si el MCP falla, ejecutá: `sudo chown -R $USER:$USER ~/.engram ~/.local/bin/engram ~/.local/bin/libe_sqlite3.so`".

### FR-008: Update documentation
All user-facing documentation must reflect the current install flow, known issues, and MCP diagnostic steps.

- **Scenario A (README):** The `README.md` install section must show the correct install URL (`install/install.sh`), expected output, and post-install steps (Reload Window, verify MCP with `flowforge doctor`).

- **Scenario B (docs/ guides):** Any existing docs in `docs/` that reference installation or MCP configuration must be reviewed and updated for accuracy (correct binary names, paths, env vars).

---

## 3. Non-functional requirements (NFR)

### NFR-001: Cross-platform compatibility
No regressions on Windows, macOS, or Linux. All changes must preserve existing platform-specific paths and behaviors.

### NFR-002: Configurable via environment variables
HTTP timeout values must be configurable via:
- `FLOWFORGE_API_TIMEOUT_SECONDS` (default: 30)
- `FLOWFORGE_DOWNLOAD_TIMEOUT_SECONDS` (default: 300)

### NFR-003: AOT-safe
All JSON serialization must continue using existing source generators (`GitHubJsonContext`, `McpJsonContext`). No dynamic code generation.

### NFR-004: CI-friendly
When `--yes` is passed or no TTY is detected, the installer must run without interactive prompts. Exit code must be 0 on success, non-zero on any failure.

---

## 4. Capability Matrix

| Requirement | Component | Implementation notes |
|-------------|-----------|---------------------|
| FR-001 | `GitHubReleasesClient.cs` | Set `HttpClient.Timeout` from env var; wrap calls in try/catch for `TaskCanceledException` (timeout) |
| FR-002 | `install/install.sh` | Change `curl -fsSL` to `curl -fSL --progress-bar` or add `echo "Descargando..."` fallback |
| FR-003 | `install/install.sh` | Add `FLOWFORGE_YES` check before `[ -t 0 ]` test; combine conditions |
| FR-004 | `InstallCommand.cs` | Move banner print to very top of `RunAsync`, before any async call |
| FR-005 | `EngramModule.cs`, `GitHubReleasesClient.cs` | Catch timeout in `EngramModule.InstallAsync`, format with AnsiConsole.MarkupLine |
| FR-006 | New `DoctorCommand.cs` + `Program.cs` | Add new command class; register in CAF routing; check File.Exists for binaries, HttpClient.GetAsync for GitHub |
| FR-007 | `install/install.sh` + `InstallCommand.cs` | Detect `$SUDO_USER` in bash; in .NET detect `SUDO_USER` env var and chown after install |
| FR-008 | `README.md`, `docs/` | Review and update all installation docs, MCP setup guide, diagnostic steps |

---

## 5. Developer manual tests (required — mark [x] before /flow-close)

| ID | Case / flow | Steps (summary) | Expected result | [ ] |
|----|-------------|-----------------|-----------------|-----|
| PM-1 | Happy path install | 1. Run `curl ... \| bash` in fresh VM/container<br>2. Observe output | Progress bar shows download %; wizard starts within 10s; engram installs successfully | [ ] |
| PM-2 | Timeout error handling | 1. Block GitHub API (e.g., via hosts file to 127.0.0.1)<br>2. Run `flowforge install` | Clear error within 35s (30s + buffer); message includes manual URL; exit code 1 | [ ] |
| PM-3 | FLOWFORGE_YES headless | 1. `export FLOWFORGE_YES=1`<br>2. Run install script from piped curl | No interactive prompts; `--yes` flag passed; installation completes | [ ] |
| PM-4 | Doctor command output | 1. Run `flowforge doctor` on working install<br>2. Delete engram binary, run again | First run: all [✓]; Second run: engram [✗] with hint | [ ] |
| PM-5 | Visual startup feedback | 1. Run `flowforge install`<br>2. Time first output | Banner/version prints within 5 seconds of command start | [ ] |

---

## 6. Open questions for human (OQ-*)

| ID | Tag | Question | Default / assumption |
|----|-----|---------|---------------------|
| OQ-1 | ~~[BLOCKER]~~ RESUELTO | ¿Existe release de `flowforge` en GitHub? | ✅ Sí — `v0.1.0-alpha.9` (pre-release) con asset `flowforge-linux-x64`. Releases desde alpha.7 a alpha.9 confirmados. |
| OQ-2 | ~~[BLOCKER]~~ RESUELTO | ¿Está `~/.local/bin/engram` en la máquina? | ✅ Sí — `engram v1.2.1` instalado en `~/.local/bin/engram` (107MB). También `flowforge` y `libe_sqlite3.so`. **MCP falla con SQLite Error 14** — posible causa: instalación ejecutada como root, `~/.engram/` sin permisos de escritura para el usuario `victor`. Fix inmediato: `sudo chown -R $USER:$USER ~/.engram ~/.local/bin/engram ~/.local/bin/libe_sqlite3.so ~/.local/bin/flowforge`. |
| OQ-3 | [OPTIONAL] | Should `flowforge doctor` be a new command or extend existing `flowforge status`? | Assumed: new `doctor` command (more discoverable, follows npm/brew conventions) |
| OQ-4 | [OPTIONAL] | Should we add a `--diagnose` flag to `install/install.sh` for offline pre-checks? | Assumed: yes, add `--diagnose` that checks curl/wget, PATH writability, and GitHub reachability without installing |

---

## Apéndice: Contexto engram-dotnet

- **Repo local**: `/run/media/victor/Nuevo vol/Proyectos/Desarrollo Personal/engram-dotnet/`
- **GitHub**: https://github.com/efreet111/engram-dotnet
- **Versión instalada**: `1.2.1`
- **3 modos de uso** (según `MCP-CONFIG.md`):
  - Solo local: `ENGRAM_SYNC_ENABLED=false`
  - Solo remoto: `ENGRAM_URL`
  - Local + sync: `ENGRAM_DATA_DIR` + `ENGRAM_SERVER_URL` + `ENGRAM_SYNC_ENABLED=true`
- **Config actual** (`~/.cursor/mcp.json`): modo local+sync con `ENGRAM_SERVER_URL=http://192.168.0.178:7437`
- **Bug MCP activo**: `SQLite Error 14 — unable to open database file` en `~/.engram/engram.db`
  - Causa probable: `~/.engram/` tiene ownership root:root (instalador corrió como root)
  - Fix inmediato: `sudo chown -R $USER:$USER ~/.engram ~/.local/bin/engram ~/.local/bin/libe_sqlite3.so ~/.local/bin/flowforge`
- **SqliteStore** valida que `DataDir` sea path absoluto; luego `Directory.CreateDirectory(DataDir)` y abre `engram.db`
- **libe_sqlite3.so**: debe estar en el mismo directorio que el binario (`~/.local/bin/`) — ya está ahí

---

> **Spec version:** 1.0  
> **Derived from:** Context Map from forge-discovery (installer "stuck" analysis)  
> **Key files read:** `install/install.sh`, `GitHubReleasesClient.cs`, `EngramModule.cs`, `PathHelper.cs`, `InstallerConfig.cs`, `Program.cs`, `StatusCommand.cs`, `InstallCommand.cs`

## Memory Signal
- type: decision
- significance: high
- summary: "Adding configurable HTTP timeouts (30s API / 300s download) with env var overrides to fix perceived 'stuck' installer behavior"
