# FlowForge Glossary

> Quick reference for terms used across FlowForge, FlowDoc, and engram-dotnet.  
> 🇪🇸 [Glosario en español](GLOSSARY.es.md)

---

## How to read this glossary

Terms are grouped by the system that defines them. If you are new, read the groups in order: **FlowForge** first (the methodology), then **FlowDoc** (the documentation layer), then **engram** (the memory engine).

---

## FlowForge — Methodology & Workflow

| Term | Plain meaning |
|------|---------------|
| **FlowForge** | The methodology itself. Defines how AI agents collaborate with humans across 5 phases and 5 checkpoints to deliver software features. Think of it as a playbook for human–agent teamwork. |
| **Phase** | One of the 5 stages a feature goes through: Discovery → Intent → Plan → Execution → Close. Each phase has a dedicated agent and produces specific artifacts. |
| **Checkpoint (CKP)** | A formal gate between phases. Some checkpoints are mechanical (the agent stops automatically), others require a human decision. There are 5: CKP-0 through CKP-4. |
| **CKP-0 🔴** | Hard stop at Discovery. If the requirement is too vague, the agent stops and asks for clarification. No negotiation. |
| **CKP-1 🟡** | Human reviews `spec.md` and approves or adjusts before the plan is written. |
| **CKP-2 🟡** | Human reviews `plan.md` and gives the green light to write code. |
| **CKP-3 🔴** | Mechanical safety: if the same feature has gone through 3 rework cycles, the agent escalates to a human instead of retrying. |
| **CKP-4 🟢** | Deploy gate. The agent produces a session summary and the human decides whether to deploy. |
| **Skill (`SKILL.md`)** | A reusable instruction file that defines the behavior of one agent role. Skills are loaded by the agent at runtime. FlowForge ships 7 core skills and 24 specialized ones. |
| **forge-discovery** | The agent that runs Phase 0. It searches memory and existing code before proposing anything new. |
| **forge-arch** | The agent that runs Phase 1. It writes `spec.md` with Given-When-Then scenarios. Never writes code. |
| **forge-plan** | The agent that runs Phase 2. It writes `plan.md` — a step-by-step technical checklist. |
| **forge-dev** | The agent that runs Phase 3. It writes production code and unit tests based on `plan.md`. |
| **forge-verify** | The auditor. It reviews the implementation against `spec.md` and generates a rework ticket if gaps are found. |
| **forge-memory** | The agent that runs Phase 4. It saves learnings to the memory engine and produces the session summary. |
| **forge-orchestrator** | The conductor. It routes commands to the right agent, enforces checkpoints, and manages state transitions. |
| **`/flow-start`** | IDE command to begin a new feature cycle (Phase 0). Creates `.ai-work/{slug}/` and kicks off forge-discovery. |
| **`/flow-plan`** | IDE command to jump to Phase 2 (plan writing). |
| **`/flow-dev`** | IDE command to jump to Phase 3 (code writing). |
| **`/flow-verify`** | IDE command to audit the implementation (forge-verify). |
| **`/flow-close`** | IDE command to run Phase 4 (session close, CKP-4). |
| **`/flow-rework`** | IDE command to process a bug report or review comment as a rework cycle. |
| **`/flow-init`** | Script (not an IDE command) that scaffolds a new project: creates `docs/`, `AGENTS.md`, `.flowforge.json`, and installs IDE packs. |
| **`.ai-work/{slug}/`** | Ephemeral workspace for a feature in progress. Lives only during the feature cycle; merged or archived after `/flow-close`. Not for permanent documentation. |
| **slug** | A short, kebab-case identifier for a feature (e.g. `user-auth`, `task-crud`). Used as the folder name under `.ai-work/`. |
| **`spec.md`** | The feature specification produced by forge-arch. Contains functional requirements, Given-When-Then scenarios, and a Capability Matrix. Lives in `.ai-work/{slug}/`. |
| **`plan.md`** | The technical implementation plan produced by forge-plan. A step-by-step checklist. Lives in `.ai-work/{slug}/`. |
| **`verify-report.md`** | The audit report produced by forge-verify. Lists gaps between spec and implementation. Lives in `.ai-work/{slug}/`. |
| **`rework_ticket.md`** | A structured bug/gap ticket that sends the feature back to forge-dev. Lives in `.ai-work/{slug}/`. Tracks `cycle_count` (max 3 before CKP-3). |
| **`summary.md`** | The session close document produced by forge-memory. Summarizes what was done, decisions made, and next steps. Lives in `.ai-work/{slug}/`. |
| **`context-map.md`** | The discovery output produced by forge-discovery. Lists prior relevant memories, epics, and reusable patterns found in the codebase. Lives in `.ai-work/{slug}/`. |
| **PM-* (Manual Tests)** | Developer manual tests defined in `spec.md`. Must be checked `[x]` by the human developer before `/flow-close` is allowed. Not automated — they require human judgment. |
| **Capability Matrix** | A table in `spec.md` that separates decisions delegated to the AI (`ai_reasoning`) from hard business rules the AI must not override (`deterministic`). |
| **`AGENTS.md`** (project) | A short file at the root of a project that gives any AI agent a quick orientation: stack, sources of truth, and workflow entry points. Not the same as FlowForge's own `AGENTS.md`. |
| **`.flowforge.json`** | Configuration file at the root of a project. Declares FlowForge version, engram settings, docs framework version, and canonical paths for `docs/`. |
| **`DEVELOPMENT.md`** | Living document in `docs/` that contains project setup, test instructions, and coding conventions. Generated by `flow-init`. |
| **ADR (methodology)** | Architecture Decision Record for FlowForge methodology decisions. Lives in `FlowForge/docs/decisions/`. Not the same as a product ADR. |
| **Rework cycle** | One iteration of fix → verify. Counted in `rework_ticket.md` as `cycle_count`. Three cycles trigger CKP-3. |
| **Orchestrator** | See *forge-orchestrator*. Also refers to the shared parity file (`workflow-orchestrator-parity.md`) that keeps all 4 IDEs in sync. |

---

## FlowDoc — Documentation Framework

| Term | Plain meaning |
|------|---------------|
| **FlowDoc** | A documentation framework for small async teams. Defines how to structure `docs/` with PRD, HUs, ADRs, and RFCs in plain Markdown. FlowForge uses FlowDoc as its documentation layer for projects. |
| **PRD (Product Requirements Document)** | A single document (`docs/PRD.md`) that describes what the product is, who it's for, and what problems it solves. The top-level source of truth for product direction. |
| **HU (Historia de Usuario / User Story)** | A structured description of a feature from the user's perspective, following the format *"As a [role], I want [action], so that [benefit]."* Lives in `docs/tasks/HU-NNN-*.md`. |
| **`docs/tasks/`** | Folder that holds all User Stories. Each file is one HU. This is the product backlog in human-readable Markdown. |
| **`flowforge_slug`** | A field in the HU frontmatter that links a User Story to its active `.ai-work/{slug}/` folder. Set when `/flow-start` is run for that HU. |
| **ADR (product)** | Architecture Decision Record for product-level decisions (e.g. "we use PostgreSQL instead of SQLite"). Lives in `docs/architecture/adr/` of the project. Different from FlowForge methodology ADRs. |
| **RFC (Request for Comments)** | A proposal document for significant technical changes that needs team discussion before a decision is made. Lives in `docs/architecture/rfc/`. |
| **`flowdoc-ciclo.md`** | A document that describes the team's 15-day sprint rhythm (planning, review, retro). Optional — relevant from adoption level L3 onwards. |
| **Adoption level (L1–L4)** | Describes how deeply a team has adopted FlowDoc. L1 = just `docs/` structure; L2 = active feature workflow; L3 = full team rhythm; L4 = metrics and formal retrospectives. |
| **`DEVELOPMENT.md`** | See FlowForge section. Defined by FlowForge's ADR-002; FlowDoc's template is adapted to include it. |

---

## engram / Memory Engine

| Term | Plain meaning |
|------|---------------|
| **engram** | Short for `engram-dotnet`. The persistent memory engine that stores agent observations across sessions. Think of it as a structured knowledge base that agents can query and write to. |
| **engram-dotnet** | The .NET 10 implementation of the engram memory engine. Exposes 25 MCP tools for saving, searching, and managing observations. |
| **MCP (Model Context Protocol)** | A protocol that lets AI agents call external tools (like the engram engine) via structured function calls. The IDE connects the agent to MCP servers. |
| **Observation** | The atomic unit of memory in engram. A structured note with fields: `What`, `Why`, `Where`, `Learned`. Saved with `mem_save`. |
| **`topic_key`** | A stable identifier that groups related observations under a named topic (e.g. `architecture/auth-model`). Prevents duplicate entries. |
| **`scope`** | Whether an observation belongs to the whole team (`team`) or just one person (`personal`). Affects visibility and retention. |
| **`mem_save`** | MCP tool call that saves a new observation to the engram database. |
| **`mem_search`** | MCP tool call that searches observations by keyword and project. |
| **`mem_session_summary`** | MCP tool call that saves a structured session close record. Required by forge-memory at every `/flow-close`. |
| **`mem_promote_to_md`** | MCP tool call that renders a stored observation as a Markdown ADR file under `docs/decisions/`. |
| **`local_memory`** | Fallback mode when the engram database is unavailable. Observations are written as `.md` files in `.engram/local_memory/`. Synced to the database at the next session close. |
| **Smart Curation** | The process forge-memory uses to filter `local_memory` files: discard noise, merge duplicates, and save only high-value observations. |
| **TTL (Time-to-Live)** | Configurable expiry time for temporary observations (e.g. command logs, file change notes). `mem_retention_prune` removes expired items. |

---

## IDE / Tooling

| Term | Plain meaning |
|------|---------------|
| **Cursor** | An AI-native IDE (VS Code fork). FlowForge installs rules, agents, and commands into `~/.cursor/`. |
| **OpenCode** | A terminal-based AI coding agent. FlowForge installs a bundle into `~/.config/opencode/`. |
| **VS Code / Copilot** | Standard VS Code with GitHub Copilot. FlowForge installs agent files into `~/.vscode/` and `.github/agents/`. |
| **Antigravity** | A Google Gemini-based agentic coding tool. FlowForge installs rules and workflows into `.agents/` in the project root. |
| **IDE parity** | The guarantee that the same FlowForge workflow (CKP-0 → CKP-4) works identically across all 4 supported IDEs. Maintained via `workflow-orchestrator-parity.md`. |
| **`workflow-orchestrator-parity.md`** | The single shared orchestration spec used by all 4 IDEs. Any change to workflow logic is made here first, then reflected in each IDE's config. |
| **`compile-agents-from-skills.py`** | A script that regenerates Cursor agent files from the canonical `skills/forge-*/SKILL.md` sources. Run automatically by the installer. |

---

## General / Cross-cutting

| Term | Plain meaning |
|------|---------------|
| **SDLC** | Software Development Life Cycle. The full process from idea to deployed software. FlowForge is an "Agentic SDLC" — it applies AI agents to each phase. |
| **Ephemeral artifact** | A file that exists only during a feature cycle (e.g. `spec.md`, `plan.md`). Archived after close; not permanent documentation. |
| **Persistent artifact** | A file that survives beyond the feature cycle (e.g. `docs/PRD.md`, ADRs, `DEVELOPMENT.md`). These live in `docs/` and are the product's long-term truth. |
| **SOLID** | Five object-oriented design principles (Single Responsibility, Open/Closed, Liskov Substitution, Interface Segregation, Dependency Inversion). FlowForge has a dedicated skill (`forge-dev-solid`) that validates generated code against these. |
| **OWASP** | Open Worldwide Application Security Project. A standard for application security. FlowForge's security skills reference OWASP ASVS for vulnerability checks. |
| **Feature flag** | A configuration toggle that enables or disables a feature at runtime without a code deploy. Relevant at FlowDoc adoption level L3+. |
| **Greenfield** | Starting a component from scratch with no existing code to reuse. forge-discovery's Pattern Search step exists specifically to prevent unnecessary greenfield designs. |
| **Spike** | A short time-boxed investigation (typically 1–4 hours) to answer a specific technical question before committing to an implementation approach. Spikes run outside the full FlowForge cycle (no `spec.md`, no `plan.md`). |
| **CKP (abbrev.)** | See *Checkpoint*. |
| **HU (abbrev.)** | See *Historia de Usuario / User Story*. |
| **ADR (abbrev.)** | See *Architecture Decision Record* (either methodology or product flavor). |
| **PRD (abbrev.)** | See *Product Requirements Document*. |
| **RFC (abbrev.)** | See *Request for Comments*. |

---

## Quick map: where does each type of content live?

| Content type | Location | Survives merge? |
|-------------|----------|-----------------|
| Feature spec, plan, verify report | `.ai-work/{slug}/` | No — ephemeral |
| Product backlog (User Stories) | `docs/tasks/HU-*.md` | Yes |
| Product requirements | `docs/PRD.md` | Yes |
| Product architecture decisions | `docs/architecture/adr/` | Yes |
| Coding conventions, setup | `docs/DEVELOPMENT.md` | Yes |
| FlowForge methodology decisions | `FlowForge/docs/decisions/` | Yes (in this repo) |
| Agent memory / learnings | engram DB or `.engram/local_memory/` | Yes |
| IDE workflow rules | `.cursor/`, `.agents/`, `.vscode/` | Yes (installed) |

---

*See [`docs/20-flowdoc-ecosystem.md`](docs/20-flowdoc-ecosystem.md) for the full guide on how FlowForge and FlowDoc work together.*  
*See [`QUICKSTART.md`](QUICKSTART.md) to get started in 5 minutes.*
