# ADR-013 — Engram MCP must not depend on `ANTHROPIC_API_KEY` for basic operation

> **Status**: Proposed  
> **Date**: 2026-07-19  
> **Feature**: Stack installer / engram-dotnet release pin  
> **Deciders**: Engineering (FlowForge + engram-dotnet)  
> **Links**: [ADR-002 runtime manifest](../architecture/adr/ADR-002-runtime-manifest-for-compatibility.md) ·
> [ADR-005 installer headless + MCP env](ADR-005-installer-headless-native-libs.md) ·
> [ADR-010 server URL prompt](ADR-010-installer-prompt-for-server-url.md) ·
> [engram-dotnet ENG-456](https://github.com/efreet111/engram-dotnet/blob/main/docs/BACKLOG.md) ·
> [NS-09 backlog](../backlog/NS-09-engram-stale-release-mcp-anthropic.md) ·
> [Failure handoff](../../.ai-work/engram-mcp-anthropic-false-dependency/failure-report.md)

---

## Context

On 2026-07-19 we verified the Cursor MCP `user-engram` against a healthy team relay
(`http://192.168.0.178:7437`, Postgres, `/health` OK) and a local SQLite journal.

Findings:

| Layer | Result |
|-------|--------|
| Relay HTTP `/health` | OK (`backend=postgres`) |
| TrueNAS git clone | `712242a` (`feat(ENG-459)…`) — includes ENG-456 `NoOpVerifier` |
| Local FlowForge repo | Same `712242a` |
| Installed `engram.exe` (FlowForge path) | `1.0.0+72eb656…` (`chore: release v1.3.0`) |
| MCP `tools/call` (`mem_stats`, `mem_doctor`, …) | **Fail**: `ANTHROPIC_API_KEY is not set` |

Root cause chain:

1. `EngramTools` receives `IVerifier` via DI.
2. Pre-ENG-456 builds constructed `LlmVerifier` eagerly → ctor throws if
   `ANTHROPIC_API_KEY` is missing → **every** MCP tool fails.
3. ENG-456 (`5764ce1`) fixed this with a factory: missing key → `NoOpVerifier`;
   only `mem_verify_artifact` returns structured `api_key_missing`.
4. That fix landed **after** GitHub release tag `v1.3.0` / commit `72eb656`.
5. FlowForge installer downloads “latest release with assets” from
   `efreet111/engram-dotnet` and writes MCP env with only:
   `ENGRAM_DATA_DIR`, `ENGRAM_USER`, `ENGRAM_SYNC_ENABLED`, optional
   `ENGRAM_SERVER_URL` — **correct by design** (wizard never asked for Anthropic).
6. Users therefore get a **stale binary** that contradicts the documented basic
   setup and the current `main` of engram-dotnet / TrueNAS clone.

`GET /health` still returns hardcoded `"version":"1.1.0"` in engram-dotnet
(`EngramServer.cs`) — **do not** use it to compare releases. Use
`engram --version` / `engram version` (InformationalVersion / commit) instead.

This is a **FlowForge packaging / release-pin** problem as much as an
engram-dotnet release problem: the installer is the distribution path that
put `72eb656` on developer machines.

---

## Decision drivers

- Basic MCP (save/search/stats/doctor/sync) must work after `flowforge install`
  with **only** the wizard inputs (user string, data dir, optional server URL).
- `ANTHROPIC_API_KEY` is optional and scoped to `mem_verify_artifact` only
  (see engram-dotnet `docs/VERIFICATION.md`).
- FlowForge must **never** inject Anthropic secrets into generated `mcp.json`
  as a workaround for a stale binary.
- Compatibility rules must be updatable without recompiling the AOT installer
  (ADR-002 `manifest.yaml`).
- Post-install verification should catch “binary too old for MCP” before the
  user hits opaque `An error occurred invoking 'mem_*'` in the IDE.

---

## Options considered

### Option A — Document “set a dummy `ANTHROPIC_API_KEY`”

**Pros**: Immediate unblock for current installs.  
**Cons**: Lies about product dependencies; pollutes MCP env; does not fix
updates for new users.  
**Rejected because**: Contradicts SETUP-WIZARD / MCP-CONFIG and trains bad ops.

### Option B — Wait for users to rebuild from source / copy TrueNAS binary

**Pros**: No FlowForge change.  
**Cons**: Breaks the installer value proposition; drift between clone and
`%LocalAppData%\Programs\FlowForge\engram.exe`.  
**Rejected because**: Recurring support failure.

### Option C — Ship a new engram-dotnet release (≥ ENG-456) + tighten FlowForge pin (chosen)

**Pros**: Aligns GitHub assets with `main`; installer/`flowforge update` heals
machines; manifest can enforce minimum.  
**Cons**: Requires coordinated release in engram-dotnet + manifest bump.  
**Accepted**.

---

## Decision

1. **Product contract (FlowForge + engram)**  
   Generated MCP config **must not** include `ANTHROPIC_API_KEY`. Absence of
   that variable is a valid production state. Verification tools degrade
   gracefully (`api_key_missing`); all other tools work.

2. **Minimum engram-dotnet for FlowForge**  
   After engram-dotnet publishes a release that contains ENG-456
   (`NoOpVerifier` factory in `Program.cs`, commit `5764ce1` or later — e.g.
   `v1.3.1` / `v1.4.0` / whatever tag carries `main` ≥ `5764ce1`):
   - Update [`install/manifest.yaml`](../../install/manifest.yaml)
     `requires.engram-dotnet` to that minimum (today’s `>=0.4.0` is **too weak**
     for this bug).
   - Ensure GitHub release assets exist (installer skips tags without binaries).

3. **Install / update path**  
   `EngramModule` continues to download the newest compatible release. Add a
   **post-install smoke** (CLI, no Anthropic in env):
   - `engram doctor` exits healthy for database + mcp_server, **or**
   - `engram mcp` + JSON-RPC `tools/call` `mem_stats` succeeds without
     `ANTHROPIC_API_KEY`.
   Failure → clear error: “engram-dotnet build too old / reinstall from
   release X; do not set Anthropic keys for basic MCP”.

4. **Version comparison guidance**  
   Prefer `engram --version` (commit suffix) over `/health.version` until
   engram-dotnet stops hardcoding `1.1.0` in HTTP health.

5. **Out of scope for this ADR**  
   Changing LLM provider for `mem_verify_artifact` (still Anthropic today).
   Rebuilding TrueNAS Docker images (separate ops); server clone at `712242a`
   already has the fix — client binary is the broken piece.

---

## Consequences

### Positive

- Restores the contract the wizard already documents (`ENGRAM_USER` as a normal string).
- One `flowforge update` / reinstall heals affected laptops.
- Manifest bump blocks silent reinstall of pre-ENG-456 assets.

### Negative / accepted

- Needs a **new engram-dotnet GitHub release** before FlowForge can pin it.
- Users on `72eb656` remain broken until they update.
- Until `/health` exposes InformationalVersion, ops must use CLI `--version`.

---

## Test plan (for implementing agent)

1. Clean env **without** `ANTHROPIC_API_KEY`.
2. Install/update engram via FlowForge to the new minimum release.
3. `engram --version` shows commit ≥ `5764ce1` (or release notes claim ENG-456).
4. Stdio MCP: `initialize` → `tools/call` `mem_stats` → success JSON (not
   `isError` / Anthropic message).
5. `mem_verify_artifact` without key → structured `api_key_missing` only.
6. Generated `mcp.json` env keys ⊆
   `{ENGRAM_DATA_DIR, ENGRAM_USER, ENGRAM_SYNC_ENABLED, ENGRAM_SERVER_URL}`.
7. `manifest.yaml` rejects installing a known-bad tag if someone pins it.

---

## Rollout

1. Land this ADR as Proposed → Accepted after CKP discussion.
2. Implement NS-09 (engram release + FlowForge pin + smoke).
3. Publish CHANGELOG note: “MCP no longer requires Anthropic for basic tools;
   update engram via FlowForge”.
4. Close failure handoff in `.ai-work/engram-mcp-anthropic-false-dependency/`.
