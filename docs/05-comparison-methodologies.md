# EngramFlow — Comparación de Metodologías

> **Última actualización**: 2026-05-13
> **Propósito**: Documentar la investigación de metodologías Agentic SDLC que informaron el diseño de EngramFlow

---

## 1. Metodologías Investigadas

### 1.1 Agentsway

**Origen**: Académico — arXiv 2510.23664 (oct 2025)
**Autores**: Eranga Bandara et al.
**URL**: https://arxiv.org/abs/2510.23664

**Qué propone**:
- Ciclo de vida estructurado centrado en orquestación humana
- Roles especializados: Planning Agent, Prompting Agent, Coding Agent, Testing Agent, Fine-Tuning Agent
- Privacidad por diseño (privacy-by-design)
- Aprendizaje retrospectivo (fine-tuning de LLMs con outputs de agentes)
- Métricas medibles de productividad y confianza

**Fortalezas**:
- Primera metodología formal para equipos con agentes AI
- Énfasis en governance y transparencia
- Privacidad por diseño es relevante para enterprise

**Debilidades para SMB**:
- Fine-tuning es caro y complejo (requiere GPUs, datasets curados)
- 5 roles de agentes + 1 humano = 6 roles, que es razonable pero la propuesta de 1 agente por rol es rígida
- Enfoque académico — falta validación práctica en equipos pequeños

**Qué tomamos**: La estructura de roles + human orchestration. La idea de que el humano es el orquestador final.

**Qué descartamos**: Fine-tuning como mecanismo de memoria (lo reemplazamos con engram-dotnet + 2 niveles de memoria). La rigidez de 1 agente = 1 rol.

---

### 1.2 TACO Framework (KPMG)

**Origen**: Enterprise — KPMG (2025)
**URL**: https://kpmg.com/au/en/services/ai-services/taco-framework.html

**Qué propone**:
- Taxonomía de 4 tipos de agentes según complejidad:
  - **Taskers**: Tareas únicas, repetitivas, mínima complejidad
  - **Automators**: Flujos multi-sistema, integraciones
  - **Collaborators**: Agentes adaptativos que trabajan con humanos
  - **Orchestrators**: Coordinación de múltiples agentes y workflows
- "Human-on-the-loop" como modelo operativo

**Fortalezas**:
- Taxonomía clara y práctica para clasificar agentes
- Útil para decidir qué tipo de agente usar para cada tarea
- El concepto "on-the-loop" vs "in-the-loop" es valioso

**Debilidades para SMB**:
- Es una taxonomía de clasificación, no una metodología de desarrollo
- No define un ciclo de vida, fases, artefactos ni checkpoints
- La categorización "Taskers → Orchestrators" sugiere complejidad creciente que no siempre aplica

**Qué tomamos**: El concepto de Orchestrator como opcional, no obligatorio. La distinción "on-the-loop" vs "in-the-loop" para checkpoints humanos.

**Qué descartamos**: La clasificación rígida en 4 tipos (un agente puede ser Tasker y Collaborator según el momento del ciclo). No usar "Taskers" como agentes separados (son skills/tools).

---

### 1.3 MCP Agentic SDLC

**Origen**: GitHub — michaelwybraniec/mcp-agentic-sdlc (2025)
**Componentes**: ASDLC (Agentic Software Development Lifecycle) + AWP (Agentic Workflow Protocol)

**Qué propone**:
- 6 fases: Problem Definition → Design → Development → Testing → Deployment → Maintenance
- Cada fase incluye colaboración humano-AI
- Framework-agnostic (funciona con Scrum, Kanban, Waterfall)
- Protocolo de workflow con context management, progress tracking, quality gates
- "Recipes" para backlog, requirements, tech specs

**Fortaleces**:
- Framework-agnostic muy flexible
- ASDLC + AWP separa el qué del cómo
- Recipes reutilizables para distintos niveles (MVP, POC, Pro)
- MCP como protocolo nativo

**Debilidades para SMB**:
- 6 fases es más que las 4 de EngramFlow (más overhead)
- Las recipes son genéricas, no adaptadas a agentes AI
- Framework-agnostic significa que no toma posición — útil pero no da guía fuerte

**Qué tomamos**: El concepto de quality gates + recipes como patrones reutilizables. MCP como ciudadano de primera clase.

**Qué descartamos**: Las 6 fases lineales (preferimos 4 fases con loops iterativos). Las recipes genéricas (preferimos spec.md + plan.md como artefactos canónicos).

---

### 1.4 Twelve-Factor Agentic SDLC

**Origen**: GitHub — tikalk/agentic-sdlc-12-factors (2025)
**URL**: https://github.com/tikalk/agentic-sdlc-12-factors

**Qué propone**: 12 principios para desarrollo con agentes AI:
1. Strategic Mindset — tratar AI como junior partner
2. Context Scaffolding — gestionar contexto como librería crítica
3. Mission Definition — Mission Brief → spec.md
4. Structured Planning — spec.md → plan.md
5. Dual Execution Loops — síncrono (complejo) + asíncrono (delegado)
6. The Great Filter — humano como árbitro final de calidad
7. Adaptive Quality Gates — Micro-Reviews + Macro-Reviews
8. AI-Augmented Testing — humano define riesgos, AI genera tests
9. Traceability — issue tracker → spec → code
10. Strategic Tooling — herramientas especializadas via gateway
11. Directives as Code — instrucciones versionadas (spec.md, AGENTS.md)
12. Team Capability — compartir best practices + evals

**Fortalezas**:
- 12 principios claros, prácticos, accionables
- "The Great Filter" (Factor VI) es conceptualmente sólido — el humano decide
- "Dual Execution Loops" (Factor V) resuelve el problema síncrono vs asíncrono
- "Directives as Code" (Factor XI) — especificaciones versionadas como código
- El que más influyó en EngramFlow

**Debilidades para SMB**:
- 12 principios son muchos para recordar y aplicar
- Algunos principios son abstractos ("Strategic Mindset", "Team Capability")
- No define un ciclo de vida concreto (fases, artefactos, roles)

**Qué tomamos**: Casi todo.
- Factor III: Mission Brief → spec.md (adoptado como spec.md)
- Factor IV: spec.md → plan.md (adoptado como plan.md)
- Factor V: Dual Execution Loops (adoptado como Inner Loop autónomo + checkpoints síncronos)
- Factor VI: The Great Filter (el humano es el filtro en los 3 checkpoints)
- Factor VIII: AI-Augmented Testing (el Dev Agent escribe tests, el Verify Agent valida)
- Factor XI: Directives as Code (spec.md, plan.md, CLAUDE.md, AGENTS.md versionados)

**Qué descartamos**: Nada significativo — Twelve-Factor es la base conceptual más sólida para EngramFlow.

---

### 1.5 ADLC — Agent Development Lifecycle (Arthur.ai)

**Origen**: Arthur.ai (2026)
**URL**: https://www.arthur.ai/blog/introducing-adlc

**Qué propone**:
- Ciclo de vida completo para sistemas con agentes probabilísticos
- Reinterpretación del SDLC clásico para sistemas no-determinísticos
- **Agent Development Flywheel**: continuous evaluation → failure identification → eval enhancement → deploy
- Énfasis en: evals, behavioral testing, observabilidad

**Fortalezas**:
- El Agent Development Flywheel es el concepto más práctico para mejorar agentes iterativamente
- Reconoce que los agentes requieren más tuning que el software tradicional
- Énfasis en evals como deliverable, no afterthought

**Debilidades para SMB**:
- Pensado para sistemas agentic complejos (customer service, fintech), no para el SDLC tradicional
- El flywheel asume que tenés data de producción para mejorar — para un equipo chico arrancando, no aplica
- Menos útil para el desarrollo de features tradicionales

**Qué tomamos**: El concepto de evaluation flywheel aplicado al Verify Agent: cada ciclo de verificación feedbacka al spec. El énfasis en que "tuning > building" para sistemas con agentes.

**Qué descartamos**: El flywheel como mecanismo central (EngramFlow usa checkpoints humanos como gate principal). La complejidad de evals estructuradas para equipos chicos que recién arrancan.

---

### 1.6 Agentic Development Playbook (THV)

**Origen**: THV VC — João Camarate (2026)
**URL**: https://thv.vc/insights/agentic-development-playbook

**Qué propone**: 7-step loop secuencial con gates humanos entre cada paso:
1. Product Manager Agent → 2. UX Designer Agent → 3. Architect Agent → 4. Implementer Agent → 5. Reviewer Agent → 6. Security Auditor Agent → 7. QA Agent → CI/CD

**Fortalezas**:
- Cada paso tiene gate humano explícito
- Roles bien definidos con alcance limitado
- Énfasis en human judgment sobre automation
- AGENTS.md como archivo de configuración central

**Debilidades para SMB**:
- 7 gates humanos es DEMASIADO para SMB (el doble que EngramFlow)
- 7 roles de agente es más de lo que un equipo chico puede mantener
- UX Designer Agent sobra para proyectos backend

**Qué tomamos**: El concepto de AGENTS.md como configuración central (EngramFlow lo usa como "constitución" del proyecto). La idea de que los gates humanos no son burocracia sino "the point where your expertise makes the difference".

**Qué descartamos**: 7 gates humanos (lo reducimos a 3). UX Designer Agent como rol separado (lo absorbe el Plan Agent o el humano).

---

## 2. Tabla Comparativa

| Dimensión | Agentsway | TACO | MCP Agentic SDLC | Twelve-Factor | ADLC (Arthur) | THV Playbook | **EngramFlow** |
|-----------|-----------|------|-----------------|---------------|---------------|--------------|----------------|
| **Agentes** | 5 roles | 4 tipos (taxonomía) | 1 (colaboración H-AI) | No define | No define | 7 roles | **7 agentes** |
| **Checkpoints humanos** | Human orchestration | Human-on-the-loop | Quality gates | The Great Filter (1) | No define | 7 gates | **5 (CKP-0→4)** |
| **Fases** | Lifecycle continuo | N/A | 6 fases | 12 factores | 6 fases | 7 pasos secuenciales | **5 fases** |
| **Artefactos** | No define | No define | Recipes (MVP/POC/Pro) | spec.md, plan.md | Evals, behavioral tests | User story, UI spec, blueprint | **spec.md, plan.md, verify-report.md, rework_ticket.md** |
| **Memoria** | Fine-tuning LLMs | No define | No define | Context scaffolding | Eval suites | No define | **engram-dotnet (2 niveles)** |
| **Orquestador** | Humano (central) | Orchestrator (AI) | Humano + AI | Dual loops | Flywheel (AI) | Humano (gate keeper) | **AI coordinator (IDE rules; no inline dev)** |
| **Complejidad** | Alta | Media | Media | Baja | Alta | Alta | **Baja** |
| **Target** | Enterprise | Enterprise | Cualquiera | Cualquiera | Enterprise | SMB + Enterprise | **SMB (2-20 personas)** |

---

## 3. Lo que EngramFlow Toma de Cada Una

```
Agentsway
├── Human orchestration como principio
└── Roles especializados con alcance definido

TACO (KPMG)
├── Orchestrator como opcional, no obligatorio
└── "On-the-loop" vs "in-the-loop" para checkpoints

MCP Agentic SDLC
├── MCP como ciudadano de primera clase
└── Quality gates entre fases

Twelve-Factor Agentic SDLC ← BASE CONCEPTUAL
├── spec.md + plan.md como artefactos
├── Dual Execution Loops (síncrono + asíncrono)
├── The Great Filter (humano como árbitro final)
├── Directives as Code (todo versionado)
└── AI-Augmented Testing (AI escribe tests)

ADLC (Arthur)
├── Evaluation flywheel para mejora continua
└── Énfasis en que tuning > building

THV Playbook
├── AGENTS.md como configuración central
└── Gates humanos = expertise, no burocracia
```

---

## 4. Decisiones Clave de Diseño

| Decisión | Por qué |
|----------|---------|
| **5 checkpoints (CKP-0→4), not 7** | CKP-0/3 are hard stops; CKP-1/2/4 are human gates. Fewer arbitrary gates than 7-step playbooks, but more precise than “3 checkpoints” legacy wording. |
| **Model routing > especialización por agente** | La investigación de Jishu Labs (2026) y BCG muestra que el tiered approach (modelo diferente según tarea) da mejor relación costo/beneficio que tener agentes especializados |
| **engram-dotnet > fine-tuning** | Fine-tuning (QLoRA) requiere GPUs, datasets curados, MLOps. RAG vectorial + FTS5 + .md da 90% del valor con una fracción del costo y la complejidad. |
| **Orquestador opcional** | La evidencia de Twelve-Factor (Factor V) y THV muestra que artefactos versionados + automation determinística (Makefile, bash) reemplazan al orquestador AI para la mayoría de los casos. |
| **Artefactos como protocolo** | spec.md → plan.md → rework_ticket.md. Estos archivos son el "mensaje" entre agentes. No necesitan un bus de mensajes ni un orquestador. Git es el transporte. |
| **Memory Janitor ≠ agente** | No todo necesita LLM. Pruning por TTL es `DELETE WHERE`. Promoción semanal puede necesitar Haiku. Pero la mayoría de las operaciones de memoria son SQL o scripts. |
