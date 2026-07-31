---
title: "Drift Detection via Sync Comments"
type: "pattern"
topic_key: "development/workflow/drift-detection"
date: "2026-07-22"
scope: "team"
---

## What

The `<!-- sync: path/to/canonical-source.md -->` HTML comment pattern for drift detection across duplicated protocol blocks in agent definitions.

## Why

FlowForge agents share protocols (Memory Curation Protocol, CKP system definitions) across 4 IDEs and their SKILL.md sources. Without drift markers, protocol updates in one file are silently missed in others. The sync comment enables grep-based drift detection.

## Where

Applied across 7+ files:
- `skills/forge-orchestrator/SKILL.md` — CKP table + Memory Curation Protocol
- `ide/cursor/agents/forge-orchestrator.md` — compiled from SKILL.md
- `ide/vscode/agents/forge-orchestrator.agent.md`
- `ide/opencode/agents/flowforge.md`
- `ide/antigravity/rules/workflow.md`

## Learned

- Format: `<!-- sync: relative/path/to/canonical -->`
- Place immediately BEFORE the duplicated block
- Detect drift: `rg '<!-- sync:'` and verify content matches between source and target
- This is a lightweight alternative to automated sync — sufficient for a 4-IDE project
- Cursor agents compile from SKILL.md, so the sync comment survives recompilation
