# FlowForge — Project Context

> Last updated: 2026-09-08
> Methodology version: [0.5.0](../VERSION.md)

---

## Business Goal

FlowForge is an **Agentic SDLC** (Software Development Lifecycle with AI Agents) methodology for small and mid-size teams (2–20 people). It solves the problem of **integrating AI agents into the development cycle without enterprise bureaucracy** — with 5 formal checkpoints, 7 specialized agents, and versioned artifacts.

**Target audience**: Teams using AI coding agents (Cursor, OpenCode, Gemini CLI, etc.) who want:
- 5 human checkpoints (CKP-0 → CKP-4) to control ambiguity
- 7 specialized agents (not 10+, less overhead)
- On-demand context (agents fetch via MCP, not everything in the prompt)
- Model routing per task (Sonnet for reasoning, Haiku for read/write)

---

## Tech Stack

| Layer | Technology |
|-------|------------|
| **Methodology** | Agentic SDLC with 5 phases + 5 checkpoints (CKP-0 → CKP-4) |
| **Agents** | 7 roles: Discovery, Architect, Plan, Dev, Verify, Memory, Orchestrator |
| **Skills** | 31 specialized skills (`skills/forge-*/SKILL.md`) |
| **Memory** | Engram (engram-dotnet v1.1.0+) with offline-first sync |
| **Server** | .NET 10 C#, PostgreSQL, Docker on TrueNAS SCALE |
| **IDEs** | OpenCode, Cursor, Antigravity (Gemini CLI), VS Code |
| **Artifacts** | `.ai-work/{feature-slug}/` with `spec.md`, `plan.md`, `verify-report.md`, `rework_ticket.md` |

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────┐
│                         FLOWFORGE                                    │
│  5 phases | 5 checkpoints (CKP-0→4) | 7 agents | Model routing      │
└─────────────────────────────────────────────────────────────────────┘

PHASE 0: DISCOVERY ─────────── CKP-0 🔴 HARD STOP ───────────────────
│  Cross-memory association, epic mapping, user-story validation
│  Agent: Discovery Agent
│  Deliverable: Memory association map
│  ⚠️ Binary, no appeal: vague requirements → STOP EVERYTHING

PHASE 1: INTENT ────────────── CKP-1 🟡 YELLOW LIGHT ────────────────
│  Priority review and business-intent validation
│  Agent: Architect Agent → spec.md + Capability Matrix
│  🟡 Human decides: "Approve or adjust?"

PHASE 2: ARCHITECTURE ──────── CKP-2 🟡 YELLOW LIGHT ────────────────
│  Validate and approve the implementation plan
│  Agent: Plan Agent → plan.md + task breakdown
│  🟡 Human decides: "Green light to code?"

PHASE 3: EXECUTION ─────────── Inner loop (autonomous) ──────────────
│  Dev Agent → code + unit tests + Ralph Wiggum loop
│  Verify Agent → traceability vs spec.md + LLM-as-Judge
│  CKP-3 🔴: max 3 rework cycles → escalate to human

PHASE 4: CLOSE ─────────────── CKP-4 🟢 DEPLOY GATE ─────────────────
│  Memory Agent → session summary, ADR promotion
│  🟢 Human decides: "Deploy / merge?"
```

---

## Key Decisions

| Decision | ADR | Date | Rationale |
|----------|-----|------|-----------|
| 7 agents (not 10+) | — | 2026-05 | Each handoff loses context. 10 agents = 10 prompts, 10 configs, unbearable overhead for SMBs. |
| 5 checkpoints (CKP-0→4) | — | 2026-05 | From ~8 human interruptions in traditional SDLC to 5: CKP-0 (binary), CKP-1 (spec), CKP-2 (plan), CKP-3 (mechanical, 3 cycles), CKP-4 (deploy). |
| On-demand context | — | 2026-05 | Agents don't receive all context in the prompt — fetch via MCP when needed. Fewer tokens, less noise. |
| Model routing per task | — | 2026-05 | Don't use one model for everything. Complex reasoning → Sonnet/Opus. Read/write → Haiku. Persistence → $0 (direct SQL). |
| Versioned artifacts as protocol | — | 2026-05 | `spec.md`, `plan.md`, `verify-report.md`, `rework_ticket.md` are the contract between agents. Orchestrator is optional — artifacts manage the flow. |
| Offline-first sync | — | 2026-05 | Teams need to share memories without constant network dependency. Local SQLite + PostgreSQL server. |
| Canonical context file in `docs/` | [ADR-004](../decisions/ADR-004-flowdoc-integration.md) | 2026-09 | New team members lose 2-3 days on onboarding without centralized context. Aligned with FlowDoc integration. |

---

## Team & Roles

| Role | Type | Responsibility |
|------|------|-----------------|
| **Orchestrator** | AI (`forge-orchestrator`) | Coordinates phases, decides when to stop and ask the human. **Does not implement product code**. |
| **Discovery** | AI (`forge-discovery`) | Searches context, maps requirements, associates cross-memories. CKP-0. |
| **Architect** | AI (`forge-arch`) | Writes `spec.md` with Capability Matrix (FR/NFR). CKP-1. |
| **Planner** | AI (`forge-plan`) | Breaks down into tasks, estimates effort, MCP contracts. CKP-2. |
| **Developer** | AI (`forge-dev`) | Implements code, follows `plan.md`, unit tests, Ralph Wiggum loop. |
| **Verifier** | AI (`forge-verify`) | LLM-as-Judge, verifies against `spec.md`, traces FR/NFR, generates `verify-report.md` or `rework_ticket.md`. |
| **Memory Agent** | AI (`forge-memory`) | Synthesizes session, saves to Engram, promotes ADRs, close CKP-4. |
| **Human** | Human | Approves CKP-1 (spec), CKP-2 (plan), CKP-4 (deploy). **Does not approve code line by line**. |

---

## Related Projects

| Project | Relationship |
|---------|-------------|
| **[engram-dotnet](https://github.com/efreet111/engram-dotnet)** | Persistent memory backend (.NET 10). FlowForge uses Engram for cross-session memory and multi-user sync. |
| **FlowForge** (this repo) | **Main project** — Agentic SDLC methodology + IDE packs + 31 skills. |
| **Cursor / OpenCode / Antigravity** | IDEs where FlowForge agents are installed via `install.sh` / `install.ps1`. |

---

## Getting Started

### 1. Clone FlowForge
```bash
git clone https://github.com/efreet111/FlowForge.git
cd FlowForge
```

### 2. Install engram-dotnet (memory server)
```bash
git clone https://github.com/efreet111/engram-dotnet.git
cd engram-dotnet
./scripts/setup.sh  # Linux/macOS
# or: .\scripts\setup.ps1  # Windows
```

### 3. Configure MCP in your IDE
**OpenCode**: `~/.config/opencode/opencode.json`
**Cursor**: `~/.cursor/mcp.json`

```json
{
  "mcpServers": {
    "engram": {
      "command": "{{ENGRAM_BINARY_PATH}}",
      "args": ["mcp"],
      "env": {
        "ENGRAM_DATA_DIR": "{{ENGRAM_DATA_DIR}}",
        "ENGRAM_USER": "your@email.com",
        "ENGRAM_SYNC_ENABLED": "true",
        "ENGRAM_SERVER_URL": "{{ENGRAM_SERVER_URL}}"
      }
    }
  }
}
```

> **Note — compiled binary vs `dotnet run`:** Use the compiled binary
> (e.g. `dist/engram.exe` on Windows, `dist/engram` on Linux) instead of
> `dotnet run --no-build`. `dotnet run --no-build` can hang if the binary
> is outdated or the build was interrupted, leaving MCP unstable for the
> entire session. Without ENGRAM_SERVER_URL → local-first mode (writes to
> SQLite before sync). With ENGRAM_SYNC_ENABLED + ENGRAM_SERVER_URL → push
> to server when healthy.

### 4. Reload IDE
- OpenCode: close and reopen
- Cursor: `Developer: Reload Window`

### 5. First flow
```
/flow-start Task CRUD — create, list, update, delete tasks with title, description, status, timestamps
```

The orchestrator will:
1. **CKP-0**: Discovery (search context, validate requirements) → **HARD STOP if vague**
2. **CKP-1**: Architect writes `spec.md` → **Human approves or adjusts**
3. **CKP-2**: Plan writes `plan.md` → **Human gives green light**
4. **CKP-3**: Dev ↔ Verify inner loop → **Max 3 rework cycles, then escalates**
5. **CKP-4**: Memory closes session → **Human decides deploy**

> Last updated: 2026-09-08
> Methodology version: [0.5.0](../VERSION.md)
