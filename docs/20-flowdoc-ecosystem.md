# FlowForge × FlowDoc — Adopter Guide

> **Status**: Active  
> **FlowForge version**: 0.4.1+  
> **FlowDoc version referenced**: 1.1 (2026-06-05)  
> **Related**: [ADR-004](decisions/ADR-004-flowdoc-integration.md) · [GLOSSARY.md](../GLOSSARY.md) · [21-flowdoc-integration-proposal.md](21-flowdoc-integration-proposal.md)

---

## What is FlowDoc?

[FlowDoc](https://github.com/crhistianmdz/FlowDocs) is a documentation framework for small async teams (2–6 people). It defines how to organize `docs/` using plain Markdown in Git, without heavyweight tools. It covers:

- **PRD** — what the product is and who it's for
- **User Stories (HU)** — backlog as human-readable Markdown files
- **ADRs & RFCs** — architecture decisions and proposals
- **Team rhythm** — optional 15-day sprint cycle

FlowForge uses FlowDoc as its **documentation layer for generated projects**. When you run `flow-init`, the created `docs/` structure comes from FlowDoc v1.1 templates (adapted and attributed — see [ADR-004](decisions/ADR-004-flowdoc-integration.md)).

---

## Two layers, one repo

FlowForge and FlowDoc serve different purposes and must not be confused:

```
┌─────────────────────────────────────────────────────┐
│  LAYER 1 — FlowForge (how we work)                  │
│  skills/forge-*  ·  CKP-0→4  ·  ADR methodology    │
└──────────────────────┬──────────────────────────────┘
                       │ orchestrates
┌──────────────────────▼──────────────────────────────┐
│  LAYER 2 — FlowDoc (what we build)                  │
│  docs/PRD.md  ·  docs/tasks/HU-*  ·  docs/arch/adr  │
└──────────────────────┬──────────────────────────────┘
                       │ triggers / feeds back
┌──────────────────────▼──────────────────────────────┐
│  EPHEMERAL — Feature in progress                    │
│  .ai-work/{slug}/spec.md · plan.md · verify-report  │
└─────────────────────────────────────────────────────┘
```

**The golden rule:**

| If the content… | It belongs in… |
|----------------|----------------|
| Survives the merge / is product truth | `docs/` (FlowDoc layer) |
| Is work in progress for a feature | `.ai-work/{slug}/` (FlowForge ephemeral) |
| Defines agent behavior by role | `skills/forge-*/SKILL.md` |
| Defines routing, gates, cross-cutting protocol | Orchestrator (`workflow-orchestrator-parity.md`) |
| Is a FlowForge methodology decision | `FlowForge/docs/decisions/` |
| Is a product architecture decision | `docs/architecture/adr/` in the target repo |

---

## What FlowForge takes from FlowDoc v1.1

### Taken without modification

| FlowDoc artifact | Where it lives in generated project |
|-----------------|--------------------------------------|
| `docs/` folder structure | `templates/project/docs/` |
| PRD template | `templates/project/docs/PRD.md` |
| ADR template (product) | `templates/project/docs/architecture/adr/` |
| RFC template | `templates/project/docs/architecture/rfc/` |
| `flowdoc-ciclo.md` (optional, L3+) | `templates/project/docs/flowdoc-ciclo.md` |
| Adoption levels concept (L1–L4) | Mapped to CKP levels below |

### Taken and modified

| FlowDoc artifact | Modification | Reason |
|-----------------|-------------|--------|
| HU template | Added `flowforge_slug` + `status` frontmatter fields; added `## FlowForge` section | Links HU ↔ `.ai-work/{slug}/`; enables traceability |

### Not taken (and why)

| FlowDoc element | Why excluded |
|----------------|-------------|
| `openspec/changes/{name}/` path | **Replaced** by `.ai-work/{slug}/` — FlowForge decision (ADR-002) |
| `sdd-context.md` (ADR-009) | Renamed proposal: `flow-context.md`; implementation deferred to Sprint 2 |
| `/sdd-*` commands | Separate ceremony; FlowForge uses `/flow-*` exclusively |
| `hu-to-issues` scripts | Deferred to Sprint 2 |

> **Important for existing FlowDoc users:** If your project previously used `openspec/`, migrate its contents to `.ai-work/` following the [FlowDoc adoption guide](https://github.com/crhistianmdz/FlowDocs). The `openspec/` path is not recognized by FlowForge agents.

---

## Adoption levels: combined matrix

FlowDoc defines L1–L4. FlowForge adds CKPs. Here is how they map:

| FlowDoc level | What it includes | FlowForge equivalent | Active CKPs |
|---------------|-----------------|---------------------|-------------|
| **L1** Docs only | PRD + HU + ADR in `docs/` | Optional: no `/flow-*` needed | None |
| **L2** SDD basic | SDD-Ready templates active | `/flow-start` … `/flow-close` | CKP-0 → CKP-4 |
| **L3** Team | Sprint cycle + feature flags + owners | L2 + `flowdoc-ciclo.md` | L2 + human retrospective |
| **L4** Full team | Metrics + formal retro | L3 + `forge-memory/metrics` | L4 + KPIs |

**Default when you run `flow-init`:** `adoption_level: 1` — your project gets documented from day 1.  
Activate FlowForge agents when your team is ready (L2).

---

## The feature lifecycle with both frameworks

```
Planning           →  Execution         →  Close
─────────────────────────────────────────────────
docs/tasks/            .ai-work/{slug}/       docs/
HU-042-login.md        spec.md                architecture/adr/
  (backlog)            plan.md                  (decisions land here)
      │                verify-report.md      HU-042-login.md
      │                summary.md              status: done ✓
      │                                        flowforge_slug: user-login
      ▼                    ▲
 /flow-start {slug}   forge-memory at /flow-close
 sets flowforge_slug  updates HU checkboxes
```

**Key traceability chain:**

1. **HU** defines the user need and acceptance criteria
2. `/flow-start` creates `.ai-work/{slug}/` and links it via `flowforge_slug` in the HU
3. forge-arch imports the HU's "As a / I want / So that" into `spec.md` (no manual copy)
4. forge-memory at `/flow-close` checks off PM-* in `spec.md` and updates the HU `status` to `done`

---

## Keeping templates in sync with FlowDoc upstream

FlowForge pins the FlowDoc version in `.flowforge.json`:

```json
"docs_framework": "flowdoc@1.1"
```

When FlowDoc releases a new version:

1. Review the FlowDoc changelog for changes to templates we've imported
2. Update affected files in `templates/project/`
3. Update the version pin in `.flowforge.json.template`
4. Update the attribution header in changed template files
5. Update this document and ADR-004 status history

Templates that FlowForge modified (HU template) must be reviewed carefully — FlowDoc upstream changes may conflict with our added fields.

---

## Known gaps (upstream FlowDoc)

These are improvement requests filed with FlowDoc maintainers (tracked in [ADR-004](decisions/ADR-004-flowdoc-integration.md) section "Upstream improvement requests"):

| Gap | Impact on FlowForge adopters | Workaround |
|----|------------------------------|------------|
| FlowDoc L2 guide still mentions `openspec/` | Confuses teams migrating from FlowDoc L2 | Use `.ai-work/` as documented here; ignore `openspec/` references |
| ADR-009 hardcodes `openspec/changes/` | `sdd-context.md` not usable with FlowForge | Deferred — `flow-context.md` coming in Sprint 2 |
| HU template lacks `flowforge_slug` upstream | Manual frontmatter needed when using FlowDoc templates directly | FlowForge templates already include it |
| No `flowdoc.version` export file | Hard to detect version programmatically | Use commit date + release tag manually |

---

## Two `AGENTS.md` files explained

FlowForge projects have **two different files** both called `AGENTS.md`. This is intentional:

| File | Location | Purpose | Maintained by |
|------|----------|---------|--------------|
| **FlowForge index** | `FlowForge/AGENTS.md` | Skill index + CKP overview for agents working on FlowForge itself | FlowForge team |
| **Project context** | `{your-project}/AGENTS.md` | Stack, sources of truth, domain rules for agents working on your product | Your tech lead |

The **project `AGENTS.md`** (generated by `flow-init`) should be short (40–60 lines) and contain only:
- Stack description
- Table of sources of truth (pointing to `docs/`)
- Workflow entry points (`/flow-start`, etc.)
- Project-specific agent rules

It should **not** contain: CKP details, skill lists, memory curation protocol, or anything that belongs in FlowForge itself.

---

## Quick start for a new project

```bash
# 1. Scaffold the project (creates docs/, AGENTS.md, .flowforge.json, IDE packs)
bash /path/to/FlowForge/flow-init.sh /path/to/my-new-project

# 2. Fill in your PRD
edit docs/PRD.md

# 3. Create your first User Story
cp docs/templates/HU-template.md docs/tasks/HU-001-first-feature.md
edit docs/tasks/HU-001-first-feature.md

# 4. Start your first feature cycle
/flow-start first-feature
```

See [`QUICKSTART.md`](../QUICKSTART.md) for the full walkthrough.  
See [`GLOSSARY.md`](../GLOSSARY.md) for term definitions.

---

*Adapted from the integration analysis in [`docs/21-flowdoc-integration-proposal.md`](21-flowdoc-integration-proposal.md).*  
*FlowDoc templates used under MIT license — see [ADR-004](decisions/ADR-004-flowdoc-integration.md) for full attribution.*
