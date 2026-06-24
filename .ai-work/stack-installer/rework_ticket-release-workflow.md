---
cycle_count: 1
severity: P0
feature: stack-installer
work_item: ENG-301
created: 2026-06-23
reporter: GitHub Actions (release workflow failure on tag push)
affects_commit: a3ff937 (merge commit on main, tag v0.1.0-alpha.1)
verifies_against: FR-007 (GitHub Actions semver pre-release detection)
status: RESOLVED
---

# Rework ticket: `release.yml` cross-compile AOT failure

## Expected (per spec.md)
Per `FR-007`:
- Tagging `v0.1.0-alpha.1` in GitHub triggers the release workflow.
- `softprops/action-gh-release@v2` receives `prerelease: true` (per semver detection step).
- linux-x64 + win-x64 binaries published as release assets.
- SHA-256 checksum files generated for each binary.

## Actual (2026-06-23, tag push v0.1.0-alpha.1)
The release workflow run failed at the win-x64 publish step:

```
Run dotnet publish src/FlowForge.Installer
  Determining projects to restore...
  Restored /home/runner/work/FlowForge/FlowForge/src/FlowForge.Installer/FlowForge.Installer.csproj (in 2.43 sec).
  FlowForge.Installer -> /home/runner/work/FlowForge/FlowForge/src/FlowForge.Installer/bin/Release/net10.0/win-x64/flowforge.dll
/home/runner/.nuget/packages/microsoft.dotnet.ilcompiler/10.0.9/build/Microsoft.NETCore.Native.Publish.targets(63,5): error : Cross-OS native compilation is not supported. [/home/runner/work/FlowForge/FlowForge/src/FlowForge.Installer/FlowForge.Installer.csproj]
Error: Process completed with exit code 1.
```

The workflow successfully restored + compiled to IL for win-x64, but the **AOT native compilation step** (ILC) failed because the runner OS (Linux) cannot produce Windows native binaries.

## Reproduction steps
1. Tag `v*` pushed to `main`.
2. `release.yml` triggers on tag push.
3. Job runs on `ubuntu-latest`.
4. Step `Publish linux-x64 (AOT)` succeeds.
5. Step `Publish win-x64 (AOT)` fails at `Microsoft.NETCore.Native.Publish.targets` with the cross-OS error.
6. Job exits with code 1. No GitHub Release created.

## Evidence
- GitHub Actions run log (2026-06-23) showing the ILC error and exit code 1.
- The error originates from `microsoft.dotnet.ilcompiler` NuGet package version 10.0.9.

## Environment
- **Runner:** `ubuntu-latest` (single OS for the entire job).
- **.NET SDK:** 10.0.x via `actions/setup-dotnet@v4`.
- **ILC version:** 10.0.9.
- **Affected commit:** `a3ff937` on `main` (merge commit for ENG-301).

## Severity
**P0** — Blocks the entire v0.1.0-alpha.1 release. Without this fix, no GitHub Release can be published, and PM-1 full (which depends on `curl | bash` from GitHub Releases) is impossible.

## Root cause
`.github/workflows/release.yml` line 14: `runs-on: ubuntu-latest` — single Linux runner for the entire job. Lines 30-48: both `Publish linux-x64` and `Publish win-x64` AOT steps run on this same Linux runner. **.NET AOT cannot cross-compile native binaries between operating systems**: ILC on Linux can only produce Linux native binaries, ILC on Windows can only produce Windows native binaries.

## Suggested fix
Restructure `release.yml` to use a matrix of `{target, os}` so each AOT target is built on its native OS runner:

1. **`publish` job** with matrix:
   - `{target: linux-x64, os: ubuntu-latest}` → runs Linux AOT publish
   - `{target: win-x64, os: windows-latest}` → runs Windows AOT publish
   - Each entry: restore → build → publish AOT → generate SHA-256 (Linux: `sha256sum`, Windows: `Get-FileHash`) → upload artifact via `actions/upload-artifact@v4`

2. **`release` job** with `needs: publish`:
   - Runs on `ubuntu-latest`
   - Downloads both artifacts via `actions/download-artifact@v4`
   - Determines `prerelease` from tag (existing logic)
   - Creates GitHub Release via `softprops/action-gh-release@v2` with all 4 files (linux binary + sha, windows binary + sha)

## Files
- `.github/workflows/release.yml` — full restructure needed

## Verification protocol (forge-dev + orchestrator)
After the fix:
1. Commit on `main`.
2. Push `main` to origin.
3. Move tag `v0.1.0-alpha.1` to new HEAD (orchestrator):
   ```bash
   git push origin :refs/tags/v0.1.0-alpha.1
   git tag -d v0.1.0-alpha.1
   git tag -a v0.1.0-alpha.1 -m "..."
   git push origin v0.1.0-alpha.1
   ```
4. Watch GitHub Actions — both matrix jobs must succeed.
5. Verify GitHub Release page has 4 assets and is marked Pre-release.

## Resolution

**Cycle 1:** `fa5b63e` — matrix restructure
- Changed `release.yml` from single `runs-on: ubuntu-latest` to matrix `{target, os}`:
  - `{target: linux-x64, os: ubuntu-latest}`
  - `{target: win-x64, os: windows-latest}`
- Each matrix entry builds on its native OS runner (AOT cannot cross-compile)
- Added `actions/upload-artifact@v4` per entry; `softprops/action-gh-release@v2` in downstream `release` job

**Cycle 2:** `9e39dd4` — PowerShell fix
- Windows AOT step used `shell: pwsh` but needed `shell: bash` for AOT commands
- Changed matrix entry to use `shell: bash` on windows-latest runner

**Verification output:**
```
# tag v0.1.0-alpha.2 pushed to origin
# GitHub Actions run: both matrix jobs passed
# Release v0.1.0-alpha.2 created with 4 assets:
#   flowforge-linux-x64 (+ .sha256)
#   flowforge-win-x64.exe (+ .sha256)
# Pre-release badge: ✅ (semver detection works)
```

**Date:** 2026-06-23

**Acceptance criteria:**
- [x] linux-x64 AOT publish on ubuntu-latest — PASS
- [x] win-x64 AOT publish on windows-latest — PASS
- [x] SHA-256 generated for both binaries — PASS
- [x] GitHub Release created with all 4 assets — PASS
- [x] Pre-release badge shown for `v0.1.0-alpha.2` — PASS

