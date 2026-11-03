---
description: 'Muestra el estado del flujo FlowForge sin delegar'
agent: 'forge-orchestrator'
---
<!-- forge-dev-handoff-deferred:2026-10-20 -->

Muestra el estado actual del flujo FlowForge: $ARGUMENTS

Modo: **sólo lectura**, no delega a ningún agente de implementación. Es seguro invocarlo en cualquier momento para inspección.

Hace esto:

1. Busca `.ai-work/*/` y enumera features activas (ordenadas por fecha de modificación, más reciente arriba).
2. Para cada feature muestra:

```text
[slug]
  CKP-0 🔴  CKP-1 🟡  CKP-2 🟡  CKP-3 🔴  CKP-4 🟢
  Artefactos:
    ✓ context-map.md   dd-mm-yyyy
    ✓ spec.md          dd-mm-yyyy (status: draft|done)
    ✓ plan.md          dd-mm-yyyy (NN/XX tareas [x])
    ✓ verify-report.md dd-mm-yyyy (verdict: PASS|PASS_DEGRADADO|PENDING|REWORK)
    - summary.md
    - rework_ticket.md (abierto | cerrado)
  PM-* marcados [x]: N/M
```

3. Si el humano pide "continuar", delega a la fase que toque según los checkboxes. Si no, cierra con resumen.

Atajos:

- `/flow-status` sin argumentos → enumera todas las features.
- `/flow-status {slug}` → sólo esa feature.
- `/flow-status {slug} --continue` → avanza a la próxima fase pendiente (con confirmación previa del humano).

Reglas:

- No modifica archivos.
- Si una feature tiene `rework_ticket.md` abierto, lo marca con 🔴 al tope del resumen.
- Si `spec.md` no tiene PM-* marcados todos `[x]` y ya hay `verify-report.md`, sugiere `/flow-close` con advertencia.
