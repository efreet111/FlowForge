---
description: 'Audita implementación contra spec (Fase 3b) y emite verdict'
agent: 'forge-verify'
---
<!-- forge-dev-handoff-deferred:2026-10-20 -->

Audita línea por línea la implementación contra el spec de la feature actual: $ARGUMENTS

Prerrequisitos:

- `plan.md` con todas las tareas `[x]`. Si falta alguna, **aborta** con sugerencia: invoca `/flow-dev` primero.

Hace esto:

1. Carga `spec.md` (FR/NFR) + `plan.md` (checklist).
2. Para cada FR y cada item del plan, verifica que el código cumple el contrato definido.
3. Chequea criterios transversales: OWASP ASVS, SOLID (post-coding), N+1, idempotencia, logs estructurados, manejo de errores.
4. Para cada Manual Test `PM-*` del `spec.md`: **NO lo califica** (es potestad del humano). Sólo verifica que existe y tiene condiciones Given/When/Then completas.
5. Emite UN verdict:

| Verdict | Acción |
|---------|--------|
| `PASS` | No hay issues. forge-memory puede cerrar. |
| `PASS_DEGRADADO` | Cumple el spec pero con observaciones no-bloqueantes (registradas en el reporte). |
| `PENDING` | Información insuficiente para auditar (pide aclaración). |
| `REWORK` | Hay issues bloqueantes. Emite `rework_ticket.md` con causa raíz reproducible. |

Output obligatorio:

- `.ai-work/{feature-slug}/verify-report.md` — SIEMPRE se escribe, incluso en PASS.
- Si verdict = `REWORK`, también `.ai-work/{feature-slug}/rework_ticket.md` (causa raíz + tarea sugerida).

Reglas:

- Trazabilidad: cada hallazgo referencia FR/NFR/STRIDE/OWASP id + archivo:línea concretos.
- Si 3 ciclos de REWORK se cumplen (CKP-3 🔴), escala al humano sin emitir 4º report.
