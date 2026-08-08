---
flowforge_slug: engram-observation-quality
status: proposed
capability_matrix:
  ai_reasoning:
    - "FR-001: Topic counting and focus detection in Paso 2b — LLM analyzes observation content for semantic topics, counts distinct themes"
    - "FR-003: Quality checklist evaluation in forge-memory — LLM applies judgment on focus, specificity, structure, and actionability"
    - "FR-004: Title specificity evaluation — LLM judges whether title is generic ('bug fix') or specific ('JWT refresh token rotation prevents replay attacks')"
    - "FR-005: Size-appropriateness guidance — LLM evaluates content length against type-specific ideal/maximum thresholds and suggests splitting when over"
  deterministic:
    - "FR-002: Memory Signal `topics` field structure — must be a YAML array; non-null; orchestrator must handle missing `topics` gracefully (treat as single topic)"
    - "FR-003: Quality Checklist is mandatory structure — forge-memory MUST emit the checklist block before any mem_save call"
    - "FR-005: Type-specific maximum-before-splitting table — fixed values (decision: 2000, bugfix: 3000, pattern: 3000, config: 5000, session_summary: 8000)"
    - "FR-006: MaxTitleLength = 200 chars — hard truncation in AddObservationAsync; both PostgresStore and SqliteStore; append '...' on truncation"
    - "FR-007: MaxObservationLength = 100_000 chars — content truncation with '[truncated]' marker; PostgresStore must match SqliteStore pattern"
    - "FR-008: 5K warning threshold in mem_save — deterministic length check; content > 5000 chars triggers non-blocking warning"
    - "Paso 2b trigger threshold: >3 distinct topics → split suggestion; ≤3 → continue (deterministic count threshold)"
    - "All quality checks (focus, specificity, size) are NON-BLOCKING — on analysis failure, save without splitting"
---
# Spec: Memory Observation Quality

> **ADR source**: [ADR-013 — Memory Observation Quality: Focus Over Size](../../docs/decisions/ADR-013-memory-observation-quality.md)
> **Incident**: ENG-475 — PostgreSQL `idx_obs_dedupe` B-tree index overflow
> **Extends**: [ADR-001 — Orchestrator Memory Curation Protocol](../../docs/decisions/ADR-001-memory-curation-protocol.md)
> **Context map**: [context-map.md](./context-map.md)

---

## 1. Objective and Scope

### Problem Statement (ENG-475 Root Cause)

On 2026-07-24, `forge-verify` of engram-dotnet sync discovered that 13 observations caused PostgreSQL B-tree index overflow:

```
PostgresException: 54000: index row size 2800 exceeds btree version 4 maximum 2704
```

The immediate fix (PR #22, removing `title` from `idx_obs_dedupe`) resolved the crash symptom. However, root cause analysis revealed a deeper quality problem: **the Memory Curation Protocol (ADR-001) provides no guidance on WHAT or HOW agents should save observations**, resulting in oversized entries mixing multiple topics — the longest at 6349 bytes covering 5 distinct themes in a single observation.

### What This Feature Solves

**Enhance the Memory Curation Protocol with quality gates — focus over size.** Instead of adding hard truncation limits that degrade quality, add:

1. **Focus check** (Paso 2b): detect multi-topic observations and suggest splitting
2. **Expanded Memory Signal**: add `topics` field so the orchestrator knows what an observation covers
3. **Quality checklist** in `forge-memory`: validate focus, specificity, and structure before `mem_save`
4. **Title specificity rules**: reject generic titles like "bug fix"
5. **Type-specific size expectations**: guide agents on appropriate content length per observation type
6. **Defensive fixes in engram-dotnet** (safety net): title truncation at 200 chars (P0), PostgresStore content truncation parity (P1), 5K warning threshold in `mem_save` (P2)

### Scope Boundaries

| IN scope | OUT of scope |
|----------|-------------|
| Update FlowForge skills (orchestrator, dev, arch, memory) | Adding new MCP tools to engram-dotnet |
| Add P0/P1/P2 defensive fixes to engram-dotnet | Semantic content summarization or auto-summarization |
| Define quality guidelines (suggest, don't enforce) | Hard enforcement that blocks saves |
| Backwards-compatible Memory Signal extension | Breaking changes to existing Memory Signal format |
| Given-When-Then acceptance scenarios | Code implementation or production code |

---

## 2. Functional Requirements (FR)

### FR-001: Focus Check — Paso 2b in Memory Curation Protocol

Add a new step to the Memory Curation Protocol (between current Step 2 "friction" and Step 3 "already in Engram?") that detects multi-topic observations and suggests splitting.

**Location**: `skills/forge-orchestrator/SKILL.md` — Memory Curation Protocol section.

**Algorithm** (LLM-based):
```
PASO 2b — ¿Está enfocado?
  Contar temas distintos en el contenido (análisis semántico o ## headers)
  SI > 3 temas:
    → SUGERIR división en observaciones enfocadas con lista de temas
    → SI el agente confirma split: guardar cada tema por separado
    → SI el agente confirma single: proceder (humano override)
  SI ≤ 3 temas:
    → CONTINUAR al PASO 3
```

- **Scenario A — Multi-topic observation triggers split suggestion**:
  **Given** an agent emits a Memory Signal with an observation whose content contains 5 distinct topics (e.g., "JWT auth", "PostgreSQL pool size", "Redis caching", "Docker compose fix", "Test flakiness")
  **When** the orchestrator executes Paso 2b — focus check
  **Then** the orchestrator identifies >3 topics, suggests splitting with a numbered list of each topic, and asks the agent to confirm: split into 5 individual observations OR proceed as single (human override)

- **Scenario B — Focused observation passes focus check**:
  **Given** an agent emits a Memory Signal with an observation focused on 2 closely related topics (e.g., "Docker networking" and "container DNS resolution")
  **When** the orchestrator executes Paso 2b — focus check
  **Then** the orchestrator counts ≤3 topics, proceeds to Paso 3 (dedup check), and does NOT suggest splitting

- **Scenario C — Content analysis fails gracefully**:
  **Given** an agent emits a Memory Signal, but the LLM's topic counting fails (unexpected content format, empty content, parsing error)
  **When** the orchestrator executes Paso 2b
  **Then** the orchestrator treats the analysis as non-blocking, logs the failure, and continues to Paso 3 without suggesting splitting (default: save as-is)

---

### FR-002: Memory Signal Expanded with `topics` Field

Expand the Memory Signal contract emitted by `forge-arch` and `forge-dev` to include a `topics` field listing the distinct themes covered by the observation.

**Locations**: `skills/forge-arch/SKILL.md`, `skills/forge-dev/SKILL.md`.

**New contract**:
```markdown
## Memory Signal
- type: decision | bugfix | config | pattern | none
- significance: high | low
- summary: "Título específico y buscable"
- topics: [tema1, tema2]
```

- **Scenario A — Agent emits signal with topics field**:
  **Given** `forge-arch` completes a handoff for a feature that introduced two architectural decisions (e.g., "stateless JWT auth" and "event-driven sync")
  **When** the agent emits the Memory Signal block
  **Then** the signal includes `topics: [JWT-auth, event-sync]`, and the orchestrator reads the topics to inform Paso 2b focus analysis

- **Scenario B — Legacy signal without topics field (backward compatibility)**:
  **Given** an agent (or legacy skill version) emits a Memory Signal with only `type`, `significance`, and `summary` — no `topics` field
  **When** the orchestrator reads the Memory Signal
  **Then** the orchestrator treats `topics` as missing/empty, defaults to single-topic behavior, and proceeds with Paso 2b using only the content text for topic analysis

- **Scenario C — Single-topic observation**:
  **Given** `forge-dev` fixes a single bug with a focused scope
  **When** the agent emits the Memory Signal block
  **Then** `topics: [sql-connection-leak]` is valid with exactly 1 topic; the orchestrator identifies ≤3 topics and continues without split suggestion

---

### FR-003: Quality Checklist in forge-memory

Add a mandatory Observation Quality Checklist to `forge-memory`'s Smart Curation protocol, executed before any `mem_save` call (both mid-session and session-close processing).

**Location**: `skills/forge-memory/SKILL.md` — after Smart Curation section, before `mem_save`.

**Checklist**:
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
  - config: 3000-8000 chars aceptable (máx 5000)
  - session_summary: 2000-5000 chars ideal (máx 8000)

Si cubre múltiples temas → dividir en observaciones separadas.
```

- **Scenario A — forge-memory validates observation quality before saving**:
  **Given** `forge-memory` is processing a local buffer observation or a handoff from another agent
  **When** about to call `mem_save` for a `decision`-type observation of 1800 chars with a specific title "ADR-013 quality gates adopted for memory curation"
  **Then** `forge-memory` runs the Quality Checklist: focus ✓ (single topic), specificity ✓ (specific title), structure ✓ (What/Why/Where/Learned), size ✓ (1800 within 500-2000 range). All items pass. Proceed to `mem_save`.

- **Scenario B — Observation fails checklist — suggest improvements**:
  **Given** `forge-memory` processes a local buffer observation titled "Bug fix" with content mixing database migration issues AND API rate limiting AND Docker networking — 4200 chars
  **When** about to call `mem_save`
  **Then** the Quality Checklist flags: ✗ multi-topic (≥3 themes), ✗ generic title, ✗ size over type maximum (bugfix max 3000). `forge-memory` suggests: split into 3 focused observations with specific titles, reduce each to type-appropriate length. The suggestion is non-blocking — if the agent confirms "save as-is", proceed.

- **Scenario C — Checklist execution is non-blocking**:
  **Given** `forge-memory` encounters an observation where the quality analysis is indeterminate (e.g., ambiguous topic boundaries)
  **When** the checklist cannot be fully evaluated
  **Then** `forge-memory` notes "Quality analysis incomplete" and proceeds with `mem_save` without blocking (suggest, don't enforce pattern)

---

### FR-004: Title Specificity Rule

Define and enforce a rule that observation titles must be specific enough for effective `mem_search` retrieval — not generic placeholders.

**Location**: Referenced in `forge-arch/SKILL.md`, `forge-dev/SKILL.md`, `forge-memory/SKILL.md`.

**Rule table**:

| ❌ Generic (rejected) | ✅ Specific (accepted) |
|-----------------------|------------------------|
| "Bug fix" | "JWT refresh token rotation prevents replay attacks" |
| "Update" | "Removed title from idx_obs_dedupe to prevent B-tree overflow" |
| "Change" | "Switched from sessions to JWT for stateless auth" |
| "Config" | "PostgreSQL connection pool set to 100 for production load" |
| "Fix" | "NullReferenceException in AuthMiddleware when token is expired" |

- **Scenario A — Generic title triggers specificity warning**:
  **Given** an agent emits `summary: "Bug fix"` in the Memory Signal or forge-memory encounters a local buffer observation titled "Update"
  **When** the orchestrator (Paso 2b) or forge-memory (Quality Checklist) evaluates the title
  **Then** the system flags it as non-specific and suggests a replacement following the pattern: "What was the problem/change + what was the resolution/outcome". Example: instead of "Bug fix", use "JWT token refresh now rotates with each use to prevent replay"

- **Scenario B — Specific title passes validation**:
  **Given** an agent emits `summary: "ADR-013 quality gates adopted for memory curation protocol"`
  **When** title specificity is checked
  **Then** the title is accepted (specific, descriptive, searchable); no warning is emitted

- **Scenario C — Title is borderline — agent decides**:
  **Given** an agent emits `summary: "Memory Curation update"` (somewhat specific but not fully descriptive)
  **When** the orchestrator evaluates the title
  **Then** a soft suggestion is emitted recommending more detail, but the save proceeds without blocking if the agent confirms

---

### FR-005: Type-Specific Size Expectations

Define size expectations per observation type to guide agents on appropriate content length. These are GUIDELINES, not hard limits.

| Type | Ideal range (chars) | Maximum before splitting | Behavior when exceeded |
|------|--------------------|--------------------------|------------------------|
| `decision` | 500 – 1,500 | 2,000 | Warning: "Consider splitting or condensing" |
| `bugfix` | 800 – 2,000 | 3,000 | Warning: "Consider splitting or condensing" |
| `pattern` | 1,000 – 2,500 | 3,000 | Warning: "Consider splitting or condensing" |
| `config` | 3,000 – 8,000 | 5,000 | Warning: "Config observations can be lengthy, but >5K may indicate multi-topic" |
| `session_summary` | 2,000 – 5,000 | 8,000 | Warning: "Session summary is comprehensive — ensure it's focused on key outcomes" |

- **Scenario A — decision at 1,800 chars exceeds ideal but under maximum**:
  **Given** a `decision`-type observation with 1,800 chars of content
  **When** the Quality Checklist evaluates size
  **Then** a soft note is emitted: "Decision is 1,800 chars — slightly above the 500-1,500 ideal range. Consider condensing." Save proceeds without blocking.

- **Scenario B — bugfix at 3,200 chars exceeds maximum**:
  **Given** a `bugfix`-type observation with 3,200 chars of content
  **When** the Quality Checklist evaluates size
  **Then** a warning is emitted: "Bugfix exceeds 3,000 char maximum-before-splitting. Content may cover multiple topics. Consider splitting into focused observations." Save proceeds but the warning is prominent.

- **Scenario C — config at 4,500 chars is within acceptable range**:
  **Given** a `config`-type observation with 4,500 chars describing a complex multi-service deployment configuration
  **When** the Quality Checklist evaluates size
  **Then** the size is accepted (within 3,000-8,000 ideal range, under 5,000 maximum); no warning emitted

---

### FR-006: P0 — MaxTitleLength in engram-dotnet

Add `MaxTitleLength = 200` to `StoreConfig` and enforce title truncation in `AddObservationAsync` for both PostgresStore and SqliteStore. This is the safety net preventing index overflow recurrence.

**Location**: `src/Engram.Store/StoreConfig.cs`, `src/Engram.Store/PostgresStore.cs`, `src/Engram.Store/SqliteStore.cs`.

- **Scenario A — Title exceeds 200 chars, gets truncated with warning**:
  **Given** an observation is being saved with title: "Comprehensive analysis of JWT token lifecycle management across microservices including refresh token rotation, blacklisting strategy, and cross-service propagation patterns for stateless authentication in Kubernetes environments"
  **When** `AddObservationAsync` processes the title
  **Then** the title is truncated to 200 chars, "…" is appended, a warning is logged: "Title truncated from N chars to 200. Original: [first 100 chars]…". The observation is saved with the truncated title.

- **Scenario B — Title at 150 chars passes without truncation**:
  **Given** an observation is being saved with title: "Removed title column from idx_obs_dedupe composite index to prevent PostgreSQL B-tree 2704-byte overflow on observations with long titles and large content payloads"
  **When** `AddObservationAsync` processes the title (the exact title is ≤200 chars)
  **Then** no truncation occurs; the title is saved verbatim; no warning is emitted.

- **Scenario C — StoreConfig validates MaxTitleLength**:
  **Given** `StoreConfig` is initialized
  **When** the configuration is loaded
  **Then** `MaxTitleLength` defaults to 200 and is immutable at runtime. Both PostgresStore and SqliteStore read this value before processing any observation title.

---

### FR-007: P1 — PostgresStore Content Truncation Parity

Add content truncation in `PostgresStore.AddObservationAsync` matching the existing pattern in `SqliteStore.AddObservationAsync`. Currently PostgresStore does NOT truncate content, while SqliteStore truncates at `MaxObservationLength` (100,000 chars). This is an inconsistency that could lead to oversized observations silently saved in PostgreSQL but rejected in SQLite.

**Location**: `src/Engram.Store/PostgresStore.cs` — `AddObservationAsync` method.

- **Scenario A — Content exceeds MaxObservationLength (100,000 chars)**:
  **Given** an observation with 120,000 chars of content is being saved to PostgreSQL
  **When** `PostgresStore.AddObservationAsync` processes the content
  **Then** the content is truncated to `MaxObservationLength` (100,000 chars), `"... [truncated]"` is appended, and a warning is logged. Behavior matches the existing SqliteStore implementation exactly.

- **Scenario B — Content at 85,000 chars is within limits**:
  **Given** an observation with 85,000 chars of content is being saved to PostgreSQL
  **When** `PostgresStore.AddObservationAsync` processes the content
  **Then** no truncation occurs; the content is saved verbatim. Behavior is consistent with SqliteStore.

- **Scenario C — Same truncation pattern applied across Add/Update/Prompt methods**:
  **Given** SqliteStore applies content truncation in `AddObservationAsync`, `UpdateObservationAsync`, and `AddPromptAsync`
  **When** implementing P1 in PostgresStore
  **Then** the same truncation pattern is applied to all three methods (`AddObservationAsync`, `UpdateObservationAsync`, `AddPromptAsync`) in PostgresStore for full parity.

---

### FR-008: P2 — 5K Warning Threshold in mem_save

Add a warning threshold at 5,000 chars in the MCP `mem_save` tool (and `mem_update`). This is a SOFT warning — it does NOT truncate content. It alerts the agent that the observation is approaching problematic lengths and suggests considering splitting.

**Location**: `src/Engram.Mcp/EngramTools.cs` — `MemSave` and `MemUpdate` methods.

- **Scenario A — Content exceeds 5K chars, warning is emitted but save proceeds**:
  **Given** an agent calls `mem_save` with content of 6,200 chars (under `MaxObservationLength` of 100K)
  **When** `MemSave` processes the request
  **Then** a warning is returned to the agent: "Content is 6,200 chars (>5,000 threshold). Consider splitting into multiple focused observations for better searchability and maintainability. Save will proceed." The observation is saved normally — no truncation.

- **Scenario B — Content at 3,500 chars passes without warning**:
  **Given** an agent calls `mem_save` with content of 3,500 chars
  **When** `MemSave` processes the request
  **Then** no 5K warning is emitted; the observation is saved silently.

- **Scenario C — Warning additive to existing MaxObservationLength warning**:
  **Given** an agent calls `mem_save` with content of 105,000 chars (>5K AND >MaxObservationLength of 100K)
  **When** `MemSave` processes the request
  **Then** BOTH warnings are emitted: the 5K soft warning AND the existing truncation warning. The observation is truncated at 100K chars as before. The 5K warning is additive, not a replacement.

---

## 3. Non-Functional Requirements (NFR)

### NFR-001: Backward Compatibility

All Memory Signal changes must be backwards-compatible. The new `topics` field is **optional** — the orchestrator must treat a missing `topics` field as equivalent to a single-topic observation. No existing handoff patterns are broken.

- **Validation**: Existing skill files (forge-arch, forge-dev) that emit the 3-field signal (type, significance, summary) without `topics` must continue to work without errors.

### NFR-002: Non-Blocking Behavior

All quality checks (focus analysis, specificity evaluation, size assessment) must be **non-blocking**. If any check fails to execute (LLM error, timeout, unexpected format), the save must proceed as-is. The guiding philosophy is "suggest, don't enforce."

- **Validation**: If the orchestrator's LLM cannot complete topic counting (e.g., content too short to analyze), it must default to "save without splitting" and log the failure.

### NFR-003: Performance — Curation Overhead

The Paso 2b focus check adds an LLM analysis step to the curation pipeline. This must not add more than 2,000 additional tokens of context processing per observation. The 5K warning in `mem_save` is O(1) string length check.

- **Validation**: The focus check is a single LLM prompt — no iterative or recursive analysis.

### NFR-004: Parity Between Stores

PostgresStore and SqliteStore must have identical truncation behavior for both title (P0) and content (P1). No divergence in behavior between storage backends.

- **Validation**: Both stores call the same `StoreConfig.MaxTitleLength` and `StoreConfig.MaxObservationLength` values and apply the same truncation + marker pattern.

### NFR-005: Deterministic Warning Thresholds

The 200-char title limit (P0) and 5K warning threshold (P2) are deterministic, not LLM-evaluated. They must be enforced by code regardless of agent behavior. The >3 topic threshold in Paso 2b is also deterministic (count-based trigger).

---

## 4. Security Assessment & STRIDE Analysis

This feature has **minimal new security attack surface**. The protocol changes are documentation-level (skill files) with zero runtime security implications. The engram-dotnet P0/P1/P2 changes add defensive limits that **reduce** risk.

| Threat | Analysis | Applicable? |
|--------|----------|-------------|
| **S**poofing | No user identity involved. Memory operations are internal agent-to-MCP calls. | ❌ N/A |
| **T**ampering | Content truncation (P0, P1) modifies observation data in transit to storage. If truncation markers are not appended, data loss is silent. Mitigation: `"…"` and `"[truncated]"` markers are always appended. Dedup queries use truncated title — no false matches expected at 200 chars. | ⚠️ Low |
| **R**epudiation | No user-facing actions to dispute. All MCP operations are internal. | ❌ N/A |
| **I**nformation Disclosure | Quality checks read content that is already being saved — no new data exposure channels. Title truncation discards characters, it does not leak them. | ❌ N/A |
| **D**enial of Service | **Original vulnerability**: ENG-475 was a DoS — long titles + large content caused index overflow, preventing new observations. P0 title truncation **prevents recurrence**. Content truncation at 100K (P1) **prevents oversized inserts**. The 5K warning (P2) is informational only — no blocking. | ✅ Mitigated |
| **E**levation of Privilege | No privilege boundaries crossed. MCP tools are called by the same process with the same permissions. | ❌ N/A |

### Security Requirements (RNF-SEC)

- **RNF-SEC-001 — Truncation markers preserved**: Truncated titles must append `"…"` and truncated content must append `"... [truncated]"` so data loss is explicit and visible.
- **RNF-SEC-002 — No injection via titles**: Title input is validated by `MaxTitleLength` but is NOT HTML-escaped or SQL-escaped at the Store layer (existing parameterization handles SQL injection; this is a length concern only).
- **RNF-SEC-003 — No secrets in observation content**: Quality checks and warnings are logged locally; observation content must NEVER be echoed in logs above 200 chars to avoid leaking sensitive data that agents might inadvertently save.

### Security Assessment Conclusion

This feature **reduces** overall system risk by preventing ENG-475 recurrence. No new threats are introduced. The non-blocking quality checks do not create availability or integrity risks because they operate on content already being saved, not on the save pipeline itself.

---

## 5. Developer Manual Tests (PM-*)

Mark `[x]` after manual execution before `/flow-close`.

| ID | Case / Flow | Steps (summary) | Expected result | [x] |
|----|-------------|-----------------|-----------------|-----|
| PM-1 | **Happy path**: Orchestrator suggests splitting for multi-topic observation | 1. Run a full FlowForge cycle for a feature that touches auth, DB, and caching<br>2. At handoff, emit a Memory Signal with a multi-topic summary<br>3. Observe orchestrator's Paso 2b output | Orchestrator detects >3 topics, suggests splitting with numbered topic list, asks agent to confirm | [ ] |
| PM-2 | **Error path**: Title >200 chars is truncated in engram-dotnet | 1. Call `mem_save` via MCP with title = 300-char string<br>2. Call `mem_get` to retrieve the saved observation<br>3. Verify the stored title | Stored title is exactly 200 chars + `"…"` (total 201 chars). Warning was logged during save. | [ ] |
| PM-3 | **Edge case**: Content >5K but <100K triggers warning, NOT truncation | 1. Call `mem_save` with content = 6,000 chars exactly<br>2. Call `mem_get` to retrieve<br>3. Verify content integrity | Warning emitted at save time ("Consider splitting"). Stored content is exactly 6,000 chars — no truncation, no data loss. | [ ] |
| PM-4 | **Edge case**: forge-memory Quality Checklist fires before `mem_save` during session close | 1. Create a local buffer file with a generic title "Update" and multi-topic content<br>2. Run `forge-memory` session close<br>3. Observe checklist output before `mem_save` | forge-memory emits Quality Checklist: ✗ generic title, ✗ multi-topic, ✗ size. Suggests improvements. `mem_save` proceeds after suggestion. | [ ] |

---

## 6. Open Questions for Human (OQ-*)

| ID | Tag | Question | Default / Assumption |
|----|-----|---------|---------------------|
| OQ-1 | [OPTIONAL] | **IDE adapter propagation** — ADR-013 lists "Actualizar IDE adapters (Cursor, OpenCode, VS Code, Antigravity)" as Phase 1 item 5. Do the adapters require explicit updates to their `ide/{adapter}/` files, or does the `ide/shared/workflow-orchestrator-parity.md` mechanism auto-propagate? | Assumed: Skill changes in `skills/` are the canonical source. Adapters that reference the parity file auto-propagate. adapters with static copies (if any) are updated in a separate thin propagation PR. This spec assumes only `skills/` changes are in scope for Phase 1. |
| OQ-2 | [OPTIONAL] | **Semantic topic counting method** — ADR-013 says "análisis semántico o ## headers." Should the orchestrator use a deterministic approach (counting markdown `## ` headers) as a first pass before LLM-based semantic analysis, or rely solely on LLM analysis? | Assumed: The LLM performs a single-pass analysis (not two-stage). If content has structured `## What`, `## Why`, `## Where`, `## Learned` headers, the LLM counts those as distinct sections. If content is unstructured, the LLM performs semantic topic clustering. The implementation is a single prompt, not a pipeline. |
| OQ-3 | [FOLLOW-UP] | **Metrics on splitting adherence** — Should we track how often agents accept vs. reject splitting suggestions, to measure protocol effectiveness over time? | Out of v1 scope. Can be captured via `mem_retention_stats` or a future `mem_quality_metrics` tool. |

---

## Memory Signal

- type: decision
- significance: high
- summary: "ADR-013 Observation Quality gates adopted: focus check (Paso 2b), expanded Memory Signal with topics field, Quality Checklist in forge-memory, title specificity rules, type-specific size guidelines, plus P0/P1/P2 defensive fixes in engram-dotnet"
- topics: [memory-curation-protocol, observation-quality, engram-dotnet, adr-013]
