# Context Map: Memory Observation Quality (engram-observation-quality)

> **Phase 0 — Discovery (forge-discovery)**
> **ADR**: ADR-013 Memory Observation Quality: Focus Over Size
> **Feature slug**: `engram-observation-quality`
> **Date**: 2026-07-25
> **Status**: Proposed → Context mapping complete

---

## 1. Specification Summary (from ADR-013)

ADR-013 addresses the root cause of **ENG-475** (PostgreSQL `idx_obs_dedupe` B-tree overflow). The technical fix (removing `title` from the index, PR #22) resolved the crash symptom, but the deeper problem remains: **the Memory Curation Protocol lacks guidance on WHAT and HOW agents should save observations**, resulting in oversized, multi-topic entries.

### Decision: Opción B — Quality gates + Splitting

| Aspect | Detail |
|--------|--------|
| Approach | Add quality verification to curation protocol, not hard size limits |
| Key insight | Focus over size — better to split than truncate |
| Enforcement | "Suggest, don't enforce" — agents can ignore, feedback educates |
| Non-blocking | Analysis is non-blocking; on failure, save without splitting |

### Key metrics from ENG-475
- 13 problematic observations (2133–6349 bytes content)
- 8 from `team/flowforge`, 5 from `team/engram-dotnet`
- Longest: `obs-17d0490c2f3bdee9` at 6349 bytes

---

## 2. Deliverables by Phase

### Phase 1 — Protocol update (FlowForge skills)

| # | What | File | Change type |
|---|------|------|-------------|
| 1 | Add **Paso 2b — Focus Check** after Step 2 (friction) in Memory Curation Protocol | `skills/forge-orchestrator/SKILL.md` | New section |
| 2 | Expand **Memory Signal** contract with `topics` field | `skills/forge-dev/SKILL.md` | Update |
| 3 | Expand **Memory Signal** contract with `topics` field | `skills/forge-arch/SKILL.md` | Update |
| 4 | Add **Observation Quality Checklist** before `mem_save` | `skills/forge-memory/SKILL.md` | New section |

### Phase 2 — Defensive fixes (engram-dotnet)

| # | Priority | What | File | Effort |
|---|----------|------|------|--------|
| 5 | P0 | Add `MaxTitleLength = 200` to `StoreConfig` | `src/Engram.Store/StoreConfig.cs` | 30 min |
| 6 | P0 | Truncate `title` in `AddObservationAsync` | `src/Engram.Store/PostgresStore.cs` + `SqliteStore.cs` | 30 min |
| 7 | P1 | Fix content truncation consistency in `PostgresStore` (match SqliteStore) | `src/Engram.Store/PostgresStore.cs` | 15 min |
| 8 | P2 | Add warning at 5K chars in `mem_save` | `src/Engram.Mcp/EngramTools.cs` | 20 min |

### Phase 3 — Verification

| # | Test scenario |
|---|--------------|
| 9 | Multi-topic observation → splitting suggestion |
| 10 | Generic title → specificity warning |
| 11 | Title > 200 chars → truncation with warning |
| 12 | Content > 5K chars → warning (no truncation) |

---

## 3. Relevant Prior Memories

### ADR-001 — Memory Curation Protocol (2026-05-30)
- **Path**: `docs/decisions/ADR-001-memory-curation-protocol.md`
- **Topic key**: `architecture/memory-curation-protocol`
- **Relevance**: ADR-013 is a direct extension of ADR-001. ADR-001 established:
  - The **Memory Signal** contract (3 fields: type, significance, summary)
  - The **3-step curation** process (eligible type → friction → dedup)
  - Orchestrator-centric decision model (only forge-arch and forge-dev emit signals)
  - `mem_session_summary` as mandatory safety net
- **What ADR-001 is missing** (ADR-013 fills):
  - No focus verification (multi-topic detection)
  - No length guidance by observation type
  - No title specificity validation
  - No splitting protocol

### Local memory: obs-2026-05-30-adr-001-memory-curation.md
- Observación guardada en `.engram/local_memory/` con el diseño completo de ADR-001
- Documenta las 3 opciones evaluadas y la decisión de curation centralizada

### ENG-475 ticket
- **Path**: `engram-dotnet/.ai-work/eng-475-postgres-dedupe-index-overflow/ticket.md`
- **Status**: ✅ Done (PR #22, commit `62eca98`)
- **Technical fix**: Removed `title` from `idx_obs_deduze`, added `MigrateDedupeIndex()` migration
- **Root cause**: 13 observations with 2133–6349 bytes content; titles + project + type combined exceeded PostgreSQL B-tree 2704-byte limit

---

## 4. FlowDoc Context

- **PRD**: `docs/PRD.md` — not checked (project uses FlowDoc v2.0, but this ADR was created directly)
- **HU referenced**: None — ADR-013 was proposed directly from ENG-475 incident analysis
- **docs_framework**: `flowdoc` v2.0 (per `docs/20-flowdoc-ecosystem.md`)

---

## 5. Associated Epics and Topic Keys

| Topic | Key | ADR/Feature |
|-------|-----|-------------|
| Memory Curation Protocol | `architecture/memory-curation-protocol` | ADR-001 |
| Memory Observation Quality | `architecture/memory-observation-quality` | ADR-013 (this) |
| PostgreSQL Dedup Index Fix | N/A (bug, not topic) | ENG-475, PR #22 |

ADR-013 **extends** the existing Memory Curation Protocol epic. No new epic needed.

---

## 6. Reusable Patterns Found

### Pattern 1: Content truncation in SqliteStore (CLONE)
- **File**: `src/Engram.Store/SqliteStore.cs` (lines 591-592, 754-755, 985-986)
- **What**: SqliteStore truncates content to `MaxObservationLength` (`100_000` chars) before inserting, appending `"... [truncated]"`. This is called in `AddObservationAsync`, `UpdateObservationAsync`, and `AddPromptAsync`.
- **Can clone for**: PostgresStore currently does NOT truncate content in `AddObservationAsync` (line 545-638). The same truncation pattern should be applied to PostgresStore for P1 consistency.
- **Gap**: SqliteStore truncates at 100K chars but never warns the agent. ADR-013 P2 wants a warning at 5K chars, which is a separate concern.

### Pattern 2: Truncation warning in mem_save (EXTEND)
- **File**: `src/Engram.Mcp/EngramTools.cs` (lines 198, 242-243)
- **What**: `MemSave` already checks `content.Length > store.MaxObservationLength` and emits a warning about truncation. `MemUpdate` has the same pattern (line 298-299).
- **Can extend for**: The 5K warning threshold (P2) can be added as an additional check alongside the existing `MaxObservationLength` check. The existing warning already says *"Consider splitting into smaller observations"* — P2 just needs a lower threshold for a softer warning.

### Pattern 3: Memory Signal in forge-arch and forge-dev (ADAPT)
- **Files**: `skills/forge-arch/SKILL.md` (lines 32-49), `skills/forge-dev/SKILL.md` (lines 27-46)
- **What**: Both agents emit a `## Memory Signal` block at the end of handoff with type/significance/summary. The orquestador reads this signal.
- **Can adapt for**: ADR-013 Section 2 proposes adding a `topics` field to the Memory Signal. The structural pattern (YAML-like block in markdown handoff) is already established — just add a new field.

### Pattern 4: Dedup query scans title in WHERE clause (INFORM)
- **File**: `src/Engram.Store/PostgresStore.cs` (line 585), `SqliteStore.cs` (line 651)
- **What**: Both stores compare `title` in the dedup WHERE clause. This means title length matters for query performance, not just index size.
- **Relevance**: P0 (MaxTitleLength=200) will help keep dedup queries efficient. Title is used in WHERE but no longer in the index (post-ENG-475).

### No patterns found for: semantic topic counting, focus detection, splitting protocol
- Search terms used: `"quality\|focus\|splitt\|multi.topic\|topic_count\|focus_check"`
- Result: **Negative** — neither FlowForge skills nor engram-dotnet has existing infrastructure for semantic topic counting or multi-topic detection. These are genuinely new additions.

---

## 7. Affected Files — Detailed Mapping

### FlowForge (`/mnt/.../FlowForge/`)

| File | Current state | Change needed | ADR-013 § |
|------|--------------|---------------|-----------|
| `skills/forge-orchestrator/SKILL.md` | Has Memory Curation Protocol (3 steps) | Add **Paso 2b — Focus Check** after Step 2 | §1 (Specification.1) |
| `skills/forge-dev/SKILL.md` | Has Memory Signal (type/significance/summary) | Add `topics` field to Memory Signal | §2 (Specification.2) |
| `skills/forge-arch/SKILL.md` | Has Memory Signal (type/significance/summary) | Add `topics` field to Memory Signal | §2 (Specification.2) |
| `skills/forge-memory/SKILL.md` | Has Smart Curation protocol, session close | Add **Quality Checklist** before mem_save | §3 (Specification.3) |

### engram-dotnet (`/mnt/.../engram-dotnet/`)

| File | Current state | Change needed | Priority |
|------|--------------|---------------|----------|
| `src/Engram.Store/StoreConfig.cs` | Has `MaxObservationLength = 100_000` | Add `MaxTitleLength = 200` | P0 |
| `src/Engram.Store/PostgresStore.cs` | `AddObservationAsync` (line 545) does NOT truncate content or title | 1. Truncate title to `MaxTitleLength` in AddObservationAsync<br>2. Add content truncation matching SqliteStore pattern | P0 + P1 |
| `src/Engram.Store/SqliteStore.cs` | `AddObservationAsync` (line 585) truncates content but NOT title | Truncate title to `MaxTitleLength` in AddObservationAsync | P0 |
| `src/Engram.Mcp/EngramTools.cs` | `MemSave` warns on truncation at `MaxObservationLength` (100K) | Add 5K warning threshold in `MemSave` and `MemUpdate` | P2 |

---

## 8. Dependencies and Risks

### Dependencies
- **ADR-001** must be fully understood before modifying the curation protocol (already read)
- **ENG-475 fix** (PR #22) must be deployed before Phase 2 changes (already merged, commit `62eca98`)
- FlowForge skill changes affect **all IDE adapters** (Cursor, OpenCode, VS Code, Antigravity) — these must receive thin propagation after skill changes

### Risks

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Agents ignore splitting suggestions | High | Low | Educational feedback; non-blocking by design |
| Content analysis (topic counting) is expensive in tokens | Medium | Medium | Non-blocking: if analysis fails, save without splitting |
| Title truncation removes meaningful context | Medium | Low | 200 chars is generous for almost all use cases; append `...` |
| 5K warning creates noise for legitimately long content | Medium | Low | Warning is informational, not a blocker; agent can override |
| PostgresStore content truncation parity inconsistent with SqliteStore | Low | High | Must implement same pattern; verified by test (Phase 3) |

### CVE / Security Check
- **No new CVEs introduced** by this feature
- Pre-existing: `SQLitePCLRaw.lib.e_sqlite3` 2.1.10 HIGH CVE (tracked separately, unrelated)
- Security implications: None — quality checks operate on content already being saved

---

## 9. Current Implementation Status (pre-change)

### Skills — Current Memory Signal contracts
- `forge-arch` (line 38-43): type: `decision | none`, significance: `high | low`, summary: "One line"
- `forge-dev` (line 28-36): type: `bugfix | config | pattern | none`, significance: `high | low`, summary: "One line"
- **Missing in both**: `topics: [tema1, tema2]` field

### Skills — Current Curation Protocol
- `forge-orchestrator` (lines 93-115): 3 steps (eligible type → friction → dedup in Engram)
- **Missing**: Paso 2b (focus check between fricción and dedup), Quality Checklist in forge-memory

### StoreConfig — Current limits
- `MaxObservationLength = 100_000` (line 20)
- **Missing**: `MaxTitleLength`

### PostgresStore — Current truncation behavior
- `AddObservationAsync` (lines 545-638): No truncation of title or content
- SqliteStore trunca contenido, PostgresStore no — **inconsistencia**

---

## 10. Recommendations for forge-arch

1. **Treat ADR-013 as canonical spec input** — the ADR is unusually detailed (includes exact code specs, test scenarios, and file paths). Use it directly as the spec source.
2. **Create 3 FRs** (one per phase) or 12 FRs (one per deliverable) — Phase 1 (protocol), Phase 2 (defensive), Phase 3 (tests).
3. **The `topics` field in Memory Signal is `[OPTIONAL]` by design** — orchestrator should handle missing `topics` gracefully (treat as single topic).
4. **Quality Checklist in forge-memory should be a "suggest, don't enforce" pattern** — matching the ADR-013 philosophy.
5. **Consider whether IDE adapter propagation is in scope** for this feature. ADR-013 lists it in Phase 1 item 5, but the actual adapters may be thin enough that skill changes propagate automatically.

---

**CLEAR** — context is sufficient, advance to Phase 1.
