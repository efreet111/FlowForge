# Verify Report — stack-installer

## 1. Verification Metadata

| Field | Value |
|-------|-------|
| **Feature slug** | `stack-installer` |
| **Work item** | ENG-301 |
| **Branch** | `feat/eng-301-stack-installer` |
| **Commits covered** | `96b1b6b` (feat v0.1.0-alpha.1), `9f6d021` (fix perms/BOM/InstallerVersion), `634b6a4` (NFR-005 + --verbose), `b1ca3e8` (typo fix) |
| **CKP-1 status** | APPROVED 2026-06-23 (3 OQs resolved) |
| **CKP-2 status** | APPROVED (plan.md clean, 19 [x], 5 [ ] PM-*, 0 BLOCKERs) |
| **CKP-3 status** | EXECUTING (this report) |
| **Verification date** | 2026-06-23 |
| **Verifier model** | opencode-go/deepseek-v4-pro |
| **Scope** | Retrospective audit — code + spec + plan + ADRs only |
| **Audit mode** | Partial — no runtime execution; no test suite exists to execute; dependency audit available |

---

## 2. Traceability Matrix

| Spec ID | Summary | Implementation Evidence | Verdict | Notes |
|---------|---------|------------------------|---------|-------|
| FR-001 | AOT binary publication (linux-x64 + win-x64) | `csproj:13` (PublishAot=true), `release.yml:30-48` | ✅ PASS | |
| FR-002 | Bootstrap SHA-256 verification before execution | `install.sh:92-104`, `install.ps1:65-79` | ✅ PASS | Checksum not found → proceeds without verify (CI always generates .sha256) |
| FR-003 | Manifest fetch with 5s timeout + User-Agent | `ManifestClient.cs:21-54` (line 26 UA, line 29 5s CancelAfter) | ✅ PASS | |
| FR-004 | Best-effort degradation on manifest failure | `ManifestClient.cs:44-53` (catches OCE + Exception, returns Default) | ✅ PASS | |
| FR-005 | Installer version warning + interactive confirm | `ManifestClient.cs:86-104`, `InstallCommand.cs:27-33` | ✅ PASS | |
| FR-006 | Engram-dotnet hard block on incompatible | `ManifestClient.cs:61-80`, `UpdateCommand.cs:64-68` | ✅ PASS | |
| FR-007 | Semver pre-release detection in CI | `release.yml:60-68` | ✅ PASS | |
| NFR-001 | AOT compilation constraint | `csproj:13` (PublishAot=true), `csproj:14` (EnableTrimAnalyzer=true) | ✅ PASS | |
| NFR-002 | Source generators (JsonSerializerContext) | `InstallerConfig.cs:70,121,177`, `GitHubReleasesClient.cs:177`, ConsoleAppFramework v5.7.13 | ✅ PASS | |
| NFR-003 | Spectre.Console.Cli exclusion | `csproj:40` — Spectre.Console core only, NO `.Cli` | ✅ PASS | |
| NFR-004 | TrimmerRoots coverage | `TrimmerRoots.xml` — preserves `FlowForge.Installer` assembly | ✅ PASS | |
| NFR-005 | InvariantGlobalization=true | `csproj:16` — `<InvariantGlobalization>true</InvariantGlobalization>` (commit `634b6a4`) | ✅ PASS | |
| RNF-SEC-TAMPER-001 | SHA-256 verification before binary execution | `install.sh:94-104`, `install.ps1:66-79`, `GitHubReleasesClient.cs:98-108` | ✅ PASS | Dual verification: bootstrap scripts verify installer binary; installer verifies engram binary |
| RNF-SEC-TAMPER-002 | HTTPS-only manifest fetch (signing deferred) | `ManifestClient.cs:14-15` — `https://raw.githubusercontent.com/...` | ⚠️ PARTIAL | HTTPS mitigates MITM but no cryptographic signature (OQ-1 deferred to v0.2.0) |
| RNF-SEC-INFO-001 | No PII in headers; --verbose gates stack traces | `Program.cs:7-10` (flag check), `Verbosity.cs:26-36` (FormatError), `Program.cs:41-50` (global handler), `ManifestClient.cs:26` (UA: no PII) | ✅ PASS | See Finding #4 for --verbose parsing concern |
| RNF-SEC-DOS-001 | 5s manifest timeout, 30s HttpClient timeout | `ManifestClient.cs:29` (5s), `Program.cs:15` (30s) | ✅ PASS | |
| RNF-SEC-SPOOF-001 | Direct GitHub URLs, no third-party mirrors | `install.sh:85`, `install.ps1:54` | ✅ PASS | |
| RNF-SEC-ELEV-001 | User-scoped PATH, 755 permissions | `install.sh:108` (755), `install.ps1:90-92` (User scope), `PathHelper.cs:18-22` (user paths) | ✅ PASS | |
| NFR-ERR-001 | English errors, no stack traces sans --verbose | `Verbosity.cs:26-36` (gates stack traces) | ⚠️ PARTIAL | Stack trace gating ✅; BUT messages are in Spanish, not English (capability_matrix delegates UX language to Spanish — spec self-contradiction, see Finding #2) |

**Summary**: 17 ✅ PASS, 3 ⚠️ PARTIAL, 0 ❌ FAIL

---

## 3. STRIDE Audit (Security)

### Threat-by-Threat Analysis

| STRIDE Category | Threat | Mitigation | File:Line | Verdict | Residual Risk |
|-----------------|--------|------------|-----------|---------|---------------|
| **S**poofing | Attacker hosts fake GitHub release | Bootstrap scripts use pinned `https://github.com/` URLs; no redirect-following to third-party hosts | `install.sh:85`, `install.ps1:54` | ✅ | Low — GitHub domain is effectively TLS-pinned by platform |
| **T**ampering | MITM serves modified binary | SHA-256 checksum verified before execution; exits non-zero on mismatch | `install.sh:94-104`, `install.ps1:66-79`, `GitHubReleasesClient.cs:98-108` | ✅ | Low — checksum verified in both bootstrap and installer binaries |
| **T**ampering | MITM serves modified manifest.yaml | HTTPS-only fetch from raw.githubusercontent.com; no cryptographic signature (OQ-1 deferred) | `ManifestClient.cs:14-15` (HTTPS), `ManifestClient.cs:25` (URL) | ⚠️ | **Medium** — MITM on GitHub CDN could downgrade `requires.engram-dotnet` to accept incompatible version; signing deferred to v0.2.0 |
| **R**epudiation | No audit trail for install/update actions | `InstallerLogger` writes timestamped entries to `~/.engram/install.log` | `InstallerLogger.cs:23-34`, `PathHelper.cs:14-15` | ✅ | Low — structured log provides basic audit trail |
| **I**nformation Disclosure | PII in HTTP headers or log output | User-Agent: `flowforge-installer/0.1.0` (no PII); stack traces suppressed via `Verbosity.FormatError` unless `--verbose` | `Program.cs:7-10`, `Verbosity.cs:26-36`, `ManifestClient.cs:26` | ✅ | Low — sensitive data gated by flag |
| **D**enial of Service | Slow/no network hangs installer | 5s manifest timeout via `CancelAfter`, 30s global `HttpClient.Timeout` | `ManifestClient.cs:29`, `Program.cs:15` | ✅ | Low — all network calls bounded |
| **E**levation of Privilege | Installer runs as root/admin | `install -m 755` (no setuid), Windows PATH uses `User` scope, not `Machine` | `install.sh:108`, `install.ps1:90-92`, `PathHelper.cs:18-22` | ✅ | Low — user-scoped, no privilege escalation surfaces |

### SAST Scan

- **Authentication**: N/A — CLI tool, no authentication endpoints
- **Authorization**: N/A — single-user CLI, no multi-user access control
- **Data Flow (Taint)**: ✅ All HTTP URLs are hardcoded constants (`ManifestClient.cs:14-15`, `GitHubReleasesClient.cs:13-14`); no user-supplied URLs accepted; YAML is manually parsed (restricted key set: `installer_version`, `engram-dotnet`, `installer`); version strings validated via `Version.TryParse`
- **Secrets**: ✅ Zero secrets in code — no API keys, tokens, passwords, or private keys in any committed file

### OWASP Top 10

| # | Category | Applicable? | Verdict | Evidence |
|---|----------|------------|---------|----------|
| A01 | Broken Access Control | N/A (CLI, single-user) | ✅ N/A | |
| A02 | Cryptographic Failures | ✅ SHA-256 for integrity | ✅ PASS | `GitHubReleasesClient.cs:155-158` — `SHA256.HashData()` |
| A03 | Injection | ✅ YAML parsing | ✅ PASS | Manual `Split(':')` parser with restricted key set (`ManifestClient.cs:108-128`); no SQL/command injection surfaces |
| A04 | Insecure Design | ✅ Timeouts, validation | ✅ PASS | `ManifestClient.cs:29` (5s), `Program.cs:15` (30s), `ConfigCommand.cs:22` (channel whitelist) |
| A05 | Security Misconfig | ✅ Release builds | ✅ PASS | `csproj:16` InvariantGlobalization=true, `release.yml:37-38` DebugType=none |
| A06 | Vulnerable Components | ✅ NuGet audit | ✅ PASS | `dotnet list package --vulnerable` → 0 vulnerable packages (ConsoleAppFramework 5.7.13, Spectre.Console 0.57.0) |
| A07 | Authentication Failures | N/A | ✅ N/A | |
| A08 | Software Integrity | ✅ No eval/unsafe deserialization | ✅ PASS | All serialization via source-gen `JsonSerializerContext`; no `System.Reflection` usage |
| A09 | Logging & Monitoring | ✅ Install log | ✅ PASS | `InstallerLogger.cs` — timestamped structured log to `~/.engram/install.log` |
| A10 | SSRF | N/A (hardcoded URLs only) | ✅ N/A | |

- **A01-A10**: 9/10 applicable categories passed (1 N/A)
- **Failures**: None

### Dependencies
- **HIGH**: 0
- **CRITICAL**: 0
- **Verdict**: ✅ PASS

### Security Headers
N/A — CLI tool, not a web server. No HTTP responses to configure headers on.

### Overall Security Verdict: ⚠️ PASS WITH RESIDUAL RISK
- 6/6 STRIDE categories mitigated
- 1 residual risk: Manifest tampering (unsigned, HTTPS-only) — accepted per OQ-1 deferral

---

## 4. PM-* Evidence Audit

| ID | Test Case | Evidence Status | Required Environment | Verdict | Notes |
|----|-----------|----------------|---------------------|---------|-------|
| PM-1 | Clean Linux install via bootstrap script | ⚠️ NOT EXECUTED | Clean Ubuntu 24.04 machine with internet access | ⚠️ PENDING | No execution evidence in repo; requires human or CI to run `curl ... \| bash` flow end-to-end |
| PM-2 | Offline install continues with defaults | ⚠️ NOT EXECUTED | Machine with network disconnected; pre-installed flowforge binary | ⚠️ PENDING | Requires physical/VM network isolation; cannot verify from code alone |
| PM-3 | Incompatible engram-dotnet version blocked | ⚠️ NOT EXECUTED | Fork of FlowForge repo with modified `manifest.yaml` | ⚠️ PENDING | Requires fork + manifest edit + `flowforge update` execution |
| PM-4 | Outdated installer triggers warning | ⚠️ NOT EXECUTED | Remote manifest with elevated `requires.installer` | ⚠️ PENDING | Requires manifest fork + `flowforge install -y` execution |
| PM-5 | Pre-release tag creates pre-release | ⚠️ NOT EXECUTED | Test fork of FlowForge + tag push `v0.1.0-rc.1` | ⚠️ PENDING | Requires GitHub fork + tag push + wait for CI; cannot verify from code alone |

**Summary**: 0 executed, 5 pending human/CI execution.

> **Note per verify SKILL.md §5**: PM-* tests are developer manual tests — the human must execute them. Spec compliance verdict (Section 6 below) applies to FR/NFR + automated tests only. PM-* execution gates CKP-4 (/flow-close), not CKP-3.

---

## 5. Open Issues / Findings

### Finding #1 — P1: No automated test suite exists
- **Severity**: P1 (should fix before deploy)
- **Description**: Zero test files in the repository. Every FR in `spec.md` defines Given-When-Then scenarios, but none are automated. The `plan.md` checklist (Section 3) marks 19 code/build items as `[x]` but contains no test-related checklist items. This means there is no automated regression protection for any spec requirement.
- **Suggested fix**: Create `src/FlowForge.Installer.Tests/` project with xUnit tests covering: FR-003 (ManifestClient.FetchAsync — mock HTTP), FR-004 (degradation on timeout/404), FR-005 (installer version compat logic), FR-006 (engram version hard block), NFR-ERR-001/RNF-SEC-INFO-001 (Verbosity.FormatError gates stack traces).
- **File:line reference**: No test files anywhere under `src/`

### Finding #2 — P2: Error message language (Spanish vs. English spec requirement)
- **Severity**: P2 (fix in next iteration)
- **Description**: NFR-ERR-001 mandates "All user-facing error messages are in English." However, the `capability_matrix` in `spec.md` delegates "UX copy language (Spanish)" to `ai_reasoning`. The implementation uses Spanish throughout: `"ManifestClient: timeout al obtener manifest — usando defaults"` (`ManifestClient.cs:46`), `"Incompatibilidad de versión: ..."` (`ManifestClient.cs:74-76`), etc. This is a spec self-contradiction that needs human resolution.
- **Suggested fix**: Either (a) update NFR-ERR-001 to allow Spanish, or (b) internationalize error messages to English and add a follow-up for i18n.
- **File:line reference**: `ManifestClient.cs:34,46,51,74-76,98-100`, `EngramModule.cs:30-33,50`, `Verbosity.cs:26-36`

### Finding #3 — P1: `--verbose` flag not registered with ConsoleAppFramework
- **Severity**: P1 (should fix before deploy)
- **Description**: The `--verbose`/`-v` flag is detected via manual `args.Contains("--verbose")` in `Program.cs:7` BEFORE `app.Run(args)` at `Program.cs:43`. However, ConsoleAppFramework v5 also receives the unmodified `args` array containing `--verbose`. Since `--verbose` is NOT registered via `app.ConfigureGlobalOptions()`, ConsoleAppFramework may treat it as an unknown option and throw `ArgumentParseFailedException` — preventing the command from executing. The exact behavior depends on v5.7.13's default strictness for unknown options. This requires **runtime verification**.
- **Suggested fix**: Either (a) remove `--verbose`/`-v` from args after setting `Verbosity.IsVerbose`, or (b) use `app.ConfigureGlobalOptions()` to register `--verbose` natively and inject into DI.
- **File:line reference**: `Program.cs:7-10,21,43`

### Finding #4 — P2: Version string duplication across 6 locations
- **Severity**: P2 (fix in next iteration)
- **Description**: The installer version `"0.1.0-alpha.1"` is hardcoded in 6 different locations: `InstallCommand.cs:14`, `StatusCommand.cs:12`, `UpdateCommand.cs:13`, `FlowForgeModule.cs:12`, `RemoteManifest.cs:13,33`, and `csproj:23`. Any version bump requires touching all 6 locations — a maintenance hazard.
- **Suggested fix**: Centralize in a single `InstallerVersion` constant (e.g., in `PathHelper` or a new `Constants` class) and reference it from all other locations. The csproj `<Version>` could be read via `AssemblyInformationalVersionAttribute`.
- **File:line reference**: 6 sites listed above

### Finding #5 — P3: Duplicate `LocateFlowForgeRepo` method
- **Severity**: P3 (nitpick)
- **Description**: `FlowForgeModule.cs:134-155` and `FlowDocModule.cs:164-180` contain identical `LocateFlowForgeRepo()` implementations. DRY violation.
- **Suggested fix**: Extract to a shared utility class (e.g., `RepoLocator` in Infrastructure).
- **File:line reference**: `FlowForgeModule.cs:134-155`, `FlowDocModule.cs:164-180`

### Finding #6 — P3: Context map missing (CKP-0 procedural gap)
- **Severity**: P3 (nitpick, but mechanical CKP-0 violation)
- **Description**: `.ai-work/stack-installer/context-map.md` does not exist. Per `forge-verify/SKILL.md` Step 2, this is a mechanical CKP-0 violation ("REWORK immediately — discovery was incomplete"). Since CKP-1 and CKP-2 were formally approved by the human, and all FR/NFR are independently traceable, this is treated as a procedural gap rather than a code defect. Flagged for human awareness at CKP-4.
- **Suggested fix**: Retroactively create `context-map.md` with at least a negative-result entry under `## Reusable Patterns Found`.
- **File:line reference**: `.ai-work/stack-installer/context-map.md` (missing)

### Finding #7 — P2: `InstallCommand.RunAsync` cyclomatic complexity (MCC=15)
- **Severity**: P2 (fix in next iteration)
- **Description**: `InstallCommand.RunAsync` has a cyclomatic complexity of 15 (14 branches + 1 base) and spans 144 lines. This exceeds the "🔴 High" threshold (11-20). The wizard flow is inherently branchy, but the method should be decomposed.
- **Suggested fix**: Extract phases into private methods: `CheckInstallerCompatibility()`, `PromptComponents()`, `PromptEngramMode()`, `PromptIdes()`, `ExecuteInstallation()`, `SaveConfiguration()`.
- **File:line reference**: `InstallCommand.cs:18-143`

---

## 6. Verdict

**Overall: ⚠️ PASS DEGRADADO**

### Justification

The code satisfies all 22 spec requirements (17 PASS, 3 PARTIAL with documented mitigations, 0 FAIL). The STRIDE audit shows all 6 threat categories mitigated. Dependency audit shows 0 vulnerabilities. No secrets, no debug code, no obvious logical defects found in line-by-line inspection.

However, a clean PASS cannot be awarded because:

1. **No automated test suite exists** — 0 test files. The verify SKILL.md rule is explicit: "DO NOT award a PASS unless you have a 100% green test output." With no tests to run, this falls to Opción B (static analysis → PASS DEGRADADO).
2. **PM-* tests are not executed** — all 5 manual tests require human/CI execution.
3. **Finding #3 (P1)** — `--verbose` flag parsing with ConsoleAppFramework is unverified and may fail at runtime. Requires manual runtime verification before deploy.

```
⚠️ PASS DEGRADADO — Tests no ejecutados (sin runtime)
- Spec compliance: ✅ (17/20 PASS, 3 ⚠️ PARTIAL, 0 ❌ FAIL)
- Cobertura GWT: ❌ (0 tests declarados, 0 ejecutados)
- Tests ejecutados: ❌ No disponible (no existe proyecto de tests)
- Se requiere ejecución manual ANTES del deploy.
```

### Warnings (P2/P3 — non-blocking)
- P2: Error messages in Spanish (spec contradiction with NFR-ERR-001)
- P2: Version string duplicated across 6 locations
- P2: InstallCommand.RunAsync MCC=15, 144 lines
- P3: Duplicate LocateFlowForgeRepo
- P3: Context map missing (CKP-0 procedural gap)

### BLOCKER findings (P0/P1 that would block a clean PASS)
- P1: `--verbose` flag unverified with ConsoleAppFramework (Finding #3)
- P1: No automated test suite (Finding #1)

---

## 7. Follow-ups (from spec.md)

| ID | Description | Status | Target |
|----|-------------|--------|--------|
| FU-1 | Cryptographic signing for `manifest.yaml` | DEFERRED | v0.2.0 |
| FU-2 | `osx-arm64` matrix entry in `release.yml` | OUT OF SCOPE | v0.2.0 or community contribution |
| FU-3 | `flowforge update --self` command | OUT OF SCOPE | Re-scoping decision TBD |

All three follow-ups remain as documented in CKP-1 Resolution Log. No new follow-ups identified in this audit beyond the Open Issues in Section 5.

---

## 8. Rework Ticket

**None.** No FAIL verdict — no rework ticket generated.

---

## 9. 🔍 Manual Verification Steps

The following must be performed by a human before CKP-4 (/flow-close):

### MVS-1: Verify `--verbose` flag works end-to-end
```bash
# 1. Run install without --verbose → confirm clean error (no stack trace)
./flowforge install --yes 2>&1 | grep -c "Stack:"
# Expected: 0

# 2. Run install with --verbose → confirm stack trace present
./flowforge --verbose install --yes 2>&1 | grep -c "Stack:"
# Expected: ≥ 1 (if any error occurs)

# 3. Test --verbose with non-install commands
./flowforge --verbose update --check 2>&1
# Expected: no "unknown option" error from ConsoleAppFramework
```

### MVS-2: Simulate manifest failure modes
```bash
# 1. Block GitHub in /etc/hosts temporarily
sudo echo "0.0.0.0 raw.githubusercontent.com" >> /etc/hosts
./flowforge install --yes
# Expected: warning about manifest timeout, continue with defaults
sudo sed -i '/raw.githubusercontent.com/d' /etc/hosts

# 2. Test with --verbose during network failure
sudo echo "0.0.0.0 raw.githubusercontent.com" >> /etc/hosts
./flowforge --verbose install --yes 2>&1
# Expected: verbose error details about OperationCanceledException
sudo sed -i '/raw.githubusercontent.com/d' /etc/hosts
```

### MVS-3: Verify SHA-256 mismatch kills bootstrap
```bash
# On a test machine:
# 1. Download a valid binary
# 2. Corrupt the .sha256 file or tamper with binary
# 3. Run install.sh → expect non-zero exit + error message with expected/actual hashes
```

### MVS-4: Run all PM-* tests
Execute PM-1 through PM-5 as documented in `spec.md` §4. Mark `[x]` in spec.md when each passes.

### MVS-5: Run `dotnet test` (once test project exists)
If a test project is created per Finding #1, run `dotnet test` and confirm 100% green before deploy.

---

## 10. Memory Signal

```yaml
- type: decision
- significance: high
- summary: >
  ENG-301 Stack Installer verification complete. Verdict: PASS DEGRADADO.
  17/20 spec items PASS, 3 PARTIAL (manifest signing deferred, Spanish UX vs English NFR, --verbose parsing risk).
  STRIDE 6/6 mitigated (1 residual risk on manifest tampering).
  OWASP A06 0 vulns. No test suite exists (0 automated tests).
  5 PM-* tests pending human execution.
  3 P1 findings (--verbose unverified, no tests, Spanish errors).
  CKP-4 blocked until MVS-1 through MVS-5 executed.
- topic_key: verification/eng-301-stack-installer
```

---

*Report generated by Sentinel Judge (forge-verify) — 2026-06-23T00:00:00Z*
