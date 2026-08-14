# FlowForge Feature Backlog

> **Purpose**: Backlog estructurado de features para FlowForge. Cada feature tiene su propia especificación inicial en `docs/backlog/FF-XXX-*/spec.md`.
>
> **Última actualización**: 2026-08-14
>
> **Fuente**: Análisis de `flowforge-feature-ideas.md` (2026-08)

---

## Estado de features

| ID | Feature | Status | Prioridad | Esfuerzo | Bloqueador | Spec |
|----|---------|--------|-----------|----------|------------|------|
| FF-001 | Code-aware Dev agent | 🔴 Blocked | P1 | XL | ENG-416, ENG-484 | [spec](FF-001-code-aware-dev-agent/spec.md) |
| FF-002 | Code-aware capture (CKP-2) | 🔴 Blocked (Parte B) / 🟢 Ready (Parte A) | P2 | S (A) / L (B) | ENG-416, ENG-483 | [spec](FF-002-code-aware-capture/spec.md) |
| FF-003 | Onboarding flow | 🟢 Ready | P1 | M | Ninguno | [spec](FF-003-onboarding-flow/spec.md) |
| FF-004 | Code-context Arch agent | 🔴 Blocked | P1 | M | ENG-416, ENG-484 | [spec](FF-004-code-context-arch/spec.md) |
| FF-005 | Contradiction detection | 🔴 Blocked | P3 | L | ENG-412, ENG-414 | [spec](FF-005-contradiction-detection/spec.md) |
| FF-006 | Cost dashboard | 🟢 Ready | P4 | L | Ninguno (ROI dudoso) | [spec](FF-006-cost-dashboard/spec.md) |
| FF-007 | Drift health check | 🟢 Ready (lite en verify) | P3 | M (lite) / L (full) | Ninguno | [spec](FF-007-drift-health-check/spec.md) |

---

## Roadmap sugerido

### Corto plazo (implementable HOY)

1. **FF-003** — Onboarding flow (P1, M) — Mayor valor para equipos
2. **FF-002 Parte A** — Extracción de decisiones del plan (P2, S) — Esfuerzo bajo, valor inmediato
3. **FF-007 lite** — Plan completeness en verify agent (P3, M) — Extensión de verify existente

### Mediano plazo (requiere trabajo en engram-dotnet)

4. **ENG-416** — Schema evolution en engram-dotnet (prerequisito)
5. **ENG-484** — Code-context tools en engram-dotnet
6. **FF-001** — Dev agent code-aware (se desbloquea)
7. **FF-004** — Arch agent code-context (se desbloquea)

### Largo plazo (evaluar ROI)

8. **FF-005** — Contradiction detection (solo si hay 500+ memorias)
9. **FF-006** — Cost dashboard (solo si hay necesidad de managers)
10. **FF-007 full** — Drift health check completo (si verify lite no es suficiente)

---

## Dependencias cross-project

```
FlowForge                          engram-dotnet
─────────                          ─────────────
FF-001 ──────────────────────────→ ENG-484 (code-context tools)
FF-002 (Parte B) ────────────────→ ENG-483 (code-aware capture)
FF-004 ──────────────────────────→ ENG-484 (code-context tools)
FF-005 ──────────────────────────→ ENG-414 (contradicción temporal)

Todos los code-aware ────────────→ ENG-416 (schema evolution)
```

---

## Criterios de priorización

| Criterio | Peso |
|----------|------|
| Valor para el usuario | 40% |
| Factibilidad técnica (hoy) | 30% |
| Esfuerzo relativo | 20% |
| Dependencias externas | 10% |

---

## Cómo usar este backlog

1. **Para implementar un feature**: Leer el `spec.md` correspondiente, promover status a "Spec-Ready", iniciar `/flow-start`
2. **Para agregar un feature nuevo**: Crear directorio `FF-XXX-nombre/`, escribir `spec.md`, agregar fila a la tabla
3. **Para re-priorizar**: Actualizar la columna "Prioridad" y mover en el roadmap
