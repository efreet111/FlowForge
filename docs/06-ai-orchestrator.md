# The Orchestrator: Traffic Light and Director of FlowForge

> **Version**: 1.2 (CKP-0 → CKP-4 normalized)
> **Role**: Primary traffic light and state director

The **Orchestrator** is the **core engine** of FlowForge (formerly framed only as an optional escalation handler). It reads project state, decides which agent runs, handles phase transitions, and **stops to ask the human** when requirements are ambiguous.

## 1. The orchestrator is always an AI agent

The orchestrator is **not** a rigid shell script. It is an **AI agent** driven by compiled IDE rules (`workflow.mdc`, `workflow-orchestrator-parity.md`) because it must judge **business viability** before burning tokens on implementation.

> **Commands vs agents:** You type **`/flow-*`** (phase commands). The orchestrator delegates to **`forge-*`** agents internally. There is no official `/forge-memory` or `/forge-dev` slash command — use `/flow-close`, `/flow-dev`, etc. See [`14-flowforge-complete-reference.md`](14-flowforge-complete-reference.md#commands-vs-agents).

- **Intelligent traffic light**: If you ask for “add telemetry” without a provider, a blind runner would proceed. The orchestrator stops on **red** and asks which provider to use.
- **IDE-native, not script-heavy**: FlowForge installs via rules/agents in your IDE (`.cursor/`, `.agents/`, `.github/agents/`, OpenCode config). No external runner is required for the methodology itself.

## 2. State machine (CKP-0 → CKP-4)

| Phase | Agent | CKP | Color | Type | Action |
|-------|--------|-----|-------|------|--------|
| 0 Discovery | `forge-discovery` | **CKP-0** | 🔴 | HARD STOP | Vague requirement → stop and clarify |
| 1 Intent | `forge-arch` | **CKP-1** | 🟡 | YELLOW | “spec.md ready. Approve or adjust?” |
| 2 Plan | `forge-plan` | **CKP-2** | 🟡 | YELLOW | “plan.md ready. Green light to code?” |
| 3 Execution | `forge-dev` ↔ `forge-verify` | **CKP-3** | 🔴 | EMERGENCY | 3 rework cycles → escalate |
| 4 Close | `forge-memory` | **CKP-4** | 🟢 | DEPLOY | “Deploy / merge?” |

### Per phase

1. **Discovery (CKP-0)** — Search memory / context. **Hard stop** if vague or missing context.
2. **Intent (CKP-1)** — Write `spec.md`. Pause for human approval.
3. **Plan (CKP-2)** — Write `plan.md`. Pause for human approval.
4. **Execution** — Dev ↔ verify inner loop. **CKP-3** if `rework_ticket.md` `cycle_count >= 3`.
5. **Close (CKP-4)** — Memory after verify PASS; human deploy decision. **PM-*** in `spec.md` must be `[x]` before real close.

## 3. Human-in-the-loop: stop types

### 🔴 Hard stops (CKP-0, CKP-3)

- **CKP-0**: business ambiguity or infeasibility — do not advance.
- **CKP-3**: three failed rework cycles — escalate immediately; no fourth automatic attempt.

### 🟡 Yellow gates (CKP-1, CKP-2)

Human may approve with open items; **verify** enforces `deterministic` items from the Capability Matrix in CKP-3.

### 🟢 Deploy gate (CKP-4)

Not a brake — a release decision after memory synthesis.

## 4. Orchestrator must not implement product code

- Delegate `spec.md`, `plan.md`, code, and `verify-report.md` to subagents.
- On bug reports: create `rework_ticket.md` → delegate **forge-dev**.
- See [`ide/shared/workflow-orchestrator-parity.md`](../ide/shared/workflow-orchestrator-parity.md) and [`11-orchestrator-delegation-protocol.md`](11-orchestrator-delegation-protocol.md).
