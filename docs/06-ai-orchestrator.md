# El Orquestador: Semáforo y Director de FlowForge

> **Versión**: 1.1 (Checkpoints Normalizados CKP-0 → CKP-4)
> **Rol**: Semáforo Principal y Director de Estado

Anteriormente concebido solo como un manejador de escaladas opcional, el **Orquestador** es en realidad el **Motor Core** de la metodología EngramFlow. Su rol es actuar como el **Semáforo Principal**: lee el estado del proyecto, decide qué agente debe ejecutarse, maneja las transiciones de fase y, lo más importante, se detiene para pedir clarificación al humano cuando detecta ambigüedad.

## 1. El Orquestador SIEMPRE es una Inteligencia (El Semáforo Inteligente)

Una confusión común es pensar que el "Orquestador" puede ser simplemente un script rígido (`.sh`) o un programa de terminal que llama a APIs. **Esto es incorrecto en EngramFlow.**

El Orquestador es obligatoriamente un **Agente de IA (Inteligencia Artificial)** cargado con la skill maestra `@forge-orchestrator`. ¿Por qué? Porque su rol crítico es **entender y verificar que lo que el usuario pide tiene lógica de negocio y viabilidad** antes de gastar recursos.

- **El Semáforo Inteligente**: Si vos pedís *"agregá telemetría al proyecto"*, un Runner ciego intentaría avanzar. Pero nuestro **Agente Orquestador** (inyectado vía reglas) usa su inteligencia para encender el Semáforo en **ROJO**, detenerse y preguntarte: *"No me dijiste qué proveedor de telemetría vamos a usar. Definilo antes de avanzar."*
- **Agnóstico por Reglas, NO por Scripts**: A diferencia de flujos pesados que requieren instalar runners `.sh` o CLI tools en tu máquina, FlowForge es **100% nativo de tu IDE**. La "instalación" consiste simplemente en copiar las reglas maestras (`@forge-orchestrator` y las skills) a la carpeta de configuración de tu entorno (`.cursorrules`, `.clinerules`, `.agent`, `.windsurf`). El motor de tu IDE (su Master Agent nativo) lee estas reglas y se comporta como nuestro Orquestador, sin necesidad de ejecutar ningún script externo de terminal.

## 2. Máquina de Estados (El Semáforo — CKP Normalizados)

El Agente Orquestador ejecuta rígidamente esta máquina de estados con 5 puntos de control:

| Fase | Agente | Checkpoint | Color | Tipo | Acción |
|------|--------|-----------|-------|------|--------|
| 0: Discovery | `@forge-discovery` | **CKP-0** | 🔴 HARD STOP | Binario, inapelable | Requerimiento vago → PARAR y pedir clarificación |
| 1: Intención | `@forge-arch` | **CKP-1** | 🟡 SEMÁFORO AMARILLO | Flexible, decide humano | *"spec.md listo. ¿Aprobás?"* |
| 2: Arquitectura | `@forge-plan` | **CKP-2** | 🟡 SEMÁFORO AMARILLO | Flexible, decide humano | *"plan.md listo. ¿Luz verde?"* |
| 3: Ejecución | `@forge-dev` ↔ `@forge-verify` | **CKP-3** | 🔴 FRENO EMERGENCIA | Mecánico (3 ciclos) | Reworks > 3 → ESCALAR al humano |
| 4: Cierre | `@forge-memory` | **CKP-4** | 🟢 DEPLOY GATE | Flexible, decide humano | *"¿Deployeamos?"* |

### Detalle por Fase

1. **Fase 0: Discovery — CKP-0 🔴 HARD STOP**
   - **Trigger**: Se crea una nueva HU (Historia de Usuario).
   - **Acción**: Lee la memoria local o consulta a `engram-dotnet` para buscar Épicas relacionadas o HUs anteriores.
   - **Hard Stop**: Si el requerimiento es vago (*"mejorar el login"*) o no hay contexto previo, DETENER TODO. Es binario, no admite "más o menos".

2. **Fase 1: Intención — CKP-1 🟡 SEMÁFORO AMARILLO**
   - **Acción**: Escribe `spec.md`.
   - **Gate**: El flujo se PAUSA. El humano decide si aprueba o pide ajustes.

3. **Fase 2: Arquitectura — CKP-2 🟡 SEMÁFORO AMARILLO**
   - **Acción**: Escribe `plan.md`.
   - **Gate**: El flujo se PAUSA. El humano decide si da luz verde.

4. **Fase 3: Ejecución y Validación — Inner Loop Autónomo**
   - **Acción**: Inner Loop de TDD (Dev ↔ Verify iterando).
   - **Semáforo**: `VERDE` (Flujo autónomo). El Orquestador permite que el Dev y el Verify iteren.
   - **CKP-3 🔴 Freno de Emergencia**: Si el contador de reworks excede 3 intentos, el Semáforo se pone en `ROJO`. Escala al humano (o el Orquestador AI modifica el plan y resetea).

5. **Fase 4: Cierre — CKP-4 🟢 DEPLOY GATE**
   - **Acción**: Tras un PASS del revisor, se extrae el conocimiento y se guarda en memoria.
   - **Gate**: El humano decide si deployear. No es un freno — es una decisión de release.

## 3. Human-in-the-Loop: Tipos de Parada

El Orquestador distingue dos tipos de parada con severidad y consecuencias muy diferentes:

### 🔴 Hard Stops (CKP-0, CKP-3) — Binarios e Inapelables

No admiten grises. El agente NO puede decidir avanzar por su cuenta:

- **CKP-0 — Ambigüedad de Negocio**: El usuario pide *"mejorar el login"* sin especificar cómo. El Orquestador frena en la Fase 0 y exige: *"¿Mejorar en velocidad, en UI, o agregar OAuth?"*
- **CKP-0 — Falta de Viabilidad**: El `Arch Agent` nota que la API requerida por el usuario no existe. El Orquestador detiene el flujo antes de generar el `spec.md`.
- **CKP-3 — Límite de Ciclos**: 3 reworks fallidos → escalación inmediata. El humano DEBE intervenir.

### 🟡 Semáforo Amarillo (CKP-1, CKP-2) — Flexibles con Autoridad Humana

El agente se detiene y CONSULTA. El humano tiene la última palabra y puede aprobar specs/planes con partes abiertas:

- **CKP-1**: *"spec.md generado. ¿Aprobás o querés ajustar algo?"*
- **CKP-2**: *"plan.md generado. ¿Luz verde para codificar?"*

La defensa contra specs/planes débiles aprobados en CKP-1/2 es la **Capability Matrix**: los items marcados como `deterministic` son auditados rigurosamente por el Verify Agent en CKP-3.
