# Proposal: Dashboard Web de Observabilidad

> **ID del Cambio**: `dashboard-web`
> **Estado**: En Propuesta
> **Autor**: Antigravity + Gentleman Architect
> **Fecha**: 2026-05-19

---

## 1. Intención y Justificación (Goal)

### El Problema
Actualmente, el ecosistema **EngramFlow** es puramente textual. Aunque la memoria de dos niveles y el ruteo autónomo de sub-agentes funcionan de manera espectacular, la observabilidad depende enteramente de comandos CLI o de la inspección manual de archivos Markdown. 

Esto genera fricción cognitiva:
1. No hay una forma visual y rápida de entender cómo se interconectan semánticamente los engramas de memoria (relaciones de tipo `conflicts_with`, `supersedes` o `related`).
2. No se puede ver la línea de tiempo histórica del crecimiento de conocimiento del proyecto.
3. El flujo de ejecución de los agentes (DAG) y los ciclos de rework (`Verify Agent -> Dev Agent`) se sienten como una "caja negra" para el desarrollador.

### La Solución
Construir un **Dashboard Web de Observabilidad** integrado en **FlowForge**. Será un cliente web SPA local, estático y de **cero dependencias (Vanilla SPA)**, que consumirá las API REST del puerto local del motor de engramas (`http://localhost:7437`). 

---

## 2. Decisiones de Diseño Clave (Architecture Decisions)

### 2.1 Estética Ultra-Premium (Visual Wow)
Siguiendo nuestra directiva de diseño premium de vanguardia, el Dashboard no será una interfaz administrativa gris y aburrida. Se diseñará con una estética **Cyberpunk-Sleek / Dark Glassmorphic**:
* **Paleta de Colores**: Fondo ultra oscuro (`#0b0f19`), acentos en cian brillante (glow de neón para estados activos) y violeta eléctrico para flujos de agentes, y grises HSL pulidos para tarjetas.
* **Glassmorphism**: Uso intensivo de efectos de desenfoque de fondo (`backdrop-filter: blur(12px)`) con bordes semi-transparentes muy finos (`border: 1px solid rgba(255,255,255,0.08)`) para simular paneles de vidrio flotantes.
* **Tipografía**: Importación nativa de Google Fonts usando *Outfit* para títulos futuristas y limpios, e *Inter* para legibilidad en bloques de texto técnico.
* **Micro-animaciones**: Transiciones suaves (`transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1)`) en estados hover, y pulsos de glow asíncronos para marcar el estado en línea de los agentes.

### 2.2 Cero Pasos de Compilación (Vanilla Tech Stack)
* **Core**: HTML5 semántico y CSS Vanilla puro.
* **Lógica**: JavaScript moderno (ES6+) estructurado en módulos nativos (`type="module"`), lo que permite separar responsabilidades en componentes lógicos reutilizables sin necesidad de empaquetadores como Webpack o Vite.
* **Visualización de Grafos**: Renderizado del grafo de engramas interactivo utilizando **pure SVG con Javascript** o un motor Canvas ligero y embebido (como d3-force o vis-network consumido directamente vía CDN/archivo local), garantizando descarga instantánea y cero bloat de dependencias en `node_modules`.

### 2.3 Jerarquía de Vistas (User Interface Modules)

1. **Dashboard General (Overview)**:
   * Tarjetas con métricas globales: Cantidad de engramas totales, porcentaje de tests en verde, ciclo de rework promedio de la semana, y estado delJanitor.
   * Consola de ejecución en vivo: Log interactivo que muestra las acciones activas del CLI.

2. **Línea de Tiempo del Conocimiento (Session Timeline)**:
   * Un timeline vertical fluido que representa las sesiones históricas cerradas por el `Memory Agent`.
   * Tarjetas interactivas que se expanden para mostrar el *What/Why/Where/Learned* de cada engrama guardado.

3. **Mapa Neuronal Interactivo (Semantic Memory Graph)**:
   * Vista interactiva tipo red (Force-Directed Graph) donde cada engrama es un nodo y sus relaciones son aristas de colores:
     * **Cian**: Relación compatible/related.
     * **Rojo neón**: Relación de conflicto (`conflicts_with`).
     * **Amarillo/Naranja**: Supersedes (engramas obsoletos que fueron reemplazados).
   * Al hacer clic en un nodo, se abre un panel lateral de vidrio ("Glass Sidebar") con el contenido del engrama.

4. **Visualizador de DAG de Agentes (Live Orchestration View)**:
   * Representación gráfica del flujo activo de agentes: `Arch -> Plan -> Dev -> Verify -> Memory`.
   * Un indicador glowing muestra cuál agente tiene la batuta de ejecución en ese milisegundo y el conteo de ciclos de rework en vivo.

---

## 3. Alcance y Límites (Scope & Boundaries)

### Queda DENTRO del Alcance:
* Interfaz web responsiva con soporte principal para monitores de escritorio (entorno de desarrollo).
* Conectividad asíncrona robusta con `engram-dotnet` a través de Fetch API.
* Estado offline elegante (pantalla de vidrio oscuro con desenfoque de neón que dice "Reconectando con el motor Engram local...").
* Visualización interactiva del grafo de conocimiento.

### Queda FUERA del Alcance:
* Autenticación de usuarios (es una herramienta de desarrollo puramente local que corre en localhost).
* Modificación directa de engramas desde la web en esta versión (el Dashboard es de solo lectura y observabilidad; las escrituras se reservan para los agentes y el CLI).

---

## 4. Preguntas Abiertas para el Humano

* ¿Te gustaría que el Dashboard se sirva directamente desde el propio ejecutable de C# de `engram-dotnet` (ej: exponiendo una ruta `/dashboard`), o preferís que sea un comando de Node/Python en FlowForge que levante un servidor estático ultraliviano? (Mi sugerencia es que en el futuro sea servido por `engram-dotnet` para ser single-binary, pero para desarrollo estático en FlowForge es perfecto levantarlo con cualquier live-server de VS Code).
