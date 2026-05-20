## Exploration: Orquestador AI Opcional

### Current State
Actualmente, EngramFlow está diseñado para funcionar de manera **descentralizada y basada en artefactos**. No existe un orquestador AI central; en su lugar, la presencia de un documento (`spec.md`, `plan.md`, `rework_ticket.md`) actúa como el "contrato" o disparador determinístico para que el siguiente agente entre en acción. El ruteo de modelos se asume determinístico (ej. `Model Router` mapea tareas a modelos específicos en base a una configuración estática). 

### Affected Areas
- `docs/01-engramflow-architecture.md` — Requiere definir formalmente el comportamiento cuando el flag `orchestrator.enabled` es `true`.
- `engram-dotnet` (Configuración) — El archivo `.engram.json` deberá soportar el esquema de configuración del orquestador.
- **Workflow Runner** — El motor que ejecuta los scripts/pipelines deberá delegar el control de flujo al LLM en lugar de seguir una secuencia estricta.

### Approaches

1. **Deterministic State Machine (Sin Orquestador AI)**
   - **Pros**: Costo $0 en LLM para ruteo. 100% predecible, determinístico y fácil de debugear.
   - **Cons**: Frágil ante excepciones de negocio (ej. "el Verify Agent falló pero el error es del Spec, no del código"). Falla si el flujo se sale del `happy path` lineal.
   - **Effort**: Low

2. **Lightweight AI Router (LLM as a Switch)**
   - **Pros**: Muy barato (usa modelos rápidos como Haiku o GPT-4o-mini). Analiza el último artefacto modificado y devuelve un JSON simple con el nombre del próximo agente a invocar.
   - **Cons**: Carece de memoria a largo plazo o comprensión profunda de la arquitectura. No puede tomar decisiones de rollback complejas.
   - **Effort**: Medium

3. **Full AI Orchestrator Agent (Sonnet)**
   - **Pros**: Inteligencia suprema. Puede coordinar tareas multi-repo, leer la `Capability Matrix` para decidir qué agente es mejor, entender cuándo escalar a un humano antes de quemar tokens inútilmente, y resolver bloqueos entre agentes (ej. Dev Agent vs Verify Agent).
   - **Cons**: Alto consumo de tokens por ciclo (el orquestador necesita todo el contexto de la conversación o de los artefactos para decidir). Mayor latencia.
   - **Effort**: High

### Recommendation
**Enfoque Híbrido (Deterministic Happy-Path + Escalation Orchestrator).**
El flujo debe mantenerse determinístico por defecto basándose en artefactos (Approach 1) para mantener los costos bajos. Sin embargo, el **Full AI Orchestrator** debe actuar como un *Exception Handler* o *Escalation Manager*. 
Si el ciclo normal se rompe (ej. 3 reworks fallidos consecutivos, o una discrepancia entre `spec.md` y `plan.md`), el workflow runner invoca al AI Orchestrator pasándole el contexto de la falla. El Orquestador analiza la situación y decide si: (a) reasignar la tarea con nuevas instrucciones, (b) forzar un rollback, o (c) detenerse y generar el Checkpoint Humano.

### Risks
- **Token Bleed**: Si el orquestador se invoca en cada paso del ciclo interno (Inner Loop), el costo del proyecto se disparará exponencialmente.
- **Infinite Loops**: Un orquestador mal configurado podría entrar en un bucle discutiendo con el Verify Agent sin llegar a una resolución.
- **Opacidad**: Es más difícil para el humano entender "por qué" el flujo tomó una decisión comparado con una máquina de estados determinística.

### Ready for Proposal
**Yes.** El concepto está claro y los riesgos identificados. Se puede redactar una propuesta (Proposal) formal para definir el esquema de configuración del Orquestador y las reglas exactas de cuándo debe intervenir en el flujo de EngramFlow.
