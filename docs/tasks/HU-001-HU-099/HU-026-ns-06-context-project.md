---
hu_id: HU-026
title: "NS-06 — context-project.md: Proyecto autoconfigurado para nuevos miembros"
status: draft
category: improvement
flowforge_slug: "ns-06-context-project"
---

# HU-026 — context-project.md: Proyecto autoconfigurado para nuevos miembros

## User Story
Como nuevo miembro del equipo, quiero disponer de un documento `ia-work/context-project.md` en el repositorio desde el primer día, para que pueda entender el proyecto sin perder 2-3 días consultando a compañeros o interrogando a Engram.

## Acceptance Criteria (business-level)
- [ ] AC-1: El archivo `ia-work/context-project.md` existe en el repositor
- [ ] AC-2: El documento sigue la plantilla definida: Business Goal, Tech Stack, Architecture Overview, Key Decisions, Team & Roles, Related Projects, Getting Started
- [ ] AC-3: El documento es versionable (git history) y sobrevive entre sesiones
- [ ] AC-4: Se actualiza automáticamente tras cambios arquitectónicos mayores
- [ ] AC-5: La información estructural vive en el archivo; la información operacional de sesión vive en Engram

## Scenarios (SDD Spec)
### Happy Path
- [ ] **[Nuevo miembro clona el repo]**
  **GIVEN** Un nuevo desarrollador se incorpora al proyecto
  **WHEN** Clona el repositorio por primera vez
  **THEN** Encontrará `ia-work/context-project.md` con toda la información essential del proyecto
  **🧪 Ref**: spec.ns-06

- [ ] **[Documento se actualiza tras cambio arquitectónico]**
  **GIVEN** El equipo completa un cambio arquitectónico mayor
  **WHEN** Se detecta o se registra un ADR nuevo
  **THEN** `ia-work/context-project.md` se actualiza con los cambios relevantes
  **🧪 Ref**: spec.ns-06

### Edge Cases
- [ ] **[Documento no existe]**
  **GIVEN** El archivo `ia-work/context-project.md` no existe
  **THEN** El sistema de onboarding detecta la ausencia y genera una alerta para crear el documento
  **🧪 Ref**: spec.ns-06

- [ ] **[Engram vs archivo:分工 clara]**
  **GIVEN** Un agente o desarrollador necesita información del proyecto
  **WHEN** Busca contexto structural (tech stack, arquitectura)
  **THEN** Consulta `ia-work/context-project.md`
  **WHEN** Busca contexto operacional (sesiones, decisiones de una sesión específica)
  **THEN** Consulta Engram
  **🧪 Ref**: spec.ns-06

## Context / Notes
[From the spec: Problema, Objetivo, Contenido mínimo, Relación con Engram]

**Problema**: New team members lose 2-3 days understanding the project; humans don't want to query Engram for basic context; context gets lost between sessions

**Objetivo**: Create `ia-work/context-project.md` that exists from day 1, lives in repo (git history), updates on major architectural changes, complements Engram

**Contenido mínimo**: Business Goal, Tech Stack, Architecture Overview, Key Decisions, Team & Roles, Related Projects, Getting Started

**Relación con Engram**: Structural info goes to file; session/operational info goes to Engram

**⚠️ NOTA DE ESPEC INCOMPLETA**: Este spec (NS-06) está marcado como incompleto (pendiente). Falta definir: trigger de actualización, plantilla exacta final, responsable de actualizaciones, y decisión de ubicación del archivo. **La primera tarea de esta HU debe ser completar la definición del spec antes de proceder a implementación.**

## Owner & Timeline
- **Owner**: @equipo-FlowForge
- **Target milestone**: v0.7.0
- **Dependencies**: ADR-004 (FlowDoc integration), NS-05 (Engram integration)
- **Effort**: S (1 día)

## Definition of Done
- [ ] Spec NS-06 completado con: trigger, plantilla exacta, responsable, ubicación
- [ ] Archivo `ia-work/context-project.md` creado con plantilla populated
- [ ] Integración con sistema de updates (post-ADR o similar) definida
- [ ] Documentación deowner actualizada

## FlowForge
> This section is managed by FlowForge agents. Do not edit manually.
- HU status lifecycle: `draft` → `in-progress` → `done`
- Status is updated by forge-memory at `/flow-close`

---

## Technical Debt (if applicable)
- ⚠️ Spec incompleto: NS-06 necesita definición de trigger, plantilla exacta, y ubicación antes de desarrollo
