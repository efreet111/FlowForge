---
title: "Pattern: Unified JSON schema for IDE model configuration"
type: "pattern"
topic_key: "architecture/model-config/unified-schema"
date: "2026-07-18"
scope: "team"
project: "flowforge"
significance: "high"
---

## What

All 4 IDEs now share a single JSON schema (`agent-models-v1.json`). Each `agent-models.json` file contains: `$schema`, `provider` (id, name, docs_url, models allowlist), `active_tier`, `tiers` (optional named overrides), and `agents` (9 required keys: forge-orchestrator, forge-discovery, forge-arch, forge-plan, forge-dev, forge-verify, forge-memory, forge-teacher, default).

## Why

- Before: 5+ scattered sources (templates, hardcoded dicts, Markdown tables, YAML frontmatter) — all could drift independently.
- After: one file per IDE, same schema. CI validates all 4 with one script.
- Adding a new agent: just add it to each JSON → all consumers pick it up automatically.
- Model rename: change one JSON value → all generated files update.

## Where

- `ide/*/config/agent-models.json` — all 4 files follow this pattern
- `scripts/validate-agent-models.sh` — single jq invocation per file validates schema compliance

## Learned

The `flowforge` → `forge-orchestrator` agent key rename was necessary to maintain consistency with the rest of FlowForge's agent naming (`forge-*`). When introducing a canonical schema, ensure all historic aliases are mapped to the canonical key.

Key jq optimization: a single `jq -r '[...] | .[]'` invocation for all structural checks runs ~6x faster than multiple jq calls per file (~29ms vs ~180ms). But agent key presence must be checked separately because jq's `to_entries` doesn't catch missing keys.
