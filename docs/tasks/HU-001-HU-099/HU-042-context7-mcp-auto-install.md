---
hu_id: HU-042
title: "FEATURE: Auto-install context7 MCP when using non-local Engram profile"
status: done
category: feature
flowforge_slug: "context7-mcp-auto-install"
priority: P1
severity: medium
created_date: 2026-10-14
closed_date: 2026-10-14
---

# HU-042 — FEATURE: Auto-install context7 MCP for non-local Engram profiles

## User Story

As a FlowForge operator,
I want FlowForge to automatically install the `context7` MCP server when installing with a non-local Engram profile (sync/remote),
so that MCP remote servers work out-of-the-box without manual configuration.

## Problem Description

When a user installs FlowForge with Engram using a **non-local profile** (sync/remote mode), OpenCode fails to connect to MCP remote servers because the `context7` MCP server is missing.

### Current Behavior

```
error: No tool named "tools.*" is currently available.
```

Despite:
- ✅ OpenCode installed
- ✅ MCP remote server configured
- ✅ Engram MCP server running

### Root Cause

OpenCode v2 requires `context7` as a prerequisite MCP server for remote MCP connections. Without it, the MCP handshake fails silently and remote tools are unavailable to the agent.

### Required Fix

1. **Documentation**: Document that `context7` is required when using non-local Engram profiles
2. **Auto-install**: Detect non-local profile during `flowforge install` and automatically add `context7` MCP

## Requirements

### FR-001: Profile Detection
- During `flowforge install`, detect if Engram profile is `local` or `sync/remote`
- This is determined by the presence of `ENGRAM_SERVER_URL` or the `sync` mode selection

### FR-002: context7 Auto-Installation
- When non-local profile detected, automatically run:
  ```bash
  opencode mcp add context7 --url https://mcp.context7.com/mcp
  ```
- This must work for both:
  - C# installer (`flowforge install --ide opencode`)
  - Bash installer (`ide/install.sh`)

### FR-003: Documentation Update
- Update `POST-INSTALL.md` to document the `context7` requirement for non-local profiles
- Add a troubleshooting section for MCP remote connection failures

## Technical Notes

### Profile Detection Logic

```bash
# Check if ENGRAM_SERVER_URL is set or sync mode was selected
if [ -n "$ENGRAM_SERVER_URL" ] || [ "$ENGRAM_PROFILE" != "local" ]; then
    echo "Non-local profile detected: installing context7 MCP..."
    opencode mcp add context7 --url https://mcp.context7.com/mcp --global
fi
```

### Alternative: Check Engram Config

```bash
# Check ~/.engram/config.json for sync.remote_url
if grep -q "sync" ~/.engram/config.json 2>/dev/null; then
    opencode mcp add context7 --url https://mcp.context7.com/mcp --global
fi
```

### OpenCode v2 MCP Documentation Reference

Per [OpenCode v2 MCP Servers docs](https://opencode.ai/v2/docs/mcp-servers/):

```bash
# Add context7 as remote MCP server
opencode mcp add context7 --url https://mcp.context7.com/mcp

# With --global flag for system-wide installation
opencode mcp add context7 --url https://mcp.context7.com/mcp --global
```

## Acceptance Criteria

| ID | Criteria | Verification | Status |
|----|----------|--------------|--------|
| AC-1 | `flowforge install --ide opencode` with sync profile adds `context7` MCP | Check `opencode mcp list` shows context7 connected | [x] PM-1 ✅ |
| AC-2 | `flowforge install --ide opencode` with local profile does NOT add `context7` | context7 not added unnecessarily | [x] PM-2 ✅ |
| AC-3 | `ide/install.sh` also handles the auto-installation | Bash script adds context7 when needed | [x] PM-3 ✅ |
| AC-4 | `POST-INSTALL.md` documents the requirement | Docs updated with troubleshooting section | [x] PM-4 ✅ |
| AC-5 | MCP remote tools work after install with sync profile | Remote MCP tools accessible | [x] PM-5 ✅ |

## Implementation Tasks

- [x] Update `InstallCommand.cs` (C#) to detect non-local profile and add context7
- [x] Update `ide/install.sh` to detect non-local profile and add context7
- [x] Update `POST-INSTALL.md` with context7 requirement documentation
- [x] Add unit tests for profile detection logic
- [x] Test with both local and sync profiles

> ✅ **CLOSED (CKP-4, 2026-10-14)** — All PM-1…PM-6 executed and verified by the human developer.
> `dotnet build` 0 errors · `dotnet test` 245/245 · `bash -n ide/install.sh` OK · rework cycle 2 = PASS (`verify-report.md`, commits `a5643f4` → `0a3dd10` → `47680b7`).
> No new ADR required — [ADR-006](docs/decisions/ADR-006-opencode-mcp-config.md) updated in place (`Update (2026-10-14)` section).
> Closing artefacts: `.ai-work/context7-mcp-auto-install/summary.md` (final). Superseded: `summary.preview.md`.
>
> **Residual debt (non-blocking)**: rework ISSUE-5 — align Bash (`grep -q '"sync"'`) vs C# (`sync` + `remote_url`) config-file detection.

## Dependencies

- None — this is a standalone feature

## Files to Modify

| File | Change |
|------|--------|
| `src/FlowForge.Installer/Commands/InstallCommand.cs` | Add context7 MCP installation for non-local profiles |
| `ide/install.sh` | Add context7 MCP installation for non-local profiles |
| `POST-INSTALL.md` | Document context7 requirement and troubleshooting |
| `docs/decisions/ADR-006-opencode-mcp-config.md` | Update to mention context7 as prerequisite |

## Related

- HU-040 (OpenCode v2 migration) — discovered the context7 requirement
- ADR-006 (OpenCode MCP config) — existing MCP configuration standard
