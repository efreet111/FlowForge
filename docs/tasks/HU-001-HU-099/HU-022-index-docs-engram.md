<!-- 
  Adapted from FlowDoc v2.0 (github.com/crhistianmdz/FlowDocs) — MIT License
  Modified by FlowForge: additions documented in ADR-004.
-->

---
hu_id: HU-022
title: "Indexar documentos en Engram"
status: done
flowforge_slug: "index-docs-engram"
---

# HU-022 — Indexar documentos en Engram

## User Story

**As a** FlowForge development team,
**I want** que los documentos de FlowDoc (PRD, ADRs, RFCs, API, DB) estén indexados en Engram con referencias y contenido clave,
**so that** el discover pueda encontrar información relevante de manera rápida y eficiente, consumiendo menos tokens.

---

## Acceptance Criteria (business-level)

- [x] AC-1: PRD.md está indexado en Engram con referencia y contenido clave
- [x] AC-2: Todos los ADRs en `docs/architecture/adr/` están indexados en Engram
- [x] AC-3: Todos los RFCs en `docs/architecture/rfc/` están indexados en Engram
- [x] AC-4: Documentación de API (`docs/api/`) indexada con metadata relevante
- [x] AC-5: Documentación de DB (`docs/database/`) indexada con metadata relevante
- [x] AC-6: HU (User Stories) no se indexan en este paso (el flujo /flow-start ya guarda la referencia)
- [x] AC-7: Cuando un documento se actualiza, su index en Engram se actualiza automáticamente
- [x] AC-8: Existe un mecanismo para indexar documentos existentes que aún no están en Engram

---

## Scenarios (SDD Spec)

### Happy Path

- [x] **[Indexar PRD]**
  **GIVEN** un proyecto con FlowDoc configurado
  **WHEN** se ejecuta la indexación de documentos
  **THEN** el PRD.md está indexado en Engram con referencia y contenido clave
  **🧪 Ref**: `docs/PRD.md`

- [x] **[Indexar ADRs]**
  **GIVEN** múltiples ADRs en `docs/architecture/adr/`
  **WHEN** se ejecuta la indexación
  **THEN** cada ADR está indexado con: título, decisión principal, contexto resumido, y path
  **🧪 Ref**: `docs/architecture/adr/*.md`

- [x] **[Indexar RFCs]**
  **GIVEN** RFCs en `docs/architecture/rfc/`
  **WHEN** se ejecuta la indexación
  **THEN** cada RFC está indexado con: título, estado, tema en discusión, y path
  **🧪 Ref**: `docs/architecture/rfc/*.md`

- [x] **[Indexar API docs]**
  **GIVEN** documentación de API en `docs/api/`
  **WHEN** se ejecuta la indexación
  **THEN** cada endpoint/contrato está indexado con metadata (nombre, método, path)
  **🧪 Ref**: `docs/api/*.md`

- [x] **[Indexar DB docs]**
  **GIVEN** documentación de base de datos en `docs/database/`
  **WHEN** se ejecuta la indexación
  **THEN** cada schema/tabla está indexado con metadata relevante
  **🧪 Ref**: `docs/database/*.md`

- [x] **[Actualización automática]**
  **GIVEN** un documento ya indexado en Engram
  **WHEN** el documento se actualiza en el filesystem
  **THEN** el index en Engram se actualiza automáticamente
  **🧪 Ref**: `skills/forge-*/SKILL.md` — integration

### Edge Cases

- [x] **[Documento sin indexar existente]**
  **GIVEN** un proyecto con documentos FlowDoc pero sin indexación en Engram
  **WHEN** un agente necesita información
  **THEN** existe un mecanismo para indexar esos documentos bajo demanda
  **🧪 Ref**: `flowdoc-discover/SKILL.md`

- [x] **[Documento indexado pero modificado]**
  **GIVEN** un documento indexado que fue modificado sin actualizar el index
  **WHEN** se consulta Engram
  **THEN** se detecta la discrepancia y se sugiere re-indexar
  **🧪 Ref**: `engram/mem_doctor`

### Error Cases

- [x] **[Documento no encontrado]**
  **GIVEN** Engram tiene una referencia a un documento
  **WHEN** se intenta leer el documento
  **THEN** se reporta el error y se sugiere remover o re-indexar la referencia
  **🧪 Ref**: `engram/mem_doctor`

---

## Context / Notes

### Background

El discover de FlowForge actualmente solo lee PRD y HUs directamente. No aprovecha la información almacenada en ADRs, RFCs, API docs ni DB docs. Para que el discover sea eficiente, estos documentos deben estar indexados en Engram.

### Concepto de Indexación

```
Engram guarda:
├── Referencia: path al archivo
├── Contenido clave: resumen/extracted info
└── Metadata: tipo, fecha, tags

El agente decide:
├── Si el contenido clave es suficiente → USA ENGRAM
└── Si necesita más → LEE EL ARCHIVO
```

### Qué guardar en Engram (y qué no)

| Tipo | Guardar en Engram | Leer archivo después |
|------|-------------------|---------------------|
| PRD | ✅ Referencia + contenido clave | Solo si necesita más |
| ADR | ✅ Referencia + decisión principal | Sí, para detalles |
| RFC | ✅ Referencia + tema en discusión | Sí, para detalles |
| API | ✅ Metadata (endpoint, método) | Sí, para contrato completo |
| DB | ✅ Metadata (tabla, columnas) | Sí, para schema completo |
| HU | ❌ No indexar | N/A (el /flow-start ya guarda referencia) |

### Dependencias con HU-021

HU-022 depende de que HU-021 (adoptar FlowDocs skills) esté completa, ya que la indexación será utilizada por el discover mejorado.

### Flujo de indexación

```
1. Nuevo proyecto:
   └── FlowDocs skill crea doc → FlowForge indexa en Engram

2. Proyecto existente sin indexar:
   └── Agente detecta docs sin index → indexa bajo demanda

3. Doc actualizada:
   └── Git hook o skill detecta cambio → actualiza index en Engram
```

### Skill necesaria

Probablemente se necesite crear una skill de FlowForge para manejar la indexación automática de documentos en Engram, ya que FlowDocs skills son agnósticas al ambiente.

---

## Owner & Timeline

- **Owner**: @equipo-FlowForge
- **Target milestone**: v0.6.0
- **Dependencies**: HU-021 (adoptar FlowDocs skills)

---

## Definition of Done

- [x] Mecanismo de indexación implementado
- [x] PRD indexado y verificable
- [x] ADRs indexados y verificables
- [x] RFCs indexados y verificables
- [x] API docs indexados y verificables
- [x] DB docs indexados y verificables
- [x] HUs NO indexadas (verificado que no se indexan)
- [x] Actualización automática funcional
- [x] Documentación del proceso de indexación

---

## FlowForge

> This section is managed by FlowForge agents. Do not edit manually.

To implement this HU:

```bash
# Start the feature cycle
/flow-start index-docs-engram
```

- `flowforge_slug` is set by forge-arch when `.ai-work/{slug}/` is created
- HU status lifecycle: `draft` → `in-progress` → `done`
- Status is updated by forge-memory at `/flow-close`

---

## Technical Debt (if applicable)

- [ ] Skill de indexación automática puede ser necesaria
- [ ] Verificar performance con muchos documentos indexados
- [ ] Definir estrategia de TTL para documentos muy antiguos
