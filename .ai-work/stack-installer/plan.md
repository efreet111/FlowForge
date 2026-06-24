# Plan: stack-installer

## 1. Plan metadata

- **Feature slug:** `stack-installer`
- **Work item:** ENG-301
- **Branch:** `feat/eng-301-stack-installer`
- **ADRs:** ADR-001, ADR-002
- **Plan mode:** **RETROSPECTIVE** (code already on branch; plan does gap analysis)
- **CKP-1 status:** APPROVED 2026-06-23
- **CKP-2 status:** pending human approval

---

## 2. Gap analysis summary

| ID | Status | Where in code | Notes |
|----|--------|---------------|-------|
| FR-001 | ✅ DONE | `csproj:13`, `release.yml:30-53` | PublishAot=true, linux-x64 + win-x64 matrix |
| FR-002 | ✅ DONE | `install.sh:92-104`, `install.ps1:65-79` | SHA-256 verification before execution |
| FR-003 | ✅ DONE | `ManifestClient.cs:21-54` | 5s timeout, User-Agent header |
| FR-004 | ✅ DONE | `ManifestClient.cs:44-53` | Catches exceptions, returns defaults |
| FR-005 | ✅ DONE | `ManifestClient.cs:86-104`, `InstallCommand.cs:27-33` | Warning + interactive confirm |
| FR-006 | ✅ DONE | `ManifestClient.cs:61-80`, `UpdateCommand.cs:64-68` | Hard block on incompatible |
| FR-007 | ✅ DONE | `release.yml:60-74` | Semver pre-release detection |
| NFR-001 | ✅ DONE | `csproj:13` | PublishAot=true |
| NFR-002 | ✅ DONE | `InstallerConfig.cs:70,121`, `GitHubReleasesClient.cs:177` | JsonSerializerContext source-gen |
| NFR-003 | ✅ DONE | `csproj:40` | Spectre.Console core only, NOT Spectre.Console.Cli |
| NFR-004 | ✅ DONE | `TrimmerRoots.xml` | ConsoleAppFramework preserve |
| NFR-005 | ✅ DONE | `csproj:16` → `true` (commit `634b6a4`) | Spec satisfied |
| RNF-SEC-TAMPER-001 | ✅ DONE | `install.sh:94-104`, `install.ps1:66-79` | SHA-256 checksum verification |
| RNF-SEC-TAMPER-002 | ✅ DONE | `ManifestClient.cs:25` | HTTPS only (signature deferred OQ-1) |
| RNF-SEC-INFO-001 | ✅ DONE | `Program.cs:7-10,41-50`, `Infrastructure/Verbosity.cs` (commit `634b6a4`) | --verbose flag wired end-to-end |
| RNF-SEC-DOS-001 | ✅ DONE | `Program.cs:9` (30s), `ManifestClient.cs:29` (5s) | Timeout enforced |
| RNF-SEC-SPOOF-001 | ✅ DONE | `install.sh:85`, `install.ps1:54` | Direct GitHub URLs |
| RNF-SEC-ELEV-001 | ✅ DONE | `install.sh:108` (755), `install.ps1:90-92` (User scope) | Least privilege |
| NFR-ERR-001 | ✅ DONE | `Program.cs:7-10`, `Infrastructure/Verbosity.cs:26-36` (commit `634b6a4`) | Verbosity.FormatError gates stack traces |
| PM-1 | ➖ PENDING | Not executable at plan time | Requires clean Linux machine |
| PM-2 | ➖ PENDING | Not executable at plan time | Requires offline test |
| PM-3 | ➖ PENDING | Not executable at plan time | Requires manifest fork |
| PM-4 | ➖ PENDING | Not executable at plan time | Requires remote manifest edit |
| PM-5 | ➖ PENDING | Not executable at plan time | Requires test fork + tag push |

---

## 3. Checklist

### 3.1 Code & build

- [x] 1.1 AOT publish with PublishAot=true (`csproj:13`)
- [x] 1.2 SHA-256 verification in bootstrap (`install.sh:94-104`, `install.ps1:66-79`)
- [x] 1.3 NFR-001: AOT compilation constraint (verified in csproj)
- [x] 1.4 NFR-002: Source generators (JsonSerializerContext at `InstallerConfig.cs:70,121`, `GitHubReleasesClient.cs:177`)
- [x] 1.5 NFR-003: Spectre.Console.Cli excluded (`csproj:40`)
- [x] 1.6 NFR-004: TrimmerRoots coverage (`TrimmerRoots.xml`)
- [x] 1.7 NFR-005: InvariantGlobalization should be `true` per spec (`csproj:16` → `true`)

### 3.2 Manifest & compatibility

- [x] 2.1 FR-003: Manifest fetch with 5s timeout (`ManifestClient.cs:21-54`)
- [x] 2.2 FR-004: Best-effort degradation on failure (`ManifestClient.cs:44-53`)
- [x] 2.3 FR-005: Installer version warning (`ManifestClient.cs:86-104`, `InstallCommand.cs:27-33`)
- [x] 2.4 FR-006: Engram-dotnet hard block (`ManifestClient.cs:61-80`, `UpdateCommand.cs:64-68`)

### 3.3 Release pipeline

- [x] 3.1 FR-007: Semver pre-release detection (`release.yml:60-74`)
- [x] 3.2 Matrix: linux-x64 + win-x64 (`release.yml:30-48`)
- [x] 3.3 SHA-256 generation (`release.yml:55-58`)

### 3.4 Security

- [x] 4.1 RNF-SEC-TAMPER-001: SHA-256 verification before execution
- [x] 4.2 RNF-SEC-TAMPER-002: HTTPS manifest fetch (signature deferred OQ-1)
- [x] 4.3 RNF-SEC-INFO-001: --verbose flag for stack trace exposure (`Program.cs:7-10`, `Verbosity.cs`)
- [x] 4.4 RNF-SEC-DOS-001: 5s manifest timeout + 30s HttpClient timeout
- [x] 4.5 RNF-SEC-SPOOF-001: Direct GitHub URLs only
- [x] 4.6 RNF-SEC-ELEV-001: User-scoped PATH, 755 permissions

### 3.5 Error handling

- [x] 5.1 NFR-ERR-001: Error messages in English (Spanish UX per spec capability_matrix)
- [x] 5.2 NFR-ERR-001: --verbose flag to expose stack traces (`Program.cs:7-10`, `Verbosity.cs`)

### 3.6 Manual tests

- [ ] PM-1: Clean Linux install via bootstrap script
- [ ] PM-2: Offline install continues with defaults
- [ ] PM-3: Incompatible engram-dotnet version blocked
- [ ] PM-4: Outdated installer triggers warning
- [ ] PM-5: Pre-release tag creates pre-release

### 3.7 Pre-merge UX (FU-5)

- [x] 1.8 FU-5: pre-install overwrite warning for existing forge-* agent files → commit bfd68ad

---

## 4. Identified gaps

**Status (post-dev pass):** Both gaps CLOSED in commit `634b6a4`. No remaining gaps.

### Gap 1: NFR-005 — InvariantGlobalization should be `true` — **CLOSED**

- Closed in commit `634b6a4`. `csproj:16` flipped to `<InvariantGlobalization>true</InvariantGlobalization>`.
- Acceptance criteria met.

### Gap 2: RNF-SEC-INFO-001 + NFR-ERR-001 — Missing `--verbose` flag — **CLOSED**

- Closed in commit `634b6a4`. New file `src/FlowForge.Installer/Infrastructure/Verbosity.cs` (52 lines) provides `IsVerbose` + `FormatError(message, ex?)`. `Program.cs:7-10` declares the CLI flag; `InstallCommand.cs:30` and `UpdateCommand.cs:64-68` route errors through `Verbosity.FormatError`. Global exception handler at `Program.cs:41-50` gates stack-trace output.
- Acceptance criteria met.

---

## 5. Open questions for CKP-2

### BLOCKER

- None. Both OQ-4 and OQ-5 were resolved by the dev pass (commit `634b6a4`).

### OPTIONAL

- None.

### FOLLOW-UP

- None from the original gap analysis. (FU-4 and FU-5 are CLOSED; they were the 2 BLOCKERs above. Items FU-1, FU-2, FU-3 from `spec.md` remain deferred to v0.2.0+ as documented there.)

---

## 6. Memory Signal

- **type:** bugfix
- **significance:** high
- **summary:** ENG-301 retrospective gap pass closed 2 CKP-2 blockers: InvariantGlobalization csproj:16 (false→true per NFR-005) and a new --verbose flag with `Verbosity.FormatError` wrapper (per RNF-SEC-INFO-001 + NFR-ERR-001). Build clean. Pattern reusable across FlowForge tools.
- **topic_key:** architecture/installer-aot-constraints

---

## Summary Statistics

- **[x] items in section 3:** 19 items (16 from initial 2 commits + 3 closed by commit `634b6a4`)
- **[ ] items in section 3:** 5 items (all PM-* tests pending human/CI execution — cannot be marked [x] at planning or dev time)
- **BLOCKERs for CKP-2:** 0
- **OPTIONAL:** 0
- **FOLLOW-UP:** 0 (FU-4 and FU-5 from initial plan were the 2 BLOCKERs; both closed)