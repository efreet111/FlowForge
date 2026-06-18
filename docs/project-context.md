# FlowForge — Project context

> **Last updated**: 2026-05-27  
> **Methodology version**: [`0.4.0`](../VERSION.md)

---

## Current state

FlowForge is at **v0.4.0 — IDE parity and public documentation**. The methodology defines **5 checkpoints (CKP-0→4)**, **7 agents**, and **31 skills** (7 core + 23 specialized + 1 teacher). The repo contains `docs/`, `skills/`, `ide/` install packs, and [`examples/crud-tareas/`](../examples/crud-tareas/) from a completed CRUD run.

Companion project: **[engram-dotnet](https://github.com/efreet111/engram-dotnet)** — persistent memory engine (.NET 10, MCP tools).

---

## Purpose

**FlowForge** is an Agentic SDLC methodology for SMB teams (2–20 people) integrating AI agents into delivery with human gates at the right moments.

### Design pillars (v0.4)

1. **5 phases, 5 checkpoints, 7 agents**
2. **31 skills** — security, SOLID, performance, migrations, metrics, etc.
3. **Two-level memory** — operational DB (TTL) + structured `.md` in repo
4. **Model routing per task** — host IDE chooses models; FlowForge does not ship a proprietary router
5. **Traffic-light checkpoints** — 🔴 binary (CKP-0, CKP-3), 🟡 human (CKP-1, CKP-2), 🟢 deploy (CKP-4)
6. **IDE-agnostic packs** — Cursor, OpenCode, Antigravity, VS Code via `ide/install`
7. **Optional teacher mode** — `forge-teacher` when `teacher_mode: true` in config

---

## Dependencies

| Dependency | Version | Status |
|------------|---------|--------|
| engram-dotnet | main | ✅ 7 archived SDD features, 258+ tests |
| .NET SDK | 10.0 | Required to build engram-dotnet |
| Python 3 | 3.x | Optional — recompile Cursor agents |

---

## Key documentation

| Document | Content |
|----------|---------|
| [`01-engramflow-architecture.md`](01-engramflow-architecture.md) | Architecture (FlowForge v0.4) |
| [`02-memory-strategy.md`](02-memory-strategy.md) | Two-level memory |
| [`04-roadmap.md`](04-roadmap.md) | Roadmap and backlog |
| [`14-flowforge-complete-reference.md`](14-flowforge-complete-reference.md) | Reference + 7 test cases |
| [`15-agent-skills-technical-spec.md`](15-agent-skills-technical-spec.md) | Agent/skill function catalog |
| [`16-ide-integration-plan.md`](16-ide-integration-plan.md) | IDE integration |
| [`ide/README.md`](../ide/README.md) | Install and parity |

---

## Skills per agent

| Agent | Core | Specialized | Total |
|-------|------|-------------|-------|
| forge-orchestrator | 1 | — | 1 |
| forge-discovery | 1 | security, compliance, cost | 4 |
| forge-arch | 1 | security, performance, a11y, domain | 5 |
| forge-plan | 1 | security, patterns, migrations, rollback | 5 |
| forge-dev | 1 | security, solid, testing, performance, refactor | 6 |
| forge-verify | 1 | security, complexity, performance, a11y | 5 |
| forge-memory | 1 | metrics, changelog, knowledge | 4 |
| forge-teacher (cross-cutting) | — | — | 1 |
| **Total** | **7** | **23** | **31** |

---

## Artifact layout

```
.ai-work/{feature-slug}/
  context-map.md
  spec.md          # includes PM-* manual tests
  plan.md          # checklist — marked by forge-dev
  verify-report.md
  rework_ticket.md # when verify fails or human reports bug
  summary.md       # after /flow-close when PM-* done
```
