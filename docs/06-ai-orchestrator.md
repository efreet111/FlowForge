# El Orquestador: Semáforo y Director de FlowForge

> **Versión**: 1.0 (Refinamiento Arquitectónico)
> **Rol**: Semáforo Principal y Director de Estado

Anteriormente concebido solo como un manejador de escaladas opcional, el **Orquestador** es en realidad el **Motor Core** de la metodología EngramFlow. Su rol es actuar como el **Semáforo Principal**: lee el estado del proyecto, decide qué agente debe ejecutarse, maneja las transiciones de fase y, lo más importante, se detiene para pedir clarificación al humano cuando detecta ambigüedad.

## 1. El Orquestador SIEMPRE es una Inteligencia (El Semáforo Inteligente)

Una confusión común es pensar que el "Orquestador" puede ser simplemente un script rígido (`.sh`) o un programa de terminal que llama a APIs. **Esto es incorrecto en EngramFlow.**

El Orquestador es obligatoriamente un **Agente de IA (Inteligencia Artificial)** cargado con la skill maestra `@forge-orchestrator`. ¿Por qué? Porque su rol crítico es **entender y verificar que lo que el usuario pide tiene lógica de negocio y viabilidad** antes de gastar recursos.

- **El Semáforo Inteligente**: Si vos pedís *"agregá telemetría al proyecto"*, un Runner ciego intentaría avanzar. Pero nuestro **Agente Orquestador** (inyectado vía reglas) usa su inteligencia para encender el Semáforo en **ROJO**, detenerse y preguntarte: *"No me dijiste qué proveedor de telemetría vamos a usar. Definilo antes de avanzar."*
- **Agnóstico por Reglas, NO por Scripts**: A diferencia de flujos pesados que requieren instalar runners `.sh` o CLI tools en tu máquina, FlowForge es **100% nativo de tu IDE**. La "instalación" consiste simplemente en copiar las reglas maestras (`@forge-orchestrator` y las skills) a la carpeta de configuración de tu entorno (`.cursorrules`, `.clinerules`, `.agent`, `.windsurf`). El motor de tu IDE (su Master Agent nativo) lee estas reglas y se comporta como nuestro Orquestador, sin necesidad de ejecutar ningún script externo de terminal.

## 2. Máquina de Estados (El Semáforo)

El Agente Orquestador ejecuta rígidamente esta máquina de estados:

1. **Fase 0: Discovery (`@forge-discovery`)**
   - **Trigger**: Se crea una nueva HU (Historia de Usuario).
   - **Acción**: Lee la memoria local o consulta a `engram-dotnet` para buscar Épicas relacionadas o HUs anteriores.
2. **Fase 1: Intención (`@forge-arch`)**
   - **Acción**: Escribe `spec.md`. 
   - **Semáforo**: `AMARILLO` (Checkpoint 1). El flujo se PAUSA obligatoriamente hasta que el humano apruebe el documento.
3. **Fase 2: Arquitectura (`@forge-plan`)**
   - **Acción**: Escribe `plan.md`.
   - **Semáforo**: `AMARILLO` (Checkpoint 2). El flujo se PAUSA esperando aprobación humana.
4. **Fase 3: Ejecución y Validación (`@forge-dev` <-> `@forge-verify`)**
   - **Acción**: Inner Loop de TDD. 
   - **Semáforo**: `VERDE` (Flujo autónomo). El Orquestador permite que el Dev y el Verify iteren basándose en el `rework_ticket.md`.
   - **Freno de Emergencia**: Si el contador de reworks (leído del ticket) excede los 3 intentos, el Semáforo se pone en `ROJO`. Detiene la ejecución y escala al humano (Orquestador como Escalation Manager).
5. **Fase 4: Cierre (`@forge-memory`)**
   - **Acción**: Tras un PASS del revisor, se extrae el conocimiento y se guarda en memoria.
   - **Semáforo**: FIN DEL FLUJO.

## 3. Human-in-the-Loop: Hard Stops (Puntos Claros de Parada)

El Orquestador no debe iniciar ni avanzar si hay dudas. Los "Hard Stops" (Semáforo Rojo) ocurren cuando:
- **Ambigüedad de Negocio**: El usuario pide *"mejorar el login"* sin especificar cómo. El Orquestador frena en la Fase 0 y exige: *"¿Mejorar en velocidad, en UI, o agregar OAuth?"*
- **Falta de Viabilidad**: El `Arch Agent` nota que la API requerida por el usuario no existe. El Orquestador detiene el flujo antes de generar el `spec.md`.

Esta integración humana asegura que el código resultante nunca sea fruto de una alucinación del modelo sobre requerimientos inexistentes.
