---
title: "Installer Protection Policy for Agent Quality Work"
type: "pattern"
topic_key: "development/workflow/installer-protection"
date: "2026-07-22"
scope: "team"
---

## What

During agent quality improvement work, **zero modifications** are allowed to installer files: `ide/install.sh`, `ide/opencode/generate-config.sh`, `ide/cursor/compile-agents-from-skills.py`, or any installer logic in `src/FlowForge.Installer/`.

## Why

The installer is a separate domain:
- Compiled as AOT (Ahead-of-Time) native binary — different constraints from agent instruction text
- Has its own security requirements (PII scanning, file permissions, atomic writes)
- Any change could break the entire installation pipeline for all 4 IDEs
- Agent quality changes are purely instructional text — they don't need installer changes

## Where

- Protected files: `ide/install.sh`, `ide/install.ps1`, `ide/opencode/generate-config.sh`, `ide/cursor/compile-agents-from-skills.py`, `src/FlowForge.Installer/*`
- Non-protected: `ide/*/agents/*.md`, `ide/*/rules/*.md`, `ide/*/config/*.json`, `skills/*/SKILL.md`, `ide/shared/*.md`

## Learned

1. Document protection policy in spec.md's scope-out section explicitly
2. If agent quality requires installer changes, it's a separate feature with its own flow cycle
3. This policy prevents accidental breakage of the install pipeline during text-only improvements
4. The Cursor compiler script IS protected because it's part of the installer pipeline (not because it's an agent)
