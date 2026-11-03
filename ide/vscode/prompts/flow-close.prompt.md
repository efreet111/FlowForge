---
description: 'Cierra la feature: ejecuta PM-* y emite summary + ADR promotions'
agent: 'forge-memory'
---
<!-- forge-dev-handoff-deferred:2026-10-20 -->

Cierra la feature FlowForge: $ARGUMENTS

Prerrequisitos (verifica antes de continuar):

- `verify-report.md` con verdict = `PASS` o `PASS_DEGRADADO` (PENDING/REWORK abortan este comando).
- `spec.md` con todas las pruebas manuales `PM-*` marcadas `[x]`. Si falta alguna, **bloquea** y lista cuáles requieren ejecución humana.
- No debe haber `rework_ticket.md` abierto.

Hace esto:

1. Por cada `PM-*` todavía `[ ]`, emite recordatorio al humano (no se auto-califican).
2. Ejecuta el protocolo Memory Curation:
   - Sintetiza aprendizajes (`summary.md`) en lenguaje humano: qué se hizo, qué se aprendió, qué quedó en backlog.
   - Promueve ADRs en estado `Proposed` → `Accepted` cuando su HU relacionada cierre.
   - Limpia retenciones (artefactos con `status: done` y > 90 días van a `archive/`).
   - Persiste **Memory Signal** en la base de patterns si es insight reusable.
3. **Stop en CKP-4 🟢** — consulta al humano: ¿deploy?
   - Si "sí" → sugiere comandos concretos de release (no los ejecuta).
   - Si "no" → marca `paused` en `summary.md` y queda la feature archivada lógicamente.

Output obligatorio:

- `.ai-work/{feature-slug}/summary.md`.
- Actualización de `BACKLOG-MASTER.md`: el item relacionado pasa a ✅ done (o 🟡 si degradado).
- `.ai-work/{feature-slug}/status: done` en `spec.md`.
- Si hubo insights, línea en `docs/decisions/` patrón registrado.

Reglas:

- Forzar bloqueo antes que cierre silencioso: si una PM-* no es testeable (entorno roto), el comando queda en `paused` con la PM-* en cuestión etiquetada `[skipped-env]`.
- El orquestador NO publica binarios ni hace commits; eso es tarea del humano.
