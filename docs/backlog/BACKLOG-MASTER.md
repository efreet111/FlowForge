# FlowForge — Backlog consolidado

> **Última actualización**: 2026-11-06
> **Fuente de verdad**: `docs/tasks/HU-001-HU-099/` + `.ai-work/{slug}/verify-report.md`
> **Nota**: este archivo es un snapshot de estado, no un historial. Si una fila contradice
> una HU o un verify report, mandan la HU y el verify report.

---

## 📊 Resumen

| Categoría | Total |
|-----------|-------|
| HUs cerradas | 19 |
| HUs en curso | 2 (HU-033, HU-035) |
| HUs en draft | 7 |
| **Total** | **28** |

---

## 🎯 Pendientes activos

| ID | Tema | Estado real | Próximo paso | Prio |
|----|------|-------------|--------------|------|
| **HU-041** | Paridad `ide/install.sh` ↔ `flowforge update` | 🟡 Parcial: D1 (parser) y D3 (cache) corregidos; **D2** (`prompts/` fuera del updater) y **D4** (`FLOWFORGE_PROVIDER` no respetado en C#) abiertos; ADR-028 en `Proposed` | Implementar D2/D4, tests D1–D4 y aprobar ADR-028 | P1 |
| **HU-039** | `flowforge init -y` no reconocido | 🔴 Abierto; `InitCommand` solo registra `--yes` | Registrar alias `-y` + test | P2 |
| **HU-032** | Demo visual en README | 🔴 Abierto; README solo tiene banner | Screenshot/GIF del flujo + alt text | P1 OSS |
| **HU-028** | Mutation testing (NS-10 P1) | 🔴 Abierto; P0 (assertion/coverage gates) ya existe; no hay Stryker/mutmut en CI | Baseline + Stage 1/2 | P2 |
| **HU-034** | Code-aware capture — Parte B | 🟡 Dependencias ENG-416/483 Done; `code-capture` existe en `forge-dev`, pero la captura end-to-end no está verificada | Implementar/verificar `file_path`/`symbol`/`namespace` en `forge-memory` | P2 |
| **HU-045** | Subagent-first invocation | 🟡 Parcial: `autoinvoke: false` aplicado y sección en `skills/flowforge/SKILL.md`; HU sigue `draft` y sin verify | Verificar AC/PM, paridad Cursor, cerrar | P2 |
| **HU-033** | Code-Aware Dev Agent | 🟡 Implementada (`ec8abea`) + verify PASS; PM runtime pendiente | PM-1..PM-5 en sesión real; luego cerrar | P1 cierre |
| **HU-035** | Code-Context Arch Agent | 🟡 Implementada (`19ed2af`) + verify PASS; PM runtime pendiente | PM-1..PM-5 en sesión real; luego cerrar | P1 cierre |
| **HU-037** | Cost Dashboard | ⏸️ Deferred por ROI dudoso; sin código | Decidir si se cierra como deferred o se prioriza | P4 |

---

## 🧹 Deuda de ingeniería / follow-ups (sin HU propia)

| Tema | Estado | Evidencia / próximo paso | Prio |
|------|--------|--------------------------|------|
| `scripts/validate-agent-models.sh` | 🔴 Falla en los 4 IDEs (`exit 1`) | Asume `.agents[*].model` string y `forge-orchestrator`; el schema actual usa map provider→model y `flowforge`. No está cableado en CI | P1 |
| OpenCode `permissions` legacy | ✅ Resuelto 2026-11-06 | `ide/opencode/agents/*.md` + `templates/agents/*.tpl` convertidos a `permissions[]` V2; `grep "^permission:"` = 0 | — |
| Cursor compiled agents | 🟡 `ide/cursor/agents/*.md` están desincronizados de `skills/` | Ejecutar `ide/cursor/compile-agents-from-skills.py` genera ~1.5k líneas de diff; requiere un commit de regeneración dedicado | P2 |
| `CHANGELOG.md` | 🟡 Tres `## [Unreleased]` (dos vacíos al inicio + uno con contenido) | Consolidar en una sola sección al tope | P2 |
| `ide/opencode/templates/agents/flowforge.md.tpl` | ✅ Pre-flight `AGENTS.md first` sincronizado (2026-11-06) | `grep "Pre-flight: AGENTS.md first"` = 1 | — |

---

## ✅ HUs cerradas (19)

| Área | HUs |
|------|-----|
| Onboarding / docs | HU-021, HU-022, HU-023, HU-024, HU-026, HU-029, HU-042, HU-047 |
| Installer / IDE | HU-025, HU-027, HU-038, HU-040, HU-043, HU-046, HU-048, HU-050 |
| Features / metodología | HU-030, HU-031, HU-036 |

> **Cierres con deuda declarada**:
> - **HU-024**: PM-4 (version-bump test) es follow-up opcional; el template OpenCode ya está sincronizado.
> - **HU-026**: PM-2 (auto-update con ADR real) quedó pendiente; cierre `PASS_DEGRADADO`.
> - **HU-036**: alcance FlowForge-layer v2 cerrado; AC-2/AC-4/AC-5 y métricas completas quedan para engram Phase 2.
> - **HU-050**: `ide/install.ps1` no se validó en Windows; `flowforge uninstall` no limpia `~/.gemini/config/` (preexistente).

---

## 🔴🟡 Backlog sin HU

| Tema | Estado | Notas |
|------|--------|-------|
| Project memory association gate | 📋 Propuesto (`docs/19-project-memory-association-backlog.md`) | P2; definir si se promueve a HU |
| FF-007 full drift health check | 📋 Solo existe la versión lite (HU-031) | Evaluar si el lite alcanza |
| NS-09 / ADR-013 — pin de engram-dotnet | 🟡 `install/manifest.yaml` sigue en `>=0.3.0`; ADR-013 en `Proposed` | Subir el mínimo y agregar smoke post-install |
| NS-10 — engram Postgres index overflow | 🔴 Upstream (`docs/backlog/NS-10-engram-postgres-dedupe-index-overflow.md`) | P0 en engram-dotnet; sync push puede fallar silenciosamente |
| Hybrid search (ENG-418) | 📋 Upstream opcional | No bloquea el contrato actual |

---

## 🔗 Dependencias cross-project

Según `docs/team/flowforge-hu-report.md` y `.ai-work/engram-dotnet-analysis/contract-status.md`:

- **ENG-416** (schema evolution), **ENG-483** (code-aware capture), **ENG-484** (query tools),
  **ENG-412/414** (taxonomy/contradictions) están **Done** en engram-dotnet.
- Eso desbloquea HU-033/034/035/036 a nivel de herramientas; lo que queda es integración,
  PM runtime y cierre documental.
- Phase 2 de contradicciones (métricas de retrieval, cron, thresholds) sigue upstream.

---

## 🎯 Recomendación de secuencia

1. **HU-041 D2/D4** + `validate-agent-models.sh` — afectan instalación/actualización.
2. **HU-039** — fix pequeño, cierra un bug de UX.
3. **HU-032** — impacto OSS alto, esfuerzo acotado.
4. **Cierre de HU-033/035** — ejecutar PM runtime; luego `forge-memory` promueve a `done`.
5. **HU-034/HU-045** — completar integración/verificación.
6. **HU-028/HU-037** — decidir prioridad según demanda real.
