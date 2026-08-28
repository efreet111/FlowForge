# ADR-021 — Document indexing in Engram: topic_key namespace & upsert protocol

> **Status**: Accepted
> **Date**: 2026-08-28
> **Feature**: `index-docs-engram` (HU-022)
> **Deciders**: FlowForge methodology team
> **Links**: [HU-022](../../docs/tasks/HU-001-HU-099/HU-022-index-docs-engram.md) · [spec](../../.ai-work/index-docs-engram/spec.md) · [plan](../../.ai-work/index-docs-engram/plan.md) · [`forge-index-docs` skill](../../skills/forge-index-docs/SKILL.md) · [ADR-004](ADR-004-flowdoc-integration.md) · [ADR-018](ADR-018-flowdocs-skills-adoption.md) · [ADR-020](ADR-020-memory-observation-quality.md)

---

## Context

FlowForge uses FlowDoc v2.0 as its documentation layer ([ADR-004](ADR-004-flowdoc-integration.md)), producing PRD, ADRs, RFCs, API and DB documents. The discovery agent (HU-023) needs fast access to this information, but reading full documents consumes excessive tokens.

The proposal (HU-022): index FlowDoc documents in Engram with **reference + key content/metadata**, so agents can answer queries from the index without reading whole files.

The core open questions were:

- **OQ-1**: Scope of ADRs — product ADRs (`docs/architecture/adr/`, 2) vs methodology ADRs (`docs/decisions/`, 20)? FlowForge's most valuable decisions live in `docs/decisions/`, so scope must not be limited to the 2 product ADRs.
- **OQ-2**: Auto-update mechanism — git hook vs skill re-index vs drift-check? Git hooks add cross-OS friction.
- **OQ-3**: Where the capability lives — new `forge-*` skill vs extension of `forge-memory`?
- **OQ-4**: `topic_key` namespace confirmation.

Constraints:

- Engram's `mem_save` with the same `topic_key` performs an **upsert** (updates the existing observation, `revision_count++`, no duplicates) — this is the natural mechanism for auto-update and idempotency.
- HUs are **explicitly excluded** from indexing (AC-6, binary): `/flow-start` already saves the HU reference.
- RFC/API/DB directories do not exist yet in this repo — the mechanism must tolerate empty cases (0 docs, no error).

---

## Decision drivers

- **Idempotency**: re-running the index N times must yield exactly 1 observation per document.
- **Token efficiency**: the index must answer common queries without reading full files (NFR-005, downstream HU-023).
- **Traceability**: every index observation must point to its source file path (NFR-004).
- **Zero new infrastructure**: reuse Engram's existing upsert behavior — no git hooks, no new services.
- **Scope completeness**: methodology ADRs in `docs/decisions/` are FlowForge's most valuable knowledge; they must be indexed.
- **Empty-case tolerance**: missing directories must not fail the index run.

---

## Options considered

### Option A — Git hook for auto-update

A git hook detects document changes and re-indexes automatically.

**Pros**: Fully automatic, no agent involvement.
**Cons**: Cross-OS friction (Windows/Linux hook differences), couples documentation lifecycle to git events, hard to test. **Rejected.**

### Option B — Skill re-index on demand + drift-check (ELEGIDA)

A dedicated FlowForge skill (`forge-index-docs`) defines the indexing protocol. Re-running the skill upserts changed documents (AC-7). Drift detection compares observation date vs file mtime (FR-009). Broken references detected via file existence check (FR-010). Both are surfaced as diagnostics via `mem_doctor`, never auto-fixed.

**Pros**: Zero infrastructure, testable, agent-triggerable on demand, aligns with existing FlowForge skill architecture.
**Cons**: Requires manual/agent invocation to refresh the index; drift is detected but not auto-corrected. **Accepted trade-offs.**

### Option C — Extend `forge-memory` instead of a new skill

Add indexing protocol inside `forge-memory`.

**Pros**: No new skill file.
**Cons**: Mixes session-closure concerns with document indexing; `forge-memory` is Phase 4-only while indexing is on-demand (any phase). **Rejected.**

---

## Decision

### 1. Dedicated skill: `forge-index-docs`

A new Core FlowForge skill `skills/forge-index-docs/SKILL.md` (hardlinked to `.agents/skills/forge-index-docs/SKILL.md`) defines the indexing protocol: document discovery, content-model per type, `mem_save` call shape, HU exclusion, drift detection, broken-reference handling, and backfill.

### 2. `topic_key` namespace: `docs/{tipo}/{id}`

| Doc type | Source path | `topic_key` pattern |
|----------|-------------|---------------------|
| PRD | `docs/PRD.md` | `docs/prd` |
| Product ADR | `docs/architecture/adr/ADR-NNN-*.md` | `docs/adr/product/{id}` |
| Methodology ADR | `docs/decisions/ADR-NNN-*.md` | `docs/adr/methodology/{id}` |
| RFC | `docs/architecture/rfc/*.md` | `docs/rfc/{id}` |
| API doc | `docs/api/*.md` | `docs/api/{file}` |
| DB doc | `docs/database/*.md` | `docs/db/{file}` |
| HU | `docs/tasks/HU-001-HU-099/` | **NO INDEX** (binary) |

### 3. Content-model per type (FR-011)

- **PRD/ADR/RFC**: reference (path) + key content (semantic summary) — full file read only if the agent needs details.
- **API/DB**: metadata only (endpoint/method, table/columns) — never full content.
- **HU**: never indexed.

### 4. Upsert semantics (FR-007, NFR-001)

Indexing reuses `mem_save` + `topic_key` = upsert: same key updates the observation (`revision_count++`), never duplicates. Idempotent by construction. Backfill (FR-008) is the same protocol run over all existing documents.

### 5. Drift & broken references (FR-009, FR-010)

- **Drift**: compare observation date vs file mtime → newer file ⇒ "stale index" diagnostic, suggest re-running `forge-index-docs`. Never auto-correct.
- **Broken ref**: `Where:` path missing on filesystem ⇒ "orphaned reference" diagnostic, suggest remove/re-index. Never auto-delete.

Both surface via `mem_doctor` integration.

---

## Consequences

### Positive

- **Fast discover**: HU-023 can answer queries from index content (token-efficient) and read full files only when needed.
- **Idempotent and safe**: upsert guarantees 1 obs/doc; diagnostics never mutate the index automatically.
- **Complete scope**: all 22 ADRs (2 product + 20 methodology) + PRD indexed — the full methodology knowledge is searchable.
- **Zero new infrastructure**: pure skill + existing Engram behavior.
- **Backfill ready**: existing repos without an index can be indexed on demand.

### Negative / accepted

- **Manual/agent-driven refresh**: index updates require re-running the skill (or a future git-hook/auto-sync per OQ-5); drift is reported, not fixed.
- **Summarization quality varies**: key-content extraction depends on the LLM's judgment (spec capability_matrix acknowledges this).
- **Namespace is a contract**: future index consumers (HU-023) must respect `docs/{tipo}/{id}`; changing it later would orphan references.

### Risks

- **Risk**: agents skip re-indexing after doc edits → mitigated by drift-check diagnostics in `mem_doctor`.
- **Risk**: obs size/quality drift → mitigated by ADR-020 quality checklist (What/Why/Where/Learned, focus, title specificity).
- **Risk**: dedupe engine flags index obs against earlier summaries (observed: 3 pending conflicts on backfill) → natural consequence, resolved as `related`; expected to recur when re-indexing docs discussed before.

---

## Status History

| Date | Change |
|------|--------|
| 2026-08-28 | Accepted — initial indexing protocol with `docs/{tipo}/{id}` namespace and upsert-based auto-update |