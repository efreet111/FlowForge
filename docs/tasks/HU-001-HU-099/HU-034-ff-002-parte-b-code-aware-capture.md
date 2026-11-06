---
hu_id: HU-034
title: "Code-Aware Capture with file metadata"
status: draft
category: feature
flowforge_slug: "hu-034-code-aware-capture"
---

# HU-034 — Code-Aware Capture with file metadata

## User Story
As a FlowForge Memory agent, I want to capture decisions with code-aware metadata (file_path, symbol, namespace), so that future queries can trace decisions to the exact code locations they affect.

## Acceptance Criteria (business-level)
- [ ] AC-1: When a decision is captured, it includes `file_path` indicating which file the decision applies to
- [ ] AC-2: When a decision is captured, it includes `symbol` indicating which class/function the decision affects
- [ ] AC-3: When a decision is captured, it includes `namespace` indicating which module the decision belongs to
- [x] AC-4: Code-aware metadata is captured using engram-dotnet tools (ENG-483 engram watch)
- [x] AC-5: Schema evolution (ENG-416) is applied to support the new metadata fields

## Scenarios (SDD Spec)
### Happy Path
- [ ] **[Capture decision with file_path]**
  **GIVEN** a decision extracted from plan.md at CKP-2
  **WHEN** code-aware capture is triggered
  **THEN** the decision is saved with `file_path` metadata pointing to the relevant source file
  **🧪 Ref**: FF-002-007

- [ ] **[Capture decision with symbol]**
  **GIVEN** a decision extracted from plan.md at CKP-2
  **WHEN** code-aware capture is triggered
  **THEN** the decision is saved with `symbol` metadata indicating the affected class or function
  **🧪 Ref**: FF-002-008

- [ ] **[Capture decision with namespace]**
  **GIVEN** a decision extracted from plan.md at CKP-2
  **WHEN** code-aware capture is triggered
  **THEN** the decision is saved with `namespace` metadata indicating the module
  **🧪 Ref**: FF-002-009

- [ ] **[Complete metadata capture]**
  **GIVEN** a decision with identified code references in plan.md
  **WHEN** code-aware capture is triggered
  **THEN** the decision is saved with all three metadata fields (file_path, symbol, namespace) populated
  **🧪 Ref**: FF-002-010

### Edge Cases
- [ ] **[Partial code awareness]**
  **GIVEN** a decision with only a file reference but no clear symbol or namespace
  **WHEN** code-aware capture is triggered
  **THEN** the decision is saved with available metadata; missing fields are left null
  **🧪 Ref**: FF-002-011

- [ ] **[No code reference in decision]**
  **GIVEN** a decision that does not reference any specific code location
  **WHEN** code-aware capture is triggered
  **THEN** the decision is saved without code-aware metadata (null file_path, symbol, namespace)
  **🧪 Ref**: FF-002-012

- [ ] **[Code-aware tool unavailable]**
  **GIVEN** ENG-483 tools are not available in engram-dotnet
  **WHEN** code-aware capture is triggered
  **THEN** the capture falls back to text-only extraction (Parte A behavior) with a warning logged
  **🧪 Ref**: FF-002-013

## Context / Notes
**Problem Statement**: Parte A (HU-030) captures decisions as text only. Parte B extends this to include code-aware metadata so decisions can be traced to exact file locations, symbols, and namespaces.

**Relationship to Parte A**:
- Parte A (HU-030): Text extraction from plan.md — no code awareness required
- Parte B (this HU): Code-aware capture with file metadata — upstream dependencies done; FlowForge capture path pending

**Dependencies**:
- ENG-416: Schema evolution — ✅ Done (columns `file_path`, `symbol`, `namespace` exist)
- ENG-483: Code-aware memory capture (`engram watch`) — ✅ Done (CLI captures all three fields with relative paths)
- HU-030 must be implemented first (no blockers) — ✅ Done

**Open Questions**:
1. How does the Plan agent identify which code files a decision relates to?
2. Should symbol resolution use static analysis or heuristics from plan.md references?
3. How to handle decisions spanning multiple files/symbols?

## Owner & Timeline
- **Owner**: @equipo-FlowForge
- **Target milestone**: v0.8.0
- **Dependencies**: ENG-416 ✅ Done, ENG-483 ✅ Done, HU-030 ✅ Done
- **Effort**: L (2-3 weeks) for Parte B alone
- **Priority**: P2
- **Status**: READY — dependencies done; FlowForge capture path not implemented

## Definition of Done
- [x] ENG-416 schema evolution is completed and deployed
- [x] ENG-483 code-aware capture tools are implemented in engram-dotnet
- [ ] Decisions captured include file_path metadata when applicable
- [ ] Decisions captured include symbol metadata when applicable
- [ ] Decisions captured include namespace metadata when applicable
- [ ] Fallback to text-only capture when code-aware tools unavailable
- [ ] Tests verify metadata capture for all three fields
- [ ] Integration tests verify end-to-end flow from plan.md to engram

## Validation (2026-11-06)

> **Verdict: NOT CLOSEABLE yet.** Upstream (engram-dotnet) is ready; the FlowForge
> capture path is not implemented. The previous `BLOCKED` status was stale.

### Upstream evidence — PASS

| Check | Result | Evidence |
|-------|--------|----------|
| ENG-416 schema | ✅ PASS | `PRAGMA table_info(observations)` includes `file_path` (col 25), `symbol` (26), `namespace` (27) |
| ENG-483 `engram watch` | ✅ PASS | Watching `src/Billing/BillingService.cs` (relative path) captured #4305 with `file_path=src/Billing/BillingService.cs`, `symbol=BillingService`, `namespace=Billing` |
| CLI metadata write | ✅ PASS | `engram save --file-path --symbol --namespace` produced #4298/#4299/#4300/#4301 with all three fields |
| MCP recall | ✅ PASS | `mem_recall_for_file/module/symbol` and `mem_decisions_for_module` returned the metadata correctly |

> **Operational note**: `engram watch` only extracted `namespace` when the watched path was
> **relative to the repo root** (`src/Billing/BillingService.cs`). With an absolute path
> (`/tmp/.../src/Payment/PaymentService.cs`) it extracted `symbol` but left `namespace` null.
> FlowForge must invoke `engram watch` from the repo root with relative paths.

### FlowForge gap — FAIL / NOT IMPLEMENTED

| Check | Result | Evidence |
|-------|--------|----------|
| AC-1 `file_path` on captured decision | ❌ NOT IMPLEMENTED | `forge-memory/SKILL.md` has no `code_context` handling and its `mem_save` format has no `file_path` |
| AC-2 `symbol` | ❌ NOT IMPLEMENTED | Same; `flowforge/SKILL.md` routes `code-capture` with `code_context`, but `forge-memory` never consumes it |
| AC-3 `namespace` | ❌ NOT IMPLEMENTED | Same |
| AC-4 `engram watch` | ✅ PASS (upstream) | See #4305 above |
| AC-5 schema | ✅ PASS (upstream) | See PRAGMA above |
| Fallback to text-only | ⚠️ PARTIAL | Text-only capture exists (HU-030), but there is no explicit "code-aware unavailable → warn + fallback" branch |

### Critical cross-project finding

The MCP `mem_save` tool **does not expose** `file_path`/`symbol`/`namespace`; extra fields
passed to it are ignored (fixture #4294 has all three columns NULL). The CLI `engram save`
does support them, and the CLI `engram watch` populates them. Therefore the FlowForge
implementation must either:

1. **Preferred (FlowForge layer)**: have `forge-memory` invoke the Engram CLI via subprocess
   when `code_context` is present, following the ADR-023 subprocess-bridge pattern; or
2. **Upstream**: extend MCP `mem_save` to accept and persist the three fields.

### Next implementation step

- Add a "Code-aware capture" procedure to `forge-memory/SKILL.md`:
  parse `code_context` from the `code-capture` signal, build the CLI call, and fall back to
  text-only `mem_save` with a warning if the CLI is unavailable.
- Add an integration test that covers: metadata roundtrip, `engram watch` with relative path,
  partial metadata, no-code-reference, and CLI-unavailable fallback.
- Do **not** mark HU-034 `done` until AC-1/2/3 and the fallback are implemented and verified.

---

## FlowForge
> This section is managed by FlowForge agents. Do not edit manually.
- HU status lifecycle: `draft` → `in-progress` → `done`
- Status is updated by forge-memory at `/flow-close`

---

## Technical Debt (if applicable)
- FlowForge capture path pending: `forge-memory` does not yet consume `code_context`; MCP `mem_save` does not expose code metadata (CLI `engram save`/`engram watch` do).
- Multiple code references in a single decision may require multiple memory entries (one per file/symbol).
