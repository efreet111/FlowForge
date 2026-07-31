# ADR-013 — Memory Observation Quality: Focus Over Size

> **Status**: Proposed
> **Date**: 2026-07-25
> **Feature**: `engram-observation-quality`
> **Deciders**: Engineering (FlowForge methodology team)
> **Incident**: ENG-475 (engram-dotnet)
> **Links**: [ENG-475 ticket](../../../engram-dotnet/.ai-work/eng-475-postgres-dedupe-index-overflow/ticket.md) · [ADR-001 Memory Curation Protocol](ADR-001-memory-curation-protocol.md)

---

## Context

Durante la verificación de sync del 2026-07-24 se descubrió ENG-475: el índice
`idx_obs_dedupe` en PostgreSQL excedía el límite de B-tree (2704 bytes) cuando
las observaciones tenían contenido largo. El error:

```
PostgresException: 54000: index row size 2800 exceeds btree version 4 maximum 2704
```

El fix técnico (remover `title` del índice, PR #22 en engram-dotnet) resolvió el
síntoma inmediato. Sin embargo, el análisis de causa raíz reveló un problema más
profundo: **el protocolo de curación de memoria no guía al agente sobre QUÉ y
CÓMO guardar, resultando en observaciones demasiado largas que mezclan múltiples
temas en una sola entrada**.

### Datos del incidente

- 13 observaciones problemáticas identificadas
- Longitud de contenido: 2133 a 6349 bytes
- 8 del proyecto `team/flowforge`, 5 del proyecto `team/engram-dotnet`
- La más larga: `obs-17d0490c2f3bdee9` con 6349 bytes de contenido

### Estado actual del protocolo (ADR-001)

El Memory Curation Protocol (ADR-001) define:

1. **Memory Signal** — agentes emiten `type`, `significance`, `summary`
2. **3-step curation** — tipo elegible → fricción → duplicado
3. **Formato esperado** — What/Why/Where/Learned
4. **Session summary** — Goal/Discoveries/Accomplished/Next Steps

Lo que **falta**:
- Sin verificación de foco (multi-tema)
- Sin guía de longitud por tipo
- Sin validación de especificidad del título
- Sin protocolo de splitting

---

## Decision drivers

- **Calidad sobre cantidad**: Una observación enfocada en un tema es más valiosa
  que una observación larga que mezcla 5 temas.
- **Searchability**: Observaciones enfocadas tienen títulos específicos que
  facilitan la búsqueda con `mem_search`.
- **Mantenibilidad**: Observaciones individuales se pueden actualizar, podar o
  promover independientemente.
- **No degradar calidad**: Los límites duros de tamaño fuerzan truncamiento y
  pierden información valiosa.
- **Compatibilidad**: El cambio debe ser compatible con el protocolo existente
  (ADR-001) y no romper el flujo actual.

---

## Options considered

### Opción A — Límites duros de tamaño

Agregar `MaxTitleLength = 200` y reducir `MaxObservationLength` a 10K-20K chars.

**Pros**: Simple de implementar, previene recurrencia del bug.

**Contras**: Fuerza truncamiento sobre curación. Penaliza decisiones complejas
que legítimamente necesitan más espacio. Crea precisión falsa — "5K" es arbitrario.
Conflicto con `session_summary` que legítimamente necesita más espacio.

**Rechazada.** Degrada calidad de las memorias guardadas.

### Opción B — Quality gates + Splitting (ELEGIDA)

Agregar verificaciones de calidad al protocolo de curación, no límites de tamaño.
Detectar multi-tema y sugerir splitting. Validar estructura y especificidad.

**Pros**: Mejora QUÉ se guarda, no limita CUÁNTO. Observaciones enfocadas son
más buscables y mantenibles. Compatible con el protocolo existente.

**Contras**: Requiere análisis de contenido (puede ser costoso en tokens). Agentes
pueden ignorar sugerencias de splitting. No previene técmemamente observaciones
largas (solo las desincentiva).

**Trade-offs aceptados**:
- El análisis de contenido es non-blocking; si falla, se guarda sin splitting.
- Los agentes pueden ignorar sugerencias, pero el feedback educativo mejora
  comportamiento a largo plazo.
- La prevención técnica (P0 en engram-dotnet: límite en `title`) es la red de
  seguridad. Esta ADR es el enfoque de calidad.

### Opción C — Solo fix técnico en engram-dotnet

Dejar el protocolo de curación sin cambios y solo aplicar fixes técnicos
(límite en `title`, truncamiento en PostgresStore).

**Pros**: Sin cambios al protocolo. Minimal.

**Contras**: No aborda la causa raíz. Las observaciones seguirán siendo largas
y mezclando temas. Solo previene el crash, no mejora calidad.

**Rechazada** como solución única (aceptada como complemento defensivo).

---

## Decision

Se implementa **Opción B — Quality gates + Splitting** como mejora al
Memory Curation Protocol (ADR-001), complementada con fixes defensivos en
engram-dotnet (P0, P1 del análisis).

---

## Specification

### 1. Focus Check (Paso 2b en curation)

Agregar al Memory Curation Protocol después del Paso 2 (fricción):

```
PASO 2b — ¿Está enfocado?
  Contar temas distintos en el contenido (análisis semántico o ## headers)
  SI > 3 temas:
    → SUGERIR: "Esta observación cubre N temas. Considerá dividir en observaciones
       enfocadas:
       1. [tema 1]
       2. [tema 2]
       ...
       Guardá como observaciones separadas para mejor searchability."
    → SI el agente confirma split: guardar cada tema por separado
    → SI el agente confirma single: proceder (humano override)
  SI <= 3 temas:
    → CONTINUAR
```

### 2. Memory Signal expandido

Actualizar el contrato de Memory Signal en `forge-dev` y `forge-arch`:

```markdown
## Memory Signal
- type: decision | bugfix | config | pattern | none
- significance: high | low
- summary: "Título específico y buscable (no genérico como 'bug fix')"
- topics: [tema1, tema2]  # Si > 2, el orquestador sugerirá splitting
```

### 3. Quality Checklist en forge-memory

Agregar checklist de calidad antes de `mem_save`:

```markdown
## Observation Quality Checklist

Antes de guardar, verificar:
- [ ] Enfocado en UN tema (no 3+ temas mezclados)
- [ ] Título específico (no "bug fix" o "update")
- [ ] Estructura completa (What/Why/Where/Learned)
- [ ] Lección o decisión actionable
- [ ] Tamaño apropiado para el tipo:
  - decision: 500-1500 chars ideal
  - bugfix: 800-2000 chars ideal
  - config: 3000-8000 chars aceptable
  - session_summary: 2000-5000 chars ideal

Si cubre múltiples temas → dividir en observaciones separadas.
```

### 4. Title specificity rule

Los títulos deben ser específicos, no genéricos:

| ❌ Malo | ✅ Bueno |
|---------|---------|
| "Bug fix" | "JWT refresh token rotation prevents replay attacks" |
| "Update" | "Removed title from idx_obs_dedupe to prevent B-tree overflow" |
| "Change" | "Switched from sessions to JWT for stateless auth" |
| "Config" | "PostgreSQL connection pool set to 100 for production" |

### 5. Expectativas por tipo de observación

| Tipo | Ideal | Máximo antes de splitting |
|------|-------|---------------------------|
| `decision` | 500-1500 chars | 2000 |
| `bugfix` | 800-2000 chars | 3000 |
| `pattern` | 1000-2500 chars | 3000 |
| `config` | 3000-8000 chars | 5000 |
| `session_summary` | 2000-5000 chars | 8000 |

Estas son **guías**, no límites duros. El agente puede excederlas si la
información lo justifica, pero debe considerar splitting primero.

### 6. Fixes defensivos en engram-dotnet (complemento)

| # | Acción | Capa | Esfuerzo |
|---|--------|------|----------|
| P0 | Límite en `title` (200 chars) en `AddObservationAsync` | Store | 30 min |
| P1 | Fix truncamiento en PostgresStore (match SqliteStore) | Store | 15 min |
| P2 | Warning threshold a 5K chars en `mem_save` | MCP | 20 min |

---

## Implementation plan

### Fase 1 — Protocol update (FlowForge)

1. Actualizar `skills/forge-orchestrator/SKILL.md` — agregar Paso 2b
2. Actualizar `skills/forge-dev/SKILL.md` — Memory Signal expandido
3. Actualizar `skills/forge-arch/SKILL.md` — Memory Signal expandido
4. Actualizar `skills/forge-memory/SKILL.md` — Quality checklist
5. Actualizar IDE adapters (Cursor, OpenCode, VS Code, Antigravity)

### Fase 2 — Defensive fixes (engram-dotnet)

6. P0: Agregar `MaxTitleLength = 200` en `StoreConfig`
7. P0: Truncar `title` en `AddObservationAsync` (PostgresStore + SqliteStore)
8. P1: Agregar truncamiento de contenido en `PostgresStore`
9. P2: Warning en `mem_save` cuando content > 5K

### Fase 3 — Verification

10. Test: observación multi-tema → sugerencia de splitting
11. Test: título genérico → warning de especificidad
12. Test: título > 200 chars → truncamiento con warning
13. Test: contenido > 5K → warning (no truncamiento)

---

## Consequences

### Positive

- Observaciones más enfocadas y buscables
- Títulos específicos facilitan `mem_search`
- Observaciones individuales se pueden actualizar/podar independientemente
- El protocolo de curación se alinea con su propósito original (ADR-001)

### Negative

- Agentes pueden ignorar sugerencias de splitting (no es enforcement)
- Análisis de contenido puede ser costoso en tokens
- Cambio de comportamiento requiere entrenamiento de agentes

### Risks

- **Riesgo**: Agentes no cambian comportamiento → mitigado por feedback educativo
- **Riesgo**: Análisis de contenido falla → mitigado por non-blocking design
- **Riesgo**: Observaciones legítimamente largas son rechazadas → mitigado por
  "suggest, don't enforce" + humano override

---

## References

- **ADR-001**: Orchestrator Memory Curation Protocol
- **ENG-475**: PostgreSQL idx_obs_dedupe overflow (engram-dotnet)
- **PR #22**: Fix for ENG-475 (commit `62eca98`)
- **PostgreSQL B-tree limitations**: https://www.postgresql.org/docs/current/btree.html
