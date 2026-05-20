# Tasks: Optional AI Orchestrator

## Phase 1: Foundation (Architecture Base)

- [x] 1.1 Modify `docs/01-engramflow-architecture.md` (Section 6) to replace the current "Orquestador Opcional" text con el new "Hybrid Escalation Manager" concept. Include the Data Flow diagram showing interception of `rework_ticket.md`.

## Phase 2: Core Implementation (Detailed Specification)

- [x] 2.1 Create `docs/06-ai-orchestrator.md` outlining the deep-dive behavior of the Orchestrator, its inputs (`spec.md`, `plan.md`, `rework_ticket.md`), and its expected output resolutions (Replan vs Checkpoint).
- [x] 2.2 Document the `.engram.json` JSON schema for the orchestrator block inside `docs/06-ai-orchestrator.md`, detailing flags like `enabled` and `escalation_triggers`.
- [x] 2.3 Draft the baseline System Prompt for the AI Orchestrator Agent inside `docs/06-ai-orchestrator.md` (instructing it to act as an escalation resolver, not a code writer).
- [x] 2.4 Document the new mandatory YAML Frontmatter structure for `rework_ticket.md` (e.g., `cycle_count: 3`, `max_cycles: 3`) to ensure reliable parsing by the Workflow Runner.

## Phase 3: Cleanup and Alignment

- [x] 3.1 Modify `docs/04-roadmap.md` to update the status of the "Orquestador AI Opcional" feature from "Solo concepto" to "Completado/Documentado".
