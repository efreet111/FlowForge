---
title: "Installer protection policy for agent quality work"
type: policy
topic_key: "policy/installer-protection"
date: "2026-07-21"
scope: team
---

## What
During agent quality improvement work, the installer files are **off-limits** — zero modifications to `ide/install.sh`, `ide/opencode/generate-config.sh`, `ide/cursor/compile-agents-from-skills.py`, or any other installer/build infrastructure.

## Why
The installer is a separate domain with its own AOT compilation, security constraints, and deployment pipeline. Agent quality work focuses on instruction text changes in agent definition files. Mixing installer changes with agent changes creates cross-domain risk:

- Installer changes need separate CI/CD validation
- Agent changes are pure documentation — no runtime effect
- Blurring the boundary makes reverts harder

## Where
- Installer files: `ide/install.sh`, `ide/opencode/generate-config.sh`, `ide/cursor/compile-agents-from-skills.py`
- Agent files: `ide/{cursor,vscode,opencode,antigravity}/agents/*.md`, `skills/*/SKILL.md`

## Learned
- When the task is "agent quality improvement," scope is strictly instruction text
- Installer changes belong to a separate feature workflow (e.g., `fix-installer`)
- Enforce via: `git diff --name-only | grep -v installer` before marking task done
