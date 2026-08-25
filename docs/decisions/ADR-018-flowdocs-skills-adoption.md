# ADR-018 — FlowDocs skills adoption & delegation protocol

> **Status**: Accepted
> **Date**: 2026-08-25
> **Feature**: `adopt-flowdocs-skills` (HU-021)
> **Deciders**: FlowForge methodology team
> **Links**: [`ADR-004`](ADR-004-flowdoc-integration.md) · [`ADR-005`](ADR-005-installer-headless-native-libs.md) · [`ADR-007`](ADR-007-flowdocs-v2-absorption.md) · [`docs/20-flowdoc-ecosystem.md`](../20-flowdoc-ecosystem.md) · [`base-context-mapping`](../../.ai-work/adopt-flowdocs-skills/base-context-mapping.md)

---

## Context

FlowForge uses FlowDoc v2.0 as its documentation layer ([ADR-004](ADR-004-flowdoc-integration.md)). The full v2.0 specification was absorbed into FlowForge's methodology ([ADR-007](ADR-007-flowdocs-v2-absorption.md)), including templates, range-bin conventions ([ADR-005](ADR-005-installer-headless-native-libs.md)), and artifact boundaries.

9 FlowDoc skills are installed globally (`~/.config/opencode/skills/`, user scope) and provide specialized documentation capabilities: `flowdoc-assist`, `flowdoc-hu`, `flowdoc-prd`, `flowdoc-adr`, `flowdoc-rfc`, `flowdoc-api`, `flowdoc-db`, `flowdoc-discover`, and `flowdoc-review`.

However, these skills were not integrated into the FlowForge workflow:

- **F-2**: `forge-orchestrator` had no knowledge of flowdoc skills (0 references in SKILL.md).
- **F-3**: Template paths expected by the skills (e.g., `docs/templates/user-stories/template-user-story.md`) did not match FlowForge's actual template locations (e.g., `docs/templates/HU-template.md`).
- **F-4**: `flowdoc-hu` writes HUs in flat layout (`docs/tasks/HU-NNN-name.md`), conflicting with ADR-005 range-bins (`docs/tasks/HU-001-HU-099/HU-NNN.md`).
- **F-7**: Skill versions were not pinned, risking silent drift when upstream updates.
- **F-8**: `flowdoc-assist` has its own discovery cycle, risking duplication with `forge-discovery` (CKP-0).

The User Story HU-021 requested formal adoption of these skills with clear delegation protocols.

---

## Decision

### 1. Two-cycle model

Adopt a **two independent cycle** architecture:

- **Cycle A (User Stories)**: `forge-orchestrator` invokes `flowdoc-hu` **directly** (no intermediary). The HU cycle is independent of the base docs cycle and exists BEFORE `flowdoc-assist`.
- **Cycle B (Base Documentation)**: `forge-orchestrator` delegates to `flowdoc-assist`, which coordinates ONLY base doc specialists: `flowdoc-discover`, `flowdoc-prd`, `flowdoc-adr`, `flowdoc-rfc`, `flowdoc-api`, `flowdoc-db`, `flowdoc-review`.

`flowdoc-assist` **never** invokes `flowdoc-hu`. `forge-orchestrator` is the single hub connecting both cycles (e.g., when `flowdoc-hu` post-dev reveals a decision needing an ADR, `forge-orchestrator` routes it to Cycle B).

### 2. Base context mapping (no upstream skill modifications)

Resolve template path mismatches (F-3) and HU path drift (F-4) via a **runtime base context mapping** (documented in `.ai-work/adopt-flowdocs-skills/base-context-mapping.md`). No flowdoc skill files are modified. The orchestrator injects FlowForge-canonical paths at invocation time.

### 3. HU path = range-bins (ADR-005)

All HUs use `docs/tasks/HU-001-HU-099/HU-NNN.md` (range-bins per ADR-005). Existing flat HUs (HU-021/022/023) migrate to this structure. `flowdoc-hu` receives the overridden output path via base context.

### 4. flowdoc-assist scope = base docs only

`flowdoc-assist` coordinates discover/prd/adr/rfc/api/db/review. It does NOT handle HUs. When invoked from the FlowForge cycle (CKP-0→4), it receives an already-populated base context from `forge-discovery` — it does NOT run autonomous discovery (avoids F-8 duplication).

### 5. ADR location = methodology layer

This ADR lives in `docs/decisions/` (FlowForge methodology decision), not `docs/architecture/adr/` (product ADRs per ADR-004 boundaries).

### 6. Version pinning

Known skill versions are pinned in `docs/20-flowdoc-ecosystem.md` (`flowdoc-assist@3.1`, `flowdoc-hu@1.1`). Drift detection is manual until auto-sync tooling is built (OQ-5, out of v1 scope).

### 7. Degradation protocol

If a flowdoc skill is unavailable: report the error with skill name + expected path, suggest remediation (install / skip / standalone mode). NEVER fail silently.

---

## Consequences

### Positive

- **Clear delegation protocol**: Every agent knows which skill to invoke and through which path. No ambiguity between `flowdoc-hu` (direct) and `flowdoc-assist` (delegated).
- **No upstream modifications**: Base context mapping resolves all path mismatches without forking or patching flowdoc skills. Upstream updates remain compatible.
- **ADR-005 compliance**: All HUs follow range-bin convention consistently.
- **Traceability**: Version pins in `docs/20-flowdoc-ecosystem.md` enable drift detection.
- **No discovery duplication**: `flowdoc-assist` receives base context from `forge-discovery`, avoiding F-8.
- **Graceful degradation**: Missing skills are reported, not silently ignored.

### Negative

- **Manual drift detection**: No auto-sync with upstream FlowDocs (private repo). Version pins require manual updates when Crhistian releases new skill versions (OQ-5, deferred).
- **User-scope dependency**: Skills live in `~/.config/opencode/skills/` (not vendored). New team members must install skills separately (documented as environment prerequisite).
- **Two-cycle complexity**: The direct vs. delegated invocation model adds cognitive load. Mitigated by clear documentation in `AGENTS.md` and this ADR.

---

## Status History

| Date | Change |
|------|--------|
| 2026-08-25 | Accepted — initial adoption of FlowDocs skills with two-cycle model |
