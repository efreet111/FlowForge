# Verify Report — ENG-453: Installer prompt for `ENGRAM_SERVER_URL`

**Date:** 2026-07-09  
**Phase:** 3b (forge-verify) — **Cycle 2**  
**Spec:** `spec.md` (retrospective synthesis, 209 lines — updated NFR-002)  
**Impl:** Commit `6f13d7e` on `main` + cycle 2 fixes in working tree (not yet committed)  
**Agent:** forge-verify (deepseek-v4-pro)  
**Cycle 1 report:** `verify-report.md` (cycle 1, line 159 — version P1)

---

## 1. Verdict: **PASS_DEGRADADO**

All 3 VERIFY issues from cycle 1 are **resolved**. All 2 CLEANUP items are **complete**. All 9 FRs and all 4 NFRs now PASS source-level audit. No new issues found. Tests could not be executed in this environment (no .NET SDK) — the human must run `dotnet test` before merging.

---

## 2. Cycle 2 Delta: What changed

| Item | Cycle 1 Verdict | Cycle 2 Fix | Cycle 2 Verdict |
|------|----------------|-------------|-----------------|
| **VERIFY-01** (exit code) | MINOR — `return;` exits 0 | `Environment.Exit(1)` added before `return;` at `InstallCommand.cs:107` | **PASS** |
| **VERIFY-02** (NFR-002 spec conflict) | MINOR — English headless error vs Spanish NFR | `spec.md:152` updated: "Error messages in headless mode may be in English" | **PASS** |
| **VERIFY-03** (atomic write) | FAIL — `File.WriteAllText` direct | `.tmp` + `File.Move(overwrite: true)` + `finally` cleanup at `ConfigStore.cs:44-61` | **PASS** |
| **CLEANUP-01** (ADR-010 status) | PENDING | `Accepted` + date `2026-07-09` at `ADR-010:3-5` | **PASS** |
| **CLEANUP-02** (POST-INSTALL §3) | PENDING | §3 rewritten, footer removed | **PASS** |

---

## 3. Issue-by-issue verification

### VERIFY-01 — Headless exit code non-zero

**File:** `src/FlowForge.Installer/Commands/InstallCommand.cs:100-110`

```csharp
if (string.IsNullOrWhiteSpace(engramSyncUrl))
{
    AnsiConsole.MarkupLine("[red]Error: sync mode requires --server-url, " +
        "ENGRAM_SERVER_URL env var, or a previous install with sync config.[/]");
    AnsiConsole.MarkupLine("[grey]  Set: flowforge install --server-url http://your-relay:7437[/]");
    Environment.Exit(1);    // ← line 107 — FIXED
    return;
}
```

**Checks:**

| Check | Result | Detail |
|-------|--------|--------|
| `Environment.Exit(1)` present before `return;`? | ✅ | Line 107 |
| Only executes when URL is truly missing? | ✅ | Guarded by `if (string.IsNullOrWhiteSpace(engramSyncUrl))` at line 102, inside `engramMode == "sync"` (line 95) and `isHeadless` (line 74) |
| Does NOT execute when URL is present (flag, env, or config)? | ✅ | Lines 97-101 populate `engramSyncUrl` from flag/env/config; if any sets it, line 102 is `false` → `Environment.Exit(1)` never reached |
| Flow control not broken? | ✅ | `Environment.Exit(1)` terminates process; the subsequent `return;` (line 108) is dead code after it, but present for readability/compiler — harmless |

> **✅ PASS** — Exit code 1 on headless missing-URL abort. No regression for interactive path (the `else` block at line 112 is independent).

---

### VERIFY-02 — NFR-002 spec alignment (English headless errors)

**File:** `.ai-work/eng-453-installer-server-url/spec.md:152`

Before (cycle 1):
```
- **NFR-002 (UX — Spanish copy):** All user-facing prompt strings are in Spanish...
```
(silence on headless errors → ambiguity, conflict with FR-004-D's English text)

After (cycle 2 — forge-dev update):
```
- **NFR-002 (UX — Spanish copy):** All user-facing prompt strings are in Spanish, matching the existing installer locale. `TextPrompt` title and validation messages are localized. Error messages in headless mode may be in English (they are consumed by scripts/CI, not by end users directly).
```

**Code check:** `InstallCommand.cs:104-105` remains English — now explicitly permitted by spec.

```
AnsiConsole.MarkupLine("[red]Error: sync mode requires --server-url, ...[/]");
```

**Checks:**

| Check | Result | Detail |
|-------|--------|--------|
| Spec now permits English headless errors? | ✅ | Line 152 — explicit clause added |
| Code emits English in headless (lines 104-105)? | ✅ | Aligned with spec |
| Interactive prompts remain Spanish? | ✅ | Lines 153-166 unchanged ("URL del servidor sync", "Requerido en modo sync", "Scheme debe ser http o https") |
| Internal spec conflict resolved? | ✅ | NFR-002 / FR-004-D no longer contradict |

> **✅ PASS** — NFR-002 updated. Code and spec are now aligned. This was a design decision by the orchestrator (human), not an implementation gap.

---

### VERIFY-03 — ConfigStore atomic write

**File:** `src/FlowForge.Installer/Infrastructure/ConfigStore.cs:44-61`

```csharp
public void Save(InstallerConfig config)
{
    Directory.CreateDirectory(Path.GetDirectoryName(_configFile)!);
    var json = JsonSerializer.Serialize(config, InstallerJsonContext.Default.InstallerConfig);
    var tmpFile = _configFile + ".tmp";
    try
    {
        File.WriteAllText(tmpFile, json + Environment.NewLine);
        File.Move(tmpFile, _configFile, overwrite: true);
    }
    finally
    {
        if (File.Exists(tmpFile))
            File.Delete(tmpFile);
    }
    _log.Info($"ConfigStore.Save: config.json actualizado");
}
```

**Edge case analysis:**

| Edge Case | Behavior | Safe? |
|-----------|----------|-------|
| `File.WriteAllText` throws | Exception propagates; `finally` runs; `File.Exists(tmpFile)` — if file was partially written, it's deleted; if not created, `Exists` returns false, skip. | ✅ Clean |
| `File.Move` throws (e.g., permissions on target dir) | Exception propagates; `finally` cleans `.tmp`. Original `config.json` untouched. | ✅ No corruption |
| `.tmp` deleted by external process between `WriteAllText` and `Move`? | `Move` throws `FileNotFoundException`; `finally` checks `Exists(tmpFile)` → false → no `Delete` needed. Exception propagates to caller. | ✅ No crash |
| `.tmp` in same directory as target? | `_configFile + ".tmp"` → same parent dir (e.g., `~/.engram/config.json.tmp`). `File.Move` is atomic on same filesystem (Linux: `rename(2)` syscall). | ✅ Atomic |
| `finally` throws during `File.Delete`? | Could happen if `.tmp` permissions are weird, but `catch` in the caller (`Update` at line 66) doesn't suppress — exception surfaces. Very unlikely in practice (user-owned `~/.engram/`). | ✅ Acceptable risk |
| Method signature changed? | Still `public void Save(InstallerConfig config)` — caller (`Update` at line 68) unchanged. | ✅ Compatible |
| Log line after `finally` | Only executes if no exception escaped `try`+`finally` (i.e., save succeeded). Correct — shouldn't log "actualizado" on failure. | ✅ Correct |

> **✅ PASS** — Atomic write implemented. All edge cases handled. Original file never left in partial/corrupt state.

---

### CLEANUP-01 — ADR-010 status: Proposed → Accepted

**File:** `docs/decisions/ADR-010-installer-prompt-for-server-url.md:3-5`

```markdown
> **Status**: Accepted
> **Date**: 2026-07-01
> **Accepted**: 2026-07-09
```

| Check | Result |
|-------|--------|
| Status now `Accepted`? | ✅ |
| Acceptance date present? | ✅ (`2026-07-09`) |
| Original `Date` (design date) preserved? | ✅ (`2026-07-01`) |

---

### CLEANUP-02 — POST-INSTALL.md §3 workaround removed

**File:** `POST-INSTALL.md:56-76` (§3), `:190-197` (footer)

**Before (cycle 1):** §3 described manual `export ENGRAM_SERVER_URL` as primary config method with note "ENG-453 (planned): flowforge install --mode=sync will prompt..."  

**After (cycle 2):**

```
## 3. Configure the sync target (sync mode only)

If you chose **Offline-first sync** during `flowforge install`, the installer
already prompted for the server URL and persisted it in `~/.engram/config.json`.
No manual configuration needed.

To change the sync server URL later, either:
- Re-run `flowforge install` and select sync again (the prompt pre-fills...)
- Or set `ENGRAM_SERVER_URL` as an environment variable:
```

| Check | Result |
|-------|--------|
| §3 workaround text ("ENG-453 planned...") removed? | ✅ Gone |
| §3 rewritten as "installer already handles this"? | ✅ First paragraph confirms installer prompted + persisted |
| Footer "Until ENG-453 ships..." removed? | ✅ File ends at line 194 (old lines 195-197 are gone) |
| Env var instructions still present (as fallback/alternative)? | ✅ Unchanged — documented as "To change... later" not as "workaround" |
| Other ENG references still in file? | ✅ ENG-454 at line 39 (different feature, out of scope) — untouched |

---

## 4. FR Coverage Matrix (cycle 2 re-audit)

| ID | Description | Verdict | Evidence |
|----|-------------|---------|----------|
| FR-001 | Interactive sync URL prompt (4 scenarios) | **PASS** | `InstallCommand.cs:146-176` — interactive path (`else` at line 112) untouched by cycle 2; prompt guard at line 146, prefill at 150-152, no prompt on local mode |
| FR-002 | URL format validation (4 scenarios) | **PASS** | `InstallCommand.cs:158-166` — unchanged from cycle 1: empty, `Uri.TryCreate`, scheme check, https passes |
| FR-003 | Server reachability probe (3 scenarios) | **PASS** | `InstallCommand.cs:171-174` call + `ProbeServerHealth` at line 298-313 — unchanged, 3s timeout at 304 |
| FR-004 | Headless mode URL resolution (4 scenarios) | **PASS** | A: flag priority line 97-98 ✅; B: env var line 99 ✅; C: config line 100-101 ✅; D: `Environment.Exit(1)` at line 107 ✅ **FIXED** |
| FR-005 | Headless auto-detection from config (3 scenarios) | **PASS** | `DetectSyncMode()` lines 275-292 — env var (277-278), config fallback (282-284), local default (291) |
| FR-006 | SyncConfig persistence (3 scenarios) | **PASS** | `ctx.Store.Update(...)` at line 225 calls `Save()` (now atomic at ConfigStore.cs:44). Public API unchanged. Sync write lines 233-239, local write line 236 (empty RemoteUrl), guard `if(installEngram)` line 227 |
| FR-007 | EngramModule MCP wiring (3 scenarios) | **PASS** | Cursor: `WriteMcpJson` env line 279; OpenCode: `MergeOpenCodeMcp` line 242; local mode: no URL (lines 219-222) |
| FR-008 | Hardcoded IP removal (3 scenarios) | **PASS** | `grep 192.168.0.178 src/FlowForge.Installer/` → 0 matches; `EngramModule.cs:31` throws `InvalidOperationException` |
| FR-009 | Re-install preserves config (2 scenarios) | **PASS** | `existingUrl` read at line 66, pre-filled at line 152, overwritten on new URL at line 236 |

**FR summary: 9/9 PASS** (was 8 PASS + 1 DEGRADADO in cycle 1)

---

## 5. NFR Coverage Matrix (cycle 2 re-audit)

| ID | Description | Verdict | Evidence |
|----|-------------|---------|----------|
| NFR-001 | AOT compatibility | **PASS** | Same code paths as cycle 1. `Uri.TryCreate`, `HttpClient`, `SyncConfig` in `InstallerJsonContext`. No new reflection. |
| NFR-002 | UX — Spanish copy | **PASS** | Spec updated line 152: headless errors may be English. All interactive strings Spanish (lines 153-166, 172-173). Headless error English (lines 104-106) — aligned. ✅ **FIXED** (was spec conflict) |
| NFR-003 | Timeout ceiling (3s) | **PASS** | `HttpClient { Timeout = TimeSpan.FromSeconds(3) }` at line 304. Single-shot probe, catch returns false. |
| NFR-004 | Config atomicity | **PASS** | `ConfigStore.cs:44-61` — `.tmp` + `File.Move(overwrite: true)` + `finally` cleanup. ✅ **FIXED** (was FAIL) |

**NFR summary: 4/4 PASS** (was 2 PASS + 1 DEGRADADO + 1 FAIL in cycle 1)

---

## 6. STRIDE Coverage (cycle 2 re-audit)

| Threat | Status | Notes |
|--------|--------|-------|
| **S**poofing | Accepted risk | LAN-only; no auth today (ADR-010 §Out of scope). No change from cycle 1. |
| **T**ampering | Accepted risk | User-scoped config file. Atomic write (NFR-004) reduces window of corruption but doesn't change trust model. |
| **R**epudiation | **Covered** | `ctx.Log.Info` at `InstallCommand.cs:266` + `connected_at` timestamp at line 239. Unchanged. |
| **I**nfo Disclosure | **Covered** | URL logged at INFO level (`EngramModule.cs:16`); no credentials in URL per ADR-010. Unchanged. |
| **D**enial of Service | **Covered** | Single `HttpClient` per probe, 3s timeout, no pooling/retry. Unchanged. |
| **E**levation of Privilege | **Covered** | All MCP configs under `$HOME`. Unchanged. |
| **None** (secrets) | **Covered** | `grep 192.168.0.178 src/` → 0 matches. Unchanged. |

**STRIDE: all 7 threats covered (4 mitigated, 3 accepted risk). No change from cycle 1.**

---

## 7. Tests

**Test file:** `tests/FlowForge.Installer.Tests/InstallerAsksForSyncUrlTests.cs` (91 lines)

### 7 source-level tests — cycle 2 re-verification

| # | Test Method | FR/NFR | Strings checked | Cycle 2 impact | Would pass? |
|---|-------------|--------|-----------------|----------------|-------------|
| 1 | `FR_010_InstallCommand_PromptsForSyncUrl_WhenSyncChosen` | FR-001 | `"URL del servidor sync"`, `"TextPrompt<string>"` | Strings unchanged (interactive path untouched) | ✅ Yes |
| 2 | `FR_010_InstallCommand_DoesNotHardcode_IP` | FR-008 | `"192.168.0.178"` absent | IP still absent | ✅ Yes |
| 3 | `FR_010_InstallCommand_HasServerUrlFlag` | FR-004 | `"string? serverUrl = null"` | Flag declaration at line 34 untouched | ✅ Yes |
| 4 | `FR_010_EngramModule_DoesNotHardcode_IP` | FR-008 | `"192.168.0.178"` absent | IP still absent | ✅ Yes |
| 5 | `FR_010_EngramModule_ThrowsWhenSyncWithoutUrl` | FR-008 | `"sync mode requires ENGRAM_SERVER_URL"`, `"InvalidOperationException"` | Strings at line 71, 31 untouched | ✅ Yes |
| 6 | `FR_010_InstallerConfig_HasSyncSection` | FR-006 | `"SyncConfig"`, `"RemoteUrl"` | Model unchanged | ✅ Yes |
| 7 | `FR_010_Installer_PersistsSyncConfig_OnInstall` | FR-006 | `"c.Sync = new SyncConfig"` | Line 233 untouched | ✅ Yes |

**Execution:** NOT run — .NET SDK not available in this environment. All tests are source-level string assertions against strings that remain unchanged. Manual verification confirms all 7 would pass.

**Coverage gap:** None of these tests cover the 3 new behaviors added in cycle 2:
- Exit code 1 on headless missing URL (requires process-level test — not feasible with source-level assertions)
- Atomic write pattern (would require runtime test mocking filesystem)
- NFR-002 spec wording (not testable via code)

This is acceptable — the existing test suite guards against regression of the core behavior. The 3 cycle 2 fixes are small, well-understood changes that don't change the observable API.

**Recommendation — run before merge:**
```bash
cd FlowForge
dotnet test tests/FlowForge.Installer.Tests/FlowForge.Installer.Tests.csproj -c Release \
  --filter "FullyQualifiedName~InstallerAsksForSyncUrl"
```

---

## 8. New Issues Found (cycle 2)

**None.** All 5 items from cycle 1 are resolved. No regressions detected. No new gaps identified.

However, one **observation** for the future:

> **Observation (non-blocking):** `ConfigStore.Save()` line 60 (`_log.Info(...)`) runs AFTER the `finally` block. If `finally` throws during `File.Delete(tmpFile)` (extremely unlikely — would require filesystem corruption on `~/.engram/`), the log line is skipped. This is desirable behavior (we shouldn't claim success on failure). No action needed.

---

## 9. Metrics (cycle 2)

| Metric | Cycle 1 | Cycle 2 | Delta |
|--------|---------|---------|-------|
| FRs PASS | 8 | **9** | +1 (FR-004-D fixed) |
| FRs PASS_DEGRADADO | 1 | 0 | −1 |
| FRs FAIL | 0 | 0 | 0 |
| NFRs PASS | 2 | **4** | +2 (NFR-002 resolved, NFR-004 fixed) |
| NFRs PASS_DEGRADADO | 1 | 0 | −1 |
| NFRs FAIL | 1 | 0 | −1 |
| STRIDE gaps | 0 | 0 | 0 |
| Tests present | 7 | 7 | 0 (unchanged) |
| Tests verified | 7 | 7 | 0 |
| Tests executed | 0 | 0 | 0 |
| CRITICAL issues | 0 | 0 | 0 |
| MINOR issues | 3 VERIFY + 2 CLEANUP | 0 | −5 |
| **NEW issues (cycle 2)** | — | **0** | 0 |

---

## 10. Summary

All 5 issues from cycle 1 are resolved:

| ID | Status | Fix |
|----|--------|-----|
| VERIFY-01 | ✅ Resolved | `Environment.Exit(1)` at `InstallCommand.cs:107` |
| VERIFY-02 | ✅ Resolved | Spec NFR-002 updated to permit English headless errors |
| VERIFY-03 | ✅ Resolved | `ConfigStore.Save()` now atomic: `.tmp` + `File.Move` + cleanup |
| CLEANUP-01 | ✅ Complete | ADR-010 → `Accepted` (2026-07-09) |
| CLEANUP-02 | ✅ Complete | POST-INSTALL.md §3 rewritten, ENG-453 footer removed |

**All 9 FRs and 4 NFRs PASS.** All 7 STRIDE threats covered. Zero new issues. Tests (7 source-level) would pass — strings unchanged.

The working tree is ready for commit. The only remaining action before merge is human execution of `dotnet test` to confirm no runtime surprises.

---

## Cycle History

| Cycle | Veredicto | Issues críticos | Issues menores | Acción |
|-------|-----------|-----------------|----------------|--------|
| 1 | PASS_DEGRADADO | 0 | 3 MINOR (VERIFY-01/02/03) + 2 CLEANUP | forge-dev fixes |
| 2 | PASS_DEGRADADO | 0 | 0 | Human: `dotnet test` + commit + merge |

---

## Memory Signal

- type: bugfix
- significance: low
- summary: "ENG-453 cycle 2 re-audit complete. All 5 cycle-1 issues resolved: Environment.Exit(1) on headless missing URL, ConfigStore atomic write (.tmp + rename), NFR-002 spec aligned, ADR-010 marked Accepted, POST-INSTALL.md §3 cleaned up. 9/9 FRs PASS, 4/4 NFRs PASS. Ready to commit after dotnet test."
