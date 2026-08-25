<!-- 
  Adapted from FlowDoc v2.0 (github.com/crhistianmdz/FlowDocs) — MIT License
  Modified by FlowForge: additions documented in ADR-004.
-->

---
hu_id: HU-023
title: "Mejorar discover con Engram y docs indexados"
status: draft
flowforge_slug: ""
---

# HU-023 — Mejorar discover con Engram y docs indexados

## User Story

**As a** FlowForge development team,
**I want** que el discover busque primero en Engram (documentos indexados), luego en docs/, y como último recurso asuma que es un requerimiento nuevo,
**so that** el discover sea más eficiente en tokens, aproveche la información existente, y solo recurra a buscar en archivos cuando sea necesario.

---

## Acceptance Criteria (business-level)

- [ ] AC-1: El discover busca en Engram primero: referencias a PRD, ADRs, RFCs, API docs, DB docs
- [ ] AC-2: Si Engram tiene contenido relevante, el discover lo usa como contexto sin leer archivos adicionales
- [ ] AC-3: Si Engram no tiene suficiente información, el discover lee SOLO los archivos necesarios de docs/
- [ ] AC-4: Si no encuentra información ni en Engram ni en docs/, el requerimiento se trata como completamente nuevo
- [ ] AC-5: El discover respeta las decisiones ya documentadas en ADRs (no las ignora)
- [ ] AC-6: El flujo de búsqueda optimiza el consumo de tokens

---

## Scenarios (SDD Spec)

### Happy Path

- [ ] **[Discover con Engram rico]**
  **GIVEN** un proyecto con documentos indexados en Engram (PRD, ADRs, RFCs, API, DB)
  **WHEN** se ejecuta discover para un requerimiento
  **THEN** el discover encuentra información relevante en Engram y la usa como contexto
  **🧪 Ref**: `skills/forge-discovery/SKILL.md`

- [ ] **[Discover con contenido insuficiente en Engram]**
  **GIVEN** Engram tiene referencias parciales al requerimiento
  **WHEN** el discover consulta Engram
  **THEN** lee SOLO los archivos relevantes de docs/ para complementar
  **🧪 Ref**: `skills/forge-discovery/SKILL.md`

- [ ] **[Discover sin información previa]**
  **GIVEN** el requerimiento es completamente nuevo y no existe en Engram ni en docs/
  **WHEN** el discover busca
  **THEN** determina que es un requerimiento nuevo y procede con búsqueda estándar en codebase
  **🧪 Ref**: `skills/forge-discovery/SKILL.md`

### Edge Cases

- [ ] **[Decisión contradice requerimiento]**
  **GIVEN** existe un ADR que contradice lo que el usuario pide
  **WHEN** el discover encuentra el ADR
  **THEN** reporta la contradicción antes de proceder
  **🧪 Ref**: `skills/forge-discovery/SKILL.md` — contradiction handling

- [ ] **[Documento referenciado pero no encontrado]**
  **GIVEN** Engram tiene una referencia a un documento que no existe
  **WHEN** el discover intenta leer el documento
  **THEN** reporta el documento faltante y sugiere re-indexar
  **🧪 Ref**: `engram/mem_doctor`

- [ ] **[Conflicto entre ADRs]**
  **GIVEN** múltiples ADRs tienen información contradictoria
  **WHEN** el discover los encuentra
  **THEN** reporta el conflicto para resolución manual
  **🧪 Ref**: `skills/forge-discovery/SKILL.md`

### Error Cases

- [ ] **[Engram no disponible]**
  **GIVEN** Engram no está accesible o no hay indexación
  **WHEN** el discover intenta buscar
  **THEN** cae back a buscar directamente en docs/
  **🧪 Ref**: `skills/forge-discovery/SKILL.md` — fallback

- [ ] **[Búsqueda ambigua]**
  **GIVEN** la búsqueda en Engram devuelve múltiples resultados no relacionados
  **WHEN** el discover los procesa
  **THEN** solicita clarificación al humano antes de proceder
  **🧪 Ref**: `skills/forge-discovery/SKILL.md`

---

## Context / Notes

### Background

El discover actual (HU-022 base) solo lee PRD y HU directamente. Con la indexación de documentos en Engram (HU-022), el discover puede ser mejorado para:

1. Buscar primero en Engram (índice rápido)
2. Usar contenido clave de Engram directamente
3. Leer SOLO archivos necesarios si Engram no tiene suficiente info
4. Determinar si el requerimiento es nuevo

### Flujo optimizado del discover

```
DISCOVER MEJORADO:
│
├── 1. BUSCAR EN ENGRAM
│       ├── mem_search(requerimiento)
│       └── ¿Encontró algo? → USA CONTEXTO
│
├── 2. SI NO ALCANZA → LEER DOCS NECESARIOS
│       └── Leer SOLO archivos relevantes
│
└── 3. SI NADA → REQUERIMIENTO NUEVO
        └── Proceder con búsqueda en codebase
```

### Optimización de tokens

```
ESCENARIO: Feature de auth

SIN MEJORA:
→ Lee 50 ADRs → 250ms → alto consumo

CON MEJORA (HU-023):
→ mem_search("auth") → 0.1ms
→ Encuentra: ADR-005, ADR-008
→ Lee SOLO esos 2 → 10ms → bajo consumo
```

### Dependencias

- [ ] HU-021: Adoptar FlowDocs skills (completado)
- [ ] HU-022: Indexar docs en Engram (completado)
- [ ] HU-023: Mejorar discover (esta HU)

### Relación con la HU como token budget

La HU (HU de FlowDoc) aporta contexto variable:
- HU con poca info → discover busca más → más tokens
- HU con mucha info → discover busca menos → menos tokens

El discover mejorado amplifica este efecto: si Engram tiene info, ni siquiera necesita buscar en docs/.

---

## Owner & Timeline

- **Owner**: @equipo-FlowForge
- **Target milestone**: v0.6.0
- **Dependencies**: HU-021, HU-022

---

## Definition of Done

- [ ] Discover busca en Engram primero
- [ ] Contenido de Engram usado como contexto primario
- [ ] Fallback a docs/ cuando Engram no tiene suficiente
- [ ] Determinación de requerimiento nuevo cuando no hay info
- [ ] Manejo de contradicciones con ADRs
- [ ] Verificación de consumo optimizado de tokens
- [ ] Documentación del flujo mejorado

---

## FlowForge

> This section is managed by FlowForge agents. Do not edit manually.

To implement this HU:

```bash
# Start the feature cycle
/flow-start improve-discover-engram
```

- `flowforge_slug` is set by forge-arch when `.ai-work/{slug}/` is created
- HU status lifecycle: `draft` → `in-progress` → `done`
- Status is updated by forge-memory at `/flow-close`

---

## Technical Debt (if applicable)

- [ ] Puede requerir cambios en forge-arch para usar el nuevo discover
- [ ] Verificar compatibilidad con el flujo existente de CKP-0
- [ ] Considerar cache de búsquedas frecuentes en Engram
