# Protocolo de Delegación y Configuración (FlowForge)

> **Versión**: 1.0 (Refinamiento Arquitectónico)
> **Tema**: Delegación Multi-Agente, Configuración CLI y Persona del Orquestador

Para que FlowForge funcione fluidamente en cualquier ecosistema de IA, el **Agente Orquestador (`@forge-orchestrator`)** requiere de reglas claras de cómo pasarle el control a las otras 6 skills y cómo adaptar su comportamiento según las preferencias del equipo.

---

## 1. El Dilema de la Delegación (El Pase de Testigo)

Las plataformas de inteligencia artificial tienen distintas capacidades:
- Algunas solo chatean con el humano (VS Code Copilot, web chat).
- Otras tienen herramientas complejas y pueden lanzar "sub-agentes" de forma autónoma (Antigravity, OpenCode, Cursor Composer, Cline).

Para no dejar "mocho" a los sistemas avanzados, pero seguir soportando los monolíticos, el Orquestador maestro de FlowForge utiliza el **Patrón de Delegación Adaptativa (Inspirado en Gentle-AI)**.

### Reglas de Delegación del Orquestador
1. **Verificar Capacidad Autónoma**: El Orquestador analiza sus propias herramientas disponibles. Si el entorno le permite ejecutar comandos nativos o lanzar sub-agentes (ej. tiene una tool `call_agent`), **el Orquestador lo hace por su cuenta**. Pasa a la siguiente fase sin pedirle ayuda al desarrollador.
2. **Caída a Modo Asistido (Human-in-the-Loop)**: Si el Orquestador no tiene cómo ejecutar comandos (o la plataforma no lo soporta), preparará el comando exacto y le dirá al humano:
   > *"Por favor, invocá a `@forge-dev` para comenzar la ejecución."*
3. **Inyección de Contexto Constante**: No basta con llamar a `@forge-dev`. El Orquestador le indica explícitamente al sub-agente las rutas de los archivos que necesita leer (ej. `"Leé el plan en openspec/changes/plan.md y ejecutá..."`).

---

## 2. Configuración y CLI Wizard

Toda la configuración del comportamiento del Orquestador y de las Skills se maneja de forma eficiente **por consola (terminal)**. No se requieren interfaces web para instalar FlowForge.

### El Comando de Instalación (`forge-wizard` / `npx flowforge init`)
En un futuro cercano, el script de inicialización guiará al usuario paso a paso en la terminal para configurar:
1. **Agentes y Modelos**: Seleccionar qué LLM (ej. Haiku, Sonnet, GPT-4o) utilizará cada fase del ciclo.
2. **API Keys**: Almacenar las credenciales de forma segura.
3. **Persona del Agente**: Definir el tono de comunicación.
4. **Endpoint de Engram**: Decidir a qué base de datos se conectará `engram-dotnet` (Local SQLite, Nube SQLite o PostgreSQL Remoto).

### Archivo de Estado `.flowforge.json`
El resultado de ese wizard se almacena en un archivo JSON en la raíz de tu proyecto (o se inyecta en el existente `.engram.json` bajo la llave `"forge"`). 

Ejemplo de estructura documentada:

```json
{
  "forge": {
    "orchestrator_mode": "adaptive",
    "persona": "Arquitecto de software Senior, tono formal y directo. Corrige si el usuario se equivoca.",
    "agents": {
      "forge-discovery": {
        "model": "claude-3-haiku-20240307",
        "provider": "anthropic"
      },
      "forge-arch": {
        "model": "claude-3-5-sonnet-20241022",
        "provider": "anthropic"
      }
    },
    "database_engine": {
      "type": "postgres",
      "connection_string_env": "ENGRAM_DB_URL"
    }
  }
}
```

El **Orquestador** lee este archivo JSON al arrancar, asume su "Persona", y ajusta la lógica de qué agente rutear según los proveedores configurados. Si querés modificar el comportamiento de tus agentes, podés editar este archivo a mano o volver a correr el Wizard CLI.
