# Copilot Instructions — FlowForge

This project follows the **FlowForge Agentic SDLC methodology** with multi-agent delegation via custom agents in `.github/agents/`.

## Agents

Select `FlowForge Orchestrator` from the agent picker to start.

| Agent | Phase | Role |
|-------|-------|------|
| `forge-orchestrator` | All | Coordinates the 5-phase flow |
| `forge-discovery` | 0 | Context, CVEs, compliance, cost |
| `forge-arch` | 1 | spec.md + manual tests (PM-*) |
| `forge-plan` | 2 | plan.md + task checklist |
| `forge-dev` | 3a | Code + tests (Ralph Wiggum) |
| `forge-verify` | 3b | Audit → PASS or rework |
| `forge-memory` | 4 | Session summary, manual test gate |

## Checkpoints

| CKP | Color | What |
|-----|-------|------|
| CKP-0 | 🔴 | Vague → STOP |
| CKP-1 | 🟡 | Human approves spec |
| CKP-2 | 🟡 | Human approves plan |
| CKP-3 | 🔴 | 3 rework cycles → escalate |
| CKP-4 | 🟢 | Deploy decision |

## Manual Tests (PM-*)

All features require manual developer tests before closure. forge-arch generates them. forge-memory blocks closure if incomplete. See `spec.md` §4.

## Security / Quality / Performance

Same rules as before: OWASP Top 10, SOLID, N+1 detection, parameterized queries.
