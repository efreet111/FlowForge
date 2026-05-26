# Especificación de las 7 Skills Core de FlowForge

> **Versión**: 0.3 (actualizado post-OLA 1-4)
> **Última actualización**: 2026-05-25
> **Estado**: Core definido — 23 skills especializadas documentadas en skills/

Este documento define la **lógica operativa y los contratos de fase** de las 7 Skills Core. Las 23 skills especializadas (seguridad, SOLID, patrones, performance, a11y, DDD, migraciones, métricas, etc.) se documentan individualmente en sus archivos `skills/*/SKILL.md`.

> **Nota**: La especificación técnica completa con todas las funciones, flujos y gaps está en [`15-agent-skills-technical-spec.md`](15-agent-skills-technical-spec.md).

---

## 1. Matriz de Contratos de Datos (7 Agentes)

| Agente | Fase | Inputs | Outputs | Skills Especializadas |
|--------|------|--------|---------|----------------------|
| **Orchestrator** | Todas | Prompt, `.flowforge.json` | Delegación + checkpoints | 1 core (sin especializadas) |
| **Discovery** | 0 | Prompt, engram-dotnet | Context Map | security, compliance, cost |
| **Arch** | 1 | Context Map, código | spec.md + Capability Matrix | security, performance, a11y, domain |
| **Plan** | 2 | spec.md, código | plan.md + task checklist | security, patterns, migrations, rollback |
| **Dev** | 3 | plan.md, spec.md | Código + tests (Ralph Wiggum) | security, solid, testing, performance, refactor |
| **Verify** | 3 | spec.md, código, tests | PASS o rework_ticket.md (CKP-3) | security, complexity, performance, a11y |
| **Memory** | 4 | Artefactos finales, logs | Session summary, ADRs, engramas | metrics, changelog, knowledge |

## Checkpoints (CKP-0 → CKP-4)

| CKP | Color | Tipo | Disparador |
|-----|-------|------|-----------|
| CKP-0 | 🔴 HARD STOP | Binario | Requerimiento vago |
| CKP-1 | 🟡 SEMÁFORO | Humano | spec.md generado |
| CKP-2 | 🟡 SEMÁFORO | Humano | plan.md generado |
| CKP-3 | 🔴 EMERGENCIA | Mecánico (3 ciclos) | 3 reworks fallidos |
| CKP-4 | 🟢 DEPLOY | Humano | Feature completa |

---

## Referencias

- Skills especializadas: `skills/forge-{rol}/{especialidad}/SKILL.md`
- Referencia completa: [`14-flowforge-complete-reference.md`](14-flowforge-complete-reference.md)
- Especificación técnica: [`15-agent-skills-technical-spec.md`](15-agent-skills-technical-spec.md)

---

## 1.5 Analogías y Equivalencias Metodológicas

Para equipos que transicionan desde otras metodologías de ingeniería de software o flujos agentic, el comportamiento de las 5 Skills Core de EngramFlow se puede mapear de la siguiente manera:

### 1.5.1 Equivalencia con SDD (Spec-Driven Development)
---

## Nota de Actualización (2026-05-25)

El contenido detallado de este documento (Analogías, Fases 1-7, System Prompts) fue escrito para la versión inicial v0.1 con 5 agentes. La versión actual v0.3 tiene **7 agentes core** y **23 skills especializadas**. 

**La especificación técnica completa y actualizada está en:**
- [`14-flowforge-complete-reference.md`](14-flowforge-complete-reference.md) — Referencia con casos de prueba
- [`15-agent-skills-technical-spec.md`](15-agent-skills-technical-spec.md) — Especificación con funciones, inputs/outputs y 30 gaps

Los System Prompts maestros de cada agente viven en sus respectivos archivos `skills/forge-*/SKILL.md`.
