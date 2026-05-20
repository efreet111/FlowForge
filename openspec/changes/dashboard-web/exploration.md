## Exploration: Dashboard Web de Observabilidad

### Current State
Actualmente, toda la observabilidad del ecosistema EngramFlow + `engram-dotnet` se realiza a través de la interfaz de línea de comandos (CLI) o consumiendo directamente los engramas formateados como archivos Markdown. No existe una interfaz gráfica (GUI) que permita a los desarrolladores y arquitectos ver visualmente la línea de tiempo de sus sesiones de programación, explorar las relaciones semánticas entre engramas, o visualizar el flujo de ejecución (DAG) de los agentes cuando el orquestador toma decisiones.

`engram-dotnet` ya expone un servidor HTTP local (`localhost:7437`) que sirve como backend de almacenamiento y sincronización, lo que nos provee de una API REST lista para ser consumida por un cliente web local.

### Affected Areas
Este cambio es **100% aditivo** y no modifica la lógica existente del CLI ni del servidor, pero sí se conectará a ellos:
- `dashboard/` — Nuevo directorio en el repositorio FlowForge que contendrá los archivos estáticos de la interfaz web.
- `docs/04-roadmap.md` — Requiere actualización para marcar la feature como "En Exploración/Diseño".

### Approaches

1. **Vanilla SPA (HTML/CSS/JS Estático y Moderno)**
   - **Descripción**: Una aplicación de una sola página (SPA) construida con HTML5 semántico, Javascript ES6 moderno y CSS Vanilla premium (Glassmorphism, gradientes fluidos, animaciones CSS). Se conecta vía REST a `http://localhost:7437` usando `fetch`.
   - **Pros**:
     - Cero pasos de compilación (sin `npm run build`, sin `node_modules` gigantes en el repo).
     - Rendimiento ultra rápido y carga instantánea.
     - Máxima flexibilidad y control estético con CSS Vanilla (perfecto para aplicar nuestra guía de diseño premium wow).
   - **Cons**: El manejo de estado complejo debe estructurarse limpiamente de forma nativa (por ejemplo, con Web Components simples o patrones de pub/sub nativos) para evitar el "spaghetti code".
   - **Effort**: Medium

2. **React/Vite SPA App**
   - **Descripción**: Un cliente web completo utilizando React, Tailwind y Vite como empaquetador.
   - **Pros**: Reutilización de componentes y facilidad de manejo de estados mediante hooks de React.
   - **Cons**: Introduce una sobrecarga de compilación enorme. Requiere un proceso de desarrollo pesado (`npm install`, `node_modules` de cientos de megabytes) que complica la simplicidad de la distribución de FlowForge.
   - **Effort**: High

### Recommendation
**Recomiendo rotundamente el Enfoque 1: Vanilla SPA con Estética Premium (Glassmorphism + CSS Vanilla).**
Para una herramienta de observabilidad local que corre junto a un servidor local, la velocidad de inicio y la falta de dependencias externas pesadas son críticas. Un diseño de interfaz premium wow utilizando CSS Vanilla moderno (gradientes lineales fluidos, sombras difusas, fondos desenfocados tipo "glass", fuentes estilizadas como *Inter* u *Outfit*) dará una experiencia de usuario espectacular y fluida, sin engordar el repositorio con herramientas de build complejas.

### Risks
- **Cross-Origin Resource Sharing (CORS)**: El servidor HTTP de `engram-dotnet` debe estar configurado para permitir peticiones CORS desde el puerto o dirección donde se sirva el Dashboard Web (se solucionará agregando cabeceras CORS en el backend o sirviendo el dashboard directamente desde el servidor local).
- **Consistencia de Datos**: Peticiones asíncronas fallidas si el servidor local de engramas está apagado (requiere una UX robusta que detecte y muestre con elegancia un estado "Offline / Reconectando").

### Ready for Proposal
**Yes.** El concepto está perfectamente definido y la recomendación técnica de usar Vanilla JS/CSS garantiza velocidad, estética premium y simplicidad metodológica. Estamos listos para armar la propuesta.
