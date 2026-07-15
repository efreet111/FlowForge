---
title: "OpenCode installer config gen — free Zen SSOT"
type: decision
topic_key: architecture/opencode-installer-config
date: 2026-07-15
scope: team
project: flowforge
---

## What

Instaladores bash + C# generan `opencode.json` completo PII-free con 8 modelos OpenCode Zen free, `model-assignments.md` desde provider, sidecar managed-paths. v0.1.0-alpha.12.

## Why

Multiagent OpenCode inoperativo: agents/*.md sin registro en opencode.json; template Antigravity con modelos Cursor y PII.

## Where

`ide/opencode/templates/`, `src/FlowForge.Installer/Modules/OpenCode/*`, `ide/opencode/generate-config.sh`

## Learned

Sidecar para merge; PII scan JSON-aware; paridad bash per-provider model resolution.
