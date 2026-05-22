# FlowForge + engram-dotnet — Roadmap Conjunto

> **Última actualización**: 2026-05-21
> **Versión actual (engram-dotnet)**: main (post PR #11)
> **Versión actual (FlowForge)**: 0.3 (fortalecimiento de agentes)

---

## Principios del Roadmap

1. **Cada feature es independiente y mergeable por separado**
2. **No hay breaking changes en la API de engram-dotnet** (cambios aditivos)
3. **Lo Simple primero** — features aisladas antes que cambios en el Store layer
4. **Lo complejo se deja para después** (verification tools dependen de formato spec.md que aún puede evolucionar)
5. **TTL es el feature más corto pero requiere completar su SDD primero**

---

## 🔴 Sistema de Checkpoints (Control Humano) — Normalizado

> **Auditoría**: 2026-05-21 — Se detectó inconsistencia entre los documentos del proyecto. El README decía "3 checkpoints", la arquitectura documentaba "4", y el orquestador usaba "3" con semántica diferente.

### Checkpoints Formalizados (5 puntos de control)

| ID | Fase | Tipo | Ambigüedad | Disparador | Acción |
|----|------|------|-----------|------------|--------|
| **CKP-0** | Discovery | 🔴 HARD STOP (binario) | 0% — no se negocia | Requerimiento vago o sin contexto previo | Frenar todo, pedir clarificación |
| **CKP-1** | Arch / spec.md | 🟡 SEMÁFORO AMARILLO | Aceptable — decide el humano | spec.md generado | *"¿Aprobás o querés ajustar algo?"* |
| **CKP-2** | Plan / plan.md | 🟡 SEMÁFORO AMARILLO | Aceptable — decide el humano | plan.md generado | *"¿Luz verde para codificar?"* |
| **CKP-3** | Verify (Escalation) | 🔴 FRENO DE EMERGENCIA | 0% — mecánico | 3 ciclos de rework fallidos | Detener Inner Loop, revisión manual |
| **CKP-4** | Cierre | 🟢 DEPLOY GATE (implícito) | Aceptable — decide el humano | Memory Agent completó | Decisión de deploy |

### Principios del Sistema de Checkpoints

1. **CKP-0 es el más estricto**: Si el discovery no encuentra contexto O el orquestador detecta ambigüedad de negocio, no se avanza. Es binario e inapelable.
2. **CKP-1 y CKP-2 son flexibles**: El humano puede aprobar specs/planes con partes abiertas. La defensa contra specs débiles es la **Capability Matrix** (items `deterministic`) que el Verify Agent audita en fase 3.
3. **CKP-3 es mecánico**: El contador de ciclos está en el YAML frontmatter de `rework_ticket.md`. Si `cycle_count = 3`, se escala sin interpretación.
4. **El Orquestador AI opcional** puede intervenir en CKP-3 con dos caminos: (A) modificar `plan.md` y resetear ciclo, o (B) escalar al humano.

### Documentos a corregir (deuda documental)

| Documento | Problema | Estado |
|-----------|----------|--------|
| `README.md` | Decía "3 checkpoints humanos" | ✅ Corregido (4 checkpoints + CKP-0 a CKP-4) |
| `01-engramflow-architecture.md` | Checkpoint ③ solapado entre Escalation y Deploy | ✅ Corregido (CKP-3 escalation, CKP-4 deploy) |
| `06-ai-orchestrator.md` | No mencionaba CKP-4 (deploy gate) ni distinguía Hard Stops | ✅ Corregido (tabla CKP, sección 3 actualizada) |
| `forge-orchestrator/SKILL.md` | No distinguía entre CKP-0 y CKP-1/2 en severidad | ✅ Corregido (tabla de checkpoints, reglas de oro) |

---

## Estado Actual del SDD (14 mayo 2026)

| Feature | Propose | Spec | Design | Tasks | Apply | Verify | Archive |
|---------|---------|------|--------|-------|-------|--------|---------|
| **verification-tools** (13 tasks) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| **promotion-level2** (21 tasks) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| **traceability** (18 tasks) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| **ttl-configurable** (13 tasks) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| **✅ doctor-diagnostic** (14 tasks) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| **✅ offline-first-sync** (43 tasks) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| **✅ advanced-engram-integration** (5 tasks) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |

---

## Checklist de Documentación Completa

### FlowForge (`docs/`)

| Documento | Estado | Notas |
|-----------|--------|-------|
| `01-engramflow-architecture.md` | ✅ Corregido (CKP normalizados) | CKP-3 escalation, CKP-4 deploy gate |
| `02-memory-strategy.md` | ✅ Completo | 2 niveles de memoria, TTL, promoción, Janitor |
| `03-engram-dotnet-gaps.md` | ✅ Actualizado | 4/4 features implementadas |
| `04-roadmap.md` | ✅ Actualizado | Este documento (v0.3 — fortalecimiento de agentes) |
| `05-comparison-methodologies.md` | ✅ Completo | 6 metodologías investigadas y justificadas |
| `06-ai-orchestrator.md` | ✅ Corregido (CKP normalizados) | Tabla CKP, Hard Stops vs Semáforo Amarillo |
| `README.md` | ✅ Corregido (CKP normalizados) | 4 checkpoints + CKP-0 → CKP-4 |
| `AGENTS.md` | ⚠️ Requiere actualización | Agregar skills especializadas al índice de agentes |
| `07-core-skills.md` | ✅ Completo | Prompts maestros de las 7 skills |
| `08-test-plan.md` | ✅ Actualizado | Plan de testing end-to-end |
| `09-open-source-integration.md` | ✅ Actualizado | Integración con OSS |
| `10-memory-mapping-fallback.md` | ✅ Completo | Degradación graciosa |
| `11-orchestrator-delegation-protocol.md` | ✅ Nuevo | Protocolo multi-agente y .flowforge.json |
| `12-engram-tool-reference.md` | ✅ Nuevo | Referencia canónica de las 25 herramientas MCP de engram-dotnet en 6 áreas |
| `13-edge-cases-and-risks.md` | ✅ Nuevo | Risk register, failure modes, y preguntas abiertas de la metodología |
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
| **offline-first-sync** (43 tasks) | ✅ Completo | 84 tests (72u + 12i) | `sdd/archive/2026-05-19-offline-first-sync-phase4/` |
| **advanced-engram-integration** (5 tasks) | ✅ Completo | 0 (Doc & Skills) | `/skills/` |

### Arquitectura resultante

> 📖 **Referencia completa de herramientas**: [`12-engram-tool-reference.md`](12-engram-tool-reference.md) — 25 herramientas MCP en 6 áreas.

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
✅ COMPLETADO                       ✅ OLA 2: Calidad y Patrones      🔷 OLA 3
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
✅ verification-tools               ✅ forge-plan-patterns             🔷 forge-discovery-security
✅ promotion-level2                 ✅ forge-dev-testing               🔷 forge-discovery-compliance
✅ traceability                     ✅ forge-dev-performance           🔷 forge-arch-performance
✅ ttl-configurable                 ✅ forge-verify-complexity         🔷 forge-arch-a11y
✅ doctor-diagnostic                ✅ forge-verify-performance        🔷 forge-arch-domain
✅ offline-first-sync               🔜 Generador de Reglas            🔷 forge-plan-migrations
✅ spec-7-skills-core               🔜 Dashboard web                  🔷 forge-plan-rollback
✅ orchestrator-delegation-protocol 🔜 Backend Config File             🔷 forge-dev-refactor
✅ git-repository-bootstrap
✅ advanced-engram-integration
✅ Grupo A: Workflow Core
✅ Grupo B: Security Skills
✅ OLA 2: Quality & Patterns

🔹 OLA 4
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
🔹 forge-discovery-cost
🔹 forge-verify-a11y
🔹 forge-memory-metrics
🔹 forge-memory-changelog
🔹 forge-memory-knowledge
```
```

---

## Features en el Backlog (priorizado)

### 🔥 OLA 1 — Workflow Core + Seguridad Transversal

#### Grupo A: Workflow Core (prioridad máxima — hace funcionar el motor)

| Feature | Proyecto | Estado | Notas |
|---------|----------|--------|-------|
| **Normalizar Documentación de Checkpoints** | FlowForge | 📝 Auditado (2026-05-21) | Corregir README, architecture doc, orchestrator skill, AI orchestrator doc |
| **Pulir Skills Core** | FlowForge | 🔧 En revisión | Alinear forge-orchestrator, forge-discovery, forge-arch, forge-plan, forge-dev, forge-verify, forge-memory con checkpoints normalizados |
| **AGENTS.md — Actualizar índice** | FlowForge | 📝 Pendiente | Agregar checkpoints normalizados + skills especializadas al índice de agentes |
| **CLI Wizard: `forge init`** | FlowForge | 📝 Exploration + SDD parcial | C# Native AOT: config interactiva de `.flowforge.json` |
| **Generador Automático de Reglas** | FlowForge | ⏳ Pendiente | Script para compilar skills en `.cursorrules` / `.clinerules` |

#### Grupo B: Skills de Seguridad (fortalecimiento)

| Feature | Proyecto | Estado | Notas |
|---------|----------|--------|-------|
| **`forge-arch-security`** | FlowForge | ✅ Creado | Threat modeling (STRIDE), RNF de seguridad obligatorios en specs |
| **`forge-plan-security`** | FlowForge | ✅ Creado | Secure-by-design, OWASP ASVS, input validation patterns en planes |
| **`forge-dev-security`** | FlowForge | ✅ Creado | OWASP Top 10, XSS/CSRF/SQLi prevention en codificación |
| **`forge-verify-security`** | FlowForge | ✅ Creado | SAST mental, OWASP checklist, dependency audit en verificación |
| **`forge-dev-solid`** | FlowForge | ✅ Creado | Validación de principios SOLID post-codificación |

### 🔜 OLA 2 — Calidad de Código y Patrones

| Feature | Proyecto | Estado | Notas |
|---------|----------|--------|-------|
| **`forge-plan-patterns`** | FlowForge | ✅ Creado | Catálogo GoF (creational/structural/behavioral), enterprise, cloud-native patterns |
| **`forge-dev-testing`** | FlowForge | ✅ Creado | Property-based testing, fuzzing, mutation testing, integration test patterns |
| **`forge-dev-performance`** | FlowForge | ✅ Creado | N+1 detection, caching patterns (5 strategies), batching, lazy vs eager loading |
| **`forge-verify-complexity`** | FlowForge | ✅ Creado | Cyclomatic complexity, nesting depth, cognitive load, code smell detection |
| **`forge-verify-performance`** | FlowForge | ✅ Creado | N+1 query audit, memory leak detection, benchmark validation, Big-O analysis |
| Generador Automático de Reglas | FlowForge | ⏳ Pendiente | Script para compilar skills en `.cursorrules` |
| Dashboard web | FlowForge | 📝 Specs/Design Listos | Vanilla SPA para visualizar engram-dotnet |
| Backend Config File | engram-dotnet | 📝 SDD Listo (Specs/Design/Tasks) | Configuración por archivo para engram-dotnet |

### 🔷 OLA 3 — Infraestructura y Dominio

| Feature | Proyecto | Estado | Notas |
|---------|----------|--------|-------|
| `forge-discovery-security` | FlowForge | 📋 Nueva | Buscar CVEs, vulnerabilidades conocidas del stack |
| `forge-discovery-compliance` | FlowForge | 📋 Nueva | Implicaciones GDPR, SOC2, HIPAA |
| `forge-arch-performance` | FlowForge | 📋 Nueva | SLAs/SLOs medibles en RNF |
| `forge-arch-a11y` | FlowForge | 📋 Nueva | Requisitos WCAG en specs de UI |
| `forge-arch-domain` | FlowForge | 📋 Nueva | DDD, bounded contexts, ubiquitous language |
| `forge-plan-migrations` | FlowForge | 📋 Nueva | Zero-downtime DB migration strategies |
| `forge-plan-rollback` | FlowForge | 📋 Nueva | Deploy y rollback strategies |
| `forge-dev-refactor` | FlowForge | 📋 Nueva | Catálogo Fowler, code smells, refactoring patterns |

### 🔹 OLA 4 — Métricas y Conocimiento (Post-MVP)

| Feature | Proyecto | Estado | Notas |
|---------|----------|--------|-------|
| `forge-discovery-cost` | FlowForge | 📋 Nueva | Estimar impacto en infraestructura |
| `forge-verify-a11y` | FlowForge | 📋 Nueva | Auditoría WCAG, contraste, navegación por teclado |
| `forge-memory-metrics` | FlowForge | 📋 Nueva | Project health: coverage, deuda técnica, cycle time |
| `forge-memory-changelog` | FlowForge | 📋 Nueva | Auto-generación de release notes y changelogs |
| `forge-memory-knowledge` | FlowForge | 📋 Nueva | Cross-project knowledge graph, ADR cross-referencing |

---

## 🔮 Ideas Futuras (Incubadora — sin priorizar)

Extraídas del análisis de riesgos en [`13-edge-cases-and-risks.md`](13-edge-cases-and-risks.md). Son features que aún necesitan maduración antes de entrar al backlog priorizado.

| Idea | Origen | Notas |
|------|--------|-------|
| **Context Poisoning Guardrail** | Edge Cases §2 | Agente ligero que valida si un engrama viejo sigue siendo relevante antes de que la Fase 2 lo use |
| **Conflict Resolution Agent** | Edge Cases §2 | Watcher que detecta colisiones entre agentes trabajando en el mismo namespace antes del merge |
| **Cost Observability Dashboard** | Edge Cases §2 | Dashboard (consola o web) que muestre costo en USD por fase/épica |
| **Drift Health Check** | Edge Cases §3 | Agente que compara código actual vs plan.md cada N commits y alerta si hay desviación |
| **Message Queue para Escrituras .md** | Edge Cases §1 | Cola secuencial en engram-dotnet para evitar colisiones de escritura en archivos de respaldo |
| **Lineage Enforcement en CKP-3** | Edge Cases §5 | El orquestador bloquea CKP-3 si el linaje de datos no es válido |

---

## Features Completadas / Descartadas

| Feature | Proyecto | Estado | Notas |
|---------|----------|--------|-------|
| Offline-First Sync | engram-dotnet | ✅ Archivado | 84 tests (72u + 12i), 4 fases |
| Doctor Diagnostic | engram-dotnet | ✅ Archivado | 27 tests pasando |
| Requirement Traceability | engram-dotnet | ✅ Archivado | 28 tests + 52 MCP regresión |
| TTL Configurable | engram-dotnet | ✅ Archivado | 22 tests |
| Orquestador AI opcional | FlowForge | ✅ Reemplazado | Reemplazado por Protocolo Multi-Agente Nativo en IDE |
| Protocolo de Delegación | FlowForge | ✅ Completado | Reglas de orquestación multi-agente / monolítico |
| Model Router MCP server | FlowForge | ❌ Descartado | Client-side Routing vía `.flowforge.json` |
