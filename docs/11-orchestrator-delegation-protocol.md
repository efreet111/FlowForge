# Orchestrator Delegation and Configuration Protocol

> **Version**: 1.2
> **Topics**: Multi-agent delegation, CLI config, orchestrator persona, FlowDocs skill delegation

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

## 1b. FlowDocs delegation (documentation skills)

> **Governing ADR**: [`ADR-018`](decisions/ADR-018-flowdocs-skills-adoption.md) · **Base context contract**: [`.ai-work/adopt-flowdocs-skills/base-context-mapping.md`](../.ai-work/adopt-flowdocs-skills/base-context-mapping.md) · **Agent reference**: [`AGENTS.md` §FlowDocs Skills](../AGENTS.md)

`forge-orchestrator` delegates documentation tasks to FlowDoc skills via **two independent cycles**. The orchestrator is the single hub connecting both — no specialist communicates directly with another.

### Trigger table — when to invoke each skill

| Trigger | Skill | Cycle | Invocation mode | Output path |
|---------|-------|-------|-----------------|-------------|
| Create or update HU | `flowdoc-hu` | A | **Direct** (forge-orchestrator → flowdoc-hu) | `docs/tasks/HU-001-HU-099/HU-NNN.md` |
| Create PRD | `flowdoc-prd` | B | Delegated (forge-orchestrator → flowdoc-assist → flowdoc-prd) | `docs/PRD.md` |
| Create product ADR | `flowdoc-adr` | B | Delegated | `docs/architecture/adr/` |
| Create RFC | `flowdoc-rfc` | B | Delegated | `docs/architecture/rfc/` |
| Document API endpoints | `flowdoc-api` | B | Delegated | `docs/endpoints.md` |
| Document DB schema | `flowdoc-db` | B | Delegated | `docs/schema.md` |
| Audit documentation | `flowdoc-review` | B | Delegated | Read-only report (no file output) |
| Discover project context | `flowdoc-discover` | B | Delegated | Base context (no file output) |

### Cycle A: User Stories (`flowdoc-hu` — direct invocation)

- **Path**: `forge-orchestrator` → `flowdoc-hu` (DIRECT, no intermediary)
- **Base context injected**: template ref (`docs/templates/HU-template.md`), output path override (range-bin `docs/tasks/HU-001-HU-099/HU-NNN.md` per ADR-005), `flowforge_slug` for traceability
- **Key rule**: `flowdoc-hu` is NEVER invoked via `flowdoc-assist`. The HU cycle is independent of the base docs cycle and exists BEFORE `flowdoc-assist` enters the picture
- **Cross-cycle routing**: If a post-dev HU reveals a technical decision without an ADR, `forge-orchestrator` routes the request to Cycle B (→ `flowdoc-assist` → `flowdoc-adr`)

### Cycle B: Base documentation (`flowdoc-assist` — delegated)

- **Path**: `forge-orchestrator` → `flowdoc-assist` → {`flowdoc-prd` | `flowdoc-adr` | `flowdoc-rfc` | `flowdoc-api` | `flowdoc-db` | `flowdoc-review` | `flowdoc-discover`}
- **Scope**: `flowdoc-assist` coordinates ONLY base doc specialists (discover/prd/adr/rfc/api/db/review). It does NOT handle HUs
- **Base context injected**: template refs mapped per `base-context-mapping.md`, output paths per ADR-004 boundaries, `flowforge_slug` for traceability
- **Mode A (with base context)**: When invoked within an active FlowForge cycle (CKP-0→4), `flowdoc-assist` receives an already-populated base context from `forge-discovery`. It does NOT run autonomous discovery (avoids F-8 duplication)
- **Mode B (standalone)**: When invoked outside a FlowForge cycle, `flowdoc-assist` may run `flowdoc-discover` to build its own base context

### Base context contract

Before invoking any flowdoc skill, `forge-orchestrator` injects the following into the base context:

```json
{
  "flowforge_slug": "<current-feature-slug>",
  "template_refs": {
    "hu": "docs/templates/HU-template.md",
    "adr": "docs/templates/adr-template.md",
    "rfc": "docs/templates/rfc-template.md",
    "prd": "docs/templates/PRD.md"
  },
  "output_paths": {
    "hu": "docs/tasks/HU-001-HU-099/HU-{NNN}.md",
    "adr": "docs/architecture/adr/",
    "rfc": "docs/architecture/rfc/",
    "prd": "docs/PRD.md"
  },
  "invocation_mode": {
    "hu": "direct",
    "base_docs": "delegated_via_flowdoc_assist"
  }
}
```

> Full mapping contract: [`.ai-work/adopt-flowdocs-skills/base-context-mapping.md`](../.ai-work/adopt-flowdocs-skills/base-context-mapping.md)

### Two-cycle interaction example

```
1. User requests new feature
2. forge-orchestrator → flowdoc-hu (Cycle A, direct) → creates HU-025
3. forge-orchestrator → CKP-0→4 cycle (discovery → arch → plan → dev → verify → memory)
4. Post-dev: flowdoc-hu updates HU-025, reveals a new architectural decision
5. forge-orchestrator detects decision without ADR → routes to Cycle B
6. forge-orchestrator → flowdoc-assist → flowdoc-adr (Cycle B, delegated) → creates ADR
```

`forge-orchestrator` is the hub. Specialists never call each other directly.

### Degradation protocol

If a flowdoc skill is not installed or not responding:

1. **Report the error** with the skill name and expected path (`~/.config/opencode/skills/flowdoc-{name}/SKILL.md`)
2. **Suggest remediation** — three options:
   - **Install**: `opencode skills install flowdoc-{name}`
   - **Skip**: Defer the documentation task, log in session register
   - **Standalone mode**: Generate the document manually following the template, without the skill
3. **NEVER fail silently** — the operator must know the skill was unavailable

### Key invariants

- `flowdoc-hu` is NEVER invoked via `flowdoc-assist` — it's a direct call from `forge-orchestrator`
- `flowdoc-assist` coordinates ONLY base docs (discover/prd/adr/rfc/api/db/review) — NOT HUs
- HU cycle is independent of base docs cycle; `forge-orchestrator` is the hub connecting both
- No specialist-to-specialist communication — all coordination passes through the orchestrator
- `.atl/skill-registry.md` is auto-generated — do NOT edit manually

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
- [`20-flowdoc-ecosystem.md`](20-flowdoc-ecosystem.md) — FlowDoc v2.0 adopter guide + skill version pins
- [`decisions/ADR-018-flowdocs-skills-adoption.md`](decisions/ADR-018-flowdocs-skills-adoption.md) — two-cycle model decision
- [`AGENTS.md` §FlowDocs Skills](../AGENTS.md) — invocation protocol table for agents
- [`.ai-work/adopt-flowdocs-skills/base-context-mapping.md`](../.ai-work/adopt-flowdocs-skills/base-context-mapping.md) — template ref + path mapping contract
