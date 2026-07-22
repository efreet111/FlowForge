# Plan: Model Configuration Architecture

> **Source:** [spec.md](./spec.md) — approved at CKP-1 (2026-01-17)
> **BLOCKERs:** 0 unresolved (OQ-1 ✅ Gemini, OQ-2 ✅ gpt-4o)

## Summary

| Metric | Value |
|--------|-------|
| Total tasks | 13 |
| Phases | 5 |
| Estimated effort | 2.5 days (20 hours) |
| Risk level | Medium (Antigravity first-time correct models) |

## 1. Impact and dependencies

### What changes

| Component | Current state | Target state |
|-----------|---------------|--------------|
| OpenCode models | `ide/opencode/templates/agent-models.json` (flat structure) | `ide/opencode/config/agent-models.json` (with `$schema`, `tiers`, `active_tier`) |
| Cursor models | Hardcoded `MODELS` dict in `compile-agents-from-skills.py` (line 12-21) | `ide/cursor/config/agent-models.json` read via `json.load()` |
| Antigravity models | `ide/antigravity/rules/model-assignments.md` — **Claude/GPT names (wrong)** | `ide/antigravity/config/agent-models.json` — Gemini models |
| VS Code models | Hardcoded `['claude-sonnet-4-20250514', 'gpt-5.2']` in all 8 `.agent.md` frontmatters | `ide/vscode/config/agent-models.json` — `gpt-4o` for all |
| `generate-config.sh` | Reads `$TEMPLATES/agent-models.json` (lines 34, 77-80) | Reads `$FF_REPO/ide/opencode/config/agent-models.json` |
| `install.sh` | Reads `templates/agent-models.json` (line 210-211) | Reads `config/agent-models.json` |
| `.agents/rules/model-assignments.md` | Stale Claude/GPT table (27 lines) | Redirect doc pointing to `ide/{ide}/config/agent-models.json` |

### New dependencies

- `jq` already required by `generate-config.sh` (line 4) — no new dependency
- Python `json` module (stdlib) — already available, no new dependency
- CI validation script: new bash/python script, no external deps

### What does NOT change

- `ide/shared/workflow-orchestrator-parity.md` — out of scope
- `skills/*/SKILL.md` — agent logic untouched
- `ide/opencode/templates/agents/*.md.tpl` — agent templates stay
- `ide/cursor/rules/model-assignments.mdc` — stays as generated output (not deleted in v1)

## 2. File changes (Proposed Changes)

### New files (4)

| File | Responsibility |
|------|----------------|
| `[NEW] ide/opencode/config/agent-models.json` | Canonical OpenCode Zen free-tier model assignments (9 agents + provider + tiers) |
| `[NEW] ide/cursor/config/agent-models.json` | Canonical Cursor Budget tier model assignments (7 agents + provider + tiers) |
| `[NEW] ide/antigravity/config/agent-models.json` | Canonical Gemini model assignments (7 agents + provider) |
| `[NEW] ide/vscode/config/agent-models.json` | Canonical Copilot free-tier model assignments (8 agents + provider) |

### Modified files (4)

| File | Changes |
|------|---------|
| `[MODIFY] ide/opencode/generate-config.sh` | Change path from `templates/agent-models.json` to `config/agent-models.json` (lines 34, 77-80) |
| `[MODIFY] ide/cursor/compile-agents-from-skills.py` | Replace `MODELS` dict (lines 12-21) with `json.load()` from `config/agent-models.json` |
| `[MODIFY] ide/install.sh` | Update OpenCode agent-models path (line 210-211) from `templates/` to `config/` |
| `[MODIFY] .agents/rules/model-assignments.md` | Replace content with redirect notice |

### Deleted files (2)

| File | Reason |
|------|--------|
| `[DELETE] ide/opencode/templates/agent-models.json` | Replaced by `ide/opencode/config/agent-models.json` |
| `[DELETE] ide/opencode/templates/model-assignments.md.tpl` | Now generated inline by `generate-config.sh` (lines 70-89) |

### Regenerated files (1 batch)

| File | Changes |
|------|---------|
| `[REGENERATE] ide/vscode/agents/*.agent.md` (8 files) | Update `model:` frontmatter from `['claude-sonnet-4-20250514', 'gpt-5.2']` to `['gpt-4o']` |

## 3. Contracts and schemas

### Unified JSON schema (all 4 files)

```jsonc
{
  "$schema": "https://flowforge.dev/schemas/agent-models-v1.json",
  "provider": {
    "id": "<string>",           // e.g. "opencode-zen", "cursor-budget", "antigravity-gemini", "vscode-copilot"
    "name": "<string>",         // Human-readable provider name
    "docs_url": "<url>",        // Link to model catalog docs
    "models": ["<string>"]      // Allowlist of valid model slugs for this provider
  },
  "active_tier": "<string>",    // e.g. "budget" — which tier is active by default
  "tiers": {                    // Optional: named tier overrides
    "<tier_name>": {
      "description": "<string>",
      "cost_policy": "<string>",
      "agents": { "<agent_key>": { "model": "<string>", "fallback": "<string>" } }
    }
  },
  "agents": {                   // Required: default assignments (all agents)
    "forge-orchestrator": { "model": "<string>", "fallback": "<string>", "mode": "primary", "purpose": "<string>" },
    "forge-discovery":        { "model": "<string>", "fallback": "<string>", "mode": "subagent", "purpose": "<string>" },
    "forge-arch":             { "model": "<string>", "fallback": "<string>", "mode": "subagent", "purpose": "<string>" },
    "forge-plan":             { "model": "<string>", "fallback": "<string>", "mode": "subagent", "purpose": "<string>" },
    "forge-dev":              { "model": "<string>", "fallback": "<string>", "mode": "subagent", "purpose": "<string>" },
    "forge-verify":           { "model": "<string>", "fallback": "<string>", "mode": "subagent", "purpose": "<string>" },
    "forge-memory":           { "model": "<string>", "fallback": "<string>", "mode": "subagent", "purpose": "<string>" },
    "forge-teacher":          { "model": "<string>", "fallback": "<string>", "mode": "subagent", "purpose": "<string>" },
    "default":                { "model": "<string>", "fallback": "<string>", "mode": "primary",  "purpose": "<string>" }
  }
}
```

### Agent key invariants

- Agent keys (`forge-orchestrator`, `forge-discovery`, etc.) are **immutable** — never renamed.
- `forge-orchestrator` replaces the legacy `flowforge` key from the existing OpenCode template. The old `flowforge` key in `templates/agent-models.json` must be renamed to `forge-orchestrator` in the new `config/agent-models.json`.
- All 9 keys must be present in every JSON file (even if some IDEs don't use `forge-teacher`).

### Model assignments per IDE (from spec decisions)

| Agent | OpenCode (Zen free) | Cursor (Budget) | Antigravity (Gemini) | VS Code (Copilot free) |
|-------|--------------------|-----------------|----------------------|------------------------|
| forge-orchestrator | `big-pickle` | `gpt-5-mini` | `gemini-3-pro` | `gpt-4o` |
| forge-discovery | `deepseek-v4-flash-free` | `gpt-5-mini` | `gemini-3-flash` | `gpt-4o` |
| forge-arch | `big-pickle` | `kimi-k2.7-code` | `gemini-3-pro` | `gpt-4o` |
| forge-plan | `big-pickle` | `kimi-k2.7-code` | `gemini-3-pro` | `gpt-4o` |
| forge-dev | `big-pickle` | `gpt-5.1-codex-mini` | `gemini-3-pro` | `gpt-4o` |
| forge-verify | `minimax-m2.5-free` | `kimi-k2.7-code` | `gemini-3-pro` | `gpt-4o` |
| forge-memory | `deepseek-v4-flash-free` | `gpt-5-mini` | `gemini-3-flash` | `gpt-4o` |
| forge-teacher | `deepseek-v4-flash-free` | `gpt-5-mini` | `gemini-3-flash` | `gpt-4o` |
| default | `big-pickle` | `gpt-5-mini` | `gemini-3-flash` | `gpt-4o` |

## 4. Implementation checklist

### Phase 1: Create canonical JSON files

---

#### Task 1.1: Create OpenCode `config/agent-models.json`

**Description:** Migrate `ide/opencode/templates/agent-models.json` to the new canonical location and schema. Rename `flowforge` agent key to `forge-orchestrator`. Add `$schema`, `active_tier`, and `tiers` wrapper. Preserve all existing model assignments and the `provider` block.

**Files:**
- CREATE: `ide/opencode/config/agent-models.json`
- Source data: `ide/opencode/templates/agent-models.json` (current, to be deleted in Phase 3)

**Acceptance criteria:**
- [x] Valid JSON (parseable by `jq` and `python3 -m json.tool`)
- [x] Contains `$schema` field pointing to `https://flowforge.dev/schemas/agent-models-v1.json`
- [x] All 9 agent keys present: `forge-orchestrator`, `forge-discovery`, `forge-arch`, `forge-plan`, `forge-dev`, `forge-verify`, `forge-memory`, `forge-teacher`, `default`
- [x] `forge-orchestrator` replaces old `flowforge` key (same model: `big-pickle`)
- [x] All model values exist in `provider.models` list
- [x] `active_tier: "budget"` present
- [x] `tiers.budget` and `tiers.quality` sections present (quality can be skeleton/commented)
- [x] `provider.models` list unchanged (8 models from current file)

**Effort:** S (1 hour)
**Dependencies:** None
**Spec ref:** FR-001, FR-002, §4 Proposed Architecture

---

#### Task 1.2: Create Cursor `config/agent-models.json`

**Description:** Extract model data from the hardcoded `MODELS` dict in `compile-agents-from-skills.py` (lines 12-21) and the `model-assignments.mdc` table, and create a canonical JSON file following the unified schema.

**Files:**
- CREATE: `ide/cursor/config/agent-models.json`
- Source data: `ide/cursor/compile-agents-from-skills.py` MODELS dict + `ide/cursor/rules/model-assignments.mdc`

**Acceptance criteria:**
- [x] Valid JSON
- [x] `$schema` field present
- [x] All 9 agent keys present
- [x] Models match current Cursor Budget tier: `gpt-5-mini`, `kimi-k2.7-code`, `gpt-5.1-codex-mini` (from `model-assignments.mdc`)
- [x] `provider.id: "cursor-budget"`, `provider.docs_url` points to Cursor models docs
- [x] `provider.models` list includes all models referenced in `agents` block + fallbacks
- [x] `tiers.quality` section present with premium models from `model-assignments.mdc` (lines 82-87)
- [x] Fallback models included (e.g., `gemini-3-flash`, `gpt-5.4-mini-medium`, `composer-2.5-fast`)

**Effort:** S (1 hour)
**Dependencies:** None (parallel with 1.1)
**Spec ref:** FR-001, FR-002, §4 Migration plan step 2

---

#### Task 1.3: Create Antigravity `config/agent-models.json`

**Description:** Create a new canonical JSON file for Antigravity with Gemini models. This is the **highest-impact task** — the current `model-assignments.md` lists Claude/GPT models that don't exist in Antigravity. Decision from CKP-1: `gemini-3-flash` for discovery/memory/teacher, `gemini-3-pro` for orchestrator/arch/plan/dev/verify.

**Files:**
- CREATE: `ide/antigravity/config/agent-models.json`

**Acceptance criteria:**
- [x] Valid JSON
- [x] `$schema` field present
- [x] All 9 agent keys present
- [x] `forge-discovery`, `forge-memory`, `forge-teacher`, `default` → model: `gemini-3-flash`
- [x] `forge-orchestrator`, `forge-arch`, `forge-plan`, `forge-dev`, `forge-verify` → model: `gemini-3-pro`
- [x] `provider.id: "antigravity-gemini"`, `provider.models` includes at least `["gemini-3-flash", "gemini-3-pro"]`
- [x] NO Claude or GPT model names anywhere in the file
- [x] Fallback models are also Gemini variants

**Effort:** S (1 hour)
**Dependencies:** None (parallel with 1.1, 1.2)
**Spec ref:** FR-001, FR-003 Scenario B, OQ-1 decision, §6 Decisiones

---

#### Task 1.4: Create VS Code `config/agent-models.json`

**Description:** Create a new canonical JSON file for VS Code Copilot. Decision from CKP-1: `gpt-4o` for all agents (Copilot free tier). Current `.agent.md` files have wrong models (`claude-sonnet-4-20250514`, `gpt-5.2`).

**Files:**
- CREATE: `ide/vscode/config/agent-models.json`

**Acceptance criteria:**
- [x] Valid JSON
- [x] `$schema` field present
- [x] All 9 agent keys present
- [x] ALL agents → model: `gpt-4o`, fallback: `gpt-4o`
- [x] `provider.id: "vscode-copilot"`, `provider.models` includes `["gpt-4o"]`
- [x] NO Claude model names anywhere in the file
- [x] `active_tier: "budget"` (free tier)

**Effort:** S (30 min)
**Dependencies:** None (parallel with 1.1-1.3)
**Spec ref:** FR-001, OQ-2 decision, §6 Decisiones

---

### Phase 2: Update consumers

---

#### Task 2.1: Update `generate-config.sh` to read from `config/`

**Description:** Change all references from `templates/agent-models.json` to `config/agent-models.json` in the OpenCode config generator script. The script reads the JSON at lines 34, 77-80 to extract model/fallback/mode/purpose per agent.

**Files:**
- MODIFY: `ide/opencode/generate-config.sh`
  - Line 9: Add `CONFIG_DIR="$FF_REPO/ide/opencode/config"` (or reuse `TEMPLATES` with new path)
  - Line 34: Change `"$TEMPLATES/agent-models.json"` → `"$FF_REPO/ide/opencode/config/agent-models.json"`
  - Lines 77-80: Same path change for the model-assignments.md generation loop
  - Note: The `flowforge` → `forge-orchestrator` key rename in JSON means the `$AGENTS` variable on line 32 must also be updated (replace `flowforge` with `forge-orchestrator`)

**Acceptance criteria:**
- [x] Script runs without error: `bash ide/opencode/generate-config.sh /path/to/repo`
- [x] Reads from `ide/opencode/config/agent-models.json` (not `templates/`)
- [x] `$AGENTS` list on line 32 uses `forge-orchestrator` instead of `flowforge`
- [x] Generated `opencode.json` has correct model fields for all 9 agents
- [x] Generated `model-assignments.md` table has correct models
- [x] No references to `templates/agent-models.json` remain in the file

**Effort:** S (1 hour)
**Dependencies:** Task 1.1 (needs `config/agent-models.json` to exist)
**Spec ref:** FR-003 Scenario A, §4 Migration plan step 6

---

#### Task 2.2: Update `compile-agents-from-skills.py` to read JSON

**Description:** Replace the hardcoded `MODELS` dict (lines 12-21) with a `json.load()` call that reads from `ide/cursor/config/agent-models.json`. The script must extract the model for each agent from the JSON's `agents` block.

**Files:**
- MODIFY: `ide/cursor/compile-agents-from-skills.py`
  - Lines 12-21: Replace `MODELS = {...}` with JSON loading logic
  - Add `import json` (if not already present)
  - Add path resolution: `CONFIG = ROOT / "ide" / "cursor" / "config" / "agent-models.json"`
  - Load: `models_data = json.loads(CONFIG.read_text(encoding="utf-8"))`
  - Extract: `MODELS = {k: v["model"] for k, v in models_data["agents"].items() if k != "default"}`

**Acceptance criteria:**
- [x] Script runs without error: `python3 ide/cursor/compile-agents-from-skills.py`
- [x] No hardcoded `MODELS` dict remains in the file
- [x] Reads from `ide/cursor/config/agent-models.json`
- [x] Compiled agents in `ide/cursor/agents/` have correct `model:` frontmatter matching JSON
- [x] Output identical to current output (same models, same agents) when run against the new JSON
- [x] Graceful error if JSON file is missing or malformed

**Effort:** M (2 hours)
**Dependencies:** Task 1.2 (needs `config/agent-models.json` to exist)
**Spec ref:** FR-001 Scenario B, FR-005 Scenario B, §4 Migration plan step 5

---

#### Task 2.3: Update `install.sh` for new JSON paths

**Description:** Update the main installer script to read `agent-models.json` from `config/` instead of `templates/`. Also update the Antigravity section to generate `model-assignments.md` from the new JSON (currently just copies the stale file).

**Files:**
- MODIFY: `ide/install.sh`
  - Line 210-211: Change `$IDE_DIR/opencode/templates/agent-models.json` → `$IDE_DIR/opencode/config/agent-models.json`
  - Line 110: Antigravity rules copy — add model-assignments generation from `ide/antigravity/config/agent-models.json` (or note that `generate-config.sh` handles OpenCode and a similar generator should handle Antigravity)

**Acceptance criteria:**
- [x] `bash ide/install.sh` runs without error
- [x] OpenCode agent patching reads from `config/agent-models.json`
- [x] Antigravity `model-assignments.md` contains Gemini models (not Claude/GPT)
- [x] No references to `templates/agent-models.json` remain

**Effort:** M (3 hours)
**Dependencies:** Tasks 1.1, 1.3 (need JSON files to exist)
**Spec ref:** FR-003, FR-005 Scenario A, §4 Migration plan steps 7, 12

---

### Phase 3: Migrate/delete old files

---

#### Task 3.1: Delete `ide/opencode/templates/agent-models.json`

**Description:** Remove the old template file now that `ide/opencode/config/agent-models.json` is the canonical source. Verify no other scripts reference it.

**Files:**
- DELETE: `ide/opencode/templates/agent-models.json`

**Acceptance criteria:**
- [x] File deleted
- [x] `grep -r "templates/agent-models.json" ide/` returns no results (all consumers updated)
- [x] `generate-config.sh` still works (reads from `config/`)
- [x] `install.sh` still works (reads from `config/`)

**Effort:** S (15 min)
**Dependencies:** Tasks 2.1, 2.3 (consumers must be updated first)
**Spec ref:** §4 Migration plan step 9

---

#### Task 3.2: Delete `ide/opencode/templates/model-assignments.md.tpl`

**Description:** Remove the template file. The `generate-config.sh` script already generates `model-assignments.md` inline (lines 70-89) without using this template. Verify no other consumer uses it.

**Files:**
- DELETE: `ide/opencode/templates/model-assignments.md.tpl`

**Acceptance criteria:**
- [x] File deleted
- [x] `grep -r "model-assignments.md.tpl" ide/` returns no results
- [x] `generate-config.sh` still produces correct `model-assignments.md` output

**Effort:** S (15 min)
**Dependencies:** Task 2.1 (verify generator works without template)
**Spec ref:** §4 Migration plan step 10

---

#### Task 3.3: Replace `.agents/rules/model-assignments.md` with redirect

**Description:** Replace the stale generic `model-assignments.md` (which lists Claude/GPT models) with a short redirect document pointing users to the canonical per-IDE JSON files.

**Files:**
- MODIFY: `.agents/rules/model-assignments.md`

**Acceptance criteria:**
- [x] File content replaced with redirect notice (not deleted — project installs may reference it)
- [x] Redirect mentions all 4 IDE-specific JSON paths: `ide/{opencode,cursor,antigravity,vscode}/config/agent-models.json`
- [x] No Claude/GPT model names remain in the file
- [x] File still has valid YAML frontmatter (`alwaysApply: false`)

**Effort:** S (15 min)
**Dependencies:** Tasks 1.1-1.4 (JSON files must exist for redirect to be meaningful)
**Spec ref:** FR-005 Scenario A, §4 Migration plan step 11

---

### Phase 4: Validation & testing

---

#### Task 4.1: Create CI validation script

**Description:** Write a validation script that checks all 4 `agent-models.json` files for schema compliance, agent key completeness, and model reference validity. This script should be runnable in CI (GitHub Actions) and locally.

**Files:**
- CREATE: `scripts/validate-agent-models.sh` (or `.py` — bash+jq preferred for CI parity with existing scripts)

**Acceptance criteria:**
- [x] Script validates all 4 JSON files: `ide/{opencode,cursor,antigravity,vscode}/config/agent-models.json`
- [x] Checks: (a) valid JSON, (b) `$schema` field present, (c) all 9 agent keys present, (d) every `model` and `fallback` value exists in `provider.models` list
- [x] Exits 0 on success, non-zero on failure with descriptive error message
- [x] Test: introducing `"model": "nonexistent-model"` causes validation failure (matches PM-4)
- [x] Runs in <50ms per file (NFR-005)

**Effort:** M (3 hours)
**Dependencies:** Tasks 1.1-1.4 (JSON files must exist to validate)
**Spec ref:** FR-006, PM-4

---

#### Task 4.2: Manual testing (PM-1 through PM-4)

**Description:** Execute all 4 manual test cases from `spec.md` §Developer manual tests. Mark `[x]` in spec.md upon success.

**Files:**
- MODIFY: `.ai-work/model-config-architecture/spec.md` — mark PM-* checkboxes

**Acceptance criteria:**
- [x] **PM-1:** Fresh install generates correct models per IDE — all 4 IDEs produce correct output
- [x] **PM-2:** User override survives reinstall — edit `opencode.json`, run install again, override preserved
- [x] **PM-3:** Cursor recompilation reads JSON — change JSON, recompile, verify frontmatter matches
- [x] **PM-4:** CI validation catches broken model reference — inject bad model, script fails with correct error

**Effort:** M (2 hours)
**Dependencies:** All Phase 1-3 tasks complete, Task 4.1 (CI script)
**Spec ref:** §Developer manual tests

---

### Phase 5: Documentation

---

#### Task 5.1: Update `ide/README.md` with new model config architecture

**Description:** Update the IDE matrix and "What each IDE gets" table to reflect the new `config/agent-models.json` architecture. Document the canonical path per IDE and the JSON schema.

**Files:**
- MODIFY: `ide/README.md`
  - Update "Model assignments" row in "What each IDE gets" table (line 97)
  - Add a new section "Model configuration" explaining the `config/agent-models.json` pattern
  - Update any references to `templates/agent-models.json`

**Acceptance criteria:**
- [x] "Model assignments" row references `config/agent-models.json` per IDE
- [x] New section explains: one JSON per IDE, schema, how to customize
- [x] No references to deleted files (`templates/agent-models.json`, `model-assignments.md.tpl`)
- [x] Table still accurate for all 4 IDEs

**Effort:** S (1 hour)
**Dependencies:** Phase 3 complete (old files deleted so docs don't reference them)
**Spec ref:** §4 Migration plan step 13

---

## 5. Execution order

```
Phase 1 (parallel):
  Task 1.1 ──┐
  Task 1.2 ──┤── All 4 JSON files created
  Task 1.3 ──┤
  Task 1.4 ──┘

Phase 2 (after Phase 1):
  Task 2.1 ──┐
  Task 2.2 ──┤── All consumers updated
  Task 2.3 ──┘

Phase 3 (after Phase 2):
  Task 3.1 ──┐
  Task 3.2 ──┤── Old files cleaned up
  Task 3.3 ──┘

Phase 4 (after Phase 3):
  Task 4.1 ──── CI script created
  Task 4.2 ──── Manual tests pass (depends on 4.1)

Phase 5 (after Phase 4):
  Task 5.1 ──── Docs updated
```

**Critical path:** 1.1 → 2.1 → 3.1 → 4.1 → 4.2 → 5.1

**Parallelism opportunities:**
- Phase 1: all 4 tasks are independent → can be done simultaneously
- Phase 2: Tasks 2.1, 2.2, 2.3 are independent of each other (but all need Phase 1)
- Phase 3: Tasks 3.1, 3.2, 3.3 are independent of each other (but all need Phase 2)

## 6. Risk mitigation

| # | Risk | Probability | Impact | Mitigation |
|---|------|-------------|--------|------------|
| R1 | Antigravity Gemini model slugs are wrong (e.g., `gemini-3-flash` doesn't exist) | Medium | High — agents fail to spawn | Verify model slugs against Antigravity docs before Task 1.3. Add to CI validation (Task 4.1). Keep fallback chain: if `gemini-3-pro` fails, Antigravity should fall back to `gemini-3-flash`. |
| R2 | `flowforge` → `forge-orchestrator` key rename breaks existing `opencode.json` | Medium | Medium — users lose orchestrator config | `generate-config.sh` merge logic (lines 49-64) only overwrites known agent keys. Add migration: if old `flowforge` key exists in user's `opencode.json`, rename to `forge-orchestrator` before merge. |
| R3 | Cursor `compile-agents-from-skills.py` produces different output after migration | Low | High — agents break in Cursor | Task 4.2 PM-3 verifies byte-identical output. Run compilation before and after; diff the `agents/` directory. |
| R4 | User's existing `opencode.json` has custom models overwritten on reinstall | Medium | High — violates FR-004 | The merge logic in `generate-config.sh` (lines 49-64) already preserves non-agent keys. Verify with PM-2 test. Add explicit check: if user's model differs from JSON default, print a warning and preserve user's choice. |
| R5 | `$schema` URL (`flowforge.dev/schemas/...`) doesn't exist yet | Certain | Low — validation warning | OQ-3 assumption: `$schema` field is present but validation treats it as warning, not error. CI script checks schema structure locally, not via URL fetch. |
| R6 | `install.sh` Antigravity section doesn't generate model-assignments.md from JSON | Medium | Medium — Antigravity still gets stale models | Task 2.3 explicitly addresses this. Consider extracting Antigravity model-assignments generation into a separate script (like `generate-config.sh` for OpenCode) for maintainability. |
| R7 | VS Code `.agent.md` files have `model` as array `['gpt-4o']` vs string `"gpt-4o"` | Low | Low — Copilot may not parse | Check Copilot agent schema: if it expects array, use `["gpt-4o"]`; if string, use `"gpt-4o"`. Current files use array format — preserve it. |

## 7. Out of scope (explicit)

These items are **NOT** part of this plan:

- Modifying the `flowforge` binary's `install` command (mentioned in spec as out of scope)
- Implementing FR-004 user override UI/UX (the merge logic exists; polish is future work)
- Hosting the `$schema` JSON schema at `flowforge.dev` (OQ-3 — future)
- `flowforge doctor` runtime model validation (OQ-5 — future)
- Windows `install.ps1` updates (NFR-004 mentions parity, but the PowerShell script doesn't currently read `agent-models.json` — it copies pre-compiled files)
- Creating per-IDE generator scripts for Antigravity and VS Code (analogous to `generate-config.sh`) — this plan updates `install.sh` directly; dedicated generators are a follow-up

---

## Memory Signal
- type: plan
- significance: high
- summary: "13-task plan for model-config-architecture. 4 canonical JSON files (one per IDE), 4 consumer updates, 2 deletions, 1 redirect, CI validation, manual tests, docs. Critical path: 1.1→2.1→3.1→4.1→4.2→5.1. Key risk: Antigravity Gemini model slug correctness."
