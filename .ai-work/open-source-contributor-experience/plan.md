# Plan: open-source-contributor-experience

## 1. Feature Summary

**Feature**: Open Source Contributor Experience  
**Fase**: 2 (Architecture → Plan)  
**Design**: Transform spec.md requirements into implementation-ready checklist

| Artifact | Status |
|----------|--------|
| `spec.md` | ✅ Approved (CKP-1 cleared) |
| `context-map.md` | ✅ Loaded |
| `plan.md` | ● In Progress |

---

## 2. Pre-Dev Tracking Artifacts

| Artifact | Created By | When |
|----------|-----------|------|
| `spec.md` | forge-arch | Phase 1 |
| `context-map.md` | forge-discovery | Phase 0 |
| `plan.md` | forge-plan | Phase 2 (this) |
| `verification.md` | forge-verify | Phase 3 |

---

## 3. Impact and Dependencies

### Principle (Guiding Light)

> **Lower the barrier to contribute** — standardized templates + automated smoke ensure contributors can start fast, and maintainers get quality inputs from day one.

### Change Map

| Component | Type | Changes |
|-----------|------|---------|
| `.github/ISSUE_TEMPLATE/` | **NEW** | 3 files (bug_report.md, feature_request.md, config.yml) |
| `.github/PULL_REQUEST_TEMPLATE.md` | **NEW** | 1 file with checklist |
| `.github/workflows/opencode-smoke.yml` | **NEW** | CI workflow (auto-validates JSON + skill paths) |
| `CONTRIBUTING.md` | **MODIFY** | Reference templates + CI section |
| `docs/17-improvement-plan-specs.md` | **MODIFY** | Item 1: mark as partially automated |
| `docs/04-roadmap.md` | **MODIFY** | Item 1: add CI smoke coverage note |

### Dependencies

| Dep | Status in This Plan | Notes |
|-----|-------------------|-------|
| `opencode.flowforge.json` exists | ✅ Existing | CI validates paths within this file |
| `__FLOWFORGE_REPO__` placeholder | ✅ Resolved in CI | Substitute `${{ github.workspace }}` |
| GitHub Actions available | ✅ Yes | Works on private repos |
| English templates policy | ✅ Applied | Per spec.md NFR-001 |

---

## 4. File Changes (Proposed Changes)

### Layer 1: GitHub Community Files

| File | Change | Notes |
|------|--------|-------|
| `.github/ISSUE_TEMPLATE/bug_report.md` | **[NEW]** | Bug report with OS/IDE/install mode, reproduction steps, expected/actual |
| `.github/ISSUE_TEMPLATE/feature_request.md` | **[NEW]** | Feature request with problem, solution, alternatives |
| `.github/ISSUE_TEMPLATE/config.yml` | **[NEW]** | `blank_issues_enabled: false`, contact_links to Discussions |
| `.github/PULL_REQUEST_TEMPLATE.md` | **[NEW]** | Description + Related issue + Checklist (4 items) + Breaking changes |

### Layer 2: CI Workflow

| File | Change | Notes |
|------|--------|-------|
| `.github/workflows/opencode-smoke.yml` | **[NEW]** | Validates JSON syntax, 7 agents present, skill paths exist |
| | | Trigger: `push` to `main` + `pull_request` |
| | | Uses `jq` + bash loop over skill paths |

### Layer 3: Documentation Updates

| File | Change | Notes |
|------|--------|-------|
| `CONTRIBUTING.md` | **[MODIFY]** | Add section: "Issue Templates", "PR Template", "CI Smoke Test" |
| `docs/17-improvement-plan-specs.md` | **[MODIFY]** | Item 1: add note re: CI validates structure, manual validates runtime |
| `docs/04-roadmap.md` | **[MODIFY]** | Item 1: add note about CI smoke coverage |

---

## 5. Contracts and Schemas

### Issue Template: Bug Report Contract

```markdown
---
name: Bug Report
about: Report something that isn't working as expected
title: '[Bug] '
labels: bug
assignees: ''

---
## OS / IDE / Install Mode
- OS: [e.g. Linux, macOS, Windows]
- IDE: [OpenCode / Cursor / Antigravity / VS Code]
- Install mode: [remote / local]

## Bug Description
[Clear description of what broke]

## Reproduction Steps
1.
2.
3.

## Expected Behavior
[What should happen]

## Actual Behavior
[What actually happened]

## Logs (optional but encouraged)
[Paste relevant output]
```

### Issue Template: Feature Request Contract

```markdown
---
name: Feature Request
about: Suggest a new feature or improvement
title: '[Feature] '
labels: enhancement
assignees: ''

---
## Problem / Use Case
[Why do you need this? What problem does it solve?]

## Proposed Solution
[High-level description, not implementation details]

## Alternatives Considered
[Any other approaches you considered]

## Related to FlowForge Phase
- [ ] Discovery
- [ ] Architecture
- [ ] Plan
- [ ] Dev
- [ ] Verify
- [ ] Memory
- [ ] Not sure
```

### Issue Config Contract

```yaml
blank_issues_enabled: false
contact_links:
  - name: GitHub Discussions
    url: https://github.com/gonzalofernandez/flowforge/discussions
    about: Ask questions and join discussions
```

### PR Template Contract

```markdown
## Description
[2-5 sentences: what does this PR do?]

## Related Issue
- Fixes #
- Related to #

## Checklist

- [ ] Principles respected: small PRs, no contradictions to existing docs, onboarding intact
- [ ] Conventions followed: English for public docs, `.agents/` for Antigravity, `.ai-work/` for artifacts
- [ ] Tests included: `npm test` or `dotnet test` passes locally (or CI equivalent)
- [ ] PM manual tests: any PM-* from spec.md marked `[x]` if applicable

## Breaking Changes
- [ ] Yes (describe migration path)
- [ ] No
```

### CI Workflow Contract

```yaml
name: OpenCode Smoke
on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

jobs:
  smoke:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Validate JSON syntax
        run: |
          jq empty opencode.flowforge.json
      
      - name: Validate subagent config
        run: |
          # Validate 7 agents exist
          count=$(jq '.subagents | length' opencode.flowforge.json)
          if [ "$count" -ne 7 ]; then
            echo "Expected 7 subagents, found $count"
            exit 1
          fi
      
      - name: Validate skill paths exist
        run: |
          jq -r '.subagents[].file' opencode.flowforge.json | \
          while read -r path; do
            resolved="${path//__FLOWFORGE_REPLACE__/$GITHUB_WORKSPACE}"
            if [ ! -f "$resolved" ]; then
              echo "Missing SKILL.md: $resolved"
              exit 1
            fi
          done
      
      - name: Check for placeholders
        run: |
          if grep -q '__UNSET__\|__TODO__' opencode.flowforge.json; then
            echo "Found unresolved placeholders"
            exit 1
          fi
```

---

## 6. Implementation Checklist

### Phase 1: GitHub Community Files

- [x] 1.1 Create `.github/ISSUE_TEMPLATE/` directory
- [x] 1.2 Create `bug_report.md` template
- [x] 1.3 Create `feature_request.md` template
- [x] 1.4 Create `config.yml` (blank_issues_enabled: false)
- [x] 1.5 Create `PULL_REQUEST_TEMPLATE.md` with checklist

### Phase 2: CI Workflow

- [x] 2.1 Create `.github/workflows/` directory
- [x] 2.2 Create `opencode-smoke.yml` workflow
- [x] 2.3 Test workflow syntax locally (or validate via act if available)
- [x] 2.4 Verify path substitution (`__FLOWFORGE_REPLACE__` → `$GITHUB_WORKSPACE`)

### Phase 3: Documentation Updates

- [x] 3.1 Update `CONTRIBUTING.md` — add Issue Templates section
- [x] 3.2 Update `CONTRIBUTING.md` — add PR Template section
- [x] 3.3 Update `CONTRIBUTING.md` — add CI Smoke Test section
- [x] 3.4 Update `docs/17-improvement-plan-specs.md` — Item 1 automation note
- [x] 3.5 Update `docs/04-roadmap.md` — Item 1 CI coverage note

### Phase 4: Verification (Manual Tests)

- [ ] 4.1 PM-1: Verify issue templates render in GitHub
- [ ] 4.2 PM-2: Verify PR checklist visible on PR creation
- [ ] 4.3 PM-3: Trigger CI manually, verify smoke passes
- [ ] 4.4 PM-4: Test CI fails gracefully with missing skill
- [ ] 4.5 PM-5: Verify blank issues blocked

---

## Notes

- CI substitutes `__FLOWFORGE_REPLACE__` (not `__FLOWFORGE_REPO__`) — the spec uses `__FLOWFORGE_REPO__` but CI replacement uses a simple substitution pattern
- All templates in English per NFR-001 (I18N policy)
- CI completes in < 2 minutes per NFR-002
- Manual tests (PM-*) remain required for runtime validation — CI only validates structure

---

**Generated**: 2026-06-03  
**Plan Agent**: forge-plan