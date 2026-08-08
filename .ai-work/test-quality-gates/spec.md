---
capability_matrix:
  ai_reasoning:
    - "Which assertions to flag as potentially wrong (expected value not in spec)"
    - "Whether an expected value is implementation-derived vs spec-derived"
    - "Which coverage gaps are critical enough to trigger REWORK"
  deterministic:
    - "Assertion validation: expected value must appear in spec constants or be explicitly marked as implementation-derived"
    - "Coverage threshold: ≥80% of git diff lines must be covered by tests"
    - "REWORK trigger: coverage <80% with ≥5 affected lines"
    - "PASS_DEGRADADO: coverage <80% with <5 affected lines"
    - "Fallback: mental checklist enforced when tools unavailable"
---
# Spec: Test Quality Gates for forge-verify

> **Feature slug:** `test-quality-gates`
> **Sources:** [`NS-10`](../../docs/backlog/NS-10-mutation-testing.md) · [`ADR-015`](../../docs/decisions/ADR-015-mutation-testing.md)
> **P0 Scope**: Assertion validation + Coverage gate
> **P1 Scope**: Mutation testing (future)

## 0. Executive Summary

**Objective**: Add two quality gates to forge-verify that catch false-positive tests: (1) assertion validation ensuring test expected values match spec constants, and (2) coverage gate ensuring tests cover git diff lines.

**Problem**: forge-verify passes tests that exist and pass, but doesn't validate that assertions are semantically correct or that tests actually exercise the modified code.

**Solution**: Extend forge-verify with assertion validation (Step 2.5) and coverage gate (Step 3.5). No new tooling required for P0.

**P0 Scope**:
- Assertion/oracle validation: verify test expected values match spec constants
- Coverage gate: verify tests cover ≥80% of git diff lines
- Enhanced fallback: mental checklist enforced when tools unavailable

**Out of scope**:
- Mutation testing (P1, future)
- CI/CD integration
- Custom thresholds per project

---

## 1. Objective and scope

### What
forge-verify currently passes tests that exist and execute green. However, it doesn't verify that:
1. Test assertions use correct expected values (derived from spec, not implementation)
2. Tests actually exercise the code paths in the git diff

This leads to false positives: tests pass but don't catch bugs.

### Solution
Add two quality gates:
- **Assertion validation** (Step 2.5): Verify test expected values match spec constants
- **Coverage gate** (Step 3.5): Verify tests cover ≥80% of git diff lines

### Scope (in)
- Assertion/oracle validation in Step 2.5
- Coverage gate on git diff in Step 3.5
- Enhanced fallback (mental checklist enforced as gate)
- REWORK trigger: coverage <80% with ≥5 affected lines
- PASS_DEGRADADO: coverage <80% with <5 affected lines

### Scope (out)
- Mutation testing (P1, future)
- Configurable thresholds per project (v2)
- CI/CD integration
- Custom mutation operator configuration

## 2. Functional requirements (FR)

### FR-001: Assertion/oracle validation
**forge-verify validates that test assertion expected values match spec constants.**

- **Scenario A — Expected value in spec**
  - Given a test assertion with `expected = "MEDIUM"` for priority
  - When forge-verify validates
  - Then if "MEDIUM" appears in spec.md constants, assertion is valid
  - And no flag is raised

- **Scenario B — Expected value NOT in spec**
  - Given a test assertion with `expected = someHardcodedValue`
  - When forge-verify validates
  - Then if someHardcodedValue does NOT appear in spec.md
  - Then forge-verify flags: "Assertion uses value not in spec. Verify this is intentional."

- **Scenario C — Implementation-derived value**
  - Given a test assertion with `expected = computeFromCode()`
  - When forge-verify validates
  - Then forge-verify warns: "Assertion may be testing implementation, not spec."

### FR-002: Coverage gate on git diff
**forge-verify verifies that tests cover ≥80% of lines modified in git diff.**

- **Scenario A — Good coverage (≥80%)**
  - Given a feature modifies 10 lines across 2 files
  - When forge-verify runs coverage on git diff
  - Then if ≥8 lines are covered by tests
  - Then coverage gate passes

- **Scenario B — Poor coverage triggers REWORK**
  - Given a feature modifies 10 lines across 2 files
  - When forge-verify runs coverage on git diff
  - Then if <8 lines are covered (coverage <80%) AND ≥5 lines affected
  - Then forge-verify issues REWORK with coverage report

- **Scenario C — Poor coverage triggers PASS_DEGRADADO**
  - Given a feature modifies 3 lines in 1 file
  - When forge-verify runs coverage on git diff
  - Then if <3 lines are covered (coverage <80%) AND <5 lines affected
  - Then forge-verify issues PASS_DEGRADADO with warning: "Coverage below threshold but few lines affected."

### FR-003: Enhanced fallback
**When coverage tools are unavailable, forge-verify enforces the mental mutation checklist as a gate.**

- **Scenario A — Tools unavailable, checklist enforced**
  - Given coverage tools are not installed (coverlet/istanbul/--cov)
  - When forge-verify attempts coverage gate
  - Then forge-verify falls back to mental mutation checklist
  - And forge-verify requires developer to demonstrate checklist completion
  - And issues PASS_DEGRADADO with note: "Coverage tools unavailable — mental checklist used."

- **Scenario B — Tools available and working**
  - Given coverage tools are installed and functional
  - When forge-verify runs coverage gate
  - Then coverage report is used directly
  - And no fallback is triggered

### FR-004: Coverage tools per language
**forge-verify auto-detects language and uses appropriate coverage tool.**

- **Scenario A — .NET project**
  - Given project contains .csproj files
  - When forge-verify runs coverage
  - Then uses coverlet (via `dotnet test --collect:"XPlat Code Coverage"`)
  - Or uses `--coverage` flag with coverlet

- **Scenario B — JS/TS project**
  - Given project contains package.json
  - When forge-verify runs coverage
  - Then uses istanbul/nyc or built-in coverage (vitest, jest --coverage)

- **Scenario C — Python project**
  - Given project contains pyproject.toml or setup.py
  - When forge-verify runs coverage
  - Then uses `pytest --cov` with coverage.py

### FR-005: Verdict integration
**Coverage gate results integrate with forge-verify verdict logic.**

- **Scenario A — All gates pass**
  - Given assertion validation passes
  - And coverage gate passes (≥80% on ≥5 lines)
  - When forge-verify issues verdict
  - Then proceeds to normal verdict (PASS, PASS_DEGRADADO, or REWORK based on other checks)

- **Scenario B — REWORK from coverage**
  - Given coverage <80% with ≥5 affected lines
  - When forge-verify issues verdict
  - Then REWORK is issued with coverage report
  - And rework_ticket.md includes coverage gaps

## 3. Non-functional requirements (NFR)

- **NFR-001 — Assertion validation is LLM-only**: No new tooling required. forge-verify uses existing LLM context to validate assertions against spec.

- **NFR-002 — Coverage tools are standard**: coverlet, istanbul, coverage.py are standard tools already used in most projects. No new tooling introduced.

- **NFR-003 — Performance overhead minimal**: Coverage gate adds ~30 seconds to verify phase for typical diff (10-20 lines, 2-3 files).

- **NFR-004 — Fallback is mandatory**: When tools unavailable, mental checklist is NOT optional. Developer must demonstrate completion.

- **NFR-005 — No breaking changes**: Existing forge-verify workflows continue to work. Gates are additive.

- **NFR-006 — Scope limited to git diff**: Only modified files are checked. Full project coverage not required.

## 4. Developer manual tests (PM-*)

| ID | Case / flow | Steps (summary) | Expected result | [x] |
|----|-------------|-----------------|-----------------|-----|
| PM-1 | Assertion validation catches wrong value | 1. Write test with expected value NOT in spec<br>2. Run forge-verify<br>3. Check assertion validation flag | forge-verify flags assertion with "value not in spec" | [ ] |
| PM-2 | Coverage gate catches uncovered lines | 1. Modify 10 lines in 2 files<br>2. Write tests that only cover 6 lines<br>3. Run forge-verify | REWORK issued with coverage <80% on ≥5 lines | [ ] |
| PM-3 | PASS_DEGRADADO for minor coverage gap | 1. Modify 3 lines in 1 file<br>2. Write tests that cover 1 line (33%)<br>3. Run forge-verify | PASS_DEGRADADO with warning (coverage <80% but <5 lines) | [ ] |
| PM-4 | Fallback when tools unavailable | 1. Remove coverage tools from PATH<br>2. Run forge-verify on feature<br>3. Verify mental checklist fallback | PASS_DEGRADADO with "tools unavailable — mental checklist used" | [ ] |
| PM-5 | .NET coverage works | 1. Run forge-verify on .NET project<br>2. Verify coverlet produces coverage report | Coverage gate uses coverlet output | [ ] |

## 5. Open questions for human (OQ-*)

| ID | Tag | Question | Default / assumption |
|----|-----|---------|---------------------|
| OQ-1 | [OPTIONAL] | Should assertion validation flag be a WARN (continues) or ERROR (REWORK)? | **Assumed: WARN** — assertion validation is advisory, not blocking |
| OQ-2 | [OPTIONAL] | Should coverage threshold be configurable per project? | **Assumed: No** for v1 — hardcoded 80% threshold |
| OQ-3 | [FOLLOW-UP] | Should we add mutation testing as P1? | Out of v1 scope — see ADR-015 P1 section |
| OQ-4 | [OPTIONAL] | What minimum lines threshold for PASS_DEGRADADO vs REWORK? | **Assumed: <5 lines → PASS_DEGRADADO, ≥5 lines → REWORK** |

---

## Memory Signal
- type: decision
- significance: high
- summary: "Test quality gates for forge-verify: (1) assertion validation Step 2.5 - verify test expected values match spec constants (advisory WARN), (2) coverage gate Step 3.5 - verify ≥80% of git diff lines covered by tests (REWORK if <80% with ≥5 lines, PASS_DEGRADADO if <5 lines). Enhanced fallback: mental checklist enforced when tools unavailable. No new tooling for P0."
