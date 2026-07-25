# Failure report — Engram MCP requires `ANTHROPIC_API_KEY` after FlowForge install

> **Status:** Open — handoff for implementing agent  
> **Date discovered:** 2026-07-19  
> **Severity:** P0 (all MCP `mem_*` tools fail in Cursor)  
> **Repos:** FlowForge installer ships / keeps stale engram binary  
> **Canonical design:** [ADR-013](../../docs/decisions/ADR-013-engram-mcp-no-anthropic-dependency.md) (Proposed)  
> **Backlog:** [NS-09](../../docs/backlog/NS-09-engram-stale-release-mcp-anthropic.md)

---

## One-line summary

FlowForge-installed `engram.exe` (`72eb656` / release `v1.3.0`) crashes every MCP tool
when `ANTHROPIC_API_KEY` is unset; engram-dotnet `main` (`712242a`) already has ENG-456
`NoOpVerifier`. Setup never asked for Anthropic — only `ENGRAM_USER` (string) + sync URL.

---

## Environment (repro)

| Item | Value |
|------|--------|
| Client OS | Windows 10 (`LAPTOP-QP31LB9C`) |
| Engram binary path | `C:\Users\efree\AppData\Local\Programs\FlowForge\engram.exe` |
| `engram version` | `engram 1.3.0` |
| `engram --version` | `1.0.0+72eb656bf6c8933d895d8462148a3dafbaf6d237` |
| Cursor MCP config | `~/.cursor/mcp.json` → that exe + `ENGRAM_DATA_DIR`, `ENGRAM_USER=efree`, `ENGRAM_SYNC_ENABLED=true`, `ENGRAM_SERVER_URL=http://192.168.0.178:7437` |
| Relay | `http://192.168.0.178:7437/health` → OK, postgres |
| TrueNAS clone | `/mnt/Pool_8TB/engram_data` @ `712242a` |
| Local FlowForge / engram-dotnet git | `712242a` (after `git -c safe.directory=...`) |

---

## Symptoms

1. Cursor MCP server `user-engram` shows `ready` / tool schemas list OK.
2. `CallMcpTool` / stdio `tools/call` for `mem_stats`, `mem_doctor`, `mem_search` →
   `"An error occurred invoking 'mem_…'."`
3. Stderr from stdio probe:

```text
"mem_stats" threw an unhandled exception.
System.InvalidOperationException: ANTHROPIC_API_KEY is not set.
   at Engram.Verification.LlmVerifier..ctor(...)
   at Program.<>c.<<Main>$>b__0_40(IServiceProvider _)
```

4. CLI without MCP still works: `engram search`, `engram doctor` (with env) OK.
5. With **any** non-empty `ANTHROPIC_API_KEY` in process env, `mem_stats` / `mem_doctor` succeed.

---

## Root cause

```text
GitHub release v1.3.0 @ 72eb656
        │
        ▼
FlowForge EngramModule downloads "latest with assets"
        │
        ▼
%LocalAppData%\Programs\FlowForge\engram.exe  (NO NoOpVerifier)
        │
        ▼
EngramTools DI → LlmVerifier ctor → throw if no ANTHROPIC_API_KEY
        │
        ▼
ALL MCP tools fail (not only mem_verify_artifact)
```

Fixed upstream in engram-dotnet **ENG-456** (`5764ce1`), present on `main` /
`712242a`, **absent** from the installed release asset.

Relevant upstream code (current repo):

- `src/Engram.Cli/Program.cs` — factory `NoOpVerifier` vs `LlmVerifier`
- `docs/VERIFICATION.md` — Anthropic only for `mem_verify_artifact`
- `docs/SETUP-WIZARD.md` — wizard asks user string, not Anthropic

FlowForge correctly does **not** write Anthropic into MCP env
(`EngramModule.WriteMcpJson`).

---

## What NOT to do

- Do **not** add `ANTHROPIC_API_KEY` to installer-generated `mcp.json`.
- Do **not** treat `/health.version` (`1.1.0` hardcoded) as release identity.
- Do **not** close this by documenting a dummy API key as the permanent UX.

---

## Required fix (see ADR-013 + NS-09)

1. **engram-dotnet:** publish new GitHub release **with binaries** from commit ≥ `5764ce1`.
2. **FlowForge:** bump `install/manifest.yaml` `requires.engram-dotnet`; add post-install
   smoke without Anthropic; update POST-INSTALL troubleshooting.
3. **Verify:** fresh install / `flowforge update` → Cursor `mem_stats` works with zero Anthropic env.

---

## Quick verification commands (after fix)

```powershell
# Client — no Anthropic in env
Remove-Item Env:ANTHROPIC_API_KEY -ErrorAction SilentlyContinue
engram --version
engram doctor

# Expect InformationalVersion commit ≥ 5764ce1 (or new release tag)
# MCP: mem_stats must succeed
```

```bash
# TrueNAS — container vs git (optional, separate from client bug)
cd /mnt/Pool_8TB/engram_data
git rev-parse HEAD
docker exec engram ./engram --version
```

---

## Agent instructions

1. Read ADR-013 and NS-09 fully before coding.
2. Prefer coordinated release + manifest pin over local workarounds.
3. Keep MCP env contract: only Engram sync/user/data vars.
4. When done: mark this file **Status: Resolved**, link PR/release tags, set ADR-013 → Accepted.

---

## Related Cursor session notes

- Discovery session: engram-dotnet workspace, 2026-07-19.
- `72eb656` is ancestor of `712242a`; ENG-456 sits between them.
- Local git “dubious ownership” on Windows is unrelated; use
  `git -c safe.directory='…'` if needed — do not require global config for diagnosis.
