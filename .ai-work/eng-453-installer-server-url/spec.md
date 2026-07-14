---
capability_matrix:
  ai_reasoning:
    - UX copy phrasing for sync server URL prompt (Spanish: "URL del servidor sync")
    - Ordering of prompts: component selection → engram mode → sync URL → IDEs
    - Reachability warning wording ("no responde a GET /health — la instalación continúa")
    - Headless error message phrasing (--server-url flag hint)
  deterministic:
    - URL must parse as Uri with http or https scheme — rejection prompt re-asks
    - ENGRAM_SERVER_URL env var must be checked before falling through to prompt
    - DetectSyncMode returns "sync" when env var or persisted config has a URL
    - SyncConfig is written atomically to ~/.engram/config.json (store.Update)
    - SyncUrl is passed to EngramModule.InstallAsync(mode, url) — not silently defaulted
    - In headless mode with mode=sync and no URL: abort with clear error, exit code non-zero
    - Existing sync config is pre-filled on re-install (existingUrl → prompt default)
    - Hardcoded 192.168.0.178 must not appear in any source file
    - ProbeServerHealth has 3-second timeout; unreachable is a warning, not a block
---

# Spec: ENG-453 — Installer must prompt for `ENGRAM_SERVER_URL` in `mode=sync`

## 1. Objective and scope

**Feature slug:** `eng-453-installer-server-url`
**Work item:** ENG-453 (tracked in engram-dotnet BACKLOG, implemented in FlowForge repo)
**Branch:** `main` (commit `6f13d7e`)
**ADRs:** [ADR-010](../../docs/decisions/ADR-010-installer-prompt-for-server-url.md) (Proposed → needs Accepted)

**Status:** retrospective synthesis — code implemented on `main`, tests present, ADR-010 status change and POST-INSTALL.md update pending.

### Problem

When installing engram-dotnet via `flowforge install` in **sync mode**, the installer originally:
- ✅ Asked if user wants sync
- ✅ Installed the binary and configured MCP
- ❌ Did **NOT** ask for the server URL (`ENGRAM_SERVER_URL`)
- ❌ Silently hardcoded `http://192.168.0.178:7437` as fallback

**Result**: SyncManager started without knowing which server to connect to → detected self-loop (ENG-452) → disabled with warning. User thought sync worked but it didn't. First-run experience killer.

### Origin

- **Discovery**: audit OSS 2026-06-23, during verification of ENG-451/452 fixes
- **Context map**: `.ai-work/eng-453-installer-server-url/context-map.md` (2026-07-06)
- **Design**: ADR-010 (2026-07-01, 235 lines, complete design)
- **Implementation**: commit `6f13d7e` (2026-07-02) — "fix(install): prompt for ENGRAM_SERVER_URL in mode=sync (ADR-010, ENG-453)"

### In scope

- Interactive mode: prompt `"URL del servidor sync"` when user chooses sync, with URL validation and reachability probe
- Headless mode: resolve URL from `--server-url` flag, `ENGRAM_SERVER_URL` env var, or persisted config; abort with clear error if none
- Persist `SyncConfig` (mode, remote_url, user, data_dir, connected_at) to `~/.engram/config.json`
- Pre-fill prompt with existing URL on re-install (config persistence)
- Remove the hardcoded `192.168.0.178` fallback from both `InstallCommand.cs` and `EngramModule.cs`
- Configure MCP env block with `ENGRAM_SERVER_URL` when sync is enabled

### Out of scope

- Auth tokens for the relay server (ENG-405 is Icebox)
- TLS / HTTPS validation beyond scheme check (LAN-only deployments today)
- `flowforge sync connect` command (ADR-009 — complementary, separate feature)
- Auto-update of `ENGRAM_SERVER_URL` on relay IP change
- PostgreSQL configuration (already handled by installer for centralized mode)
- `install.sh` / `install.ps1` bootstrap scripts (the URL is asked in the AOT binary, not the shell bootstrap)
- Mirroring to PowerShell installer (same codebase — C# AOT binary cross-platform)

### Files involved

| File | Role | Status |
|------|------|--------|
| `src/FlowForge.Installer/Commands/InstallCommand.cs` | Interactive wizard + headless mode + URL prompt + health probe | ✅ Implemented |
| `src/FlowForge.Installer/Modules/EngramModule.cs` | MCP config generation with ENGRAM_SERVER_URL | ✅ Implemented |
| `src/FlowForge.Installer/Models/InstallerConfig.cs` | `SyncConfig` model with `RemoteUrl` | ✅ Implemented |
| `tests/FlowForge.Installer.Tests/InstallerAsksForSyncUrlTests.cs` | 7 source-level regression tests | ✅ Implemented |
| `docs/decisions/ADR-010-installer-prompt-for-server-url.md` | Design doc status | 🔲 Needs "Accepted" |
| `POST-INSTALL.md` §3 | Manual workaround text | 🔲 Needs update |

---

## 2. Functional requirements (FR)

### FR-001 — Interactive sync URL prompt
When the user selects "Offline-first sync" in interactive mode, the installer must ask for the server URL with validation and a pre-filled default from previous installs.

- **Scenario A (first install, sync chosen):** Given a clean machine with no `~/.engram/config.json`, When `flowforge install` runs interactively and user selects "Offline-first sync", Then a `TextPrompt<string>` appears asking `"URL del servidor sync (http://host:port):"`, with validation that rejects empty, non-parseable, and non-http/https URLs, and re-prompts on invalid input.
- **Scenario B (re-install, pre-filled):** Given an existing `~/.engram/config.json` with `sync.remote_url: "http://10.0.0.5:7437"`, When `flowforge install` runs interactively and user selects sync, Then the prompt default value is `"http://10.0.0.5:7437"` and pressing Enter accepts it.
- **Scenario C (explicit flag pre-fill):** Given a machine with no config and `flowforge install` invoked interactively, When user selects sync and `--server-url http://10.0.0.5:7437` was passed, Then the prompt default is `"http://10.0.0.5:7437"`.
- **Scenario D (local mode, no prompt):** Given `flowforge install` runs interactively, When user selects "Local (SQLite, sin sync)", Then no URL prompt appears and `engramMode` is `"local"`.

### FR-002 — URL format validation
The interactive prompt must validate the URL before accepting it.

- **Scenario A (valid http URL):** Given the sync URL prompt is active, When user enters `"http://192.168.0.178:7437"`, Then `Uri.TryCreate(url, UriKind.Absolute, out _)` returns true, scheme is `http`, and the prompt accepts it.
- **Scenario B (invalid URL):** Given the prompt is active, When user enters `"not-a-url"` or empty string, Then `ValidationResult.Error` is returned with `"URL inválida"` or `"Requerido en modo sync"` and the prompt re-asks.
- **Scenario C (non-http scheme):** Given the prompt is active, When user enters `"ftp://server:21"`, Then scheme check fails and `ValidationResult.Error("Scheme debe ser http o https")` is returned.
- **Scenario D (https URL):** Given the prompt is active, When user enters `"https://sync.example.com:443"`, Then `Uri.TryCreate` succeeds with scheme `https` and the prompt accepts it.

### FR-003 — Server reachability probe (non-blocking)
After the URL is accepted, the installer optionally probes the server and warns if unreachable, but does **not** block installation.

- **Scenario A (reachable server):** Given the user entered `"http://192.168.0.178:7437"`, When `ProbeServerHealth(url)` sends `GET {url}/health` with 3-second timeout and receives HTTP 200, Then no warning is displayed and installation proceeds.
- **Scenario B (unreachable server):** Given the user entered `"http://10.0.0.99:7437"`, When `ProbeServerHealth` times out or receives non-2xx, Then `AnsiConsole.MarkupLine` emits `"⚠ http://10.0.0.99:7437 no responde a GET /health"` with the note `"La instalación continúa; el sync reintentará en cada ciclo."` and installation proceeds.
- **Scenario C (probe timeout):** Given the URL resolves but `/health` hangs, When `HttpClient.Timeout = 3s`, Then the probe returns `false` after 3 seconds and the warning is displayed — no hang beyond 3 seconds.

### FR-004 — Headless mode URL resolution
In headless mode (`--yes` or non-interactive console), the installer must resolve the sync URL without prompting.

- **Scenario A (--server-url flag):** Given `flowforge install --yes --server-url http://10.0.0.5:7437`, When `DetectSyncMode()` returns `"sync"` from env/config and `isHeadless` is `true`, Then `engramSyncUrl` is set to `"http://10.0.0.5:7437"` (flag takes priority).
- **Scenario B (ENGRAM_SERVER_URL env var):** Given `ENGRAM_SERVER_URL=http://10.0.0.5:7437` in environment and no `--server-url` flag, When headless install runs, Then `engramSyncUrl` is read from `Environment.GetEnvironmentVariable("ENGRAM_SERVER_URL")`.
- **Scenario C (persisted config):** Given `~/.engram/config.json` has `sync.remote_url` and neither flag nor env var is set, When headless install runs, Then `engramSyncUrl` is read from `existingUrl` (persisted config).
- **Scenario D (no URL anywhere, sync mode):** Given `ENGRAM_SERVER_URL` is unset, `--server-url` not passed, `DetectSyncMode` returns `"sync"` (maybe from a remote manifest), and headless mode, When the installer resolves `engramSyncUrl` as null/empty, Then it emits `"Error: sync mode requires --server-url, ENGRAM_SERVER_URL env var, or a previous install with sync config."` and **returns without installing** (exit code non-zero).

### FR-005 — Headless mode auto-detection from config
`DetectSyncMode()` must check persisted config as a fallback, not just env vars.

- **Scenario A (env var set):** Given `ENGRAM_SERVER_URL=http://...`, When `DetectSyncMode()` is called, Then it returns `"sync"`.
- **Scenario B (persisted config):** Given no env var but `~/.engram/config.json` has `sync.remote_url: "http://...`, When `DetectSyncMode()` is called, Then `ctx.Store.Load()` is read and `sync` mode is returned.
- **Scenario C (nothing set):** Given no env var and no persisted sync config, When `DetectSyncMode()` is called, Then it returns `"local"`.

### FR-006 — SyncConfig persistence
The installer must write `SyncConfig` to `~/.engram/config.json` on every successful install.

- **Scenario A (install with sync):** Given `flowforge install` completed with `engramMode = "sync"` and `engramSyncUrl = "http://10.0.0.5:7437"`, When `ctx.Store.Update(...)` is called, Then `~/.engram/config.json` contains a `sync` section with `mode: "sync"`, `remote_url: "http://10.0.0.5:7437"`, `user` (from ENGRAM_USER or `$USER@local.dev`), `data_dir` (from PathHelper.EngramDir), and `connected_at` (ISO 8601 UTC).
- **Scenario B (install with local):** Given install completed with `engramMode = "local"`, When `ctx.Store.Update(...)` is called for engram, Then sync config is written with `mode: "local"` and empty `remote_url`.
- **Scenario C (no engram component):** Given user deselected engram-dotnet in component selection, When `ctx.Store.Update(...)` runs, Then no `SyncConfig` is written (installEngram is false).

### FR-007 — EngramModule MCP wiring with sync URL
When `mode=sync`, the MCP config for each IDE must include `ENGRAM_SERVER_URL` in the environment block.

- **Scenario A (Cursor MCP):** Given sync mode with URL `"http://10.0.0.5:7437"`, When `ConfigureMcp(engramMode, engramSyncUrl)` runs with `syncEnabled=true`, Then `WriteMcpJson` writes `env["ENGRAM_SERVER_URL"] = "http://10.0.0.5:7437"` into the Cursor `mcp.json`.
- **Scenario B (OpenCode MCP merge):** Given sync mode with URL, When `MergeOpenCodeMcp` runs, Then `system["environment"]["ENGRAM_SERVER_URL"]` is set in the OpenCode config, alongside `ENGRAM_SYNC_ENABLED=true`.
- **Scenario C (local mode, no URL):** Given `engramMode = "local"`, When `ConfigureMcp` runs, Then no `ENGRAM_SERVER_URL` key appears in any MCP config, and `ENGRAM_SYNC_ENABLED=false`.

### FR-008 — Hardcoded IP removal
No source file in `src/FlowForge.Installer/` may contain the string `192.168.0.178`.

- **Scenario A (InstallCommand.cs):** Given the source file at HEAD, When searched for `192.168.0.178`, Then the string is not found.
- **Scenario B (EngramModule.cs):** Given the source file at HEAD, When searched for `192.168.0.178`, Then the string is not found.
- **Scenario C (no silent default):** Given sync mode is active and no URL is available, When `EngramModule.InstallAsync` is invoked, Then it throws `InvalidOperationException("ENGRAM_SERVER_URL is required for sync mode.")` — it does **not** fall back to any hardcoded IP.

### FR-009 — Re-install preserves existing sync config
Running `flowforge install` again must not wipe a previously persisted sync URL.

- **Scenario A (re-install, same URL accepted):** Given `~/.engram/config.json` has `sync.remote_url: "http://10.0.0.5:7437"`, When `flowforge install` runs interactively, sync is chosen, and user presses Enter on the pre-filled URL, Then the config retains `remote_url: "http://10.0.0.5:7437"`.
- **Scenario B (re-install, different URL entered):** Given persisted URL `"http://10.0.0.5:7437"`, When user enters `"http://10.0.0.10:7437"` in the prompt, Then `~/.engram/config.json` is updated to `remote_url: "http://10.0.0.10:7437"` and `connected_at` is refreshed.

---

## 3. Non-functional requirements (NFR)

- **NFR-001 (AOT compatibility):** No new reflection-based code. `Uri.TryCreate` is AOT-compatible. `HttpClient` usage is AOT-compatible. `SyncConfig` is registered in `InstallerJsonContext` source generator. All existing constraints from `stack-installer` NFR-001 through NFR-005 continue to apply.
- **NFR-002 (UX — Spanish copy):** All user-facing prompt strings are in Spanish, matching the existing installer locale. `TextPrompt` title and validation messages are localized. Error messages in headless mode may be in English (they are consumed by scripts/CI, not by end users directly).
- **NFR-003 (Timeout ceiling):** `ProbeServerHealth` uses `HttpClient.Timeout = 3s`. The probe must not delay the overall install by more than 5 seconds (3s timeout + 2s overhead). If the probe hangs past the timeout, the `catch` block returns `false` immediately.
- **NFR-004 (Config atomicity):** `ctx.Store.Update(...)` must write atomically (write to `.tmp`, rename). This is the existing pattern in `ConfigStore` — no new implementation needed.

### Security Requirements (STRIDE)

| Threat | Question | Mitigation | Impact |
|--------|----------|------------|--------|
| **S**poofing | Can a user be tricked into entering a malicious server URL? | No auth today; any URL is accepted if it parses as http/https. The probe warns if unreachable but doesn't enforce identity. This is a known limitation of LAN-only deployments (see ADR-010 §Out of scope). | Low — LAN-only threat model; no secrets sent to the URL. |
| **T**ampering | Can the persisted `~/.engram/config.json` be modified by another process between the installer and engram startup? | Config file is user-writable (`~/.engram/`); no integrity check beyond filesystem permissions. Future: ADR-009 `flowforge sync status` detects drift between config and running processes. | Low — same-user threat model; no escalation possible. |
| **R**epudiation | Can a user deny installing sync mode? | Install log (`~/.engram/install.log`) records mode and resolved URL. `connected_at` timestamp is written to `sync.connected_at` on each install. | Covered by existing logging. |
| **I**nformation Disclosure | Can the sync URL leak in error messages or logs? | The URL is logged to `install.log` at INFO level (expected — user needs to see what was configured). No credentials are embedded in the URL (bearer tokens are out of scope per ADR-010). Stack traces are suppressed unless `--verbose`. | Low — URL is not secret; no credentials in transit. |
| **D**enial of Service | Can the reachability probe be abused to exhaust resources? | `HttpClient` instantiated per-probe with 3-second timeout; no connection pooling or retry. Single request — if the server responds slowly it times out; if it returns huge payload it's discarded after status code check. | No amplification possible. |
| **E**levation of Privilege | Can the installer be tricked into writing `ENGRAM_SERVER_URL` to a privileged location? | The MCP configs written (`~/.cursor/`, `~/.config/opencode/`) are all under the user's home directory; no sudo/root path involved. `~/.engram/config.json` is also user-scoped. The sudo warning (lines 258-263 in InstallCommand.cs) is informational only. | No privilege boundary crossed. |
| **None (secrets in source)** | Could hardcoded IPs or URLs leak internal network info? | FR-008 guarantees no hardcoded IP. Source-level tests assert `192.168.0.178` is absent from both `InstallCommand.cs` and `EngramModule.cs`. | Verified by test suite. |

---

## 4. Developer manual tests (required — mark [x] before /flow-close)

| ID | Case / flow | Steps (summary) | Expected result | [x] |
|----|-------------|-----------------|-----------------|-----|
| PM-1 | Interactive install with sync, reachable URL | 1. Start `flowforge install` interactively<br>2. Select "engram-dotnet" + "FlowForge"<br>3. Select "Offline-first sync"<br>4. Enter `http://localhost:7437` (engram serve running)<br>5. Confirm install | URL accepted with no warning; `~/.engram/config.json` has `sync.remote_url: "http://localhost:7437"`; `engram serve` uses it after reinstall | [ ] |
| PM-2 | Interactive install with sync, unreachable URL | 1. Same as PM-1 but enter `http://192.168.99.99:7437` (no server)<br>2. Observe probe warning | Yellow warning `"⚠ ... no responde a GET /health"`; installation continues; config has the unreachable URL | [ ] |
| PM-3 | Headless install with --server-url | 1. Run `flowforge install --yes --server-url http://10.0.0.5:7437`<br>2. Check `~/.engram/config.json` | Sync section written with correct URL; no interactive prompts appear | [ ] |
| PM-4 | Regression: local mode does not prompt | 1. Run `flowforge install` interactively<br>2. Select "Local (SQLite, sin sync)"<br>3. Complete install | No `ENGRAM_SERVER_URL` in MCP configs; `ENGRAM_SYNC_ENABLED=false`; no URL prompt appears | [ ] |
| PM-5 | Re-install preserves URL | 1. First install with sync URL `http://10.0.0.5:7437`<br>2. Run `flowforge install` again, select sync<br>3. Observe prompt default + accept | Prompt pre-filled with `http://10.0.0.5:7437`; config retains same URL | [ ] |

---

## 5. Open questions — none blocking

There are no `[BLOCKER]` questions — the code is already implemented and design is settled by ADR-010. Two `[FOLLOW-UP]` items are tracked in ADR-010 §Consequences > Negative / accepted:

- OQ-1 (`[FOLLOW-UP]`): Auth tokens — when the relay server adds authentication (ENG-405), `flowforge sync connect` (ADR-009) will need a `--token` flag. Does not affect the installer URL prompt.
- OQ-2 (`[FOLLOW-UP]`): TLS certificate validation — when public relays ship, the installer should validate certs by default. Today's LAN-only deployments don't need this.

---

## Acceptance summary

| Tier | Deliverables | Status |
|------|-------------|--------|
| **Code** | `InstallCommand.cs` URL prompt + health probe + headless resolution | ✅ Implemented |
| **Code** | `EngramModule.cs` no hardcoded IP, throws on missing URL | ✅ Implemented |
| **Code** | `InstallerConfig.cs` `SyncConfig` model | ✅ Implemented |
| **Tests** | 7 source-level tests in `InstallerAsksForSyncUrlTests.cs` | ✅ Present |
| **Docs** | ADR-010 status: Proposed → Accepted | 🔲 Pending |
| **Docs** | POST-INSTALL.md §3 manual workaround removed | 🔲 Pending |
| **Verify** | forge-verify audit against this spec | 🔲 Pending |

---

## Memory Signal

- type: decision
- significance: low
- summary: "ADR-010 implemented — installer now prompts for ENGRAM_SERVER_URL in sync mode with URL validation and /health probe; hardcoded 192.168.0.178 removed; SyncConfig persisted to ~/.engram/config.json."
