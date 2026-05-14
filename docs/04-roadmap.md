# EngramFlow + engram-dotnet — Roadmap Conjunto

> **Última actualización**: 2026-05-14
> **Versión actual (engram-dotnet)**: main (post PR #11)
> **Versión actual (EngramFlow)**: 0.2 (diseño conceptual)

---

## Principios del Roadmap

1. **Cada feature es independiente y mergeable por separado**
2. **No hay breaking changes en la API de engram-dotnet** (cambios aditivos)
3. **Lo Simple primero** — features aisladas antes que cambios en el Store layer
4. **Lo complejo se deja para después** (verification tools dependen de formato spec.md que aún puede evolucionar)
5. **TTL es el feature más corto pero requiere completar su SDD primero**

---

## Estado Actual del SDD (14 mayo 2026)

| Feature | Propose | Spec | Design | Tasks | Apply | Verify | Archive |
|---------|---------|------|--------|-------|-------|--------|---------|
| **verification-tools** (13 tasks) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| **promotion-level2** (21 tasks) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| **traceability** (18 tasks) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| **ttl-configurable** (13 tasks) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |

---

## Checklist de Documentación Completa

### FlowForge (`docs/`)

| Documento | Estado | Notas |
|-----------|--------|-------|
| `01-engramflow-architecture.md` | ✅ Completo | Diseño v0.2: 4 fases, 5 agentes, 3 checkpoints |
| `02-memory-strategy.md` | ✅ Completo | 2 niveles de memoria, TTL, promoción, Janitor |
| `03-engram-dotnet-gaps.md` | ⚠️ Parcialmente desactualizado | 3/4 features implementadas, falta TTL |
| `04-roadmap.md` | ✅ Actualizado | Este documento |
| `05-comparison-methodologies.md` | ✅ Completo | 6 metodologías investigadas y justificadas |
| `PRD.md` | ✅ Completo | Visión general del ecosistema FlowForge |
| `README.md` | ✅ Creado | Visión general del proyecto |
| `test-matrix.md` | ✅ Nuevo | 236 tests documentados por feature y tipo |
| `new-workflow.md` | 🟡 Desactualizado | Pre-EngramFlow, conservado como referencia |
| `project-context.md` | ✅ Actualizado | Refleja estado actual |

### engram-dotnet SDD — Features Archivadas

| Feature | SDD Cycle | Tests | Archivo |
|---------|-----------|-------|---------|
| **verification-tools** (13 tasks) | ✅ Completo | 16 + 52 MCP regresión | `sdd/archive/2026-05-14-verification-tools/` |
| **promotion-level2** (21 tasks) | ✅ Completo | 17 + 139 Store regresión | `sdd/archive/2026-05-14-promotion-level2/` |
| **traceability** (18 tasks) | ✅ Completo | 28 + 52 MCP regresión | `sdd/traceability/` |
| **ttl-configurable** | ⬜ Solo proposal | — | `sdd/ttl-configurable/` |

### Arquitectura resultante

```
engram-dotnet/
├── src/
│   ├── Engram.Store/               ← Motor DB + RetentionConfig + TTL prune
│   ├── Engram.Verification/         ← mem_verify_artifact + traceability
│   ├── Engram.MdGeneration/         ← mem_promote_to_md + engram promote
│   ├── Engram.Mcp/                  ← 24 MCP tools total
│   ├── Engram.Server/               ← HTTP API + retention + redirects
│   └── Engram.Cli/                  ← CLI + retention commands
├── tests/
│   ├── Engram.Verification.Tests/   ← 28 tests (trace + verify)
│   ├── Engram.MdGeneration.Tests/   ← 17 tests
│   └── Engram.Store.Tests/          ← 161 tests (22 retention + 139 legacy)
├── sdd/
│   ├── archive/                     ← 4 features completadas
│   └── specs/                       ← 3 specs sincronizados
└── 258 tests totales — 0 fallos
```

---

## Timeline Actualizado

```
✅ COMPLETADO                       🔜 PRÓXIMOS
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
✅ verification-tools               🔜 Offline-First Sync (PR #14)
✅ promotion-level2                 🔜 Doctor Diagnostic (sprint activo)
✅ traceability                     🔜 Backend Config File (SDD proposal)
✅ ttl-configurable
✅ FlowForge docs + rename
```

---

## Features en el Backlog (sin priorizar)

| Feature | Proyecto | Estado SDD | Notas |
|---------|----------|-----------|-------|
| Offline-First Sync (PR #14) | engram-dotnet | SDD completo en progreso | 32-44h, 4 fases |
| Doctor Diagnostic | engram-dotnet | Sprint activo | 4-6h |
| Requirement Traceability | engram-dotnet | ✅ Completo (18 tasks) | 28 tests + 52 MCP regresión |
| TTL Configurable | engram-dotnet | ✅ Completo (13 tasks) | 22 tests + 0 regresiones |
| Backend Config File | engram-dotnet | Solo proposal | 4-6h |
| Orquestador AI opcional | EngramFlow | Solo concepto | Depende de tener features core |
| Model Router MCP server | EngramFlow | Solo concepto | Baja prioridad |
| Dashboard web | EngramFlow | Futuro | Sin fecha |
