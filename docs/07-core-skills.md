# FlowForge — Core skills index

> **Version**: 0.4.0  
> **Last updated**: 2026-05-27

This document is an **index** to the seven core agents. Executable instructions live in English in `skills/forge-*/SKILL.md` (compiled to `ide/cursor/agents/` when needed).

---

## Data contracts (7 agents)

| Agent | Phase | Inputs | Outputs | Specialized skills |
|--------|-------|--------|---------|-------------------|
| **Orchestrator** | All | Prompt, config | Delegation + CKPs | 1 core |
| **Discovery** | 0 | Prompt, memory | `context-map.md` | security, compliance, cost |
| **Arch** | 1 | Context map | `spec.md` + capability matrix | security, performance, a11y, domain |
| **Plan** | 2 | `spec.md` | `plan.md` checklist | security, patterns, migrations, rollback |
| **Dev** | 3 | `plan.md`, `spec.md` | Code + tests | security, solid, testing, performance, refactor |
| **Verify** | 3 | `spec.md`, code | `verify-report.md` / `rework_ticket.md` | security, complexity, performance, a11y |
| **Memory** | 4 | Artifacts | `summary.md`, ADRs, engrams | metrics, changelog, knowledge |

## Checkpoints (CKP-0 → CKP-4)

| CKP | Color | Type | Trigger |
|-----|-------|------|---------|
| CKP-0 | 🔴 | Hard stop | Vague requirement |
| CKP-1 | 🟡 | Human | `spec.md` ready |
| CKP-2 | 🟡 | Human | `plan.md` ready |
| CKP-3 | 🔴 | Mechanical (3 reworks) | `cycle_count >= 3` |
| CKP-4 | 🟢 | Deploy gate | Memory done, PM-* complete |

---

## Where to read more

| Need | Document |
|------|----------|
| Full walkthrough + 7 test cases | [`14-flowforge-complete-reference.md`](14-flowforge-complete-reference.md) |
| Function catalog (conceptual) | [`15-agent-skills-technical-spec.md`](15-agent-skills-technical-spec.md) |
| Architecture | [`01-engramflow-architecture.md`](01-engramflow-architecture.md) |
| Orchestrator behavior | [`06-ai-orchestrator.md`](06-ai-orchestrator.md) + [`skills/forge-orchestrator/SKILL.md`](../skills/forge-orchestrator/SKILL.md) |

---

## Legacy note

Older versions of this file contained v0.1 prompts for five agents. **Do not use them.** Always load `skills/*/SKILL.md` or compiled IDE agents.
