# Context Map: Mutation Testing for forge-verify (NS-10)

> **Phase 0 — Discovery (forge-discovery)**
> **Feature slug**: `mutation-testing`
> **Backlog**: [`NS-10`](../../docs/backlog/NS-10-mutation-testing.md)
> **ADR**: [`ADR-015`](../../docs/decisions/ADR-015-mutation-testing.md) (status: Proposed)
> **Date**: 2026-08-07
> **Status**: Context mapping complete

---

## 1. Specification Summary

The user reported an incident: `forge-dev` generated unit tests, `forge-verify` validated them as passing, but a bug existed in production because **the tests passed with an incorrect expected variable** (false positive). The proposed solution (ADR-015) is to integrate mutation testing into `forge-verify` as a step after test execution, with:

- Multi-language: Stryker (.NET), StrykerJS (JS/TS), mutmut (Python)
- Scope limited to files in `git diff HEAD`
- 80% minimum mutation score threshold
- REWORK if score <80% with ≥5 survived mutants; PASS_DEGRADADO if <5
- Timeout guards: 5 min/file, 15 min total
- Fallback: mental mutation checklist (from `forge-dev/testing`)
- Report: `mutation-report.md`

**⚠️ Critical finding (this discovery)**: mutation testing validates that tests *fail when production code changes* — it does **NOT** validate that the *expected values in assertions are correct*. If the reported incident was an assertion comparing against a wrong-but-matching expected value, mutation testing alone would give **false confidence** (the test still kills most mutants). The incident analysis is in §4.

---

## 2. Current Verification Process (Mapped)

### 2.1 forge-verify workflow (`skills/forge-verify/SKILL.md`)

| Step | Action | What it verifies | What it does NOT verify |
|------|--------|------------------|------------------------|
| **0** | Line-by-line inspection | Obvious logic errors, missing returns, empty blocks, debug prints | Semantic correctness, assertion strength |
| **1** | `mem_verify_artifact` (LLM-as-Judge) | Cross-agent eval of code diff vs `spec.md` — mismatches, undeclared constants, incomplete assertions. Cycle control (max 3, CKP-3 🔴) | **Test assertion quality** (reviews production diff, not test semantics) |
| **2** | Constant & Test Case Matching | Code constants match spec exactly; each GWT scenario has ≥1 unit test; Context Map has `## Reusable Patterns Found` | That the test *would fail* if behavior changed; that the test's expected values are correct |
| **3** | Test Execution Check | Runs the test suite; **100% green required for PASS**. Fallback modes A (human pastes output), B (PASS DEGRADADO static-only), C (PENDING) | Whether a green suite is *meaningful* (semantic weakness passes through) |
| **4** | Capability Matrix & Manual Validation | `deterministic` items are hard-coded; emits `## 🔍 Manual Verification Steps` | — |
| **5** | PM-* excluded | Human-run manual tests are NOT graded by the agent | — |
| **Final** | Verdict | PASS / PASS_DEGRADADO / PENDING / REWORK (+ `mem_traceability` on PASS) | — |

### 2.2 Specialized forge-verify skills

| Skill | Trigger | Verifies |
|-------|---------|----------|
| `forge-verify/security` | Always | SAST-style mental audit (auth, authorization, taint), OWASP Top 10, **dependency audit (npm audit / dotnet list package / pip-audit)**, security headers, secrets |
| `forge-verify/complexity` | Dense logic | Cyclomatic complexity (MCC), nesting depth, cognitive load, code smells |
| `forge-verify/performance` | Perf RNFs | N+1, memory leaks, Big-O, benchmark validity |
| `forge-verify/a11y` | UI features | Semantic HTML, ARIA, keyboard, contrast |

### 2.3 The gap that allowed the false positive

**Root cause chain**:
1. Step 2 verifies **production constants** against the spec ("Default: MEDIUM" → code has "MEDIUM").
2. Step 3 verifies the **suite runs green**.
3. **No step verifies that test *assertions* are semantically strong**: that an incorrect expected value in the test would fail against correct behavior, or that the test exercises the branch it claims to cover.
4. `forge-dev/testing` has a *mental* mutation checklist (lines 105–143) — but it's **manual, optional, and not enforced** by any downstream gate.

**Result**: a test asserting `Assert.Equal(wrongExpected, actual)` where `wrongExpected` happens to match the buggy output passes Step 2 (it exists, maps to a GWT), Step 3 (100% green), and Step 1 (spec compliance — the spec says "return X" and the test says "return X"; only the *code* is wrong, and code-vs-test consistency was never checked).

---

## 3. Mutation Testing Analysis

### 3.1 What it is

Mutation testing (fault injection / "test your tests") systematically introduces small defects — **mutants** — into production code (e.g., change `>` to `>=`, invert a boolean, change a literal, delete a statement), then runs the test suite once per mutant:

- **Killed mutant** → the tests caught the change (good).
- **Survived mutant** → tests still pass despite the injected fault → **the tests are weak** at that location.
- **Mutation score** = killed / (killed + survived + timeout + noCoverage).

The mental checklist in `forge-dev/testing` (change `>` to `>=`, `&&` to `||`, remove a statement, return a constant) is exactly the same idea — done by hand. Tools automate it.

### 3.2 Tool facts (researched)

| Aspect | Stryker.NET | StrykerJS | mutmut |
|--------|-------------|-----------|--------|
| Command | `dotnet stryker` (after `dotnet tool install -g dotnet-stryker`) | `npx stryker run` | `mutmut run` (pip) |
| Runtime req | Requires **dotnet 10 runtime** (app itself may target older); NuGet for .NET Framework | Node, uses Babel/TS; needs a test runner plugin (jest/vitest/mocha/karma...) | Requires **fork support** (WSL on Windows); uses pytest |
| Config | `stryker-config.json` | `stryker.config.json` | `setup.cfg` or `[tool.mutmut]` in `pyproject.toml` |
| Scope control | Project/assembly-level; `mutate` option accepts globs | `mutate` option (globs) | Path/function wildcards: `mutmut run "my_module*"`; `only_mutate` / `do_not_mutate` |
| Incremental | Yes (reuse results) | Yes (`incremental` mode) | Yes — remembers results in `mutants/`, only retests changed functions; auto-detects non-Python file changes via git |
| Output | Console progress, mutation score, HTML/JSON reporters | Console + `reporter` config (html, json, dashboard) | Console table + `mutmut browse` TUI; `mutmut show <id>`, `mutmut apply <id>` |
| Per-mutant detail | File:line, operator applied, status | File:line, status, diff | File:line, status; applyable to disk |
| Mutator suppression | `ignore-mutations` config | `disable-mutants` / `// Stryker disable` comments | `# pragma: no mutate`, `# pragma: no mutate block/start/end` |

**Mutant statuses reported**: `Killed`, `Survived`, `Timeout` (mutant hung the suite), `NoCoverage` (code not reached by tests), `CompileError`/`RuntimeError`. The score in ADR-015's formula must treat Timeout and NoCoverage as *surviving* (they indicate weakness).

### 3.3 Interpreting the mutation score

- Score is a **lower bound of test adequacy**: `killed / total`. 80% is a common industry target.
- **NoCoverage mutants are a strong signal** — they point at lines no test touches (exactly the "test never activated" failure mode).
- A score of 100% is practically **unreachable**: Stryker's own docs confirm *equivalent mutants* (mutants that are semantically identical to the original, e.g. `a >= b` vs `a <= b` when `a == b`) cannot be auto-detected, so a surviving mutant may be harmless — and no tool can mark them automatically.

### 3.4 Limitations (critical for the decision)

1. **Does not validate oracle correctness** — the single most important limitation for this incident (see §4).
2. **Time cost** — runs the full suite per mutant; mitigated by incremental mode, parallel workers (StrykerJS), and smart test selection (mutmut). Still, 5 min/file + 15 min total may be too tight for multi-file diffs (a full `dotnet stryker` on a small project typically takes minutes to tens of minutes).
3. **Environment friction** — dotnet 10 runtime, fork support, pytest assumption, per-framework runner plugins.
4. **Scope mismatch with the proposal** — Stryker.NET works at project/assembly level; restricting to `git diff HEAD` files is not a first-class flag and requires `mutate` globs + `--diff` semantics that vary per tool. mutmut mutates only functions *called by tests* by default — files not covered by the suite generate 0 mutants.
5. **Mock-heavy suites** can yield false confidence — mutants in code hidden behind mocks never run.
6. **Flaky tests** produce unreliable scores (a mutant "killed" by a flaky failure is noise).
7. **Equivalent mutants** inflate the survivor count with harmless mutants → score looks lower than real adequacy.
8. Tool install on the user's machine is **assumed**; the fallback (mental checklist) is not enforced.

---

## 4. Gaps Identified (incident-focused)

### 4.1 Did the detection fail, and would mutation testing have caught it?

**Two possible interpretations of the incident** — the fix differs:

| Interpretation | What happened | Would mutation testing catch it? |
|----------------|--------------|----------------------------------|
| **A. Wrong expected value matches buggy output** (e.g., test asserts the same wrong constant the buggy code returns; or `expected` var computed with the same flawed logic) | Assertion passes; behavior is wrong; test is a tautology with the bug | ⚠️ **NOT RELIABLY.** The test still kills most mutants — the mutation score stays high while the assertion is semantically wrong. Score gives **false confidence**. |
| **B. Test never exercised the target code** (NoCoverage / wrong branch / empty assertion / `assert(true)`) | Code path unexecuted → bug invisible to tests; the NS-10 phrase *"the unit test never activated"* points here | ✅ **YES.** Mutants in unexecuted code survive → score drops below threshold → caught. |

**Conclusion**: mutation testing reliably fixes interpretation **B** and partially fixes A only when the wrong expected value is *independent* of the code (then a mutant diverges). For interpretation A, the correct fix is **assertion/oracle validation** (§5, item 1) — mutation testing alone is **insufficient**.

### 4.2 Other gaps in the current process

1. **No assertion-quality gate** — nothing checks that test assertions are non-trivial and their expected values derive from the spec, not from the implementation.
2. **No coverage enforcement** — Step 3 checks "tests exist + pass", not "the diff lines are actually executed". Gap #24 in `docs/15-agent-skills-technical-spec.md` also flags missing RF-XXX traceability in test names.
3. **No real SAST** — `forge-verify/security` is a mental scan; `docs/15` (line 488) flags missing Semgrep/SonarQube integration (lower priority for this NS).
4. **Mental checklist not enforced** — `forge-dev/testing` lines 105–143 describe the technique but nothing blocks a weak suite.
5. **`mem_verify_artifact` reviews the production diff against spec — it never reviews the *tests* against spec** (spec constants → test assertion values).

---

## 5. Comparison: Current vs Mutation Testing

| Aspect | Verificación actual | Con mutation testing |
|--------|--------------------|---------------------|
| ¿Qué verifica? | Spec compliance, constants, GWT presence, green suite, security/complexity/perf/a11y | All of the above + **fault-injection resistance** of the suite |
| ¿Qué NO verifica? | Assertion correctness, code-path execution, "would a bug be caught" | (Same gaps remain:) correct expected values, mock-hidden code, equivalent mutants |
| Tiempo de ejecución | Minutes (suite run + LLM review) | +5–15+ min (per-file/per-mutant suite runs) |
| Costo (tokens/complejidad) | LLM-only; no new deps | Tool install + parsing reports + possibly REWORK cycles; more skill complexity |
| Precisión | High on *intent*, zero on *test strength* | High on *test strength* (kills), zero on *oracle correctness* |
| Falsos positivos | **The reported incident** (green but wrong) | Reduced for NoCoverage/weak-branch cases; NOT for wrong-expected-value |
| Falsos negativos | Misses weak tests entirely | Can over-flag: equivalent mutants, mock-heavy code, tool/env issues → spurious REWORK |

**Net**: mutation testing complements — it does not replace — the missing assertion gate.

---

## 6. Alternative / Complementary Improvements (ranked by ROI)

| # | Improvement | Cost | Effect on incident | Notes |
|---|-------------|------|-------------------|-------|
| 1 | **Assertion/oracle validation in forge-verify Step 2**: verify test expected values against spec constants (e.g., "spec says priority MEDIUM" → test must assert MEDIUM, and the *constant referenced in the test must equal the spec value*). | Low (LLM-only, no tools) | **Directly fixes interpretation A** | Extension of the existing Constant & Test Case Matching step — natural fit, near-zero infra |
| 2 | **Coverage gate on the diff** (line/branch coverage of `git diff HEAD` files via `--cov` / coverlet / istanbul). | Low–Med (well-supported CLIs) | **Directly fixes interpretation B** ("test never activated") at ~1/10th the time cost of mutation testing | Cheapest way to catch NoCoverage |
| 3 | **Enforce the mental mutation checklist** as a forge-verify fallback gate (when tools unavailable) instead of relying on forge-dev to self-report it. | Very low | Partial (interpretation B) | Formalizes existing content; no new deps |
| 4 | **RF-XXX traceability in test names** (gap #24, docs/15) | Very low | Indirect — links tests to requirements | Mechanical grep check |
| 5 | **Mutation testing** (as an add-on gate, not the primary fix) | High (tooling, time, env) | Interpretation B; partial A | Recommended **after** 1–2 |
| 6 | Real SAST (Semgrep) | Med | None (security, not correctness) | Already logged as post-MVP in docs/15 |
| 7 | Property-based / differential testing in forge-dev-testing | Med | Indirect — raises baseline quality | Already partially documented |

---

## 7. Recommendations

1. **Do NOT ship mutation testing as the sole fix for this incident.** The failure mode in the report ("wrong expected variable") is an **oracle problem**, and mutation testing does not validate oracles. Shipping it as the headline fix would add time + tool friction while leaving interpretation A unresolved.

2. **Ship the cheap, targeted fixes first (P0)**:
   - Extend forge-verify Step 2 with **assertion-value validation against spec** (fix A).
   - Add a **diff-coverage gate** (fix B) — same root cause, ~10× cheaper than mutation testing.

3. **Ship mutation testing as an additive, staged gate (P1)**:
   - Integrate into `forge-verify` **after** test execution, as proposed by ADR-015 (Option C is the right placement).
   - **Stage 1: informational/PASS_DEGRADADO only** (report `mutation-report.md`, never auto-REWORK). Tune tooling and thresholds against real projects before making it blocking.
   - **Stage 2 (later):** enforce 80% threshold with ≥5 survivors → REWORK, once score distributions are known (ADR-015's own validation plan already measures this).
   - Keep the fallback **mandatory** (mental checklist enforced as a gate, not a soft suggestion).
   - Treat **NoCoverage mutants as failures**, not neutral.
   - Make the threshold configurable in `.flowforge.json` (v2), and reconsider `git diff HEAD`-only scope: mutating only diff files while running the whole suite is awkward in Stryker.NET (project-level tool) — validate scope per tool before committing to it.
   - Do not raise the threshold above 80%: Stryker's docs confirm equivalent mutants make 100% unreachable.

4. **Trade-offs to accept**:
   - +5–15 min per verify run (or more for .NET); timeout guards are essential.
   - Tool/env friction (dotnet 10 runtime, fork support, pytest); the fallback path will be the *common* path, not the exception, for many users.
   - REWORK over-rejection risk (false REWORK from equivalent mutants/mock-heavy code) → mitigated by Stage 1 informational approach.

5. **Risks introduced**:
   - Developer friction and abandonment if mutation gates block work over false positives.
   - Working-tree corruption if a mutation run is interrupted (mutmut's `apply` explicitly warns: commit before applying; interruption during `run` should auto-restore, but verify this before writing the skill).
   - Score gaming: teams may add mutants-suppressing comments (`// Stryker disable`, `# pragma: no mutate`) to pass gates.

---

## 8. Reusable Patterns Found

> Mandatory section (ADR-003 / forge-discovery skill). Search terms: ["mutation", "test quality gate", "external tool invocation", "verification fallback", "coverage"].

- **`skills/forge-verify/security/SKILL.md` (dependency audit, lines 72–90)** — existing pattern for "invoke an external verification CLI → parse output → map to verdict rules → auto-fail triggers". **Clone this shape** for mutation tool invocation (run → parse score/survivors → verdict rules → fallback). This is the strongest reusable pattern in-repo.
- **`skills/forge-verify/SKILL.md` (fallback modes A/B/C, lines 48–72)** — existing pattern for "primary tool unavailable → degrade gracefully". **Reuse** for the mutation-tools-unavailable fallback.
- **`skills/forge-verify/SKILL.md` (Step 2 Constant & Test Case Matching)** — the natural extension point for assertion-value validation (recommendation §7). Extend, don't create new skill.
- **`skills/forge-dev/testing/SKILL.md` (lines 105–143)** — the mental mutation checklist already exists; promote it into the fallback gate.
- **`docs/15-agent-skills-technical-spec.md` (§6 forge-verify function catalog + gaps §10/#24)** — prior spec of the verify process; documents the same gap independently (line 417: "no hay `testing/cobertura_mutation_tool()` real"). Reuse as baseline for updating the catalog after the change.
- **No existing mutation-tool integration in the repo** (negative result — nothing to clone). Confirmed by grep: no `stryker`/`mutmut` config or scripts under `skills/`, `docs/`, `src/`.

---

## 9. Relevant Prior Memories & Epics

### Memory search (Attempt B — local fallback; MCP `mem_search` unavailable in this session)

- **Result**: negative for mutation testing. `.engram/local_memory/` (7 observations) contains no entry on test quality, mutation testing, or the 2026-08-06 incident. The incident postdates the newest local snapshot (2026-07-15).
- **Closest adjacent**: `obs-20260715-pii-scanner-json-aware.md` (8/8 unit tests PASS pattern — evidences the "green suite ≠ correct" theme at the engram-dotnet level, PM-5 failure caught only by manual review).

### Epics / topic keys

- No existing epic or `topic_key` for test-quality/verification strength. This NS effectively opens a new topic: **`verification/test-quality`**.
- Adjacent architecture decisions: ADR-003 (pattern-search mandate — relevant because the verify gate for `## Reusable Patterns Found` is enforced here), ADR-013/ADR-001 (memory quality — same "quality over existence" philosophy applied to *tests*).

### FlowDoc context

- PRD: `docs/PRD.md` (read: yes — §1–2, ecosystem configurator for AI agents; test-quality is an implicit PRD concern under "coding skills: curated best-practice patterns").
- HU referenced: NS-10 — *Mutation Testing for forge-verify* (`docs/backlog/NS-10-mutation-testing.md`).
- HU flowforge_slug: `mutation-testing` (current, this feature).

---

## 10. Security & Compliance Assessment

> Loaded: `forge-discovery-security` (new dev-time dependencies introduced) and `forge-discovery-cost`.

### Security Assessment
- **Dependencies reviewed**: `dotnet-stryker` (Apache-2.0, actively maintained, requires dotnet 10 runtime), `@stryker-mutator/*` (Apache-2.0, actively maintained), `mutmut` (MIT, actively maintained, requires fork support).
- Critical CVEs: 0 known for these dev-time tools (no production deployment).
- **Risk**: running mutation tools mutates source on disk. If a run is interrupted, the working tree could retain mutated code. **Mitigation required in the skill**: document that mutation runs must start from a clean `git status` and that tools auto-restore after interruption (verify per tool); never `mutmut apply` in verify.
- Past security issues in this stack: none in local memory.
- Verdict: ✅ SAFE (with the clean-tree mitigation documented).

### Cost Assessment
- Cloud/infra cost: **$0** — no new services, storage, or APIs. Pure local dev-time tooling.
- Non-monetary cost: verify-phase wall-clock (+5–15 min), token cost of parsing `mutation-report.md`, and developer time on tool setup (dotnet tool / npm / pip).
- Cost verdict: ✅ LOW (infra) / 🟡 MEDIUM (time budget — the 15-min guard is a hard cap by design).

---

## 11. Constraints

1. **CKP-0 🔴**: requirements are well-defined (NS-10 + ADR-015 exist with acceptance criteria) — no hard stop needed. **CLEAR.**
2. Must not modify production code in this phase (discovery is analysis-only).
3. `git diff HEAD` scope limitation is a design assumption that must be validated per tool during Phase 1 (see §7).
4. Fixes must preserve forge-verify's existing fallback modes A/B/C and CKP-3 cycle control.
5. Git: no push without explicit request (`.agents/rules/git-sin-push.md`).
6. ADR-015 references are inconsistent with `docs/15` (line 816: "PIT (.NET), MutPy (Python)" vs ADR-015 "Stryker.NET, mutmut") — **reconcile during Phase 1**; this discovery's research confirms ADR-015's tool choices (Stryker.NET/mutmut are the current maintained tools; PIT/MutPy are older).

---

## 12. Memory Signal

- **type**: decision
- **significance**: high
- **summary**: Incident "tests pass with wrong expected variable" is an *oracle* problem that mutation testing alone cannot fix (tests still kill mutants while asserting wrong values). Recommended: (1) add assertion-value validation vs spec to forge-verify Step 2 (cheap, fixes oracle), (2) add diff-coverage gate (cheap, fixes NoCoverage), (3) add mutation testing as an additive informational gate (PASS_DEGRADADO-first), staged to blocking after score baselines are known. Tools confirmed: Stryker.NET (needs dotnet 10), StrykerJS, mutmut (needs fork support). 80% threshold realistic; 100% impossible (equivalent mutants).
- **topics**: `verification/test-quality`, `mutation-testing`

---

*Discovery complete. Handoff to `forge-arch` (CKP-1) with the recommendation to open the spec by prioritizing §7 items 1–2 (assertion validation + coverage gate) before the mutation-testing gate.*
