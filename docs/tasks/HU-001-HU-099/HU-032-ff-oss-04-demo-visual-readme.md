---
hu_id: HU-032
title: "Demo visual en README"
status: draft
category: oss
flowforge_slug: "ff-oss-04-demo-visual-readme"
---

# HU-032 — Demo visual en README

## User Story
Como visitante del repositorio en GitHub, quiero ver una demo visual (screenshot o GIF) en el README para poder evaluar rápidamente FlowForge y entender su propuesta de valor sin necesidad de instalar o ejecutar el proyecto.

## Acceptance Criteria (business-level)
- [ ] AC-1: El README principal contiene una demo visual (screenshot o GIF) visible sin hacer scroll
- [ ] AC-2: La demo visual muestra FlowForge en acción (interfaz, flujo de trabajo, o resultado tangible)
- [ ] AC-3: La demo visual es de alta calidad y representa fielmente la funcionalidad actual
- [ ] AC-4: La demo visual tiene dimensiones apropiadas para visualización en GitHub (máx 1200px ancho)

## Scenarios (SDD Spec)
### Happy Path
- [ ] **[Demo visible en README]**
  **GIVEN** Un visitante llega al repositorio de FlowForge en GitHub
  **WHEN** Carga la página del README
  **THEN** Puede ver inmediatamente una demo visual (screenshot o GIF) sin hacer scroll
  **🧪 Ref**: inspection

### Edge Cases
- [ ] **[Demo con peso optimizado]**
  **GIVEN** Un visitante con conexión lenta
  **WHEN** Carga el README
  **THEN** La demo visual carga de forma progresiva y no bloquea la lectura del contenido
  **🧪 Ref**: inspection

- [ ] **[Demo accesible]**
  **GIVEN** Un visitante que usa lectores de pantalla
  **WHEN** Navega por el README
  **THEN** La demo visual tiene texto alternativo descriptivo
  **🧪 Ref**: inspection

## Context / Notes
From the backlog - FF-OSS-04 es tarea OSS, demo visual en README. Alto impacto para OSS adoption. Puede ser screenshot o GIF. Es una tarea de documentación, NO una feature de código. No tiene blockers - implementable inmediatamente.

## Owner & Timeline
- **Owner**: @equipo-FlowForge
- **Target milestone**: v0.7.0
- **Dependencies**: Ninguna (es tarea de documentación)

## Definition of Done
- [ ] El archivo README.md en la raíz del proyecto contiene una demo visual
- [ ] La demo visual es un screenshot o GIF de FlowForge en acción
- [ ] La demo está posicionada en la parte superior del README, visible sin scroll
- [ ] La imagen/GIF tiene texto alternativo (alt) descriptivo
- [ ] El peso del archivo visual está optimizado (< 2MB recomendado para GIFs)
- [ ] Las dimensiones son apropiadas para visualización en GitHub

## FlowForge
> This section is managed by FlowForge agents. Do not edit manually.
- HU status lifecycle: `draft` → `in-progress` → `done`
- Status is updated by forge-memory at `/flow-close`

---

## Technical Debt (if applicable)
No aplica — es una tarea de documentación sin impacto en el codebase de producción.
