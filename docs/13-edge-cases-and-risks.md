# FlowForge — Edge Cases, Riesgos y Preguntas Abiertas

> **Tipo**: Risk Register & Brainstorming
> **Fecha**: 2026-05
> **Estado**: Vivo — se actualiza con nuevas preguntas y edge cases

---

## 1. Preguntas Técnicas Pendientes

¿Cómo piensas manejar las colisiones si dos agentes intentan escribir en el mismo archivo .md de respaldo al mismo tiempo? ¿Delegarás eso al sistema de archivos o tienes algún bloqueo ligero pensado?

## 2. ¿Qué más podría tener FlowForge? (Nuevas Capacidades)
El "Context Poisoning" Guardrail: En entornos reales, un agente puede alucinar o escribir un plan.md basado en una librería obsoleta que encontró en un engrama de hace un año. FlowForge podría tener una Fase de Validación de Engramas: antes de que la Fase 2 use un recuerdo, un agente ligero verifica si esa información sigue siendo válida respecto al código actual.

Conflict Resolution Agent (El Mediador): Si tienes dos agentes trabajando en ramas distintas pero que afectan al mismo Namespace de engram-dotnet, necesitas un agente que detecte colisiones antes del merge. Un "Watcher" que avise: "Oye, el Agente B está planeando cambiar la misma interfaz que tú estás refactorizando".

Costo de Observabilidad: Un dashboard (aunque sea en consola) que te diga cuánto te ha costado cada Fase en USD. "Esta Épica ha costado 1.20 USD en tokens asiáticos". Para una pyme, esto es oro para presupuestar.

## 3. ¿Dónde puede fallar en un entorno real?
La "Fuga de Intención" (Drift): El mayor fallo ocurre cuando el humano aprueba el plan.md (Checkpoint 2), pero durante la Fase 3 (Ejecución), el agente encuentra un problema técnico, decide improvisar y se desvía del plan. Si el agente no tiene la "humildad" de volver a la Fase 2, terminas con un código que no coincide con la documentación.

Solución: Un "Health Check" automático cada 5 commits donde un agente compara el código actual contra el plan.md.

Latencia de Consistencia en Multi-usuario: Si un dev en Colombia sube un engrama y otro en España está trabajando en lo mismo, la sincronización de engram-dotnet debe ser impecable. Si el Sync Cycle (Push → Replay → Pull) falla o es lento, el agente en España trabajará con "recuerdos falsos".

El "Efecto Teléfono Escacharrado": Al pasar de Fase 0 (Haiku) -> Fase 1 (Sonnet) -> Fase 3 (Sonnet), la pérdida de matices en los prompts puede ser crítica. Lo que el humano quería en la Fase 1 puede diluirse para cuando llega a la Fase 3.

Bloqueos de Archivos (El problema de los .md): Si usas el respaldo en .md y dos agentes intentan escribir el cierre de la Fase 4 al mismo tiempo, el sistema de archivos lanzará una excepción.

Sugerencia: Necesitas una Cola de Escritura (Message Queue) simple en engram-dotnet para que los archivos de respaldo se escriban de forma secuencial, incluso si las peticiones llegan en paralelo.

## 4. Casos de Uso que podrían "romperse"
Refactorizaciones Masivas: FlowForge parece ideal para "features" nuevas. Pero si le pides: "Cambia toda la lógica de autenticación de JWT a OAuth", el flujo de 5 fases puede volverse un bucle infinito de errores si los engramas viejos siguen sugiriendo la lógica de JWT.

Onboarding de nuevos Devs: Un desarrollador nuevo entra al equipo, no conoce FlowForge y empieza a escribir código "a la antigua" sin pasar por los checkpoints.

Fallo: El sistema de memoria se rompe porque hay cambios en el código que no tienen un "Lineage" (linaje) registrado en la base de datos.

Hardware Meh + Modelos Baratos: Si el modelo asiático es demasiado barato, puede que no entienda bien las reglas de .cursorrules complejas. El "semáforo" de FlowForge podría fallar si el modelo no tiene la capacidad de seguir instrucciones de control de flujo.


## 5. Pregunta Abierta

¿Has considerado si el skill `forge-orchestrator` debería tener la capacidad de "bloquear" el CKP-3 si el `forge-verify` no da el visto bueno al linaje de los datos?



