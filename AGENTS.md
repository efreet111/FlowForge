# FlowForge — Agent Skills Index

When working on this project, load the relevant skill(s) BEFORE writing any code.

> **🔧 engram-dotnet Reference**: For the complete catalog of 25 MCP tools across 6 capability areas (Core Memory, Sync, Verification, ADR Promotion, Obsidian Export, Diagnostics), see [`docs/12-engram-tool-reference.md`](docs/12-engram-tool-reference.md). Every agent should know which tools are available before starting any phase.

## How to Use

1. Check the trigger column to find skills that match your current task.
2. Load the skill by reading the `SKILL.md` file at the listed path.
3. Follow ALL patterns and rules from the loaded skill.
4. Multiple skills can apply simultaneously.

---

## 🔴🟡🟢 Checkpoint System (CKP-0 → CKP-4)

The orchestrator enforces 5 control points. Learn the difference:

| CKP | Color | Type | Severity | Behavior |
|-----|-------|------|----------|----------|
| **CKP-0** | 🔴 | HARD STOP | Binary, no appeal | Vague requirements → STOP. Ask human to clarify. |
| **CKP-1** | 🟡 | Semáforo Amarillo | Human decides | spec.md complete → *"Approve or adjust?"* |
| **CKP-2** | 🟡 | Semáforo Amarillo | Human decides | plan.md complete → *"Green light to code?"* |
| **CKP-3** | 🔴 | Freno de Emergencia | Mechanical (3 cycles) | Rework count = 3 → ESCALATE to human |
| **CKP-4** | 🟢 | Deploy Gate | Human decides | Memory Agent done → *"Deploy?"* |

**Red = STOP, no negotiation. Yellow = CONSULT, human decides. Green = INFORM, human decides.**

---

## Core FlowForge Skills (v0.3)

| Skill | Fase | CKP | Trigger / Context | Path |
|-------|------|-----|-------------------|------|
| `forge-orchestrator` | All | CKP‑0→4 | When starting a new session, analyzing the workflow state, or managing state transitions. | [`skills/forge-orchestrator/SKILL.md`](skills/forge-orchestrator/SKILL.md) |
| `forge-discovery` | 0 | CKP‑0 🔴 | When starting a new epic, exploring memories (DB or local grep fallback), or mapping requirements. | [`skills/forge-discovery/SKILL.md`](skills/forge-discovery/SKILL.md) |
| `forge-arch` | 1 | CKP‑1 🟡 | When designing the spec.md, writing Given-When-Then scenarios, or defining capabilities. | [`skills/forge-arch/SKILL.md`](skills/forge-arch/SKILL.md) |
| `forge-plan` | 2 | CKP‑2 🟡 | When writing plan.md, proposing technical changes, or creating a checklist of tasks. | [`skills/forge-plan/SKILL.md`](skills/forge-plan/SKILL.md) |
| `forge-dev` | 3 | Inner Loop | When writing product code, fixing syntax errors, or running unit tests. | [`skills/forge-dev/SKILL.md`](skills/forge-dev/SKILL.md) |
| `forge-verify` | 3 | CKP‑3 🔴 | Auditing implementation (LLM-as-Judge & traceability) or generating rework tickets. | [`skills/forge-verify/SKILL.md`](skills/forge-verify/SKILL.md) |
| `forge-memory` | 4 | CKP‑4 🟢 | Session closure (smart synthesis, database upload, ADR promotion, retention cleanup). | [`skills/forge-memory/SKILL.md`](skills/forge-memory/SKILL.md) |

---

## Specialized Skills (OLA 1 → OLA 4)

Load these on-demand when the context demands domain expertise. The core skills above remain the foundation.

### 🔥 OLA 1 — Security & SOLID (P0)

| Skill | Role | Trigger | Path |
|-------|------|---------|------|
| `forge-arch-security` | Arch (Fase 1) | Feature touches auth, data, or external APIs | `skills/forge-arch/security/SKILL.md` |
| `forge-plan-security` | Plan (Fase 2) | Any plan — secure-by-design, OWASP ASVS | `skills/forge-plan/security/SKILL.md` |
| `forge-dev-security` | Dev (Fase 3) | Code handles user input, forms, queries | `skills/forge-dev/security/SKILL.md` |
| `forge-verify-security` | Verify (Fase 3) | Always — SAST, OWASP checklist, dependency audit | `skills/forge-verify/security/SKILL.md` |
| `forge-dev-solid` | Dev (Fase 3) | All production code — SOLID post-coding validation | `skills/forge-dev/solid/SKILL.md` |

### 🔜 OLA 2 — Code Quality & Patterns (P1)

| Skill | Role | Trigger | Path |
|-------|------|---------|------|
| `forge-plan-patterns` | Plan (Fase 2) | Structural decisions needed | `skills/forge-plan/patterns/SKILL.md` |
| `forge-dev-testing` | Dev (Fase 3) | Complex business logic | `skills/forge-dev/testing/SKILL.md` |
| `forge-dev-performance` | Dev (Fase 3) | DB or API-heavy features | `skills/forge-dev/performance/SKILL.md` |
| `forge-verify-complexity` | Verify (Fase 3) | Dense conditional logic | `skills/forge-verify/complexity/SKILL.md` |
| `forge-verify-performance` | Verify (Fase 3) | Performance RNF in spec | `skills/forge-verify/performance/SKILL.md` |

### 🔷 OLA 3 — Infrastructure & Domain (P2)

| Skill | Role | Trigger | Path |
|-------|------|---------|------|
| `forge-discovery-security` | Discovery (Fase 0) | Feature touches auth or sensitive data | `skills/forge-discovery/security/SKILL.md` |
| `forge-discovery-compliance` | Discovery (Fase 0) | Compliance requirements (GDPR, SOC2) | `skills/forge-discovery/compliance/SKILL.md` |
| `forge-arch-performance` | Arch (Fase 1) | Performance-critical features | `skills/forge-arch/performance/SKILL.md` |
| `forge-arch-a11y` | Arch (Fase 1) | UI/UX features | `skills/forge-arch/a11y/SKILL.md` |
| `forge-arch-domain` | Arch (Fase 1) | Multiple bounded contexts | `skills/forge-arch/domain/SKILL.md` |
| `forge-plan-migrations` | Plan (Fase 2) | New or modified DB schemas | `skills/forge-plan/migrations/SKILL.md` |
| `forge-plan-rollback` | Plan (Fase 2) | Features modifying contracts or APIs | `skills/forge-plan/rollback/SKILL.md` |
| `forge-dev-refactor` | Dev (Fase 3) | During Ralph Wiggum loop | `skills/forge-dev/refactor/SKILL.md` |

### 🔹 OLA 4 — Metrics & Knowledge (P3, Post-MVP)

| Skill | Role | Trigger | Path |
|-------|------|---------|------|
| `forge-discovery-cost` | Discovery (Fase 0) | Features touching DB or adding services | `skills/forge-discovery/cost/SKILL.md` |
| `forge-verify-a11y` | Verify (Fase 3) | UI features | `skills/forge-verify/a11y/SKILL.md` |
| `forge-memory-metrics` | Memory (Fase 4) | Feature closure | `skills/forge-memory/metrics/SKILL.md` |
| `forge-memory-changelog` | Memory (Fase 4) | Pre-release | `skills/forge-memory/changelog/SKILL.md` |
| `forge-memory-knowledge` | Memory (Fase 4) | Multi-repo projects | `skills/forge-memory/knowledge/SKILL.md` |

---

*This file acts as a public contract for IDE-native AI agents (Cursor Composer, Cline, Antigravity, OpenCode) to adhere strictly to the FlowForge methodology.*
