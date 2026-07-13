---
feature: flowdoc-v2-doc-drift
date: 2026-07-13
status: partial_complete_with_known_drift
related_decisions:
  - docs/decisions/ADR-004-flowdoc-integration.md
  - commit: d6a2288 (feat: migrate to FlowDoc v2.0, update templates and documentation)
  - commit: 0f3b872 (Merge branch 'absorb/flowdocs-v2-adoption' into main)
  - engram obs #28 (FlowDoc v2.0 migration merged to main)
---

# Gap capture — FlowDoc v2.0 doc drift

> **Origen:** Detectado el 2026-07-13 durante la sesión `flowdoc-integration-cleanup`.
> El pin en `.flowforge.json` (`docs_framework: "flowdoc@1.1"`) y otros 6 archivos
> internos aún referenciaban v1.1 aunque la migración a v2.0 ya estaba mergeada
> a `main` desde 2026-07-08 (commits `d6a2288` + `0f3b872`).

## Goal

Consolidar el estado real de la documentación tras la migración a FlowDoc v2.0,
documentar qué se arregló, qué queda pendiente, y proponer el plan para cerrar
el drift completo sin afectar a proyectos generados downstream.

## Acciones tomadas (2026-07-13)

### ✅ Archivos corregidos en esta sesión

| Archivo | Cambio | Líneas |
|---|---|---|
| `.flowforge.json` | `"flowdoc"` + `"docs_framework_version": "2.0"` (formato nuevo del installer fix) | 1 |
| `AGENTS.md` | Callout "FlowDoc v2.0 Adopter" + versión del pin | 1 |
| `ide/opencode/AGENTS.md` | Sección "FlowDoc Integration" — versión + pin | 2 |
| `ide/cursor/agents/forge-discovery.md` | Ejemplo precondition formato nuevo | 1 |
| `skills/forge-discovery/SKILL.md` | Ejemplo precondition formato nuevo | 1 |
| `QUICKSTART.md` | 3 menciones en tabla + ejemplo JSON | 3 |
| `QUICKSTART.es.md` | 3 menciones en tabla + ejemplo JSON | 3 |

**Total:** 7 archivos, 12 menciones corregidas.

## Drift restante (no corregido intencionalmente)

### ⚠️ Archivos que requieren análisis sustantivo (no solo bump de versión)

| Archivo | Razón para no tocar ahora |
|---|---|
| `docs/decisions/ADR-004-flowdoc-integration.md` | Es un ADR **histórico** sobre la decisión de adoptar v1.1. Reescribirlo = perder historia. **Recomendación:** crear `ADR-005-flowdoc-v2-migration.md` con la decisión de migración, dejar ADR-004 "frozen" en v1.1. |
| `docs/20-flowdoc-ecosystem.md` | Tiene contenido sustantivo sobre features de v1.1 (artifact boundaries, L1-L4, etc.). Actualizarlo requiere entender qué cambió en v2.0 (ADR-009 discovery block, 45-line switch rule, R2 HU propagation, docs/observaciones/ pattern, L1→L2→L3 staged adoption). **Recomendación:** reescritura mayor en `/flow-start flowdoc-v2-doc-rewrite`. |

### ⛔ Archivos NO tocables en esta sesión (afectan downstream)

| Archivo | Razón |
|---|---|
| `templates/project/docs/PRD.md` | Header `<!-- Adapted from FlowDoc v1.1 ... -->`. Template generado por `flow-init` en cada proyecto nuevo. Cambiar requiere verificar que v2.0 mantiene compatibilidad de estructura. |
| `templates/project/docs/templates/HU-template.md` | Mismo header. HU es el artefacto central de FlowDoc. Cambio debe coordinarse con v2.0 templates upstream. |
| `templates/project/docs/templates/rfc-template.md` | Mismo header. |
| `templates/project/docs/templates/adr-template.md` | Mismo header. |
| `templates/project/docs/tasks/HU-001-example.md` | Mismo header + nota al pie "Template from FlowDoc v1.1". |

**Total templates bloqueados:** 5 archivos.

### 📜 Archivos históricos (NO tocar)

| Archivo | Razón |
|---|---|
| `docs/archive/21-flowdoc-integration-proposal.archived.md` | Es un snapshot histórico de la propuesta de integración con v1.1. Debe permanecer como registro. |
| `CHANGELOG.md` línea 53 | Mención histórica "FlowDoc v1.1-compatible" — debe quedarse como está (es changelog). |

## Diagnóstico del drift

**Lo que pasó:**

1. **2026-06-05**: FlowDoc v1.1 publicado por Cristian M.
2. **2026-06-18**: ADR-004 escrito, integración v1.1 adoptada en FlowForge
3. **~2026-07-01 a 07-08**: Crhistian publica FlowDoc v2.0; rama `absorb/flowdocs-v2-adoption` creada
4. **2026-07-08**: Merge PR #1 commit `d6a2288` — `feat: migrate to FlowDoc v2.0, update templates and documentation`
5. **2026-07-08**: Merge commit `0f3b872` — branch absorbed a main
6. **2026-07-09**: Obs #28 en engram registra el merge

**Lo que NO se hizo completamente:**

- ❌ Actualización del pin en `.flowforge.json` (se quedó en v1.1)
- ❌ Actualización de ejemplos en QUICKSTART, SKILL.md, agents
- ❌ Actualización de headers de atribución en templates
- ❌ Creación de ADR-005 documentando la migración
- ❌ Reescritura de `docs/20-flowdoc-ecosystem.md` con features de v2.0

**Patrón observado:** La migración fue **código/template-céntrica** (templates y
cambio mecánico) pero **no documentó la decisión** (sin ADR) ni **actualizó
la documentación existente** (drift residual). Esto es un anti-patrón que el
Memory Curation Protocol (ADR-001) intenta evitar.

## Recomendación para cerrar el drift

### Paso 1 — Inmediato (esta sesión, ya hecho)
- ✅ Corregir pin y ejemplos internos (7 archivos)

### Paso 2 — Siguiente sprint (1-2 días)
1. Crear `/flow-start flowdoc-v2-doc-rewrite` formal con spec.md + plan.md
2. Crear `docs/decisions/ADR-005-flowdoc-v2-migration.md` documentando:
   - Trigger: rama `absorb/flowdocs-v2-adoption` de Crhistian
   - Decisión: migrar a v2.0 con backward compat donde sea posible
   - Cambios: discovery block format, 45-line switch rule, etc.
   - Consecuencias: 5 templates + doc 20 a actualizar
3. Reescribir `docs/20-flowdoc-ecosystem.md` con:
   - Nuevas features de v2.0 (Discovery block ADR-009, L1→L3 staged adoption)
   - Mapeo actualizado CKP ↔ FlowDoc levels
   - Sección "Migrating from v1.1 to v2.0"

### Paso 3 — Post-templates (1 día con tests)
1. Actualizar 5 templates con headers v2.0
2. Verificar que `flowforge init .` sigue funcionando en proyecto de prueba
3. Verificar que proyectos generados son compatibles con v1.1→v2.0 (si es que
   hay proyectos existentes)
4. Actualizar `templates/.flowforge.json.template` con `flowdoc` + `docs_framework_version: "2.0"`

### Paso 4 — Comunicación
1. Anuncio en CHANGELOG.md (nueva entrada bajo "Unreleased")
2. Nota en `docs/04-roadmap.md` sobre el cierre de drift

## Referencias cruzadas

| Recurso | Path / Comando |
|---|---|
| Pin actual | `.flowforge.json` → `docs_framework: "flowdoc"` + `docs_framework_version: "2.0"` ✅ |
| Adopter Guide | `docs/20-flowdoc-ecosystem.md` (sigue con contenido v1.1 — pendiente) |
| Decisión original v1.1 | `docs/decisions/ADR-004-flowdoc-integration.md` (frozen) |
| Commits de migración | `d6a2288`, `0f3b872` |
| Obs engram del merge | `team/flowforge` obs #28 (2026-07-09 03:34:30) |
| Análisis profundo v2.0 | `team/flowforge` obs #27 (2026-07-09 02:57:29) |

## Files relevantes

- `.ai-work/flowdoc-v2-doc-drift/summary.md` (este archivo)
- `.flowforge.json` (corregido)
- `AGENTS.md`, `ide/opencode/AGENTS.md`, `ide/cursor/agents/forge-discovery.md`,
  `skills/forge-discovery/SKILL.md`, `QUICKSTART.md`, `QUICKSTART.es.md`
  (todos corregidos en esta sesión)

---

**Status final:** `partial_complete_with_known_drift` — Pin y docs internos
alineados a v2.0. Templates y doc 20 quedan como deuda técnica documentada
para el próximo sprint.