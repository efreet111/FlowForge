# ADR-004 — FlowDoc integration & artifact boundaries

> **Status**: Accepted  
> **Date**: 2026-06-18  
> **Last updated**: 2026-07-08 (v2.0 migration)  
> **Feature**: `flowdoc-integration` (roadmap items **5–6** — `flow-init` + `.flowforge.json`)  
> **Deciders**: FlowForge methodology team  
> **Links**: [`docs/21-flowdoc-integration-proposal.md`](../21-flowdoc-integration-proposal.md) · [`ADR-002`](ADR-002-scaffold-doc-policy.md) · [`04-roadmap.md`](../04-roadmap.md) items 5–6 · [`docs/20-flowdoc-ecosystem.md`](../20-flowdoc-ecosystem.md)

---

## Attribution

The documentation templates and structural conventions adopted in this ADR are adapted from:

> **FlowDoc** by Cristian M. (`crhistianmdz`)  
> Repository: `https://github.com/crhistianmdz/FlowDocs` (private as of 2026-06-18)  
> Version referenced: **FlowDoc v2.0** (2026-06-05 — original v1.1 superseded 2026-07-08)  
> License: MIT  

Templates included in `templates/project/` that originate from FlowDoc carry the header:
```
<!-- Adapted from FlowDoc v2.0 (github.com/crhistianmdz/FlowDocs) — MIT License -->
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

**FlowForge adopts FlowDoc v2.0 templates as its documentation layer for generated projects.**

This means:
1. `flow-init` copies a curated subset of FlowDoc v2.0 templates into `templates/project/docs/`
2. The artifact boundary table below is the canonical rule for where each content type lives
3. `openspec/` is explicitly prohibited in FlowForge + FlowDoc projects; `.ai-work/{slug}/` replaces it
4. `.flowforge.json` records the FlowDoc version via split keys: `"docs_framework": "flowdoc"` + `"docs_framework_version": "2.0"`
5. The upstream FlowDoc repository is declared in `.flowforge.json` under `upstream` for provenance tracking

### Artifact boundaries (the canonical table)

| Content type | Location | Layer | Ephemeral? |
|-------------|----------|-------|-----------|
| Feature spec, plan, verify report | `.ai-work/{slug}/` | FlowForge | Yes — archived at close |
| Session summary | `.ai-work/{slug}/summary.md` | FlowForge | Yes — archived at close |
| Product backlog (User Stories) | `docs/tasks/HU-001-HU-099/HU-NNN.md` | FlowDoc | No |
| Product requirements | `docs/PRD.md` | FlowDoc | No |
| Product architecture decisions | `docs/architecture/adr/` | FlowDoc | No |
| RFCs in discussion | `docs/architecture/rfc/` | FlowDoc | No |
| Coding conventions, setup | `docs/DEVELOPMENT.md` | FlowForge (ADR-002) | No |
| FlowForge methodology decisions | `FlowForge/docs/decisions/` | FlowForge | No — this repo only |
| Agent rules (project-specific) | `AGENTS.md` at project root | FlowForge | No |
| Agent memory / observations | engram DB or `.engram/local_memory/` | engram | No |
| **PROHIBITED** | `openspec/` | — | Forbidden in FF+FD projects |

> **Note (2026-07-08):** The `docs/flowdoc-ciclo.md` row has been removed from the artifact boundaries table. `flowdoc-ciclo.md` is deprecated in FlowDoc v2.0. FlowForge provides its own workflow lifecycle via CKP-0→4. See [Non-adopted elements](#what-we-do-not-take-from-flowdoc) below.

### What we take from FlowDoc v2.0

| Taken from FlowDoc | Adapted how | Lives in FlowForge |
|--------------------|-------------|-------------------|
| `docs/` folder structure | Unchanged | `templates/project/docs/` |
| PRD template | Unchanged | `templates/project/docs/PRD.md` |
| HU template (detailed) | Added `flowforge_slug` frontmatter + `## FlowForge` section + status lifecycle; adapted Owner & Timeline and Technical Debt sections from v2.0 | `templates/project/docs/templates/HU-template.md` |
| GWT Scenarios section (Happy Path, Edge Cases, Error Cases) | Adopted from v2.0, extended with `🧪 Ref` test links | `templates/project/docs/templates/HU-template.md` |
| Owner & Timeline section | Adopted from v2.0 (owner, milestone, dependencies) | `templates/project/docs/templates/HU-template.md` |
| Technical Debt section | Adopted from v2.0 as optional trailing section | `templates/project/docs/templates/HU-template.md` |
| Range-binned folder structure (ADR-005) | Adopted: `docs/tasks/HU-001-HU-099/` | `templates/project/docs/tasks/` |
| ADR template for product | Unchanged | `templates/project/docs/architecture/adr/` |
| RFC template | Unchanged | `templates/project/docs/architecture/rfc/` |
| Adoption levels L1–L3 concept | Mapped to FlowForge equivalents (see doc 20) | `docs/20-flowdoc-ecosystem.md` |

### What we do NOT take from FlowDoc

| Not taken | Reason |
|-----------|--------|
| `openspec/changes/{name}/` path | Replaced by `.ai-work/{slug}/` — FlowForge decision |
| `sdd-context.md` (ADR-009 pattern) | Renamed to `flow-context.md`; implementation deferred to Sprint 2 |
| `hu-to-issues` scripts | Deferred to Sprint 2 (multi-OS complexity) |
| `/sdd-*` commands | Separate ceremony; FlowForge uses `/flow-*` exclusively |
| `flowdoc-ciclo.md` | **Deprecated in FlowDoc v2.0.** FlowForge provides its own workflow lifecycle via CKP-0→4 and `/flow-*` commands. The team rhythm layer (L3) is covered by `forge-memory` summaries and optional retrospectives. |
| `priority` frontmatter field | Removed. FlowForge uses `status` for lifecycle tracking; prioritization is done through the order of HUs in the backlog. The `priority` field from FlowDoc v1.1 was not adopted in v2.0. |
| `created` frontmatter field | Removed. Git history provides creation timestamps; an explicit frontmatter field is redundant and prone to staleness. |
| **Tasks section** (FlowDoc v2.0) | Not adopted. FlowForge captures implementation tasks in `.ai-work/{slug}/plan.md` (generated by forge-plan). The HU stays business-focused. |
| **Contract section** (FlowDoc v2.0) | Not adopted. API contracts live alongside the code (OpenAPI specs, protobuf definitions). The HU should not duplicate technical specifications. |

### FlowForge-specific additions to the HU template

The following frontmatter fields and template sections are FlowForge additions to the FlowDoc v2.0 HU template. They serve traceability and process automation:

| Addition | Type | Reason |
|----------|------|--------|
| `hu_id: HU-NNN` | Frontmatter | Unique identifier for each User Story; used by forge-discovery to list and order backlog items |
| `status: draft` | Frontmatter | Lifecycle tracking: `draft` → `in-progress` (set by `/flow-start`) → `done` (set by forge-memory at `/flow-close`). Replaces the removed `priority` field. |
| `flowforge_slug: ""` | Frontmatter | Links HU ↔ `.ai-work/{slug}/`. Set by forge-arch when `.ai-work/{slug}/` is created. Enables forge-memory to check off acceptance criteria at close. |
| `## FlowForge` section | Template body | Provides `/flow-start` invocation instructions and documents the status lifecycle. Managed by FlowForge agents — adopters should not edit this section. |
| `## Definition of Done` | Template body | Standardizes the checklist gates (review, tests, manual PM-*, ADR, docs) that must pass before closing. Applied by forge-verify + forge-memory. |
| `🧪 Ref` field in Scenarios | Template body | Links each GWT scenario to its corresponding test file. Populated during implementation for full traceability from spec to tests. |

### Modifications to imported templates (v2.0)

| Change | File affected | Reason |
|--------|-------------|--------|
| Replaced v1.1 simple template with v2.0 detailed template | HU template | Adopts GWT scenarios (Happy Path, Edge Cases, Error Cases), Owner & Timeline, Technical Debt, and expanded DoD |
| Added `hu_id`, `status`, `flowforge_slug` frontmatter | HU template | Process automation and traceability (see FlowForge additions above) |
| Added `## FlowForge` section | HU template | Points to `/flow-start` command + status lifecycle documentation |
| Added `🧪 Ref` fields to GWT scenarios | HU template | Links scenarios to test files for full traceability |
| Range-binned folder structure (ADR-005) | `docs/tasks/` | `docs/tasks/HU-001-HU-099/` replaces flat `docs/tasks/` for filesystem performance and navigation at scale |
| Attribution header comment | All FlowDoc-derived templates | MIT license compliance; header updated to reference v2.0 |

---

## Version pin strategy

FlowForge declares the upstream documentation framework and its version through **split keys** in `.flowforge.json`, not a single concatenated string. This enables programmatic parsing, diff-friendly version bumps, and explicit upstream provenance:

```json
{
  "docs_framework": "flowdoc",
  "docs_framework_version": "2.0",
  "upstream": {
    "repo": "https://github.com/crhistianmdz/FlowDocs",
    "status": "private"
  }
}
```

| Field | Purpose |
|-------|---------|
| `docs_framework` | Identifier for the documentation framework ("flowdoc"). Used by forge-discovery to determine which parsing rules to apply. |
| `docs_framework_version` | Semver-compatible version pin. Used to detect template drift and flag when a migration review is needed. |
| `upstream.repo` | Canonical upstream URL. For attribution, changelog comparison, and future auto-sync tooling. |
| `upstream.status` | Visibility of the upstream repo. Documented so adopters know whether they can browse the original source. |

### Why split keys (not a single `"flowdoc@2.0"` string)

- **Parseability**: agents and tools can read `docs_framework_version` as a semver value without string splitting
- **Diff-friendly**: upgrading `"2.0"` to `"3.0"` is a one-line JSON change without touching the framework name
- **Extensibility**: `upstream` block can grow to include branch, commit hash, or sync frequency without changing the existing keys
- **Scripting**: CI/CD and init scripts can gate on numeric version comparisons (`>= 2.0`) without regex

The previous v1.1 format (`"docs_framework": "flowdoc@1.1"`) is superseded. New `flowforge init` runs write the split format. Existing projects should migrate their `.flowforge.json` when adopting v2.0 templates.

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
**Accepted.** Mitigation: `docs_framework` + `docs_framework_version` split keys in `.flowforge.json` make the version explicit, auditable, and parsable by agents.

---

## Consequences

**Positive:**
- `flow-init` now produces a complete, opinionated project skeleton in one command
- HUs in `docs/tasks/HU-001-HU-099/` link directly to `.ai-work/{slug}/` via `flowforge_slug` — full traceability
- forge-memory can close the loop by updating HU checkboxes at `/flow-close`
- Adopters get a proven documentation structure without having to invent it
- Attribution is explicit and auditable in every template file
- The v2.0 template's GWT scenarios, Owner & Timeline, and Technical Debt sections provide richer upfront planning without overloading the HU

**Negative / accepted:**
- Templates in `templates/project/` may drift from FlowDoc upstream over time
  - Mitigation: `docs_framework` + `docs_framework_version` split keys + entry in this ADR when templates are updated
- Two representations of a feature exist simultaneously (HU + `spec.md`) during the cycle
  - Mitigation: forge-memory sync at close; `flowforge_slug` field links them
- FlowDoc L2 adoption guide still references `openspec/` (upstream issue F1/F2 from proposal doc 21)
  - Mitigation: document the conflict in `docs/20-flowdoc-ecosystem.md`; propose fix upstream
- v2.0 removes `flowdoc-ciclo.md` which was previously listed as adopted in v1.1 ADR
  - Mitigation: FlowForge already provides CKP workflow; removed from artifact table and template set

---

## Implementation roadmap (FlowForge)

| Step | Deliverable | Status |
|------|-------------|--------|
| **1** | This ADR accepted | ✅ |
| **2** | `templates/project/` with curated FlowDoc v1.1 subset + modifications | ✅ |
| **3** | `flow-init.sh` + `flow-init.ps1` scripts | 📋 |
| **4** | `.flowforge.json` schema implemented | 📋 |
| **5** | forge-discovery: read `docs/PRD.md` + list HUs | 📋 |
| **6** | forge-arch: import HU As/I want/To when `flowforge_slug` is set | 📋 |
| **7** | forge-memory: update HU status + project CHANGELOG at close | 📋 |
| **8** | `docs/20-flowdoc-ecosystem.md` adopter guide published | 📋 |
| **9** | **v2.0 migration**: replace HU template, update ADR-004, update ecosystem doc, update QUICKSTART.md, add split keys to `.flowforge.json` | ✅ (2026-07-08) |

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
- **2026-06-18**: ADR-004 written and accepted — v1.1 implementation begins
- **2026-07-08**: v2.0 migration completed — HU template replaced with detailed v2.0 template (GWT scenarios, Owner & Timeline, Technical Debt); range-binned folder structure adopted (ADR-005); `flowdoc-ciclo.md` removed from artifact table (deprecated in v2.0); `.flowforge.json` updated to split keys format (`docs_framework` + `docs_framework_version` + `upstream`); ADR-004 rewritten to document v2.0 changes, FlowForge additions, and non-adopted elements
