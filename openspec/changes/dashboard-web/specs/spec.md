---
capability_matrix:
  ai_reasoning:
    - UX transitions and micro-animations spacing details.
    - Graph auto-layout stabilization physics parameters.
    - Responsive card layout grid collapsing thresholds.
  deterministic:
    - API endpoint mapping strictly points to `http://localhost:7437/api/observations` and `/api/sessions`.
    - Relationship rendering colors: Conflicts = `#ff4646`, Related = `#00f0ff`, Supersedes = `#ffaa00`.
    - Auto-reconnect polling interval set to exactly 5000ms when server connection is lost.
---

# Spec: Dashboard Web de Observabilidad

## 1. Objetivo y Alcance
El Dashboard Web de Observabilidad de FlowForge es una interfaz gráfica (SPA) estática de cero dependencias externas diseñada para visualizar las sesiones de desarrollo, el grafo de engramas semánticos de memoria y el estado del DAG de orquestación en tiempo real. 

---

## 2. Requerimientos Funcionales (RF)

### RF-001: Visualización del Grafo Semántico de Engramas
La aplicación debe pintar dinámicamente un grafo interactivo de nodos y enlaces donde cada nodo representa un engrama guardado en la memoria neuronal del proyecto.

* **Escenario A: Carga y Renderizado Exitoso**
  * **Given** que el servidor de engramas local está levantado en `http://localhost:7437` con 3 engramas interconectados,
  * **When** el usuario abre el Dashboard Web de FlowForge en su navegador,
  * **Then** la aplicación debe realizar una petición `fetch` asíncrona a `/api/observations`, obtener los datos semánticos, y pintar un grafo de 3 nodos y sus respectivas aristas coloreadas según el tipo de relación (Cian para `related`, Rojo para `conflicts_with`).

* **Escenario B: Selección de Nodo y Panel Lateral (Glass Sidebar)**
  * **Given** que el grafo se renderizó correctamente con los 3 nodos,
  * **When** el usuario hace clic izquierdo sobre el nodo del engrama titulado "FTS5 Escape Bugfix",
  * **Then** se debe desplegar suavemente desde el lateral derecho un panel flotante de vidrio ("Glass Sidebar") mostrando el contenido formateado del engrama (*What/Why/Where/Learned*) sin recargar la página.

---

### RF-002: Manejo de Estado de Conexión Offline / Online
El dashboard debe ser resiliente ante la desconexión del servidor local de engramas (por ejemplo, si el desarrollador apaga el servidor HTTP de `engram-dotnet`).

* **Escenario A: Pérdida de Conexión en Caliente**
  * **Given** que el Dashboard está abierto y conectado con éxito en tiempo real,
  * **When** el servidor local de engramas se detiene o pierde conexión,
  * **Then** la aplicación debe capturar el fallo de red, difuminar la interfaz con un efecto de desenfoque de fondo (`backdrop-filter: blur(15px)`), desactivar los controles interactivos, y mostrar un modal flotante con un pulso rojo brillante y el mensaje: "Conexión Perdida: Reconectando en 5s...".

* **Escenario B: Reconexión Autónoma**
  * **Given** que el Dashboard está en estado "Offline" y realizando intentos de reconexión cada 5000ms,
  * **When** el desarrollador vuelve a encender el backend de engramas local y la petición de sondeo responde con `HTTP 200`,
  * **Then** el dashboard debe desvanecer el modal de offline, retirar el difuminado, actualizar todos los componentes de datos en caliente y volver a habilitar la interactividad completa de la interfaz de usuario.

---

## 3. Requerimientos No Funcionales (RNF)

### RNF-001: Rendimiento y Cero-Compilación (Vanilla Single Page App)
* **RNF-001.1**: La aplicación web completa debe ser un conjunto de archivos estáticos (`index.html`, `main.css`, `app.js`) servibles por cualquier servidor estático web sin pasos de empaquetado ni transpilación (`webpack`, `vite`, `tsc`).
* **RNF-001.2**: El tiempo de carga inicial de la SPA (FCP - First Contentful Paint) debe ser inferior a **200ms** en localhost.

### RNF-002: Estética Visual de Vanguardia (Premium Cyberpunk Glassmorphism)
* **RNF-002.1**: La tipografía de la interfaz debe cargarse desde Google Fonts utilizando *Outfit* para títulos principales y *Inter* para fuentes secundarias.
* **RNF-002.2**: Todos los paneles contenedores deben lucir un efecto de vidrio esmerilado translúcido usando `background: rgba(11, 15, 25, 0.65)` con `backdrop-filter: blur(12px)` y bordes sutiles de `border: 1px solid rgba(255, 255, 255, 0.08)`.
* **RNF-002.3**: Los estados activos o Glowing Accents deben utilizar sombras de caja de neón difusas (`box-shadow: 0 0 20px rgba(0, 240, 255, 0.35)`).
