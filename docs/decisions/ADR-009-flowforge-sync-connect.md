# ADR-009 — `flowforge sync connect` command

> **Status**: Proposed
> **Date**: 2026-07-01
> **Feature**: `flowforge sync connect` (new command)
> **Deciders**: Engineering (FlowForge methodology team)
> **Links**: [ADR-006 `ENGRAM_SERVER_URL` not persisted](ADR-006-opencode-mcp-config.md) ·
> [engram-dotnet ADR-008 self-loop detection](https://github.com/efreet111/engram-dotnet/blob/main/docs/architecture/adr/ADR-008-sync-self-loop-detection.md) ·
> [FlowForge ENG-005 stack installer] ·
> [POST-INSTALL.md](../../POST-INSTALL.md) (the manual checklist this replaces)

---

## Context

The current path from "I have engram installed" to "my memories sync to the team server" requires the user to:

1. Know that `ENGRAM_SERVER_URL` is a thing
2. Know that `ENGRAM_SYNC_ENABLED` must be `"true"` (not the default `"false"`)
3. Edit the MCP config JSON for every IDE they use (OpenCode, Cursor, VS Code, Claude Desktop) — each with different schemas
4. Restart every active `engram mcp` process (the old one cached the env vars)
5. Verify the SyncManager actually connects (`engram sync status` from a separate process)
6. If something is wrong, read logs to figure out which step failed

Documented in [POST-INSTALL.md](../../POST-INSTALL.md) §3 (sync target setup). Today, this is the standard "offline-first" setup — and it requires the user to know things they shouldn't have to know.

We discovered this concretely on 2026-07-01 while verifying ENG-451/452 fixes: the user's `engram mcp` was running with stale env vars (`ENGRAM_SYNC_ENABLED=false`, no `ENGRAM_SERVER_URL`) — config that FlowForge had installed back on 2026-06-29 but never updated to actually enable sync. After 4 rebuilds and 8 commits, we still couldn't get sync running without manual config editing.

The user feedback was direct: *"la idea es preguntarnos como mejorar este proceso... para que podamos hacer este paso transparente para el usuario. levantamos el servidor, creamos el mcp y lo levantamos ambos?"*

---

## Decision

Add a `flowforge sync connect <server-url>` command that takes the user's intent ("I want my memory to sync to *this* server") and runs every step needed to make it happen:

```
$ flowforge sync connect http://192.168.0.178:7437

✓ Server reachable (1.1.0, postgres)
✓ ENGRAM_SERVER_URL persisted to ~/.engram/config.json
✓ ENGRAM_SYNC_ENABLED=true (was false)
✓ OpenCode MCP config updated
✓ Cursor MCP config updated
✓ Restarted 2 engram mcp processes (now using new env)
✓ SyncManager connected: last_pulled_seq=1124, healthy
```

The command is **idempotent**: running it twice with the same URL is a no-op. Running it with a different URL migrates cleanly (updates env vars, restarts processes).

### Why a FlowForge command, not an `engram` command

FlowForge already has:
- The IDE registry (`ide/cursor/`, `ide/opencode/`, `ide/claude/`, `ide/vscode/`) with each IDE's config format
- The post-install wiring code (`EngramModule`, `InstallCommand`)
- The `manifest.yaml` mechanism for version-aware components

Adding `flowforge sync connect` keeps IDE-specific config knowledge in the tool that's already responsible for IDE integration. The `engram` binary stays focused on the runtime (server, MCP, sync cycle).

### Why not transparent (Option C from the proposal)

We considered making `engram mcp` auto-discover the server URL from `~/.engram/config.json` when `ENGRAM_SERVER_URL` is unset. This would be invisible to the user. **Rejected** because:

1. **Debuggability**: when sync silently uses a stale URL from 6 months ago, the user has no way to know.
2. **Multi-IDE confusion**: a developer with both OpenCode and Cursor pointed at different servers (work + personal) would have hidden behavior — different env vars per IDE, different sync targets.
3. **Security**: silently connecting to any configured URL without user consent is surprising behavior for a tool that ships to teams.

Explicit is better than implicit. The `connect` command is the explicit path; it persists the user's choice so subsequent sessions use it without re-prompting.

---

## Decision drivers

- **One command, full setup**: the user should not have to read the POST-INSTALL.md checklist manually.
- **Idempotent**: running twice is safe; changing URL is safe.
- **Visible**: the command shows what it did (which IDEs were updated, which processes restarted).
- **Reversible**: `flowforge sync disconnect` reverts to local-only mode (clears env vars, doesn't delete the URL — user might want to reconnect).
- **Fail loudly**: if the server URL is unreachable, do NOT update configs. Tell the user.
- **No new process ownership**: FlowForge does not become a daemon. It modifies configs and signals existing processes via SIGHUP or process kill (existing pattern).

---

## Command surface

```
flowforge sync connect <server-url>     [flags]
  --user <name>                       Override ENGRAM_USER (default: existing value)
  --data-dir <path>                   Override ENGRAM_DATA_DIR
  --yes                               Skip confirmation prompts
  --dry-run                           Show what would change, don't apply

flowforge sync disconnect              [flags]
  --yes                               Skip confirmation prompts
  --dry-run

flowforge sync status                  Show current sync configuration and status
```

### `connect` algorithm

```
1. Validate URL: scheme must be http/https; parseable.
2. Probe: GET <url>/health with 3s timeout.
   - If 200: parse version + backend from response. Show user.
   - If unreachable: ERROR. Do not modify configs. Show what was tried.
3. Read current state:
   - ~/.engram/config.json → existing sync.remote_url, components.engram_dotnet
   - For each IDE in components.flowforge.ides:
       - Open its MCP config file (per-IDE format)
       - Read current ENGRAM_SERVER_URL / ENGRAM_SYNC_ENABLED values
4. Plan changes (show user unless --yes):
   - "Will update: ~/.engram/config.json (sync.remote_url)"
   - "Will update: OpenCode config (set ENGRAM_SERVER_URL=..., ENGRAM_SYNC_ENABLED=true)"
   - "Will update: Cursor config (...)"
   - "Will restart: 2 engram mcp processes"
   - "Continue? [Y/n]"
5. Apply:
   a. Write ~/.engram/config.json (atomic: write to .tmp, rename)
   b. For each IDE: update env block (preserve all other fields)
   c. Find running engram mcp processes (pidof or ps); send SIGHUP if upstream supports config reload, otherwise kill and let IDE restart
6. Verify:
   - Wait 3s
   - GET <url>/sync/status from server (not client) to confirm enrollment works
   - Show: "Sync connected. Test with: <your IDE's memory tool>"
```

### `disconnect` algorithm

```
1. Plan: clear ENGRAM_SERVER_URL and ENGRAM_SYNC_ENABLED in:
   - ~/.engram/config.json
   - Each IDE's MCP config
2. Apply (same atomic-write pattern as connect)
3. Restart MCP processes
4. Show: "Sync disabled. Memories are local-only now."
```

The URL is preserved in `~/.engram/config.json.sync.remote_url` for easy reconnection. To remove entirely, the user runs `flowforge sync disconnect --purge`.

### `status` algorithm

```
1. Read ~/.engram/config.json → show configured sync.remote_url, ENGRAM_SYNC_ENABLED
2. Detect running engram mcp processes → show their effective env (read /proc/<pid>/environ)
3. Probe each detected ENGRAM_SERVER_URL → show /health summary
4. Compare: do MCP processes' env match the config? Warn if drift detected.
```

This catches the exact bug class that hit us: config says one thing, running MCPs use another.

---

## Persisted schema

In `~/.engram/config.json`:

```json
{
  "version": "0.1.0",
  "channel": "stable",
  "auto_update": false,
  "flowdoc": { "enabled": true },
  "sync": {
    "mode": "sync",
    "remote_url": "http://192.168.0.178:7437",
    "user": "victor@local.dev",
    "data_dir": "/home/victor/.engram",
    "connected_at": "2026-07-01T22:30:00Z"
  },
  "components": { ... }
}
```

This adds `sync` as a first-class section (currently the installer writes only `components` and `flowdoc`). The installer must be updated to **read** this section too — so a fresh `flowforge install` honors a previously persisted sync target instead of writing empty config.

---

## Consequences

### Positive

- User goes from "I want sync" to "sync works" with one command, one confirmation.
- Drift detection: `status` command catches the bug class that hit us on 2026-07-01 (config says X, running MCP uses Y).
- Reusable for future sync-related commands: `flowforge sync pause` for temporary disable without losing config; `flowforge sync switch <new-url>` for migrations.
- Aligns with how modern tools (gh, doctl, gh auth login) work: one command, persistent config, explicit status.
- Becomes the canonical place to document the offline-first sync flow in FlowForge, replacing the manual POST-INSTALL.md steps.

### Negative / accepted

- FlowForge takes on more responsibility (sync lifecycle). Mitigated by being a thin wrapper around config edits, not a new daemon.
- Requires careful atomic-write semantics for `~/.engram/config.json` (write to `.tmp`, rename) — partial writes would break FlowForge's own state machine. Accepted: this pattern is already used elsewhere in the codebase.
- For users with non-standard IDE setups (custom MCP config path), the command may not detect the IDE. Mitigation: clear error message saying "couldn't find MCP config for IDE X" with instructions to set env vars manually.

### Out of scope

- **Auto-update of `engram` binary** (covered by ENG-454, separate ADR).
- **Auth tokens** for the relay server. Today's relay has no auth header requirement (`X-Engram-User` is informational only). When auth is added, `connect` will need a `--token` flag.
- **TLS / HTTPS**. Today's deployments are LAN-only. When public relays ship, `connect` should validate certs by default.

---

## Implementation sketch

New file: `src/FlowForge.Installer/Commands/SyncCommand.cs`

```csharp
public sealed class SyncCommand
{
    public int Connect(string url, SyncConnectOptions opts) { ... }
    public int Disconnect(SyncDisconnectOptions opts) { ... }
    public int Status() { ... }

    // Shared helpers
    private ConfigPlan PlanChanges(ConfigFile config, string newUrl) { ... }
    private void AtomicWrite(string path, string content) { ... }
    private List<Process> FindEngramProcesses() { ... }
    private bool ProbeServer(string url, out ServerHealth health) { ... }
}

public sealed record ConfigPlan(
    IReadOnlyList<string> ConfigFilesToUpdate,
    IReadOnlyList<string> IdeConfigsToUpdate,
    IReadOnlyList<int> ProcessesToRestart);

public sealed record ServerHealth(string Version, string Backend);
```

New file: `src/FlowForge.Installer/Ide/McpConfigUpdater.cs`

Per-IDE config writers. Each IDE has its own JSON schema:
- OpenCode: `~/.config/opencode/opencode.json` → `mcp.<name>.environment`
- Cursor: `~/.cursor/mcp.json` → `mcpServers.<name>.env`
- VS Code: `~/.vscode/mcp.json` → `servers.<name>.env`
- Claude Desktop: `claude_desktop_config.json` → `mcpServers.<name>.env`

These writers are extracted from the existing post-install wiring (`EngramModule.cs`). The `SyncCommand` calls into them.

---

## Test plan

Unit tests (`tests/FlowForge.Installer.Tests/SyncCommandTests.cs`):

1. `Connect_ReachableServer_PersistsAndUpdatesIdeConfigs`
2. `Connect_UnreachableServer_LeavesConfigsUntouched`
3. `Connect_Idempotent_SameUrl_NoChanges`
4. `Connect_DifferentUrl_UpdatesExisting`
5. `Disconnect_ClearsSyncEnvVars`
6. `Disconnect_PurgeFlag_RemovesRemoteUrl`
7. `Status_DetectsDriftBetweenConfigAndRunningProcess`

Integration test:
- Spin up a test HTTP server (HttpListener on random port)
- Write a fake `~/.engram/config.json`
- Run `Connect` against the test server
- Assert config + IDE configs + dry-run output

Manual smoke test:
```bash
# From a clean machine:
curl -fsSL https://github.com/efreet111/FlowForge/install.sh | bash
flowforge sync connect http://192.168.0.178:7437
# Open IDE, send a memory, observe it lands on the relay.
```

---

## Rollout

1. Land ADR-009 (this doc) as **Accepted**.
2. Implement `SyncCommand` + `McpConfigUpdater` in a feature branch.
3. Verify with forge-verify (cycle 1 expected to find at least 2 things; iterate).
4. Tag a FlowForge alpha release.
5. Update POST-INSTALL.md to point at the new command (`flowforge sync connect` replaces §3 of the manual checklist).
6. Add `flowforge sync connect` to QUICKSTART.md §3 (first command after install).
7. Once shipped, mark ENG-453 (manual env var in POST-INSTALL.md) as Done.