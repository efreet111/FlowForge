# ADR-012: IDE-Specific Model Configuration via Canonical JSON

> **Status:** Accepted (implemented)
> **Date:** 2026-07-18
> **Feature:** `model-config-architecture`
> **Deciders:** forge-arch (CKP-1), Human (OQ-1, OQ-2)
> **Replaces:** ADR-N/A (informal model-assignments.md, hardcoded Python dicts, template-based JSON, stale Markdown)

---

## Context

FlowForge supports 4 IDEs (OpenCode, Cursor, Antigravity, VS Code), each with different AI model providers. Before this ADR, model assignments were scattered across:

1. `ide/opencode/templates/agent-models.json` — flat JSON template (OpenCode-specific)
2. `ide/cursor/compile-agents-from-skills.py` — hardcoded `MODELS` dict (Python)
3. `.agents/rules/model-assignments.md` — generic Markdown table (Claude/GPT, wrong for Antigravity)
4. `ide/vscode/agents/*.agent.md` — frontmatter in 8 files (outdated models)
5. `ide/cursor/rules/model-assignments.mdc` — generated Markdown (duplicated)

This caused:
- **Antigravity** users saw Claude/GPT models that don't work in Gemini
- **VS Code** users had `claude-sonnet-4` and `gpt-5.2` — neither available in Copilot free tier
- **Adding a new agent** required editing 5+ files, some in hardcoded scripts
- **Model renames** (e.g., OpenCode renames `big-pickle` → `big-pickle-v2`) required script changes

Two CKP-1 BLOCKER questions were resolved:
- **OQ-1**: Antigravity defaults → `gemini-3-flash` (cheap) + `gemini-3-pro` (reasoning)
- **OQ-2**: VS Code defaults → `gpt-4o` for all agents (Copilot free tier)

## Decision

**Create one canonical JSON file per IDE** at `ide/{ide}/config/agent-models.json` that all consumers read from.

### Schema (v1)

```json
{
  "$schema": "https://flowforge.dev/schemas/agent-models-v1.json",
  "provider": { "id": "<string>", "name": "<string>", "docs_url": "<url>", "models": ["<string>"] },
  "active_tier": "budget|quality",
  "tiers": {
    "budget": { "description": "<string>", "cost_policy": "<string>", "agents": { "<agent_key>": { "model": "<string>", "fallback": "<string>", "mode": "primary|subagent", "purpose": "<string>" } } },
    "quality": { ... }
  },
  "agents": {
    "forge-orchestrator": { "model": "<string>", "fallback": "<string>", "mode": "primary", "purpose": "<string>" },
    "forge-discovery": { ... },
    "forge-arch": { ... },
    "forge-plan": { ... },
    "forge-dev": { ... },
    "forge-verify": { ... },
    "forge-memory": { ... },
    "forge-teacher": { ... },
    "default": { ... }
  }
}
```

### Example: Antigravity (Gemini)

```json
{
  "$schema": "https://flowforge.dev/schemas/agent-models-v1.json",
  "provider": {
    "id": "antigravity-gemini",
    "name": "Antigravity (Gemini)",
    "docs_url": "https://ai.google.dev/models/gemini",
    "models": ["gemini-3-flash", "gemini-3-pro"]
  },
  "active_tier": "budget",
  "tiers": {
    "budget": {
      "description": "Default tier for all agents",
      "cost_policy": "Flash for cheap tasks, Pro for reasoning",
      "agents": {}
    }
  },
  "agents": {
    "forge-orchestrator": { "model": "gemini-3-pro", "fallback": "gemini-3-flash", "mode": "primary", "purpose": "CKP-0..4 coordinator" },
    "forge-discovery": { "model": "gemini-3-flash", "fallback": "gemini-3-flash", "mode": "subagent", "purpose": "Context map / CKP-0" },
    "forge-arch": { "model": "gemini-3-pro", "fallback": "gemini-3-flash", "mode": "subagent", "purpose": "Spec + STRIDE analysis" },
    "forge-plan": { "model": "gemini-3-pro", "fallback": "gemini-3-flash", "mode": "subagent", "purpose": "Plan decomposition" },
    "forge-dev": { "model": "gemini-3-pro", "fallback": "gemini-3-flash", "mode": "subagent", "purpose": "Implementation and tests" },
    "forge-verify": { "model": "gemini-3-pro", "fallback": "gemini-3-flash", "mode": "subagent", "purpose": "Audit and verify-report" },
    "forge-memory": { "model": "gemini-3-flash", "fallback": "gemini-3-flash", "mode": "subagent", "purpose": "Session closure" },
    "forge-teacher": { "model": "gemini-3-flash", "fallback": "gemini-3-flash", "mode": "subagent", "purpose": "Explanations" },
    "default": { "model": "gemini-3-flash", "fallback": "gemini-3-flash", "mode": "primary", "purpose": "Fallback" }
  }
}
```

### Agent key rename

The legacy `flowforge` agent key (used in the original OpenCode template) is renamed to `forge-orchestrator` in all JSON files. This aligns with the canonical agent name used throughout FlowForge:

- **Before:** `"flowforge": { "model": "big-pickle", ... }`
- **After:** `"forge-orchestrator": { "model": "big-pickle", ... }`

All consumer scripts (`generate-config.sh`, `compile-agents-from-skills.py`, `install.sh`) were updated to use `forge-orchestrator` instead of `flowforge`.

### Consumer pattern

| Consumer | Reads from | Output |
|----------|-----------|--------|
| `generate-config.sh` (OpenCode) | `ide/opencode/config/agent-models.json` | `opencode.json` + rules |
| `compile-agents-from-skills.py` (Cursor) | `ide/cursor/config/agent-models.json` | Agent `.md` frontmatter |
| `install.sh` (Antigravity) | `ide/antigravity/config/agent-models.json` | `model-assignments.md` |
| VS Code (manual/scripted) | `ide/vscode/config/agent-models.json` | Agent `.agent.md` frontmatter |
| **CI validation** | `scripts/validate-agent-models.sh` | Error report |

### CI validation

The script `scripts/validate-agent-models.sh` validates all 4 JSON files in ~29ms per file (single `jq` invocation per file). It checks:

1. **JSON syntax** — parseable by `jq`
2. **Schema compliance** — `$schema`, `provider`, `agents`, `tiers`, `active_tier` present
3. **Agent key completeness** — all 9 required keys (`forge-orchestrator`, `forge-discovery`, `forge-arch`, `forge-plan`, `forge-dev`, `forge-verify`, `forge-memory`, `forge-teacher`, `default`)
4. **Model reference validity** — every `model` and `fallback` value exists in `provider.models` allowlist

Run manually: `bash scripts/validate-agent-models.sh`

Expected output:
```
=== FlowForge agent-models.json validator ===
[opencode] OK: 9 agents, provider=opencode-zen, tier=budget
[cursor] OK: 9 agents, provider=cursor-budget, tier=budget
[antigravity] OK: 9 agents, provider=antigravity-gemini, tier=budget
[vscode] OK: 9 agents, provider=vscode-copilot, tier=budget
=== Summary ===
Files checked: 4
Result: PASS (all 4 files valid)
```

### Migration

- Old files deleted: `templates/agent-models.json`, `templates/model-assignments.md.tpl`
- Generic `.agents/rules/model-assignments.md` replaced with redirect doc
- All consumers updated to read from `config/` instead of `templates/` or hardcoded values

## Consequences

### Positive

- **Single source of truth** per IDE — no duplication, no drift
- **Adding a new agent**: add 4 JSON entries (one per IDE) — no script changes
- **Model rename**: change one JSON value — all generated artifacts update
- **CI validation** catches model reference errors before deploy (~29ms per file)
- **FR-004** (user override preservation) works: merge logic keeps user edits on reinstall

### Negative

- 4 JSON files to maintain instead of 1 (though each is specific to its IDE's provider)
- `$schema` URL (`flowforge.dev/schemas/...`) not yet hosted — validation is local-only for now
- Per-IDE generator scripts for Antigravity and VS Code are not yet created (logic lives in `install.sh`)

### Risks

- **Antigravity model slug correctness**: `gemini-3-flash`/`gemini-3-pro` are based on current docs; if model names change, CI will catch the mismatch via `provider.models` allowlist
- **VS Code array format**: agent frontmatter uses `['gpt-4o']` (array) not `"gpt-4o"` (string) — preserved from existing convention

## Compliance

| FR | Status |
|----|--------|
| FR-001: Single canonical file per IDE | ✅ 4 files exist |
| FR-002: Unified JSON schema | ✅ All 4 follow same schema |
| FR-003: Installer consumes JSON | ✅ All 3 consumers updated |
| FR-004: User override preservation | ✅ Merge logic verified |
| FR-005: Migration of existing files | ✅ Old files deleted/redirected |
| FR-006: Validation | ✅ CI validator passing |

All 67 acceptance criteria verified. 4/4 PM tests passed.

## Model assignments per IDE

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

## How to add a new agent

When adding a new agent (e.g., `forge-reviewer`), follow these steps:

1. **Add entry to all 4 JSON files:**
   ```bash
   # For each IDE, add to ide/{ide}/config/agent-models.json
   "forge-reviewer": {
     "model": "<appropriate-model>",
     "fallback": "<fallback-model>",
     "mode": "subagent",
     "purpose": "Code review and feedback"
   }
   ```

2. **Update `provider.models` if needed:**
   - If the new agent uses a model not in the allowlist, add it to `provider.models`

3. **Run validation:**
   ```bash
   bash scripts/validate-agent-models.sh
   ```

4. **Regenerate IDE artifacts:**
   ```bash
   # OpenCode
   bash ide/opencode/generate-config.sh /path/to/repo
   
   # Cursor
   python3 ide/cursor/compile-agents-from-skills.py
   
   # Antigravity & VS Code
   bash ide/install.sh
   ```

5. **Commit changes:**
   - All 4 JSON files
   - Regenerated artifacts (opencode.json, cursor agents, etc.)
   - No script changes needed (consumers read JSON dynamically)

## How to change a model

When a provider renames a model (e.g., `big-pickle` → `big-pickle-v2`):

1. **Update `agent-models.json`:**
   ```json
   "agents": {
     "forge-orchestrator": { "model": "big-pickle-v2", ... }
   }
   ```

2. **Update `provider.models` allowlist:**
   ```json
   "provider": {
     "models": ["big-pickle-v2", ...]  // remove old, add new
   }
   ```

3. **Run validation and regenerate** (same as steps 3-5 above)

4. **No script changes needed** — consumers read JSON dynamically

## Files created/modified

### New files (4)

- `ide/opencode/config/agent-models.json` — OpenCode Zen free-tier models
- `ide/cursor/config/agent-models.json` — Cursor Budget tier models
- `ide/antigravity/config/agent-models.json` — Gemini models
- `ide/vscode/config/agent-models.json` — Copilot free-tier models

### Modified files (4)

- `ide/opencode/generate-config.sh` — reads from `config/` instead of `templates/`
- `ide/cursor/compile-agents-from-skills.py` — `json.load()` replaces hardcoded `MODELS` dict
- `ide/install.sh` — reads from `config/` path
- `.agents/rules/model-assignments.md` — replaced with redirect document

### Deleted files (2)

- `ide/opencode/templates/agent-models.json` — replaced by `config/agent-models.json`
- `ide/opencode/templates/model-assignments.md.tpl` — now generated inline

### Regenerated files (8+)

- `ide/vscode/agents/*.agent.md` — model frontmatter updated to `gpt-4o`
- `ide/cursor/agents/*.md` — recompiled with models from JSON
- `~/.config/opencode/opencode.json` — regenerated by `generate-config.sh`
- `~/.cursor/rules/model-assignments.mdc` — regenerated by `install.sh`
- `~/.gemini/config/rules/model-assignments.md` — regenerated by `install.sh`

## References

- Feature directory: `.ai-work/model-config-architecture/`
- Spec: `.ai-work/model-config-architecture/spec.md`
- Plan: `.ai-work/model-config-architecture/plan.md`
- Verify report: `.ai-work/model-config-architecture/verify-report.md`
- Summary: `.ai-work/model-config-architecture/summary.md`
- ADR-008: IDE installer path matrix (informed JSON file locations)
