---
capability_matrix:
  ai_reasoning:
    - UX copy language (Spanish) and interactive prompt phrasing
    - Which channel (stable/beta/nightly) is default for bootstrap scripts
    - Interactive confirmation flow when installer is outdated (continue prompt vs hard block)
    - Upgrade prompt phrasing for out-of-date installer
  deterministic:
    - AOT compilation with PublishAot=true — no dynamic reflection allowed
    - Spectre.Console.Cli is explicitly excluded; all CLI routing via ConsoleAppFramework
    - SHA-256 checksum must be verified before binary execution (bootstrap scripts)
    - manifest.yaml fetched with 5-second timeout; on any failure, defaults apply (no block)
    - requires.installer check emits a warning (not a hard block); user can opt to continue
    - requires.engram-dotnet check emits a hard block if unsatisfied
    - GitHub Actions semver pre-release detection: tags matching \-(alpha|beta|rc) are prerelease=true
---

# Spec: stack-installer

## 1. Objective and scope

**Feature slug:** `stack-installer`
**Work item:** ENG-301
**Branch:** `feat/eng-301-stack-installer`
**ADRs:** [ADR-001](../../docs/architecture/adr/ADR-001-installer-technology-stack.md), [ADR-002](../../docs/architecture/adr/ADR-002-runtime-manifest-for-compatibility.md)
**Status:** retrospective synthesis (code already implemented on branch)

### Context

The FlowForge stack installer (`flowforge`) is a self-contained binary that installs and manages three components: `engram-dotnet` (memory backend), `FlowForge` (skills/agents for IDEs), and `FlowDoc` (project documentation structure). Users must be able to install the stack on Linux, macOS, and Windows without pre-installing any runtime (.NET SDK, Node, Python, etc.).

ADR-001 establishes C# .NET 10 AOT as the implementation technology, selecting ConsoleAppFramework for CLI routing (ruling out Spectre.Console.Cli for AOT-incompatibility reasons), Spectre.Console core for UI rendering, and System.Net.Http + System.Text.Json with source-generated JsonSerializerContext. It also defines the bootstrap distribution model: lightweight shell scripts that download and verify the AOT binary from GitHub Releases, plus a GitHub Actions pipeline that publishes `linux-x64` and `win-x64` binaries on every `v*` tag.

ADR-002 establishes a remote `manifest.yaml` fetched at runtime from the `main` branch of the FlowForge repository. This allows compatibility constraints to be updated without recompiling the static AOT binary. The manifest defines minimum required versions for both the installer itself and `engram-dotnet`. On network failure (timeout, 404, or any exception), the installer degrades gracefully to built-in defaults rather than blocking the user.

### Out of scope
- Auto-update of the installer binary itself (`flowforge update --self`) — the ADR describes the workflow; the bootstrap scripts (`install.sh`/`install.ps1`) handle binary replacement
- Multiple package formats beyond AOT binaries (npm, PyPI, etc.)
- Rollback or downgrade of installed components

---

## 2. Functional requirements (FR)

- FR-001: AOT binary publication — `dotnet publish -p:PublishAot=true` produces a single self-contained binary for `linux-x64` and `win-x64` targets (ADR-001, release.yml lines 30–53)
  * Scenario A: Given a clean Ubuntu 24.04 machine with .NET 10 SDK installed, When the CI runs `dotnet publish -c Release -r linux-x64 --self-contained -p:PublishAot=true`, Then the output `out/flowforge-linux-x64` is a single ELF binary with no .NET runtime dependency, confirmed by `file out/flowforge-linux-x64` showing `ELF 64-bit`
  * Scenario B: Given a clean Windows Server 2022 machine with .NET 10 SDK, When the CI runs the win-x64 publish step, Then `out/flowforge-win-x64.exe` is a PE executable that runs without requiring .NET to be installed

- FR-002: Bootstrap download + SHA-256 verification — `install.sh` and `install.ps1` detect OS and architecture, download the correct binary from GitHub Releases, verify its SHA-256 checksum, and execute `flowforge install` (ADR-001, install.sh lines 85–103, install.ps1 lines 65–79)
  * Scenario A: Given a macOS arm64 machine with no FlowForge installed, When the user runs `curl -fsSL https://raw.githubusercontent.com/efreet111/FlowForge/main/install/install.sh | bash`, Then the script detects `macos`/`arm64`, downloads `flowforge-macos-arm64` (or falls back to `macos-x64`), verifies the SHA-256 from `{url}.sha256`, installs to `~/.local/bin/flowforge`, and runs the install wizard
  * Scenario B: Given a Windows machine, When the user runs `.\install.ps1 -Version v0.1.0-alpha.1`, Then the script downloads `flowforge-win-x64.exe`, verifies its SHA-256 from the accompanying `.sha256` file, installs to `$env:LOCALAPPDATA\Programs\FlowForge\flowforge.exe`, and runs the wizard

- FR-003: Runtime manifest fetch — `flowforge install` and `flowforge update` download `manifest.yaml` from `https://raw.githubusercontent.com/efreet111/FlowForge/main/install/manifest.yaml` with a 5-second timeout (ADR-002, ManifestClient.cs lines 21–54)
  * Scenario A: Given a machine with internet access, When `flowforge install` is invoked, Then `ManifestClient.FetchAsync()` makes an HTTP GET to the manifest URL with a 5-second cancellation deadline and a `User-Agent: flowforge-installer/0.1.0` header
  * Scenario B: Given a machine with internet access and the manifest URL returns HTTP 200, When the YAML is parsed, Then `RemoteManifest.IsRemote` is set to `true` and the `requires.engram-dotnet` and `requires.installer` fields are populated

- FR-004: Best-effort degradation on manifest failure — when the manifest is unreachable (network error, HTTP non-2xx, timeout, parse error), the installer emits a warning and continues with built-in defaults (ADR-002, ManifestClient.cs lines 44–53)
  * Scenario A: Given a machine with no internet access, When `flowforge install` is invoked, Then `ManifestClient.FetchAsync()` catches the exception, logs `"ManifestClient: timeout al obtener manifest — usando defaults"` (ManifestClient.cs line 46), and returns `RemoteManifest.Default`; the install wizard proceeds without blocking
  * Scenario B: Given a machine where the manifest URL returns HTTP 404, When `flowforge install` is invoked, Then `ManifestClient.FetchAsync()` logs `"ManifestClient: HTTP 404 — usando manifest por defecto"` and returns `RemoteManifest.Default`

- FR-005: Installer version warning — when the remote manifest's `requires.installer` is greater than the running binary's version, a warning is emitted with the command `flowforge update --self`; the operation may continue interactively (ADR-002, ManifestClient.cs lines 86–104, InstallCommand.cs lines 27–33)
  * Scenario A: Given an installer running `v0.1.0-alpha.1` and a remote manifest with `requires.installer: ">=0.2.0"`, When `flowforge install` fetches the remote manifest and calls `CheckInstallerCompatibility`, Then `AnsiConsole.MarkupLine` emits `"Este installer (v0.1.0-alpha.1) está desactualizado. Se requiere v0.2.0 o superior. Actualizá con: flowforge update --self"`; if the user answers `yes` the operation continues, otherwise it exits
  * Scenario B: Given a local manifest (not remote), When `CheckInstallerCompatibility` is called, Then it returns `null` immediately (no check performed for non-remote manifests)

- FR-006: Engram-dotnet compatibility hard block — when the target `engram-dotnet` version does not satisfy `requires.engram-dotnet`, the operation is blocked with a clear error (ADR-002, ManifestClient.cs lines 61–80, UpdateCommand.cs lines 64–68)
  * Scenario A: Given a remote manifest with `requires.engram-dotnet: ">=1.0.0"` and a candidate `engram-dotnet` version `0.9.0`, When `flowforge update` calls `CheckEngramCompatibility`, Then `AnsiConsole.MarkupLine` emits `"Incompatibilidad de versión: engram-dotnet 0.9.0 no es compatible. Se requiere >=1.0.0."` and the update is aborted before any download begins
  * Scenario B: Given a remote manifest with `requires.engram-dotnet: ">=0.3.0"` and a candidate `engram-dotnet` version `0.5.0`, When `CheckEngramCompatibility` is called, Then it returns `null` (compatible) and the update proceeds

- FR-007: Semver pre-release flagging in release pipeline — pushing a tag matching `v*-(alpha|beta|rc)*` triggers the release workflow; the `softprops/action-gh-release` step receives `prerelease: true` (ADR-001, release.yml lines 60–74)
  * Scenario A: Given a developer pushes tag `v0.1.0-alpha.1` to GitHub, When the `release.yml` workflow runs the semver step, Then `steps.semver.outputs.prerelease` is set to `true` and the created GitHub Release is marked as a pre-release
  * Scenario B: Given a developer pushes tag `v0.1.0` (no pre-release suffix), When the semver step runs, Then `prerelease=false` and the release is published as a stable release

---

## 3. Non-functional requirements (NFR)

- NFR-001: AOT compilation constraint — every new NuGet dependency MUST be AOT-compatible (no reflection-based serialization, no runtime IL generation). All serialization uses `JsonSerializerContext` source generators. Violations cause a trim analyzer error at compile time. (ADR-001)
- NFR-002: Source generators — wherever reflection would otherwise be required (CLI routing, JSON serialization, HTTP client configuration), a source generator must be used. `ConsoleAppFramework` provides the CLI source generator; `System.Text.Json` with `JsonSerializerContext` provides serialization. (ADR-001, csproj lines 34–41)
- NFR-003: Spectre.Console.Cli exclusion — `Spectre.Console.Cli` is explicitly prohibited in the project. All CLI subcommand routing goes through `ConsoleAppFramework`. `Spectre.Console` core is used for interactive prompts, tables, and progress rendering only. (ADR-001)
- NFR-004: TrimmerRoots coverage — any non-AOT-safe type that is used implicitly (e.g., through a generic overload) must be declared in `TrimmerRoots.xml`. New dependencies that require reflection must be reviewed against the [AOT compatibility matrix](https://github.com/Cysharp/ConsoleAppFramework) before inclusion. (ADR-001)
- NFR-005: Globalisation invariant mode — `<InvariantGlobalization>true</InvariantGlobalization>` is set in the csproj to avoid embedding ICU data; all string operations are culture-invariant. (csproj line 16)

### Security Requirements (STRIDE)

| Threat | Question | Mitigation | RNF |
|--------|----------|------------|-----|
| **T**ampering | Could a man-in-the-middle serve a modified binary? | Bootstrap scripts download from `https://github.com/` (HTTPS); SHA-256 checksum from `.sha256` sidecar file is verified before execution | RNF-SEC-TAMPER-001 |
| **T**ampering | Could the manifest.yaml be modified in transit? | No signature on manifest.yaml currently; downloads over HTTPS only | RNF-SEC-TAMPER-002 |
| **I**nformation Disclosure | Could the installer leak sensitive data in HTTP headers or logs? | `User-Agent` header is set to `flowforge-installer/0.1.0`; no credentials in HTTP requests; error messages do not contain stack traces unless `--verbose` | RNF-SEC-INFO-001 |
| **D**enial of Service | Could a slow/no network cause the installer to hang? | `ManifestClient.FetchAsync` has a hard 5-second timeout via `CancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(5))`; on timeout, degrades to defaults without blocking | RNF-SEC-DOS-001 |
| **S**poofing | Could an attacker host a fake GitHub release? | Bootstrap scripts use pinned `https://github.com/{repo}/releases/download/{version}/` URLs; no third-party mirrors supported | RNF-SEC-SPOOF-001 |
| **E**levation of Privilege | Could the installer run arbitrary code as root/admin? | Bootstrap scripts use `install -m 755` (not `777`); PowerShell installer uses `User` scope PATH, not Machine scope | RNF-SEC-ELEV-001 |

- RNF-SEC-TAMPER-001: The downloaded binary MUST be verified against the SHA-256 checksum published alongside it in the GitHub Release assets before execution. If the checksum does not match, the bootstrap script MUST exit with a non-zero code and emit an error message naming the expected and actual hashes.
- RNF-SEC-TAMPER-002: The manifest.yaml is fetched over HTTPS. No cryptographic signature is currently applied to the manifest. (Note: manifest signing is tracked as OQ-1.)
- RNF-SEC-INFO-001: No PII, credentials, or internal hostnames appear in HTTP headers, log output, or user-facing error messages. Stack traces are suppressed in all user-facing output unless the `--verbose` flag is passed.
- RNF-SEC-DOS-001: All outbound HTTP calls from the installer binary have a maximum lifetime of 30 seconds (`HttpClient.Timeout = TimeSpan.FromSeconds(30)`); the manifest fetch specifically has a 5-second timeout.
- RNF-SEC-SPOOF-001: Bootstrap scripts only support direct GitHub Downloads URLs. No redirect-following to third-party hosts is performed.
- RNF-SEC-ELEV-001: The installer does not request elevated privileges. PATH modifications are scoped to the current user (`User` scope on Windows, `~/.bashrc` annotation on Linux).

### Error Messages

- NFR-ERR-001: User-facing messages follow the UX copy language declared in `capability_matrix` (currently Spanish for v0.1.0-alpha.1; see frontmatter). Error strings do not include exception type names, stack traces, or internal assembly versions unless `--verbose` is passed. (NFR from ADR-001 consequence: "Error handling tipado, logs estructurados, fácil de testear unitariamente")

---

## 4. Developer manual tests (required — mark [x] before /flow-close)

| ID | Case / flow | Steps (summary) | Expected result | [x] |
|----|-------------|-----------------|-----------------|-----|
| PM-1 | Clean Linux install via bootstrap script | 1. On a clean Ubuntu 24.04 machine, run `curl -fsSL https://raw.githubusercontent.com/efreet111/FlowForge/main/install/install.sh \| bash`<br>2. Observe download, SHA-256 verification, binary install<br>3. Run `flowforge --version` | `flowforge` outputs `0.1.0-alpha.1`; wizard launches interactively | [x] |
| PM-2 | Offline install continues with defaults | 1. Disconnect machine from network<br>2. Run `flowforge install`<br>3. Observe warning about manifest being unreachable | Warning is emitted but operation continues with built-in defaults; no hard error or crash | [x] |
| PM-3 | Incompatible engram-dotnet version is blocked | 1. Fork the FlowForge repo<br>2. Edit `install/manifest.yaml` setting `requires.engram-dotnet: ">=99.0.0"`<br>3. Push fork to GitHub<br>4. Run `flowforge update` | Operation is blocked with error: `"Incompatibilidad de versión: engram-dotnet X.X.X no es compatible. Se requiere >=99.0.0."` | [ ] |
| PM-4 | Outdated installer version triggers warning | 1. Set remote `requires.installer: ">=99.0.0"` in manifest<br>2. Run `flowforge install -y`<br>3. Observe warning and confirmation prompt | Warning: `"Este installer (v0.1.0-alpha.1) está desactualizado. Actualizá con: flowforge update --self"`; `-y` flag bypasses the interactive confirm | [ ] |
| PM-5 | Pre-release tag creates pre-release on GitHub | 1. On a test fork, push tag `v0.1.0-rc.1`<br>2. Wait for `release.yml` to complete<br>3. Open the GitHub Release page | Release is marked with `(Pre-release)` badge; assets include `flowforge-linux-x64`, `flowforge-win-x64.exe`, and their `.sha256` files | [ ] |

---

## 5. Open questions for human (OQ-*)

| ID | Tag | Question | Default / assumption |
|----|-----|---------|---------------------|
| OQ-1 | **RESOLVED (CKP-1)** | **Manifest integrity**: ADR-002 notes the manifest is "best-effort" and downloaded over HTTPS. Should the manifest be signed (e.g., with a project public key) to prevent a MITM from serving a modified manifest that lowers `requires.engram-dotnet` to accept an incompatible version? | **Decision: DEFERRED to v0.2.0** — not blocking for alpha; signing added to follow-up list. |
| OQ-2 | **RESOLVED (CKP-1)** | **Self-update command**: ADR-002 describes a `flowforge update --self` workflow, but the current `UpdateCommand` only updates `engram-dotnet`. Should `flowforge update --self` be implemented in v0.1.0-alpha.1, or deferred to a follow-up? | **Decision: DEFERRED** — bootstrap scripts own binary replacement (correct AOT pattern: a running binary cannot safely replace itself due to reentrancy); `UpdateCommand.cs` keeps engram-dotnet-only scope. |
| OQ-3 | **RESOLVED (CKP-1)** | **macOS arm64 binary**: The `release.yml` currently publishes only `linux-x64` and `win-x64`. Is `macos-arm64` (Apple Silicon) in scope for v0.1.0-alpha.1, or deferred? | **Decision: OUT OF SCOPE for v0.1.0-alpha.1** — no local Mac validation capacity; deferred to v0.2.0 or community contribution. |

---

## Memory Signal
- type: none
- significance: low
- summary: "Retrospective spec synthesis — no new architecture decisions; all decisions already captured in ADR-001 and ADR-002."

## CKP-1 Resolution Log (2026-06-23)
- **OQ-1** (manifest signing) → DEFERRED to v0.2.0. Rationale: not blocking for alpha.
- **OQ-2** (`flowforge update --self`) → DEFERRED. Rationale: bootstrap owns binary replacement (correct AOT pattern — a running binary cannot safely self-replace due to reentrancy); `UpdateCommand.cs` keeps engram-dotnet-only scope.
- **OQ-3** (macOS arm64) → OUT OF SCOPE for v0.1.0-alpha.1. Rationale: no local Mac validation capacity; deferred to v0.2.0 or community contribution.

## Follow-ups (post-CKP-1, for v0.2.0+)
- **FU-1**: Add cryptographic signing for `manifest.yaml` to prevent MITM.
- **FU-2**: Add `osx-arm64` matrix entry to `.github/workflows/release.yml` when Mac testing capacity exists (community contribution welcome).
- **FU-3**: Track `flowforge update --self` if/when re-scoping becomes necessary (currently out of scope by design).
- **FU-4** (added 2026-06-23): Add pre-install port-availability check for `engram-dotnet` default port (7437) so the installer fails fast with a clear error if another service is bound there.
- **FU-5** (added 2026-06-23): Add interactive conflict detection for existing `forge-*` agent files in `~/.config/opencode/`, `~/.cursor/agents/`, `~/.vscode/agents/` (current behavior: silent overwrite). For v0.1.0-alpha.1: non-blocking informational warning. For v0.2.0: interactive prompts (overwrite / backup-then-overwrite / skip).
- **FU-6** (added 2026-06-23): Same as FU-5 but for workflow/skill files.
- **FU-7** (added 2026-06-23, post-closure): Create `src/FlowForge.Installer.Tests/` with xUnit coverage for `ManifestClient.FetchAsync` (mock HTTP), compatibility check logic, `Verbosity.FormatError` gating, and bootstrap SHA-256 logic. Tracked from CKP-3 Finding #1 (zero automated tests). Current state: regression surface unprotected.
- **FU-8** (added 2026-06-23, post-closure): Resolve `NFR-ERR-001` self-contradiction (English requirement vs Spanish implementation + `capability_matrix`). Tracked from CKP-3 Finding #2. Either update NFR-ERR-001 to allow the project language, or translate messages to English.
- **FU-9** (added 2026-06-23, post-closure): Component-level install/uninstall/status. Add optional component argument to `flowforge install` / `flowforge uninstall` (e.g. `flowforge install engram-dotnet` to add a single missing component, `flowforge uninstall engram-dotnet` to remove just one), plus `flowforge status --component <name>` for specific queries. Current state: `flowforge install` / `flowforge uninstall` are all-or-nothing; `flowforge` status shows whole table.
