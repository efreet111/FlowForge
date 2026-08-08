---
title: "Session Close — Model Configuration Architecture"
type: "session_summary"
topic_key: "architecture/model-config"
date: "2026-07-18"
scope: "team"
project: "flowforge"
significance: "high"
---

## Goal

Establish a single-source-of-truth per IDE for agent-to-model mappings. Four canonical JSON files (one per IDE under `ide/{ide}/config/agent-models.json`) replace 5+ scattered model sources (templates, hardcoded Python dicts, stale Markdown files) that referenced wrong models (e.g., Claude/GPT on Antigravity which uses Gemini).

## Discoveries

1. **Antigravity was using Claude/GPT models**: The `model-assignments.md` for Antigravity listed `claude-sonnet-4` and `gpt-5.2` — models that don't exist in Gemini/Antigravity. Fixed to `gemini-3-flash` (cheap/fast) and `gemini-3-pro` (powerful).

2. **VS Code agents had wrong model frontmatter**: All 8 `.agent.md` files listed `['claude-sonnet-4-20250514', 'gpt-5.2']` — neither available in Copilot free tier. Fixed to `['gpt-4o']`.

3. **Cursor MODELS dict was hardcoded in Python**: `compile-agents-from-skills.py` had a dict that required manual edits when models changed. Replaced with `json.load()` for CI-versioned config.

4. **Directory `templates/` vs `config/` confusion**: Old template files at `ide/opencode/templates/agent-models.json` were being read by the installer but had a flat structure. Moved to `ide/opencode/config/agent-models.json` with the new layered schema.

5. **`flowforge` → `forge-orchestrator` rename**: The canonical JSON files use `forge-orchestrator` as the agent key (not `flowforge`), matching all other FlowForge agent naming.

## Accomplished

- **4 canonical JSON files created**: `ide/{opencode,cursor,antigravity,vscode}/config/agent-models.json`
- **3 consumer scripts updated**: `generate-config.sh`, `compile-agents-from-skills.py`, `install.sh`
- **2 old files deleted**: `templates/agent-models.json`, `templates/model-assignments.md.tpl`
- **1 redirect created**: `.agents/rules/model-assignments.md` → points to per-IDE JSON files
- **8 VS Code agent files updated**: model frontmatter changed to `gpt-4o`
- **1 CI validator created**: `scripts/validate-agent-models.sh` — single jq invocation per file, ~29ms
- **67 acceptance criteria verified**: all 13 plan tasks pass with 100% compliance
- **4 PM tests passed**: Fresh install (PM-1), user override (PM-2), Cursor recompilation (PM-3), CI validation (PM-4)
- **1 rework cycle resolved**: missing `context-map.md` (P3 process gap — mechanical CKP-0 violation)

## Next Steps

- Deploy (CKP-4 human decision)
- Update `docs/decisions/ADR-008-ide-installer-path-matrix.md` to reference new config paths
- Consider hosting the `$schema` URL at `flowforge.dev/schemas/agent-models-v1.json` (OQ-3 follow-up)
- Potential `flowforge doctor` live model validation (OQ-5 follow-up)

## Relevant Files

- `ide/opencode/config/agent-models.json` — New canonical OpenCode Zen free-tier
- `ide/cursor/config/agent-models.json` — New canonical Cursor Budget tier
- `ide/antigravity/config/agent-models.json` — New canonical Antigravity Gemini
- `ide/vscode/config/agent-models.json` — New canonical VS Code Copilot free
- `scripts/validate-agent-models.sh` — CI validator (single jq invocation, ~29ms/file)
- `ide/opencode/generate-config.sh` — Updated to read from `config/` not `templates/`
- `ide/cursor/compile-agents-from-skills.py` — Replaced MODELS dict with json.load()
- `ide/install.sh` — Updated paths, Antigravity Gemini generation
- `.agents/rules/model-assignments.md` — Redirect doc
- `ide/README.md` — Updated model config section
- `.ai-work/model-config-architecture/` — Full feature artifacts
