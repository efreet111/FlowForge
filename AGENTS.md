# FlowForge — Agent Skills Index

When working on this project, load the relevant skill(s) BEFORE writing any code.

> **📚 FlowDoc v2.0 Adopter**: This project follows [FlowDoc v2.0](https://github.com/crhistianmdz/FlowDocs) as its documentation layer. See [`docs/20-flowdoc-ecosystem.md`](docs/20-flowdoc-ecosystem.md) for the canonical adopter guide and [`docs/decisions/ADR-004-flowdoc-integration.md`](docs/decisions/ADR-004-flowdoc-integration.md) for the integration decision. FlowDoc is pinned in `.flowforge.json` via `docs_framework: "flowdoc"` and `docs_framework_version: "2.0"`.

> **🔧 engram-dotnet Reference**: For the complete catalog of 25 MCP tools across 6 capability areas (Core Memory, Sync, Verification, ADR Promotion, Obsidian Export, Diagnostics), see [`docs/12-engram-tool-reference.md`](docs/12-engram-tool-reference.md). Every agent should know which tools are available before starting any phase.

## How to Use

1. Check the trigger column to find skills that match your current task.
2. Load the skill by reading the `SKILL.md` file at the listed path.
3. Follow ALL patterns and rules from the loaded skill.
4. Multiple skills can apply simultaneously.

> **🖥️ Cursor IDE note**: In Cursor, agents are **pre-compiled** — their instructions are already embedded in the agent `.md` files under `ide/cursor/agents/`. You do NOT need to manually load `SKILL.md` files inside a Cursor agent context. Manual skill loading is only needed in IDEs that use `AGENTS.md` as the sole instruction source (OpenCode, Antigravity, VS Code Copilot). If `workflow.mdc` is active, defer to it; it overrides `AGENTS.md` for Cursor runtime behavior.

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

## Core FlowForge Skills (v0.4)

| Skill | Fase | CKP | Trigger / Context | Path |
|-------|------|-----|-------------------|------|
| `forge-orchestrator` | All | CKP‑0→4 | When starting a new session, analyzing the workflow state, or managing state transitions. | [`skills/forge-orchestrator/SKILL.md`](skills/forge-orchestrator/SKILL.md) |
| `forge-discovery` | 0 | CKP‑0 🔴 | When starting a new epic, exploring memories (DB or local grep fallback), or mapping requirements. | [`skills/forge-discovery/SKILL.md`](skills/forge-discovery/SKILL.md) |
| `forge-arch` | 1 | CKP‑1 🟡 | When designing the spec.md, writing Given-When-Then scenarios, or defining capabilities. | [`skills/forge-arch/SKILL.md`](skills/forge-arch/SKILL.md) |
| `forge-plan` | 2 | CKP‑2 🟡 | When writing plan.md, proposing technical changes, or creating a checklist of tasks. | [`skills/forge-plan/SKILL.md`](skills/forge-plan/SKILL.md) |
| `forge-dev` | 3 | Inner Loop | When writing product code, fixing syntax errors, or running unit tests. | [`skills/forge-dev/SKILL.md`](skills/forge-dev/SKILL.md) |
| `forge-verify` | 3 | CKP‑3 🔴 | Auditing implementation (LLM-as-Judge & traceability) or generating rework tickets. | [`skills/forge-verify/SKILL.md`](skills/forge-verify/SKILL.md) |
| `forge-memory` | 4 | CKP‑4 🟢 | Session closure (smart synthesis, database upload, ADR promotion, retention cleanup). | [`skills/forge-memory/SKILL.md`](skills/forge-memory/SKILL.md) |
| `forge-index-docs` | On-demand | — | Index FlowDoc docs (PRD, ADR, RFC, API, DB) into Engram. Trigger: "index docs", "indexar documentos", "backfill engram", "drift-check docs". | [`skills/forge-index-docs/SKILL.md`](skills/forge-index-docs/SKILL.md) |

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

## 📚 FlowDocs Skills (Documentation Layer)

> These 9 skills live in `~/.config/opencode/skills/` (user scope, NOT vendored in repo).
> Versions pinned in [`docs/20-flowdoc-ecosystem.md`](docs/20-flowdoc-ecosystem.md).
> **Two independent cycles**: `flowdoc-hu` (HUs, direct invocation) vs `flowdoc-assist` (base docs).
> Base context mapping contract: [`.ai-work/adopt-flowdocs-skills/base-context-mapping.md`](.ai-work/adopt-flowdocs-skills/base-context-mapping.md).

### Invocation protocol

| Trigger | Skill | Cycle | Output path |
|---------|-------|-------|-------------|
| Create/update HU | `flowdoc-hu` | Direct (forge-orchestrator → flowdoc-hu) | `docs/tasks/HU-001-HU-099/HU-NNN.md` |
| Create PRD | `flowdoc-assist` → `flowdoc-prd` | Delegated (forge-orchestrator → flowdoc-assist) | `docs/PRD.md` |
| Create product ADR | `flowdoc-assist` → `flowdoc-adr` | Delegated | `docs/architecture/adr/` |
| Create RFC | `flowdoc-assist` → `flowdoc-rfc` | Delegated | `docs/architecture/rfc/` |
| Document API | `flowdoc-assist` → `flowdoc-api` | Delegated | `docs/endpoints.md` |
| Document DB | `flowdoc-assist` → `flowdoc-db` | Delegated | `docs/schema.md` |
| Audit docs | `flowdoc-assist` → `flowdoc-review` | Delegated | Read-only report |
| Discover project | `flowdoc-assist` → `flowdoc-discover` | Delegated | Base context |

### Degradation (skill unavailable)

If a flowdoc skill is not installed or not responding:
1. Report the error with the skill name and expected path
2. Suggest remediation: install (`opencode skills install flowdoc-{name}`), skip, or standalone mode
3. NEVER fail silently

### Key rules

- `flowdoc-hu` is NEVER invoked via `flowdoc-assist` — it's a direct call from `forge-orchestrator`
- `flowdoc-assist` coordinates ONLY base docs (discover/prd/adr/rfc/api/db/review) — NOT HUs
- HU cycle is independent of base docs cycle; forge-orchestrator is the hub connecting both
- `.atl/skill-registry.md` is auto-generated — do NOT edit manually

---

## 🎯 Onboarding Detection (When to suggest `flowforge onboard`)

**Context**: The `flowforge onboard` command generates a first-day briefing from engram memories (decisions, patterns, blockers). AI agents should suggest it when appropriate.

### Detection Signals

Suggest `flowforge onboard` when you observe any of these signals:

| Signal | Example User Input | Action |
|--------|-------------------|--------|
| **New user** | "How do I get started?" / "What should I know?" | Suggest running `flowforge onboard` |
| **Long break** | User hasn't worked on this project in >7 days | Suggest `flowforge onboard` to refresh context |
| **Project switch** | User is switching to a different project in the same team | Suggest `flowforge onboard --project <name>` |
| **No ONBOARDING.md** | No `ONBOARDING.md` exists in the repo | Suggest generating one with `--output` |
| **Major feature** | User is about to start a large feature | Suggest reviewing decisions first |

### What to Say

**Template response**:
```
Before we start, you might want to run `flowforge onboard` to get a briefing
from the team's memories. It will show you:
- Recent activity (last sessions and observations)
- Key architectural decisions (type=decision)
- Team conventions and patterns (type=pattern)
- Known blockers and gotchas

Run: flowforge onboard --project <project-name>

Or export to ONBOARDING.md for the team:
flowforge onboard --project <project-name> --output ONBOARDING.md
```

### When NOT to Suggest

- User is in the middle of a task (don't interrupt flow)
- User explicitly says "I know this project" or "skip onboarding"
- User has run `flowforge onboard` recently (check for recent ONBOARDING.md timestamp)
- Solo dev working on their own project (less critical, but still useful after breaks)

### Integration with `/flow-start` (Future)

**Current**: Agents manually detect and suggest.
**Future**: `/flow-start` could automatically check if onboarding is needed and suggest it before proceeding with a new feature.

---

*This file acts as a public contract for IDE-native AI agents (Cursor Composer, Cline, Antigravity, OpenCode) to adhere strictly to the FlowForge methodology.*

---

## 🧑‍🏫 Cross-Cutting Skills (Afectan a cualquier agente)

| Skill | Trigger | Path |
|-------|---------|------|
| `forge-teacher` | Only when `.flowforge.json` explicitly sets `"teacher_mode": true` under `forge.persona`. **Default is OFF** — do not load unless explicitly configured. In Cursor, use `ide/cursor/agents/forge-teacher.md` instead. | [`skills/forge-teacher/SKILL.md`](skills/forge-teacher/SKILL.md) |
