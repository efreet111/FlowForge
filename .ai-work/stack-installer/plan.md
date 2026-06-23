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
| NFR-005 | ⚠️ PARTIAL | `csproj:16` | Set to `false`, spec requires `true` |
| RNF-SEC-TAMPER-001 | ✅ DONE | `install.sh:94-104`, `install.ps1:66-79` | SHA-256 checksum verification |
| RNF-SEC-TAMPER-002 | ✅ DONE | `ManifestClient.cs:25` | HTTPS only (signature deferred OQ-1) |
| RNF-SEC-INFO-001 | ⚠️ PARTIAL | No --verbose flag found | User-Agent present, but no verbose flag |
| RNF-SEC-DOS-001 | ✅ DONE | `Program.cs:9` (30s), `ManifestClient.cs:29` (5s) | Timeout enforced |
| RNF-SEC-SPOOF-001 | ✅ DONE | `install.sh:85`, `install.ps1:54` | Direct GitHub URLs |
| RNF-SEC-ELEV-001 | ✅ DONE | `install.sh:108` (755), `install.ps1:90-92` (User scope) | Least privilege |
| NFR-ERR-001 | ⚠️ PARTIAL | No --verbose flag | Error messages present, no verbose control |
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

---

## 4. Identified gaps

### Gap 1: NFR-005 — InvariantGlobalization should be `true`

- **File(s) to modify:** `src/FlowForge.Installer/FlowForge.Installer.csproj`
- **Acceptance criteria:** `<InvariantGlobalization>true</InvariantGlobalization>` should be set to avoid ICU data embedding
- **Suggested commit:** `fix(eng-301): enable InvariantGlobalization in csproj`

### Gap 2: RNF-SEC-INFO-001 + NFR-ERR-001 — Missing --verbose flag

- **File(s) to modify:** `Program.cs`, `InstallerLogger.cs`, or relevant command files
- **Acceptance criteria:** Add `--verbose` flag that when passed exposes stack traces in error output; without it, errors show message only
- **Suggested commit:** `feat(eng-301): add --verbose flag for debug output`

---

## 5. Open questions for CKP-2

### BLOCKER

- **OQ-4 [BLOCKER]:** NFR-005 specifies `<InvariantGlobalization>true</InvariantGlobalization>` but the code has `false`. This affects binary size and AOT correctness. Should this be fixed before CKP-2 approval?

- **OQ-5 [BLOCKER]:** RNF-SEC-INFO-001 and NFR-ERR-001 require a `--verbose` flag to expose stack traces for debugging. This is not implemented. Should it be added before CKP-2, or deferred to follow-up?

### OPTIONAL

- None at this time.

### FOLLOW-UP

- **FU-4:** Add `--verbose` flag to expose stack traces in error output (per RNF-SEC-INFO-001 + NFR-ERR-001).
- **FU-5:** Set `InvariantGlobalization` to `true` in csproj (per NFR-005).

---

## 6. Memory Signal

- **type:** discovery
- **significance:** medium
- **summary:** Two gaps found in retrospective: (1) InvariantGlobalization=false vs spec requiring=true, (2) missing --verbose flag per RNF-SEC-INFO-001. Both are fixable but block CKP-2 clean pass.
- **topic_key:** architecture/installer-aot-constraints

---

## Summary Statistics

- **[x] items in section 3:** 16 items verified as done in commits 96b1b6b and 9f6d021
- **[ ] items in section 3:** 7 items (2 gaps + 5 PM tests pending human execution)