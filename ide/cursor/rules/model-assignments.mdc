---
alwaysApply: false
description: Model assignments for FlowForge agents. Adapt to your available models.
---

## Model Assignments

The orchestrator reads this table at session start. When delegating, use the preferred model.

| Agent | Preferred Model | Fallback | Mode | Purpose |
|-------|----------------|----------|------|---------|
| forge-discovery | `claude-4.5-haiku-thinking` | `gpt-5-mini` | pinned | Fast search, cheap |
| forge-arch | `claude-4.5-sonnet-thinking` | `gpt-5.2` | pinned | Reasoning + spec writing |
| forge-plan | `claude-4.5-sonnet-thinking` | `gpt-5.2` | pinned | Architecture + task breakdown |
| forge-dev | `gpt-5.3-codex` | `claude-4.5-sonnet-thinking` | pinned | Code iteration |
| forge-verify | `claude-4.6-opus-high-thinking` | `claude-4.5-sonnet-thinking` | pinned | Rigorous audit |
| forge-memory | `claude-4.5-haiku-thinking` | `gpt-5-mini` | pinned | Documentation, cheap |
| default | `claude-4.5-haiku-thinking` | `gpt-5-mini` | pinned | Cheap fallback |

## Cost Policy

- Discovery/Memory: cheapest model (read/classify only)
- Dev: specialized code model (iterative)
- Arch/Plan: reasoning model (design heavy)
- Verify: strongest model (judgment quality matters)

Adapt this file to the models available in your environment.
