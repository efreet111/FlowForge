# Tasks: Dashboard Web de Observabilidad

## Phase 1: Foundation (Directorio y Diseño Estético)

- [ ] 1.1 Crear el directorio raíz `dashboard/` en la raíz del repositorio de FlowForge.
- [ ] 1.2 Crear el archivo `dashboard/index.html` conteniendo la estructura semántica base de la SPA e importando las fuentes *Outfit* e *Inter* de Google Fonts.
- [ ] 1.3 Crear `dashboard/styles.css` y declarar las CSS Custom Variables para nuestro diseño oscuro y premium:
  * `--bg-main: #0b0f19;`
  * `--glass-bg: rgba(11, 15, 25, 0.65);`
  * `--glass-border: rgba(255, 255, 255, 0.08);`
  * `--accent-cyan: #00f0ff;`
  * `--accent-purple: #8b5cf6;`
  * `--accent-red: #ff4646;`
  * `--text-main: #f3f4f6;`
- [ ] 1.4 Escribir en `styles.css` las clases de utilidad para el efecto Glassmorphism (`.glass-panel`) aplicando `backdrop-filter: blur(12px)` y sombras Glowing Accents (`.glow-cyan`).

---

## Phase 2: Core Javascript y Consumo REST (API)

- [ ] 2.1 Crear `dashboard/js/api.js` conteniendo los servicios asíncronos nativos (`fetch`) para pegarle a `http://localhost:7437/api/observations` y `/api/sessions`.
- [ ] 2.2 Crear `dashboard/js/app.js` (entry point `type="module"`) para coordinar el ciclo de vida del Dashboard.
- [ ] 2.3 Implementar en `app.js` el motor de monitoreo de conexión (Heartbeat / Reconnect Loop) que lance una consulta liviana cada 5000ms. Si falla, activa el difuminado y la vista "Offline".

---

## Phase 3: Módulos Visuales (Timeline y Grafo)

- [ ] 3.1 Crear `dashboard/js/timeline.js` para iterar y renderizar la línea de tiempo vertical de sesiones en el panel izquierdo. Agregar la lógica de expansión interactiva para ver los detalles del engrama.
- [ ] 3.2 Crear `dashboard/js/graph.js` para pintar el grafo semántico usando pure SVG y fuerzas de atracción/repulsión básicas de física por Javascript, o integrando un CDN ultraliviano.
- [ ] 3.3 Programar en `graph.js` la detección del clic sobre nodos para disparar la animación de despliegue de la "Glass Sidebar" y poblar sus campos de información (*What/Why/Where/Learned*).

---

## Phase 4: Ensamble y Validación Local

- [ ] 4.1 Unificar en `index.html` la inyección de los componentes de Javascript y el CSS.
- [ ] 4.2 Probar de forma local levantando un servidor web estático simple (ej: `python3 -m http.server 8080` dentro de `dashboard/`) para verificar que el layout renderiza en menos de 200ms con una estética premium espectacular.
- [ ] 4.3 Verificar el comportamiento responsivo en resoluciones de pantalla comunes (escritorio FHD y laptop).
