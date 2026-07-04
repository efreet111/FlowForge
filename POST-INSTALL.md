# Post-Install Checklist — FlowForge + engram-dotnet

> **Run this after `flowforge install` completes.** Each step verifies that
> the installation is actually working. If you skipped it the first time and
> something is broken now, run it and see which step fails.

---

## 1. Verify the binary is the latest version

```bash
engram --version
```

**Expected output:** `1.0.0+<40-char commit hash>` (e.g. `1.0.0+abc123…`).

**If you see an older commit hash** (or you installed before the sync recovery
fixes), update manually:

```bash
# Linux/macOS
curl -fsSL https://github.com/efreet111/engram-dotnet/releases/latest/download/engram-linux-x64 \
  -o ~/.local/bin/engram
chmod +x ~/.local/bin/engram

# Windows (PowerShell)
Invoke-WebRequest -Uri "https://github.com/efreet111/engram-dotnet/releases/latest/download/engram-win-x64.exe" `
  -OutFile "$env:LOCALAPPDATA\Programs\FlowForge\engram.exe"
```

Verify SHA-256 if you want to be paranoid:

```bash
EXPECTED=$(curl -fsSL https://github.com/efreet111/engram-dotnet/releases/latest/download/engram-linux-x64.sha256 | awk '{print $1}')
ACTUAL=$(sha256sum ~/.local/bin/engram | awk '{print $1}')
[[ "$EXPECTED" == "$ACTUAL" ]] && echo "OK" || echo "MISMATCH"
```

> **ENG-454 (planned)**: `flowforge update` will automate this.

---

## 2. Decide your sync mode

| Mode | When to use | Setup |
|------|-------------|-------|
| **Local-only** | Single machine, no team sync needed | Do nothing — defaults are correct |
| **Offline-first sync** | Team with shared engram server (PostgreSQL relay) | Configure `ENGRAM_SERVER_URL` (step 3) |

**Default after install is local-only.** If you chose `mode=sync` during
`flowforge install` but did not configure a remote URL, the server will print
a warning at startup (see step 4).

---

## 3. Configure the sync target (sync mode only)

If you have a team relay server (e.g. a TrueNAS machine running engram with
PostgreSQL), set its URL:

```bash
# Linux/macOS — add to ~/.bashrc to persist
echo 'export ENGRAM_SERVER_URL="http://192.168.x.x:7437"' >> ~/.bashrc
source ~/.bashrc

# Windows — set as user environment variable
[System.Environment]::SetEnvironmentVariable(
  "ENGRAM_SERVER_URL", "http://192.168.x.x:7437", "User"
)
# Then restart your shell / IDE
```

> **ENG-453 (planned)**: `flowforge install --mode=sync` will prompt for the
> remote URL and persist it in `~/.engram/config.json`. Until then, set the
> env var manually as shown.

---

## 4. Smoke test the installation

```bash
# Start the server in the background
engram serve &

# Wait a moment, then check sync status
sleep 5
engram sync status
```

**Expected output (local-only, no remote configured):**

```
[engram] warning: SyncManager disabled — ENGRAM_SERVER_URL points to this server itself
[engram]   configured: http://localhost:7437
[engram]   this server: http://0.0.0.0:7437
[engram]   Set ENGRAM_SERVER_URL to a remote sync server, or ENGRAM_SYNC_ENABLED=false to silence this warning.

Sync status (mutation-based):
  Enabled:              True
  Phase:                idle
  Health:               healthy
  Pending push:         0
  Total pushed:         0
  Total pulled:         0
```

The `[engram] warning:` line is **expected and informational** in local-only
mode. It means the SyncManager correctly detected it would be talking to
itself and disabled itself. See [ADR-008](docs/decisions/ADR-008-sync-self-loop-detection.md).

**If you configured `ENGRAM_SERVER_URL` (sync mode):**

```
Sync status (mutation-based):
  Enabled:              True
  Phase:                healthy
  Health:               healthy
  Consecutive failures: 0
  Pending push:         N
  Total pushed:         N
  Total pulled:         N
```

No warning line — the SyncManager is reaching the remote server.

**Stop the test server:**

```bash
pkill -f "engram serve"
```

---

## 5. Verify MCP servers are wired

Open your IDE and check that the **engram** MCP server is registered.

| IDE | Where to check |
|-----|----------------|
| **Cursor** | Settings → MCP → should list `engram` |
| **OpenCode** | `~/.config/opencode/opencode.json` → `mcp.engram` |
| **Claude Desktop** | `claude_desktop_config.json` → `mcpServers.engram` |
| **VS Code** | `.vscode/mcp.json` → `servers.engram` |

If absent, re-run `flowforge install` and select your IDE.

---

## 6. Memory test

In your IDE, send a message that triggers a memory save. Example:

```
Save this memory: my favorite color is purple.
```

Then verify the memory was stored:

```bash
sqlite3 ~/.engram/engram.db \
  "SELECT id, type, project, substr(content, 1, 80) FROM observations ORDER BY id DESC LIMIT 3;"
```

Expected: a row with the saved content appears.

---

## Common post-install problems

| Symptom | Likely cause | Fix |
|---------|--------------|-----|
| `DllNotFoundException` at startup | Missing `libe_sqlite3.so` | Install libsqlite3 system package |
| `error: No se pudo conectar al servidor` from `engram sync status` | `engram serve` not running, or wrong port | Run `engram serve` or set `ENGRAM_SERVER_URL` |
| `Mutation transport failed with status 501` every 30ms in logs | Self-loop (no `ENGRAM_SERVER_URL` set) — **fixed in v0.4.0+**, warning should appear instead | Update binary (step 1) or set `ENGRAM_SYNC_ENABLED=false` |
| Stale binary after FlowForge update | FlowForge does not auto-update engram-dotnet (ENG-454) | Manual update via step 1, or wait for `flowforge update` |
| Memory not visible from team | Server not running, or different `ENGRAM_USER` | Check `engram doctor` and verify `ENGRAM_USER` matches |

---

## Version compatibility

| FlowForge | Required engram-dotnet | Reason |
|-----------|------------------------|--------|
| `0.1.0-alpha.6` | `>=0.3.0` | Original constraint — but lacks ENG-451/452 sync recovery |
| **0.1.0-alpha.6 (recommended `>=0.4.0`)** | `>=0.4.0` | Sync recovery for orphaned pulled mutations + self-loop detection |

**Why the version constraint matters:**
- **ENG-451 (BUG-1)**: without it, mutations pulled from the server get stuck
  with `acked_at=NULL` if sync is interrupted → silent data loss
- **ENG-452**: without it, `engram serve` without `ENGRAM_SERVER_URL` spams
  `Mutation transport 501` every 30ms with no clear remediation
- Both are in `engram-dotnet` `>=0.4.0`. Any `flowforge install` that fetches
  an older release will have broken sync.

Until **ENG-453** ships (FlowForge persists `ENGRAM_SERVER_URL` in
`mode=sync` install), users who want sync must set the env var manually as
shown in step 3.