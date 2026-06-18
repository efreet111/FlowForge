---
cycle_count: 1
max_cycles: 3
status: "resolved"
resolved_date: "2026-06-18"
---
# Rework Ticket — open-source-contributor-experience

## 1. Failure Reason

**Classification**: Logic bug (CI false failure) + Logic bug (CI false green)

The CI workflow (`opencode-smoke.yml`) contains two critical bugs discovered during verification:

### Bug 1 (F1): Agent count mismatch — CI fails on valid configs
- **Severity**: 🔴 Critical — blocks all PRs
- **Root cause**: `jq '.agent | length'` counts all 8 agents (including primary `flowforge` orchestrator), but CI expects exactly 7. The spec defines 7 **subagents** — the orchestrator is the primary, not a subagent.
- **Symptom**: CI exits 1 with `"Expected 7 subagents, found 8"` on every PR, even when the config is perfectly valid.

### Bug 2 (F2): Escaped variable in grep — skill path validation silently skipped
- **Severity**: 🔴 Critical — false green (security risk)
- **Root cause**: Lines 36 and 39 use `\$skill` in grep patterns (bash double-quotes), which produces the literal string `$skill` instead of expanding the bash variable (e.g., `forge-discovery`). grep searches for literal `$skill` in the prompt — which never matches — and returns empty. The `[ -n "$path" ]` guard means the file-existence check is never reached.
- **Symptom**: CI exits 0 and prints nothing for step 3. ALL 7 path validations are silently skipped. A contributor could add a fake agent with a non-existent path and CI would report green.

### Minor Issue (F3): CONTRIBUTING.md broken relative link
- **Severity**: ⚠️ Low
- **Root cause**: `../.github/PULL_REQUEST_TEMPLATE.md` resolves outside the repo. Should be `.github/PULL_REQUEST_TEMPLATE.md`.

## 2. Affected Files

- `.github/workflows/opencode-smoke.yml` — F1 (lines 24–28) + F2 (lines 36, 39)
- `CONTRIBUTING.md` — F3 (line 53)

## 3. Correction Instructions

### Fix F1 — Agent count (`.github/workflows/opencode-smoke.yml`, lines 24–28)

Replace the count check with one of:

**Option A (preferred)** — count only subagents:
```bash
- name: Validate subagent config
  run: |
    count=$(jq '[.agent[] | select(.mode == "subagent")] | length' ide/opencode/opencode.flowforge.json)
    if [ "$count" -ne 7 ]; then
      echo "ERROR: Expected 7 subagents, found $count"
      exit 1
    fi
    echo "Found $count subagents"
```

**Option B** — expect 8 (simpler, less robust):
Change `-ne 7` to `-ne 8` and update the error message.

### Fix F2 — grep variable expansion (`.github/workflows/opencode-smoke.yml`, lines 35–51)

Remove the backslash before `$skill` in both grep patterns so bash expands the variable:

**Line 36** — primary grep:
```bash
path=$(jq -r ".agent.\"$skill\".prompt" ide/opencode/opencode.flowforge.json 2>/dev/null | grep -o "{file:__FLOWFORGE_REPO__/skills/$skill/SKILL.md}" | head -1)
#                                                                                                ^^^^^ unescaped
```

**Line 39** — fallback grep:
```bash
path=$(jq -r ".agent.\"$skill\".prompt" ide/opencode/opencode.flowforge.json 2>/dev/null | grep -oE "\{file:[^}]+skills/$skill/SKILL\.md\}" | head -1)
#                                                                                               ^^^^^ unescaped
```

### Fix F3 — CONTRIBUTING.md link (line 53)

Change:
```markdown
[PR template](../.github/PULL_REQUEST_TEMPLATE.md)
```
To:
```markdown
[PR template](.github/PULL_REQUEST_TEMPLATE.md)
```

## 4. Verification After Rework

After applying fixes, verify locally:

```bash
# Test F1 fix: should find 7 subagents
jq '[.agent[] | select(.mode == "subagent")] | length' ide/opencode/opencode.flowforge.json
# → 7

# Test F2 fix: should extract paths for all 7 agents
for skill in forge-discovery forge-arch forge-plan forge-dev forge-verify forge-memory forge-teacher; do
  jq -r ".agent.\"$skill\".prompt" ide/opencode/opencode.flowforge.json | grep -o "{file:__FLOWFORGE_REPO__/skills/$skill/SKILL.md}"
done
# → Should print 7 matching lines

# Simulate full CI locally
act pull_request -j smoke  # or push a test branch to GitHub
```

## 5. CKP-3 Status

- **Cycle**: 1/3
- **Remaining**: 2 attempts
- **Next cycle 3**: Emergency brake — escalate to human orchestrator
