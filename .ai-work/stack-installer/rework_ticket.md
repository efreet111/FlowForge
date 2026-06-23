---
cycle_count: 0
severity: P1
feature: stack-installer
work_item: ENG-301
created: 2026-06-23
reporter: user (manual smoke test on Ubuntu 24.04 Docker container)
affects_commit: b1ca3e8
verifies_against: RNF-SEC-INFO-001, NFR-ERR-001
related_verify_finding: P1 #1 in .ai-work/stack-installer/verify-report.md
status: OPEN
---

# Rework ticket: `--verbose` flag crashes ConsoleAppFramework

## Expected (per spec.md)
Per `RNF-SEC-INFO-001` + `NFR-ERR-001`:
- A global `--verbose` flag MUST exist on the `flowforge` CLI.
- `flowforge --verbose --help` MUST print help text AND set `Verbosity.IsVerbose=true` with no exception.
- `flowforge <command> --verbose` MUST route errors through `Verbosity.FormatError` with stack traces visible.
- `flowforge <command>` (no flag) MUST emit clean errors with NO stack traces.

## Actual (observed 2026-06-23)
On commit `b1ca3e8` (HEAD of `feat/eng-301-stack-installer`), inside an Ubuntu 24.04 Docker container running the AOT-built `flowforge` binary:

```
$ /out/flowforge --help
Usage: [command] [-h|--help] [--version]

Commands:
  config get
  config set
  install
  uninstall
  update
# exit 0 ✅

$ /out/flowforge --verbose --help
System.NullReferenceException: Object reference not set to an instance of an object.
   at ConsoleAppFramework.ConsoleApp.ConsoleAppBuilder.RunCommand0(String[], Int32, Action`1, CancellationToken) + 0x24a
# exit 1 ❌
```

`--version` works (prints `0.1.0-alpha.1`), `--help` works, but `--verbose --help` throws a `NullReferenceException` inside ConsoleAppFramework's command dispatch.

## Reproduction steps
1. On a Linux host with Docker (verified on Pop!_OS, kernel matches Ubuntu 24.04).
2. Build the AOT binary:
   ```bash
   dotnet publish src/FlowForge.Installer/FlowForge.Installer.csproj \
     -c Release -r linux-x64 --self-contained -p:PublishAot=true -o ./out
   ```
3. Launch a clean Ubuntu 24.04 container:
   ```bash
   docker run -it --rm -v $(pwd)/out:/out ubuntu:24.04 bash
   ```
4. Inside the container:
   ```bash
   /out/flowforge --verbose --help
   echo "exit code: $?"
   ```
5. Observe `System.NullReferenceException` and exit code `1`.

## Evidence
- User manual smoke test on 2026-06-23 against commit `b1ca3e8`.
- Stack trace: `NullReferenceException` at `ConsoleAppFramework.ConsoleAppBuilder.RunCommand0(String[], Int32, Action\`1, CancellationToken) + 0x24a`.
- `forge-verify` audit had flagged this as **P1 #1** in `.ai-work/stack-installer/verify-report.md` (predicted risk: CAF rejects unknown flag).

## Environment
- **OS:** Ubuntu 24.04 (Docker container, base image `ubuntu:24.04`, sha256:786a8b558f7be160c6c8c4a54f9a57274f3b4fb1491cf65146521ae77ff1dc54).
- **Binary:** AOT-compiled `flowforge` (8,677,120 bytes), no .NET runtime needed.
- **Commit:** `b1ca3e8` on `feat/eng-301-stack-installer`.
- **.NET SDK used to build:** .NET 10 (target `net10.0`).
- **ConsoleAppFramework version:** unknown (visible in stack trace; check `src/FlowForge.Installer/FlowForge.Installer.csproj`).

## Severity
**P1** — Debugging capability (`--verbose`) is unavailable. Default error behavior (no stack trace leak) still works, so the security RNF is partially satisfied. The PM-* tests that need to see stack traces for diagnosis are blocked.

## Root cause hypothesis
In commit `634b6a4`, `forge-dev` implemented `--verbose` by extracting it manually from `args` BEFORE `app.Run(args)`. This mutation either:
- (a) leaves `--verbose` in the args array when CAF receives them, causing CAF to fail to match a command name → `RunCommand0` dereferences a null command object; or
- (b) leaves CAF's internal state inconsistent because the manual mutation bypassed CAF's option parsing pipeline.

The first one matches the observed stack trace (NRE during `RunCommand0` dispatch).

## Suggested fix for forge-dev
Register `--verbose` as a proper ConsoleAppFramework global option. Approaches in order of preference:

1. **CAF `[Option]` attribute on a global settings class** (preferred) — bind `--verbose` to a `VerbosityOptions` instance via CAF's source generator; CAF handles parsing natively.
2. **CAF `ConfigureServices` injection** — register `Verbosity` as a singleton service; use CAF's host configuration to parse `--verbose` from config sources before the args reach command dispatch.
3. **CAF builder option `TreatUnmatchedTokensAsErrors = false`** — if CAF v5 truly lacks a global option mechanism, suppress the unknown-option rejection so the manual extraction succeeds without conflict. (Last resort.)

Whichever approach, the fix MUST satisfy:
- `flowforge --verbose --help` exits 0 and prints help.
- `flowforge install --verbose` exits 0 on success; on failure, error includes stack trace.
- `flowforge install` (no flag) exits 0 on success; on failure, error contains ONLY the message (no stack trace, no assembly version).
- Build remains AOT-compatible (no reflection, no new NuGet deps).
- `dotnet build` is clean (0 warnings, 0 errors).

## Files likely affected
- `src/FlowForge.Installer/Program.cs:7-10,41-50` — CLI flag parsing + global exception handler.
- `src/FlowForge.Installer/Infrastructure/Verbosity.cs` — consumer of the flag.

## Verification protocol (forge-dev MUST run)
After applying the fix:
```bash
dotnet publish src/FlowForge.Installer/FlowForge.Installer.csproj \
  -c Release -r linux-x64 --self-contained -p:PublishAot=true -o ./out

docker run -it --rm -v $(pwd)/out:/out ubuntu:24.04 bash
# inside:
/out/flowforge --verbose --help          # must exit 0, print help
/out/flowforge install --verbose --help  # must exit 0
/out/flowforge --version                 # must print 0.1.0-alpha.1
echo "exit code: $?"
```
Capture results, attach to the Resolution section below.

## Resolution
<!-- forge-dev fills in after fix -->

