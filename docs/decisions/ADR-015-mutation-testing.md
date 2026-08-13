# ADR-015 — Test Quality Gates for forge-verify

> **Status**: **Accepted — P0 shipped** in PR #22 (`775c678`, 2026-08-07); PM-1..PM-5 manual validation deferred as technical debt (requires real project context + coverage tooling); P1 pending baseline data  
> **Date**: 2026-08-06  
> **Updated**: 2026-08-07 (discovery revealed mutation testing alone insufficient — P0: assertion validation + coverage gate first; P0 shipped, validation deferred)  
> **Feature**: `test-quality-gates` (NS-10)  
> **Deciders**: Engineering (FlowForge methodology team)  
> **Links**: [`NS-10`](../backlog/NS-10-mutation-testing.md) · [`skills/forge-verify/SKILL.md`](../../skills/forge-verify/SKILL.md) · [`skills/forge-dev/testing/SKILL.md`](../../skills/forge-dev/testing/SKILL.md)

---

## Context

Users reported a critical gap in FlowForge's test validation: **tests can pass without actually detecting real bugs**.

**Incident report**:
- `forge-dev` generated unit tests for a feature
- `forge-verify` validated that tests existed and passed
- **But**: there was a use case where tests passed with an incorrect expected variable
- The unit test never activated (false positive)
- Bug existed in production despite "passing" tests

**Critical discovery**: The incident has TWO interpretations requiring DIFFERENT fixes:

| Interpretation | What happened | Correct fix |
|---------------|---------------|-------------|
| **A. Wrong expected value** | Test asserts `wrongExpected == actual` where `wrongExpected` matches buggy output. Assertion is a tautology with the bug. | **Assertion validation** — verify expected values match spec constants |
| **B. Test never activated** | Code path unexecuted (NoCoverage, empty assertion, wrong branch). Bug invisible. | **Coverage gate** — verify tests cover diff lines |

**Mutation testing alone does NOT fix interpretation A**: the test still kills most mutants while asserting wrong values, giving false confidence.

**Current behavior**:
- `forge-verify` checks test existence and execution
- `forge-dev-testing` mentions "mental mutation testing" (lines 105-143) but it's manual and not enforced
- No automated mechanism to validate assertion quality or code coverage

**Desired behavior**:
- Assertion validation catches wrong expected values (fix A)
- Coverage gate catches unexecuted code paths (fix B)
- Mutation testing as additive gate for deeper validation (P1)
- Users can trust that passing tests genuinely validate correct behavior

---

## Decision drivers

- **Test quality assurance**: Passing tests ≠ good tests. We need to validate that tests would catch real bugs.
- **False-positive prevention**: Catch tests that pass despite incorrect expected values or unexecuted code paths
- **Oracle correctness**: Assertion expected values must match spec constants (not implementation)
- **Coverage verification**: Tests must actually execute the code they claim to cover
- **Confidence building**: Users trust passing tests more when gates validate quality
- **Cost-effectiveness**: Cheap fixes first (assertion validation + coverage gate), expensive tools (mutation testing) only when baseline data justifies it
- **Incremental value**: P0 fixes the root cause; P1 adds depth

---

## Options considered

### Option A — Assertion Validation + Coverage Gate (✅ P0 Accepted)

Extend forge-verify Step 2 with assertion/oracle validation and add coverage gate for git diff lines.

**Pros**:
- Directly fixes both interpretations of the incident (wrong expected value + unexecuted code)
- No new tooling required (LLM-only for assertion validation, standard coverage tools)
- Low implementation cost (2-3 days)
- Fast to validate (can test immediately)
- No new dependencies for users

**Cons**:
- Does not detect all mutation-style faults (but mutation testing can add this later as P1)
- Coverage tools vary by language (.NET, JS/TS, Python)

**Decision**: ✅ **Accepted (P0)** — directly fixes the root cause of the incident at low cost.

### Option B — Mutation Testing Only (❌ Rejected for P0)

Implement only mutation testing as the fix for the incident.

**Pros**:
- Comprehensive fault injection validation
- Industry best practice

**Cons**:
- Does NOT fix interpretation A (wrong expected value) — gives false confidence
- High tooling cost and complexity
- Slow execution (+5-15 min per verify)
- High friction for users (tools must be installed)

**Decision**: ❌ **Rejected for P0** — insufficient for the incident, too expensive. Mutation testing can be added as P1 for deeper validation.

### Option C — Mutation Testing as P1 (✅ Accepted for P1)

Add mutation testing as an additive gate after P0 fixes, staged as informational first, then blocking after baseline measurement.

**Pros**:
- Adds depth beyond P0 gates
- Catches mutation-style faults that P0 doesn't catch
- Industry best practice

**Cons**:
- High cost and complexity
- Tool installation friction
- Slow execution

**Decision**: ✅ **Accepted (P1)** — added after P0 baselines are measured and mutation testing value is confirmed.

---

## Decision

**P0: Assertion Validation + Coverage Gate first. Mutation Testing as P1.**

### P0 Implementation (Assertion Validation + Coverage Gate)

#### Rationale

1. **Directly fixes both incident interpretations**: Assertion validation fixes wrong expected values (A); coverage gate fixes unexecuted code (B).

2. **Low cost**: No new tooling required for assertion validation (LLM-only). Coverage tools are standard in all three languages (.NET coverlet, JS/TS istanbul, Python --cov).

3. **Fast to validate**: Can test immediately without waiting for tool installation or baseline measurement.

4. **No new dependencies**: Users don't need to install anything extra.

5. **Clear value**: Both fixes address concrete gaps in the current verification process.

#### Implementation approach

1. **Step 2.5: Assertion/oracle validation** (extend existing Step 2):
   - After verifying constants match spec, verify test assertion expected values match spec constants
   - If test asserts a value not in spec → flag as potential issue
   - If test asserts implementation-derived value → warn (not error)

2. **Step 3.5: Coverage gate** (after test execution):
   - Run coverage on git diff files
   - If diff lines have <80% coverage → REWORK (or PASS_DEGRADADO if <5 lines affected)
   - Tools: coverlet (.NET), istanbul/coverage.js (JS/TS), --cov (Python)

3. **Enhanced fallback**:
   - When coverage tools unavailable → enforce mental mutation checklist as a gate
   - Not optional → forge-dev must demonstrate checklist completion

### P1 Implementation (Mutation Testing) — Future

#### Rationale

1. **Adds depth beyond P0**: P0 fixes the direct incident; mutation testing catches mutation-style faults P0 doesn't.

2. **Staged approach**: Informational first (measure baselines), blocking later (after data supports it).

3. **Industry best practice**: Mutation testing is standard in high-quality codebases.

#### Implementation approach (P1)

1. **Stage 1: Informational only**:
   - Run mutation testing after test execution
   - Generate `mutation-report.md`
   - Issue PASS_DEGRADADO with mutation score (never blocking)

2. **Stage 2: Blocking** (after baseline measurement):
   - 80% threshold with ≥5 survivors → REWORK
   - Tune based on real score distributions

---

## Consequences

### Positive (P0)

- **Directly fixes the incident**: Both interpretations A and B are addressed
- **Low implementation cost**: 2-3 days, no new tooling
- **No dependencies for users**: Standard coverage tools already used in most projects
- **Fast validation**: Can test immediately
- **Enhanced fallback**: Mental checklist becomes a gate, not a suggestion

### Positive (P1, future)

- **Deeper fault detection**: Catches mutation-style faults P0 doesn't
- **Industry best practice**: Mutation testing is standard in high-quality codebases

### Negative (P0)

- **LLM-only assertion validation**: Depends on LLM correctly identifying spec constants
- **Coverage tool variance**: Different tools have different accuracy (.NET coverlet vs JS/TS istanbul vs Python --cov)

### Negative (P1, future)

- **Tool installation friction**: dotnet 10 runtime, fork support, pytest
- **Slow execution**: +5-15 min per verify run
- **False REWORK risk**: Equivalent mutants can cause spurious failures

### Neutral

- **Backward compatible**: Existing forge-verify workflows continue to work
- **P1 only if justified**: Mutation testing added only if P0 proves insufficient

---

## Implementation notes

### P0 Files to modify

- `skills/forge-verify/SKILL.md` — add Step 2.5 (assertion validation) and Step 3.5 (coverage gate)
- `CHANGELOG.md` — add entry for NS-10

### P0 Workflow integration

```
[forge-verify starts]
  → [Step 0: Line-by-line inspection]
  → [Step 1: mem_verify_artifact (LLM-as-Judge)]
  → [Step 2: Constant & Test Case Matching]
     → [Step 2.5: Assertion/oracle validation] ← NEW
       → [Verify test assertion expected values match spec constants]
       → [Flag if test asserts implementation-derived value]
  → [Step 3: Test execution check]
     → [Step 3.5: Coverage gate on git diff] ← NEW
       → [Run coverage on git diff files]
       → [Verify ≥80% coverage on diff lines]
       → [REWORK if <80% coverage with ≥5 lines affected]
  → [Step 4: Capability matrix validation]
  → [Step 5: Final verdict]
```

### P0 Assertion validation logic

```
For each test assertion:
  → Extract expected value from assertion
  → Check if expected value appears in spec.md constants
  → If not in spec:
    → FLAG: "Assertion uses value not in spec. Verify this is intentional."
  → If derived from implementation:
    → WARN: "Assertion may be testing implementation, not spec."
```

### P0 Coverage gate logic

```
Run coverage on git diff files:
  → Get line coverage for each file in git diff
  → If diff lines have <80% coverage:
    → If affected lines ≥5: REWORK
    → If affected lines <5: PASS_DEGRADADO with warning
  → If tools unavailable: enforce mental checklist as fallback gate
```

### P1 (Mutation Testing) — Future

- Add `mutation-report.md` generation
- Integrate Stryker/StrykerJS/mutmut
- Stage 1: informational only (PASS_DEGRADADO)
- Stage 2: 80% threshold with ≥5 survivors → REWORK

---

## Validation plan

### P0 Validation

1. **Assertion validation test**: Create a test with wrong expected value (not in spec). Verify assertion validation flags it.
2. **Coverage gate test**: Create a test that doesn't cover diff lines. Verify coverage gate catches it.
3. **Coverage threshold test**: Create diff with 70% coverage on 10 lines. Verify REWORK triggers.
4. **Fallback test**: Run without coverage tools. Verify mental checklist fallback enforced.
5. **Real project test**: Run on a real project. Verify gates work without false positives.

### P1 Validation (Future)

1. **Mutation score test**: Create weak test (assert true). Verify mutation testing detects it.
2. **Baseline measurement**: Measure mutation scores across 5-10 features.
3. **Threshold tuning**: If scores consistently >95%, raise threshold to 85%.

### Success criteria (P0)

- Assertion validation flags wrong expected values
- Coverage gate catches tests that don't exercise diff lines
- REWORK triggers when coverage <80% with ≥5 affected lines
- Fallback enforced when tools unavailable
- No false positives on well-tested code

---

## Future considerations

- **v2**: Configurable threshold per project in `.flowforge.json`
- **v2**: Mutation testing in forge-dev Ralph Wiggum loop (if performance allows)
- **v2**: Support for additional languages (Go, Rust, Java)
- **v3**: Mutation testing in CI/CD pipelines
- **v3**: Mutation score trending across features (track improvement over time)

---

## References

- User incident: Conversation 2026-08-06 (tests passing with incorrect expected values)
- Related: NS-10 (mutation-testing backlog item)
- Affected skill: `skills/forge-verify/SKILL.md`
- Related skill: `skills/forge-dev/testing/SKILL.md` (mental mutation checklist, lines 105-143)
- Tools: Stryker (.NET), StrykerJS (JS/TS), mutmut (Python)
- Industry examples: Google mutation testing, Meta mutation testing
