# NS-10: Test Quality Gates for forge-verify

> **Status:** Proposed  
> **Priority:** P0 — High (prevents false-positive tests, improves test quality)  
> **Created:** 2026-08-06  
> **Updated:** 2026-08-07 (discovery revealed mutation testing alone is insufficient — added P0 assertion validation + coverage gate)  
> **Related:** User report of tests passing with incorrect expected values, forge-dev-testing mental mutation checklist (lines 105-143)  
> **ADR:** [`ADR-015`](../decisions/ADR-015-mutation-testing.md)

---

## 🎯 Problem

### Current situation

`forge-dev` generates unit tests correctly, and `forge-verify` validates that tests exist and pass. However, there's a gap: **tests can pass without actually detecting real bugs**.

### User report (incident)

- `forge-dev` generated unit tests for a feature
- `forge-verify` validated that development was correct
- **But**: there was a use case where tests passed with an incorrect expected variable
- The unit test never activated (false positive)
- Bug existed in production despite "passing" tests

### Root cause (discovery finding)

**Critical**: The incident has TWO possible interpretations that require DIFFERENT fixes:

| Interpretation | What happened | Correct fix |
|---------------|---------------|-------------|
| **A. Wrong expected value** | Test asserts `wrongExpected == actual` where `wrongExpected` matches buggy output. Assertion passes but is semantically wrong. | **Assertion validation** — verify test expected values match spec constants |
| **B. Test never activated** | Code path unexecuted (NoCoverage, empty assertion, wrong branch). Bug invisible to tests. | **Coverage gate** — verify tests cover diff lines |

Mutation testing alone only fixes interpretation B reliably. For interpretation A, mutation testing gives **false confidence** (test still kills most mutants while asserting wrong values).

### Impact

| Metric | Current | After P0 fixes | After P1 (mutation) |
|--------|---------|----------------|---------------------|
| Assertion correctness | Unchecked | ✅ Validated | ✅ Validated |
| Code coverage (diff) | Unchecked | ✅ Gate enforced | ✅ Gate enforced |
| False-positive tests | Undetected | ✅ Caught (A+B) | ✅ Enhanced |
| Mutation score visibility | None | N/A | ✅ Reported |

---

## 📋 Scope

### P0 — Assertion Validation & Coverage Gate (this implementation)

1. **Assertion/oracle validation** in forge-verify Step 2:
   - Verify test assertion expected values match spec constants
   - Extend existing "Constant & Test Case Matching" step
   - No new tooling required (LLM-only)
   - Directly fixes interpretation A (wrong expected value)

2. **Coverage gate on git diff**:
   - Verify tests cover lines in `git diff HEAD`
   - Use `--cov` (Python), coverlet (.NET), istanbul/coverage.js (JS/TS)
   - Gate: if diff lines have <80% coverage → REWORK or PASS_DEGRADADO
   - Directly fixes interpretation B (test never activated)

3. **Enhanced fallback**:
   - When tools unavailable, enforce mental mutation checklist as a gate
   - Not optional — forge-dev must demonstrate checklist completion

### P1 — Mutation Testing (future, after P0 baseline)

4. **Mutation testing** as add-on gate:
   - Stryker (.NET), StrykerJS (JS/TS), mutmut (Python)
   - Stage 1: informational only (PASS_DEGRADADO, never blocking)
   - Stage 2: 80% threshold with ≥5 survivors → REWORK (after baseline measurement)
   - Fallback: mental mutation checklist

### Out of scope

- CI/CD integration (local execution only for v1)
- Custom mutation operator configuration per project
- Mutation testing in forge-dev Ralph Wiggum loop (too slow)
- Real-time mutation testing during development

---

## ✅ Tasks

### Phase 1: Design (P0) (0.5 day)

- [ ] Update NS-10 with new scope (assertion validation + coverage gate first, mutation as P1)
- [ ] Update ADR-015 with discovery findings
- [ ] Define assertion validation logic (what to check, how to check)
- [ ] Define coverage gate thresholds and tools per language

### Phase 2: Implementation P0 (2-3 days)

- [ ] Create branch `feat/test-quality-gates`
- [ ] Modify `skills/forge-verify/SKILL.md`:
  - [ ] Add Step 2.5: Assertion/oracle validation
  - [ ] Add Step 3.5: Coverage gate on git diff
  - [ ] Add enhanced fallback (enforce mental checklist as gate)
- [ ] Test assertion validation with sample specs
- [ ] Test coverage gate with sample projects (.NET, JS/TS, Python)
- [ ] Update `CHANGELOG.md` → [Unreleased] → item for NS-10

### Phase 3: Validation P0 (1 day)

- [ ] Create test case with wrong expected value → verify assertion validation catches it
- [ ] Create test case with NoCoverage → verify coverage gate catches it
- [ ] Verify REWORK verdict triggers correctly
- [ ] Verify enhanced fallback works when tools unavailable
- [ ] Collect user feedback on gates usefulness

### Phase 4: Documentation & Merge P0 (0.5 day)

- [ ] Update `docs/14-flowforge-complete-reference.md` with new gates
- [ ] Update `docs/04-roadmap.md` to mark P0 as done
- [ ] Push branch and create PR
- [ ] **CKP-1**: Human reviews and approves approach
- [ ] **CKP-2**: Human reviews SKILL.md changes and approves merge
- [ ] Merge P0 to `main`

### Phase 5: P1 Design (Mutation Testing) (1 day) — Future

- [ ] Based on P0 baseline data, design mutation testing integration
- [ ] Define Stage 1 (informational only) vs Stage 2 (blocking)
- [ ] Choose tools and thresholds based on measured distributions

### Phase 6: P1 Implementation (3-4 days) — Future

- [ ] Modify `skills/forge-verify/SKILL.md`:
  - [ ] Add mutation testing step (Stage 1: informational)
  - [ ] Add tool invocation (Stryker/StrykerJS/mutmut)
  - [ ] Add scope limitation (git diff HEAD files only)
  - [ ] Add threshold enforcement (80% minimum)
  - [ ] Add timeout guards
  - [ ] Add mutation-report.md generation
- [ ] Test with .NET project (Stryker)
- [ ] Test with JS/TS project (StrykerJS)
- [ ] Test with Python project (mutmut)

### Phase 7: P1 Validation (1-2 days) — Future

- [ ] Measure mutation score distribution across features
- [ ] Tune thresholds based on real data
- [ ] If score distribution supports it → upgrade to Stage 2 (blocking)

### Phase 8: P1 Documentation & Merge (1 day) — Future

- [ ] Update documentation with mutation testing
- [ ] Push and create PR
- [ ] Merge to `main`

### Phase 9: Closure (CKP-4) — Future

- [ ] `forge-memory` closes NS-10 with `summary.md`:
  - Link to merged PR
  - Metrics: assertion validation catches, coverage gate catches, mutation scores
- [ ] Update ADR-015 status from "Proposed" → "Accepted"
- [ ] Update NS-10 status from "Proposed" → "Done"

---

## 🔗 Cross-references

- **User incident**: Conversation 2026-08-06 (tests passing with incorrect expected values)
- **Related skill**: `skills/forge-dev/testing/SKILL.md` (lines 105-143, mental mutation testing)
- **Affected skill**: `skills/forge-verify/SKILL.md`
- **Tools**: Stryker (.NET), StrykerJS (JS/TS), mutmut (Python)
- **Future consideration**: Add mutation testing to forge-dev Ralph Wiggum loop (if performance allows)

---

## 🧪 How to validate success

Once merged to `main`, validate in the next 2-4 weeks:

1. **Mutation score tracking**: What's the average mutation score across features?
   - Expected: 80-90% for well-tested features
   - Red flag: <70% indicates weak tests
2. **False-positive detection**: How many weak tests are caught?
   - Expected: 1-2 per feature on average
3. **REWORK rate**: How often does mutation testing trigger REWORK?
   - Expected: 10-20% of features (not too high, not too low)
4. **Performance impact**: Does mutation testing add significant overhead?
   - Expected: <20% increase in verify phase duration
5. **User confidence**: Do users trust passing tests more?
   - Expected: Positive feedback on mutation score visibility

If after 3 months:
- Mutation score is consistently >95% → threshold might be too low, consider raising to 85%
- Mutation score is consistently <70% → either tests are genuinely weak OR threshold is too high
- REWORK rate is >40% → investigate if mutation operators are too aggressive

---

## 📅 Dates

- **2026-08-06**: Trigger (user incident report) + creation of this NS
- **Pending**: ADR-015 creation
- **Pending**: Implementation (3-4 days estimated)
- **Pending**: PR and merge
- **Pending**: Closure (CKP-4)

---

## 💡 Effort estimate

### P0 (Assertion Validation + Coverage Gate)

| Phase | Tasks | Effort |
|-------|-------|--------|
| Phase 1 (Design P0) | 4 tasks | 0.5 day |
| Phase 2 (Implementation P0) | 6 tasks | 2-3 days |
| Phase 3 (Validation P0) | 5 tasks | 1 day |
| Phase 4 (Documentation P0) | 4 tasks | 0.5 day |
| **P0 Subtotal** | **19 tasks** | **4-5 days** |

### P1 (Mutation Testing) — Future

| Phase | Tasks | Effort |
|-------|-------|--------|
| Phase 5 (Design P1) | 3 tasks | 1 day |
| Phase 6 (Implementation P1) | 8 tasks | 3-4 days |
| Phase 7 (Validation P1) | 3 tasks | 1-2 days |
| Phase 8 (Documentation P1) | 3 tasks | 1 day |
| **P1 Subtotal** | **17 tasks** | **6-8 days** |

### Total

| Scope | Tasks | Effort |
|-------|-------|--------|
| P0 only | 19 tasks | 4-5 days |
| P0 + P1 | 36 tasks | 10-13 days |

---

## 🎯 Acceptance criteria

### P0 (Assertion Validation + Coverage Gate)

- [ ] Assertion validation in Step 2: test expected values validated against spec constants
- [ ] Coverage gate: diff lines verified to have ≥80% test coverage
- [ ] Coverage gate works for .NET (coverlet), JS/TS (istanbul/coverage.js), Python (--cov)
- [ ] REWORK verdict triggers when diff lines have <80% coverage
- [ ] Enhanced fallback enforced when tools unavailable (not optional)
- [ ] User feedback: gates catch real issues

### P1 (Mutation Testing) — Future

- [ ] Mutation testing runs after test execution (Stage 1: informational only)
- [ ] Language auto-detection works for .NET, JS/TS, Python
- [ ] 80% mutation score threshold (Stage 2 only, after baseline measurement)
- [ ] mutation-report.md generated in `.ai-work/{feature-slug}/`

---

## ⚠️ Risks and mitigations

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| Mutation tools not installed | High | Medium | Fallback to mental checklist |
| Mutation testing too slow | Medium | High | Timeout guards + scope limitation |
| False positives from mutation | Low | Medium | 80% threshold + 5 mutant minimum |
| Tool incompatibility | Low | High | Test with multiple projects before merge |
| User resistance to REWORK | Medium | Low | Clear documentation of value |

---

## 📚 References

- Stryker (.NET): https://stryker-mutator.io/docs/stryker-net/Introduction
- StrykerJS: https://stryker-mutator.io/docs/stryker-js/introduction
- mutmut (Python): https://mutmut.readthedocs.io/
- forge-dev-testing mental mutation checklist: `skills/forge-dev/testing/SKILL.md` lines 105-143
