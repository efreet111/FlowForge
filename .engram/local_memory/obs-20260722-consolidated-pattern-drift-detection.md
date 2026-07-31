---
title: "[CONSOLIDATED] Drift Detection via Sync Comments"
type: pattern
topic_key: "development/workflow/drift-detection"
date: "2026-07-22"
scope: team
consolidates:
  - "obs-20260722-pattern-drift-detection.md"
  - "obs-20260722-pattern-drift-detection-sync-comments.md"
---

## What

The `<!-- sync: path/to/canonical-source.md -->` HTML comment pattern for drift detection across duplicated protocol blocks in agent definitions. Protocol blocks duplicated across multiple agent files are annotated with these comments, creating a grep-able link back to the canonical source.

## Why

FlowForge agents share protocols (Memory Curation Protocol, CKP system definitions) across 4 IDEs and their SKILL.md sources. Without drift markers, protocol updates in one file are silently missed in others — 4 copies of the CKP table had already diverged slightly (different capitalizations of "deploy gate"). The sync comment enables grep-based drift detection.

## Where

Applied across 7+ files (commit `6b4aa88`):
- `skills/forge-orchestrator/SKILL.md` — CKP table + Memory Curation Protocol
- `ide/cursor/agents/forge-orchestrator.md` — compiled from SKILL.md (sync comment survives recompilation)
- `ide/vscode/agents/forge-orchestrator.agent.md`
- `ide/opencode/agents/flowforge.md`
- `ide/antigravity/rules/workflow.md`

## Format

```html
<!-- sync: relative/path/to/canonical.md -->
```

Place immediately BEFORE the duplicated block.

## Learned

1. Format: `<!-- sync: relative/path/to/canonical -->` — always point to the canonical source, not another derivative
2. Place immediately BEFORE the duplicated block, not inside it
3. Detect drift: `rg '<!-- sync:'` finds all drift points for manual verification
4. This is a lightweight alternative to automated sync — sufficient for a 4-IDE project
5. Cursor agents compile from SKILL.md, so the sync comment survives recompilation (tested in Cycle 2 rework)
6. Future: CI lint could verify sync targets exist and content matches between source and target
7. Self-containment principle: keep protocol blocks inline for readability, mark with sync comment for auditability
