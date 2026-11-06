# FlowForge Documentation Index

> **Last updated**: 2026-11-06
> **Framework**: [FlowDoc v2.0](20-flowdoc-ecosystem.md)
> **Purpose**: canonical map of every documentation type, its source of truth, and its status.

---

## Canonical documents

| Type | Path | Status | Notes |
|------|------|--------|-------|
| **PRD** | [`docs/PRD.md`](PRD.md) | In Review | Original product vision; implementation status tracked in the header |
| **PRD starter kit** | [`docs/PRD-starter-kit.md`](PRD-starter-kit.md) | Active | Scaffold/template PRD |
| **Project context** | [`docs/project-context.md`](project-context.md) | Active | Structural context for new contributors and agents |
| **Methodology ADRs** | [`docs/decisions/INDEX.md`](decisions/INDEX.md) | Active | 33 ADRs (ADR-001…ADR-033) |
| **Product ADRs** | [`docs/architecture/adr/INDEX.md`](architecture/adr/INDEX.md) | Active | Installer/runtime decisions |
| **RFCs** | [`docs/architecture/rfc/INDEX.md`](architecture/rfc/INDEX.md) | Empty | No RFCs yet; index is a placeholder |
| **API docs** | [`docs/endpoints.md`](endpoints.md) | N/A | FlowForge has no HTTP API surface |
| **DB schema** | [`docs/schema.md`](schema.md) | N/A | FlowForge has no database; Engram owns persistence |
| **HUs** | [`docs/tasks/HU-001-HU-099/`](tasks/HU-001-HU-099/) | Active | 28 HUs; status lives in each HU frontmatter |
| **Backlog** | [`docs/backlog/BACKLOG-MASTER.md`](backlog/BACKLOG-MASTER.md) | Active | Current snapshot; HUs + verify reports are the source of truth |
| **Roadmap** | [`docs/04-roadmap.md`](04-roadmap.md) | Historical | Snapshot; current status lives in the backlog |
| **Archive** | [`docs/archive/`](archive/) | Historical | Links into the archive may be stale by design |
| **Team reports** | [`docs/team/`](team/) | Active | Cross-project dependency and diagnostic reports |
| **PII policy** | [`docs/PII-POLICY.md`](PII-POLICY.md) | Active | Data-handling rules |
| **i18n policy** | [`docs/I18N.md`](I18N.md) | Active | Language rules for docs and prompts |
| **Documentation audit** | [`docs/audit/2026-11-06-documentation-audit.md`](audit/2026-11-06-documentation-audit.md) | Active | Last validation snapshot and open findings |

---

## Validation rules

1. **`.ai-work/` is not versioned** (ADR-002). Links into `.ai-work/` are session artifacts;
   they are valid only in a working clone that still has the local feature directory.
2. **`docs/archive/` is historical**. Broken links inside archived documents are expected and
   should not be “fixed” unless the archive is being restored.
3. **`docs/04-roadmap.md` is a historical snapshot**. Current status is tracked in
   `docs/backlog/BACKLOG-MASTER.md` and in the HU frontmatter.
4. **API/DB docs are N/A by design**. FlowForge is a CLI/installer and IDE-pack generator;
   it delegates persistence to `engram-dotnet`. If an HTTP API or local DB is added, replace
   the N/A docs with real documentation.
5. **ADR numbers are unique and sequential**. The canonical index is `docs/decisions/INDEX.md`;
   any renumbering must be recorded in its “Renumbered ADRs” table.
6. **HU status values**: `draft`, `in-progress`, `done`. The canonical status is the
   frontmatter field, not a checkbox in the body.

---

## How to audit

- Run `flowdoc-review` over `docs/` for template/format/reference checks.
- Run the link checker used in the 2026-11-06 audit to catch broken relative paths.
- Cross-check `docs/decisions/INDEX.md` against `docs/decisions/ADR-*.md`.
- Cross-check `AGENTS.md` skill rows against `skills/*/SKILL.md`.
