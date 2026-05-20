# Design: Orquestador AI Opcional

## Technical Approach

El "Escalation Manager" se implementará modificando la lógica del **Workflow Runner** (el motor o script que coordina las fases). El Runner interceptará la salida del Verify Agent (el archivo `rework_ticket.md`) y parseará el contador de ciclos. Si el ciclo supera el `max_retry_cycles` configurado en `.engram.json`, el Runner suspenderá la invocación del Dev Agent y disparará el prompt del AI Orchestrator pasándole el contexto del conflicto. Todo este diseño se documentará formalmente en los archivos de arquitectura de FlowForge.

## Architecture Decisions

### Decision: Ubicación del Interceptor

**Choice**: En el Workflow Runner (el script/orquestador externo).
**Alternatives considered**: Hacer que el Verify Agent decida invocar al Orquestador cuando falla la verificación.
**Rationale**: El Verify Agent debe mantenerse enfocado única y exclusivamente en verificar código contra los specs (Single Responsibility). La responsabilidad de decidir hacia dónde fluye el proceso de negocio recae naturalmente sobre el Workflow Runner.

### Decision: Carga de Contexto (Context Payload) del Orquestador

**Choice**: El Orquestador recibe `spec.md` (intención), `plan.md` (diseño), y `rework_ticket.md` (falla). No recibe el código fuente completo.
**Alternatives considered**: Pasar el código fuente entero más los logs de error.
**Rationale**: Pasar todo el código quemaría demasiados tokens. La mayoría de los bloqueos de 3 ciclos se deben a contradicciones entre el `spec` y el `plan`, o a un `plan` imposible. El Orquestador puede diagnosticar la contradicción con los artefactos de alto nivel.

## Data Flow

    [Happy Path - Determinístico]
    Dev Agent ──(código)──> Verify Agent ──(rework_ticket 1/3)──> Dev Agent ...

    [Escalation Path - Invoca IA]
    Verify Agent ──(rework_ticket 3/3)──> Workflow Runner ──(intercepta)──> AI Orchestrator
                                                                                 │
                                          ┌──────────────(Analiza)───────────────┘
                                          │
                            [Opción A] ───┴─── [Opción B]
                       Modifica plan.md        Detiene flujo
                      Resetea ciclo a 1/3      Checkpoint Humano
                              │                      │
                          Dev Agent               (Humano)

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `docs/01-engramflow-architecture.md` | Modify | Actualizar Sección 6 para detallar el rol de Escalation Manager y el flujo de intercepción del Workflow Runner. |
| `docs/06-ai-orchestrator.md` | Create | Nuevo documento dedicado al Orquestador AI: Prompts recomendados, estrategias de mitigación de bloqueos y ejemplos de configuración. |

## Interfaces / Contracts

El esquema JSON en `.engram.json` o en el Runner:

```json
{
  "engramFlow": {
    "orchestrator": {
      "enabled": true,
      "type": "ai",
      "model": "claude-3-7-sonnet-20250219",
      "escalation_triggers": {
        "max_retry_cycles": 3,
        "on_spec_plan_mismatch": true
      }
    }
  }
}
```

## Testing Strategy

| Layer | What to Test | Approach |
|-------|-------------|----------|
| Unit (Runner) | Lógica de Intercepción | Simular un `rework_ticket.md` con "Cycle 3/3" y verificar que el Runner invoca la función del Orquestador. |
| Unit (Runner) | Disable Flag | Configurar `enabled: false` y verificar que el ciclo 4/3 sigue enviándose al Dev Agent (o falla por defecto determinístico). |

## Migration / Rollout

No aplica. Se trata de una definición de Arquitectura en FlowForge. No requiere migración de datos.

## Open Questions

- [ ] Si el Orquestador modifica el `plan.md` para destrabar al Dev Agent, ¿debería notificar al usuario (Human Checkpoint informativo) sin bloquear el flujo?
- [ ] ¿Cómo parsea el Workflow Runner el ciclo de manera confiable? ¿Se debe imponer un formato YAML/JSON en el encabezado (frontmatter) de `rework_ticket.md` en lugar de texto libre?
