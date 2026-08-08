---
title: "[CONSOLIDATED] Parallel data structures not migrated to canonical JSON"
type: bugfix
topic_key: "bugs/parallel-data-structures"
date: "2026-07-22"
scope: team
consolidates:
  - "obs-20260718-bug-hardcoded-dict-descriptions.md"
  - "obs-20260722-bug-descriptions-dict.md"
---

## What

When migrating `compile-agents-from-skills.py` from hardcoded Python dicts to JSON loading, only the primary `MODELS` dict was replaced. A parallel `DESCRIPTIONS` dict (mapping agent keys to purpose/description strings) was left behind, keeping hardcoded values that could drift from the canonical JSON.

The `DESCRIPTIONS` dict was not considered part of "model configuration" during the initial scope, so it was overlooked. But it contained per-agent metadata that should live in the canonical JSON.

## Why

The migration scope was "model configuration" — agent names, providers, model assignments, fallbacks. The purpose/description strings were labeled as "documentation metadata" and were not considered in scope. However, they are per-agent data that must remain in sync with the model assignments.

## Where

- `ide/cursor/compile-agents-from-skills.py` (fixed in commit `2637562`)
- Fix: Extract agent purpose/description from canonical JSON's `purpose` field in the `agents` block

## Learned

1. When replacing a hardcoded data structure with an external config file, audit the ENTIRE file for ALL hardcoded data, not just the obvious one
2. Look for parallel structures (descriptions, fallbacks, modes, tier mappings) that may also need migration
3. Use `grep` for hardcoded agent names or model values to find all of them
4. Schema design: include optional metadata fields (purpose, description) even if not all consumers use them yet
5. Rule of thumb: "if it's per-agent, it goes in the agent JSON"
6. Parallel data structures are a code smell — they indicate incomplete migration or abstraction leakage
