---
capability_matrix:
  ai_reasoning:
    - PR checklist wording and UX phrasing (tone, warmth)
    - Issue template labels and descriptions (contributor-facing language)
    - CI step ordering and error message clarity
  deterministic:
    - opencode.flowforge.json must be valid JSON (parseable by jq/yq)
    - All `{file:__FLOWFORGE_REPO__/skills/forge-*/SKILL.md}` paths must resolve to real files
    - Issue config: `blank_issues_enabled: false` (force templates)
    - CI must use `${{ github.workspace }}` as base path and substitute `__FLOWFORGE_REPO__` placeholder
    - PR template must contain checklist items: Principles, Conventions, Tests, PM verification
---

# Spec: open-source-contributor-experience

## 1. Objective and Scope

**Objective**: Lower the barrier to contribute to FlowForge by providing standardized issue templates, a PR template with quality gates, and a CI smoke test that validates the OpenCode agent configuration.

**In Scope**:
- `.github/ISSUE_TEMPLATE/bug_report.md` — structured bug report
- `.github/ISSUE_TEMPLATE/feature_request.md` — structured feature request
- `.github/ISSUE_TEMPLATE/config.yml` — issue configuration (blank issues disabled)
- `.github/PULL_REQUEST_TEMPLATE.md` — PR template with contributor checklist
- `.github/workflows/opencode-smoke.yml` — CI workflow for smoke test
- Update `CONTRIBUTING.md` to reference templates and CI

**Out of Scope**:
- CODE_OF_CONDUCT.md translation (Spanish → English bug, tracked separately)
- Public repo publishing
- Demo repo creation (`flowforge-demo-task-manager`)
- Items 2–14 of the improvement plan

---

## 2. Functional Requirements

### FR-001: Issue Templates

#### FR-001-A: Bug Report Template
- File: `.github/ISSUE_TEMPLATE/bug_report.md`
- Language: **English** (follows I18N policy for public-facing docs)
- Required fields:
  - OS, IDE (OpenCode / Cursor / Antigravity / VS Code), install mode (remote vs local)
  - Clear bug description
  - Reproduction steps (numbered)
  - Expected vs actual behavior
  - Logs / installer output (optional but encouraged)
- Scenario A — Valid bug report: Given a contributor opens a new issue and selects "Bug Report"; When they fill all required fields; Then the issue is created with correct labels and triage info.
- Scenario B — Incomplete submission: Given a contributor submits with missing OS field; When the template enforces required标记; Then they are prompted to complete before submission.

#### FR-001-B: Feature Request Template
- File: `.github/ISSUE_TEMPLATE/feature_request.md`
- Language: **English**
- Required fields:
  - Problem or use case (why does the contributor need this?)
  - Proposed solution (high-level, not implementation)
  - Alternatives considered
  - Is this related to an existing FlowForge phase? (discovery / arch / plan / dev / verify / memory)
- Scenario A — Valid feature request: Given a contributor submits a feature request; When it includes problem + proposed solution; Then it is labeled `enhancement` and routed to the appropriate backlog.
- Scenario B — Vague request: Given a contributor submits "it doesn't work"; When the template prompts for specific use case; Then the issue is flagged as needing clarification.

#### FR-001-C: Issue Configuration
- File: `.github/ISSUE_TEMPLATE/config.yml`
- `blank_issues_enabled: false` — contributors **must** use a template
- `contact_links`: point to GitHub Discussions for questions (not issues)
- Language for contact: English

---

### FR-002: PR Template with Checklist

- File: `.github/PULL_REQUEST_TEMPLATE.md`
- Language: **English**
- Sections:
  1. **Description** — what does this PR do? (2–5 sentences)
  2. **Related issue** — link to issue (if any)
  3. **Checklist** (all required before merge):
     - [ ] Principles respected: small PRs, no contradictions to existing docs, onboarding intact
     - [ ] Conventions followed: English for public docs, `.agents/` for Antigravity, `.ai-work/` for artifacts
     - [ ] Tests included: `npm test` or `dotnet test` passes locally (or CI equivalent)
     - [ ] PM manual tests: any PM-* from spec.md marked `[x]` if applicable
  4. **Breaking changes** — yes/no; if yes, describe migration path
- Scenario A — Clean PR: Given a contributor opens a PR following the template; When all checklist items are checked and CI passes; Then the maintainer can review without asking for context.
- Scenario B — Missing test: Given a PR with code changes but no test run documented; When the CI smoke test validates the artifact structure; Then the PR is flagged with a note to include test evidence.

---

### FR-003: CI Workflow — OpenCode Smoke Test

- File: `.github/workflows/opencode-smoke.yml`
- **Trigger**: `push` to `main` + `pull_request` (both)
- **What the CI validates** (automatable):
  1. `opencode.flowforge.json` is valid JSON (parse with `jq .`)
  2. All 7 subagent entries exist (`forge-discovery`, `forge-arch`, `forge-plan`, `forge-dev`, `forge-verify`, `forge-memory`, `forge-teacher`)
  3. Each subagent's `{file:__FLOWFORGE_REPO__/skills/forge-*/SKILL.md}` resolves to a **real file** — substitute `${{ github.workspace }}` for `__FLOWFORGE_REPO__`
  4. No placeholder text (`__UNSET__`, `__TODO__`, etc.) remains in JSON
- **What the CI does NOT validate** (manual, documented in `docs/17`):
  - That OpenCode actually loads the subagents at runtime (requires human invocation)
  - That the orchestrator delegation works end-to-end
- **CI approach**: Use `bash` step + `jq` + `for` loop over skill paths. Fail fast on first missing file.
- Path resolution: Replace `__FLOWFORGE_REPO__` with `${{ github.workspace }}` before checking existence.
- Scenario A — Valid config on PR: Given a contributor opens a PR modifying `skills/`; When the CI runs; Then it verifies all referenced SKILL.md files exist and JSON is valid.
- Scenario B — Broken skill path: Given a PR adds a new agent referencing `skills/forge-new/SKILL.md` but the file doesn't exist; When CI runs; Then the job fails with: `Missing SKILL.md: skills/forge-new/SKILL.md`.

---

## 3. Non-Functional Requirements

| ID | Requirement | Type |
|----|-------------|------|
| NFR-001 | Templates must be in **English** (I18N policy: public docs = English) | deterministic |
| NFR-002 | CI workflow must complete in < 2 minutes | deterministic |
| NFR-003 | CI must use `${{ github.workspace }}` as base for path resolution (not hardcoded paths) | deterministic |
| NFR-004 | PR template checklist items must be copy-pasteable with `[ ]` syntax (GitHub markdown) | deterministic |
| NFR-005 | CONTRIBUTING.md must reference templates and CI after this feature lands | deterministic |

---

## 4. Developer Manual Tests (Required — mark [x] before /flow-close)

| ID | Case / Flow | Steps (Summary) | Expected Result | [x] |
|----|-------------|-----------------|-----------------|-----|
| PM-1 | Issue template renders correctly | 1. Go to GitHub Issues → New Issue<br>2. See "Bug Report" and "Feature Request" options<br>3. Select Bug Report<br>4. Verify OS/IDE/install mode fields appear | Template loads with all required fields visible and labeled | [X] |
| PM-2 | PR template checklist is visible on PR creation | 1. Open a draft PR against main<br>2. Observe PR description box<br>3. Verify checklist items (Principles, Conventions, Tests, PM) appear above the comment box | Checklist visible, all 4 items with `[ ]` syntax | [X] |
| PM-3 | CI smoke passes with valid config | 1. Open a PR that modifies `opencode.flowforge.json`<br>2. Wait for CI to complete<br>3. Check the "opencode-smoke" job | Job passes — JSON valid, all 7 agents present, all skill paths resolve | [X] |
| PM-4 | CI smoke fails gracefully with missing skill | 1. Create a branch that adds a fake agent with a non-existent SKILL.md path<br>2. Open PR<br>3. Observe CI failure message | CI fails with clear message: `Missing SKILL.md: skills/forge-fake/SKILL.md` | [X] |
| PM-5 | Blank issues are blocked | 1. As maintainer, go to Issues<br>2. Attempt "Open a blank issue" (should not exist)<br>3. Verify only template options appear | No blank issue option; contributor must select a template | [X] |

---

## 5. Open Decisions for Human at CKP-1

| # | Question | Recommendation | Rationale |
|---|----------|----------------|-----------|
| OD-1 | **CI trigger: PR only, or PR + push to main?** | PR + push to main | Push to main validates the baseline after merges; PR validates every contribution. Both is standard practice. |
| OD-2 | **Codeowners file?** | Skip for now (single maintainer) | Codeowners adds ownership rules that are premature while repo is private and single-maintainer. Revisit at public launch. |
| OD-3 | **Funding.yml?** | Skip for now | Premature. Add when repo goes public and funding is set up. |
| OD-4 | **CODE_OF_CONDUCT.md Spanish → English?** | Leave as separate bug | This is a consistency bug with I18N policy, not a contributor-experience improvement. File a separate issue after this feature ships. |
| OD-5 | **Issue body labels auto-applied?** | Yes — use `bug` and `enhancement` via `config.yml` | GitHub applies these labels automatically based on template choice. No extra tooling needed. |
| OD-6 | **PR template: include "screenshots" item for docs PRs?** | No — keep checklist minimal | Screenshots are only relevant for UI changes. General template should stay lean; contributors doing UI can add manually. |

---

## 6. File Inventory (New Files)

| Path | Purpose |
|------|---------|
| `.github/ISSUE_TEMPLATE/bug_report.md` | Bug report form |
| `.github/ISSUE_TEMPLATE/feature_request.md` | Feature request form |
| `.github/ISSUE_TEMPLATE/config.yml` | Issue config (blank disabled) |
| `.github/PULL_REQUEST_TEMPLATE.md` | PR description + checklist |
| `.github/workflows/opencode-smoke.yml` | CI smoke test |

---

## 7. Update to Existing Files

| File | Change |
|------|--------|
| `CONTRIBUTING.md` | Add section referencing issue templates and PR template; add note that CI smoke validates OpenCode config on every PR |
| `docs/17-improvement-plan-specs.md` | Item 1: mark criteria as partially automated (CI validates structure; manual test validates runtime) |
| `docs/04-roadmap.md` | Item 1: add note about CI smoke coverage |

---

## Memory Signal

- type: decision
- significance: low
- summary: "CI smoke validates JSON + file existence only; runtime agent loading remains manual per Item 1 criteria in docs/17"

---

## Notes

- `__FLOWFORGE_REPO__` placeholder is resolved at CI time by substituting `${{ github.workspace }}`. No build-time patch script needed.
- The CI does **not** install OpenCode — it only validates that the JSON config references files that exist in the repo. Full agent loading is a manual test described in docs/17-improvement-plan-specs.md Item 1.
- If the repo stays private, issue templates won't be visible to external contributors until publish. The CI is still functional for maintainers.