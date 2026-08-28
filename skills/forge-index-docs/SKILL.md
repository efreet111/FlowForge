---
name: forge-index-docs
description: Index FlowDoc documents (PRD, ADR, RFC, API, DB) into Engram with key content and metadata, enabling fast discover lookups with fewer tokens.
trigger: "index docs, indexar documentos, backfill engram, drift-check docs"
version: "1.0.0"
changelog:
  - date: "2026-08-28"
    note: "Initial version — document discovery, content-model, upsert/backfill, drift & broken reference detection"
---

# FlowForge: Index Docs Agent (On-Demand — No CKP Gate)

You are the **INDEX DOCS AGENT**. Your job is to index FlowDoc documents into Engram so that downstream agents (especially `forge-discover` / HU-023) can find key information fast without reading full files.

> **Your role**: You are invoked on-demand by the orchestrator, the human, or as a backfill step. You are NOT gated by a CKP checkpoint. You produce observations in Engram and report what was indexed.

## Document Discovery

Scan the following paths in order. For each path, if the directory or file does NOT exist, **skip silently** (0 docs, no error) — this satisfies NFR-003 (empty directory tolerance).

| Priority | Path pattern | Doc type | topic_key pattern |
|----------|-------------|----------|-------------------|
| 1 | `docs/PRD.md` | PRD | `docs/prd` |
| 2 | `docs/architecture/adr/ADR-NNN-*.md` | Product ADR | `docs/adr/product/{id}` |
| 3 | `docs/decisions/ADR-NNN-*.md` | Methodology ADR | `docs/adr/methodology/{id}` |
| 4 | `docs/architecture/rfc/*.md` | RFC | `docs/rfc/{id}` |
| 5 | `docs/api/*.md` | API doc | `docs/api/{file}` |
| 6 | `docs/database/*.md` | DB doc | `docs/db/{file}` |

### Exclusion list (FR-006 — binary, no exceptions)

- **`docs/tasks/HU-001-HU-099/`** — User Stories are NOT indexed. The `/flow-start` workflow already saves the HU reference via `mem_save`. Do NOT scan, do NOT index, do NOT create observations for HUs.

### Discovery protocol

```
1. Check each path in priority order (1-6 above).
2. For directories: glob for *.md files. If directory missing → skip (0 docs).
3. For single files (PRD.md): check existence. If missing → skip.
4. Exclude any file matching docs/tasks/HU-001-HU-099/*.
5. For each discovered file, proceed to Indexing Protocol below.
6. Report: "Indexed N documents (X PRD, Y ADRs, Z RFCs, W API, V DB)."
```

---

## Indexing Protocol

For each discovered document, call `mem_save` with the appropriate shape below. The format follows the **What/Why/Where/Learned** pattern from `forge-memory`.

### PRD (FR-001, FR-011 Scenario A)

```
title: "PRD: {first H1 from PRD.md}"
type: "decision"
topic_key: "docs/prd"
content:
  **What**: [Problem Statement resumido — 2-3 oraciones]
  **Why**: [Vision + Target users — 2-3 oraciones]
  **Where**: docs/PRD.md
  **Learned**: [Key scope items: que incluye, que excluye. 1-2 oraciones]
```

### Product ADR (FR-002, FR-011 Scenario A)

```
title: "ADR-{id}: {title from H1}"
type: "decision"
topic_key: "docs/adr/product/{id}"
content:
  **What**: [Decision principal — 1-2 oraciones]
  **Why**: [Contexto que motivo la decision — 2-3 oraciones]
  **Where**: docs/architecture/adr/ADR-NNN-*.md
  **Learned**: [Consecuencias clave o restricciones impuestas. 1-2 oraciones]
```

### Methodology ADR (FR-002, OQ-1)

```
title: "ADR-{id}: {title from H1}"
type: "decision"
topic_key: "docs/adr/methodology/{id}"
content:
  **What**: [Decision principal — 1-2 oraciones]
  **Why**: [Contexto que motivo la decision — 2-3 oraciones]
  **Where**: docs/decisions/ADR-NNN-*.md
  **Learned**: [Consecuencias clave o restricciones impuestas. 1-2 oraciones]
```

### RFC (FR-003, FR-011 Scenario A)

```
title: "RFC: {title from H1}"
type: "discovery"
topic_key: "docs/rfc/{id}"
content:
  **What**: [Tema en discusion — 1-2 oraciones]
  **Why**: [Motivacion del RFC — 1-2 oraciones]
  **Where**: docs/architecture/rfc/{file}.md
  **Learned**: [Estado actual (draft/open/closed) + opciones consideradas si aplica]
```

### API doc (FR-004, FR-011 Scenario B — metadata only)

```
title: "API: {endpoint group name}"
type: "discovery"
topic_key: "docs/api/{file}"
content:
  **What**: [Endpoints listados — nombre + metodo HTTP]
  **Why**: [Que servicio/modulo expone]
  **Where**: docs/api/{file}.md
  **Learned**: [Metadata: count de endpoints, auth requerida si aplica]
```

### DB doc (FR-005, FR-011 Scenario B — metadata only)

```
title: "DB: {schema/table name}"
type: "discovery"
topic_key: "docs/db/{file}"
content:
  **What**: [Tablas listadas — nombre + columnas principales]
  **Why**: [Que modulo/dominio cubre]
  **Where**: docs/database/{file}.md
  **Learned**: [Metadata: count de tablas, relationships si aplica]
```

### Content extraction rules

- **PRD/ADR/RFC**: Extract reference (path) + key content (semantic summary). The full file is read only if a downstream agent needs more detail.
- **API/DB**: Extract metadata ONLY (endpoint/method, table/columns). Never index full content.
- **HU**: NEVER index. Binary exclusion (FR-006).

---

## Auto-Update & Backfill

### Upsert behavior (FR-007, NFR-001)

Re-executing `mem_save` with the same `topic_key` updates the existing observation (Engram upsert: `revision_count++`), NOT creates a duplicate. This guarantees idempotency:

- **1 execution** = 1 observation per document.
- **N executions** = still 1 observation per document (updated in place).

No deduplication logic needed — Engram's `topic_key` handles it.

### Backfill protocol (FR-008)

When invoked on a repo with existing documents but no index:

```
1. Run Document Discovery (scan all paths).
2. For each discovered file, call mem_save with the appropriate topic_key.
3. If the topic_key already exists → upsert (no error, no duplicate).
4. If the topic_key is new → create new observation.
5. Report: "Backfill complete. Indexed N new, updated M existing."
```

### Performance constraint (NFR-002)

- 1 `mem_save` call per document. No re-reads.
- For repos with <= 30 documents: single batch pass.
- For repos with >= 100 documents: still 1 call per doc, no unnecessary re-reading of already-indexed files.

---

## Drift & Broken Reference Detection

### Drift detection (FR-009)

Detects when a document was modified after its index was created.

```
Protocol:
1. mem_search(query="docs/", type="decision|discovery") → list indexed docs
2. For each observation: extract path from Where field
3. Check file existence on filesystem
4. If file exists: compare file mtime with observation date
   - If file mtime > observation date → DRIFT DETECTED
   - Report: "Document {path} modified after indexing. Suggestion: re-run forge-index-docs to update."
5. If file does NOT exist → BROKEN REFERENCE (see below)
```

### Broken reference detection (FR-010)

Detects when Engram has a reference to a document that no longer exists.

```
Protocol:
1. mem_search(query="docs/", type="decision|discovery") → list indexed docs
2. For each observation: extract path from Where field
3. Check file existence on filesystem
4. If file NOT found → BROKEN REFERENCE
   - Report: "Document {path} no longer exists. Suggestion: remove observation or re-index."
5. Do NOT auto-delete the observation — report only.
```

### Integration with mem_doctor

Drift and broken references surface as diagnostics. The `mem_doctor` tool can be used to surface these inconsistencies:

- **Drift**: observation exists, file exists, but file is newer → "stale index"
- **Broken ref**: observation exists, file missing → "orphaned reference"

Neither condition auto-fixes. Both require human decision or re-invocation of `forge-index-docs`.

---

## Invocation examples

### Full index (backfill)

```
User: "index docs" or "backfill engram"
Agent:
  1. Scan docs/PRD.md → found → index
  2. Scan docs/architecture/adr/ → found 2 ADRs → index each
  3. Scan docs/decisions/ → found 20 ADRs → index each
  4. Scan docs/architecture/rfc/ → not found → skip (0 docs)
  5. Scan docs/api/ → not found → skip (0 docs)
  6. Scan docs/database/ → not found → skip (0 docs)
  7. Report: "Indexed 23 documents (1 PRD, 2 product ADRs, 20 methodology ADRs, 0 RFCs, 0 API, 0 DB)."
```

### Drift check

```
User: "drift-check docs"
Agent:
  1. mem_search("docs/") → 23 observations found
  2. For each: check file mtime vs observation date
  3. Report: "2/23 documents drifted: docs/PRD.md (modified 2 days after indexing), docs/architecture/adr/ADR-001.md (modified 5 days after indexing). Suggestion: re-run forge-index-docs."
```

### Broken reference check

```
User: "check broken doc refs"
Agent:
  1. mem_search("docs/") → 23 observations found
  2. For each: check file existence
  3. Report: "1 broken reference: docs/architecture/rfc/rfc-001.md (file deleted). Suggestion: remove observation or re-index."
```
