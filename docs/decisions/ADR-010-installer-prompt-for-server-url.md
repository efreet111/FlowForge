# ADR-010 — Installer must prompt for `ENGRAM_SERVER_URL` in `mode=sync`

> **Status**: Proposed
> **Date**: 2026-07-01
> **Feature**: Stack installer / engram-dotnet sync setup
> **Deciders**: Engineering (FlowForge methodology team)
> **Links**: [ADR-006 OpenCode MCP config](ADR-006-opencode-mcp-config.md) ·
> [ADR-009 `flowforge sync connect`](ADR-009-flowforge-sync-connect.md) ·
> [engram-dotnet ADR-008 self-loop](https://github.com/efreet111/engram-dotnet/blob/main/docs/architecture/adr/ADR-008-sync-self-loop-detection.md) ·
> [POST-INSTALL.md](../../POST-INSTALL.md)

---

## Context

The FlowForge installer (`InstallCommand.cs` + `EngramModule.cs`) lets the user
choose between **Local** (SQLite, no sync) and **Offline-first sync** (SQLite +
relay server). When the user picks sync, the wizard silently hardcodes
`ENGRAM_SERVER_URL=http://192.168.0.178:7437` as a fallback — **without asking**
and **without validating**. The user's actual team relay server may be on a
completely different IP, host, port, scheme, or path.

This was discovered concretely on 2026-07-01 while verifying ENG-451/452 fixes:

```
$ sqlite3 ~/.engram/engram.db "SELECT * FROM sync_state;"
cloud|healthy|0|0|1124|0||victor-laptop-9030|2026-07-04 02:19:20|non-enrolled-pending: 1 projects not enrolled|...
```

The install completed in March with `mode=sync` selected, but the relay server
the user wanted (their TrueNAS box at `192.168.0.178`) only got configured
through manual `opencode.json` editing after the install. The default
`192.168.0.178` was actually correct in this case — but only by coincidence.

---

## Three concrete bugs

### Bug 1 — `InstallCommand.cs:113-121`: choosing sync does not prompt for URL

```csharp
engramMode = AnsiConsole.Prompt(
    new SelectionPrompt<string>()
        .Title("[bold]Modo de uso de engram-dotnet:[/]")
        .AddChoices([
            "Local (SQLite, sin sync)",
            "Offline-first sync (SQLite + servidor)",
        ]));
engramMode = engramMode.StartsWith("Local") ? "local" : "sync";
```

If the user picks "Offline-first sync", the wizard knows the user wants a
remote server but **never asks which one**. The URL gets silently defaulted
elsewhere.

### Bug 2 — `EngramModule.cs:204-205` and `264-265`: hardcoded fallback IP

```csharp
var serverUrl = Environment.GetEnvironmentVariable("ENGRAM_SERVER_URL")
                ?? "http://192.168.0.178:7437";
env["ENGRAM_SERVER_URL"] = serverUrl;
```

The hardcoded `192.168.0.178:7437` is the IP from the spec example. In any real
deployment it's almost certainly wrong. The user's only options to fix it
are: set the env var before running the installer, or hand-edit the generated
MCP config afterward. Neither is acceptable for a "first install" experience.

### Bug 3 — `InstallCommand.cs:201-202`: `DetectSyncMode` also reads env only

```csharp
static string DetectSyncMode()
{
    var serverUrl = Environment.GetEnvironmentVariable("ENGRAM_SERVER_URL");
    return string.IsNullOrWhiteSpace(serverUrl) ? "local" : "sync";
}
```

In headless mode (`flowforge install --yes`), the wizard skips the prompt and
falls back to `DetectSyncMode`, which returns "local" if `ENGRAM_SERVER_URL`
is unset. So the user's flag-based choice is silently overridden.

---

## Decision

Fix all three bugs with one coherent change: **`ENGRAM_SERVER_URL` is a
required input when `mode=sync`, sourced from one of three places in priority
order**:

1. `--server-url <url>` flag (for headless / scripted installs)
2. `ENGRAM_SERVER_URL` environment variable (for CI / Docker)
3. Interactive prompt with **validation** (for interactive installs)

The hardcoded IP is removed entirely. If neither flag nor env is set and the
mode is `sync`, the prompt **must** be answered — there's no silent default.

### New flow in `InstallCommand.cs`

```
[if not headless]
  Mode = Local / Offline-first sync?
  if Mode == "sync":
    url = prompt("Sync server URL?", default=last-used-or-empty)
    if not url: ABORT with clear error
    if not reachable: WARN but continue (allow offline install, sync will retry)
  else:
    url = null
[if headless]
  url = $ENGRAM_SERVER_URL  OR  --server-url  OR  error: "sync requires --server-url or ENGRAM_SERVER_URL"

ctx.Store.Update(cfg => cfg.Sync = new SyncConfig { Mode, Url, User, DataDir })
EngramModule.InstallAsync(mode, url)
```

### New flow in `EngramModule.cs` (line 204-205, 264-265)

```csharp
if (syncEnabled)
{
    if (string.IsNullOrWhiteSpace(serverUrl))
        throw new InvalidOperationException(
            "ENGRAM_SERVER_URL is required for sync mode. " +
            "Set it via --server-url, ENGRAM_SERVER_URL, or interactive prompt.");
    env["ENGRAM_SERVER_URL"] = serverUrl;
}
```

The hardcoded IP is gone. The config store becomes the source of truth.

### Persisted schema in `~/.engram/config.json`

Already proposed in [ADR-009](../../decisions/ADR-009-flowforge-sync-connect.md)
schema, but now also written by the installer at first install:

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

This is now also **read** on subsequent installs (re-runs preserve the user's
choice). For installs that don't yet have a `sync` section, the wizard prompts
as before; for installs that do have one, the prompt is prefilled with the
existing URL.

### URL validation

The interactive prompt accepts any string, but:

1. Format check: must parse as `Uri` with scheme `http` or `https`
2. Reachability check: `GET <url>/health` with 3-second timeout
3. If unreachable: warn and continue (offline install is OK; sync will retry
   on first cycle)

For headless mode, format check only — no reachability probe, since the
network may not be ready during provisioning.

---

## Consequences

### Positive

- Choosing sync mode forces a URL — no more silent `192.168.0.178` default.
- `flowforge install --server-url <url>` works for headless / scripted / CI.
- URL persists to `~/.engram/config.json`; subsequent installs honor it.
- `engram serve` after install reads the URL from config (via `ENGRAM_SERVER_URL`
  injected by the MCP wrapper or directly from the config — see [ADR-009]).

### Negative / accepted

- One more prompt in interactive mode (`Sync server URL?`). Mitigated: the
  default is prefilled from previous install, so re-running is one Enter.
- Headless mode now fails if `--server-url` is missing and mode is sync.
  This is a behavior change but strictly better than silent local install.
- Existing installs with the hardcoded `192.168.0.178` baked into their MCP
  config will continue working if that URL happens to be right; otherwise
  users run `flowforge sync connect <correct-url>` (per ADR-009) to fix.

---

## Out of scope

- **Auth tokens** for the relay server. Today the relay has no auth — when it
  does, `connect` will need a `--token` flag (per ADR-009).
- **TLS / HTTPS validation**. Today's deployments are LAN-only.
- **Auto-update of `ENGRAM_SERVER_URL` on relay IP change**. The user runs
  `flowforge sync connect <new-url>` (ADR-009).

---

## Test plan

Unit tests (`tests/FlowForge.Installer.Tests/InstallCommandTests.cs`):

1. `Install_InteractiveMode_PromptsForUrl_WhenSyncChosen`
2. `Install_InteractiveMode_DoesNotPrompt_WhenLocalChosen`
3. `Install_HeadlessMode_UsesServerUrlFlag`
4. `Install_HeadlessMode_UsesEnvVar`
5. `Install_HeadlessMode_SyncWithoutUrl_AbortsWithClearError`
6. `Install_PreservesExistingSyncConfig_OnRerun`
7. `EngramModule_SyncModeWithoutUrl_ThrowsClearException`

Manual smoke test:

```bash
# From a clean machine:
curl -fsSL https://github.com/efreet111/FlowForge/raw/main/install/install.sh | bash
# Wizard now asks for URL when sync chosen.
# MCP config gets the entered URL, not the hardcoded one.
```

---

## Rollout

1. Land ADR-010 as Accepted.
2. Implement in feature branch off `main`.
3. Verify with forge-verify (cycle 1 expected to find at least 2 things).
4. Tag a new FlowForge alpha.
5. Update POST-INSTALL.md §3 to remove the "set env var manually" step (now
   done by the installer).
6. Once shipped, ENG-453 closes.