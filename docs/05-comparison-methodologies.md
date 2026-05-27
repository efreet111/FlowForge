# FlowForge — Methodology comparison

> **Last updated**: 2026-05-27
> **Purpose**: Research that informed FlowForge design (legacy research name: EngramFlow)

---

## 1. Methodologies studied

### 1.1 Agentsway

**Origin**: Academic — arXiv 2510.23664 (Oct 2025)  
**URL**: https://arxiv.org/abs/2510.23664

**Proposal**: Structured lifecycle with human orchestration; specialized roles (Planning, Prompting, Coding, Testing, Fine-Tuning); privacy-by-design; retrospective learning via LLM fine-tuning; measurable productivity metrics.

**Strengths**: First formal methodology for AI-agent teams; governance and transparency.

**Weaknesses for SMB**: Fine-tuning is expensive; rigid one-agent-per-role; academic validation gap.

**Adopted**: Role structure + human orchestration.  
**Rejected**: Fine-tuning as memory (replaced by engram-dotnet + two levels). Rigid 1:1 agent roles.

---

### 1.2 TACO Framework (KPMG)

**Origin**: Enterprise — KPMG (2025)

**Proposal**: Four agent types — Taskers, Automators, Collaborators, Orchestrators; human-on-the-loop.

**Strengths**: Practical taxonomy; on-the-loop vs in-the-loop distinction.

**Weaknesses for SMB**: Classification only — no lifecycle, artifacts, or checkpoints.

**Adopted**: Orchestrator as optional; checkpoint semantics.  
**Rejected**: Fixed four-type taxonomy; separate “Tasker” agents (use skills/tools instead).

---

### 1.3 MCP Agentic SDLC

**Origin**: GitHub — michaelwybraniec/mcp-agentic-sdlc (2025)

**Proposal**: Six phases from problem definition to maintenance; AWP protocol; framework-agnostic; quality gates; recipes for MVP/POC/Pro.

**Strengths**: Flexible; MCP-native; reusable recipes.

**Weaknesses for SMB**: Six linear phases add overhead; generic recipes.

**Adopted**: Quality gates; MCP-first; recipe idea → canonical `spec.md` + `plan.md`.  
**Rejected**: Six linear phases (FlowForge uses five with inner loops).

---

### 1.4 Twelve-Factor Agentic SDLC

**Origin**: GitHub — tikalk/agentic-sdlc-12-factors (2025)

**Proposal**: Twelve principles including context scaffolding, mission → spec, spec → plan, dual execution loops, Great Filter (human quality arbiter), directives as code, traceability.

**Strengths**: Clear principles; strongest conceptual influence on FlowForge.

**Weaknesses for SMB**: Many principles to remember; no concrete role/phase diagram.

**Adopted**: spec.md, plan.md, inner loop + human gates, directives as code, AI-augmented testing.  
**Rejected**: Little — primary conceptual base.

---

### 1.5 ADLC — Arthur.ai

**Origin**: Arthur.ai (2026)

**Proposal**: Agent Development Flywheel — evaluate → failures → improve evals → deploy; emphasis on behavioral testing.

**Strengths**: Practical flywheel for probabilistic systems.

**Weaknesses for SMB**: Assumes production eval data; less about traditional feature SDLC.

**Adopted**: Verify feedback into spec/plan quality.  
**Rejected**: Flywheel as primary gate (FlowForge uses human checkpoints).

---

### 1.6 THV Agentic Development Playbook

**Origin**: THV VC — João Camarate (2026)

**Proposal**: Seven sequential steps with human gate each (PM → UX → Architect → Implementer → Reviewer → Security → QA).

**Strengths**: Explicit human judgment; AGENTS.md as config hub.

**Weaknesses for SMB**: Seven gates and seven roles — too heavy.

**Adopted**: AGENTS.md as project constitution; gates as expertise, not bureaucracy.  
**Rejected**: Seven gates (FlowForge uses five CKPs); separate UX agent.

---

## 2. Comparison table

| Dimension | Agentsway | TACO | MCP SDLC | Twelve-Factor | ADLC | THV | **FlowForge** |
|-----------|-----------|------|----------|---------------|------|-----|---------------|
| **Agents** | 5 roles | 4 types | H+AI collab | Not defined | Not defined | 7 roles | **7 agents** |
| **Human checkpoints** | Human orchestration | On-the-loop | Quality gates | Great Filter | Not defined | 7 gates | **5 (CKP-0→4)** |
| **Phases** | Continuous | N/A | 6 phases | 12 factors | 6 phases | 7 steps | **5 phases** |
| **Artifacts** | Undefined | Undefined | Recipes | spec, plan | Evals | User story… | **spec, plan, verify-report, rework_ticket** |
| **Memory** | Fine-tuning | Undefined | Undefined | Context scaffolding | Eval suites | Undefined | **engram-dotnet (2 levels)** |
| **Orchestrator** | Human | AI optional | Human+AI | Dual loops | Flywheel | Human | **IDE coordinator (no inline dev)** |
| **Complexity** | High | Medium | Medium | Low | High | High | **Low** |
| **Target** | Enterprise | Enterprise | General | General | Enterprise | SMB+Ent | **SMB 2–20** |

---

## 3. What FlowForge takes from each

```
Agentsway     → human orchestration, scoped roles
TACO          → optional orchestrator, on-the-loop checkpoints
MCP SDLC      → MCP-native, quality gates
Twelve-Factor → spec/plan, dual loops, Great Filter, directives as code, AI tests
ADLC          → evaluation flywheel mindset
THV           → AGENTS.md hub, gates = expertise
```

---

## 4. Key design decisions

| Decision | Rationale |
|----------|-----------|
| **5 checkpoints (CKP-0→4)** | Hard stops at 0 and 3; human gates at 1, 2, 4 — fewer than seven-gate playbooks, clearer than legacy “3 checkpoints” |
| **Model routing over agent sprawl** | Tiered models per task beat many specialized agents for SMB cost/quality |
| **engram-dotnet over fine-tuning** | RAG + FTS5 + `.md` at fraction of QLoRA cost |
| **Optional AI orchestrator** | Versioned artifacts + IDE rules often suffice |
| **Artifacts as protocol** | spec → plan → verify/rework; Git is the bus |
| **Memory Janitor ≠ agent** | TTL prune is SQL; LLM only where needed |
