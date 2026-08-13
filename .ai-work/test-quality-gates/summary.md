# Summary — Test Quality Gates for forge-verify (NS-10)

> **Closed:** 2026-08-07
> **Verdict:** ✅ SHIPPED — PR #22 merged to main (`775c678`), pushed to origin
> **PM tests:** 0/5 executed — **all deferred** as technical debt (human decision; require real project context, coverage tooling, and test scenarios)
> **Rework cycles:** 0
> **Deliverable:** Merge of `feat/test-quality-gates` → `main` (assertion validation + coverage gate in forge-verify)

---

## 1. What was delivered

PR #22 (`feat/test-quality-gates` → `main`, merge commit `775c678`) shipped the P0 scope of NS-10 / ADR-015:

### 🔬 Test Quality Gates (P0)

1. **Assertion/oracle validation (Step 2.5)** — forge-verify validates that test assertion expected values match spec constants:
   - Expected value in spec → valid, no flag
   - Expected value NOT in spec → flag: "Assertion uses value not in spec. Verify this is intentional."
   - Implementation-derived value → warn: "Assertion may be testing implementation, not spec."
2. **Coverage gate on git diff (Step 3.5)** — forge-verify verifies tests cover ≥80% of modified git diff lines:
   - Coverage <80% with ≥5 affected lines → **REWORK** with coverage report
   - Coverage <80% with <5 affected lines → **PASS_DEGRADADO** with warning
3. **Enhanced fallback** — when coverage tools are unavailable, the mental mutation checklist is enforced as a mandatory gate (not optional); PASS_DEGRADADO notes "tools unavailable — mental checklist used"
4. **Coverage tool auto-detection per language**: coverlet (.NET), istanbul/nyc or vitest/jest `--coverage` (JS/TS), `pytest --cov` (Python)

### Files affected

| Area | Files |
|------|-------|
| Core skill | `skills/forge-verify/SKILL.md` (Step 2.5 + Step 3.5) |
| IDE parity | `ide/cursor/agents/forge-verify.md`, `ide/opencode/agents/forge-verify.md`, `ide/opencode/templates/agents/forge-verify.md.tpl`, `ide/vscode/agents/forge-verify.agent.md`, `.opencode/agents/forge-verify.md` |
| Docs | `docs/decisions/ADR-015-mutation-testing.md`, `docs/backlog/NS-10-mutation-testing.md` |
| Changelog | `CHANGELOG.md` (NS-10 entry under `[Unreleased] → Added`, kept alongside NS-09 entry after conflict resolution) |

**Out of scope (deferred/future):** mutation testing (P1, ADR-015), configurable thresholds (v2), CI/CD integration.

---

## 2. Developer Manual Tests

| PM | Test | Status |
|----|------|--------|
| PM-1 | Assertion validation catches wrong value | ⏸️ Deferred — requires development context |
| PM-2 | Coverage gate catches uncovered lines | ⏸️ Deferred — requires development context |
| PM-3 | PASS_DEGRADADO for minor coverage gap | ⏸️ Deferred — requires development context |
| PM-4 | Fallback when tools unavailable | ⏸️ Deferred — requires development context |
| PM-5 | .NET coverage works (coverlet) | ⏸️ Deferred — requires development context |

> ⚠️ **Decision (human, 2026-08-07):** PM-1..PM-5 cannot be run without additional development work — they require a **real project context** (a sample project to verify against), **installed coverage tools** (coverlet/istanbul/`--cov`), and **crafted test scenarios** (wrong expected values, partially covered diffs). These prerequisites do not exist in the FlowForge methodology repo itself. **Closure authorized with merge as deliverable; PM-* deferred as technical debt** (documented in `spec.md` §4).

---

## 3. Key Learnings

### Pattern: CHANGELOG merge conflict resolution

1. **Keep BOTH entries when two branches append to `[Unreleased] → Added`.** NS-09 (Executive Summary) and NS-10 (Test Quality Gates) both appended to CHANGELOG.md; resolving the conflict by keeping both entries (rather than dropping one) preserved both shipped features' changelog records.
2. **Pre-merge integration reduces conflict surface.** Merging `main` into the feature branch first (`9812eb4`) meant the final PR merge (`775c678`) touched only CHANGELOG.md — one file needed manual resolution instead of a multi-file conflict storm.

### Requirement: PM-* test validation prerequisites

3. **PM-* manual tests for tooling/methodology changes cannot run inside the methodology repo itself.** Tests that exercise forge-verify's assertion validation and coverage gate need an external host project with: (a) a spec to validate against, (b) real code + a git diff, (c) tests with deliberately wrong expectations, and (d) coverage tools on PATH. When a feature changes *verification tooling* rather than *product code*, plan PM-* execution against a fixture/demo project from the start — or explicitly defer and track as technical debt, as done here.

### Other

4. **Gates are additive and backward compatible** (NFR-005): existing forge-verify workflows continue unchanged; assertion validation is advisory (WARN, per OQ-1 assumption) and only coverage below threshold with ≥5 lines escalates to REWORK.

---

## 4. Metrics

| Metric | Value |
|--------|-------|
| **PR / merge** | #22 → `775c678` (main), pushed to origin |
| **Manual conflicts** | 1 (CHANGELOG.md — resolved keeping both entries) |
| **FR coverage** | 5/5 ✅ (FR-001..FR-005 specified; implementation shipped in skill + parity files) |
| **NFR compliance** | 6/6 ✅ (LLM-only, standard tools, ~30s overhead, mandatory fallback, no breaking changes, diff-scoped) |
| **PM tests** | 0/5 — ⏸️ deferred (technical debt) |
| **Rework cycles** | 0 |
| **Test coverage delta** | N/A — methodology/docs feature, no product code |

---

## 5. Open Items (non-blocking)

| Item | Type | Details |
|------|------|---------|
| PM-1..PM-5 manual validation | 🟠 Technical debt | Requires real project context + coverage tools + crafted test scenarios. Tracked in `spec.md` §4. Run before next feature using forge-verify gates. |
| Mutation testing (P1) | 🔵 Future | ADR-015 P1 — staged informational-first, blocking after baseline data. Requires dotnet 10/Stryker, StrykerJS, mutmut. |
| Configurable coverage thresholds | 🔵 Future | v2 — per-project threshold in `.flowforge.json` |

---

## 6. Memory Signal

- **type:** decision
- **significance:** high
- **summary:** "Test quality gates for forge-verify shipped via PR #22 (NS-10, P0, ADR-015): (1) assertion/oracle validation — test expected values must match spec constants, advisory WARN; (2) coverage gate — ≥80% of git diff lines covered, REWORK if <80% with ≥5 lines, PASS_DEGRADADO if <5 lines; (3) mental-checklist fallback enforced as a gate when coverage tools unavailable; (4) per-language coverage tools (coverlet/istanbul/--cov). Feature CLOSED with merge as deliverable — PM-1..PM-5 manual tests DEFERRED as technical debt (require real project context, coverage tooling, and test scenarios). Pattern: when two branches both append to CHANGELOG [Unreleased]→Added, keep BOTH entries; pre-merging main into the feature branch minimizes final merge conflicts. P1 follow-up: mutation testing (staged)."

---

## 7. Closure Status

| Gate | Status |
|------|--------|
| 🔴 PM-* all [x] | ⏸️ **Deferred by human decision** (technical debt) — 0/5 executed, documented in spec.md §4 |
| 🟡 Rework open | ✅ Pass — no rework tickets |
| 🟢 Artifacts in place | ✅ Pass — spec.md (+ deferred note), summary.md |
| 🔵 CHANGELOG updated | ✅ Pass — NS-10 entry under [Unreleased], kept alongside NS-09 |
| 🔵 ADR promotion | ✅ Pass — ADR-015 shipped with merge; status updated to implemented (P0) |
| 🟢 **CKP-4** | ✅ **Deploy Gate complete — feature closed with deferred PM-* tracked as technical debt** |
