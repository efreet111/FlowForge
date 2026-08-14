# FlowForge — Backlog Consolidado

> **Última actualización**: 2026-08-14  
> **Versión actual**: v0.1.0-alpha.13  
> **Total de items**: 19 (8 NS-* + 7 FF-* + 4 Roadmap)

---

## 📊 Resumen ejecutivo

| Categoría | Total | ✅ Done | 🔄 In Progress | 📋 Ready | 🔴 Blocked | ⏸️ Deferred |
|-----------|-------|---------|----------------|----------|------------|-------------|
| **NS-*** (Non-Functional/Methodology) | 8 | 3 | 2 | 2 | 0 | 1 |
| **FF-*** (Feature Ideas) | 7 | 0 | 0 | 3 | 4 | 0 |
| **Roadmap** (OSS/Post-release) | 4 | 3 | 0 | 1 | 0 | 0 |
| **TOTAL** | **19** | **6** | **2** | **6** | **4** | **1** |

---

## 🎯 Estado actual por categoría

### ✅ Completados (6 items)

| ID | Feature | Fecha | Impacto |
|----|---------|-------|---------|
| NS-07 | Pattern Search step | 2026-06-18 | Metodología: búsqueda obligatoria de patrones existentes |
| NS-09 | Executive Summary in spec.md | 2026-08-07 | UX: resumen ejecutivo de 15-20 líneas en cada spec |
| NS-10 (P0) | Test Quality Gates | 2026-08-07 | Calidad: validación de assertions + coverage gate |
| FF-OSS-01 | Fix versión en README | 2026-06-23 | Docs: consistencia de versión |
| FF-OSS-02 | Commit de roadmap | 2026-06-23 | Docs: marcar repo como público |
| FF-OSS-03 | Tabla de decisión de instalación | 2026-06-23 | UX: guía de instalación para nuevos usuarios |

### 🔄 En progreso (2 items)

| ID | Feature | Status | Próximo paso | Esfuerzo |
|----|---------|--------|--------------|----------|
| NS-06 | Context project file | Propuesto | Definir trigger y template | S (1 día) |
| NS-08 | Agent Quality Improvement | Propuesto | Implementar Phase 1 (critical fixes) | M (1-2 días) |

### 📋 Ready para implementar (6 items)

| ID | Feature | Prioridad | Esfuerzo | Bloqueador | Valor |
|----|---------|-----------|----------|------------|-------|
| **FF-003** | Onboarding flow | P1 | M (1-2 días) | Ninguno | Alto para equipos |
| **FF-002 (Parte A)** | Extracción de decisiones del plan | P2 | S (1 día) | Ninguno | Medio |
| **FF-007 (lite)** | Drift check en verify agent | P3 | M (1 día) | Ninguno | Medio |
| **NS-09** | Executive Summary | P1 | S (1 día) | Ninguno | Alto (feedback usuario) |
| **NS-10 (P1)** | Mutation Testing | P2 | L (3-4 días) | Baseline de P0 | Medio |
| **FF-OSS-04** | Demo visual en README | P1 | L (4 horas) | Ninguno | Alto para OSS |

### 🔴 Blocked (4 items)

| ID | Feature | Bloqueador | Dependencia | Esfuerzo |
|----|---------|------------|-------------|----------|
| **FF-001** | Code-aware Dev agent | ENG-416, ENG-484 | engram-dotnet | XL (2-3 semanas) |
| **FF-002 (Parte B)** | Code-aware capture | ENG-416, ENG-483 | engram-dotnet | L (2-3 semanas) |
| **FF-004** | Code-context Arch agent | ENG-416, ENG-484 | engram-dotnet | M (2-3 días) |
| **FF-005** | Contradiction detection | ENG-412, ENG-414 | engram-dotnet | L (1-2 días) |

### ⏸️ Deferred (1 item)

| ID | Feature | Razón | Re-evaluar |
|----|---------|-------|------------|
| **FF-006** | Cost dashboard | ROI dudoso, demanda no validada | Cuando haya 10+ equipos usando FlowForge |

---

## 🗺️ Mapa de relaciones

```
NS-06 (Context project) ──────────────────────┐
                                               │
NS-07 (Pattern Search) ✅ ─────────────────────┤
                                               │
NS-08 (Agent Quality) ─────────────────────────┤
                                               │
NS-09 (Executive Summary) ✅ ──────────────────┤
                                               │
NS-10 (Test Quality) ✅ P0 / 📋 P1 ────────────┤
                                               │
FF-001 (Code-aware Dev) 🔴 ───────────────────→│ ENG-416, ENG-484 (engram)
FF-002 (Code-aware capture) 🔴 ───────────────→│ ENG-416, ENG-483 (engram)
FF-003 (Onboarding) 📋 ────────────────────────┤
FF-004 (Code-context Arch) 🔴 ────────────────→│ ENG-416, ENG-484 (engram)
FF-005 (Contradiction) 🔴 ────────────────────→│ ENG-412, ENG-414 (engram)
FF-006 (Cost dashboard) ⏸️ ────────────────────┤
FF-007 (Drift check) 📋 ───────────────────────┤
                                               │
FF-OSS-04 (Demo visual) 📋 ────────────────────┘
```

---

## 🎯 Roadmap recomendado

### Corto plazo (próximas 2-4 semanas)

#### Sprint 1: Quick wins (1 semana)

1. **FF-003** — Onboarding flow (P1, M, 1-2 días)
   - Mayor valor para equipos
   - Sin bloqueadores
   - Spec completa lista en `docs/backlog/FF-003-onboarding-flow/spec.md`

2. **FF-OSS-04** — Demo visual en README (P1, L, 4 horas)
   - Alto impacto para OSS adoption
   - Sin bloqueadores
   - Puede ser screenshot o GIF

3. **NS-08** — Agent Quality Improvement Phase 1 (P1, M, 1-2 días)
   - Critical fixes: traducciones ES→EN, OpenCode stubs, VS Code RF/RNF
   - Sin bloqueadores
   - Mejora calidad de agentes inmediatamente

#### Sprint 2: Foundation (1-2 semanas)

4. **FF-002 Parte A** — Extracción de decisiones del plan (P2, S, 1 día)
   - Esfuerzo bajo, valor inmediato
   - Sin bloqueadores
   - Spec completa lista

5. **FF-007 lite** — Drift check en verify agent (P3, M, 1 día)
   - Extensión de verify existente
   - Sin bloqueadores
   - Spec completa lista

6. **NS-06** — Context project file (P1, S, 1 día)
   - Complementa FF-003 (Onboarding)
   - Sin bloqueadores
   - Spec incompleta (requiere definir trigger y template)

### Mediano plazo (1-3 meses)

7. **NS-10 P1** — Mutation Testing (P2, L, 3-4 días)
   - Requiere baseline de P0 (ya implementado)
   - Spec completa lista

8. **FF-005** — Contradiction detection (P3, L, 1-2 días)
   - Solo si hay 500+ memorias en engram
   - Requiere ENG-412/414 en engram-dotnet

### Largo plazo (3-6 meses)

9. **ENG-416** — Schema evolution en engram-dotnet (prerequisito para FF-001/002/004)
10. **ENG-484** — Code-context tools en engram-dotnet
11. **FF-001** — Dev agent code-aware (se desbloquea)
12. **FF-004** — Arch agent code-context (se desbloquea)

---

## 📈 Análisis de valor vs esfuerzo

```
Alto valor │ FF-003 (P1, M)        NS-08 (P1, M)
           │ FF-OSS-04 (P1, L)     NS-06 (P1, S)
           │ 
           │ FF-002A (P2, S)       FF-007 (P3, M)
           │
           │ NS-10 P1 (P2, L)      FF-006 (P4, L)
           │
Bajo valor │ FF-001 (P1, XL) 🔴    FF-005 (P3, L) 🔴
           └────────────────────────────────────────
             Bajo esfuerzo              Alto esfuerzo
```

**Cuadrante superior-izquierdo** (alto valor, bajo esfuerzo):
- FF-003 (Onboarding) — P1, M
- FF-OSS-04 (Demo visual) — P1, L
- NS-08 (Agent Quality) — P1, M

**Cuadrante superior-derecho** (alto valor, alto esfuerzo):
- FF-001 (Code-aware Dev) — P1, XL 🔴 (blocked)
- FF-005 (Contradiction) — P3, L 🔴 (blocked)

---

## 🔍 Análisis detallado por item

### NS-* (Non-Functional/Methodology)

| ID | Feature | Status | Prioridad | Esfuerzo | Spec |
|----|---------|--------|-----------|----------|------|
| NS-06 | Context project file | 📋 Propuesto | P1 | S (1 día) | [spec](NS-06-context-project-file.md) — incompleta |
| NS-07 | Pattern Search step | ✅ Done | P0 | S | [spec](NS-07-pattern-search-step.md) |
| NS-08 | Agent Quality Improvement | 🔄 Propuesto | P1 | M (1-2 días) | [spec](NS-08-agent-quality-improvement.md) |
| NS-09 | Executive Summary in spec.md | ✅ Done | P1 | S | [spec](NS-09-executive-summary-in-spec.md) |
| NS-10 | Test Quality Gates | ✅ P0 / 📋 P1 | P0/P1 | L (3-4 días P1) | [spec](NS-10-mutation-testing.md) |

**Observaciones**:
- NS-06 tiene spec incompleta (falta definir trigger exacto, template, quién lo actualiza)
- NS-08 tiene spec completa con 3 fases (Critical, High, Medium priority)
- NS-10 P0 está implementado, P1 (mutation testing) pendiente de baseline

### FF-* (Feature Ideas)

| ID | Feature | Status | Prioridad | Esfuerzo | Spec |
|----|---------|--------|-----------|----------|------|
| FF-001 | Code-aware Dev agent | 🔴 Blocked | P1 | XL | [spec](FF-001-code-aware-dev-agent/spec.md) |
| FF-002 | Code-aware capture (CKP-2) | 🟡 Partial | P2 | S/L | [spec](FF-002-code-aware-capture/spec.md) |
| FF-003 | Onboarding flow | 📋 Ready | P1 | M | [spec](FF-003-onboarding-flow/spec.md) |
| FF-004 | Code-context Arch agent | 🔴 Blocked | P1 | M | [spec](FF-004-code-context-arch/spec.md) |
| FF-005 | Contradiction detection | 🔴 Blocked | P3 | L | [spec](FF-005-contradiction-detection/spec.md) |
| FF-006 | Cost dashboard | ⏸️ Deferred | P4 | L | [spec](FF-006-cost-dashboard/spec.md) |
| FF-007 | Drift health check | 📋 Ready | P3 | M/L | [spec](FF-007-drift-health-check/spec.md) |

**Observaciones**:
- FF-001, FF-002 (Parte B), FF-004 comparten mismas dependencias de engram-dotnet
- FF-002 tiene Parte A (implementable hoy) y Parte B (blocked)
- FF-003 es el feature con mejor relación esfuerzo/valor para equipos
- FF-006 tiene ROI dudoso, requiere validación de demanda

### Roadmap (OSS/Post-release)

| ID | Feature | Status | Prioridad | Esfuerzo |
|----|---------|--------|-----------|----------|
| FF-OSS-01 | Fix versión en README | ✅ Done | P0 | XS |
| FF-OSS-02 | Commit de roadmap | ✅ Done | P0 | XS |
| FF-OSS-03 | Tabla de decisión de instalación | ✅ Done | P1 | S |
| FF-OSS-04 | Demo visual en README | 📋 Ready | P1 | L (4 horas) |

**Observaciones**:
- FF-OSS-01 a 003 están completados
- FF-OSS-04 es el único pendiente, alto impacto para OSS adoption

---

## 🎯 Recomendación de priorización

### Criterios de priorización

| Criterio | Peso |
|----------|------|
| Valor para el usuario | 40% |
| Factibilidad técnica (hoy) | 30% |
| Esfuerzo relativo | 20% |
| Dependencias externas | 10% |

### Top 5 recomendados (orden de implementación)

1. **FF-003** — Onboarding flow
   - **Por qué**: Mayor valor para equipos, sin bloqueadores, spec completa
   - **Cuándo**: Sprint 1 (próximos 1-2 días)
   - **Comando**: `/flow-start ff-003-onboarding-flow`

2. **FF-OSS-04** — Demo visual en README
   - **Por qué**: Alto impacto para OSS adoption, esfuerzo bajo (4 horas)
   - **Cuándo**: Sprint 1 (puede hacerse en paralelo con FF-003)
   - **Comando**: No requiere `/flow-start`, es tarea de documentación

3. **NS-08** — Agent Quality Improvement Phase 1
   - **Por qué**: Critical fixes que afectan calidad de agentes
   - **Cuándo**: Sprint 1 (después de FF-003)
   - **Comando**: `/flow-start ns-08-agent-quality-phase-1`

4. **FF-002 Parte A** — Extracción de decisiones del plan
   - **Por qué**: Esfuerzo bajo (S), valor inmediato
   - **Cuándo**: Sprint 2 (próxima semana)
   - **Comando**: `/flow-start ff-002-parte-a-decision-extraction`

5. **FF-007 lite** — Drift check en verify agent
   - **Por qué**: Extensión natural de verify, sin bloqueadores
   - **Cuándo**: Sprint 2 (después de FF-002 Parte A)
   - **Comando**: `/flow-start ff-007-lite-drift-check`

---

## 📊 Métricas de salud del backlog

| Métrica | Valor | Target | Status |
|---------|-------|--------|--------|
| Items completados (últimos 30 días) | 6 | 4-6 | ✅ On track |
| Items blocked | 4 | <3 | ⚠️ Depende de engram-dotnet |
| Items sin spec | 1 (NS-06) | 0 | ⚠️ Requiere completar spec |
| Esfuerzo promedio por item | M (2-3 días) | M | ✅ Healthy |
| Ratio valor/esfuerzo | 70% alto valor | >60% | ✅ Healthy |

---

## 🔗 Enlaces rápidos

### Specs completadas
- [NS-06](NS-06-context-project-file.md) — incompleta
- [NS-07](NS-07-pattern-search-step.md) — ✅ done
- [NS-08](NS-08-agent-quality-improvement.md) — 🔄 proposed
- [NS-09](NS-09-executive-summary-in-spec.md) — ✅ done
- [NS-10](NS-10-mutation-testing.md) — ✅ P0 done / 📋 P1 pending

### Feature specs
- [FF-001](FF-001-code-aware-dev-agent/spec.md) — 🔴 blocked
- [FF-002](FF-002-code-aware-capture/spec.md) — 🟡 partial
- [FF-003](FF-003-onboarding-flow/spec.md) — 📋 ready
- [FF-004](FF-004-code-context-arch/spec.md) — 🔴 blocked
- [FF-005](FF-005-contradiction-detection/spec.md) — 🔴 blocked
- [FF-006](FF-006-cost-dashboard/spec.md) — ⏸️ deferred
- [FF-007](FF-007-drift-health-check/spec.md) — 📋 ready

### Master backlogs
- [FF-BACKLOG.md](FF-BACKLOG.md) — Feature ideas backlog
- [04-roadmap.md](../04-roadmap.md) — Roadmap general

---

## 📝 Notas finales

### Dependencias críticas

**engram-dotnet** es el principal bloqueador:
- 4 features de FlowForge dependen de cambios en engram-dotnet
- ENG-416 (schema evolution) es el prerequisito más importante
- Si engram-dotnet prioriza ENG-416/483/484, se desbloquean FF-001/002/004

### Riesgos

| Riesgo | Probabilidad | Impacto | Mitigación |
|--------|--------------|---------|------------|
| engram-dotnet no prioriza ENG-416 | Alto | Alto | FF-003/002A/007 no dependen de engram |
| NS-06 spec incompleta retrasa implementación | Medio | Bajo | Completar spec antes de implementar |
| FF-006 (Cost dashboard) no tiene demanda | Alto | Bajo | Validar con usuarios antes de implementar |

### Oportunidades

- **FF-003 (Onboarding)** puede ser el "killer feature" para equipos
- **FF-002 Parte A** es quick win (S effort, valor inmediato)
- **NS-08 Phase 1** mejora calidad de agentes inmediatamente
- **FF-OSS-04** puede aumentar OSS adoption significativamente

---

**Próximo paso recomendado**: Iniciar FF-003 (Onboarding flow) con `/flow-start ff-003-onboarding-flow`
