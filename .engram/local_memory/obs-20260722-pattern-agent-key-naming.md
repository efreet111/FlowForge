---
title: "Agent Key Naming Convention: forge-orchestrator"
type: "pattern"
topic_key: "development/conventions/agent-key-naming"
date: "2026-07-22"
scope: "team"
---

## What

Agent identity keys follow the pattern `forge-{role}` (e.g., `forge-orchestrator`, `forge-arch`, `forge-dev`). The orchestrator agent key is `forge-orchestrator`, **not** `flowforge`, `orchestrator`, or `forge-flowforge`.

## Why

During model configuration architecture work, the agent key `flowforge` was found in some JSON files and `model-assignments.md`. This is inconsistent with the naming convention used everywhere else (`forge-discovery`, `forge-arch`, etc.). All agent-models.json files now use `forge-orchestrator`.

## Where

- `ide/*/config/agent-models.json` — all 4 files
- `ide/*/rules/model-assignments.md` — generated files

## Learned

1. Agent keys are immutable once established — never rename them across files
2. All agent keys must follow the `forge-{role}` pattern
3. Orchestrator key is `forge-orchestrator` — if anyone uses `flowforge` or `orchestrator`, it's a bug
4. Document this in model-assignments.md generation to prevent drift
