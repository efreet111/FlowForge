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

The repo is **public** as of ENG-301 release (v0.1.0-alpha.2, 2026-06-23). Remote install via `raw.githubusercontent.com` works directly — no local clone required for end users.

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

## 🚀 OSS Launch — semana 2026-06-23

Ítems bloqueantes y de alta prioridad identificados en el audit crítico pre-lanzamiento OSS del 2026-06-23. Ver análisis completo en [`.ai-work/oss-launch-audit/context-map.md`](../.ai-work/oss-launch-audit/context-map.md).

### P0 — Antes de publicitar en comunidades

| # | ID | Tipo | Item | Estado | Effort | Notas |
|---|-----|------|------|--------|--------|-------|
| 1 | FF-OSS-01 | Doc | Fix README.md: versión `0.4.0` → `0.5.0` | ✅ Done | XS | `be257c2` — README.md + README.es.md actualizados |
| 2 | FF-OSS-02 | Chore | Commitear cambio no-commiteado de `docs/04-roadmap.md` | ✅ Done | XS | `6b0f7ce` — incluido en commit de audit OSS |

### P1 — Esta semana

| # | ID | Tipo | Item | Estado | Effort | Notas |
|---|-----|------|------|--------|--------|-------|
| 3 | FF-OSS-03 | Doc | Agregar tabla de decisión de instalación en README | ✅ Done | S | `90f7acb` — tabla EN+ES + nota alpha aclaratoria + fix "repo privado" stale en README.es.md |
| 4 | FF-OSS-04 | Doc | Demo GIF o screenshot en README | Ready | L | Sin prueba visual → menos estrellas; `examples/crud-tareas/` existe pero requiere 3 clicks |

### Detalle de ítems

#### FF-OSS-01 — Fix versión en README (XS, ~5 minutos)

**Problema:** `README.md` línea 5 dice `**Version:** [\`0.4.0\`](VERSION.md)`. `VERSION.md` dice `0.5.0`. El CHANGELOG tiene `[0.5.0]` con adiciones significativas (CKP-1 BLOCKER gate, FlowDoc, Pattern Search, Stack Installer). Un visitante de GitHub hace clic en el link del changelog y ve `0.5.0` — inconsistencia visible en segundos.

**Criterios:**
- [ ] `README.md` dice `0.5.0` y el link apunta correctamente a `VERSION.md`
- [ ] `README.es.md` actualizado también (misma línea)

---

#### FF-OSS-02 — Commitear cambio de roadmap (XS, ~2 minutos)

**Problema:** `docs/04-roadmap.md` tiene un cambio unstaged: la línea que anuncia que el repo es público (ENG-301 release, v0.1.0-alpha.2, 2026-06-23). Está escrito pero no commiteado.

**Criterios:**
- [ ] `git status` no muestra `docs/04-roadmap.md` como modificado
- [ ] Commit mensaje: `docs(roadmap): mark repo as public (ENG-301 v0.1.0-alpha.2)`

---

#### FF-OSS-03 — Tabla de decisión de instalación (S, ~30 minutos)

**Problema:** El README presenta tres caminos de instalación sin ningún criterio para elegir:
1. One-liner public repo (IDE skills)
2. Stack installer AOT binary (alpha label puede asustar)
3. Local clone (para contribuidores)

Un nuevo usuario no sabe cuál usar. El Stack installer es la opción recomendada para la mayoría, pero "alpha" en el label genera dudas.

**Propuesta:** Agregar una tabla de 4 filas antes de los bloques de código:

```markdown
| If you are... | Use |
|---------------|-----|
| Trying FlowForge for the first time | Stack installer (one-liner) |
| Setting up a specific IDE only | `ide/install.sh` / `ide/install.ps1` |
| Integrating into an existing project | `ide/install.ps1 -ProjectPath <path>` |
| Contributing to FlowForge | Local clone + manual install |
```

**Criterios:**
- [ ] Tabla presente en README.md antes de los bloques de código de instalación
- [ ] Label "alpha" en Stack installer acompañado de nota breve: "stable for daily use, binary format is alpha"
- [ ] README.es.md actualizado con tabla equivalente

---

#### FF-OSS-04 — Demo visual en README (L, ~4 horas)

**Problema:** Para OSS discovery, la ausencia de prueba visual es la brecha más costosa. El `flowforge-demo-task-manager` existe y tiene código TypeScript funcional. Los artefactos en `examples/crud-tareas/` son texto, no interactivos.

**Opciones (en orden de esfuerzo):**
1. **Screenshot** de un spec.md + verify-report.md abiertos en el IDE (1h) — mínimo viable
2. **GIF animado** mostrando `/flow-start → CKP-1 → /flow-plan → CKP-2 → código generado` (~4h) — máximo impacto
3. **Video embed** (YouTube/Loom) — mayor esfuerzo pero reutilizable para blog posts

**Criterios mínimos (opción 1):**
- [ ] Al menos una imagen en README.md que muestre el workflow en acción
- [ ] Alt text descriptivo para accesibilidad
- [ ] Imagen alojada en `docs/assets/` o en GitHub Assets (drag-drop en PR)

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
| ENG-301 Stack Installer (C# AOT binary, bootstrap scripts, remote manifest) | FlowForge | ✅ v0.1.0-alpha.2 — see [CHANGELOG](../CHANGELOG.md) and [summary](../.ai-work/stack-installer/summary.md) |
| Model Router MCP server | FlowForge | ❌ Discarded — host IDE routing |

---

## engram-dotnet SDD (reference)

All listed SDD features (verification-tools, promotion-level2, traceability, ttl-configurable, doctor-diagnostic, offline-first-sync, advanced-engram-integration) are **archived** with tests passing. See [`03-engram-dotnet-gaps.md`](03-engram-dotnet-gaps.md) and [`12-engram-tool-reference.md`](12-engram-tool-reference.md).
