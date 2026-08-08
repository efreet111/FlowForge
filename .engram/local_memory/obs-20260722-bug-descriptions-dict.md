---
title: "Parallel DESCRIPTIONS dict not migrated to JSON"
type: bugfix
topic_key: "bugs/parallel-data-structures"
date: "2026-07-18"
scope: team
---

## What
`compile-agents-from-skills.py` had a hardcoded `DESCRIPTIONS` Python dict mapping agent keys to purpose/description strings. When model data was migrated into the canonical JSON files, the DESCRIPTIONS dict was left behind — creating a parallel data structure that could drift.

## Why
The migration scope was "model configuration" — agent names, providers, model assignments, fallbacks. The purpose/description strings were not considered part of "model configuration" and were overlooked.

## Where
- `ide/cursor/compile-agents-from-skills.py` (fixed in commit `2637562`)
- Fix: Extract agent purpose/description from the canonical JSON alongside model/fallback

## Learned
- When migrating data to a new canonical source, identify ALL data structures that consume the old format
- grep for hardcoded dicts, arrays, and maps that contain per-agent values
- Schema design: include optional metadata fields (purpose, description) even if not all consumers use them yet
- Future-proofing: "if it's per-agent, it goes in the agent JSON"
