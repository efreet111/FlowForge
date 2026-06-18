# Verify Report: open-source-contributor-experience

> **Verdict**: ❌ **FAIL** — Rework ticket issued  
> **Cycle**: 1/3  
> **Agent**: forge-verify (Sentinel Judge)  
> **Date**: 2026-06-12  
> **CKP-3**: 🟡 Cycle 1 — no emergency brake yet (threshold: 3)

---

## 1. Audit Scope

7 files audited (5 new + 2 modified):

| File | Type | Status |
|------|------|--------|
| `.github/ISSUE_TEMPLATE/bug_report.md` | NEW | ✅ Pass |
| `.github/ISSUE_TEMPLATE/feature_request.md` | NEW | ✅ Pass |
| `.github/ISSUE_TEMPLATE/config.yml` | NEW | ✅ Pass |
| `.github/PULL_REQUEST_TEMPLATE.md` | NEW | ✅ Pass |
| `.github/workflows/opencode-smoke.yml` | NEW | ❌ **2 CRITICAL bugs** |
| `CONTRIBUTING.md` | MODIFIED | ⚠️ 1 non-blocking issue |
| `docs/17-improvement-plan-specs.md` | MODIFIED | ✅ Pass |
| `docs/04-roadmap.md` | MODIFIED | ✅ Pass |

---

## 2. Findings (CRITICAL — Blocking)

### 🔴 F1: CI Step 2 — Agent count mismatch causes false failure

**File**: `.github/workflows/opencode-smoke.yml`, lines 24–28

```bash
count=$(jq '.agent | length' ide/opencode/opencode.flowforge.json)
if [ "$count" -ne 7 ]; then
  echo "ERROR: Expected 7 subagents, found $count"
  exit 1
fi
```

**Reality**: `opencode.flowforge.json` has **8 entries** under `.agent` — 1 primary orchestrator (`flowforge`) + 7 subagents (`forge-discovery`, `forge-arch`, `forge-plan`, `forge-dev`, `forge-verify`, `forge-memory`, `forge-teacher`).

**Impact**: CI fails on **every valid config** because `.agent | length` returns 8 ≠ 7. The CI would block all PRs even when the config is correct.

**Evidence**:
```json
"agent": {
  "flowforge":       { "mode": "primary" },    // ← counted by CI
  "forge-discovery": { "mode": "subagent" },
  ...
  "forge-teacher":   { "mode": "subagent" }
}
// Total: 8 keys. CI expects 7. FAIL.
```

**Fix**: Either expect `-ne 8` or filter by `mode == "subagent"`:
```bash
count=$(jq '[.agent[] | select(.mode == "subagent")] | length' ide/opencode/opencode.flowforge.json)
```

---

### 🔴 F2: CI Step 3 — Escaped `$skill` in grep produces false green

**File**: `.github/workflows/opencode-smoke.yml`, lines 36 and 39

```bash
path=$(jq -r ".agent.\"$skill\".prompt" ... | grep -o "{file:__FLOWFORGE_REPO__/skills/\$skill/SKILL.md}" | head -1)
#                                                                                       ^^^^^^
#                                                              literal string "$skill" — never matches
```

**What happens**: Inside bash double-quotes, `\$skill` escapes the `$`, producing the literal string `$skill` instead of expanding to e.g. `forge-discovery`. grep searches for literal `$skill` in the prompt text — which never appears — and returns empty. The fallback on line 39 has the exact same bug.

**Consequence**: `path` is always empty. `[ -n "$path" ]` is always false. The `if` block is **never entered**. The CI loop prints nothing and exits 0 → **false green**: CI reports success but never validates a single skill path.

**Evidence (tested locally)**:
```
$ skill="forge-discovery"
$ prompt=$(jq -r '.agent."forge-discovery".prompt' ide/opencode/opencode.flowforge.json)
$ echo "$prompt" | grep -o "{file:__FLOWFORGE_REPO__/skills/\$skill/SKILL.md}"
# → (empty — no match)

$ echo "$prompt" | grep -o "{file:__FLOWFORGE_REPO__/skills/$skill/SKILL.md}"
# → {file:__FLOWFORGE_REPO__/skills/forge-discovery/SKILL.md}  ← correct
```

**Fix**: Remove the backslash before `$skill` in both grep invocations:
```bash
grep -o "{file:__FLOWFORGE_REPO__/skills/$skill/SKILL.md}"        # line 36
grep -oE "\{file:[^}]+skills/$skill/SKILL\.md\}"                  # line 39
```

---

## 3. Findings (Non-Blocking — Warning)

### ⚠️ F3: CONTRIBUTING.md — Broken relative link

**File**: `CONTRIBUTING.md`, line 53

```markdown
[PR template](../.github/PULL_REQUEST_TEMPLATE.md)
```

From repo root (`CONTRIBUTING.md` location), `../` resolves **outside the repository**.  
Correct link: `[PR template](.github/PULL_REQUEST_TEMPLATE.md)` (single dot, no `../`).

**Impact**: Link is dead on GitHub when `CONTRIBUTING.md` is rendered at repo root level.

---

## 4. FR Traceability Matrix

| FR | Requirement | File(s) | Status |
|----|-------------|---------|--------|
| **FR-001-A** | Bug report template (OS/IDE/install mode, repro steps, expected/actual, logs) | `.github/ISSUE_TEMPLATE/bug_report.md` | ✅ All fields present, English, labels configured |
| **FR-001-B** | Feature request template (problem, solution, alternatives, FlowForge phase) | `.github/ISSUE_TEMPLATE/feature_request.md` | ✅ All fields present, English, checkbox phases |
| **FR-001-C** | Issue config — blank issues disabled, contact links to Discussions | `.github/ISSUE_TEMPLATE/config.yml` | ✅ `blank_issues_enabled: false`, contact link present |
| **FR-002** | PR template with 4-item checklist + description + breaking changes | `.github/PULL_REQUEST_TEMPLATE.md` | ✅ All 4 items, breaking changes section |
| **FR-003** | CI workflow — validates JSON, 7 agents, skill paths, no placeholders | `.github/workflows/opencode-smoke.yml` | ❌ **F1 + F2** — count check fails, path check silent-skipped |

---

## 5. NFR Traceability Matrix

| NFR | Requirement | Status | Evidence |
|-----|-------------|--------|----------|
| **NFR-001** | Templates in English | ✅ | All 3 templates + PR template: English frontmatter, English body, English labels |
| **NFR-002** | CI < 2 min (structure) | ✅ | 5 lightweight bash/jq steps, no deps install, no compilation |
| **NFR-003** | CI uses `$GITHUB_WORKSPACE` | ✅ | Lines 42–43: `${path//__FLOWFORGE_REPO__/$GITHUB_WORKSPACE}` |
| **NFR-004** | PR template checklist `[ ]` syntax | ✅ | All 4 items use `- [ ]` GitHub-compatible markdown |
| **NFR-005** | CONTRIBUTING.md references templates + CI | ✅ | Issue Templates (§42–49), PR Template (§51–62), CI Smoke Test (§64–73) — all present |

---

## 6. Automated Validations

| Check | Result |
|-------|--------|
| YAML syntax (`opencode-smoke.yml`) | ✅ Valid |
| YAML syntax (`config.yml`) | ✅ Valid |
| JSON syntax (`opencode.flowforge.json`) | ✅ Valid |
| CI has 3+ steps | ✅ 5 steps |
| 7 subagents referenced in CI | ✅ Hardcoded for loop |
| All 7 SKILL.md paths exist on disk | ✅ All resolve |
| CI triggers: push to main + pull_request | ✅ Both configured |

---

## 7. PM Tests Status (Manual — Human Required)

These are NOT evaluated by forge-verify. They must be executed by the human developer before `/flow-close`.

| PM | Description | Status in spec |
|----|-------------|----------------|
| PM-1 | Issue template renders correctly in GitHub | `[X]` marked in spec |
| PM-2 | PR checklist visible on PR creation | `[X]` marked in spec |
| PM-3 | CI smoke passes with valid config | `[X]` marked in spec |
| PM-4 | CI fails gracefully with missing skill | `[X]` marked in spec |
| PM-5 | Blank issues blocked | `[X]` marked in spec |

> ⚠️ **Note**: PM-3 cannot pass in current state due to F1 (CI fails on valid config). PM-4 cannot be verified because F2 causes path checks to silently skip. Re-test after rework.

---

## 8. Documentation Updates

| File | Expected Change | Actual | Status |
|------|----------------|--------|--------|
| `CONTRIBUTING.md` | Add Issue Templates, PR Template, CI Smoke sections | ✅ All 3 sections present | ⚠️ Broken link (F3) |
| `docs/17-improvement-plan-specs.md` | Item 1: add CI/manual split notes | ✅ Lines 22–23: CI validates structure + manual validates runtime | ✅ |
| `docs/04-roadmap.md` | Item 1: add CI smoke coverage note | ✅ Line 105: "**CI validates config structure**" | ✅ |

---

## 🔒 Security Audit

### SAST Scan
- **Authentication**: N/A — no endpoint exposed; CI uses public `actions/checkout@v4` (trusted)
- **Authorization**: N/A — no protected resources
- **Data Flow (Taint)**: N/A — no user input processed; CI runs `jq` on repo files only
- **Secrets**: ✅ No secrets, tokens, or credentials in any file. `$GITHUB_WORKSPACE` is a runtime env var, not a secret.

### OWASP Top 10
- A01–A10: N/A — this is a CI + documentation feature with no runtime application surface
- **Dependencies**: 0 HIGH, 0 CRITICAL (no package dependencies)

### Overall Security: ✅ PASS (no security surface)

---

## 🧠 Complexity Audit

All files are declarative (Markdown templates, YAML config, shell script). No functions exceed complexity thresholds.

| File | MCC | Nesting | Lines | Verdict |
|------|-----|---------|-------|---------|
| `opencode-smoke.yml` (step 3) | 4 | 2 | 19 | ✅ Low |
| `bug_report.md` | N/A (template) | N/A | 36 | ✅ Readable |
| `feature_request.md` | N/A (template) | N/A | 30 | ✅ Readable |

### Overall Complexity: ✅ PASS

---

## ⚡ Performance Audit

| RNF | Analysis | Verdict |
|-----|----------|---------|
| NFR-002 (< 2 min) | 4 bash steps (jq × 3 + grep × 1), no network calls beyond checkout | ✅ Theoretically < 1s for this repo size |

---

## 🏁 Final Verdict

**Verdict: FAIL**

Rework required for:
- 🔴 **F1**: CI agent count (8 ≠ 7) — false failure on valid configs
- 🔴 **F2**: CI grep `\$skill` → silent skip of all path validation — false green
- ⚠️ **F3**: `CONTRIBUTING.md` broken relative link (nice-to-fix, not blocking)

`rework_ticket.md` generated at `.ai-work/open-source-contributor-experience/rework_ticket.md`.

---

## 🔍 Manual Verification Steps (Post-Rework)

After rework, before re-submission for re-verification:

1. Run `jq '.agent | length' ide/opencode/opencode.flowforge.json` — should match CI expectation
2. Simulate CI step 3 locally: verify grep extracts paths correctly for all 7 agents
3. Click the PR template link in `CONTRIBUTING.md` rendered on GitHub (or markdown preview)
4. Run full CI locally via `act` or push a test PR to verify green pipeline

---

*Generated by forge-verify (Sentinel Judge) — 2026-06-12*
