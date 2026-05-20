# FlowForge — Agent Skills Index

When working on this project, load the relevant skill(s) BEFORE writing any code.

## How to Use

1. Check the trigger column to find skills that match your current task.
2. Load the skill by reading the `SKILL.md` file at the listed path.
3. Follow ALL patterns and rules from the loaded skill.
4. Multiple skills can apply simultaneously.

## Core FlowForge Skills

| Skill | Trigger / Context | Path |
|-------|-------------------|------|
| `forge-orchestrator` | When starting a new session, analyzing the workflow state, or managing state transitions. | [`skills/forge-orchestrator/SKILL.md`](skills/forge-orchestrator/SKILL.md) |
| `forge-discovery` | Fase 0: When starting a new epic, exploring previous memories, or mapping requirements. | [`skills/forge-discovery/SKILL.md`](skills/forge-discovery/SKILL.md) |
| `forge-arch` | Fase 1: When designing the spec.md, writing Given-When-Then scenarios, or defining capabilities. | [`skills/forge-arch/SKILL.md`](skills/forge-arch/SKILL.md) |
| `forge-plan` | Fase 2: When writing plan.md, proposing technical changes, or creating a checklist of tasks. | [`skills/forge-plan/SKILL.md`](skills/forge-plan/SKILL.md) |
| `forge-dev` | Fase 3: When writing product code, fixing syntax errors, or running unit tests. | [`skills/forge-dev/SKILL.md`](skills/forge-dev/SKILL.md) |
| `forge-verify` | Fase 3.5: When validating implementation against specifications, running tests, or writing rework tickets. | [`skills/forge-verify/SKILL.md`](skills/forge-verify/SKILL.md) |
| `forge-memory` | Fase 4: When concluding a session, documenting learnings, or saving engrams to database. | [`skills/forge-memory/SKILL.md`](skills/forge-memory/SKILL.md) |

---

*This file acts as a public contract for IDE-native AI agents (Cursor Composer, Cline, Antigravity, OpenCode) to adhere strictly to the FlowForge methodology.*
