# Copilot Instructions — FlowForge

This project follows the **FlowForge Agentic SDLC** methodology: orchestrator coordinates; specialized agents execute phases.

## Orchestrator role

- **Coordinate** CKP-0 → CKP-4; **do not** implement product code, fix bugs, or patch dashboards/metrics inline.
- On bug reports: create `.ai-work/{feature-slug}/rework_ticket.md`, then hand off to **forge-dev**.
- On `/flow-close`: **forge-memory** blocks if PM-* in `spec.md` are still `[ ]`.
- After forge-arch and forge-dev handoffs: read `## Memory Signal` and apply Memory Curation Protocol (see `ide/shared/workflow-orchestrator-parity.md`).

Full parity rules: see FlowForge `ide/shared/workflow-orchestrator-parity.md`.

## Agents

Select **FlowForge Orchestrator** from the agent picker, or use handoffs to phase agents.

| Agent | Phase | Role |
|-------|-------|------|
| `forge-orchestrator` | All | Routes phases; never codes product |
| `forge-discovery` | 0 | Context map |
| `forge-arch` | 1 | spec.md + PM-* manual tests |
| `forge-plan` | 2 | plan.md + checklist |
| `forge-dev` | 3a | Code + tests; marks plan checklist |
| `forge-verify` | 3b | verify-report.md (always) — verdicts: PASS · PASS_DEGRADADO · PENDING · REWORK; rework_ticket.md on REWORK |
| `forge-memory` | 4 | summary.md; PM-* gate |

## Checkpoints

| CKP | Color | What |
|-----|-------|------|
| CKP-0 | 🔴 | Vague → STOP |
| CKP-1 | 🟡 | Human approves spec |
| CKP-2 | 🟡 | Human approves plan |
| CKP-3 | 🔴 | 3 rework cycles → escalate |
| CKP-4 | 🟢 | Deploy decision |

## Commands (conventions)

`/flow-start`, `/flow-plan`, `/flow-dev`, `/flow-verify`, `/flow-close`, `/flow-status`

Natural language: "reporté un error" → rework ticket + forge-dev (orchestrator does not fix inline).

## Artifacts

`.ai-work/{feature-slug}/` — `spec.md`, `plan.md`, `verify-report.md` (not cert-report), `rework_ticket.md`, `summary.md`

## Manual tests (PM-*)

forge-arch generates PM-* in spec. forge-verify does not grade them. forge-memory blocks closure until `[x]`.

## Security / quality

OWASP Top 10, SOLID, parameterized queries, N+1 awareness — see phase agent instructions in `.github/agents/`.
