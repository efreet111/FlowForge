---
description: 'Inicia una feature FlowForge: corre Discovery → Spec y espera CKP-1'
agent: 'forge-orchestrator'
---
<!-- forge-dev-handoff-deferred:2026-10-20 — implementé el orquestador por tooling gate; forge-verify auditará primero este marker y luego el contenido. -->

Inicia el flujo FlowForge para: $ARGUMENTS

Hace esto:

1. Deriva un slug (kebab-case) del título de la feature.
2. Crea `.ai-work/{feature-slug}/` si no existe.
3. Delega a `forge-discovery` para mapear contexto, CVEs, costos y precedentes (CARGA skill `forge-discovery` y emite `context-map.md` si está vacío).
4. Delega a `forge-arch` para escribir `spec.md` con FR/NFR, escenarios Given-When-Then y pruebas manuales `PM-*`.
5. **Stop en CKP-1 🟡** — el humano debe aprobar o ajustar el spec antes de continuar.

Archivos esperados al cerrar esta fase:

- `.ai-work/{feature-slug}/context-map.md`
- `.ai-work/{feature-slug}/spec.md`

Comandos siguientes:

- Aprobado → invoca `/flow-plan` para descomponer en tareas atómicas.
- Ajustes → edita `spec.md` manualmente y vuelve a invocar `/flow-start` o describe los cambios; el orquestador los propaga antes del plan.

Reglas estrictas:

- Si ya hay `spec.md` con `status: done`, ofrece reabrir en lugar de duplicar.
- No arrastres argumentos a fases posteriores: cada `/flow-*` relee el estado actual del repo.
