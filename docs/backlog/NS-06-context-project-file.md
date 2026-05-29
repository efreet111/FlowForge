# NS-06: `context-project.md` en `ia-work/`

> **Estado**: Propuesto  
> **Prioridad**: P1 — UX / onboarding  
> **Fase**: Discovery (CKP-0)  
> **Agente**: `@forge-discovery`

---

## 🎯 Problema que resuelve

### Situación actual

1. **Nuevos team members** pierden 2-3 días entendiendo el proyecto porque:
   - No hay un documento central con el contexto
   - Las memorias están en Engram (requiere saber buscar)
   - Cada persona tiene que "preguntar al agente" para entender

2. **Humans no quieren consultar Engram** para el contexto básico:
   - Engram es ideal para queries específicas ("¿qué decidimos sobre JWT?")
   - Pero es overkill para "¿de qué trata este proyecto?"
   - La barrera de entrada es alta (saber los commands de Engram)

3. **El contexto se pierde** entre sesiones:
   - Cada sesión empieza "de cero" si no hay un archivo de contexto
   - El agente tiene que re-leer todo el código o buscar en Engram
   - Onboarding de nuevos agentes también es lento

### ¿Por qué nace esta necesidad?

**Feedback de usuarios reales**:
> "Me recomendaron que cuando cree un item del backlog escribamos también por qué nace, y qué problema está resolviendo."

Esta recomendación viene de la práctica de **documentar el problema antes de la solución**. Sin el "por qué":
- Los items del backlog son una lista de deseos sin contexto
- Es difícil priorizar (¿por qué esto es importante?)
- Los nuevos colaboradores no entienden la motivación

---

## 📊 Impacto

| Stakeholder | Beneficio |
|-------------|-----------|
| **Nuevos devs** | Onboarding de horas en lugar de días |
| **Team leads** | Menos tiempo explicando lo mismo |
| **Agentes AI** | Contexto inmediato sin buscar en Engram |
| **Humans** | Pueden leer el contexto sin herramientas especiales |

---

## 🎯 Objetivo

Crear un archivo **`ia-work/context-project.md`** que:

1. **Existe desde el día 1** del proyecto (after Discovery)
2. **Vive en el repo** (git history, versionado, reviewable)
3. **Se actualiza** cuando hay cambios arquitectónicos mayores
4. **Complementa a Engram**, no lo reemplaza

---

## 📋 Contenido mínimo

```markdown
# Project Context — {project-name}

## Business Goal
[Qué problema resuelve este proyecto, para quién]

## Tech Stack
[Lenguajes, frameworks, DB, infra, versions clave]

## Architecture Overview
[Componentes principales y cómo se relacionan]

## Key Decisions
[Decisiones arquitectónicas que NO pueden cambiar sin discutir]

## Team & Roles
[Quiénes trabajan y en qué área]

## Related Projects
[Dependencias externas o proyectos relacionados]

## Getting Started
[Cómo levantar el proyecto localmente en 5 pasos o menos]
```

---

## 🔄 Relación con Engram

| Tipo de info | Dónde va | Por qué |
|--------------|----------|---------|
| Contexto estructural del proyecto | `ia-work/context-project.md` | **Estable**, no cambia por sesión |
| Sesiones, tool calls, comandos | Engram (`tool_use`, `command`) | **Efímero**, operativo |
| Decisiones diarias, bugfixes | Engram (`decision`, `bugfix`) | **Táctico**, no arquitectónico |
| Patrones establecidos | Engram (`pattern`) + docs/ | **Reutilizable** |

**Regla**: Si la info es **estructural** y no va a cambiar en la próxima sesión → archivo. Si es **operativa** y específica de una sesión → Engram.

---

## 📐 Especificación (pendiente)

- [ ] Definir trigger exacto: ¿al finalizar Discovery o al iniciar Apply?
- [ ] Definir formato exacto del template
- [ ] Definir quién lo actualiza (humano, agente, ambos)
- [ ] Definir si va en `ia-work/` o en `docs/`
- [ ] Integrar con `@forge-discovery` skill

---

## 🧪 Criterios de aceptación

- [ ] `@forge-discovery` genera el archivo al completar CKP-0
- [ ] El archivo tiene el template mínimo completado
- [ ] El archivo se committea a git como parte del entregable de Discovery
- [ ] Nuevos team members pueden entender el proyecto leyéndolo en <10 min
- [ ] El agente puede leer el archivo al iniciar sesión para contexto rápido

---

## 🔗 Referencias

- EngramFlow Level 2: Structured Memory (docs/02-memory-strategy.md)
- Discovery phase (skills/forge-discovery/SKILL.md)
- Feedback de usuario: sesión 2026-05-29
