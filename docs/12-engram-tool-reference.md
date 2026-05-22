# Exploración: Análisis de Capacidades Avanzadas de engram-dotnet

## Estado Actual
Actualmente, los agentes de IA (nosotros incluidos) solemos limitarnos a un uso muy elemental de `engram-dotnet`: guardar memorias sueltas con `mem_save` y buscar con `mem_search`. Esto es desperdiciar una **locura cósmica** de potencia. 

`engram-dotnet` no es solo una base de datos de texto; es un motor de memoria neuronal de dos niveles, con sincronización colaborativa en equipo (Offline-First), herramientas de auditoría automática de código mediante LLM-as-Judge, generación automática de documentación técnica (ADRs), exportación a Obsidian con grafos de relaciones, y diagnósticos robustos de entorno. 

Para sacarle el 100% del provecho, nuestros agentes tienen que entender qué hace cada módulo y **cuándo** invocar cada una de las herramientas MCP.

---

## Áreas y Capacidades del Sistema

`engram-dotnet` está estructurado en 6 grandes superpoderes que el agente puede (y debe) explotar en su flujo diario:

```
engram-dotnet (25 Herramientas MCP)
├── 1. Core Memory Layer (Deduplicación, FTS, Control de Sesión)
├── 2. Colaboración & Sync (Multi-User, Offline-First, HTTP Proxy)
├── 3. LLM-as-Judge Verification (Auditoría de Requisitos, Trazabilidad, Rework)
├── 4. Markdown ADR Promotion (Nivel 2: Documentos Versionados)
├── 5. Obsidian Knowledge Sync (Vaults, Wikis, Grafos de Relación)
└── 6. Diagnostics (Doctor, Retention Pruning)
```

---

## 1. Core Memory Layer (Nivel 1: Memoria Operativa)

Es la base del sistema. Permite capturar el contexto de desarrollo y recuperarlo de forma ultra rápida.

### Características Clave:
*   **FTS Avanzado (Full-Text Search)**: Usa SQLite FTS5 de forma local y PostgreSQL `tsvector` + índices GIN en la nube. Las consultas se sanitizan automáticamente para evitar errores de sintaxis si el usuario escribe caracteres especiales.
*   **Deduplicación en Ventana (15m)**: Si el agente intenta guardar información repetida consecutivamente, el sistema no duplica la fila; calcula un hash SHA-256 normalizado del contenido y simplemente incrementa un contador `duplicate_count`.
*   **Temas Evolutivos (`topic_key`)**: Permite actualizar una memoria en lugar de crear una nueva. Si se usa el mismo `topic_key` (ej: `architecture/auth-model`), la observación se actualiza y sube `revision_count`. ¡Ideal para diseños que van cambiando!
*   **Captura Pasiva (`mem_capture_passive`)**: Parsea texto plano, chat logs o salidas de consola buscando secciones como `## Key Learnings:` para extraer y guardar aprendizajes estructurados de manera 100% automatizada.

### Herramientas MCP del Core:
*   `mem_session_start`: Inicializa una sesión asignándola a un proyecto específico.
*   `mem_save`: Guarda una observación estructurada bajo el formato canónico (**What / Why / Where / Learned**).
*   `mem_search`: Busca memorias. **IMPORTANTE**: Devuelve resultados truncados (`[preview]`). ¡Siempre hay que llamar a `mem_get_observation` con el ID para ver el contenido completo!
*   `mem_get_observation`: Devuelve el contenido completo y real de una memoria.
*   `mem_suggest_topic_key`: Ayuda al agente a generar una clave estable y normalizada antes de guardar (ej: espacios a guiones, minúsculas, etc.).
*   `mem_update`: Edita o corrige una observación directamente por su ID.
*   `mem_context`: Recupera el resumen de las últimas sesiones y las memorias más importantes (tanto personales como de equipo). Es la primera herramienta que debe correrse al iniciar el día.
*   `mem_timeline`: Permite ver qué pasó cronológicamente antes y después de una observación específica para reconstruir el contexto del bug.
*   `mem_save_prompt`: Guarda los prompts literales del usuario para mantener la trazabilidad de sus intenciones.
*   `mem_session_summary`: Al finalizar las tareas, genera un resumen estructurado con las metas, descubrimientos y archivos afectados para que el próximo agente no empiece a ciegas.
*   `mem_session_end`: Cierra la sesión activa.

---

## 2. Colaboración & Sync (Offline-First Multi-User)

Soluciona el problema de que los miembros del equipo no compartan contexto. Combina la velocidad del SQLite local con la robustez de un servidor central.

### Características Clave:
*   **Estrategia de Sync Bidireccional**: Las escrituras locales son inmediatas. Un administrador de sincronización en segundo plano (`SyncManager`) encola las mutaciones y las envía en batches mediante HTTP al servidor remoto (que corre PostgreSQL).
*   **Aislamiento y Namespacing**: El sistema de isolation implementa aislamiento multi-usuario mediante el header `X-Engram-User`.
*   **Scopes de Memoria**:
    *   `scope="team"`: Se sincroniza al servidor de base de datos compartida y es visible para todo el equipo.
    *   `scope="personal"`: Se mantiene 100% privado en tu SQLite local y en el servidor remoto se aísla automáticamente bajo `personal:{usuario}`.
*   **Resolución de Conflictos Deferida**: Si una mutación pulled falla por una clave foránea (ej: la sesión asociada aún no se ha descargado), la encola en `sync_apply_deferred` y la reintenta automáticamente en el siguiente ciclo sin bloquear el cursor de sincronización. Gana siempre la última escritura (`Last-Write-Wins`).

### Herramientas MCP y HTTP de Sync:
*   `mem_doctor`: Diagnóstica si el endpoint del servidor HTTP está online y accesible.
*   `POST /sync/enroll` / `DELETE /sync/enroll`: Registra o desregistra un proyecto en el bucle de sincronización en la nube.
*   `POST /sync/pause`: Pausa temporalmente la sincronización del proyecto por mantenimiento.
*   `GET /sync/status`: Obtiene métricas en tiempo real del sync (número de cursor local vs nube, fallos de red, etc.).

---

## 3. LLM-as-Judge Verification (Fase de Verificación)

¡Esto es oro puro para el flujo SDD! Permite verificar de forma autónoma si el código escrito realmente cumple con lo que exige la especificación.

### Características Clave:
*   **LLM-as-Judge**: Toma un archivo `spec.md` escrito en formato canónico (requisitos funcionales como `- RF-001` y no funcionales como `- RNF-001`) y el diff de git. Envía el contexto al modelo evaluador (Claude) para dictaminar si el código cumple la spec.
*   **Veredicto Estructurado**: Retorna un JSON con veredictos individuales (`Pass`, `Fail`, `Untested`), justificaciones detalladas y evidencia de archivos/líneas de código.
*   **Cycle Tracker (Rework limits)**: Rastrea cuántas veces se ha intentado verificar el mismo cambio. Si llega al límite de ciclos de corrección configurado (por defecto 3), detiene el reintento automático y escala el problema a un desarrollador humano para evitar bucles infinitos del agente de desarrollo.
*   **Matriz de Trazabilidad**: Mapea dinámicamente cada requisito de la spec a archivos específicos del código fuente.

### Herramientas MCP de Verificación:
*   `mem_verify_artifact`: Evalúa un cambio (`code_diff`) contra una especificación (`spec_path`). Genera un reporte detallado con veredicto, tasa de éxito y el plan de re-trabajo (Rework Ticket) si algo falló.
*   `mem_traceability`: Construye y actualiza la matriz de trazabilidad RF/RNF → archivos de código.
*   `mem_trace_source` y `mem_lineage`: Permiten rastrear el origen de una lógica y cómo evolucionaron las modificaciones a lo largo del tiempo.

---

## 4. Markdown ADR Promotion (Nivel 2: Memoria Estructurada)

La DB local de SQLite y Postgres central es el **Nivel 1 (Memoria Operativa)**, sujeta a borrado o expiración. Las decisiones de arquitectura duraderas deben promoverse al **Nivel 2 (Memoria Estructurada)**, la cual vive directamente como archivos `.md` en el repositorio Git.

### Características Clave:
*   **Generación de ADRs canónicos**: Convierte observaciones dinámicas de la DB en archivos markdown limpios con frontmatter completo (ID, título, autor, fecha, tipo, etc.).
*   **Mantenimiento de Índices**: Genera o actualiza automáticamente un índice maestro en `docs/decisions/index.md` con enlaces cruzados.
*   **Verificación de Links**: Valida que los enlaces en la base de datos coincidan exactamente con la ruta física del archivo en el repositorio de manera bidireccional.

### Herramientas MCP de Promoción:
*   `mem_promote_to_md`: Promueve una observación seleccionada por ID a un archivo `.md` físico dentro de la ruta del proyecto.
*   `mem_sync_md_to_repo`: Sincroniza en batch todas las observaciones pendientes que califiquen para promoción, actualizando el índice del repositorio de forma automática.

---

## 5. Obsidian Vault Export (Visualización del Gráfico Neuronal)

Permite exportar todo el conocimiento estructurado y el historial a un vault local de Obsidian para que el desarrollador pueda navegar visualmente por las interconexiones de su cerebro de desarrollo.

### Características Clave:
*   **Exportación Incremental**: Rastrea en un archivo `.engram-sync-state.json` qué observaciones ya se exportaron para procesar únicamente los cambios nuevos.
*   **Session & Topic Hubs**: Crea notas concentradoras automatizadas para agrupar sesiones y clústeres de tópicos recurrentes.
*   **Wikilinks Automáticos**: Traduce las conexiones lógicas entre memorias a la sintaxis nativa de Obsidian (`[[nota]]`) para habilitar el gráfico interactivo de relaciones.

### Invocación:
Se realiza directamente mediante la CLI:
```bash
engram obsidian-export --vault /ruta/al/vault --project flowforge
```

---

## 6. Diagnostics & Janitor (Mantenimiento)

Mantiene la base de datos limpia y el entorno libre de fallos de configuración.

### Características Clave:
*   **Pruning de Retención (Janitor)**: Los datos operativos de depuración (como `tool_use`, `command`, `search`) tienen tiempos de vida (TTL) cortos configurables (ej: 30 a 90 días). Las decisiones y arquitecturas son permanentes.
*   **Healthchecks Paralelos**: Ejecuta diagnósticos asíncronos concurrentes sobre la DB, el servidor web y la salud de la configuración del servidor de MCP.

### Herramientas MCP de Diagnóstico:
*   `mem_doctor`: Corre diagnósticos completos del estado operativo.
*   `mem_retention_stats`: Muestra estadísticas de la cantidad de registros por tipo y su fecha de expiración.
*   `mem_retention_prune`: Ejecuta el pruning manual para liberar espacio en disco borrando registros expirados.
*   `mem_project_redirects`: Administra redirecciones y fusiones de proyectos que hayan cambiado de nombre o namespace.

---

## Flujo Recomendado para el Agente (Cómo sacarle provecho)

Para sacarle jugo a esto, el agente debe cambiar su mentalidad y seguir este ciclo en cada tarea:

```
[Inicio de Sesión]
  └── 1. mem_context: Ver qué se hizo recientemente en el proyecto.
  └── 2. mem_search: Buscar antecedentes o decisiones de diseño previas.

[Fase de Especificación (Spec Phase)]
  └── 3. Escribir un `spec.md` canónico con IDs claros (RF-001, RNF-001).

[Fase de Codificación (Apply Phase)]
  └── 4. Escribir código enfocado únicamente en la especificación.
  └── 5. Usar mem_suggest_topic_key + mem_save para registrar decisiones evolutivas.

[Fase de Verificación (Verify Phase)]
  └── 6. Correr mem_verify_artifact para auditar automáticamente el código.
  └── 7. Si hay fallos, resolver el Rework Ticket y re-iterar (máx 3 ciclos).
  └── 8. Correr mem_traceability para mapear los requisitos al código final.

[Fase de Cierre (Closure Phase)]
  └── 9. mem_promote_to_md: Promover las decisiones importantes a ADRs (.md) en el repo.
  └── 10. mem_session_summary: Escribir el resumen maestro de la sesión.
```

---

## Conclusión

El uso inteligente de `engram-dotnet` convierte a un agente de desarrollo común en un **Arquitecto Senior Sistemático**. En lugar de adivinar el código o perder el contexto entre compactions, el agente usa la memoria neuronal para auto-corregirse, auditar su calidad, versionar sus decisiones y documentar el repositorio de manera impecable. 

¡Es así de fácil, loco! Incorporando estas 25 herramientas en nuestro flujo diario, llevamos la calidad de desarrollo a una **locura cósmica**.
