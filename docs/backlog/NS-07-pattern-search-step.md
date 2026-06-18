# NS-07: Pattern Search step in `forge-discovery` (mandatory)

> **Estado**: En proceso (cambio aplicado en `feat/pattern-search-mandate` branch, esperando merge a `main`)
> **Prioridad**: P0 — methodology
> **Fase**: Implementación (CKP-2)
> **Agente**: `@forge-arch` (spec) + `@forge-dev` (skill change)
> **ADR**: [`ADR-003`](../decisions/ADR-003-pattern-search-mandate.md)

---

## 🎯 Problema que resuelve

### Situación actual (antes de este NS)

El skill `forge-discovery` en FlowForge NO incluía un paso obligatorio para buscar **patrones arquitectónicos existentes** en el codebase del proyecto bajo cambio. Esto causaba:

1. **Estimaciones infladas por greenfield** — cuando una feature nueva tenía forma de "voy a tener que diseñar desde cero", el agente proponía diseño greenfield sin verificar si el repo ya tenía una implementación del mismo patrón.
2. **Trabajo duplicado** — código re-implementado cuando ya existía una versión 80% similar.
3. **Pérdida de institutional knowledge** — el patrón estaba en el repo, pero el agente no lo descubría porque no había un paso que lo forzara.

### ¿Por qué nace esta necesidad?

**Trigger event**: ENG-404 spike en `engram-dotnet` (2026-06-18).

ENG-404 (memory relations) estaba estimada **XL** en el backlog de `engram-dotnet` — un Icebox item. Cuando se hizo el análisis, descubrimos que `src/Engram.Verification/` ya tenía un **pattern library completo** (TraceRepository + LineageBuilder + RelationValidator) para requirement traceability.

Un spike de 2h clonó este patrón para observaciones generales usando `topic_key: memrel/{project}/{obsId}`. Resultado:

- 6/6 tests pasan en 0.8s
- Cero cambios de schema
- **Esfuerzo re-estimado: XL → M** (o S sin inverse traversal)

El usuario dijo: _"esto es otro proyecto pero es el mismo flowforge que estamos usando, y necesitamos dejar documentado todo de forma correcta para que no se vuelva un descontrol."_

El gap: el agente **debió** haber corrido pattern search en step 0 (discovery) de FlowForge, no después. La metodología debe cambiar en el orquestador (FlowForge), no en el proyecto que reveló el gap.

---

## 📋 Scope

### Dentro del alcance

1. **Agregar paso 5 (Pattern Search) al skill `forge-discovery`** — obligatorio, con grep commands y ejemplos concretos.
2. **Agregar sección "Why This Skill Exists (Provenance)" al inicio del SKILL.md** — para que el "porqué" cargue con el skill en runtime.
3. **Requerir sección `## Reusable Patterns Found` en el Context Map** — auditable, mecánico.
4. **CHANGELOG entry** — item 21.
5. **Este NS-07** en el backlog.
6. **ADR-003** en `docs/decisions/`.

### Fuera del alcance

- **No** duplicar la regla en docs de proyectos individuales (engram-dotnet AGENTS.md, FlowDocs, etc.) — el orquestador es la única fuente de verdad.
- **No** crear un skill separado `forge-pattern-search` — prematura abstracción; una sola use case real (ENG-404) no justifica una skill nueva.
- **No** cambiar otros skills (`forge-arch`, `forge-plan`, etc.) en este NS — el cambio está localizado en `forge-discovery`.
- **No** agregar tooling automático que haga el search — la responsabilidad es del agente (con verificación de `forge-verify`).

---

## ✅ Tareas

### Implementación (rama `feat/pattern-search-mandate`)

- [x] Crear rama `feat/pattern-search-mandate` en FlowForge
- [x] Crear `docs/decisions/ADR-003-pattern-search-mandate.md` (status: Proposed)
- [x] Crear `docs/backlog/NS-07-pattern-search-step.md` (este archivo, status: En proceso)
- [x] Modificar `skills/forge-discovery/SKILL.md`:
  - [x] Agregar Provenance section al inicio
  - [x] Agregar step 5 (Pattern Search — MANDATORY) en Overview
  - [x] Agregar `## Reusable Patterns Found` requirement al Context Map output
  - [x] Agregar Cross-References section al final
- [x] Modificar `CHANGELOG.md` → [Unreleased] → item 21 (Pattern Search step)
- [ ] Push de la rama
- [ ] PR contra `main`

### Revisión y merge (humano)

- [ ] **CKP-1**: Humano revisa el ADR-003 y aprueba el approach (Option A vs B vs C)
- [ ] **CKP-2**: Humano revisa el cambio al SKILL.md y aprueba el merge
- [ ] Merge a `main` con PR
- [ ] `forge-verify` valida que el cambio cumple con el spec del ADR-003

### Cierre

- [ ] **CKP-4**: `forge-memory` cierra este NS-07 con `summary.md`:
  - Link al PR mergeado
  - Confirmación de que el paso 5 está activo en `main`
  - Métrica: ¿cuántos Context Maps post-merge incluyen la sección?
- [ ] Actualizar ADR-003 status de "Proposed" → "Accepted"
- [ ] Actualizar este NS-07 status de "En proceso" → "Done"

### Post-merge (backlog futuro, no parte de este NS)

- [ ] Considerar agregar la misma sección `## Reusable Patterns Found` a `forge-verify` (para que pueda rechazar diseños que la omiten) — nuevo NS-08 futuro
- [ ] Considerar agregar comando `engram pattern-search <keywords>` para hacer el search automáticamente — nuevo ENG en engram-dotnet, fuera de scope de este NS

---

## 🔗 Cross-references

- **Trigger event**: [engram-dotnet ENG-404 spike](https://github.com/efreet111/engram-dotnet) → `.ai-work/eng-404-spike/spike.md` y `learnings.md`
- **Memoria Engram** (cross-project): observación #2555 (architecture) — `flowforge/pattern-search-mandate`
- **Memoria Engram** (pattern general): observación #2556 (pattern, personal) — "cross-project learnings belong in the orchestrator"
- **ADR relacionado**: `ADR-001-memory-curation-protocol.md` (precedente de cambios metodológicos al orquestador)

---

## 🧪 Cómo validar que funcionó

Una vez mergeado a `main`, validar en las próximas 2-4 semanas:

1. **Auditoría manual**: revisar los próximos 5-10 Context Maps generados por `forge-discovery`. ¿Todos tienen la sección `## Reusable Patterns Found`? Si no, el paso no se está ejecutando.
2. **Métrica de savings**: trackear si hay features que bajan de effort estimate gracias al pattern search. (Esperado: al menos 1-2 por mes, basado en el patrón ENG-404.)
3. **Feedback del usuario**: si el humano reporta que el paso es fricción sin valor, revisar el ADR-003 y considerar refinar (no eliminar — refinar).

Si en 3 meses no hay evidencia de valor, abrir NS-08 para revisar (no necesariamente eliminar; el problema puede ser que el search no se hace bien, no que es innecesario).

---

## 📅 Fechas

- **2026-06-18**: Trigger event (ENG-404 spike) + creación de este NS
- **Pendiente**: PR abierto, esperando revisión humana
- **Pendiente**: Merge a `main`
- **Pendiente**: Cierre (CKP-4)
