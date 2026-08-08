# Backlog — Engram MCP false Anthropic dependency (stale FlowForge binary)

> **Status:** Proposed (not implemented)  
> **Priority:** P0 — Critical (MCP tools unusable after normal install)  
> **Created:** 2026-07-19  
> **IDs:** NS-09 · ADR-013 (Proposed) · engram-dotnet ENG-456 (already fixed in `main`)  
> **Related:** [ADR-013](../decisions/ADR-013-engram-mcp-no-anthropic-dependency.md) ·
> [Failure report](../../.ai-work/engram-mcp-anthropic-false-dependency/failure-report.md) ·
> [ADR-002 manifest](../architecture/adr/ADR-002-runtime-manifest-for-compatibility.md)

---

## User story

**As a** developer who installed Engram via FlowForge,  
**I want** MCP memory tools to work with only the basic wizard config (`ENGRAM_USER`, data dir, optional sync URL),  
**so that** I am not blocked by a demand for `ANTHROPIC_API_KEY` that was never part of setup and is unrelated to the memory server.

---

## Problem (evidence 2026-07-19)

1. Wizard / `EngramModule.WriteMcpJson` correctly writes only Engram env vars (no Anthropic).
2. Installed binary: `C:\Users\efree\AppData\Local\Programs\FlowForge\engram.exe` →
   `1.0.0+72eb656bf6c8933d895d8462148a3dafbaf6d237` (release `v1.3.0`).
3. That commit is **before** ENG-456 (`5764ce1` NoOpVerifier).
4. TrueNAS + local git `main` are at `712242a` (includes the fix) — **source ≠ shipped client**.
5. MCP tool calls return: `An error occurred invoking 'mem_*'` with underlying
   `InvalidOperationException: ANTHROPIC_API_KEY is not set` from `LlmVerifier` ctor.
6. Workaround confirmed: any non-empty `ANTHROPIC_API_KEY` unblocks tools — **must not**
   become the official fix.

---

## Acceptance criteria

- [ ] AC-1: After `flowforge install` / `update` on a machine **without** `ANTHROPIC_API_KEY`, `mem_stats` and `mem_doctor` succeed via MCP.
- [ ] AC-2: Generated IDE `mcp.json` never contains `ANTHROPIC_API_KEY`.
- [ ] AC-3: `install/manifest.yaml` requires an engram-dotnet release that includes ENG-456 (commit ≥ `5764ce1`).
- [ ] AC-4: Docs (POST-INSTALL / QUICKSTART) state Anthropic is optional and only for `mem_verify_artifact`.
- [ ] AC-5: Post-install smoke fails loudly if the binary still throws on missing Anthropic key.
- [ ] AC-6: ADR-013 moved to Accepted when criteria above land.

---

## Work breakdown (for implementing agent)

### A — engram-dotnet (release)

1. Confirm `main` contains `5764ce1` / NoOpVerifier factory.
2. Cut and publish GitHub release **with binaries** (e.g. `v1.3.1` or next semver) from a commit ≥ ENG-456 (ideally current `main` / `712242a`+).
3. Optional follow-up: stop hardcoding `/health` `version: 1.1.0` (separate ENG; do not block NS-09).

### B — FlowForge (this repo)

1. Bump `requires.engram-dotnet` in `install/manifest.yaml` to the new minimum.
2. Add post-install smoke in `EngramModule` (or doctor hook) per ADR-013.
3. Update POST-INSTALL / troubleshooting: “do not set Anthropic for basic MCP; update engram”.
4. Manual: `flowforge update` on a machine stuck on `72eb656`; re-verify Cursor MCP tools.

### C — Ops note (TrueNAS)

- Server git at `712242a` already has the fix; still run `docker exec engram ./engram --version`
  after `--build` to confirm the **container** binary matches. Client laptop was the failure mode.

---

## Definition of done

- [ ] New engram release published with assets
- [ ] Manifest pin merged
- [ ] Smoke test automated or scripted
- [ ] ADR-013 Accepted
- [ ] Failure report marked resolved in `.ai-work/.../failure-report.md`
- [ ] Spot-check: Cursor `user-engram` `mem_stats` without Anthropic env

---

## Suggested FlowForge cycle

```text
/flow-start engram-mcp-anthropic-false-dependency
# or implement from ADR-013 + this NS without full cycle if hotfix agreed
```

Owner agent should start at
[`.ai-work/engram-mcp-anthropic-false-dependency/failure-report.md`](../../.ai-work/engram-mcp-anthropic-false-dependency/failure-report.md).
