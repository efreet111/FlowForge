<!-- 
  Adapted from FlowDoc v2.0 (github.com/crhistianmdz/FlowDocs) — MIT License
  Modified by FlowForge: additions documented in ADR-004.
-->

---
hu_id: HU-021
title: "Adopt FlowDocs skills para documentación"
status: in-progress
flowforge_slug: "adopt-flowdocs-skills"
---

# HU-021 — Adopt FlowDocs skills para documentación

## User Story

**As a** FlowForge development team,
**I want** adoptar las skills de FlowDocs y su orquestador (flowdoc-assist) para crear documentación,
**so that** la documentación del proyecto se cree de manera estructurada y consistente usando las skills especializadas de FlowDocs.

---

## Acceptance Criteria (business-level)

These criteria define when this feature is "done" from a **business perspective**.

- [ ] AC-1: Las skills de FlowDocs (flowdoc-assist, flowdoc-prd, flowdoc-hu, flowdoc-adr, flowdoc-rfc, flowdoc-api, flowdoc-db, flowdoc-discover, flowdoc-review) están integradas y accesibles desde el flujo de FlowForge
- [ ] AC-2: El orquestador flowdoc-assist puede coordinar las skills especializadas para crear documentación
- [ ] AC-3: La documentación existente del proyecto puede ser auditada y mejorada usando las skills de FlowDocs
- [ ] AC-4: Los agentes de FlowForge pueden invocar las skills de FlowDocs según necesidad

---

## Scenarios (SDD Spec)

### Happy Path

- [ ] **[Adoption de FlowDocs skills]**
  **GIVEN** un proyecto que usa FlowForge con FlowDoc como framework de documentación
  **WHEN** se requiere crear o actualizar documentación (PRD, HU, ADR, RFC, API, DB)
  **THEN** las skills de FlowDocs están disponibles y son invocables desde flowdoc-assist
  **🧪 Ref**: `skills/flowdoc-*/SKILL.md`

- [ ] **[Crear HU con flowdoc-hu]**
  **GIVEN** un requerimiento nuevo que necesita una HU
  **WHEN** se invoca flowdoc-hu para crear la HU
  **THEN** la HU se crea en `docs/tasks/` usando el template de FlowDoc
  **🧪 Ref**: `docs/tasks/HU-*.md`

- [ ] **[flowdoc-assist como orquestador]**
  **GIVEN** una solicitud de documentación compleja (ej: crear PRD + HU + ADR relacionados)
  **WHEN** se invoca flowdoc-assist
  **THEN** coordina las skills especializadas y produce la documentación solicitada
  **🧪 Ref**: `flowdoc-assist/SKILL.md`

### Edge Cases

- [ ] **[Skills no disponibles]**
  **GIVEN** una skill de FlowDocs no está instalada o no responde
  **WHEN** se intenta invocar via flowdoc-assist
  **THEN** se reporta el error y se sugiere remediación
  **🧪 Ref**: `flowdoc-assist/SKILL.md` — error handling

- [ ] **[Documentación existente en formato diferente]**
  **GIVEN** el proyecto tiene documentación en un formato no estándar
  **WHEN** flowdoc-discover analiza el proyecto
  **THEN** puede identificar y convertir/adaptar la documentación existente
  **🧪 Ref**: `flowdoc-discover/SKILL.md`

### Error Cases

- [ ] **[Conflicto de documentación]**
  **GIVEN** existe documentación contradictoria entre archivos
  **WHEN** flowdoc-review audita la documentación
  **THEN** se detecta el conflicto y se reporta para resolución manual
  **🧪 Ref**: `flowdoc-review/SKILL.md`

---

## Context / Notes

### Background

FlowForge actualmente usa FlowDoc v2.0 como framework de documentación (ver ADR-004). Las skills de FlowDocs están instaladas globalmente en OpenCode, pero no están formalmente integradas en el flujo de trabajo de FlowForge.

### Motivation

Al adoptar las skills de FlowDocs, el equipo podrá:
1. Crear documentación estructurada de manera consistente
2. Usar el orquestador flowdoc-assist para coordinar documentación compleja
3. Delegar la creación de docs a agentes especializados
4. Mejorar la trazabilidad entre HU, ADRs, RFCs y código

### Dependencies

- [x] Engram ya está adoptado (memoria persistente)
- [ ] FlowDocs skills instaladas globalmente en OpenCode
- [ ] FlowForge tiene integración con skills externas (verificar)

### References

- [ADR-004: FlowDoc integration](docs/decisions/ADR-004-flowdoc-integration.md)
- [docs/20-flowdoc-ecosystem.md](docs/20-flowdoc-ecosystem.md)
- Skills de FlowDocs: `flowdoc-assist`, `flowdoc-prd`, `flowdoc-hu`, `flowdoc-adr`, `flowdoc-rfc`, `flowdoc-api`, `flowdoc-db`, `flowdoc-discover`, `flowdoc-review`

---

## Owner & Timeline

- **Owner**: @equipo-FlowForge
- **Target milestone**: v0.6.0
- **Dependencies**: Skills de FlowDocs instaladas, verificación de integración

---

## Definition of Done

- [ ] Skills de FlowDocs verificadas y documentadas
- [ ] flowdoc-assist configurado y funcional
- [ ] Prueba de creación de HU con flowdoc-hu
- [ ] Prueba de creación de ADR con flowdoc-adr
- [ ] Documentación del proceso de adopción creado
- [ ] ADR de la decisión de adopción (si es necesario)

---

## FlowForge

> This section is managed by FlowForge agents. Do not edit manually.

To implement this HU:

```bash
# Start the feature cycle
/flow-start adopt-flowdocs-skills
```

- `flowforge_slug` is set by forge-arch when `.ai-work/{slug}/` is created
- HU status lifecycle: `draft` → `in-progress` → `done`
- Status is updated by forge-memory at `/flow-close`

---

## Technical Debt (if applicable)

- [ ] La integración de FlowDocs skills podría requerir cambios en `AGENTS.md` del proyecto
- [ ] Verificar compatibilidad con múltiples IDEs (actualmente solo verificado en OpenCode)
