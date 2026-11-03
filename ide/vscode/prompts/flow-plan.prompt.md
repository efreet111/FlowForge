---
description: 'Genera plan.md atómico (Fase 2) y espera CKP-2'
agent: 'forge-orchestrator'
---
<!-- forge-dev-handoff-deferred:2026-10-20 -->

Genera el plan de tareas atómicas para la feature actual: $ARGUMENTS

Prerrequisitos:

- `spec.md` aprobado por humano (CKP-1 cerrado). Si no lo está, **aborta y sugiere `/flow-start` primero**.

Hace esto:

1. Localiza `.ai-work/*/spec.md` con `status: draft` o ya aprobado pero sin `plan.md`.
2. Delega a `forge-plan` para descomponer el spec en tareas discretas con:
   - Contratos (entradas / salidas / efectos colaterales).
   - Design patterns aplicables (SOLID, parámetros, idempotencia).
   - Estimación de esfuerzo.
3. Escribe `.ai-work/{feature-slug}/plan.md` con un checklist en formato `-[ ] T-A1 …`.
4. **Stop en CKP-2 🟡** — luz verde humana antes de `/flow-dev`.

Comandos siguientes:

- Aprobado → invoca `/flow-dev` para implementar.
- Desglose adicional → pide más tareas en lenguaje natural; `forge-plan` las agrega numeradas.

Reglas estrictas:

- El plan NO incluye código; sólo contratos.
- Cualquier migración de datos o cambio de contrato externo dispara un ADR-RAFT (ver ADR-002).
