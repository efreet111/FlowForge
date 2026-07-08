# Context Map — ENG-453: Installer must prompt for ENGRAM_SERVER_URL

**Date**: 2026-07-06  
**Status**: Ready for implementation  
**Repo**: FlowForge (not engram-dotnet)  
**Priority**: P1  
**Effort**: S (1-2h)

---

## Problem Summary

When installing engram-dotnet via FlowForge installer in **sync mode**, the installer:
- ✅ Asks if user wants sync → yes
- ✅ Installs the binary
- ✅ Configures MCP
- ❌ **Does NOT ask for the server URL** (`ENGRAM_SERVER_URL`)
- ❌ **Does NOT persist it** in config

**Result**: SyncManager starts without knowing which server to connect to → detects self-loop (ENG-452) → disables with warning. User thinks sync is working but it's not.

---

## Current State

### What exists

1. **ADR-010** (`docs/decisions/ADR-010-installer-prompt-for-server-url.md`)
   - Status: **Proposed** (not yet Accepted)
   - 235 lines with complete design
   - Identifies 3 specific bugs with code locations
   - Defines new flow for interactive + headless modes
   - Test plan with 7 unit tests
   - Rollout plan

2. **ADR-009** (`docs/decisions/ADR-009-flowforge-sync-connect.md`)
   - Complementary: `flowforge sync connect <url>` command
   - For post-install URL changes
   - Also in Proposed status

3. **POST-INSTALL.md** (lines 73-75)
   - Documents the manual workaround
   - References ENG-453 as "planned"

4. **engram-dotnet BACKLOG.md** (line 120)
   - ENG-453 marked as Ready, P1
   - Notes it's in FlowForge repo, not engram-dotnet

### What's missing

- ❌ Implementation in `InstallCommand.cs` and `EngramModule.cs`
- ❌ ADR-010 status change from "Proposed" to "Accepted"
- ❌ Unit tests (7 tests defined in ADR-010 §Test plan)
- ❌ Update to POST-INSTALL.md §3 (remove manual workaround)

---

## Files to Modify (in FlowForge repo)

### Code changes

| File | Line(s) | Change |
|------|---------|--------|
| `src/FlowForge.Installer/InstallCommand.cs` | 113-121 | Add URL prompt when sync mode chosen |
| `src/FlowForge.Installer/InstallCommand.cs` | 201-202 | Update `DetectSyncMode` to handle headless mode properly |
| `src/FlowForge.Installer/EngramModule.cs` | 204-205, 264-265 | Remove hardcoded `192.168.0.178`, require URL for sync mode |

### Documentation changes

| File | Change |
|------|--------|
| `docs/decisions/ADR-010-installer-prompt-for-server-url.md` | Change status from "Proposed" to "Accepted" |
| `POST-INSTALL.md` | Remove §3 manual workaround (lines 60-75), replace with "installer now handles this" |

### Test changes

| File | Change |
|------|--------|
| `tests/FlowForge.Installer.Tests/InstallCommandTests.cs` | Add 7 tests from ADR-010 §Test plan |

---

## Dependencies

| Dependency | Status | Notes |
|------------|--------|-------|
| ENG-452 (self-loop detection) | ✅ Done | engram-dotnet `fec9d73` |
| ADR-010 design | ✅ Complete | Just needs status change to Accepted |
| ADR-009 (sync connect command) | 🔲 Not implemented | Complementary, can be done after ENG-453 |

---

## Acceptance Criteria

From ADR-010:

- [ ] Interactive mode: when sync chosen, prompt for URL
- [ ] Interactive mode: URL validation (format + reachability check)
- [ ] Interactive mode: warn if unreachable but allow continue (offline install OK)
- [ ] Headless mode: require `--server-url` flag OR `ENGRAM_SERVER_URL` env var
- [ ] Headless mode: abort with clear error if sync mode but no URL provided
- [ ] Remove hardcoded `192.168.0.178` fallback
- [ ] Persist URL to `~/.engram/config.json` under `sync.remote_url`
- [ ] On re-install, prefill prompt from existing config
- [ ] 7 unit tests pass (see ADR-010 §Test plan)
- [ ] POST-INSTALL.md updated (manual workaround removed)
- [ ] ADR-010 status changed to "Accepted"

---

## Implementation Notes

### For the implementing agent

1. **Read ADR-010 completely** before starting — it has all the details
2. **Start with the tests** (TDD approach) — the 7 tests are well-defined
3. **Interactive mode first**, then headless mode
4. **URL validation**: use `Uri.TryCreate` for format, `HttpClient` with 3s timeout for reachability
5. **Config persistence**: see ADR-010 §Persisted schema for the JSON structure
6. **Don't forget**: update POST-INSTALL.md to remove the manual workaround

### Potential gotchas

- The hardcoded IP `192.168.0.178` appears in multiple places — grep for it
- `DetectSyncMode` is used in headless mode — make sure it doesn't silently fall back to "local"
- Existing installs with hardcoded IP in MCP config will continue working (don't break them)
- The prompt should have a sensible default (prefill from existing config if available)

---

## Verification Plan

After implementation:

1. **Unit tests**: all 7 tests from ADR-010 pass
2. **Manual smoke test** (from ADR-010):
   ```bash
   # From a clean machine:
   curl -fsSL https://github.com/efreet111/FlowForge/raw/main/install/install.sh | bash
   # Wizard now asks for URL when sync chosen
   # MCP config gets the entered URL, not the hardcoded one
   ```
3. **Integration test**: install with sync mode, verify `~/.engram/config.json` has `sync.remote_url`
4. **Regression test**: install with local mode, verify no URL prompt appears

---

## Rollout

From ADR-010 §Rollout:

1. Land ADR-010 as Accepted
2. Implement in feature branch off `main`
3. Verify with forge-verify (cycle 1 expected to find at least 2 things)
4. Tag a new FlowForge alpha (e.g., `v0.1.0-alpha.8`)
5. Update POST-INSTALL.md §3 to remove manual workaround
6. Once shipped, ENG-453 closes in engram-dotnet BACKLOG

---

## References

- **ADR-010**: `docs/decisions/ADR-010-installer-prompt-for-server-url.md` (complete design)
- **ADR-009**: `docs/decisions/ADR-009-flowforge-sync-connect.md` (complementary command)
- **ADR-008** (engram-dotnet): self-loop detection
- **POST-INSTALL.md**: current workaround documentation
- **engram-dotnet BACKLOG**: ENG-453 entry (line 120)

---

## Next Steps for Implementing Agent

1. ✅ Read this context-map.md
2. ✅ Read ADR-010 completely
3. 🔲 Change ADR-010 status to "Accepted"
4. 🔲 Implement the 7 unit tests (TDD)
5. 🔲 Implement the code changes
6. 🔲 Update POST-INSTALL.md
7. 🔲 Run forge-verify
8. 🔲 Tag new FlowForge alpha
9. 🔲 Update engram-dotnet BACKLOG (mark ENG-453 as Done)

---

**Last updated**: 2026-07-06  
**Created by**: orchestrator (flowforge) during ENG-443/ENG-303 session
