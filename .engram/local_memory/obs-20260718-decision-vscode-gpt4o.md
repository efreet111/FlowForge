---
title: "Decision: VS Code Copilot agents use gpt-4o for all roles"
type: "decision"
topic_key: "architecture/model-config/vscode-models"
date: "2026-07-18"
scope: "team"
project: "flowforge"
significance: "high"
---

## What

All VS Code Copilot agents (8 `.agent.md` files) use `gpt-4o` as their single model — no tier differentiation, no fallback chain. This replaces the previous frontmatter that listed `['claude-sonnet-4-20250514', 'gpt-5.2']`.

## Why

- CKP-1 BLOCKER OQ-2: human confirmed `gpt-4o` is available to Copilot free-tier users; `claude-sonnet-4` and `gpt-5.2` are not.
- VS Code Copilot's agent system doesn't differentiate agent roles — all agents use the same underlying model capability.
- Simplicity: one model, one source of truth, no tier complexity.

## Where

- `ide/vscode/config/agent-models.json` — canonical source (all agents → `gpt-4o`)
- `ide/vscode/agents/*.agent.md` — 8 files, all frontmatter updated to `model: ['gpt-4o']`

## Learned

VS Code agent frontmatter uses array format for model (`['gpt-4o']`), not string. Preserved this format during migration (plan risk R7 mitigation). When instrumenting a new IDE, always check whether the agent schema expects a string or array for model values.
