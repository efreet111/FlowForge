# ADR-002 — Scaffold documentation policy (`flow-init` / project template)

> **Status**: Accepted  
> **Date**: 2026-06-09  
> **Feature**: `project-template` (roadmap items **5–6**, future `flow-init`)  
> **Deciders**: FlowForge methodology + engram-dotnet audit (ENG-208 / AUD-068)  
> **Links**: [`04-roadmap.md`](../04-roadmap.md) · [`17-improvement-plan-specs.md`](../17-improvement-plan-specs.md) (item 5) · [engram-dotnet ENG-208 audit](https://github.com/efreet111/engram-dotnet) (origen del hallazgo)

---

## Context

FlowForge instala **skills, reglas IDE y orquestación** en un repo objetivo (`ide/install.sh`), pero **no define aún** el esqueleto documental completo del producto (`AGENTS.md` operativo del repo, `docs/DEVELOPMENT.md`, `docs/decisions/`, etc.).

Durante la auditoría **ENG-208** en `engram-dotnet` se detectó un patrón transversal:

1. **Comentarios XML (`///`) redundantes** en código nuevo (parsers CLI) — `<param>`/`<returns>` que repiten la firma.
2. **Política documental dispersa** — tentativa de fijarla en specs temporales (`.ai-work/`), en el repo objetivo y en skills de FlowForge sin un **ADR canónico** en FlowForge.
3. **Confusión de capas** — `[Description]` MCP (contrato de tool) mezclado mentalmente con XML doc de implementación.

**Principio rector:** FlowForge define **qué estructura y convenciones** recibe un proyecto al inicializarse; el repo objetivo **implementa** contenido específico del dominio, no reinventa la política de scaffolding.

> **Corrección explícita:** Un ADR en `engram-dotnet` sobre comentarios XML **no es el lugar canónico** si la convención debe aplicarse a todos los proyectos FlowForge. Ese ADR puede existir como *adopción local* o enlace, pero la **decisión de diseño vive aquí** (FlowForge `docs/decisions/`).

---

## Decision drivers

- **`flow-init` / project template (item 5)** debe generar archivos consistentes en cualquier stack.
- Agentes (FlowForge, Cursor, etc.) leen `AGENTS.md` y `docs/` — políticas duplicadas o contradictorias degradan la señal.
- Las specs de feature (`.ai-work/{slug}/spec.md`) son **temporales**; no deben ser fuente de verdad de convenciones de repo.
- ADRs de **metodología FlowForge** pertenecen a **este repo**; ADRs de **dominio del producto** pertenecen al repo objetivo (`docs/decisions/` o `docs/architecture/adr/` según template).

---

## Options considered

### Option A — Cada repo define su política ad hoc

Cada proyecto escribe su propio ADR o sección DEVELOPMENT cuando surge un hallazgo (como ENG-208).

**Pros:** Flexibilidad máxima por stack.  
**Cons:** Drift entre repos; FlowForge no puede asumir convenciones en `flow-init`; agentes pierden predictibilidad.  
**Rechazada** como modelo principal.

---

### Option B — Política solo en skills FlowForge (`forge-dev`, `forge-arch`)

**Pros:** IDE-agnóstico, cerca del agente que escribe código.  
**Cons:** No llega a humanos que leen `docs/`; no se versiona en el repo objetivo; desaparece si alguien no usa FlowForge skills.  
**Rechazada** como única fuente.

---

### Option C — FlowForge ADR + templates generados en `flow-init` (elegida)

FlowForge mantiene ADRs de **scaffolding y metodología**. `flow-init` / project template materializa:

| Artefacto | Contenido generado | Profundidad |
|-----------|-------------------|-------------|
| `AGENTS.md` | Reglas del agente **del producto** + punteros | Breve — enlaza a `docs/` |
| `docs/DEVELOPMENT.md` | Setup, tests, convenciones de código (incl. § XML) | Operativo completo |
| `docs/decisions/README.md` | Guía ADR + template | Copiado/adaptado de FlowForge |
| `docs/decisions/ADR-001-*.md` | Solo si aplica al template | Opcional |
| `.ai-work/` | Estructura vacía | Carpetas + `.gitkeep` |
| `.flowforge.json` | Schema item 6 | Config engram + workflow |

**Pros:** Una fuente canónica; `flow-init` reproducible; repos adoptan la misma forma.  
**Cons:** Requiere mantener templates en FlowForge cuando la política evolucione.  
**Aceptada.**

---

## Decision

**FlowForge es dueño de la política de documentación de scaffolding.**  
`flow-init` (evolución del roadmap item **5 — Project template**) generará, como mínimo:

### 1. `AGENTS.md` (repo objetivo)

- Metadatos del **proyecto** (stack, fuentes de verdad, backlog).
- Reglas del agente **específicas del producto**.
- **Un puntero** a `docs/DEVELOPMENT.md#comentarios-xml-en-código` — **no** la política completa inline.
- Puntero a workflow FlowForge si aplica (`ide/` instalado).

### 2. `docs/DEVELOPMENT.md` (repo objetivo)

Secciones estándar del template:

- Requirements / project structure / tests / build  
- **Comentarios XML en código** (política operativa — ver abajo)  
- Workflow T1–T5 u equivalente del stack  

### 3. Política de comentarios XML (incrustada en DEVELOPMENT template)

**Regla:** escribir `///` solo si aporta información que **no** deduce de nombre, tipos, cuerpo o tests.

| Escribir | No escribir |
|----------|-------------|
| `<summary>` de **tipo** con reglas no obvias | `<param>` / `<returns>` / `<exception>` espejo de firma |
| Comentario inline en invariante no testeada | One-liner en propiedad autoexplicativa |
| `[Description]` en tools MCP (atributo) | Duplicar en XML lo que ya dice xUnit |

**Niveles de poda:** (A) tags espejo de firma → eliminar; (B) props DTO → eliminar one-liners; (C) class summary con reglas → mantener.

**Fu fuera de alcance de flow-init:** poda masiva retroactiva en repos existentes (PRs pequeños al tocar archivos).

### 4. ADRs en repos objetivo

- **Metodología FlowForge** → `FlowForge/docs/decisions/` (este ADR).
- **Decisiones de arquitectura del producto** → `docs/decisions/` del repo (generado por template + `mem_promote_to_md` / `/flow-plan`).
- **No** duplicar ADRs de metodología FlowForge en cada repo salvo enlace de adopción.

### 5. Qué NO genera `flow-init`

- Specs de feature (`.ai-work/{slug}/`) — las crea `/flow-start`.
- Política completa en `AGENTS.md` — solo punteros.
- ADRs de dominio sin decisión — el template incluye README + template vacío.

---

## Consequences

**Positivas:**

- Un solo lugar para evolucionar convenciones (`FlowForge/docs/decisions/` + templates).
- `flow-init` predecible para equipos y agentes.
- Menos ruido XML en código generado por agentes (señal ↑).
- Separación clara: FlowForge = *cómo trabajamos*; repo = *qué construimos*.

**Negativas / aceptadas:**

- FlowForge debe mantener templates sincronizados con ADRs (coste de mantenimiento).
- Repos creados antes de `flow-init` adoptan manualmente o vía PR de alineación (ej. engram-dotnet).
- Stacks no-.NET necesitan adaptar la sección XML a convenciones equivalentes (docstrings, JSDoc mínimo).

---

## Implementation roadmap (FlowForge)

| Fase | Entregable | Roadmap |
|------|------------|---------|
| **1** | ADR-002 Accepted | ✅ este documento |
| **2** | Templates en `templates/project/` o branch `template/` | Item **5** |
| **3** | Comando `flow-init` (CLI o script) que copia templates + `ide/install` | Item **5–6** |
| **4** | Sección XML en `templates/project/docs/DEVELOPMENT.md` | Item **5** |
| **5** | Bullet puntero en `templates/project/AGENTS.md` | Item **5** |
| **6** | Skill `forge-dev` referencia ADR-002 (thin) | Item **15** / skills backlog |

**Repos existentes (ej. engram-dotnet):** mantener sección DEVELOPMENT + puntero AGENTS; enlace a este ADR como fuente canónica; poda de código en PRs locales (no bloqueante).

---

## Related

- [ADR-001 — Memory Curation Protocol](ADR-001-memory-curation-protocol.md)
- [docs/decisions/README.md](README.md) — guía ADR en repos FlowForge
- Roadmap items **5** (Project template), **6** (`.flowforge.json` schema)
- Origen empírico: engram-dotnet AUD-068 / ENG-208 manual audit

---

## Status History

- **2026-06-09:** Proposed — hallazgo AUD-068 en engram-dotnet; debate sobre ubicación canónica (FlowForge vs repo objetivo).
- **2026-06-09:** Accepted — FlowForge dueño de scaffold policy; `flow-init` materializa templates.
