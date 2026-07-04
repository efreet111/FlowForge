# Glosario FlowForge

> Referencia rápida de términos usados en FlowForge, FlowDoc y engram-dotnet.  
> 🇬🇧 [English Glossary](GLOSSARY.md)

---

## Cómo leer este glosario

Los términos están agrupados por el sistema que los define. Si eres nuevo, lee los grupos en orden: **FlowForge** primero (la metodología), luego **FlowDoc** (la capa documental), luego **engram** (el motor de memoria).

---

## FlowForge — Metodología y Flujo de Trabajo

| Término | Significado simple |
|---------|-------------------|
| **FlowForge** | La metodología en sí. Define cómo los agentes de IA colaboran con humanos a través de 5 fases y 5 checkpoints para entregar funcionalidades de software. Es como un manual de juego para el trabajo humano–agente. |
| **Fase** | Una de las 5 etapas que atraviesa una funcionalidad: Descubrimiento → Intención → Plan → Ejecución → Cierre. Cada fase tiene un agente dedicado y produce artefactos específicos. |
| **Checkpoint (CKP)** | Una compuerta formal entre fases. Algunos checkpoints son mecánicos (el agente se detiene automáticamente), otros requieren una decisión humana. Hay 5: CKP-0 a CKP-4. |
| **CKP-0 🔴** | Parada total en Descubrimiento. Si el requerimiento es demasiado vago, el agente se detiene y pide clarificación. Sin negociación. |
| **CKP-1 🟡** | El humano revisa `spec.md` y aprueba o ajusta antes de que se escriba el plan. |
| **CKP-2 🟡** | El humano revisa `plan.md` y da luz verde para escribir código. |
| **CKP-3 🔴** | Seguro mecánico: si la misma funcionalidad ha pasado por 3 ciclos de retrabajo, el agente escala a un humano en lugar de reintentar. |
| **CKP-4 🟢** | Compuerta de despliegue. El agente produce un resumen de sesión y el humano decide si desplegar. |
| **Skill (`SKILL.md`)** | Archivo de instrucciones reutilizable que define el comportamiento de un rol de agente. Los skills los carga el agente en tiempo de ejecución. FlowForge incluye 7 skills core y 24 especializados. |
| **forge-discovery** | El agente de la Fase 0. Busca en memoria y código existente antes de proponer algo nuevo. |
| **forge-arch** | El agente de la Fase 1. Escribe `spec.md` con escenarios Given-When-Then. Nunca escribe código. |
| **forge-plan** | El agente de la Fase 2. Escribe `plan.md`: un checklist técnico paso a paso. |
| **forge-dev** | El agente de la Fase 3. Escribe código de producción y pruebas unitarias basándose en `plan.md`. |
| **forge-verify** | El auditor. Revisa la implementación contra `spec.md` y genera un ticket de retrabajo si encuentra brechas. |
| **forge-memory** | El agente de la Fase 4. Guarda aprendizajes en el motor de memoria y produce el resumen de sesión. |
| **forge-orchestrator** | El director. Enruta comandos al agente correcto, hace cumplir los checkpoints y gestiona las transiciones de estado. |
| **`/flow-start`** | Comando IDE para comenzar un nuevo ciclo de funcionalidad (Fase 0). Crea `.ai-work/{slug}/` e inicia forge-discovery. |
| **`/flow-plan`** | Comando IDE para saltar a la Fase 2 (escritura del plan). |
| **`/flow-dev`** | Comando IDE para saltar a la Fase 3 (escritura de código). |
| **`/flow-verify`** | Comando IDE para auditar la implementación (forge-verify). |
| **`/flow-close`** | Comando IDE para ejecutar la Fase 4 (cierre de sesión, CKP-4). |
| **`/flow-rework`** | Comando IDE para procesar un reporte de bug o comentario de revisión como ciclo de retrabajo. |
| **`/flow-init`** | Script (no es un comando IDE) que arma el esqueleto de un nuevo proyecto: crea `docs/`, `AGENTS.md`, `.flowforge.json` e instala los paquetes IDE. |
| **`.ai-work/{slug}/`** | Espacio de trabajo efímero para una funcionalidad en curso. Solo existe durante el ciclo de la funcionalidad; se archiva después de `/flow-close`. No es para documentación permanente. |
| **slug** | Identificador corto en kebab-case para una funcionalidad (ej: `autenticacion-usuario`, `crud-tareas`). Se usa como nombre de carpeta dentro de `.ai-work/`. |
| **`spec.md`** | La especificación de funcionalidad producida por forge-arch. Contiene requerimientos funcionales, escenarios Given-When-Then y una Matriz de Capacidades. Vive en `.ai-work/{slug}/`. |
| **`plan.md`** | El plan técnico de implementación producido por forge-plan. Un checklist paso a paso. Vive en `.ai-work/{slug}/`. |
| **`verify-report.md`** | El reporte de auditoría producido por forge-verify. Lista brechas entre spec e implementación. Vive en `.ai-work/{slug}/`. |
| **`rework_ticket.md`** | Un ticket estructurado de bug/brecha que envía la funcionalidad de vuelta a forge-dev. Vive en `.ai-work/{slug}/`. Registra `cycle_count` (máximo 3 antes del CKP-3). |
| **`summary.md`** | El documento de cierre de sesión producido por forge-memory. Resume qué se hizo, decisiones tomadas y próximos pasos. Vive en `.ai-work/{slug}/`. |
| **`context-map.md`** | La salida del descubrimiento producida por forge-discovery. Lista memorias previas relevantes, épicas y patrones reutilizables encontrados en el código. Vive en `.ai-work/{slug}/`. |
| **PM-* (Pruebas Manuales)** | Pruebas manuales del desarrollador definidas en `spec.md`. Deben ser marcadas `[x]` por el desarrollador humano antes de que se permita `/flow-close`. No son automatizables — requieren juicio humano. |
| **Matriz de Capacidades** | Una tabla en `spec.md` que separa las decisiones delegadas a la IA (`ai_reasoning`) de las reglas de negocio duras que la IA no debe sobrepasar (`deterministic`). |
| **`AGENTS.md`** (proyecto) | Un archivo corto en la raíz de un proyecto que da a cualquier agente de IA una orientación rápida: stack, fuentes de verdad y puntos de entrada al flujo de trabajo. No es el mismo que el `AGENTS.md` de FlowForge. |
| **`.flowforge.json`** | Archivo de configuración en la raíz de un proyecto. Declara la versión de FlowForge, configuración de engram, versión del framework documental y rutas canónicas de `docs/`. |
| **`DEVELOPMENT.md`** | Documento vivo en `docs/` que contiene la configuración del proyecto, instrucciones de pruebas y convenciones de código. Generado por `flow-init`. |
| **ADR (metodología)** | Architecture Decision Record para decisiones de metodología FlowForge. Vive en `FlowForge/docs/decisions/`. No es igual a un ADR de producto. |
| **Ciclo de retrabajo** | Una iteración de corrección → verificación. Se cuenta en `rework_ticket.md` como `cycle_count`. Tres ciclos activan CKP-3. |
| **Orquestador** | Ver *forge-orchestrator*. También se refiere al archivo de paridad compartida (`workflow-orchestrator-parity.md`) que mantiene los 4 IDEs sincronizados. |

---

## FlowDoc — Framework de Documentación

| Término | Significado simple |
|---------|-------------------|
| **FlowDoc** | Un framework de documentación para equipos pequeños y asíncronos. Define cómo estructurar `docs/` con PRD, HUs, ADRs y RFCs en Markdown plano. FlowForge usa FlowDoc como su capa documental para proyectos. |
| **PRD (Product Requirements Document)** | Un documento único (`docs/PRD.md`) que describe qué es el producto, para quién es y qué problemas resuelve. La fuente de verdad principal para la dirección del producto. |
| **HU (Historia de Usuario)** | Descripción estructurada de una funcionalidad desde la perspectiva del usuario, siguiendo el formato *"Como [rol], quiero [acción], para que [beneficio]."* Vive en `docs/tasks/HU-NNN-*.md`. |
| **`docs/tasks/`** | Carpeta que contiene todas las Historias de Usuario. Cada archivo es una HU. Este es el backlog del producto en Markdown legible por humanos. |
| **`flowforge_slug`** | Campo en el frontmatter de la HU que la vincula con su carpeta activa `.ai-work/{slug}/`. Se asigna cuando se ejecuta `/flow-start` para esa HU. |
| **ADR (producto)** | Architecture Decision Record para decisiones a nivel de producto (ej: "usamos PostgreSQL en lugar de SQLite"). Vive en `docs/architecture/adr/` del proyecto. Diferente de los ADRs de metodología FlowForge. |
| **RFC (Request for Comments)** | Documento de propuesta para cambios técnicos significativos que necesita discusión del equipo antes de tomar una decisión. Vive en `docs/architecture/rfc/`. |
| **`flowdoc-ciclo.md`** | Documento que describe el ritmo de sprint de 15 días del equipo (planificación, revisión, retro). Opcional — relevante desde el nivel de adopción L3 en adelante. |
| **Nivel de adopción (L1–L4)** | Describe qué tan profundamente un equipo ha adoptado FlowDoc. L1 = solo estructura `docs/`; L2 = flujo de funcionalidades activo; L3 = ritmo de equipo completo; L4 = métricas y retrospectivas formales. |

---

## engram / Motor de Memoria

| Término | Significado simple |
|---------|-------------------|
| **engram** | Abreviatura de `engram-dotnet`. El motor de memoria persistente que almacena observaciones de agentes entre sesiones. Piénsalo como una base de conocimiento estructurada que los agentes pueden consultar y escribir. |
| **engram-dotnet** | La implementación .NET 10 del motor de memoria engram. Expone 25 herramientas MCP para guardar, buscar y gestionar observaciones. |
| **MCP (Model Context Protocol)** | Protocolo que permite a los agentes de IA llamar herramientas externas (como el motor engram) mediante llamadas a funciones estructuradas. El IDE conecta el agente con los servidores MCP. |
| **Observación** | La unidad atómica de memoria en engram. Una nota estructurada con campos: `What` (qué), `Why` (por qué), `Where` (dónde), `Learned` (aprendido). Se guarda con `mem_save`. |
| **`topic_key`** | Identificador estable que agrupa observaciones relacionadas bajo un tema nombrado (ej: `architecture/auth-model`). Evita entradas duplicadas. |
| **`scope`** | Si una observación pertenece a todo el equipo (`team`) o solo a una persona (`personal`). Afecta la visibilidad y retención. |
| **`mem_save`** | Llamada a herramienta MCP que guarda una nueva observación en la base de datos engram. |
| **`mem_search`** | Llamada a herramienta MCP que busca observaciones por palabra clave y proyecto. |
| **`mem_session_summary`** | Llamada a herramienta MCP que guarda un registro estructurado de cierre de sesión. Requerida por forge-memory en cada `/flow-close`. |
| **`mem_promote_to_md`** | Llamada a herramienta MCP que renderiza una observación almacenada como archivo Markdown ADR en `docs/decisions/`. |
| **`local_memory`** | Modo de respaldo cuando la base de datos engram no está disponible. Las observaciones se escriben como archivos `.md` en `.engram/local_memory/`. Se sincronizan con la base de datos en el próximo cierre de sesión. |
| **Smart Curation** | El proceso que usa forge-memory para filtrar los archivos de `local_memory`: descartar ruido, fusionar duplicados y guardar solo observaciones de alto valor. |
| **TTL (Time-to-Live)** | Tiempo de expiración configurable para observaciones temporales (ej: registros de comandos, notas de cambios de archivos). `mem_retention_prune` elimina los elementos expirados. |

---

## IDE / Herramientas

| Término | Significado simple |
|---------|-------------------|
| **Cursor** | IDE nativo de IA (fork de VS Code). FlowForge instala reglas, agentes y comandos en `~/.cursor/`. |
| **OpenCode** | Agente de codificación IA basado en terminal. FlowForge instala un bundle en `~/.config/opencode/`. |
| **VS Code / Copilot** | VS Code estándar con GitHub Copilot. FlowForge instala archivos de agente en `~/.vscode/` y `.github/agents/`. |
| **Antigravity** | Herramienta de codificación agéntica basada en Google Gemini. FlowForge instala paquetes en `~/.gemini/antigravity/` (AGENTS.md + rules/ + workflows/) y, por proyecto, en `.agents/rules/`, `.agents/workflows/` y `AGENTS.md`. No es Claude Desktop. |
| **Paridad de IDEs** | La garantía de que el mismo flujo de trabajo FlowForge (CKP-0 → CKP-4) funciona de forma idéntica en los 4 IDEs soportados. Se mantiene a través de `workflow-orchestrator-parity.md`. |
| **`workflow-orchestrator-parity.md`** | La especificación de orquestación compartida única usada por los 4 IDEs. Cualquier cambio en la lógica del flujo de trabajo se hace aquí primero, luego se refleja en la configuración de cada IDE. |
| **`compile-agents-from-skills.py`** | Script que regenera los archivos de agente de Cursor desde las fuentes canónicas `skills/forge-*/SKILL.md`. Se ejecuta automáticamente por el instalador. |

---

## General / Transversal

| Término | Significado simple |
|---------|-------------------|
| **SDLC** | Software Development Life Cycle (Ciclo de Vida del Desarrollo de Software). El proceso completo desde la idea hasta el software desplegado. FlowForge es un "SDLC Agéntico" — aplica agentes de IA a cada fase. |
| **Artefacto efímero** | Archivo que existe solo durante un ciclo de funcionalidad (ej: `spec.md`, `plan.md`). Se archiva después del cierre; no es documentación permanente. |
| **Artefacto persistente** | Archivo que sobrevive más allá del ciclo de funcionalidad (ej: `docs/PRD.md`, ADRs, `DEVELOPMENT.md`). Estos viven en `docs/` y son la verdad a largo plazo del producto. |
| **SOLID** | Cinco principios de diseño orientado a objetos (Responsabilidad Única, Abierto/Cerrado, Sustitución de Liskov, Segregación de Interfaces, Inversión de Dependencias). FlowForge tiene un skill dedicado (`forge-dev-solid`) que valida el código generado contra estos principios. |
| **OWASP** | Open Worldwide Application Security Project. Estándar para la seguridad de aplicaciones. Los skills de seguridad de FlowForge referencian OWASP ASVS para verificaciones de vulnerabilidades. |
| **Feature flag** | Toggle de configuración que habilita o deshabilita una funcionalidad en tiempo de ejecución sin un despliegue de código. Relevante en el nivel de adopción FlowDoc L3+. |
| **Greenfield** | Comenzar un componente desde cero sin código existente para reutilizar. El paso de Búsqueda de Patrones de forge-discovery existe específicamente para evitar diseños greenfield innecesarios. |
| **Spike** | Una investigación corta con tiempo fijo (típicamente 1–4 horas) para responder una pregunta técnica específica antes de comprometerse con un enfoque de implementación. Los spikes corren fuera del ciclo completo de FlowForge (sin `spec.md`, sin `plan.md`). |

---

## Mapa rápido: ¿dónde vive cada tipo de contenido?

| Tipo de contenido | Ubicación | ¿Sobrevive al merge? |
|------------------|----------|----------------------|
| Spec, plan, verify report de la funcionalidad | `.ai-work/{slug}/` | No — efímero |
| Backlog del producto (Historias de Usuario) | `docs/tasks/HU-*.md` | Sí |
| Requerimientos del producto | `docs/PRD.md` | Sí |
| Decisiones de arquitectura del producto | `docs/architecture/adr/` | Sí |
| Convenciones de código, configuración | `docs/DEVELOPMENT.md` | Sí |
| Decisiones de metodología FlowForge | `FlowForge/docs/decisions/` | Sí (en este repo) |
| Memoria de agentes / aprendizajes | engram DB o `.engram/local_memory/` | Sí |
| Reglas de flujo de trabajo IDE | `.cursor/`, `.agents/`, `.vscode/` | Sí (instalados) |

---

*Ver [`docs/20-flowdoc-ecosystem.md`](docs/20-flowdoc-ecosystem.md) para la guía completa de cómo trabajan juntos FlowForge y FlowDoc.*  
*Ver [`QUICKSTART.es.md`](QUICKSTART.es.md) para comenzar en 5 minutos.*
