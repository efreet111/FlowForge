# FlowForge — Architecture

> **Version**: 0.4 (checkpoints normalized)
> **Last updated**: 2026-05-27
> **Status**: Draft — subject to change during implementation
>
> *Legacy name in older notes: EngramFlow.*

---

## 1. Overview

FlowForge is an **Agentic SDLC** methodology for small and medium teams (2–20 people) that want AI agents as active participants in the software lifecycle, without enterprise-style bureaucracy.

### Design principles

| Principle | Explanation |
|-----------|-------------|
| **Less is more** | Seven focused agents instead of 10+ specialized roles. Each extra agent is context loss and maintenance overhead. |
| **Context on demand** | Agents do not get full context in the prompt — they **fetch** it via MCP when needed. Fewer tokens, less irrelevant context. |
| **Model routing per task** | Do not use one model for everything. Complex reasoning → Sonnet/Opus. Read/write → Haiku. Persistence → $0 (direct SQL). |
| **Versioned artifacts as protocol** | `spec.md`, `plan.md`, `verify-report.md`, and `rework_ticket.md` are the contract between agents. The orchestrator is optional — artifacts drive the flow. |
| **Five human checkpoints (CKP-0→4)** | From ~8 human interruptions in a traditional SDLC down to five control points: CKP-0 (binary hard stop), CKP-1 (spec), CKP-2 (plan), CKP-3 (mechanical escalation), CKP-4 (deploy gate). Humans validate intent, architecture, and outcome — not every line of code. |
| **Layered memory** | Two levels: operational (DB with TTL, 30–90 days) and structured (versioned `.md`, permanent). Ephemeral data expires; important knowledge is preserved. |
| **Optional AI orchestrator** | The flow works without a central orchestrator agent. Teams that want one configure it via IDE rules / JSON — it **coordinates**, it does not implement product code. |

### Why not 10 agents?

Research from Agentsway, Twelve-Factor Agentic SDLC, and ADLC shows:

1. **Each handoff loses context** — loss compounds, it does not divide cleanly.
2. **Ten agents mean ten role definitions, ten prompt sets, ten configs** — unbearable overhead for SMBs.
3. **Overlapping roles** (Mission Architect ↔ Intent Engineer, Synthesis ↔ Evolution) conflict.
4. **Model routing beats agent specialization** — one agent with Sonnet for reasoning and Haiku for cheap tasks replaces several specialized agents.

---

## 2. Five phases and five checkpoints (CKP-0→4)

```
┌─────────────────────────────────────────────────────────────────────┐
│                           FLOWFORGE                                 │
│  5 phases | 5 checkpoints (CKP-0→4) | 7 agents | Model routing      │
└─────────────────────────────────────────────────────────────────────┘

PHASE 0: DISCOVERY ─────────── CKP-0 🔴 HARD STOP ───────────────────
│  Cross-memory association, epic mapping, user-story validation
│
│  Agents: Discovery Agent — context analysis
│  Deliverables: Memory association map
│  Models: Haiku / Flash / GPT-4o-mini (fast and cheap)
│
│  ⚠️  Binary, no appeal: vague requirements → STOP EVERYTHING.

PHASE 1: INTENT ────────────── CKP-1 🟡 YELLOW LIGHT ────────────────
│  Priority review and business-intent validation
│
│  Agents: Arch Agent — spec.md + Capability Matrix
│
│  Deliverables: spec.md, Capability Matrix
│  Models: Sonnet (reason), Haiku (read context / tool_use)
│
│  🟡 Human decides: "Approve or adjust?"

PHASE 2: ARCHITECTURE ──────── CKP-2 🟡 YELLOW LIGHT ────────────────
│  Validate and approve the implementation plan
│
│  Agents: Plan Agent — plan.md + task breakdown
│
│  Deliverables: plan.md, MCP contracts
│  Models: Sonnet (architecture, decomposition), Haiku (routines)
│
│  🟡 Human decides: "Green light to code?"

PHASE 3: EXECUTION ─────────── Inner loop (autonomous) ──────────────
│  No human checkpoint — supervised autonomy with self-correction
│
│  Agents:
│    Dev Agent — code + unit tests + Ralph Wiggum loop
│    Verify Agent — traceability vs spec.md + FR/NFR + LLM-as-Judge
│
│  Deliverables: Source, tests, verify-report.md
│  Models: Sonnet/Opus (code), Sonnet (verify), Haiku (simple debug)
│
│  Feedback loop:
│    Verify fails → rework_ticket.md
│    Dev takes ticket as work queue → retry
│    Max 3 cycles → CKP-3 🔴 EMERGENCY BRAKE → ESCALATE to human
│    (Optional AI orchestrator routes rework; it does not patch src/ inline)

PHASE 4: CLOSURE ───────────── CKP-4 🟢 DEPLOY GATE ───────────────
│  Final review + deploy decision
│
│  Agents:
│    Memory Agent — update CLAUDE.md/AGENTS.md + persist engrams
│                    + promote to Level 2 (.md) when appropriate
│
│  Deliverables: Updated CLAUDE.md/AGENTS.md, persisted engrams, structured .md
│  Models: Haiku (structure, classify), $0 (direct SQL for persistence)
│
│  🟢 Human decides: "Deploy?"
```

---

## 3. Roles and responsibilities

### 3.0 Discovery Agent (Phase 0)

| Aspect | Description |
|--------|-------------|
| **Mission** | Map the new user story against past memories or epics (cross-memory) before any planning. |
| **Models** | Fast, cheap models (Haiku, Flash, GPT-4o-mini) — read and classify only. |
| **Tools** | MCP: `search_memory`, `read_epics` |
| **Checkpoint** | Hard stop: insufficient business context → entire flow stops here. |

### 3.1 Arch Agent (Phase 1)

**Merge of**: Knowledge Scout + Mission Architect + Intent Engineer

| Aspect | Description |
|--------|-------------|
| **Mission** | Explore project context and translate human vision into an executable `spec.md` with Capability Matrix |
| **Models** | Sonnet for reasoning, Haiku for context read and tool_use |
| **Tools** | MCP: `read_repo_context`, engram-dotnet search, read docs |
| **Output** | `spec.md`, Capability Matrix |
| **Checkpoint** | Human gate CKP-1 |

**Capability Matrix**: Explicitly defines what the AI may decide vs fixed deterministic logic. Example:

```yaml
capability_matrix:
  ai_reasoning:
    - "Validate email format"           # ✅ AI may infer
    - "Draft UX error message"          # ✅ AI may write copy
  deterministic:
    - "Execute INSERT in DB"            # ❌ Fixed logic
    - "Check JWT expiry"                # ❌ Hard business rule
    - "Compute price with VAT"          # ❌ Math formula
```

### 3.2 Plan Agent (Phase 2)

**Merge of**: Experience Designer (optional) + Scaffold Engineer

| Aspect | Description |
|--------|-------------|
| **Mission** | Turn `spec.md` into atomic tasks with implementation order |
| **Models** | Sonnet for architecture, Haiku for basic wireframes |
| **Tools** | MCP: `read_schema`, `read_existing_code` |
| **Output** | `plan.md`, task breakdown, MCP contracts |
| **Checkpoint** | Human gate CKP-2 |

### 3.3 Dev Agent (Phase 3)

**Role**: Inner-loop agent — core productivity

| Aspect | Description |
|--------|-------------|
| **Mission** | Implement, test, and iterate per `plan.md`; mark checklist items when done |
| **Models** | Sonnet/Opus to code, Haiku for simple debug and file reads |
| **Tools** | MCP: `write_file`, `run_tests`, `read_errors` |
| **Output** | Source code, tests |
| **Loop** | Ralph Wiggum: code → test → fail → fix |

### 3.4 Verify Agent (Phase 3)

**Role**: Sentinel judge + verification + testing

| Aspect | Description |
|--------|-------------|
| **Mission** | Verify code meets `spec.md`, functional/non-functional requirements, no regressions |
| **Models** | Sonnet for judgment, Haiku for quick secret/vulnerability scans |
| **Tools** | MCP: `mem_verify_artifact`, `mem_traceability`, `run_tests` |
| **Output** | `verify-report.md` with PASS/FAIL, or `rework_ticket.md` with specific items |
| **Feedback** | On fail: `rework_ticket.md` with cycle count; Dev Agent resumes |
| **Escalation** | After 3 failed cycles → CKP-3 escalate to human |

### 3.5 Memory Agent (Phase 4)

**Merge of**: Synthesis Agent + Evolution Engine (no QLoRA)

| Aspect | Description |
|--------|-------------|
| **Mission** | Close the cycle: update living docs and persist memory |
| **Models** | Haiku to structure and classify |
| **Tools** | MCP: `mem_save`, `write_file` (.md), `mem_promote_to_md` |
| **Output** | Updated CLAUDE.md/AGENTS.md, persisted engrams, structured `.md` |
| **Pruning** | Not an agent — cron + SQL (Memory Janitor) |

---

## 4. Artifact protocol

Agent handoffs do not require an orchestrator. They require versioned artifacts under `.ai-work/{slug}/`:

```
spec.md ──→ plan.md ──→ code ──→ verify-report.md
                      ↘ rework_ticket.md (on FAIL) ↗
```

| Artifact | Producer | Consumer |
|----------|----------|----------|
| `spec.md` | Arch Agent | Plan, Verify |
| `plan.md` | Plan Agent | Dev (checklist) |
| `verify-report.md` | Verify Agent | Human, Memory |
| `rework_ticket.md` | Verify Agent | Dev Agent |

### spec.md (executable intent)

```markdown
# Spec: User Registration Endpoint

## Objective
Allow user registration with email and password.

## Functional Requirements
- FR-001: POST /users accepts {email, password}
- FR-002: Validate email format (regex)
- FR-003: Password min 8 chars, 1 upper, 1 digit
- FR-004: Duplicate email → 409 Conflict

## Non-Functional Requirements
- NFR-001: Response time < 200ms (p99)
- NFR-002: Do not log passwords
- NFR-003: Rate limit 10 req/min per IP
```

### plan.md (task breakdown)

```markdown
# Plan: User Registration Endpoint

## Tasks
1. [ ] Create User model with Email and PasswordHash
2. [ ] Implement POST /users
...
```

### rework_ticket.md (feedback)

```markdown
# Rework Ticket — Cycle 1/3

## Failed Items
- [ ] FR-004: Duplicate email returns 500 instead of 409
...
```

---

## 5. Model routing

**You do not need separate agents — you need separate models for separate tasks.**

| Task | Recommended model | Relative cost |
|------|-------------------|---------------|
| Complex reasoning, architecture, design | Sonnet / Opus / Qwen 3.5 Pro | $$$ |
| Feature coding | Sonnet / DeepSeek | $$ |
| Verification, LLM-as-Judge | Sonnet / DeepSeek Pro | $$ |
| Context read, code exploration | Haiku / Flash / GPT-4o-mini | $ |
| Documentation | Haiku / Flash | $ |
| Simple debug | Haiku / Flash | $ |
| DB persistence | No LLM (direct SQL) | $0 |
| Pruning, TTL, batch ops | No LLM (cron + SQL) | $0 |

### Delegation to the host (IDE)

**FlowForge does not ship a proprietary "Model Router MCP server."**

After operational risk analysis (account blocks, IDE TOS, third-party key proxies), architecture **delegates model routing to the host IDE**:

- **OpenCode**: `opencode.json` / bundle under `~/.config/opencode/flowforge/`
- **Antigravity / VS Code**: global model + skills for role-play
- **Copilot / Cursor**: native model picker per phase

Model routing is a **recommended practice**, not a FlowForge software component.

---

## 6. AI orchestrator (hybrid escalation manager)

The default flow **does not need a continuous AI orchestrator**. Artifacts (`spec.md`, `rework_ticket.md`, etc.) are the deterministic routing protocol.

Teams may enable an **optional AI orchestrator** as an **escalation manager**, not a per-step router (which causes severe token bleed). See [06-ai-orchestrator.md](06-ai-orchestrator.md) and [11-orchestrator-delegation-protocol.md](11-orchestrator-delegation-protocol.md).

**v0.4 rule:** On bugs or verify failures, the orchestrator writes or forwards `rework_ticket.md` and delegates **forge-dev** — it does not patch `src/`, tests, or dashboards inline.

### When it intervenes

The workflow runner reads `rework_ticket.md` frontmatter (`cycle_count`). When `cycle_count >= max_retry_cycles` (default 3), the inner loop pauses and CKP-3 escalates to a human (optional orchestrator may analyze `spec.md`, `plan.md`, and the ticket first).

---

## 7. Shared infrastructure

```
┌──────────────────────────────────────────────────────────────┐
│                   FLOWFORGE INFRASTRUCTURE                   │
├──────────────────────────────────────────────────────────────┤
│  engram-dotnet — persistent memory (2 levels)                │
│    ├── Level 1: operational DB (SQLite/Postgres, TTL)        │
│    └── Level 2: versioned .md in repo (permanent)            │
│                                                              │
│  MCP tools — fetch-on-demand context                         │
│                                                              │
│  Workflow runner — bash/Makefile/GitHub Actions / IDE rules  │
│    ├── Five phases                                           │
│    ├── Human checkpoints                                     │
│    └── Retry and escalation                                  │
│                                                              │
│  Memory Janitor — cron + SQL (not an agent)                  │
└──────────────────────────────────────────────────────────────┘
```

---

## 8. Failure handling

| Scenario | Handler | How |
|----------|---------|-----|
| Provider timeout | MCP transport | Retry 3× + exponential backoff |
| Rate limit | MCP transport | Backoff + queue |
| Code does not compile | Dev loop | Fix and iterate |
| Tests fail | Dev loop | Ralph Wiggum |
| Spec not met | Verify Agent | `rework_ticket.md` + cycle count |
| 3+ rework cycles | CKP-3 | Human escalation / GitHub issue |
| Complex product decision | Human | CKP-1 or CKP-2 |
| Multi-repo coordination | Optional orchestrator | Routing + prioritization only |

---

## 9. References

- [02-memory-strategy.md](02-memory-strategy.md) — Two-level memory strategy
- [03-engram-dotnet-gaps.md](03-engram-dotnet-gaps.md) — engram-dotnet gaps
- [04-roadmap.md](04-roadmap.md) — Roadmap
- [05-comparison-methodologies.md](05-comparison-methodologies.md) — Methodology comparison
- [06-ai-orchestrator.md](06-ai-orchestrator.md) — Orchestrator and traffic lights
- [16-ide-integration-plan.md](16-ide-integration-plan.md) — IDE integration
- Agentsway (arXiv 2510.23664, 2025)
- Twelve-Factor Agentic SDLC (tikalk/agentic-sdlc-12-factors, 2025)
- ADLC — Arthur.ai (2026)
- TACO Framework — KPMG (2025)
- MCP Agentic SDLC — michaelwybraniec/mcp-agentic-sdlc (2025)
