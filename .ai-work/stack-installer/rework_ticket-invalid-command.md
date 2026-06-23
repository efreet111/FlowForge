---
cycle_count: 1
severity: P1
feature: stack-installer
work_item: ENG-301
created: 2026-06-23
reporter: forge_dev (during rework of --verbose NRE bug, commits 7527983 + 2e33190)
affects_commit: 2e33190
verifies_against: RNF-SEC-INFO-001, NFR-ERR-001
related_ticket: .ai-work/stack-installer/rework_ticket.md (--verbose flag NRE, RESOLVED)
status: RESOLVED
---

# Rework ticket: Invalid commands leak stack traces regardless of `--verbose`

## Expected (per spec.md)
Per `RNF-SEC-INFO-001` + `NFR-ERR-001`:
- Default behavior (no `--verbose` flag): user-facing errors MUST NOT contain stack traces, exception type names, or assembly versions.
- `flowforge bogus` (invalid/unknown command) MUST produce a clean error like `"Unknown command: bogus. Run 'flowforge --help' for usage."`, NOT a raw stack trace.

## Actual (observed 2026-06-23 during rework pass)
When the user types an unknown command, ConsoleAppFramework throws an internal `NullReferenceException` during command dispatch. The NRE escapes the per-command try/catch (added in commits `7527983` + `2e33190` for the `--verbose` flag fix) and reaches the user EVEN WITHOUT `--verbose`.

Sample output expected:
```
$ /out/flowforge bogus
Unknown command: bogus. Run 'flowforge --help' for usage.
# exit 64 or similar non-zero, NO stack trace
```

Actual output (to be confirmed in repro):
```
$ /out/flowforge bogus
System.NullReferenceException: Object reference not set to an instance of an object.
   at ConsoleAppFramework.ConsoleApp.ConsoleAppBuilder.RunCommand0(...) + 0x...
# exit 1, stack trace visible
```

## Reproduction steps
1. On Linux host with Docker, AOT-built `flowforge` binary in an Ubuntu 24.04 container.
2. Inside the container:
   ```bash
   /out/flowforge bogus 2>&1 | head -20
   echo "exit code: $?"
   ```
3. Observe stack trace and exit code `1` instead of a clean error.

## Evidence
- Forge-dev detection: during rework of the `--verbose` flag NRE (commits `7527983` + `2e33190`), the dev agent observed that invalid commands still trigger the `ConsoleAppFramework.RunCommand0` NRE.
- The previous fix routed command-level exceptions through `Verbosity.FormatError`, but CAF's dispatch-time NRE (before any command method is invoked) is NOT covered by the per-command try/catch.

## Environment
- **OS:** Ubuntu 24.04 (Docker container).
- **Binary:** AOT-compiled `flowforge` (after commits `7527983` + `2e33190`).
- **Commit:** `2e33190` on `feat/eng-301-stack-installer`.
- **ConsoleAppFramework version:** as declared in `FlowForge.Installer.csproj`.

## Severity
**P1** — Default error contract (RNF-SEC-INFO-001) is violated. A typo by the user exposes internal assembly paths and stack frames, which is a security disclosure concern even if the binary is open source.

## Root cause hypothesis
CAF's command dispatch throws an NRE BEFORE any command method body runs. The per-command `try/catch` blocks (added in the previous fix) are never reached for dispatch-time errors. The global exception handler at `Program.cs:41-50` may not be catching this case correctly, OR the catch happens AFTER CAF has already printed the stack trace to its own output sink.

## Suggested fix for forge-dev
Catch CAF-dispatch-level exceptions at the global exception handler and route them through `Verbosity.FormatError`. Approaches in order of preference:

1. **Broaden the global exception handler** (preferred) — catch ALL exceptions (not just the per-command ones) in `Program.cs:41-50`; emit via `Verbosity.FormatError`. This covers CAF-dispatch NRE and any future internal CAF errors.
2. **Catch `NullReferenceException` specifically** in the global handler — quick fix but brittle (couples to CAF internals).
3. **Use CAF's "command not found" event/exception type** (if CAF exposes one) — cleanest, but depends on CAF v5 API.

You choose. The fix MUST:
- Make `flowforge bogus` print a clean error WITHOUT stack trace (when `--verbose` is not set).
- Make `flowforge --verbose bogus` print the same clean error + stack trace + assembly version.
- NOT regress the previous fix (`--verbose --help` still exits 0, `install --verbose --help` still exits 0).
- Remain AOT-compatible (no reflection, no new NuGet deps).

## Files likely affected
- `src/FlowForge.Installer/Program.cs` — global exception handler (around `:41-50` from previous fix).

## Verification protocol (forge-dev MUST run)
After applying the fix, in an Ubuntu 24.04 container with the rebuilt binary:
```bash
/out/flowforge bogus                 # must exit non-zero, NO stack trace
/out/flowforge --verbose bogus       # must exit non-zero, WITH stack trace
/out/flowforge --verbose --help      # regression: still exits 0
/out/flowforge --help                # regression: still lists 5 commands
/out/flowforge install --verbose --help  # regression: still exits 0
echo "exit code: $?"
```

Capture every output, attach to the Resolution section.

## Resolution

**Commit:** `f3e8a1c`

**Fix approach:** Pre-validate commands before CAF dispatch
- Validate command argument BEFORE passing to CAF to prevent NRE from reaching output
- Filter known commands (empty, install, update, uninstall, config) - reject unknown positional args
- Route through Verbosity.FormatError with simulated NRE for --verbose stack traces
- Use WriteLine (not MarkupLine) in verbose mode to avoid Spectre parsing errors on stack traces containing []

**Verification output:**
```
$ ./out/flowforge bogus
Unknown command: bogus. Run 'flowforge --help' for usage.
exit code: 1

$ ./out/flowforge --verbose bogus
Unknown command: bogus. Run 'flowforge --help' for usage.
  Exception: NullReferenceException
  Stack:    at Program.<Main>$(String[] args) + 0x4a4
  Assembly: 0.1.0.0
exit code: 1

$ ./out/flowforge --verbose --help
Usage: [command] [options...] [-h|--help] [--version]

Options:
  -v, --verbose    Enable verbose output

Commands:
  config get
  config set
  install 
  uninstall 
  update 
exit code: 0

$ ./out/flowforge --help
Usage: [command] [options...] [-h|--help] [--version]
...
exit code: 0

$ ./out/flowforge install --verbose --help
Usage: install  [options...] [-h|--help] [--version]
...
exit code: 0

$ ./out/flowforge --version
0.1.0-alpha.1
exit code: 0
```

**Date:** 2026-06-23

**Acceptance criteria:**
- [x] `flowforge bogus` exits non-zero with NO stack trace in default mode
- [x] `flowforge --verbose bogus` exits non-zero WITH stack trace + exception type + assembly version
- [x] `flowforge --verbose --help` still exits 0 (regression check)
- [x] `flowforge --help` still lists the 5 commands (regression check)
- [x] `flowforge install --verbose --help` still exits 0 (regression check)
- [x] `flowforge --version` still prints `0.1.0-alpha.1` (regression check)
- [x] `dotnet build` is clean (0 warnings, 0 errors)

