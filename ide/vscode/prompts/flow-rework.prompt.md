---
description: 'Reporta bug o reabre rework para una feature existente'
agent: 'forge-orchestrator'
---
<!-- forge-dev-handoff-deferred:2026-10-20 -->

Reporta un error o reabre el rework sobre la feature actual: $ARGUMENTS

Regla fundamental: **el orquestador NO arregla código inline**. Siempre crea o actualiza un rework ticket y delega.

Hace esto:

1. Si el humano proveyó un fallo concreto (stack trace, output, comportamiento esperado vs real), extrae:
   - Síntoma (qué vio)
   - Pasos para reproducir
   - Causa raíz probable (si la identifica)
   - Archivos sospechosos (si los hay)
2. Crea o actualiza `.ai-work/{feature-slug}/rework_ticket.md` con la estructura:
   - Síntoma
   - Root cause (verificado si hay datos, hipotético si no)
   - Alcance del fix (qué archivos, qué tests)
   - Criterios de aceptación (con PM-* si son pruebas nuevas)
   - Memory Signal preliminar
3. Delega a `forge-dev` (no a sí mismo) — el agente activo cambia a `forge-dev` para la próxima tarea.

Para desburocratizar:

- Si decís "reporté un error" sin más contexto, el orquestador pregunta una sola vez por lo mínimo (síntoma) y crea el ticket en modo borrador.
- Si decís "falló la prueba X con output Y", el ticket queda cerrado para que `forge-dev` lo arregle sin más preguntas.

Output esperado:

- `.ai-work/{feature-slug}/rework_ticket.md` (creado o actualizado).
- Próximo comando sugerido: `/flow-dev` (para tomar la tarea).
