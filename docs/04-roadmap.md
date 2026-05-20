# FlowForge + engram-dotnet — Roadmap Conjunto

> **Última actualización**: 2026-05-19
> **Versión actual (engram-dotnet)**: main (post PR #11)
> **Versión actual (FlowForge)**: 0.2 (diseño conceptual)

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
| **✅ doctor-diagnostic** (14 tasks) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |

---

## Checklist de Documentación Completa

### FlowForge (`docs/`)

| Documento | Estado | Notas |
|-----------|--------|-------|
| `01-engramflow-architecture.md` | ✅ Completo | Diseño v0.2: 4 fases, 5 agentes, 3 checkpoints |
| `02-memory-strategy.md` | ✅ Completo | 2 niveles de memoria, TTL, promoción, Janitor |
| `03-engram-dotnet-gaps.md` | ✅ Actualizado | 4/4 features implementadas |
| `04-roadmap.md` | ✅ Actualizado | Este documento |
| `05-comparison-methodologies.md` | ✅ Completo | 6 metodologías investigadas y justificadas |
| `06-ai-orchestrator.md` | ✅ Actualizado | Rol de orquestador nativo en IDE |
| `07-core-skills.md` | ✅ Completo | Prompts maestros de las 7 skills |
| `08-test-plan.md` | ✅ Actualizado | Plan de testing end-to-end |
| `09-open-source-integration.md` | ✅ Actualizado | Integración con OSS |
| `10-memory-mapping-fallback.md` | ✅ Completo | Degradación graciosa |
| `11-orchestrator-delegation-protocol.md` | ✅ Nuevo | Protocolo multi-agente y .flowforge.json |
| `PRD.md` | ✅ Completo | Visión general del ecosistema FlowForge |
| `README.md` | ✅ Actualizado | Visión general del proyecto |
| `test-matrix.md` | ✅ Completo | 236 tests documentados por feature y tipo |
| `archive/new-workflow-deprecated.md` | 🗄️ Archivado | Pre-EngramFlow, conservado como referencia |
| `project-context.md` | ✅ Actualizado | Refleja estado actual |

### engram-dotnet SDD — Features Archivadas

| Feature | SDD Cycle | Tests | Archivo |
|---------|-----------|-------|---------|
| **verification-tools** (13 tasks) | ✅ Completo | 16 + 52 MCP regresión | `sdd/archive/2026-05-14-verification-tools/` |
| **promotion-level2** (21 tasks) | ✅ Completo | 17 + 139 Store regresión | `sdd/archive/2026-05-14-promotion-level2/` |
| **traceability** (18 tasks) | ✅ Completo | 28 + 52 MCP regresión | `sdd/traceability/` |
| **ttl-configurable** (13 tasks) | ✅ Completo | 22 + 0 regresiones | `sdd/archive/2026-05-14-ttl-configurable/` |
| **doctor-diagnostic** (14 tasks) | ✅ Completo | 27 tests | `sdd/archive/2026-05-17-doctor-diagnostic/` |

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
✅ verification-tools               🔜 CLI Wizard: Configuración de Entorno (npx flowforge init)
✅ promotion-level2                 🔜 Generador de Reglas (generate-rules.sh)
✅ traceability                     🔜 Dashboard web
✅ ttl-configurable                 🔜 Offline-First Sync (Fase 3/4 en progreso)
✅ doctor-diagnostic
✅ spec-7-skills-core
✅ orchestrator-delegation-protocol
```

---

## Features en el Backlog (sin priorizar)

| Feature | Proyecto | Estado SDD | Notas |
|---------|----------|-----------|-------|
| CLI Wizard de Configuración | FlowForge | ⏳ Siguiente en la lista | Script para generar el `.flowforge.json` (modelos, base de datos, persona) de forma interactiva en terminal |
| Generador Automático de Reglas | FlowForge | ⏳ Siguiente en la lista | Script `generate-rules.sh` para compilar las 7 skills en un solo archivo inyectable al IDE |
| Offline-First Sync (PR #14) | engram-dotnet | ✅ SDD completo (archivado) | 32-44h, 4 fases |
| Doctor Diagnostic | engram-dotnet | ✅ SDD completo (archivado) | 27 tests pasando |
| Requirement Traceability | engram-dotnet | ✅ Completo (18 tasks) | 28 tests + 52 MCP regresión |
| TTL Configurable | engram-dotnet | ✅ Completo (13 tasks) | 22 tests + 0 regresiones |
| Backend Config File | engram-dotnet | ⏳ SDD Listo (Specs/Design/Tasks) | Esperando implementación |
| Orquestador AI opcional | FlowForge | ✅ Reemplazado | Reemplazado por Protocolo Multi-Agente Nativo en IDE |
| Protocolo de Delegación | FlowForge | ✅ Completado | Reglas de orquestación multi-agente / monolítico documentadas |
| Model Router MCP server | FlowForge | ❌ Descartado (Delegado al IDE) | Client-side Routing vía `.flowforge.json` |
| Dashboard web | FlowForge | ⏳ Specs/Design Listos | Estructura Vanilla SPA de cero dependencias para visualizar engram-dotnet |
