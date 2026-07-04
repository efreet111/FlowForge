# Orchestrator Delegation and Configuration Protocol

> **Version**: 1.1
> **Topics**: Multi-agent delegation, CLI config, orchestrator persona

FlowForge must work across IDE ecosystems. The **orchestrator (`forge-orchestrator`)** needs clear rules for handing off to the other six agents and for reading team preferences from config files.

---

## 1. Delegation dilemma (handoff pattern)

Platforms differ:

- Chat-only (some Copilot modes).
- Subagent-capable (Cursor, OpenCode, Antigravity, Cline).

The orchestrator uses **adaptive delegation**:

### Rules

1. **Autonomous when possible**: If the environment supports `Task` / subagents / `call_agent`, the orchestrator **invokes the next agent itself** without asking the human to copy prompts.
2. **Assisted fallback**: If not, provide an exact instruction: *“Please invoke `forge-dev` to start implementation.”*
3. **Context injection**: Always pass file paths — e.g. *“Read `.ai-work/{slug}/plan.md` and implement…”* — not vague “continue coding.”

### Bug reports

Orchestrator creates `.ai-work/{slug}/rework_ticket.md` and delegates to **forge-dev**. It does **not** patch `src/`, tests, or dashboards inline.

---

## 2. Configuration and CLI wizard (future)

Configuration is intended to live in the repo root as **`.flowforge.json`** (or under `"forge"` in `.engram.json`).

Planned `forge init` / wizard topics:

1. Models per phase.
2. API keys (secure storage).
3. Orchestrator persona / tone.
4. Engram endpoint (local SQLite, cloud, Postgres).

Example shape:

```json
{
  "forge": {
    "orchestrator_mode": "adaptive",
    "persona": "Senior software architect, formal and direct.",
    "agents": {
      "forge-discovery": { "model": "gpt-5-mini", "provider": "openai" },
      "forge-arch": { "model": "kimi-k2.7-code", "provider": "moonshot" }
    },
    "database_engine": {
      "type": "postgres",
      "connection_string_env": "ENGRAM_DB_URL"
    }
  }
}
```

The orchestrator reads this at session start when present. Until the wizard ships, use [`ide/cursor/rules/model-assignments.mdc`](../ide/cursor/rules/model-assignments.mdc) and per-IDE packs.

---

## 3. Artifact paths (canonical)

```
.ai-work/{feature-slug}/
├── context-map.md
├── spec.md          # includes PM-* manual tests
├── plan.md          # checklist — marked by forge-dev
├── verify-report.md
├── rework_ticket.md
├── revision_cycle.md
└── summary.md
```

Use **kebab-case** slugs. Do not use `FLOW-` prefix or `cert-report.md`.

---

## 4. Related documents

- [`06-ai-orchestrator.md`](06-ai-orchestrator.md) — traffic light semantics
- [`14-flowforge-complete-reference.md`](14-flowforge-complete-reference.md) — full reference + test cases
- [`16-ide-integration-plan.md`](16-ide-integration-plan.md) — per-IDE file layout
