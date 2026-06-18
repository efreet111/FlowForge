# Plan de Mejora — Especificación Detallada de los 14 Items

> **Propósito**: Tener claro qué hay que hacer, cómo y cuándo en cada item del plan de mejora.
> **Versión**: 1.0 — 2026-05-26
> **Roadmap**: [`04-roadmap.md`](04-roadmap.md) § Plan de Mejora

---

## ⏰ SEMANA 1 — MVP Funcional (P0)

---

### Item 1: Probar `ide/opencode/opencode.flowforge.json` en OpenCode

**Objetivo**: Verificar que los 7 subagentes de FlowForge se cargan correctamente en OpenCode y pueden ser invocados mediante delegación.

**Criterios de éxito**:
- [ ] Los 7 agentes aparecen como subagentes disponibles en OpenCode
- [ ] Cada agente carga su core skill correctamente (`{file:...}` apunta al SKILL.md correcto)
- [ ] El orchestrator (agente primario) puede delegar a cada subagente
- [ ] forge-verify puede ejecutar `npm test` / `dotnet test` según el proyecto
- [ ] **CI validates structure**: `.github/workflows/opencode-smoke.yml` runs on every PR and validates JSON syntax, 7 agents present, skill paths exist
- [ ] **Manual test validates runtime**: human invokes OpenCode and verifies agents load correctly (CI cannot test runtime agent loading)

**Enfoque**:
1. Mergear las entradas de `agent` del `opencode.flowforge.json` en el `~/.config/opencode/opencode.json` activo
2. Verificar que los paths `{file:.../skills/...}` existen (no deben estar hardcodeados a una máquina específica)
3. Invocar a cada subagente manualmente y verificar que cargue la skill correcta
4. Probar un ciclo completo: discovery → arch → plan → dev → verify → memory

**Dependencias**: Ninguna. Solo tener OpenCode funcionando.

**Esfuerzo**: 30-45 min

**Deliverables**:
- Confirmación de que los 7 agentes funcionan
- O ajustes al `opencode.flowforge.json` si algo falla

---

### Item 2: Ejecutar Caso 1 (CRUD) de docs/14 con un proyecto real

**Objetivo**: Probar el flujo FlowForge de punta a punta con un proyecto real (Task Manager API) para validar que la teoría funciona en la práctica.

**Criterios de éxito**:
- [ ] Discovery genera Context Map con búsqueda en memoria
- [ ] CKP-0 se supera (requerimiento claro)
- [ ] Arch escribe spec.md con RF + RNF + Capability Matrix
- [ ] CKP-1: humano aprueba spec
- [ ] Plan escribe plan.md con checklist ordenado
- [ ] CKP-2: humano da luz verde
- [ ] Dev implementa código + tests (Ralph Wiggum en verde)
- [ ] Verify audita y emite PASS
- [ ] CKP-3: PASS (cycle_count = 0)
- [ ] Memory escribe session summary
- [ ] CKP-4: humano decide deploy
- [ ] Tiempo total < 30 min (con modelo rápido)

**Enfoque**:
1. Crear proyecto Node.js/TypeScript vacío: `mkdir task-manager-api && cd task-manager-api && npm init -y`
2. Ejecutar el flujo manualmente siguiendo la secuencia de Fases 0-4
3. Documentar cada paso: qué funcionó, qué no, qué faltó

**Dependencias**: Item 1 (OpenCode funcionando)

**Esfuerzo**: 1-2 h (incluye crear el proyecto + ejecutar el flujo)

**Deliverables**:
- Proyecto `task-manager-api/` con los artefactos generados
- Notas de qué gaps aparecieron en la práctica

---

### Item 3: Crear artefactos de ejemplo de un ciclo completo

**Objetivo**: Tener spec.md, plan.md y verify-report.md reales producidos por FlowForge para mostrar a otros miembros del equipo.

**Criterios de éxito**:
- [ ] spec.md con RF-001 a RF-N, escenarios GWT, Capability Matrix, RNFs
- [ ] plan.md con análisis de impacto, contratos, checklist topológico
- [ ] verify-report.md con veredicto, cobertura de tests, observaciones
- [ ] Los artefactos están en un lugar accesible (`.ai-work/{feature-slug}/` o runbook en docs/18)

**Enfoque**:
1. Del Item 2, extraer los artefactos generados
2. Limpiarlos para que sirvan como plantilla/referencia
3. Documentar el runbook en docs/18 (repo demo público opcional)

**Dependencias**: Item 2 (para tener artefactos reales)

**Esfuerzo**: 30 min (limpiar y documentar lo generado en Item 2)

**Deliverables**:
- `.ai-work/{feature-slug}/spec.md`
- `.ai-work/{feature-slug}/plan.md`
- `.ai-work/{feature-slug}/verify-report.md`

---

### Item 9: Diagnosticar engram-dotnet

**Objetivo**: Determinar por qué la memoria persistente (engram-dotnet MCP) no conecta en varias sesiones, y resolverlo o documentar el blocker.

**Criterios de éxito**:
- [ ] Ejecutar `mem_doctor` y obtener diagnóstico completo
- [ ] Identificar si es problema de red (servidor caído), MCP (tool no encontrado), o config (path mal)
- [ ] Si se puede resolver: dejarlo funcionando
- [ ] Si no: documentar el blocker con pasos para resolverlo

**Enfoque**:
1. Revisar el MCP config en `~/.config/opencode/opencode.json` → `mcp.engram`
2. Verificar que `engram mcp` está disponible en el PATH
3. Verificar que `ENGRAM_URL` apunta al servidor correcto
4. Ejecutar `mem_doctor` desde el agente
5. Probar `mem_save` y `mem_search`
6. Si falla: revisar logs del servidor engram-dotnet

**Dependencias**: Acceso al servidor engram-dotnet

**Esfuerzo**: 30 min - 2 h (depende de si el servidor está caído)

**Deliverables**:
- Diagnóstico escrito o servidor funcionando

---

## ⏰ SEMANA 2 — Onboarding Mínimo (P0-P1)

---

### Item 4: QUICKSTART.md de 1 página

**Objetivo**: Cualquier persona nueva debe poder leer FlowForge en 5 minutos y saber cómo empezar. Sin leer 16 documentos.

**Criterios de éxito**:
- [ ] Se lee en < 5 minutos
- [ ] Responde: ¿qué es FlowForge?, ¿qué necesito instalar?, ¿cómo empiezo?
- [ ] Incluye ejemplo de prompt para arrancar una feature
- [ ] Tiene links a docs de profundización

**Enfoque**:
1. Escribir en la raíz del repo como `QUICKSTART.md`
2. Estructura: ¿Qué es? → ¿Qué necesito? → ¿Cómo empiezo? → ¿Y ahora qué?
3. Links a: docs/14 (casos de prueba), docs/15 (technical spec), ide/ (instalación)

**Dependencias**: Items 1-3 (para poder decir "probado con X proyecto")

**Esfuerzo**: 1 h

**Deliverables**:
- `QUICKSTART.md` en la raíz del repo

---

### Item 5: Template de proyecto FlowForge

**Objetivo**: Tener un repo template (GitHub template o branch) que cualquier equipo pueda clonar y tener FlowForge pre-configurado.

**Decisión de scaffolding:** [`ADR-002-scaffold-doc-policy.md`](decisions/ADR-002-scaffold-doc-policy.md) — qué genera `flow-init` en `AGENTS.md`, `docs/DEVELOPMENT.md`, `docs/decisions/`.

**Criterios de éxito**:
- [ ] Tiene `.flowforge.json` con configuración base
- [ ] Tiene `.ai-work/` con estructura de carpetas
- [ ] Tiene `.gitignore` que excluya `.engram/local_memory/`
- [ ] Incluye `AGENTS.md` o `.cursor/rules/` según IDE
- [ ] Viene con QUICKSTART.md

**Enfoque**:
1. Forkear un repo base o crear branch `template` en el repo actual
2. Poner estructura mínima con comentarios en cada archivo
3. Documentar en QUICKSTART cómo usarlo

**Dependencias**: Items 1-4 (saber qué funciona antes de templatearlo)

**Esfuerzo**: 2 h

**Deliverables**:
- Branch `template/` o repo separado `flowforge-template`

---

### Item 15: Auditoría y limpieza de documentación

**Objetivo**: Revisar los 17 documentos de `docs/`, identificar redundancias, contenido obsoleto, fusionar lo que corresponda, archivar lo que no aporta. Demasiada documentación desactualizada es ruido que perjudica la adopción.

**Criterios de éxito**:
- [ ] Cada doc revisado tiene estado: ✅ OK / 🔧 Requiere actualización / 🗄️ Archivar
- [ ] Docs con "5 agentes", "3 checkpoints", "v0.1", "2026-05-13" actualizados o archivados
- [ ] Docs pequeños (< 50 líneas) fusionados con docs principales cuando tenga sentido
- [ ] Docs huérfanos (sin enlaces desde otros docs) identificados y tratados
- [ ] Tabla de contenido actualizada en docs/04-roadmap.md

**Sospechosos de obsolescencia** (revisar primero):

| Documento | Líneas | Riesgo |
|-----------|--------|--------|
| `03-engram-dotnet-gaps.md` | 214 | Todos los gaps están cerrados. ¿Sigue siendo útil o es histórico? |
| `07-core-skills.md` | 45 | Truncado — sobrevive como esqueleto. ¿Fusionar con 15? |
| `05-comparison-methodologies.md` | 249 | Escrito en v0.1. ¿Sigue siendo relevante o es histórico? |
| `02-memory-strategy.md` | 237 | Puede tener referencias desactualizadas a engram-dotnet |
| `test-matrix.md` + `testing-capabilities.md` | 162 | Dos docs de testing. ¿Fusionar? |
| `archive/new-workflow-deprecated.md` | — | Ya archivado. ¿Mantener o eliminar? |
| `docs/06-engram-sync-convention.md` | 166 | ¿Sigue siendo relevante tras offline-first sync? |
| `09-open-source-integration.md` | 151 | Escrito antes de los IDE files. ¿Está obsoleto? |

**Enfoque**:
1. Leer cada doc y clasificarlo:
   - **✅ OK**: mantenlo como está
   - **🔧 Requiere update**: corregir referencias (checkpoints, skills, etc.)
   - **🗄️ Archivar**: mover a `docs/archive/` con nota de reemplazo
   - **➕ Fusionar**: combinar con otro doc más completo
2. Docs candidatos a archivar: `03-engram-dotnet-gaps.md` (histórico, gaps cerrados), `05-comparison-methodologies.md` (histórico)
3. Docs candidatos a fusionar: `test-matrix.md` + `testing-capabilities.md`
4. Actualizar el checklist de documentación en `04-roadmap.md`

**Dependencias**: Items 1-3 (para saber qué docs están desactualizados vs la realidad probada)

**Esfuerzo**: 2-3 h

**Deliverables**:
- `docs/` limpio y actualizado
- Checklist de documentación actualizado en roadmap
- Posiblemente: docs archivados, fusionados, o redirigidos

---

### Item 6: Schema de `.flowforge.json`

**Objetivo**: Documentar el schema completo de configuración de FlowForge, incluyendo modelos, persona, teacher_mode, SAST config.

**Criterios de éxito**:
- [ ] Schema documentado con todos los campos posibles
- [ ] Ejemplo completo de `.flowforge.json` comentado
- [ ] Compatible con el CLI Wizard cuando exista

**Enfoque**:
1. Definir el schema basado en lo que ya necesitan las skills (modelos, persona, teacher_mode)
2. Documentar en `docs/flowforge-config-schema.md`
3. Crear `.flowforge.example.json` en la raíz

**Dependencias**: Ninguna — es diseño puro

**Esfuerzo**: 1.5 h

**Deliverables**:
- `docs/flowforge-config-schema.md`
- `.flowforge.example.json`

---

## ⏰ SEMANA 3 — Instalación y Testing (P1)

---

### Item 7: `install.sh` / `install.ps1`

**Objetivo**: Script que detecte el IDE (OpenCode / Cursor / Antigravity / VS Code) y copie los archivos correspondientes de `ide/` a las rutas correctas.

**Criterios de éxito**:
- [ ] Detecta automáticamente qué IDE está instalado
- [ ] Copia archivos a las rutas correctas según el IDE
- [ ] No sobrescribe archivos existentes sin backup
- [ ] Funciona en Linux y (si hay .ps1) Windows
- [ ] Mensaje claro de éxito/fracaso

**Enfoque**:
1. Detectar IDE: revisar `~/.config/opencode/`, `~/.cursor/`, `~/.vscode/`, `~/.agents/`
2. Copiar archivos desde `ide/{ide}/` a las rutas del perfil
3. Crear backups con sufijo `.bak`
4. Reportar resultado

**Dependencias**: Items 1, 8 (saber que los IDE files funcionan antes de instalarlos)

**Esfuerzo**: 2-3 h

**Deliverables**:
- `install.sh` (Linux/macOS)
- `install.ps1` (Windows) [opcional]

---

### Item 8: Probar IDE files en los 4 IDEs

**Objetivo**: Verificar que los 20 archivos de `ide/` funcionan correctamente en cada IDE: OpenCode, Cursor, Antigravity, VS Code.

**Criterios de éxito**:
- [ ] OpenCode: los 7 subagentes cargan y delegan
- [ ] Cursor: `workflow.mdc` se aplica en el chat (alwaysApply)
- [ ] Cursor: los 6 `agents/*.md` se cargan como subagentes
- [ ] Antigravity: `workflow.md` + workflows fluyen correctamente
- [ ] VS Code: `copilot-instructions.md` se aplica a las respuestas

**Enfoque**:
1. Para cada IDE: copiar los archivos y probar un comando básico
2. Documentar qué funciona y qué no
3. Ajustar los templates según los resultados

**Dependencias**: El usuario debe tener acceso a los 4 IDEs (o al menos OpenCode + 1 más)

**Esfuerzo**: 2-3 h (30-45 min por IDE)

**Deliverables**:
- Reporte de compatibilidad por IDE
- Ajustes a los templates si es necesario

---

### Item 10: Manejo de features concurrentes

**Objetivo**: Definir un protocolo para cuando dos desarrolladores trabajan features que tocan el mismo código al mismo tiempo.

**Criterios de éxito**:
- [ ] Documento describe el flujo: detectar colisión → alertar → resolver
- [ ] Define roles: ¿quién resuelve el conflicto? ¿el orquestador de cada feature?
- [ ] Propuesta de herramienta o proceso (branching, feature flags, locking)

**Enfoque**:
1. Analizar escenarios: (a) mismo archivo, (b) mismo namespace engram, (c) API que cambia
2. Proponer protocolo basado en git branching + comunicación entre orquestadores
3. Si aplica, crear skill `forge-orchestrator/conflict-resolution`

**Dependencias**: Items 1-3 (entender el flujo base antes de agregar concurrencia)

**Esfuerzo**: 2 h (diseño + documento)

**Deliverables**:
- Sección en docs/ o skill de resolución de conflictos

---

## ⏰ SEMANA 4 — Medición y Maduración (P2)

---

### Item 11: KPIs de efectividad

**Objetivo**: Definir qué métricas usar para medir si FlowForge mejora la productividad, calidad y costo del desarrollo.

**Criterios de éxito**:
- [ ] 3-5 KPIs definidos con fórmula de cálculo
- [ ] Línea base establecida (sin FlowForge)
- [ ] Dashboard o reporte planteado

**Indicadores propuestos**:
| KPI | Fórmula | Qué mide |
|-----|---------|----------|
| Tiempo por feature | Ciclo completo (discovery → deploy) en horas | Velocidad |
| Rework rate | N° de rework tickets / feature | Calidad de spec |
| Bug rate | Bugs post-deploy en 30 días / feature | Calidad de código |
| Token cost | Tokens consumidos por feature (suma de todos los agentes) | Costo |
| Cycle efficiency | Tiempo de implementación / tiempo total | Eficiencia del proceso |

**Dependencias**: Items 2-3 (tener features para medir)

**Esfuerzo**: 1 h

**Deliverables**:
- Documento de KPIs

---

### Item 12: 3 features con FlowForge vs 3 sin

**Objetivo**: Ejecutar 3 features usando FlowForge y 3 features sin (desarrollo ad-hoc tradicional) y comparar los KPIs definidos.

**Criterios de éxito**:
- [ ] 3 features completadas con FlowForge (ciclo completo)
- [ ] 3 features similares sin FlowForge (mismo desarrollador, mismo stack)
- [ ] Tabla comparativa de KPIs
- [ ] Conclusión: ¿FlowForge mejora algo? ¿empeora algo? ¿en qué?

**Enfoque**:
1. Seleccionar 6 features pequeñas (CRUD, auth, report) del mismo proyecto
2. Asignar 3 a FlowForge, 3 a ad-hoc
3. Medir tiempo, calidad, costo
4. Publicar resultados

**Dependencias**: Items 2, 11 (saber ejecutar flow + tener KPIs)

**Esfuerzo**: 4-6 h (depende del tamaño de las features)

**Deliverables**:
- Reporte de benchmark en docs/

---

### Item 13: Guía de migración

**Objetivo**: Ayudar a equipos que usan otras metodologías a migrar a FlowForge sin fricción.

**Criterios de éxito**:
- [ ] Tabla de equivalencias: Scrum → FlowForge, ad-hoc → FlowForge
- [ ] Pasos concretos: qué dejar de hacer, qué empezar a hacer
- [ ] Ejemplo de migración de un proyecto real

**Enfoque**:
1. Mapear roles de Scrum (PO, SM, Dev) a agentes FlowForge
2. Mapear ceremonias (sprint planning, daily, review) a checkpoints
3. Escribir guía paso a paso

**Dependencias**: Items 1-3 (tener el flujo validado)

**Esfuerzo**: 2 h

**Deliverables**:
- `docs/migration-guide.md`

---

### Item 14: Release versioning

**Objetivo**: Adoptar versionado semántico para la metodología FlowForge y publicar releases en GitHub.

**Criterios de éxito**:
- [ ] Versión actual documentada en VERSION.md
- [ ] CHANGELOG.md con formato Keep a Changelog
- [ ] Tags de git para releases: v0.3.0, v0.4.0, etc.
- [ ] GitHub Release notes publicados

**Enfoque**:
1. Crear `VERSION.md` con versión actual (0.3.0)
2. Mantener `CHANGELOG.md` con cambios desde v0.1
3. Taggear el commit actual como v0.3.0
4. Publicar release en GitHub con resumen de cambios

**Dependencias**: Items 1-3 (tener algo que versionar)

**Esfuerzo**: 1 h

**Deliverables**:
- `VERSION.md`, tags de git, GitHub Release

---

## 📊 Resumen de Esfuerzo Total

| Semana | Items | Esfuerzo estimado |
|--------|-------|-------------------|
| Semana 1 | 1, 2, 3, 9 | 3-5 h |
| Semana 2 | 4, 15, 5, 6 | 6.5 h |
| Semana 3 | 7, 8, 10 | 6-8 h |
| Semana 4 | 11, 12, 13, 14 | 8-10 h |
| **Total** | **15** | **23-30 h** |

## 📋 Dependencias Entre Items

```
Item 1 (OpenCode) ──→ Item 2 (Caso CRUD) ──→ Item 3 (Artefactos) ──→ Item 15 (Auditar Docs)
                                        │                              │
                    Item 4 (QUICKSTART) ←┘                              │
                    Item 5 (Template) ←──┘                              │
                              │                                         │
                    Item 6 (Schema) ─ ─ ─ ─ ─ (independiente)           │
                                                                        │
                    Item 15 (Docs) ──→ actualizar ──→ docs/04 (checklist)
                    
Item 8 (Probar IDEs) ──→ Item 7 (install.sh)
                              │
                    Item 10 (Concurrencia) ─ ─ (independiente)
                    
Item 2 + Item 11 (KPIs) ──→ Item 12 (Benchmark)
                              │
                    Item 13 (Migración) ─ ─ → Item 14 (Release)
```

> **Nota**: Items sin flechas son independientes y se pueden hacer en paralelo.
