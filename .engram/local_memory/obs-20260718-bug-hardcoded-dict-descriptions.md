---
title: "Bug: When expanding hardcoded dict to JSON, also expand parallel data structures"
type: "bugfix"
topic_key: "architecture/model-config/bugs"
date: "2026-07-18"
scope: "team"
project: "flowforge"
significance: "medium"
---

## What

When migrating the Cursor `compile-agents-from-skills.py` from a hardcoded `MODELS` dict to JSON loading, the initial implementation only replaced the `MODELS` dict itself. It missed a parallel `DESCRIPTIONS` dict that also contained per-agent metadata. This caused generated agents to have incomplete frontmatter.

## Why

The original Python script had two parallel data structures: `MODELS` (maps agent key → model string) and `DESCRIPTIONS` (maps agent key → description string). Both must be sourced from the canonical JSON or generated dynamically. Extracting descriptions from the JSON's `purpose` field solved it.

## Where

- `ide/cursor/compile-agents-from-skills.py` — both `MODELS` and `DESCRIPTIONS` replaced with JSON-driven data

## Learned

When replacing a hardcoded data structure with an external config file, audit the entire file for ALL hardcoded data, not just the obvious one. Look for parallel structures (descriptions, fallbacks, modes, tier mappings) that may also need migration. A grep for hardcoded agent names or model values helps find them all.
