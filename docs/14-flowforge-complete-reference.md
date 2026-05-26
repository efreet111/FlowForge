# FlowForge — Referencia Completa y Casos de Prueba

> **Versión**: 1.0 (post-OLA 1-4)
> **Skills totales**: 30 (7 core + 23 especializadas)
> **Checkpoints**: 5 (CKP-0 a CKP-4)
> **Agentes**: 7 roles
> **Artefactos**: spec.md → plan.md → rework_ticket.md → deploy

---

## PARTE 1: ARQUITECTURA DEL FLUJO

### 🔴🟡🟢 Sistema de Checkpoints

| CKP | Fase | Color | Tipo | Qué pasa | Quién decide |
|-----|------|-------|------|----------|-------------|
| **CKP-0** | Discovery | 🔴 HARD STOP | Binario, inapelable | Requerimiento vago → PARAR y pedir clarificación | El agente (no avanza si no hay claridad) |
| **CKP-1** | Arch (spec.md) | 🟡 SEMÁFORO AMARILLO | Flexible | spec.md listo → "¿Aprobás o querés ajustar?" | El humano |
| **CKP-2** | Plan (plan.md) | 🟡 SEMÁFORO AMARILLO | Flexible | plan.md listo → "¿Luz verde para codificar?" | El humano |
| **CKP-3** | Verify (Inner Loop) | 🔴 FRENO EMERGENCIA | Mecánico (3 ciclos) | 3 reworks fallidos → ESCALAR al humano | Nadie — es mecánico |
| **CKP-4** | Memory (Cierre) | 🟢 DEPLOY GATE | Flexible | Feature completa → "¿Deployeamos?" | El humano |

### 🔄 Las 5 Fases

```
FASE 0 ─── DISCOVERY ───── CKP-0 🔴
  Agente:  forge-discovery
  Skills:  core | security | compliance | cost
  Output:  Context Map (asociaciones de memoria)

FASE 1 ─── INTENCIÓN ───── CKP-1 🟡
  Agente:  forge-arch
  Skills:  core | security | performance | a11y | domain
  Output:  spec.md + Capability Matrix

FASE 2 ─── ARQUITECTURA ── CKP-2 🟡
  Agente:  forge-plan
  Skills:  core | security | patterns | migrations | rollback
  Output:  plan.md

FASE 3 ─── EJECUCIÓN ───── Inner Loop (CKP-3 🔴 si falla)
  Agentes: forge-dev ↔ forge-verify (loop)
  Skills DEV:  core | security | solid | testing | performance | refactor
  Skills VERIFY: core | security | complexity | performance | a11y
  Output:  Código + tests + PASS o rework_ticket.md

FASE 4 ─── CIERRE ──────── CKP-4 🟢
  Agente:  forge-memory
  Skills:  core | metrics | changelog | knowledge
  Output:  Session summary + ADRs + memoria persistida
```

### 📜 Protocolo de Artefactos

```
[Context Map] ──→ [spec.md + Capability Matrix] ──→ [plan.md] ──→ [código + tests] ──→ [deploy]
      ↑                    ↑                            ↑                ↑
  discovery              arch                         plan            dev ↔ verify
  (Fase 0)              (Fase 1)                     (Fase 2)         (Fase 3)

Si verify falla:
  [rework_ticket.md] ──→ dev corrige ──→ verify re-audita (máx 3 ciclos)
```

---

## PARTE 2: CATÁLOGO COMPLETO DE AGENTES Y SKILLS

### 🎯 forge-orchestrator (El Semáforo)
**Skills**: 1 core
**Fase**: Transversal (todas)
**Rol**: Director de estado — no escribe código, no diseña specs, no verifica. Solo delega y aplica checkpoints.
**Para qué se carga**: Siempre — es el entry point del flujo.

---

### 🔍 forge-discovery (Fase 0)
**Skills**: 4 (1 core + 3 especializadas)

| Skill | Trigger | Qué hace |
|-------|---------|----------|
| **core** | Siempre | Busca memorias pasadas, extrae keywords, mapea épicas |
| **security** | Feature toca auth/datos/APIs | Busca CVEs pasados, evalúa riesgos de dependencias |
| **compliance** | Feature toca datos personales | Identifica GDPR/SOC2/HIPAA/PCI-DSS aplicables |
| **cost** | Feature nueva con impacto infraestructura | Estima costos de compute, storage, bandwidth, APIs |

**Output**: Context Map (prefacio obligatorio para Fase 1)

---

### 🏗️ forge-arch (Fase 1)
**Skills**: 5 (1 core + 4 especializadas)

| Skill | Trigger | Qué hace |
|-------|---------|----------|
| **core** | Siempre | Escribe spec.md con RF/RNF en formato Given-When-Then |
| **security** | Feature toca auth/datos/APIs | STRIDE threat modeling + RNF de seguridad obligatorios |
| **performance** | Feature crítica o de cara al usuario | SLAs/SLOs medibles, identificación de hot paths |
| **a11y** | Feature con UI | WCAG 2.1 AA requirements por tipo de componente |
| **domain** | Múltiples bounded contexts | DDD: contextos, lenguaje ubicuo, aggregates, eventos |

**Output**: spec.md + Capability Matrix (ai_reasoning vs deterministic)

---

### 📐 forge-plan (Fase 2)
**Skills**: 5 (1 core + 4 especializadas)

| Skill | Trigger | Qué hace |
|-------|---------|----------|
| **core** | Siempre | Descompone spec.md en tareas atómicas con orden topológico |
| **security** | Siempre que haya input de usuario | OWASP ASVS checklist, secure-by-design patterns |
| **patterns** | Decisiones estructurales | Catálogo GoF + enterprise + cloud-native, árbol de decisión |
| **migrations** | Schemas de DB nuevos/modificados | Estrategias zero-downtime (additive/modifying/destructive) |
| **rollback** | Features que modifican contratos | Blue-green/canary/feature flags, plan de rollback |

**Output**: plan.md con checklist de implementación

---

### 💻 forge-dev (Fase 3a)
**Skills**: 6 (1 core + 5 especializadas)

| Skill | Trigger | Qué hace |
|-------|---------|----------|
| **core** | Siempre | Codifica + Ralph Wiggum Loop (test → fail → fix) |
| **security** | Código con input/DB/auth | OWASP Top 10 prevention + 7 red flags auto-fail |
| **solid** | Todo código de producción | Self-audit SOLID (5/5 scoring), anti-patrones por principio |
| **testing** | Lógica de negocio compleja | Property-based testing, fuzzing (14 inputs), mutation testing |
| **performance** | Código con DB o APIs | N+1 detection (3 lenguajes), caching (5 estrategias) |
| **refactor** | Code smells detectados en loop | Catálogo Fowler (7 transformaciones), 10 smells |

**Output**: Código + tests unitarios (con trazabilidad RF-XXX)

---

### ⚖️ forge-verify (Fase 3b)
**Skills**: 5 (1 core + 4 especializadas)

| Skill | Trigger | Qué hace |
|-------|---------|----------|
| **core** | Siempre | Verifica spec compliance, ejecuta tests, emite PASS/rework |
| **security** | Siempre | SAST mental, OWASP Top 10 verification, dependency audit |
| **complexity** | Código con lógica condicional | MCC, nesting depth, cognitive load, 8 smells |
| **performance** | RNF de performance en spec | N+1 query audit, memory leaks, Big-O, benchmarks |
| **a11y** | Feature con UI | WCAG AA audit (6 áreas), auto-fail triggers |

**Output**: PASS + manual test steps, o rework_ticket.md

---

### 🧠 forge-memory (Fase 4)
**Skills**: 4 (1 core + 3 especializadas)

| Skill | Trigger | Qué hace |
|-------|---------|----------|
| **core** | Siempre | Sintetiza aprendizajes, persiste engramas, promueve ADRs |
| **metrics** | Cierre de feature | Trackea coverage, cycle time, tech debt, tendencias |
| **changelog** | Pre-release | Changelog automático desde commits, release notes |
| **knowledge** | Multi-repo | Knowledge graph cross-project, ADR cross-referencing |

**Output**: Session summary, ADRs, changelog, memoria persistida

---

### 📊 Resumen de Skills por Rol

| Rol | Core | OLA 1 | OLA 2 | OLA 3 | OLA 4 | Total |
|-----|------|-------|-------|-------|-------|-------|
| Orchestrator | 1 | — | — | — | — | **1** |
| Discovery | 1 | — | — | 2 | 1 | **4** |
| Arch | 1 | 1 | — | 3 | — | **5** |
| Plan | 1 | 1 | 1 | 2 | — | **5** |
| Dev | 1 | 2 | 2 | 1 | — | **6** |
| Verify | 1 | 1 | 2 | — | 1 | **5** |
| Memory | 1 | — | — | — | 3 | **4** |
| **TOTAL** | **7** | **5** | **5** | **8** | **5** | **30** |

---

## PARTE 3: CASOS DE PRUEBA PRÁCTICOS

### 🎯 Proyecto de Prueba Sugerido

Para testear el flujo completo, propongo un **proyecto real pero acotado** que toque todos los skills:

**Proyecto**: *"Task Manager API"* — una API REST para gestión de tareas
**Stack**: Node.js/TypeScript + PostgreSQL + Redis
**Equipo**: 1 persona (vos) simulando todos los checkpoints humanos

---

### Caso 1: Feature Simple — "CRUD de Tareas"

**Objetivo**: Validar el flujo base (sin skills especializadas)

| Fase | Qué debería pasar | Skills que se activan |
|------|------------------|----------------------|
| Discovery | Buscar épicas previas de tareas/todos | discovery/core |
| CKP-0 | Contexto suficiente → avanzar | — |
| Arch | spec.md con RF-001 a RF-005 + escenarios GWT | arch/core |
| CKP-1 | Humano aprueba spec | — |
| Plan | plan.md con 6-8 tareas orden topológico | plan/core |
| CKP-2 | Humano da luz verde | — |
| Dev | Código + tests unitarios | dev/core, dev/solid |
| Verify | Tests pasan, spec compliance | verify/core, verify/complexity |
| CKP-3 | PASS (o rework si hay errores) | — |
| Memory | Session summary, mem_save | memory/core |

**Criterio de éxito**: Feature implementada en < 3 ciclos de rework, todas las tareas del plan.md completadas, tests en verde.

---

### Caso 2: Feature con Seguridad — "Autenticación JWT"

**Objetivo**: Validar skills de seguridad en todas las fases

| Fase | Skills extras | Qué verificar |
|------|--------------|---------------|
| Discovery | **discovery/security** | Buscar CVEs de la librería JWT, vulnerabilidades pasadas de auth |
| CKP-0 | **discovery/compliance** | OK si no hay PHI ni datos personales |
| Arch | **arch/security** | STRIDE aplicado: Spoofing (¿JWT firmado?), Tampering (¿HMAC?), Repudiation (¿logs?), Disclosure (¿PII en tokens?), DoS (¿rate limiting?), Elevation (¿roles?) |
| Plan | **plan/security** | OWASP ASVS V2-V4: password policy, session timeout, RBAC |
| Dev | **dev/security** | OWASP Top 10: A01 (access control), A02 (bcrypt), A03 (parameterized queries), A05 (cors headers), A07 (brute force lockout) |
| Verify | **verify/security** | SAST audit: secretos en código, SQL injection, auth bypass |

**Criterio de éxito**: spec.md tiene RNF-SEC-001 a RNF-SEC-006. plan.md tiene tareas [SEC]. Código sin red flags de seguridad. Verify emite PASS con auditoría de seguridad.

---

### Caso 3: Feature con Performance — "Dashboard en Tiempo Real"

**Objetivo**: Validar skills de performance + patrones

| Fase | Skills extras | Qué verificar |
|------|--------------|---------------|
| Discovery | **discovery/cost** | Estimar costos: WebSocket connections, consultas a DB, frecuencia de refresco |
| Arch | **arch/performance** | SLOs: TTFB < 100ms, FCP < 1.5s, throughput de WebSocket |
| Plan | **plan/patterns** | ¿CQRS para separar lecturas de escrituras? ¿Cache? ¿Observer para WebSocket? |
| Dev | **dev/performance** | N+1 detection en queries del dashboard, caching con Redis, batching |
| Verify | **verify/performance** | N+1 audit, memory leaks en conexiones WebSocket, Big-O de agregaciones |

**Criterio de éxito**: SLOs definidos y verificables. Código sin N+1. Caché implementada con TTL. Plan de rollback documentado.

---

### Caso 4: Feature con UI — "Panel de Administración"

**Objetivo**: Validar skills de accesibilidad

| Fase | Skills extras | Qué verificar |
|------|--------------|---------------|
| Arch | **arch/a11y** | WCAG AA: contraste 4.5:1, navegación por teclado, aria attributes, formularios con labels |
| Verify | **verify/a11y** | Auditoría de 6 áreas: HTML semántico, ARIA, teclado, contraste, formularios, contenido dinámico |

**Criterio de éxito**: spec.md tiene RNF-A11Y-001 a RNF-A11Y-030. Verify detecta al menos 3 violaciones a11y (y son corregidas antes del PASS).

---

### Caso 5: Feature con Migración — "Agregar Campo a Users"

**Objetivo**: Validar skills de migraciones y rollback

| Fase | Skills extras | Qué verificar |
|------|--------------|---------------|
| Plan | **plan/migrations**, **plan/rollback** | Estrategia additive (agregar columna nullable), fases pre-deploy/post-deploy, rollback script |
| Dev | **dev/testing** | Fuzzing del nuevo campo (caracteres especiales, Unicode, SQL injection) |

**Criterio de éxito**: plan.md tiene migración en 2 fases (pre-deploy + post-deploy). Rollback script existe. Tests de borde para el nuevo campo.

---

### Caso 6: Feature con Refactor — "Migrar a Patrón Repositorio"

**Objetivo**: Validar skills de refactor y domain

| Fase | Skills extras | Qué verificar |
|------|--------------|---------------|
| Arch | **arch/domain** | DDD: bounded contexts, aggregates, lenguaje ubicuo |
| Plan | **plan/patterns** | Repository pattern seleccionado, interfaces definidas |
| Dev | **dev/refactor** | Extract Method, Introduce Parameter Object, test-preserving workflow |
| Verify | **verify/complexity** | MCC antes/después: debería Bajar después del refactor |

**Criterio de éxito**: Código refactorizado sin cambiar comportamiento. Tests existentes pasan SIN modificaciones. MCC baja en las funciones refactorizadas.

---

### Caso 7: Feature Multi-Repo — "Servicio de Notificaciones"

**Objetivo**: Validar skills de conocimiento cross-project

| Fase | Skills extras | Qué verificar |
|------|--------------|---------------|
| Discovery | **discovery/security**, **discovery/compliance** | Notificaciones → datos personales → GDPR aplica |
| Memory | **memory/knowledge** | ADR cross-referenciado con otros servicios, knowledge graph actualizado |
| Memory | **memory/metrics** | Project health snapshot del feature |
| Memory | **memory/changelog** | Release notes generadas |

**Criterio de éxito**: Compliance identificado (GDPR por datos de usuario). ADR linkeado a servicios dependientes. Changelog generado con feat/sec/fix.

---

### 🧪 Checklist de Validación del Flujo

Para CADA feature, verificar:

```
Antes de empezar:
  [ ] Feature tiene un nombre claro y un objetivo medible

CKP-0 (Discovery):
  [ ] Context Map generado (o declaración de "sin contexto previo")
  [ ] Si toca seguridad: discovery/security se cargó
  [ ] Si toca datos personales: discovery/compliance se cargó
  [ ] Si toca infraestructura: discovery/cost se cargó

CKP-1 (spec.md):
  [ ] RFs con IDs claros (RF-001, RF-002...)
  [ ] RNFs con IDs claros (RNF-SEC-XXX, RNF-PERF-XXX, RNF-A11Y-XXX)
  [ ] Escenarios Given-When-Then para cada RF
  [ ] Capability Matrix con ai_reasoning vs deterministic
  [ ] RNFs de seguridad presentes si aplica
  [ ] SLOs definidos si aplica
  [ ] WCAG requirements si hay UI

CKP-2 (plan.md):
  [ ] Tareas en orden topológico (dependencias primero)
  [ ] Contratos y estructuras definidas (no ambigüedad para dev)
  [ ] Tareas de seguridad con prefijo [SEC] si aplica
  [ ] Patrones seleccionados con [PATTERN: X] si aplica
  [ ] Migración documentada en fases (pre/code/post)
  [ ] Plan de rollback documentado

Fase 3 (Ejecución):
  [ ] Cada tarea del plan.md se completó
  [ ] Tests unitarios: 1 por escenario GWT
  [ ] Tests con nombre que incluye RF-XXX para trazabilidad
  [ ] Ralph Wiggum Loop: compila + tests verdes
  [ ] SOLID self-audit pasado (≥ 4/5)
  [ ] Red flags de seguridad verificados (0 encontrados)
  [ ] N+1 audit: ≤ 3 queries por request
  [ ] Complejidad: MCC ≤ 20 por función

CKP-3 (Verify):
  [ ] verify/core: spec compliance check
  [ ] verify/security: SAST audit + OWASP verification
  [ ] verify/complexity: MCC, nesting, cognitive load
  [ ] verify/performance: N+1, memory, Big-O (si aplica)
  [ ] verify/a11y: WCAG AA compliance (si hay UI)
  [ ] Test suite 100% verde
  [ ] cycle_count < 3 (si no, escalar)

CKP-4 (Memory):
  [ ] Session summary escrito
  [ ] Decisiones importantes promovidas a ADRs
  [ ] Métricas de proyecto actualizadas
  [ ] Changelog actualizado (pre-release)
  [ ] Cross-project knowledge links (si multi-repo)
```

---

## PARTE 4: PRÓXIMOS PASOS

### 🔧 Herramientas Pendientes (Después de validar el flujo)

| Herramienta | Por qué es necesaria |
|-------------|---------------------|
| **Generador de Reglas** | Compila las 30 skills en `.cursorrules` / `.clinerules` para inyectar en el IDE |
| **CLI Wizard (forge init)** | Setup interactivo de `.flowforge.json`: modelos, persona, base de datos |
| **Dashboard web** | Visualización de engram-dotnet, métricas de proyecto en tiempo real |
| **Backend Config File** | Configuración por archivo para engram-dotnet (reemplaza variables de entorno) |

### 🧠 Ideas Futuras (Incubadora)

| Idea | Fuente |
|------|--------|
| Context Poisoning Guardrail | docs/13-edge-cases-and-risks.md |
| Conflict Resolution Agent | docs/13-edge-cases-and-risks.md |
| Cost Observability Dashboard | docs/13-edge-cases-and-risks.md |
| Drift Health Check | docs/13-edge-cases-and-risks.md |
| Message Queue para Escrituras .md | docs/13-edge-cases-and-risks.md |
| Lineage Enforcement en CKP-3 | docs/13-edge-cases-and-risks.md |

---

> **Última actualización**: 2026-05-25
> **Commits en esta sesión**: 2 (e7fcab6, 3798f22)
> **Skills totales**: 30 | **Checkpoints**: 5 | **Fases**: 5 | **Agentes**: 7
