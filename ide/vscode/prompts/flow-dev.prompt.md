---
description: 'Implementa tareas del plan (Fase 3a) con Ralph Wiggum Loop'
agent: 'forge-dev'
---
<!-- forge-dev-handoff-deferred:2026-10-20 -->

Implementa la siguiente tarea del plan pendiente para la feature actual: $ARGUMENTS

Prerrequisitos:

- `plan.md` aprobado por humano (CKP-2 cerrado).
- Lee `.ai-work/{feature-slug}/plan.md` y toma la primera tarea con `[ ]` en orden (o la que indique el humano).

Ralph Wiggum Loop (auto-corrección):

1. Escribe el código.
2. Corre tests locales; si fallan, lee el error y corrige hasta 3 veces.
3. Si tras 3 intentos no compila/testea, marca la tarea como `[-] blocker`, escribe un resumen de causa raíz, y emite escalación (no se inventan fixes).
4. Marca la tarea `[x]` en `plan.md` solo cuando los tests pasan.

Backlog:

- Si hay un `rework_ticket.md` sin cerrar, **prioriza esa tarea antes** de cualquier otra.
- Aplica OWASP Top 10, SOLID, queries parametrizadas, N+1 awareness (ver `forge-dev-solid`, `forge-dev-security`, `forge-dev-performance`).

Output esperado:

- Cambios en archivos del producto con tests verdes.
- Checklist actualizado en `plan.md`.
- Si se cerró un rework, brevemente: 1 línea de causa raíz + 1 línea de fix aplicado.

Comandos siguientes:

- Plan completo → invoca `/flow-verify`.
- Faltan tareas → vuelve a invocar `/flow-dev` (puede tomar argumento opcional con el id de tarea, ej. `T-B3`).
