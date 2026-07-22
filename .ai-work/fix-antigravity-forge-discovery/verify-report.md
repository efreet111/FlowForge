---
feature_slug: fix-antigravity-forge-discovery
verdict: PASS
cycle_count: 1
rework_status: resolved
verified_at: 2026-07-15
verifier: forge-verify
reaudit: true
reaudit_scope: P0 global_workflows path rework
---

# Verify Report — fix-antigravity-forge-discovery (re-audit P0)

## Verdict: PASS

Re-auditoría tras rework P0 (`config/workflows/` → `config/global_workflows/`). El fix resuelve la causa raíz del picker vacío en Antigravity 2.1+. Layer A cumple criterios automatizados; PM-* siguen pendientes para CKP-4.

| Capa | Resultado |
|------|-----------|
| Rework P0 — destino `global_workflows` | ✅ |
| Migración legacy `config/workflows/` | ✅ |
| Doctor — alerta legacy workflows dir | ✅ |
| CI + docker — asserts `global_workflows` | ✅ |
| Docs ADR-009 / AGENTS / QUICKSTART | ✅ |
| Proyecto `.agents/workflows/` sin cambio | ✅ |
| Pack fuente frontmatter (7/7) | ✅ |
| `validate-antigravity-pack.sh` | ✅ PASS |
| `dotnet test` (AntigravityPackValidator + PathHelperTests) | ✅ **7/7 PASS** |
| PM-1..PM-5 (humano) | ⏳ Pendiente — no evaluado en verify |

---

## Resumen ejecutivo

El rework P0 migra correctamente el destino global de workflows a `~/.gemini/config/global_workflows/` en los tres canales (C#, `install.sh`, `install.ps1`), con migración best-effort desde `config/workflows/` legacy y check de doctor dedicado. CI Linux/Windows y `docker-pm1-test.sh` assertan la ruta nueva. ADR-009 y documentación operativa actualizados. El pack fuente mantiene frontmatter válido; tests unitarios verdes (incl. `FR_016`). No se emitió REWORK.

---

## Criterios de aceptación rework P0

| # | Criterio | Estado | Evidencia |
|---|----------|--------|-----------|
| 1 | `PathHelper.AntigravityWorkflows` → `global_workflows` | ✅ | `PathHelper.cs` L64–65; test `FR_016_AntigravityWorkflowsUsesGlobalWorkflowsPath` |
| 2 | C#, `install.sh`, `install.ps1` instalan en `global_workflows` | ✅ | `FlowForgeModule.InstallAntigravity` L307; `install.sh` L154–160; `install.ps1` L165–182 |
| 3 | Migración legacy `config/workflows/` | ✅ | `MigrateLegacyWorkflowsDir()` C# L322–339; `migrate_legacy_antigravity_workflows()` bash L138–149; `Migrate-LegacyAntigravityWorkflows` PS1 L144–155 |
| 4 | Doctor advierte legacy `config/workflows/` | ✅ | `DoctorCommand` L272–283; `LegacyWorkflowsDirDetected()`; test `FR_016_LegacyWorkflowsDirDetectedWhenObsoletePathHasFlowFiles` |
| 5 | CI/docker assert `global_workflows` | ✅ | `test-installer.yml` L106, L176–179, L310–313; `docker-pm1-test.sh` L185–189 |
| 6 | Docs/ADR-009 actualizados | ✅ | ADR-009 L81–87; `ide/antigravity/AGENTS.md`; `QUICKSTART*.md`; `ide/README.md` |
| 7 | Proyecto `.agents/workflows/` sin cambio | ✅ | C# espejo L309; bash L162, L181; PS1 L184 — destino proyecto sigue `.agents/workflows/` |
| 8 | Frontmatter pack válido | ✅ | `validate-antigravity-pack.sh` PASS; test `FR_001_*` PASS |
| 9 | Tests AntigravityPackValidator + PathHelperTests | ✅ | 7/7 PASS (5 + 2 tests) |

---

## Evidencia de ejecución (2026-07-15)

### Script pack — PASS

```bash
bash scripts/validate-antigravity-pack.sh
# Antigravity pack validation passed (7 workflows + workflow rule)
```

### Tests unitarios — PASS

```bash
DOTNET_ROOT=$HOME/.dotnet PATH=$DOTNET_ROOT:$PATH \
  dotnet test tests/FlowForge.Installer.Tests \
  --filter "FullyQualifiedName~AntigravityPackValidator|FullyQualifiedName~PathHelperTests"
```

**Resultado:** `Superado: 7, Con error: 0, Omitido: 0, Total: 7, Duración: 8 ms`

| Test | FR / scope | Resultado |
|------|------------|-----------|
| `FR_001_GoldenPackWorkflowsHaveValidFrontmatter` | FR-001 | ✅ |
| `FR_003_WorkflowRuleHasAlwaysApply` | FR-003 | ✅ |
| `FR_010_WorkflowWithoutFrontmatterFailsValidation` | FR-010 | ✅ |
| `FR_011_HasForgeDiscoverySkillDetectsPresenceAndAbsence` | FR-011 | ✅ |
| `FR_016_LegacyWorkflowsDirDetectedWhenObsoletePathHasFlowFiles` | Rework P0 | ✅ |
| `FR_007_OwnershipTargetsIncludeCriticalPaths` | NFR | ✅ |
| `FR_016_AntigravityWorkflowsUsesGlobalWorkflowsPath` | Rework P0 | ✅ |

### Spot-check estático

| Check | Resultado |
|-------|-----------|
| `grep config/workflows` en instaladores — solo rutas legacy/migración | ✅ |
| `grep global_workflows` en C#/sh/ps1/CI/docker | ✅ coherente |
| `diff -q .agents/workflows/ ide/antigravity/workflows/` | ✅ idénticos (7 pares) |
| `rework_ticket.md` → `status: resolved` | ✅ |
| Sin debug prints en archivos modificados | ✅ |

---

## Auditoría por FR (Layer A — acumulado + rework)

### Pack fuente (FR-001–FR-003)

| FR | Estado | Notas |
|----|--------|-------|
| FR-001 | ✅ | 7/7 frontmatter; validador + tests |
| FR-002 | ✅ | Paridad `.agents/` ↔ `ide/antigravity/` |
| FR-003 | ✅ | `alwaysApply: true` en regla orquestador |

### Instalación skills (FR-004–FR-007)

| FR | Estado | Notas |
|----|--------|-------|
| FR-004 | ✅ | `install.sh` + C# skills symlink/copy |
| FR-005 | ✅ | `Install-AntigravitySkills` en PS1 |
| FR-006 | ✅ | Proyecto `.agents/skills/` |
| FR-007 | ✅ | Sin `skills.json` escrito por instalador |

### Migración install.ps1 (FR-008, FR-009)

| FR | Estado | Notas |
|----|--------|-------|
| FR-008 | ✅ | Destino `%USERPROFILE%\.gemini\config\`; **global** workflows en `global_workflows/` (supersede escenarios spec que citan `config/workflows/`) |
| FR-009 | ✅ | Paridad artefactos; MCP solo C# documentado |

### Validación doctor / CI (FR-010–FR-012)

| FR | Estado | Notas |
|----|--------|-------|
| FR-010 | ✅ | Script CI + validator + tests negativos |
| FR-011 | ⚠️ parcial | CI valida `global_workflows` post-`flowforge install`; CI Windows no ejecuta `install.ps1` (PM-2) |
| FR-012 | ✅ | Doctor global FM en `global_workflows`; legacy dir check; `--strict` |

### Documentación (FR-013, FR-014)

| FR | Estado | Notas |
|----|--------|-------|
| FR-013 | ✅ | ADR-009, AGENTS, README, QUICKSTART |
| FR-014 | ✅ | Post-install reload documentado |

### Paridad multi-canal (FR-015)

| FR | Estado | Notas |
|----|--------|-------|
| FR-015 | ⚠️ parcial | Linux automatizado; Windows PS1 implementado — PM-2 humano |

---

## Step Zero — Inspección línea por línea (rework)

- `MigrateLegacyWorkflowsDir`: copia condicional por mtime; no borra legacy (doctor alerta residuos) — comportamiento aceptable.
- Migración bash/PS1: misma semántica que C# (copia si destino ausente o legacy más nuevo).
- `LegacyWorkflowsDirDetected`: detecta `flow-*.md` en dir obsoleto — coherente con doctor.
- Espejo `config/.agents/workflows/` conservado para workspace mixto — sin regresión.

---

## Herramientas Engram

- `mem_verify_artifact`: no disponible (error MCP en sesión).
- `mem_traceability`: no disponible (error MCP en sesión).
- Trazabilidad documentada manualmente en este reporte.

---

## Gaps residuales (no bloquean PASS)

1. **`spec.md` drift:** FR-008/FR-011/FR-013 y PM-5 aún citan `config/workflows/` como destino global; implementación y ADR-009 usan `global_workflows/`. Recomendado sync spec en follow-up doc, no rework de código.
2. **CI Windows** no ejecuta `ide/install.ps1` — cubrir con PM-2.
3. **PM-1..PM-5** sin marcar — requerido para CKP-4, no para veredicto verify.
4. **Legacy dir no se elimina** tras migración — doctor advierte; usuario puede limpiar manualmente.

---

## Pending Manual Tests

El desarrollador debe ejecutar PM-* de `spec.md` antes de `/flow-close`:

| ID | Estado |
|----|--------|
| PM-1 | [ ] Linux `/flow-*` tras install global + reload Antigravity |
| PM-2 | [ ] Windows install.ps1 paridad config |
| PM-3 | [ ] Proyecto `.agents/skills/forge-discovery` |
| PM-4 | [ ] Doctor diagnóstico |
| PM-5 | [ ] Reinstall no degrada frontmatter en `global_workflows/` |

---

## 🔍 Manual Verification Steps

1. **PM-1 (Linux picker — crítico post-rework):** Tras `flowforge install`, reiniciar Antigravity → escribir `/` → verificar 7 comandos `flow-*`. Confirmar archivos en `~/.gemini/config/global_workflows/` (no solo `config/workflows/`).

2. **PM-2 (Windows):** Ejecutar `ide/install.ps1` → verificar `%USERPROFILE%\.gemini\config\global_workflows\` (7 archivos con FM) → no legacy en `%LOCALAPPDATA%\Google\Gemini\antigravity\`.

3. **PM-3 (Proyecto):** `flowforge init .` → confirmar `.agents/skills/forge-discovery/SKILL.md` y `.agents/workflows/flow-*.md`.

4. **PM-4 (Doctor):** Con install correcto: `flowforge doctor` sin alertas Antigravity. Simular legacy: crear `~/.gemini/config/workflows/flow-test.md` → doctor debe reportar `Antigravity: legacy workflows dir`.

5. **PM-5 (Idempotencia):** Re-ejecutar install → `head -5 ~/.gemini/config/global_workflows/flow-start.md` sigue con frontmatter.

6. **Marcar PM-* en spec.md** antes de invocar `/flow-close`.

---

## Decisión CKP-3

`cycle_count: 1` — rework P0 resuelto y verificado. Sin REWORK ticket abierto. Máximo 3 ciclos no alcanzado.

---

## Historial de veredictos

| Fecha | Veredicto | Motivo |
|-------|-----------|--------|
| 2026-07-15 (inicial) | PASS_DEGRADADO | Tests bloqueados por permisos `obj/` |
| 2026-07-15 (re-audit v1) | PASS | 4/4 tests verdes + estático OK (pre-rework path) |
| 2026-07-15 (re-audit P0) | **PASS** | Rework `global_workflows` verificado; 7/7 tests + validador OK |
