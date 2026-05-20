# Memoria Cruzada (Épicas) y Estrategia de Fallback

> **Versión**: 1.0 (Refinamiento Arquitectónico)
> **Tema**: Integración de Historias de Usuario (HUs) y Graceful Degradation

La metodología EngramFlow asume que el conocimiento se acumula con el tiempo. Sin embargo, en el mundo real, los desarrollos no ocurren en el vacío. Una nueva Historia de Usuario (HU) casi siempre depende del contexto de una Épica mayor o de HUs previas.

Este documento formaliza cómo la **Fase 0 (Discovery)** mapea la memoria cruzada y cómo el sistema sobrevive si el motor `engram-dotnet` no está disponible.

---

## 1. Fase 0: Context Discovery (Mapeo de Épicas)

Antes de que el `Arch Agent` escriba una sola línea de especificación (Fase 1), el Orquestador invoca la **Fase 0**.

### El Rol del Discovery Agent (`@forge-discovery`)
- **Misión**: Leer el requerimiento inicial del humano y buscar en la memoria del proyecto (vía MCP o archivos locales) cualquier contexto relacionado, restricciones o Épicas previas.
- **Model Routing**: Como esta fase es de puramente de lectura y clasificación (Data Fetching), se debe usar un modelo **rápido y barato** (como Claude 3 Haiku, Gemini 1.5 Flash o GPT-4o-mini). No se requiere razonamiento arquitectónico pesado aquí.

### Proceso de Mapeo de Memoria Cruzada
Cuando se inicia una nueva HU (ej. "Agregar login con Google"), el Discovery Agent realiza lo siguiente:
1. Extrae las *keywords* principales (ej: "login", "auth", "security").
2. Consulta la base de memoria buscando esas keywords.
3. Si encuentra engramas previos (ej. una HU anterior que implementó JWT), extrae sus IDs o referencias.
4. Genera un **Mapa de Asociaciones** (Association Map) que inyecta en el prompt inicial del `Arch Agent`.

*Ejemplo del output de Fase 0 (entregado al Arch Agent):*
> "Atención Arch Agent: Para esta nueva HU de Login con Google, tené en cuenta que pertenecemos a la **Epic: Identity v2**. La memoria indica que en la HU anterior (engrama #104) se definió que TODAS las cookies deben ser `HttpOnly`. Asegurate de incluir esto en tu `spec.md`."

---

## 2. Estrategia de Fallback (Graceful Degradation)

El diseño ideal de FlowForge asume que el backend **`engram-dotnet`** está corriendo en el puerto local, brindando capacidades de búsqueda semántica (Vector DB/SQLite) y un servidor MCP robusto. 

Pero, ¿qué pasa si un equipo no quiere instalar .NET o no tiene el servidor levantado?
La respuesta es **Graceful Degradation (Degradación Elegante)**.

### El Protocolo de Fallback a Archivos Locales
Si la conexión a `engram-dotnet` falla (timeout o conexión rechazada), el Orquestador y los Agentes deben cambiar automáticamente a un **modo de archivo estático (File-based Memory)**.

1. **Ubicación**: En lugar de hacer HTTP POST, el `Memory Agent` escribe el engrama como un archivo Markdown estructurado en una carpeta oculta dentro del proyecto:
   `./.engram/local_memory/obs-<timestamp>.md`

2. **Estructura del Engrama Local**:
   El archivo utilizará YAML Frontmatter para simular la metadata de la base de datos:
   ```markdown
   ---
   title: "Fixed N+1 en lista de usuarios"
   type: "bugfix"
   topic_key: "performance/user-list"
   date: "2026-05-19"
   ---
   
   ## What
   Se implementó Include() en EF Core para pre-cargar los roles.
   
   ## Why
   ...
   ```

3. **Búsqueda en Fallback**:
   Cuando el `Discovery Agent` necesita buscar memoria, y `engram-dotnet` no responde, en lugar de usar la herramienta `mem_search`, ejecuta un `grep` (o búsqueda por expresiones regulares) sobre la carpeta `./.engram/local_memory/*.md` para encontrar las *keywords*.

Esta arquitectura garantiza que FlowForge funcione al 100% como metodología incluso en entornos totalmente desconectados, simples, o donde instalar dependencias externas de backend sea imposible.
