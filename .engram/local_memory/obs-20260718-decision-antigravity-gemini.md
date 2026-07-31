---
title: "Decision: Antigravity default models are Gemini Flash/Pro"
type: "decision"
topic_key: "architecture/model-config/antigravity-models"
date: "2026-07-18"
scope: "team"
project: "flowforge"
significance: "high"
---

## What

Antigravity (Gemini-based IDE) should use `gemini-3-flash` for cheap/fast agent roles (discovery, memory, teacher) and `gemini-3-pro` for reasoning-heavy roles (orchestrator, arch, plan, dev, verify). Replaces the incorrect `claude-sonnet-4` and `gpt-5.2` models that previously appeared in Antigravity config.

## Why

- The old `model-assignments.md` listed Claude/GPT models that **don't exist in Antigravity/Gemini** — agents would fail to spawn.
- CKP-1 BLOCKER OQ-1: human approved Gemini Flash/Pro split.
- `gemini-3-flash` is fast and cheap for exploratory agents; `gemini-3-pro` provides sufficient reasoning for design/implementation/verification.

## Where

- `ide/antigravity/config/agent-models.json` — canonical source
- `ide/antigravity/rules/model-assignments.md` — generated output (Gemini models)
- `.agents/rules/model-assignments.md` — redirect doc (no longer stale)

## Learned

When configuring an IDE for the first time, always verify the provider's actual model catalog before writing defaults. Never assume a model name from one provider exists in another. The `provider.models` list in the JSON schema acts as an allowlist — CI validation catches stale references.
