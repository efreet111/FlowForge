# EngramFlow — Arquitectura

> **Versión**: 0.3 (checkpoints normalizados)
> **Última actualización**: 2026-05-21
> **Estado**: Draft — sujeto a cambios durante implementación

---

## 1. Visión General

EngramFlow es una metodología **Agentic SDLC** diseñada para equipos pequeños y medianos (SMB, 2-20 personas) que quieren integrar agentes de IA como participantes activos en el ciclo de desarrollo de software, sin la sobrecarga burocrática de las metodologías enterprise.

### Principios de diseño

| Principio | Explicación |
|-----------|-------------|
| **Menos es más** | 5-6 agentes en lugar de 10+ roles especializados. Cada agente adicional es un punto de pérdida de contexto y un overhead de mantenimiento. |
| **Contexto on-demand** | Los agentes no reciben contexto en el prompt — lo FETCHEAN via MCP cuando lo necesitan. Reduce tokens y evita contaminación por contexto irrelevante. |
| **Model routing por tarea** | No se usa el mismo modelo para todo. Razonamiento complejo → Sonnet/Opus. Lectura/escritura → Haiku. Persistencia → $0 (SQL directo). |
| **Artefactos versionados como protocolo** | spec.md, plan.md y rework_ticket.md son el "contrato" entre agentes. El orquestador es opcional — los artefactos mismos dirigen el flujo. |
| **4 checkpoints humanos + 1 deploy gate** | De ~8 interrupciones humanas en un SDLC tradicional, se reduce a 5 puntos de control: CKP-0 (Hard Stop binario), CKP-1 (spec), CKP-2 (plan), CKP-3 (escalation mecánico), CKP-4 (deploy gate). El humano no revisa código línea por línea — valida intención, arquitectura, y resultado final. |
| **Memoria estratificada** | Dos niveles: operativa (DB con TTL, 30-90 días) y estructurada (.md versionados, permanentes). Lo efímero se borra solo; lo importante se preserva. |
| **Orquestador AI opcional** | El flujo funciona sin un agente orquestador central. Para equipos que lo quieran, se configura desde JSON. |

### ¿Por qué no 10 agentes?

La investigación de Agentsway, Twelve-Factor Agentic SDLC y ADLC muestra que:

1. **Cada handoff entre agentes pierde contexto** — no se divide, se multiplica la pérdida
2. **Mantener 10 agentes requiere 10 definiciones de rol, 10 sets de prompts, 10 configs** — overhead inasumible para SMB
3. **Roles con solapamiento funcional** (Mission Architect ↔ Intent Engineer, Synthesis ↔ Evolution) se pisaban
4. **Model routing es más eficiente que especialización por agente** — un solo agente con Sonnet para razonar y Haiku para tareas baratas reemplaza varios agentes especializados

---

## 2. Las 5 Fases, 4 Checkpoints y 1 Deploy Gate

```
┌─────────────────────────────────────────────────────────────────────┐
│                           ENGRAMFLOW                                │
│  5 fases | 4 checkpoints + 1 deploy gate | 7 agentes | Model routing│
└─────────────────────────────────────────────────────────────────────┘

FASE 0: DISCOVERY ───────────── CKP-0 🔴 HARD STOP ──────────────────
│  Asociación de memoria cruzada, mapeo de épicas y validación de HU
│
│  Agentes: Discovery Agent ── Análisis de contexto
│  Entregables: Mapa de asociaciones de memoria
│  Models: Haiku / Flash / GPT-4o-mini (rápido y barato)
│
│  ⚠️  Binario e inapelable: si el requerimiento es vago, PARA TODO.

FASE 1: INTENCIÓN ───────────── CKP-1 🟡 SEMÁFORO AMARILLO ─────────
│  Revisión de prioridades y validación de intención de negocio
│
│  Agentes: Arch Agent ─── spec.md + Capability Matrix
│
│  Entregables: spec.md, Capability Matrix
│  Models: Sonnet (razonar), Haiku (leer contexto/tool_use)
│
│  🟡 El humano decide: "¿Aprobás o querés ajustar algo?"

FASE 2: ARQUITECTURA ───────── CKP-2 🟡 SEMÁFORO AMARILLO ─────────
│  Validación y aprobación del plan de implementación
│
│  Agentes: Plan Agent ─── plan.md + descomposición de tareas
│
│  Entregables: plan.md, contratos MCP
│  Models: Sonnet (arquitectura, descomposición), Haiku (rutinas)
│
│  🟡 El humano decide: "¿Luz verde para codificar?"

FASE 3: EJECUCIÓN ──────────── Inner Loop autónomo ────────────────
│  Sin checkpoint humano — autonomía supervisada con autocorrección
│
│  Agentes:
│    Dev Agent ──── código + tests unitarios + Ralph Wiggum Loop
│    Verify Agent ── traceability vs spec.md + RF/RNF + LLM-as-Judge
│
│  Entregables: Código fuente, tests, reporte de verificación
│  Models: Sonnet/Opus (codificar), Sonnet (verificar), Haiku (debug simple)
│
│  Feedback loop:
│    Verify Agent falla → escribe rework_ticket.md
│    Dev Agent lo toma como nuevo plan → reintenta
│    Máximo 3 ciclos → CKP-3 🔴 FRENO DE EMERGENCIA → ESCALATE a humano
│    (El Orquestador AI opcional puede modificar plan.md o escalar)

FASE 4: CIERRE ─────────────── CKP-4 🟢 DEPLOY GATE ────────────────
│  Revisión final + decisión de deploy
│
│  Agentes:
│    Memory Agent ── actualiza CLAUDE.md/AGENTS.md + persiste engramas
│                    + promueve a Nivel 2 (.md) si corresponde
│
│  Entregables: CLAUDE.md actualizado, engramas persistidos, .md estructurados
│  Models: Haiku (estructurar, clasificar), $0 (SQL directo para persistencia)
│
│  🟢 El humano decide: "¿Deployeamos?"
```

---

## 3. Roles y Responsabilidades

### 3.0 Discovery Agent (Fase 0)

| Aspecto | Descripción |
|---------|-------------|
| **Misión** | Mapear la nueva Historia de Usuario (HU) contra memorias pasadas o Épicas (memoria cruzada) antes de planificar nada. |
| **Models** | Modelo rápido y barato (Haiku, Flash, GPT-4o-mini) porque solo lee y clasifica. |
| **Herramientas** | MCP: search_memory, read_epics |
| **Checkpoint** | Freno Duro: Si no hay información suficiente de negocio, se detiene todo el flujo aquí. |

### 3.1 Arch Agent (Fase 1)

**Fusión de**: Knowledge Scout + Mission Architect + Intent Engineer

| Aspecto | Descripción |
|---------|-------------|
| **Misión** | Explorar contexto del proyecto y traducir la visión humana en un spec.md ejecutable con Capability Matrix |
| **Models** | Sonnet para razonamiento, Haiku para lectura de contexto y tool_use |
| **Herramientas** | MCP: read_repo_context, search engram-dotnet, leer docs |
| **Output** | `spec.md`, `Capability Matrix` |
| **Checkpoint** | Entrega al humano para Checkpoint ① |

**Capability Matrix**: Define explícitamente qué decide la IA y qué es lógica fija determinística. Ejemplo:

```yaml
capability_matrix:
  ai_reasoning:
    - "Validar formato de email"           # ✅ La IA puede inferir
    - "Decidir mensaje de error UX"        # ✅ La IA puede redactar
  deterministic:
    - "Ejecutar INSERT en DB"              # ❌ Lógica fija
    - "Verificar token JWT expirado"       # ❌ Regla de negocio dura
    - "Calcular precio con IVA"            # ❌ Fórmula matemática
```

### 3.2 Plan Agent (Fase 2)

**Fusión de**: Experience Designer (opcional) + Scaffold Engineer

| Aspecto | Descripción |
|---------|-------------|
| **Misión** | Traducir spec.md en tareas atómicas con orden de implementación |
| **Models** | Sonnet para arquitectura, Haiku para wireframes básicos |
| **Herramientas** | MCP: read_schema, read_existing_code |
| **Output** | `plan.md`, task breakdown, contratos MCP |
| **Checkpoint** | Entrega al humano para Checkpoint ② |

### 3.3 Dev Agent (Fase 3)

**Rol**: Inner-Loop Agent — el corazón de la productividad

| Aspecto | Descripción |
|---------|-------------|
| **Misión** | Codificar, testear e iterar siguiendo el plan.md |
| **Models** | Sonnet/Opus para codificar, Haiku para debug simple, lectura de archivos |
| **Herramientas** | MCP: write_file, run_tests, read_errors |
| **Output** | Código fuente, tests |
| **Loop** | "Ralph Wiggum Loop": codificar → probar → fallar → corregir |

### 3.4 Verify Agent (Fase 3)

**Rol**: Sentinel Judge + Verification + Testing

| Aspecto | Descripción |
|---------|-------------|
| **Misión** | Verificar que el código cumple spec.md, RF/RNF, y no introduce regresiones |
| **Models** | Sonnet para juicio, Haiku para scan rápido de secretos/vulnerabilidades |
| **Herramientas** | MCP: mem_verify_artifact, mem_traceability, run_tests |
| **Output** | `PASS` o `rework_ticket.md` con items específicos |
| **Feedback** | Si falla: escribe `rework_ticket.md` con ciclo count. Dev Agent lo retoma. |
| **Escalation** | Después de 3 ciclos fallidos → ESCALATE a humano |

### 3.5 Memory Agent (Fase 4)

**Fusión de**: Synthesis Agent + Evolution Engine (sin QLoRA)

| Aspecto | Descripción |
|---------|-------------|
| **Misión** | Cerrar el ciclo: actualizar documentación viva y persistir memoria |
| **Models** | Haiku para estructurar y clasificar |
| **Herramientas** | MCP: mem_save, write_file (para .md), mem_promote_to_md |
| **Output** | CLAUDE.md/AGENTS.md actualizados, engramas persistidos, .md estructurados |
| **Pruning** | No es un agente — es un cron + SQL (Memory Janitor) |

---

## 4. Protocolo de Artefactos

El flujo entre agentes no depende de un orquestador. Depende de artefactos versionados:

```
spec.md ─────────→ plan.md ─────────→ rework_ticket.md ─────────→ deploy
   ↑                    ↑                    ↑
Arch Agent          Plan Agent          Verify Agent
(Fase 1)            (Fase 2)            (Fase 3)
```

### spec.md (Executable Intent)

```markdown
# Spec: User Registration Endpoint

## Objective
Permitir registro de usuarios con email y password.

## Functional Requirements
- RF-001: POST /users acepta {email, password}
- RF-002: Valida formato de email (regex)
- RF-003: Password mínimo 8 chars, 1 mayúscula, 1 número
- RF-004: Email duplicado → 409 Conflict

## Non-Functional Requirements
- RNF-001: Tiempo de respuesta < 200ms (p99)
- RNF-002: No exponer password en logs
- RNF-003: Rate limiting: 10 requests/min por IP
```

### plan.md (Task Breakdown)

```markdown
# Plan: User Registration Endpoint

## Tasks
1. [ ] Crear modelo User con Email y PasswordHash
2. [ ] Implementar endpoint POST /users
3. [ ] Validación de email (regex)
4. [ ] Validación de password (policy)
5. [ ] Hash de password con BCrypt
6. [ ] Manejo de email duplicado
7. [ ] Tests unitarios
8. [ ] Rate limiting middleware

## File Order
1. src/Models/User.cs
2. src/Validators/EmailValidator.cs
3. src/Validators/PasswordPolicy.cs
4. src/Endpoints/UsersEndpoint.cs
```

### rework_ticket.md (Feedback)

```markdown
# Rework Ticket — Cycle 1/3

## Failed Items
- [ ] RF-004: Email duplicado no retorna 409 (retorna 500)
- [ ] RNF-002: Password visible en logs (line 42: console.log(password))
- [ ] No cubre: edge case email con caracteres Unicode

## Instructions
Corregir los items marcados. El spec.md original sigue siendo válido.
```

---

## 5. Model Routing

Uno de los descubrimientos más importantes de nuestro análisis es que **no necesitás agentes separados — necesitás modelos separados para tareas separadas.**

| Tarea | Modelo recomendado | Costo relativo |
|-------|-------------------|----------------|
| Razonamiento complejo, arquitectura, diseño | Sonnet / Opus / Qwen 3.5 Pro | $$$ |
| Codificación de features | Sonnet / DeepSeek | $$ |
| Verificación, juicio, LLM-as-Judge | Sonnet / DeepSeek Pro | $$ |
| Lectura de contexto, exploración de código | Haiku / Flash / GPT-4o-mini | $ |
| Escritura de documentación | Haiku / Flash | $ |
| Debug simple, análisis de errores | Haiku / Flash | $ |
| Persistencia en DB | No necesita LLM (SQL directo) | $0 |
| Pruning, TTL, operaciones batch | No necesita LLM (cron + SQL) | $0 |

### Delegación al Host (IDE)

**EngramFlow NO provee un "Model Router MCP Server" propio.** 

Tras analizar los riesgos operativos (posibles bloqueos de cuentas, violaciones de TOS en IDEs propietarios, y problemas de manejo de keys con proxies de terceros), la decisión de arquitectura es **Delegar el Model Routing al Host (IDE)**.

EngramFlow es una metodología "Client-Agnostic". La responsabilidad de mapear qué modelo se ejecuta en cada fase recae en la configuración nativa de tu entorno:
- En **OpenCode**: Usar `opencode.json` para mapear los modelos a cada sub-agente (ej: `deepseek-v4-flash` para explore, `qwen3.5-plus` para código).
- En **Antigravity / VS Code**: Usar un modelo potente seleccionado globalmente en la UI que actúe como monolito cargando las "skills" para hacer roleplay.
- En **Copilot / Cursor**: Usar el selector de modelos nativo según la fase.

El Model Router es una *práctica recomendada*, no un componente de software de EngramFlow.

---

## 6. Orquestador AI (Hybrid Escalation Manager)

El flujo default de EngramFlow **no necesita un orquestador AI continuo**. Los artefactos versionados (como `spec.md` o `rework_ticket.md`) actúan como el protocolo de comunicación y ruteo determinístico para los agentes base.

Sin embargo, para evitar bloqueos y loops infinitos sin requerir intervención humana constante, los equipos pueden habilitar el **Orquestador AI Opcional**. Este orquestador NO actúa como un router paso a paso (lo que causaría un severo "Token Bleed"), sino que funciona estrictamente como un **Escalation Manager**.

### Cuándo interviene

El Workflow Runner intercepta los artefactos tras cada ejecución. Si detecta que el contador de ciclos en un `rework_ticket.md` (leído desde su YAML Frontmatter) excede el límite configurado (`max_retry_cycles`), pausa el Inner Loop determinístico e invoca al Orquestador AI inyectándole el contexto (`spec.md`, `plan.md` y `rework_ticket.md`).

### Data Flow

```text
[Happy Path - Determinístico]
Dev Agent ──(código)──> Verify Agent ──(rework_ticket 1/3)──> Dev Agent ...

[Escalation Path - Invoca IA]
Verify Agent ──(rework_ticket 3/3)──> Workflow Runner ──(intercepta)──> AI Orchestrator
                                                                             │
                                      ┌──────────────(Analiza)───────────────┘
                                      │
                        [Opción A] ───┴─── [Opción B]
                   Modifica plan.md        Detiene flujo
                  Resetea ciclo a 1/3      Checkpoint Humano
                          │                      │
                      Dev Agent               (Humano)
```

Para equipos que quieran habilitarlo, se configura desde JSON. Ver detalles en [06-ai-orchestrator.md](06-ai-orchestrator.md).

---

## 7. Infraestructura Compartida

```
┌──────────────────────────────────────────────────────────────┐
│                   ENGRAMFLOW INFRASTRUCTURE                    │
├──────────────────────────────────────────────────────────────┤
│                                                              │
│  engram-dotnet ── memoria persistente (2 niveles)            │
│    ├── Nivel 1: DB operativa (SQLite/Postgres, TTL)          │
│    └── Nivel 2: .md versionados en repo (permanentes)        │
│                                                              │
│  MCP tools ──── contexto fetch-on-demand                     │
│    ├── Herramientas de memoria (mem_save, mem_search, etc.)   │
│    ├── Herramientas de verificación (mem_verify_artifact)     │
│    └── Herramientas de promoción (mem_promote_to_md)          │
│                                                              │
│  Model Router ── dispatcher tarea → modelo óptimo            │
│                                                              │
│  Workflow Runner ── bash/Makefile/GitHub Actions             │
│    ├── Orquesta las 4 fases                                   │
│    ├── Dispara checkpoints humanos                            │
│    └── Maneja retry y escalation                              │
│                                                              │
│  Memory Janitor ── cron + SQL (no es un agente)              │
│    ├── Diario: pruning por TTL                                │
│    └── Semanal: batch de promoción a Nivel 2                  │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

---

## 8. Gestión de Fallos

| Escenario | Lo maneja | Cómo |
|-----------|-----------|------|
| Provider timeout | Transport layer (MCP) | Retry 3x + backoff exponencial + escalate |
| Rate limit | Transport layer (MCP) | Backoff + queue |
| Código no compila | Dev Agent loop | Corrige e itera |
| Tests fallan | Dev Agent loop | Ralph Wiggum |
| Spec no cumplida | Verify Agent | `rework_ticket.md` + cycle_count |
| Loop infinito (3+ reworks) | Human escalation | Crear issue en GitHub |
| Decisión compleja (¿refactor o feature?) | Human Checkpoint | Checkpoint ② o ③ |
| Coordinación multi-repo compleja | Orquestador AI (opcional) | Routing + priorización |

---

## 9. Referencias

- [02-memory-strategy.md](02-memory-strategy.md) — Estrategia de 2 niveles de memoria
- [03-engram-dotnet-gaps.md](03-engram-dotnet-gaps.md) — Cambios necesarios en engram-dotnet
- [04-roadmap.md](04-roadmap.md) — Roadmap conjunto
- [05-comparison-methodologies.md](05-comparison-methodologies.md) — Investigación de metodologías
- Agentsway (arXiv 2510.23664, 2025) — Metodología formal para equipos con agentes AI
- Twelve-Factor Agentic SDLC (GitHub tikalk/agentic-sdlc-12-factors, 2025) — 12 principios
- ADLC — Arthur.ai (2026) — Agent Development Lifecycle con flywheel de evaluación
- TACO Framework — KPMG (2025) — Taxonomía de tipos de agentes
- MCP Agentic SDLC — GitHub michaelwybraniec/mcp-agentic-sdlc (2025)
