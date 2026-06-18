# ADR-001 — Orchestrator Memory Curation Protocol

> **Status**: Accepted
> **Date**: 2026-05-30
> **Feature**: `agent-proactive-memory`
> **Deciders**: Engineering (FlowForge methodology team)
> **Links**: [`spec.md`](../.ai-work/agent-proactive-memory/spec.md) ·
> [`plan.md`](../.ai-work/agent-proactive-memory/plan.md)

---

## Context

Durante la sesión del 2026-05-30 se recibió un reporte de fallo en el flujo Engram
offline-first. El análisis reveló que el problema no era el diseño del producto
(local → cola → sync funciona), sino que **el agente nunca llamó las tools de memoria**
durante ni al cierre de la sesión.

Tres causas raíz identificadas:

1. **Sin criterios claros de qué guardar.** El `forge-dev` tenía "on hard bugs,
   `mem_save`" — lenguaje soft sin definición de "difícil". `forge-arch` solo tenía
   `mem_search` para leer; sin `mem_save` para nuevas decisiones.

2. **`mem_session_summary` era opcional.** El orquestador tenía "optionally mem_save
   metrics at each checkpoint" — sin obligación de summary al cerrar.

3. **Responsabilidad distribuida sin contexto.** Cada agente debía decidir por sí
   mismo si algo valía guardarse, sin acceso al contexto cross-fase (cuántos
   revision_cycles hubo, si hubo rework_ticket con múltiples ciclos).

---

## Decision drivers

- El protocolo debe ser **IDE-agnóstico** y **model-agnóstico** — no puede depender
  de mecanismos de un IDE específico (e.g. `alwaysApply: true` de Cursor).
- Se acepta **pérdida parcial de información** — no se busca perfección, sino que
  lo más relevante se guarde consistentemente.
- El orquestador ya hace coordinación inline (traceability log); memory curation
  es el mismo patrón.
- El `mem_session_summary` al cierre es la **red de seguridad** — aunque falle
  la curation mid-session, el resumen final siempre captura el contexto de la sesión.

---

## Options considered

### Opción A — Distribuida: cada agente guarda directamente

Cada agente (forge-arch, forge-dev, forge-plan, forge-verify) llama `mem_save`
para sus propias decisiones, con criterios definidos en cada skill.

**Pros**: Simple, directo, sin intermediario.

**Contras**: Cada agente decide en el vacío — no sabe si hubo revision_cycles,
rework_count, o si el tema ya está en Engram. Calidad inconsistente. Duplicados
probables. Agentes con responsabilidad de memoria además de su trabajo principal.

**Rechazada.** La calidad de decisión es demasiado dependiente del modelo sin
contexto cross-fase.

---

### Opción B — Global rule: criterios en archivo compartido, cada agente aplica

Un archivo `ide/shared/memory-protocol.md` (o regla `alwaysApply: true` en Cursor)
define los criterios; todos los agentes los leen y aplican independientemente.

**Pros**: Un solo lugar para los criterios. En Cursor, inyección automática.

**Contras**: El problema del contexto cross-fase persiste — ningún agente conoce
revision_cycle o rework_count de otras fases. En IDEs fuera de Cursor, no hay
inyección automática (requiere modificación de cada agent file de todas formas).
Posibles duplicados si dos agentes guardan el mismo tema.

**Rechazada** como mecanismo principal (aceptada como complemento thin en IDE adapters).

---

### Opción C — Orchestrator Curation: signal mínimo + decisión centralizada (ELEGIDA)

Dos agentes (forge-arch, forge-dev) emiten un `## Memory Signal` de 3 campos al
final de su handoff. El orquestador lee el signal y aplica un proceso de 3 pasos
con el contexto cross-fase que solo él tiene.

**Pros**: Decisiones de calidad usando revision_cycle y rework_count. Un solo punto
de lógica de curation (orquestador). Subagentes simples — solo describen, no deciden.
Un solo punto de fallback. IDE-agnóstico.

**Contras**: Saves solo ocurren en transiciones de fase (no mid-execution). Si el
agente omite el signal, el orquestador no puede curar (fallo silencioso). Agrega
un paso a las transiciones de fase (mem_search para deduplicación).

**Trade-offs aceptados**:
- La pérdida de saves mid-execution es aceptable; `mem_session_summary` es la red.
- Los fallos silenciosos de signal vacío son menos frecuentes que los fallos del
  enfoque distribuido (todos los agentes deben recordar guardar).
- El paso adicional de mem_search es non-blocking; en timeout se skippea.

**Elegida.**

---

## Decision

Se implementa **Opción C — Orchestrator Memory Curation Protocol**.

### Diseño

**Memory Signal** (contrato del agente emisor):

```markdown
## Memory Signal
- type: decision | bugfix | config | pattern | none
- significance: high | low
- summary: "Una sola línea describiendo qué ocurrió"
```

Regla para el agente: describe, no decide. No llama `mem_save` directamente.

**Memory Curation Protocol** (orquestador, 3 pasos):

```
PASO 1 — Tipo elegible?
  type == none → SKIP
  type en {decision, bugfix, config, pattern} → continuar

PASO 2 — Hubo fricción?
  significance == high → continuar
  revision_cycle >= 1 → continuar
  rework_count >= 2 → continuar
  ninguno → SKIP

PASO 3 — ¿Ya existe en Engram?
  mem_search(query=summary, limit=1)
  resultado reciente y similar → SKIP
  no existe → mem_save(title, type, content, topic_key)
  MCP no responde → .engram/local_memory/obs-<ts>.md
```

**Agentes que emiten signal**: `forge-arch`, `forge-dev` únicamente.

**Agentes sin cambios**: `forge-plan`, `forge-verify`, `forge-discovery`, `forge-memory`.

**Session close**: `forge-memory` llama `mem_session_summary` siempre en `/flow-close`.
Es obligatorio, no opcional. Si MCP falla → `.engram/local_memory/obs-<ts>-session-close.md`.

### Scope de implementación

La lógica vive en `skills/` (IDE-agnóstica). Los IDE adapters reciben propagación
thin sin lógica propia:

```
skills/forge-orchestrator/SKILL.md  ← Memory Curation Protocol
skills/forge-arch/SKILL.md          ← Memory Signal en output
skills/forge-dev/SKILL.md           ← Memory Signal (reemplaza mem_save directo)
ide/shared/workflow-parity.md       ← Contrato cross-IDE
ide/{cursor,antigravity,opencode,vscode}  ← Propagación thin
```

---

## Consequences

**Positivas:**

- El conocimiento de decisiones contestadas y bugs difíciles se guarda con alta
  confiabilidad porque el orquestador tiene el contexto para evaluarlos.
- Los subagentes no mezclan responsabilidades de memoria con su trabajo principal.
- La lógica de fallback está en un solo lugar (orquestador), no dispersa.
- IDE-agnóstico: cualquier IDE que implemente el skill recibe el comportamiento.

**Negativas / aceptadas:**

- Conocimiento generado mid-execution (antes del handoff de fase) no se guarda
  automáticamente. Red de seguridad: `mem_session_summary` al cierre.
- Si un agente omite el Memory Signal, el orquestador no puede curar esa fase.
  Mitigación: el formato es de 3 campos — baja probabilidad de omisión.
- Agrega `mem_search` como paso en las transiciones de fase. Impacto aceptable
  dado que es non-blocking y solo se ejecuta cuando hay un signal relevante.

---

## Notes

Este ADR es el primero en `docs/decisions/`. El directorio fue creado como parte
del ítem 20 del roadmap. Los ADRs futuros deben seguir este formato y ser nombrados
`ADR-NNN-kebab-case-title.md`.

La herramienta `mem_promote_to_md` de engram-dotnet puede generar y sincronizar
ADRs desde Engram. Este ADR fue creado manualmente como parte del proceso de
establecer el directorio.
