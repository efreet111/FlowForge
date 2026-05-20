# Proposal: Orquestador AI Opcional

## Intent

Proporcionar a los equipos una capa inteligente de recuperación ante fallos (Escalation Manager) dentro de la metodología EngramFlow, sin destruir la rentabilidad del proyecto por consumo excesivo de tokens (Token Bleed). Este cambio introduce un Orquestador AI que interviene *exclusivamente* cuando el flujo determinístico (basado en artefactos) encuentra un bloqueo o excepción compleja.

## Scope

### In Scope
- Definir el esquema JSON de configuración (`orchestrator` block) para activar el orquestador AI.
- Establecer las reglas de intervención (ej. 3 reworks fallidos, discrepancias de contratos).
- Diseñar el flujo de decisión del Orquestador (Reasignar, Rollback, Checkpoint Humano).

### Out of Scope
- Reemplazar el flujo determinístico base (los agentes seguirán disparándose por la existencia de `spec.md`, `plan.md`, etc.).
- Desarrollar un nuevo LLM-router de bajo nivel (eso es el Model Router, otra iniciativa).

## Capabilities

### New Capabilities
- `workflow-orchestration`: Configuración y reglas de intervención del Orquestador AI opcional como Escalation Manager.

### Modified Capabilities
- None

## Approach

Implementaremos un **Enfoque Híbrido**. El motor de EngramFlow mantendrá su arquitectura de máquina de estados determinística (Approach 1 de la Exploración) para el 95% de los ciclos (el *Happy Path*). El Orquestador AI será invocado únicamente como un *Exception Handler* cuando se detecten bucles (ej. count en `rework_ticket.md` > 3) o ambigüedades. Esto se configurará vía `.engram.json`.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `docs/01-engramflow-architecture.md` | Modified | Actualizar la sección 6 con el concepto de Escalation Manager. |
| `docs/06-ai-orchestrator.md` | New | Documentación profunda del comportamiento del orquestador. |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Consumo excesivo de tokens | Low | Estricto control de invocación: solo se llama tras fallos repetidos, no en el Inner Loop. |
| Orquestador entra en loop con otros agentes | Med | Forzar un Human Checkpoint si el Orquestador falla en su primer intento de destrabar el flujo. |

## Rollback Plan

Dado que esto es un cambio puramente arquitectónico y metodológico en la documentación de FlowForge, el rollback consiste en revertir los commits en la carpeta `docs/` y marcar el Orquestador AI como "Deprecado/Descartado" en el roadmap.

## Dependencies

- Ninguna dependencia técnica externa. Depende conceptualmente de la arquitectura base de EngramFlow v0.2.

## Success Criteria

- [ ] La documentación define claramente cuándo despierta y cuándo duerme el Orquestador AI.
- [ ] El esquema de configuración JSON propuesto permite apagar el Orquestador completamente (`orchestrator.enabled: false`).
