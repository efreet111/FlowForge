# ADR-004 — FlowDoc integration & artifact boundaries

> **Status**: Accepted  
> **Date**: 2026-06-18  
> **Feature**: `flowdoc-integration` (roadmap items **5–6** — `flow-init` + `.flowforge.json`)  
> **Deciders**: FlowForge methodology team  
> **Links**: [`docs/21-flowdoc-integration-proposal.md`](../21-flowdoc-integration-proposal.md) · [`ADR-002`](ADR-002-scaffold-doc-policy.md) · [`04-roadmap.md`](../04-roadmap.md) items 5–6 · [`docs/20-flowdoc-ecosystem.md`](../20-flowdoc-ecosystem.md)

---

## Attribution

The documentation templates and structural conventions adopted in this ADR are adapted from:

> **FlowDoc** by Cristian M. (`crhistianmdz`)  
> Repository: `https://github.com/crhistianmdz/FlowDocs` (private as of 2026-06-18)  
> Version referenced: **FlowDoc v1.1** (2026-06-05)  
> License: MIT  

Templates included in `templates/project/` that originate from FlowDoc carry the header:
```
<!-- Adapted from FlowDoc v1.1 (github.com/crhistianmdz/FlowDocs) — MIT License -->
```

Original FlowForge templates do not carry this header.

---

## Context

ADR-002 (*Scaffold documentation policy*) decided that `flow-init` will generate a consistent project skeleton — `AGENTS.md`, `docs/DEVELOPMENT.md`, `docs/decisions/`, and `.flowforge.json` — for every repo that adopts FlowForge.

However, ADR-002 left the concrete templates undefined. Meanwhile, FlowDoc (an independent documentation framework for small async teams) already provides battle-tested templates for exactly the `docs/` layer FlowForge needs: PRD, User Stories, ADRs, RFCs, and a team rhythm document.

FlowDoc has explicitly positioned itself as complementary to FlowForge:
> *"FlowForge minimizes SDD overhead; FlowDoc is the documentation that flows."*

Without explicit integration, FlowForge adopters would:
- Invent `docs/` structure ad hoc, creating drift between projects
- Mix `.ai-work/` (FlowForge ephemeral layer) with `openspec/` (FlowDoc legacy path, now deprecated)
- Have no canonical source of truth for product documentation alongside the methodology

---

## Decision

**FlowForge adopts FlowDoc v1.1 templates as its documentation layer for generated projects.**

This means:
1. `flow-init` copies a curated subset of FlowDoc templates into `templates/project/docs/`
2. The artifact boundary table below is the canonical rule for where each content type lives
3. `openspec/` is explicitly prohibited in FlowForge + FlowDoc projects; `.ai-work/{slug}/` replaces it
4. `.flowforge.json` records the FlowDoc version via `"docs_framework": "flowdoc@1.1"`

### Artifact boundaries (the canonical table)

| Content type | Location | Layer | Ephemeral? |
|-------------|----------|-------|-----------|
| Feature spec, plan, verify report | `.ai-work/{slug}/` | FlowForge | Yes — archived at close |
| Session summary | `.ai-work/{slug}/summary.md` | FlowForge | Yes — archived at close |
| Product backlog (User Stories) | `docs/tasks/HU-*.md` | FlowDoc | No |
| Product requirements | `docs/PRD.md` | FlowDoc | No |
| Product architecture decisions | `docs/architecture/adr/` | FlowDoc | No |
| RFCs in discussion | `docs/architecture/rfc/` | FlowDoc | No |
| Coding conventions, setup | `docs/DEVELOPMENT.md` | FlowForge (ADR-002) | No |
| Team rhythm, sprint cycle | `docs/flowdoc-ciclo.md` | FlowDoc | No |
| FlowForge methodology decisions | `FlowForge/docs/decisions/` | FlowForge | No — this repo only |
| Agent rules (project-specific) | `AGENTS.md` at project root | FlowForge | No |
| Agent memory / observations | engram DB or `.engram/local_memory/` | engram | No |
| **PROHIBITED** | `openspec/` | — | Forbidden in FF+FD projects |

### What we take from FlowDoc v1.1

| Taken from FlowDoc | Adapted how | Lives in FlowForge |
|--------------------|-------------|-------------------|
| `docs/` folder structure | Unchanged | `templates/project/docs/` |
| PRD template | Unchanged | `templates/project/docs/PRD.md` |
| HU template | Added `flowforge_slug` frontmatter field + `/flow-start` reference | `templates/project/docs/tasks/HU-001-example.md` |
| ADR template for product | Unchanged | `templates/project/docs/architecture/adr/` |
| RFC template | Unchanged | `templates/project/docs/architecture/rfc/` |
| Adoption levels L1–L4 concept | Mapped to CKP levels (see doc 20) | `docs/20-flowdoc-ecosystem.md` |
| `flowdoc-ciclo.md` | Unchanged, optional (L3+) | `templates/project/docs/flowdoc-ciclo.md` |

### What we do NOT take from FlowDoc

| Not taken | Reason |
|-----------|--------|
| `openspec/changes/{name}/` path | Replaced by `.ai-work/{slug}/` — FlowForge decision |
| `sdd-context.md` (ADR-009 pattern) | Renamed to `flow-context.md`; implementation deferred to Sprint 2 |
| `hu-to-issues` scripts | Deferred to Sprint 2 (multi-OS complexity) |
| `/sdd-*` commands | Separate ceremony; FlowForge uses `/flow-*` exclusively |

### Modifications to imported templates

| Change | File affected | Reason |
|--------|-------------|--------|
| Added `flowforge_slug: ""` frontmatter | HU template | Links HU ↔ `.ai-work/{slug}/` |
| Added `status: draft\|in-progress\|done` frontmatter | HU template | Visible progress in backlog |
| Added `## FlowForge` section | HU template | Points to `/flow-start` command |
| Attribution header comment | All FlowDoc-derived templates | MIT license compliance |

---

## Options considered

### Option A — Define FlowForge-only `docs/` templates from scratch

**Pros:** No external dependency; full control.  
**Cons:** Duplicates battle-tested work already done in FlowDoc; drift between communities.  
**Rejected.**

### Option B — Require teams to install FlowDoc separately

**Pros:** Clean separation; FlowDoc evolves independently.  
**Cons:** Two install steps; FlowForge `flow-init` cannot guarantee a consistent project skeleton.  
**Rejected** for MVP; may revisit for a plugin model post-v1.

### Option C — Embed a versioned snapshot of FlowDoc templates (chosen)

**Pros:** `flow-init` is self-contained; one command = full skeleton; version-pinned = reproducible.  
**Cons:** Templates may drift from FlowDoc upstream; requires attribution and changelog discipline.  
**Accepted.** Mitigation: `"docs_framework": "flowdoc@1.1"` in `.flowforge.json` makes the version explicit and auditable.

---

## Consequences

**Positive:**
- `flow-init` now produces a complete, opinionated project skeleton in one command
- HUs in `docs/tasks/` link directly to `.ai-work/{slug}/` via `flowforge_slug` — full traceability
- forge-memory can close the loop by updating HU checkboxes at `/flow-close`
- Adopters get a proven documentation structure without having to invent it
- Attribution is explicit and auditable in every template file

**Negative / accepted:**
- Templates in `templates/project/` may drift from FlowDoc upstream over time
  - Mitigation: `docs_framework` version pin + entry in this ADR when templates are updated
- Two representations of a feature exist simultaneously (HU + `spec.md`) during the cycle
  - Mitigation: forge-memory sync at close; `flowforge_slug` field links them
- FlowDoc L2 adoption guide still references `openspec/` (upstream issue F1/F2 from proposal doc 21)
  - Mitigation: document the conflict in `docs/20-flowdoc-ecosystem.md`; propose fix upstream

---

## Implementation roadmap (FlowForge)

| Step | Deliverable | Status |
|------|-------------|--------|
| **1** | This ADR accepted | ✅ |
| **2** | `templates/project/` with curated FlowDoc subset + modifications | 📋 |
| **3** | `flow-init.sh` + `flow-init.ps1` scripts | 📋 |
| **4** | `.flowforge.json` schema implemented | 📋 |
| **5** | forge-discovery: read `docs/PRD.md` + list HUs | 📋 |
| **6** | forge-arch: import HU As/I want/To when `flowforge_slug` is set | 📋 |
| **7** | forge-memory: update HU status + project CHANGELOG at close | 📋 |
| **8** | `docs/20-flowdoc-ecosystem.md` adopter guide published | 📋 |

**Out of scope for this sprint:**
- `flow-context.md` / ADR-009 FlowDoc adaptation → Sprint 2
- `hu-to-issues` multi-OS scripts → Sprint 2
- Legacy project migration guide → Sprint 3
- Full `es/` templates → Sprint 3

---

## Upstream improvement requests (FlowDoc)

The following improvements have been identified for FlowDoc upstream (from proposal doc 21, section 8):

| ID | Request | Priority |
|----|---------|----------|
| F1 | Deprecate `openspec/` in active docs (adoption L2, ADR-009) | High — blocks clean integration |
| F2 | ADR-009: make artifact store path configurable | High |
| F3 | HU template: add FlowForge block + `flowforge_slug` frontmatter | High |
| F4 | Export VERSION pin (`flowdoc.version` file or semver tag) | High |
| F5 | Rename `sdd-context.md` → neutral name (`flow-context.md`) | Medium |
| F6 | Differentiate `AGENTS.project.md` from methodology `AGENTS.md` | Medium |

---

## Related

- [ADR-001 — Memory Curation Protocol](ADR-001-memory-curation-protocol.md)
- [ADR-002 — Scaffold documentation policy](ADR-002-scaffold-doc-policy.md)
- [ADR-003 — Pattern Search Mandate](ADR-003-pattern-search-mandate.md)
- [docs/21-flowdoc-integration-proposal.md](../21-flowdoc-integration-proposal.md) — full analysis and discussion document
- [docs/20-flowdoc-ecosystem.md](../20-flowdoc-ecosystem.md) — adopter guide (generated from this ADR)
- Roadmap items **5** (Project template), **6** (`.flowforge.json` schema)

---

## Status history

- **2026-06-14**: Draft proposal created (`docs/21-flowdoc-integration-proposal.md`)
- **2026-06-18**: ADR-004 written and accepted — implementation begins
