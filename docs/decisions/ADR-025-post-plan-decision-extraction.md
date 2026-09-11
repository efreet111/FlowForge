# ADR-025 — Post-Plan hook: decision extraction from plan.md into Engram (HU-030 Parte A)

> **Status**: Accepted
> **Date**: 2026-09-11
> **Feature**: `hu-030-decision-extraction` (FF-002, Parte A)
> **Deciders**: FlowForge methodology team
> **Links**: [HU-030](../../docs/tasks/HU-001-HU-099/HU-030-ff-002-parte-a-decision-extraction.md) · [spec](../../.ai-work/hu-030-decision-extraction/spec.md) · [plan](../../.ai-work/hu-030-decision-extraction/plan.md) · [verify-report](../../.ai-work/hu-030-decision-extraction/verify-report.md) · [ADR-001](ADR-001-memory-curation-protocol.md) · [ADR-021](ADR-021-doc-indexing-topic-key-namespace.md) · [ADR-020](ADR-020-memory-observation-quality.md)

---

## Contexto

Cuando FlowForge produce `plan.md` en la fase Plan (CKP-2), las decisiones arquitectónicas (`[DECISION]`), convenciones (`[CONVENTION]`) y la capability matrix quedan **atrapadas en markdown**: no se persisten como memorias engram, lo que perjudica la discoverability y traceabilidad cross-sesión. El equipo quería que las decisiones del plan fueran capturadas como observaciones engram con trazabilidad al feature-slug, sin romper el flujo de planificación.

La capture debía ser **Parte A (text-only)**: sin awareness de código, sin metadata de archivos (`file_path`, `symbol`, `namespace`) — eso es Parte B, bloqueada por ENG-416 y ENG-483.

## Decision drivers

- **No bloquear nunca la finalización del plan** (NFR-001, hard constraint): capture es fire-and-forget desde la perspectiva del plan.
- **Opt-in por defecto** (DET-4): nunca implícita, nunca por default.
- **Trazabilidad obligatoria** (DET-2): toda capture lleva el feature-slug; sin slug → skip + warning.
- **Separación de concerns** (OQ-1): capture vive en un hook post-Plan, no dentro del agente `forge-plan`.
- **Idempotencia** (NFR-004): reusar el mismo `topic_key` → upsert, no duplicados (verificado en re-run v2).
- **Seguridad** (RNF-SEC-003): redacción de secretos como pre-procesador determinístico (6 regex → `[REDACTED]`), aplicada a contenido Y logs.

## Options considered

### Option A — Capture inline en el agente Plan
**Pros**: Simple; el Plan agent ya tiene el contexto del plan.
**Cons**: Acopla capture con authoring del plan; el agente Plan mezcla responsabilidad de memoria con su trabajo principal; dificulta opt-in granular y fallback. **Rejected** — viola separación de concerns (OQ-1 resuelto en spec).

### Option B — Post-Plan hook delegado a forge-memory (ELEGIDA)
El `forge-orchestrator` agrega un paso en Step 2 (CKP-2): tras la aprobación humana, si el flag `forge.decision_capture.enabled` (`.flowforge.json`) es `true`, invoca el procedimiento de extracción de `forge-memory`. Nunca lanza; los errores se loguean y Step 3 procede siempre.

**Pros**: Capture ortoponal al authoring; un solo lugar de lógica (forge-memory); non-blocking por diseño; IDE-agnóstico (skills + propagación thin).
**Cons**: Capture solo ocurre en la transición CKP-2 (no mid-execution) — aceptado; si el flag está off no hay captures — correcto por DET-4. **Accepted.**

### Option C — Cambiar el motor engram / mem_save
**Pros**: Tipo enum exacto, sesiones automáticas.
**Cons**: Violaría C-4 (consumir mem_save como está); fuera de alcance. **Rejected.**

## Decision

1. **Post-Plan hook** en `forge-orchestrator/SKILL.md` (Step 2, L93–116): tras aprobación CKP-2, chequea opt-in flag; si enabled → delega a forge-memory. Non-blocking: errores logueados, Step 3 procede.
2. **Procedimiento de extracción** en `forge-memory/SKILL.md` (v1.2.0, L196–346): **3-pass regex line-scan** — Pass 1 `[DECISION]` → `decision`, Pass 2 `[CONVENTION]` → `convention`, Pass 3 capability matrix → `capability`. Type fallback si engram rechaza: `convention`→`pattern`, `capability`→`architecture` (OQ-6; no fue necesario — engram acepta los tipos).
3. **Trazabilidad** (CONV-1): `topic_key` prefix `decision-extraction/{feature-slug}/` + `session_id` `plan-capture-{feature-slug}`. Sin feature-slug → skip capture con warning (FR-004 Scenario B).
4. **Redacción de secretos** (CONV-2 / RNF-SEC-003): 6 regex aplicados como pre-procesador determinístico a contenido y logs; nunca delegado al LLM.
5. **Fallo graceful** (DEC-3): aislamiento por item (try/catch per mem_save), logs estructurados, `Captured {n}/{total}` final. **Session bootstrap (F-001)**: `mem_session_start` ANTES del primer `mem_save`; si falla, WARN y continuar sin `session_id` — nunca lanzar.
6. **Markers documentados** en `forge-plan/SKILL.md` (v1.1.0): sintaxis `[DECISION]`/`[CONVENTION]` opcional pero recomendada para extractabilidad (OQ-5 companion).

## Consequences

**Positivas:**
- Las decisiones de plan.md ahora son discoverables vía `mem_search` (PM-5: 10/10 encontradas por feature-slug).
- Trazabilidad completa: plan → memoria engram → feature-slug.
- La capture es segura (secretos redactados) y no interrumpe el flujo (fail-safe).
- Idempotente: re-ejecutar capture sobre el mismo plan no duplica (NFR-004 verificado).

**Negativas / aceptadas:**
- Capture solo ocurre en transición CKP-2; conocimiento mid-execution depende de `mem_session_summary` (red de seguridad de ADR-001).
- Planes legacy sin markers `[DECISION]`/`[CONVENTION]` → no-op silencioso (DET-5); los nuevos planes los incluyen por A.1.
- Parte B (capture code-aware con `file_path`/`symbol`/`namespace`) queda diferida a ENG-416/ENG-483.
- P3 no bloqueantes: F-002 (regex AWS deja char residual en keys de 17 chars → `AKIA[A-Z0-9]{16,}`), F-003 (fixture header `## 6.` vs regex primario `## 3.` — mitigado por fallback a subheaders).

## Notes

- Tipo de observación persistida en Engram: `decision` (obs #3857, topic_key `decision-extraction/procedure`) + `bugfix` F-001 (obs #3858, topic_key `engram/session-bootstrap`).
- Este ADR es la promoción Level-2 del Memory Signal del spec (`type: decision`, `significance: high`).
- Lección transversal reafirmada: `mem_save` con `session_id` custom exige `mem_session_start` previo (verificado en vivo durante este mismo cierre).