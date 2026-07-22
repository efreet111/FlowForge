# Summary: Model Configuration Architecture

> **Phase 4 — CKP-4 🟢 Ready for deploy gate**
> **Date:** 2026-07-18
> **Agent:** forge-memory

---

## Feature Overview

**Slug:** `model-config-architecture`

**Problem:** FlowForge had model assignments scattered across 5+ files (templates, hardcoded Python dicts, stale Markdown tables). Antigravity showed Claude/GPT models that don't work in Gemini. VS Code showed premium models unavailable in Copilot free tier.

**Solution:** One canonical JSON file per IDE (`ide/{ide}/config/agent-models.json`) — all consumers read from it, no duplication.

---

## What Was Implemented

| Artifact | Count | Description |
|----------|-------|-------------|
| Canonical JSON files | 4 | `ide/{opencode,cursor,antigravity,vscode}/config/agent-models.json` |
| Consumer scripts updated | 3 | `generate-config.sh`, `compile-agents-from-skills.py`, `install.sh` |
| Old files deleted | 2 | `templates/agent-models.json`, `templates/model-assignments.md.tpl` |
| Redirect created | 1 | `.agents/rules/model-assignments.md` |
| VS Code agents updated | 8 | Frontmatter from `claude-sonnet-4/gpt-5.2` → `gpt-4o` |
| CI validator created | 1 | `scripts/validate-agent-models.sh` (~29ms per file) |
| ADR created | 1 | `docs/decisions/ADR-012-ide-specific-model-config.md` |

## Key Decisions

| Decision | Value |
|----------|-------|
| **Antigravity models** | `gemini-3-flash` (discovery/memory/teacher), `gemini-3-pro` (orchestrator/arch/plan/dev/verify) |
| **VS Code models** | `gpt-4o` for all agents (Copilot free tier) |
| **Agent key rename** | `flowforge` → `forge-orchestrator` in all JSON files |
| **Schema** | Unified v1 with `$schema`, `provider`, `agents`, `tiers`, `active_tier` |

## Metrics

| Metric | Value |
|--------|-------|
| Tasks completed | 13/13 |
| Acceptance criteria passed | 67/67 |
| PM tests passed | 4/4 (PM-1 through PM-4) |
| JSON files validated | 4/4 (all PASS) |
| Rework cycles | 1 (P3: missing context-map.md — resolved) |
| Models corrected | 36 assignments (9 agents × 4 IDEs) verified |

## Bugs Found & Fixed

| Bug | Fix |
|-----|-----|
| `jq def func($var)` variable scoping issue | Inline filter logic instead of `def` |
| `git commit` timeout with long multi-line messages | Keep commit messages concise (≤72 chars) |
| Parallel `DESCRIPTIONS` dict not migrated | Extract purpose/description from JSON alongside model |

## Patterns Established

- **Unified JSON schema** for all IDE model config — extensible to new IDEs
- **Single jq invocation** for validation (~6x faster than multiple calls)
- **IDE-specific config** — no generic `model-assignments.md` with wrong models
- **Consumer contract**: JSON → generated config (installer never contains model names)

## What to Remember

1. Antigravity = Gemini models only (no Claude/GPT)
2. VS Code Copilot free tier = `gpt-4o` only
3. When adding a new agent: edit 4 JSON files, scripts pick it up automatically
4. CI validator at `scripts/validate-agent-models.sh` — run before commit
5. User overrides survive reinstall via merge logic in `generate-config.sh`

## Next Steps (Post-Deploy)

1. Host `$schema` URL at `flowforge.dev/schemas/agent-models-v1.json`
2. Create dedicated generator scripts for Antigravity and VS Code
3. Update ADR-008 to reference new config paths
4. Consider `flowforge doctor` live model validation

## ✅ Pruebas Manuales del Desarrollador

| ID | Test | Status |
|----|------|--------|
| PM-1 | Fresh install generates correct models per IDE | ✅ ejecutada |
| PM-2 | User override survives reinstall | ✅ ejecutada |
| PM-3 | Cursor recompilation reads JSON | ✅ ejecutada |
| PM-4 | CI validation catches broken model reference | ✅ ejecutada |

Verificadas por el desarrollador humano.

---

## Memory Signal

- type: implementation
- significance: high
- summary: "Model Configuration Architecture implemented. 4 canonical JSON files (one per IDE under `ide/{ide}/config/agent-models.json`) are now the single source of truth for agent-to-model mappings. Key patterns: (1) unified JSON schema with `$schema`, provider, agents, tiers, active_tier fields; (2) `flowforge` → `forge-orchestrator` agent key rename; (3) Antigravity corrected from Claude/GPT to Gemini (flash/pro); (4) VS Code corrected from `claude-sonnet-4/gpt-5.2` to `gpt-4o`; (5) CI validator at `scripts/validate-agent-models.sh` runs in ~29ms/file with single jq invocation per file."
