# Plan: Memory Observation Quality

> **Feature slug**: `engram-observation-quality`
> **Spec**: [spec.md](./spec.md)
> **ADR**: [ADR-013](../../docs/decisions/ADR-013-memory-observation-quality.md)
> **Context map**: [context-map.md](./context-map.md)
> **Status**: Ready for implementation

---

## 1. Impact and Dependencies

### Repositories affected

| Repo | Changes | Scope |
|------|---------|-------|
| **FlowForge** | 4 skill files modified (Phase 1) | Protocol documentation — no runtime code |
| **engram-dotnet** | 4 source files modified (Phase 2) | Defensive truncation + warning logic |

### Dependencies

| Dependency | Status | Risk |
|-----------|--------|------|
| ADR-001 (Memory Curation Protocol) | ✅ Understood | None — canonical reference |
| ENG-475 fix (PR #22, commit `62eca98`) | ✅ Merged | None — prerequisite deployed |
| `ide/shared/workflow-orchestrator-parity.md` | Exists | Low — auto-propagates to adapters that reference it |

### Open Questions (from spec.md §6)

| ID | Tag | Resolution |
|----|-----|-----------|
| OQ-1 | [OPTIONAL] | IDE adapters: skill changes in `skills/` are canonical. Adapters referencing `ide/shared/workflow-orchestrator-parity.md` auto-propagate. Static-copy adapters (if any) get a separate thin propagation PR. |
| OQ-2 | [OPTIONAL] | Topic counting: single-pass LLM analysis (not two-stage). Structured `## What/Why/Where/Learned` headers are counted as sections. Unstructured content gets semantic clustering. |
| OQ-3 | [FOLLOW-UP] | Metrics on splitting adherence: out of v1 scope. Future `mem_quality_metrics` tool. |

---

## 2. File Changes (Proposed Changes)

### Phase 1 — Protocol Updates (FlowForge)

- [MODIFY] `skills/forge-orchestrator/SKILL.md` (line ~95)
  - Insert **Paso 2b — Focus Check** between STEP 2 and STEP 3 of the Memory Curation Protocol
  - Reference `ide/shared/workflow-orchestrator-parity.md` for canonical process

- [MODIFY] `skills/forge-arch/SKILL.md` (lines 38-43)
  - Add `topics` field to Memory Signal block
  - Add title specificity guidance (FR-004)

- [MODIFY] `skills/forge-dev/SKILL.md` (lines 32-36)
  - Add `topics` field to Memory Signal block
  - Add title specificity guidance (FR-004)

- [MODIFY] `skills/forge-memory/SKILL.md` (after line ~121, before `mem_save` call)
  - Add **Observation Quality Checklist** section
  - Include type-specific size expectations table (FR-005)
  - Include title specificity rule (FR-004)

- [MODIFY] `ide/shared/workflow-orchestrator-parity.md`
  - Mirror the Paso 2b addition from forge-orchestrator (parity file)

### Phase 2 — Defensive Fixes (engram-dotnet)

- [MODIFY] `src/Engram.Store/StoreConfig.cs` (after line ~20)
  - Add `MaxTitleLength` property, default = 200

- [MODIFY] `src/Engram.Store/PostgresStore.cs` (AddObservationAsync ~line 545, UpdateObservationAsync, AddPromptAsync)
  - Add title truncation to `MaxTitleLength` in `AddObservationAsync`
  - Add content truncation matching SqliteStore pattern in `AddObservationAsync`, `UpdateObservationAsync`, `AddPromptAsync`

- [MODIFY] `src/Engram.Store/SqliteStore.cs` (AddObservationAsync ~line 585)
  - Add title truncation to `MaxTitleLength` in `AddObservationAsync`

- [MODIFY] `src/Engram.Mcp/EngramTools.cs` (MemSave ~line 198, MemUpdate ~line 298)
  - Add 5K warning threshold check before existing `MaxObservationLength` check

### Phase 3 — Tests (engram-dotnet)

- [NEW/MODIFY] `test/Engram.Store.Tests/PostgresStoreTests.cs`
  - Title truncation tests (P0)
  - Content truncation parity tests (P1)

- [NEW/MODIFY] `test/Engram.Store.Tests/SqliteStoreTests.cs`
  - Title truncation tests (P0)

- [NEW/MODIFY] `test/Engram.Mcp.Tests/EngramToolsTests.cs`
  - 5K warning threshold tests (P2)

---

## 3. Contracts and Schemas

### 3.1 Memory Signal Contract (v2 — expanded)

```markdown
## Memory Signal
- type: decision | bugfix | config | pattern | none
- significance: high | low
- summary: "Título específico y buscable (no 'bug fix' o 'update')"
- topics: [tema1, tema2]   # OPTIONAL — orchestrator treats missing as single-topic
```

**Backward compatibility**: If `topics` is absent, orchestrator defaults to single-topic behavior and uses content text for Paso 2b analysis.

### 3.2 Paso 2b — Focus Check Algorithm

```
PASO 2b — ¿Está enfocado?
  Leer topics[] del Memory Signal (si existe)
  Analizar contenido para contar temas distintos (LLM single-pass)
  SI > 3 temas:
    → SUGERIR división con lista numerada de temas
    → SI agente confirma split: guardar cada tema por separado
    → SI agente confirma single: proceder (humano override)
  SI ≤ 3 temas:
    → CONTINUAR al PASO 3
  SI análisis falla:
    → Non-blocking: continuar al PASO 3 sin sugerencia
```

### 3.3 Observation Quality Checklist

```markdown
## Observation Quality Checklist

Antes de guardar, verificar:
- [ ] Enfocado en UN tema (no 3+ temas mezclados)
- [ ] Título específico (no "bug fix" o "update")
- [ ] Estructura completa (What/Why/Where/Learned)
- [ ] Lección o decisión actionable
- [ ] Tamaño apropiado para el tipo:
  - decision: 500-1500 chars ideal (máx 2000)
  - bugfix: 800-2000 chars ideal (máx 3000)
  - pattern: 1000-2500 chars ideal (máx 3000)
  - config: 3000-8000 chars aceptable (máx 5000)
  - session_summary: 2000-5000 chars ideal (máx 8000)

Si cubre múltiples temas → dividir en observaciones separadas.
Si algún check no se puede evaluar → "Quality analysis incomplete", proceed without blocking.
```

### 3.4 Title Specificity Rule

| ❌ Generic (rejected) | ✅ Specific (accepted) |
|-----------------------|------------------------|
| "Bug fix" | "JWT refresh token rotation prevents replay attacks" |
| "Update" | "Removed title from idx_obs_dedupe to prevent B-tree overflow" |
| "Change" | "Switched from sessions to JWT for stateless auth" |
| "Config" | "PostgreSQL connection pool set to 100 for production load" |
| "Fix" | "NullReferenceException in AuthMiddleware when token is expired" |

Pattern: **"What was the problem/change + what was the resolution/outcome"**

### 3.5 StoreConfig — New Property

```csharp
// StoreConfig.cs — new property
public int MaxTitleLength { get; } = 200;

// Existing (unchanged):
public int MaxObservationLength { get; } = 100_000;
```

### 3.6 Title Truncation Contract (P0 — FR-006)

```csharp
// In AddObservationAsync (both PostgresStore and SqliteStore):
if (title.Length > storeConfig.MaxTitleLength)
{
    var original = title;
    title = title[..storeConfig.MaxTitleLength] + "…";
    logger.LogWarning(
        "Title truncated from {OriginalLength} chars to {Max}. Original: {Preview}…",
        original.Length, storeConfig.MaxTitleLength, original[..100]);
}
```

### 3.7 Content Truncation Contract (P1 — FR-007)

```csharp
// In PostgresStore — match SqliteStore pattern exactly:
// Apply in AddObservationAsync, UpdateObservationAsync, AddPromptAsync
if (content.Length > storeConfig.MaxObservationLength)
{
    content = content[..storeConfig.MaxObservationLength] + "... [truncated]";
    logger.LogWarning(
        "Content truncated from {OriginalLength} to {Max} chars.",
        originalContent.Length, storeConfig.MaxObservationLength);
}
```

### 3.8 5K Warning Threshold Contract (P2 — FR-008)

```csharp
// In EngramTools.cs — MemSave and MemUpdate methods
// BEFORE the existing MaxObservationLength check:
if (content.Length > 5_000)
{
    warning = $"Content is {content.Length} chars (>5,000 threshold). " +
              "Consider splitting into multiple focused observations for better " +
              "searchability and maintainability. Save will proceed.";
}

// EXISTING check remains (additive, not replacement):
if (content.Length > store.MaxObservationLength)
{
    // ... existing truncation warning + behavior
}
```

---

## 4. Implementation Checklist

### Phase 1 — Protocol Updates (FlowForge skills)

> **No code changes.** Documentation/protocol updates to skill markdown files.

- [ ] **T-001**: Add Paso 2b — Focus Check to `forge-orchestrator/SKILL.md` (FR-001)
  - **File**: `skills/forge-orchestrator/SKILL.md` (after line 95)
  - **Also mirror in**: `ide/shared/workflow-orchestrator-parity.md`
  - **Change**: Insert new `### Paso 2b — ¿Está enfocado?` section between STEP 2 (fricción) and STEP 3 (dedup). Include:
    - Algorithm pseudocode from §3.2 above
    - >3 topics → suggest split with numbered list
    - ≤3 topics → continue
    - Analysis failure → non-blocking, continue
  - **Dependencies**: None
  - **Effort**: Small (30 min)
  - **Acceptance criteria**:
    - Paso 2b section exists between Step 2 and Step 3
    - Algorithm includes >3 topic threshold, split suggestion, and non-blocking fallback
    - Backward compatibility: mentions that missing `topics` field defaults to single-topic

- [ ] **T-002**: Add `topics` field to Memory Signal in `forge-dev/SKILL.md` (FR-002)
  - **File**: `skills/forge-dev/SKILL.md` (lines 32-36)
  - **Change**: Expand the Memory Signal block from 3 fields to 4 fields:
    ```markdown
    ## Memory Signal
    - type: bugfix | config | pattern | none
    - significance: high | low
    - summary: "Título específico y buscable (no 'bug fix' o 'update')"
    - topics: [tema1, tema2]
    ```
  - **Also add**: Title specificity guidance (examples from §3.4)
  - **Also add**: Note that `topics` is OPTIONAL — orchestrator handles missing gracefully
  - **Dependencies**: None
  - **Effort**: Small (15 min)
  - **Acceptance criteria**:
    - Memory Signal block has 4 fields including `topics`
    - `topics` marked as optional
    - Title specificity examples included
    - Existing `type`/`significance`/`summary` rules unchanged

- [ ] **T-003**: Add `topics` field to Memory Signal in `forge-arch/SKILL.md` (FR-002)
  - **File**: `skills/forge-arch/SKILL.md` (lines 38-43)
  - **Change**: Same expansion as T-002 but for forge-arch's signal format:
    ```markdown
    ## Memory Signal
    - type: decision | none
    - significance: high | low
    - summary: "Título específico y buscable (no 'bug fix' o 'update')"
    - topics: [tema1, tema2]
    ```
  - **Also add**: Title specificity guidance (examples from §3.4)
  - **Dependencies**: None (parallel with T-002)
  - **Effort**: Small (15 min)
  - **Acceptance criteria**:
    - Memory Signal block has 4 fields including `topics`
    - `topics` marked as optional
    - Title specificity examples included
    - forge-arch-specific rules (type: decision|none) unchanged

- [ ] **T-004**: Add Observation Quality Checklist to `forge-memory/SKILL.md` (FR-003, FR-004, FR-005)
  - **File**: `skills/forge-memory/SKILL.md` (insert before `mem_save` call in Smart Curation section, around line 121)
  - **Change**: Add new section `## Observation Quality Checklist` containing:
    - The full checklist from §3.3 above
    - Type-specific size expectations table from FR-005
    - Title specificity rule from FR-004 (table from §3.4)
    - Non-blocking clause: "If any check cannot be evaluated → proceed without blocking"
  - **Placement**: After Smart Curation step 3 (Consolidation) and before step 4 (Organised Ingestion / `mem_save`)
  - **Dependencies**: None
  - **Effort**: Medium (45 min)
  - **Acceptance criteria**:
    - Quality Checklist section exists before `mem_save` calls
    - All 5 checklist items present (focus, title, structure, actionable, size)
    - Type-specific size table with all 5 types (decision, bugfix, pattern, config, session_summary)
    - Title specificity examples included
    - Non-blocking clause explicitly stated
    - Applies to both mid-session and session-close processing

### Phase 2 — Defensive Fixes (engram-dotnet)

> **Code changes in separate repository.** Must compile and pass existing tests.

- [ ] **T-005**: Implement MaxTitleLength + title truncation in both stores (FR-006, P0)
  - **Files**:
    - `src/Engram.Store/StoreConfig.cs` — add `MaxTitleLength` property
    - `src/Engram.Store/PostgresStore.cs` — add title truncation in `AddObservationAsync`
    - `src/Engram.Store/SqliteStore.cs` — add title truncation in `AddObservationAsync`
  - **Changes**:
    1. `StoreConfig.cs`: Add `public int MaxTitleLength { get; } = 200;` (immutable, after `MaxObservationLength`)
    2. `PostgresStore.AddObservationAsync`: Before DB insert, check `title.Length > MaxTitleLength`. If so, truncate to 200 chars + append `"…"`. Log warning with original length and first 100 chars preview.
    3. `SqliteStore.AddObservationAsync`: Same truncation logic as PostgresStore.
  - **Security**: RNF-SEC-001 — truncated titles MUST append `"…"` so data loss is visible. RNF-SEC-003 — log preview must NOT exceed 200 chars.
  - **Dependencies**: None
  - **Effort**: Medium (45 min)
  - **Acceptance criteria**:
    - `StoreConfig.MaxTitleLength` defaults to 200
    - Title >200 chars → truncated to 200 + `"…"` (total 201 chars)
    - Title ≤200 chars → saved verbatim, no warning
    - Warning logged on truncation with original length and preview
    - Both PostgresStore and SqliteStore behave identically (NFR-004)

- [ ] **T-006**: Implement PostgresStore content truncation parity (FR-007, P1)
  - **File**: `src/Engram.Store/PostgresStore.cs`
  - **Methods**: `AddObservationAsync`, `UpdateObservationAsync`, `AddPromptAsync`
  - **Changes**: Clone the truncation pattern from `SqliteStore` (lines 591-592, 754-755, 985-986):
    1. Before DB insert/update, check `content.Length > MaxObservationLength` (100,000)
    2. If exceeded, truncate to `MaxObservationLength` + append `"... [truncated]"`
    3. Log warning
  - **Security**: RNF-SEC-001 — truncated content MUST append `"... [truncated]"` so data loss is visible.
  - **Dependencies**: T-005 (uses `StoreConfig.MaxObservationLength` — already exists)
  - **Effort**: Small (30 min)
  - **Acceptance criteria**:
    - PostgresStore content truncation matches SqliteStore exactly (NFR-004)
    - All 3 methods covered: Add, Update, AddPrompt
    - Content ≤100K → saved verbatim
    - Content >100K → truncated to 100K + `"... [truncated]"`
    - Warning logged on truncation

- [ ] **T-007**: Implement 5K warning threshold in mem_save/mem_update (FR-008, P2)
  - **File**: `src/Engram.Mcp/EngramTools.cs`
  - **Methods**: `MemSave` (~line 198), `MemUpdate` (~line 298)
  - **Changes**: Add a new check BEFORE the existing `MaxObservationLength` check:
    ```csharp
    // Additive warning — does NOT truncate, just alerts
    if (content.Length > 5_000)
    {
        // Append warning to response/message
        $"Content is {content.Length} chars (>5,000 threshold). " +
        "Consider splitting into multiple focused observations for better " +
        "searchability and maintainability. Save will proceed."
    }
    ```
  - **Behavior**: Warning is returned to the agent. Content is NOT truncated. Existing `MaxObservationLength` truncation remains unchanged (additive, not replacement).
  - **Dependencies**: None
  - **Effort**: Small (20 min)
  - **Acceptance criteria**:
    - Content >5K → warning returned to agent, save proceeds without truncation
    - Content ≤5K → no warning
    - Content >5K AND >100K → BOTH warnings emitted (5K soft + 100K truncation)
    - Applied to both `MemSave` and `MemUpdate`

### Phase 3 — Tests (engram-dotnet)

> **Verification tests for all defensive fixes.**

- [ ] **T-008**: Write tests for P0 — title truncation (FR-006)
  - **Files**: `test/Engram.Store.Tests/PostgresStoreTests.cs`, `test/Engram.Store.Tests/SqliteStoreTests.cs`
  - **Test cases**:
    1. Title at 150 chars → saved verbatim (no truncation, no warning)
    2. Title at exactly 200 chars → saved verbatim (boundary)
    3. Title at 250 chars → truncated to 200 + `"…"` (total 201 chars)
    4. Title at 500 chars → truncated to 200 + `"…"` with warning logged
    5. `StoreConfig.MaxTitleLength` defaults to 200
  - **Dependencies**: T-005
  - **Effort**: Small (30 min)
  - **Acceptance criteria**: All 5 test cases pass for both PostgresStore and SqliteStore

- [ ] **T-009**: Write tests for P1 — PostgresStore content truncation parity (FR-007)
  - **File**: `test/Engram.Store.Tests/PostgresStoreTests.cs`
  - **Test cases**:
    1. Content at 85K chars → saved verbatim in PostgresStore
    2. Content at 120K chars → truncated to 100K + `"... [truncated]"` in PostgresStore
    3. Parity test: same content saved to both stores produces identical truncation behavior
    4. `UpdateObservationAsync` truncates content >100K
    5. `AddPromptAsync` truncates content >100K
  - **Dependencies**: T-006
  - **Effort**: Small (30 min)
  - **Acceptance criteria**: All 5 test cases pass. PostgresStore behavior matches SqliteStore exactly.

- [ ] **T-010**: Write tests for P2 — 5K warning threshold (FR-008)
  - **File**: `test/Engram.Mcp.Tests/EngramToolsTests.cs`
  - **Test cases**:
    1. Content at 3,500 chars → no 5K warning returned
    2. Content at 6,200 chars → 5K warning returned, content saved as-is (no truncation)
    3. Content at exactly 5,000 chars → no warning (boundary: ≤5K)
    4. Content at 5,001 chars → warning returned (boundary: >5K)
    5. Content at 105K chars → BOTH 5K warning AND truncation warning emitted
    6. Same tests for `MemUpdate`
  - **Dependencies**: T-007
  - **Effort**: Small (30 min)
  - **Acceptance criteria**: All 6 test cases pass. Warning message contains ">5,000 threshold" and "Consider splitting".

### Phase 4 — IDE Adapter Propagation (if needed)

- [ ] **T-011**: Verify and propagate IDE adapter changes (OQ-1)
  - **Scope**: Check if `ide/shared/workflow-orchestrator-parity.md` auto-propagates to all adapters
  - **Files to check**:
    - `ide/cursor/` — Cursor agent files (pre-compiled, may need manual update)
    - `ide/opencode/` — OpenCode agent files
    - `ide/vscode/` — VS Code Copilot files
    - `ide/antigravity/` — Antigravity files
  - **Changes**: If adapters have static copies of the Memory Curation Protocol, update them to include Paso 2b. If they reference the parity file, verify the parity file was updated in T-001.
  - **Dependencies**: T-001
  - **Effort**: Small (15 min) — may be a no-op if parity file handles propagation
  - **Acceptance criteria**: All IDE adapters reflect the updated Memory Curation Protocol with Paso 2b

---

## 5. Implementation Order (Topological)

```
Phase 1 (no code, protocol only — parallel):
  T-001 ─────┐
  T-002 ─────┤ (all independent, can be done in parallel)
  T-003 ─────┤
  T-004 ─────┘
       │
       ▼
Phase 2 (code changes — sequential by dependency):
  T-005 (P0: MaxTitleLength) ─── can start immediately
  T-006 (P1: PostgresStore) ─── depends on T-005 (uses StoreConfig)
  T-007 (P2: 5K warning) ────── independent of T-005/T-006
       │
       ▼
Phase 3 (tests — after corresponding Phase 2 tasks):
  T-008 (test P0) ─── depends on T-005
  T-009 (test P1) ─── depends on T-006
  T-010 (test P2) ─── depends on T-007
       │
       ▼
Phase 4 (propagation — after Phase 1):
  T-011 (IDE adapters) ─── depends on T-001
```

**Recommended execution sequence**:
1. T-001 → T-002 → T-003 → T-004 (Phase 1, can be parallel)
2. T-005 → T-006 (sequential) | T-007 (parallel with T-006)
3. T-008 → T-009 → T-010 (after respective Phase 2 tasks)
4. T-011 (after Phase 1 is complete)

---

## 6. Effort Summary

| Task | FR | Phase | Effort | Dependencies |
|------|----|-------|--------|-------------|
| T-001 | FR-001 | 1 | Small (30 min) | None |
| T-002 | FR-002 | 1 | Small (15 min) | None |
| T-003 | FR-002 | 1 | Small (15 min) | None |
| T-004 | FR-003/004/005 | 1 | Medium (45 min) | None |
| T-005 | FR-006 | 2 | Medium (45 min) | None |
| T-006 | FR-007 | 2 | Small (30 min) | T-005 |
| T-007 | FR-008 | 2 | Small (20 min) | None |
| T-008 | FR-006 | 3 | Small (30 min) | T-005 |
| T-009 | FR-007 | 3 | Small (30 min) | T-006 |
| T-010 | FR-008 | 3 | Small (30 min) | T-007 |
| T-011 | OQ-1 | 4 | Small (15 min) | T-001 |
| **Total** | | | **~5.25 hours** | |

---

## 7. Risks and Mitigations

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Agents ignore splitting suggestions | High | Low | Non-blocking by design; educational feedback improves behavior over time |
| LLM topic counting is expensive in tokens | Medium | Medium | Single-pass analysis (OQ-2 resolution); max 2K additional tokens per observation (NFR-003) |
| Title truncation at 200 chars removes meaningful context | Low | Low | 200 chars is generous; `"…"` marker makes truncation visible |
| 5K warning creates noise for legitimately long content | Medium | Low | Warning is informational only; agent can proceed without action |
| PostgresStore content truncation diverges from SqliteStore | Low | High | Clone exact pattern from SqliteStore; parity test (T-009) verifies |
| IDE adapters have stale static copies of curation protocol | Medium | Medium | T-011 checks all adapters; parity file mechanism auto-propagates where supported |

---

## 8. Verification Matrix (FR → Task → Test)

| FR | Task | Test | PM-* |
|----|------|------|------|
| FR-001 (Paso 2b focus check) | T-001 | PM-1 (manual) | PM-1 |
| FR-002 (topics field) | T-002, T-003 | Verified by PM-1 (orchestrator reads topics) | — |
| FR-003 (Quality Checklist) | T-004 | PM-4 (manual) | PM-4 |
| FR-004 (Title specificity) | T-002, T-003, T-004 | Covered by PM-1 and PM-4 | — |
| FR-005 (Size expectations) | T-004 | Covered by PM-4 | — |
| FR-006 (MaxTitleLength P0) | T-005 | T-008 (automated) | PM-2 |
| FR-007 (PostgresStore P1) | T-006 | T-009 (automated) | — |
| FR-008 (5K warning P2) | T-007 | T-010 (automated) | PM-3 |

---

## 9. Implementation Checklist (forge-dev marks [x])

### Phase 1 — Protocol Updates (FlowForge)
- [x] T-001: Add Paso 2b — Focus Check to `forge-orchestrator/SKILL.md` + parity file
- [x] T-002: Add `topics` field to Memory Signal in `forge-dev/SKILL.md`
- [x] T-003: Add `topics` field to Memory Signal in `forge-arch/SKILL.md`
- [x] T-004: Add Quality Checklist to `forge-memory/SKILL.md`

### Phase 2 — Defensive Fixes (engram-dotnet)
- [x] T-005: Implement MaxTitleLength + title truncation (P0)
- [x] T-006: Implement PostgresStore content truncation parity (P1)
- [x] T-007: Implement 5K warning threshold in mem_save/mem_update (P2)

### Phase 3 — Tests (engram-dotnet)
- [x] T-008: Write tests for title truncation (P0)
- [x] T-009: Write tests for PostgresStore content truncation parity (P1)
- [x] T-010: Write tests for 5K warning threshold (P2)

### Phase 4 — IDE Adapter Propagation
- [x] T-011: Verify and propagate IDE adapter changes

---

## 10. Definition of Done

All items below must be true before marking this feature complete:

1. ✅ All Phase 1 tasks (T-001 → T-004) complete — skill files updated
2. ✅ All Phase 2 tasks (T-005 → T-007) complete — engram-dotnet code compiles
3. ✅ All Phase 3 tests (T-008 → T-010) pass — `dotnet test` green
4. ✅ Phase 4 (T-011) verified — IDE adapters checked
5. ✅ PM-1 through PM-4 manual tests executed and marked in spec.md §5
6. ✅ NFR-004 (store parity) verified by T-009 parity test
7. ✅ RNF-SEC-001 (truncation markers) verified by T-008 and T-009
8. ✅ RNF-SEC-003 (no secrets in logs >200 chars) verified by code review
