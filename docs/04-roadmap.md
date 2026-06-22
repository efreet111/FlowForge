# FlowForge + engram-dotnet — Combined roadmap

> **Last updated**: 2026-06-21  
> **FlowForge version**: [`0.5.0`](../VERSION.md) — see [`CHANGELOG.md`](../CHANGELOG.md)  
> **engram-dotnet**: `main` (archived SDD features)

---

## Roadmap principles

1. Each feature is independently mergeable  
2. No breaking changes to engram-dotnet public API (additive only)  
3. Simple first — isolated features before Store-layer refactors  
4. Defer heavy verification until `spec.md` format stabilizes  

---

## Release gate (public launch)

While the repo is **private**, remote install via `raw.githubusercontent.com` returns 404 — use local `ide/install.ps1` or `install.sh`.

### Required for “ready to publish”

| Criterion | Status | Notes |
|-----------|--------|-------|
| Replication via docs | ✅ | QUICKSTART (EN+ES), `ide/README`, `docs/18` |
| IDE parity v0.4 | ✅ | `workflow-orchestrator-parity.md` + 4 IDEs |
| Installers | ✅ | Global + `-ProjectPath` |
| Real flow proof | ✅ | CRUD done; [`examples/crud-tareas/`](../examples/crud-tareas/) |
| CKP-coherent docs | ✅ | Item **15** — core + ide EN fully translated |
| OSS files | ✅ | LICENSE, CONTRIBUTING, SECURITY, CODE_OF_CONDUCT |
| Public language | ✅ | Core docs EN complete — see [`I18N.md`](I18N.md) |

### Optional (non-blocking)

- Public demo repo `flowforge-demo-task-manager`  
- engram MCP stable in daily use (item **9**)  

### i18n policy

| Scope | Language |
|-------|----------|
| `README.md` | English (GitHub default) |
| `README.es.md`, `QUICKSTART.es.md` | Spanish entry |
| `docs/*`, `skills/*`, `ide/*` | English (phased) |
| `/flow-*` commands | Unchanged |

---

## v0.4 release checklist (done vs pending)

### ✅ Done

| Item | Deliverable |
|------|-------------|
| **4** QUICKSTART | EN + ES |
| **7** Installers | `install.ps1` / `install.sh` |
| **2** CRUD case | ✅ [`CASE-1-VALIDATION.md`](../examples/crud-tareas/CASE-1-VALIDATION.md) |
| **3** Example artifacts | `examples/crud-tareas/` |
| **14** (partial) | `VERSION.md`, `CHANGELOG.md`, tag `v0.4.0` |
| **20** Memory Curation Protocol | ✅ ADR-001, skills + 4 IDEs, [`.ai-work/agent-proactive-memory/`](../.ai-work/agent-proactive-memory/summary.md) |

### 🟡 Before broad public adoption (resolved)

| Item | Action |
|------|--------|
| **15** Doc audit | ✅ Completed — `docs/08`, `03` EN; optional Part 1 tables in `15` deferred |
| **8** IDE smoke | OpenCode on Linux (primary), then VS Code / Antigravity |
| **1** OpenCode | Bundle smoke after OS migration |

### 🟡 Post-release

| Items | Topic |
|-------|--------|
| **19** | Project memory association on first save ([`19-project-memory-association-backlog.md`](19-project-memory-association-backlog.md)) — engram has passive similar-project warning only |
| **5–6** | Project template + `.flowforge.json` schema + **`flow-init`** ([`ADR-002`](decisions/ADR-002-scaffold-doc-policy.md)) — scripts drafted, need stack-detection + git-init logic |
| **9** | engram-dotnet MCP diagnostics |
| **10–13** | Concurrency, KPIs, migration guides |
| **14** | Ongoing semver + GitHub releases |
| **22** | Regression / eval suite — see *Testing backlog* below |

---

## Checkpoints (CKP-0 → CKP-4)

| ID | Phase | Type | Trigger | Action |
|----|-------|------|---------|--------|
| **CKP-0** | Discovery | 🔴 Hard stop | Vague requirement | Stop; clarify |
| **CKP-1** | spec.md | 🟡 Human | Spec written | Approve or adjust |
| **CKP-2** | plan.md | 🟡 Human | Plan written | Green light to code |
| **CKP-3** | Verify | 🔴 Mechanical | 3 rework cycles | Escalate |
| **CKP-4** | Close | 🟢 Deploy | Memory done | Deploy decision |

Principles: CKP-0/3 are binary; CKP-1/2/4 are human decisions. `cycle_count` in `rework_ticket.md` frontmatter drives CKP-3.

---

## Improvement plan — 14 items

> Original analysis: 2026-05-26. Status updated 2026-05-30.

### Week 1 — MVP

| # | Item | Priority | Status |
|---|------|----------|--------|
| 1 | OpenCode smoke | P0 | ✅ CI validates JSON, 7 subagents, skill paths, placeholders — passes on GitHub Actions |
| 2 | CRUD case (docs/14) | P0 | ✅ | Validation doc + 20/20 tests |
| 3 | Example artifacts | P0 | ✅ |

### Week 2 — Onboarding

| # | Item | Priority | Status |
|---|------|----------|--------|
| 4 | QUICKSTART | P0 | ✅ |
| 15 | Doc audit / i18n | P0 | ✅ |
| 5 | Project template + `flow-init` scaffold | P1 | ✅ `flow-init.sh` + `flow-init.ps1` + `templates/project/` (11 files) — [`ADR-004`](decisions/ADR-004-flowdoc-integration.md) |
| 6 | `.flowforge.json` schema | P1 | ✅ `templates/project/.flowforge.json.template` with required `paths.*` fields |

### Week 3 — Install & test

| # | Item | Priority | Status |
|---|------|----------|--------|
| 7 | Install scripts | P0 | ✅ |
| 8 | All IDE smoke | P1 | 📋 |
| 9 | engram-dotnet diagnostic | P0 | ✅ 2026-06-18 — server healthy (postgres v1.1.0), binary in PATH, write/read OK, buffer synced (obs 592–595) |
| 10 | Concurrent features | P2 | 📋 |

**Backlog note (OpenCode installs)**:
- OpenCode config parsing is strict: do not include placeholder `file:` references (e.g. `file:...`) inside JSON string values.
- `ide/opencode/opencode.flowforge.json` must not hardcode a developer-specific clone path; installer should patch a repo-path placeholder into the copied file.

### Week 4 — Maturity

| # | Item | Priority | Status |
|---|------|----------|--------|
| 11–12 | KPIs + A/B runs | P2 | 📋 |
| 13 | Migration guides | P2 | 📋 |
| 14 | Semver releases | P3 | 🟡 started |
| 19 | Project memory association (first save) | P2 | 📋 spec — [`19-project-memory-association-backlog.md`](19-project-memory-association-backlog.md) |
| 20 | Memory Curation Protocol | P1 | ✅ — [`ADR-001`](decisions/ADR-001-memory-curation-protocol.md) |
| 21 | Scaffold doc policy (`AGENTS.md`, `DEVELOPMENT.md`, XML `///`) | P1 | ✅ ADR — [`ADR-002`](decisions/ADR-002-scaffold-doc-policy.md) · 📋 `flow-init` templates |

---

## Skills backlog

All **31 skills** are written (7 core + 23 specialized + teacher). Focus is adoption and docs, not new skills.

Details: [`15-agent-skills-technical-spec.md`](15-agent-skills-technical-spec.md).

---

## Future incubator

From [`13-edge-cases-and-risks.md`](13-edge-cases-and-risks.md):

| Idea | Notes |
|------|--------|
| **Project memory association gate** | On first save for a new project: ask human to link to similar project, create new, or **personal-only** scope — see [`19-project-memory-association-backlog.md`](19-project-memory-association-backlog.md) |
| Context poisoning guardrail | Validate stale engrams before Phase 2 |
| Conflict resolution agent | Cross-agent namespace collisions |
| Cost dashboard | USD per phase/epic |
| Drift health check | Code vs `plan.md` every N commits |
| Sequential `.md` write queue | engram-dotnet |
| Lineage enforcement at CKP-3 | Optional orchestrator rule |

---

---

## Testing backlog (item 22)

Regression and eval strategy for FlowForge. See analysis: [can FlowForge be regression-tested?](project-context.md)

Three layers, ordered by effort/value:

### Layer 1 — Structural linting (P1, low effort) ✅ 2026-06-21

Deterministic checks that run on every PR. No LLM required. **Implemented in `opencode-smoke.yml`.**

| Test | What it checks | Where to add |
|------|---------------|-------------|
| SKILL.md completeness | Required sections present in all 7 core skills | CI script |
| `spec.md` schema | Sections 1–4 present; OQ-* rows have valid tag if section 5 exists | CI script |
| `plan.md` schema | Sections 1–4 present | CI script |
| `.flowforge.json` validity | Valid JSON + required `paths.*` fields | CI script |
| `flow-init` smoke | Script runs without error on a temp dir, produces expected files | `opencode-smoke.yml` |
| Cross-references | SKILL.md files referenced in AGENTS.md actually exist | CI script |

### Layer 2 — LLM-as-Judge evals (P2, medium effort)

Use a judge LLM to evaluate agent output against golden examples. Inherits the `forge-verify` LLM-as-Judge pattern.

| Eval | Input | Assert |
|------|-------|--------|
| CKP-1 BLOCKER gate | spec.md with `[BLOCKER]` row + "adelante" | Response does NOT invoke forge-plan; lists the blocker |
| CKP-0 vague req | "improve performance" (no context) | Response asks for clarification, does NOT produce spec |
| forge-arch output | Context Map from CRUD example | Output has sections 1–4, PM-* with ≥2 items, Capability Matrix |
| forge-verify pass/fail | Known passing spec+code pair | Verdict is PASS |
| forge-verify fail | Known failing pair (missing FR) | Verdict is FAIL, rework ticket produced |

### Layer 3 — Golden example re-run (P3, manual)

`examples/crud-tareas/` is the golden baseline. When a skill changes significantly, re-run the CRUD flow manually and verify `CASE-1-VALIDATION.md` still passes 20/20.

This is the "integration test" for the methodology.

### Priority for implementation

1. ~~Layer 1 CI script~~ ✅ Done — added to `opencode-smoke.yml` (2026-06-21)
2. Layer 2 evals for CKP-1 BLOCKER gate (validates the fix we just shipped)
3. Layer 3 is already done manually; formalize trigger in CONTRIBUTING.md

## Completed / discarded

| Feature | Project | Status |
|---------|---------|--------|
| Offline-first sync | engram-dotnet | ✅ Archived |
| Doctor diagnostic | engram-dotnet | ✅ Archived |
| Traceability / TTL / verification | engram-dotnet | ✅ Archived |
| IDE delegation protocol | FlowForge | ✅ |
| Memory Curation Protocol (item 20) | FlowForge | ✅ [`ADR-001`](decisions/ADR-001-memory-curation-protocol.md) |
| Scaffold doc policy (item 21) | FlowForge | ✅ [`ADR-002`](decisions/ADR-002-scaffold-doc-policy.md) · 📋 templates/`flow-init` pending |
| Model Router MCP server | FlowForge | ❌ Discarded — host IDE routing |

---

## engram-dotnet SDD (reference)

All listed SDD features (verification-tools, promotion-level2, traceability, ttl-configurable, doctor-diagnostic, offline-first-sync, advanced-engram-integration) are **archived** with tests passing. See [`03-engram-dotnet-gaps.md`](03-engram-dotnet-gaps.md) and [`12-engram-tool-reference.md`](12-engram-tool-reference.md).
