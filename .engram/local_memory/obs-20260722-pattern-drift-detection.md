---
title: "Drift detection via sync comments"
type: pattern
topic_key: "patterns/drift-detection"
date: "2026-07-21"
scope: team
---

## What
Protocol blocks duplicated across multiple agent files are annotated with `<!-- sync: path/to/canonical-source.md -->` comments. This creates a grep-able link back to the canonical source.

## Why
Without drift markers, updating a shared protocol (e.g., Memory Signal, CKP rules) requires finding every copy manually. With `<!-- sync: -->` comments, a developer can run `rg '<!-- sync:'` to find all derivative copies and verify they match the canonical source.

## Where
- All 7 × 4 = 28+ agent files across Cursor, VS Code, OpenCode, Antigravity
- `ide/shared/workflow-orchestrator-parity.md` (canonical protocol source)
- Pattern established in commit `6b4aa88`

## Learned
- Format: `<!-- sync: relative/path/to/canonical.md -->`
- Always point to the canonical source, not another derivative
- Grep: `rg '<!-- sync:'` finds all drift points
- Future: CI lint could verify sync targets exist and match
