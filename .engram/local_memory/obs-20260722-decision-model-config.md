---
title: "IDE-specific canonical JSON for model configuration"
type: decision
topic_key: "architecture/model-config"
date: "2026-07-18"
scope: team
---

## What
Each IDE (OpenCode, Cursor, Antigravity, VS Code) now has its own canonical `config/agent-models.json` file at `ide/{ide}/config/agent-models.json`. All consumers (installer scripts, agent compilers, rule generators) read from this single source of truth.

## Why
Previous model assignments were scattered across 5+ files in mixed formats (JSON, Markdown, hardcoded Python dicts, YAML frontmatter). Antigravity listed Claude/GPT models that don't exist in the Gemini ecosystem. This caused confusion, installer fragility, and prevented the Starter Kit's "5-minute first flow" goal.

## Where
- `ide/opencode/config/agent-models.json`
- `ide/cursor/config/agent-models.json`
- `ide/antigravity/config/agent-models.json`
- `ide/vscode/config/agent-models.json`
- `scripts/validate-agent-models.sh`
- `docs/decisions/ADR-012-ide-specific-model-config.md`

## Learned
- Unified JSON schema: `$schema`, `provider`, `agents` (per agent with model/fallback), `tiers`, `active_tier`
- Agent key naming: `forge-orchestrator` (not `flowforge`)
- User overrides preserved on reinstall via deep-merge strategy (FR-004)
- CI validator: ~29ms per file using shell + jq, validates JSON, schema, agent keys, model references
