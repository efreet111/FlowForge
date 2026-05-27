# FlowForge + engram-dotnet — Roadmap Conjunto

> **Última actualización**: 2026-05-26
> **Versión actual (engram-dotnet)**: main (post PR #11)
> **Versión actual (FlowForge)**: **0.4.0** — ver [`VERSION.md`](../VERSION.md) y [`CHANGELOG.md`](../CHANGELOG.md)

---

## Principios del Roadmap

1. **Cada feature es independiente y mergeable por separado**
2. **No hay breaking changes en la API de engram-dotnet** (cambios aditivos)
3. **Lo Simple primero** — features aisladas antes que cambios en el Store layer
4. **Lo complejo se deja para después** (verification tools dependen de formato spec.md que aún puede evolucionar)
5. **TTL es el feature más corto pero requiere completar su SDD primero**

---

## Release Gate (antes de hacerlo público)

Mientras FlowForge esté **privado**, la instalación remota desde `raw.githubusercontent.com` devuelve 404 — usar instalación local (`ide/install.ps1` / `install.sh`).

### Obligatorio para “listo para publicar”

| Criterio | Estado | Notas |
|----------|--------|-------|
| **Replicación por documentación** | ✅ | [`QUICKSTART.md`](../QUICKSTART.md), [`ide/README.md`](../ide/README.md), [`docs/18-replicable-demo-definition.md`](18-replicable-demo-definition.md) (runbook) |
| **Paridad IDE v0.4** | ✅ | `ide/shared/workflow-orchestrator-parity.md` + Cursor / Antigravity / VS Code / OpenCode |
| **Instaladores** | ✅ | `ide/install.ps1`, `ide/install.sh` (+ `-ProjectPath` para Antigravity/`.cursor` en repo) |
| **Prueba real del flujo** | ✅ | Caso CRUD completado (demo local); evidencia en [`examples/crud-tareas/`](../examples/crud-tareas/) |
| **Docs CKP coherentes** | 🟡 | CKP-0..4 alineados; item **15** en curso (core docs + ide EN) |
| **Archivos OSS** | ✅ | `LICENSE`, `CONTRIBUTING.md`, `SECURITY.md`, `CODE_OF_CONDUCT.md` |
| **Idioma público** | 🟡 | README/QUICKSTART EN+ES ✅ · core `docs/` EN ✅ · skills especializadas / `docs/05` pendiente |

### Opcional (no bloquea release)

- **Repo demo publicado** (`flowforge-demo-task-manager`): útil como referencia, no requerido si el runbook en docs alcanza.
- **Carpeta `examples/`** en FlowForge: alternativa futura al demo externo.
- **engram-dotnet estable en MCP** (roadmap item **9**): mejora memoria, no bloquea metodología sin Engram.

### i18n — decisión de producto (pendiente)

| Ámbito | Idioma target | Archivo / nota |
|--------|---------------|----------------|
| README principal (GitHub) | **English** | `README.md` |
| Entrada en español | **Español** | `README.es.md` (mantener como puerta de entrada LATAM) |
| `QUICKSTART.md`, `docs/*`, `skills/*/SKILL.md`, `ide/*` | **English** | Migración por fases; mensajes de producto en APIs pueden quedar ES en ejemplos |
| Comandos `/flow-*` y CKP gates | Bilingüe mínimo | Los prompts al humano en gates pueden conservar ES en `README.es.md` |

---

## Plan de mejora — qué sacar del backlog para v0.4 “listo”

> Los 14 items completos son **post-MVP**. Para publicar v0.4, enfocarse solo en lo marcado **RELEASE**.

### ✅ Ya hecho (sacar del “pendiente crítico”)

| Item roadmap | Qué era |
|--------------|---------|
| **4** QUICKSTART | Existe + modo privado |
| **7** install.ps1 / install.sh | Implementados con paridad + compile agents |
| **2** Caso CRUD | ✅ Completado en demo local |
| **3** Artefactos ejemplo | ✅ [`examples/crud-tareas/`](../examples/crud-tareas/) |

### 🔴 RELEASE — hacer antes de publicar

| Item | Acción concreta |
|------|-----------------|
| **15** Auditoría docs | 🟡 Core EN + `ide/` EN; pendiente `docs/05`, skills especializadas |
| **i18n** | 🟡 README/QUICKSTART EN+ES; ver [`I18N.md`](I18N.md) |
| **8** (mínimo) | 1 smoke por IDE: install → `/flow-start` en proyecto vacío |
| **1** | OpenCode: merge `opencode.flowforge.json` + rutas `{file:...}` documentadas |

### 🟡 Post-release (dejar en roadmap, no bloquear)

| Items | Motivo |
|-------|--------|
| **5** Template repo, **6** `.flowforge.json` schema | Adopción avanzada |
| **9** engram MCP diagnóstico | Infra opcional |
| **10–14** KPIs, migración Scrum, semver releases | Maduración |
| **Demo repo público** | Opcional |

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
✅ COMPLETADO — Skills                      🚀 SEMANA 1: MVP          🚀 SEMANA 2-3: Adopción
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
✅ 7 Skills Core                              🚀 1. Probar OpenCode     🚀 4. QUICKSTART.md
✅ OLA 1: Security + SOLID (5)                🚀 2. Caso CRUD real      🚀 5. Template proyecto
✅ OLA 2: Quality + Patterns (5)              🚀 3. Artefactos demo     🚀 6. Schema flowforge.json
✅ OLA 3: Infrastructure & Domain (8)         🚀 9. Diagnosticar engram  🚀 7. install.sh
✅ OLA 4: Metrics & Knowledge (5)                                         🚀 8. Probar IDE files
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```
✅ COMPLETADO — 30 Skills                         🔧 PRÓXIMOS
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
✅ 7 Skills Core                                  🔧 Generador de Reglas
✅ OLA 1: Security + SOLID (5)                    🔧 Dashboard web
✅ OLA 2: Quality + Patterns (5)                  🔧 Backend Config File
✅ OLA 3: Infrastructure & Domain (8)             🔧 CLI Wizard (forge init)
✅ OLA 4: Metrics & Knowledge (5)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```
```

---

## Backlog de Skills Especializadas

Todas las 23 skills especializadas + 1 teacher están **completadas** (7 core + 23 especializadas + 1 teacher = 31 total).

Ver detalle en [`15-agent-skills-technical-spec.md`](15-agent-skills-technical-spec.md) y estructura en `skills/`.

El foco actual está en el **[Plan de Mejora — 14 items](#🚀-plan-de-mejora--14-items-para-llevar-flowforge-a-producción)** que antecede a esta sección.

---

## 🚀 Plan de Mejora — 14 items para llevar FlowForge a Producción

> **Análisis objetivo**: 2026-05-26 — El proyecto es 96% documentación, 4% código. No se ha probado con un proyecto real. Estos items son necesarios para que FlowForge sea usable en el día a día.

### ⏰ Semana 1 — MVP Funcional (probar que existe)

| Item | Prioridad | Estado | Notas |
|------|-----------|--------|-------|
| **1. Probar ide/opencode en tu OpenCode** | P0 | 📋 Pendiente | Smoke en Linux: bundle + `/flow-start` (usuario usa OpenCode + modelos free) |
| **2. Ejecutar Caso 1 (CRUD) de docs/14** | P0 | ✅ Hecho | Demo local completado según expectativa |
| **3. Crear artefactos de ejemplo** | P0 | ✅ Hecho | `examples/crud-tareas/` en repo |

### ⏰ Semana 2 — Onboarding Mínimo

| Item | Prioridad | Estado | Notas |
|------|-----------|--------|-------|
| **4. QUICKSTART.md de 1 página** | P0 | ✅ Hecho | Instalación local/pública + primer `/flow-start` |
| **15. Auditoría y limpieza de documentación** | P0 | 🟡 En curso | Core docs + ide EN; skills core EN; pendiente especializadas y `docs/05` |
| **5. Template de proyecto FlowForge** | P1 | 📋 Pendiente | Repo separado (o branch) con `.flowforge.json`, `.ai-work/`, estructura pre-creada |
| **6. Schema de `.flowforge.json`** | P1 | 📋 Pendiente | Documentar el schema completo con modelos, persona, DB, teacher_mode, SAST config |

### ⏰ Semana 3 — Instalación y Testing

| Item | Prioridad | Estado | Notas |
|------|-----------|--------|-------|
| **7. install.sh / install.ps1** | P0 | ✅ Hecho | Paridad v0.4, shared parity, `-ProjectPath`, compile agents |
| **8. Probar IDE files en todos los IDEs** | P1 | 📋 Pendiente | Verificar que los 20 archivos de `ide/` funcionan en OpenCode, Cursor, Antigravity, VS Code |
| **9. Diagnóstico de engram-dotnet** | P0 | 📋 Pendiente | La memoria no conecta en varias sesiones — diagnosticar: ¿MCP? ¿red? ¿servidor? |
| **10. Manejo de features concurrentes** | P2 | 📋 Pendiente | Protocolo para cuando dos devs trabajan features que tocan el mismo código (lock, merge, conflict) |

### ⏰ Semana 4 — Medición y Maduración

| Item | Prioridad | Estado | Notas |
|------|-----------|--------|-------|
| **11. KPIs de efectividad** | P2 | 📋 Pendiente | Definir métricas: tiempo por feature (ciclo completo), bugs post-deploy, rework rate, tokens consumidos |
| **12. 3 features con FlowForge vs ad-hoc** | P2 | 📋 Pendiente | Correr 3 features con FlowForge y 3 sin, comparar KPIs. Evidencia empírica. |
| **13. Guía de migración** | P2 | 📋 Pendiente | "De Scrum a FlowForge", "De ad-hoc a FlowForge" — tablas de equivalencia |
| **14. Release versioning** | P3 | 📋 Pendiente | Adoptar semver, publicar releases en GitHub con changelog |

### 📊 Resumen de Prioridades

| Prioridad | Items | Semana |
|-----------|-------|--------|
| **P0 — Hace que FlowForge funcione de verdad** | 1, 2, 3, 4, 7, 9 | Semana 1-3 |
| **P1 — Hace que FlowForge sea adoptable** | 5, 6, 8 | Semana 2-3 |
| **P2 — Hace que FlowForge sea medible** | 10, 11, 12, 13 | Semana 4 |
| **P3 — Ecosistema** | 14 | Post-MVP |

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
