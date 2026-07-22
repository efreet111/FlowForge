---
alwaysApply: false
description: "Redirect: model assignments are now defined per-IDE in canonical JSON files."
---

## Model Assignments — Redirect

Model assignments are no longer defined in this generic file. Each IDE has its own canonical JSON file that is the single source of truth for agent-to-model mappings.

### Canonical files (per IDE)

| IDE | Path |
|-----|------|
| OpenCode | `ide/opencode/config/agent-models.json` |
| Cursor | `ide/cursor/config/agent-models.json` |
| Antigravity | `ide/antigravity/config/agent-models.json` |
| VS Code | `ide/vscode/config/agent-models.json` |

Each JSON file follows the unified FlowForge agent-models schema (`$schema: https://flowforge.dev/schemas/agent-models-v1.json`) and contains:

- `provider` metadata (id, name, docs_url, allowed models list)
- `agents` block with per-agent model, fallback, mode, and purpose
- `tiers` (optional) for budget/quality tier switching

### Why this changed

Previously this file listed generic model names that didn't match each IDE's actual provider catalog. The per-IDE JSON files ensure agents reference models that actually exist in each environment (Gemini on Antigravity, Zen free-tier on OpenCode, etc.).

### How to customize

Edit the `agents` block in your IDE's `config/agent-models.json`. All consumers (installer scripts, agent compilers, rule generators) read from this file.
