# Integración IDE y Lanzamiento Open Source — Guía Definitiva

> **Estado**: Listo para Publicar
> **Ámbito**: FlowForge (Orquestación e IDEs)
> **Autor**: Antigravity + Gentleman Architect

Para que **FlowForge** sea una metodología de código abierto atractiva, funcional y que capte la atención de la comunidad (GitHub Stars, Patreons, reclutadores), no puede quedarse solo en "teoría en archivos Markdown". Necesita integraciones técnicas directas con los editores de IA más potentes del mercado y una estrategia de automatización de instalación.

Esta guía detalla la integración técnica con **Cursor**, **Cline (Roo-Cline)**, el script automatizado de instalación de Skills, y el modelo de adopción para la comunidad.

---

## 1. Integración con Cursor (`.cursorrules` Maestro)

**Cursor** es actualmente el editor líder para desarrollo con IA por su modo "Composer/Agent". Para forzar a Cursor a respetar las 4 fases de EngramFlow y evitar que "freelancee" código en tus archivos, debés colocar un archivo `.cursorrules` en la raíz del proyecto.

### El Archivo `.cursorrules` para EngramFlow
```markdown
# SOP de Desarrollo EngramFlow — INSTRUCCIÓN OBLIGATORIA PARA COMPOSER/AGENT

Actúas bajo la metodología EngramFlow. Tu comportamiento está rígidamente acotado según la fase de desarrollo activa.

## 1. Detección de Estado
- Antes de tomar cualquier acción, leé el archivo `.engram.json` en la raíz para detectar si hay un "active_change" (ej: `001-prioridades-tareas`).
- Si existe un cambio activo, tu carpeta de aislamiento para artefactos es `openspec/changes/<active_change>/`.

## 2. Restricciones de Fase por Rol
No tienes permitido mezclar tareas. Debes operar en uno de los siguientes roles según el comando del usuario:

### A. FASE 1: INTENCIÓN (Rol: Arch Agent)
- **Activación**: Cuando el usuario pide diseñar una feature o inicia un cambio.
- **Acción**: Crea `openspec/changes/<active_change>/spec.md` incluyendo la Capability Matrix y escenarios Given-When-Then.
- **REGLA DURA**: Queda TERMINANTEMENTE PROHIBIDO escribir, modificar o proponer código de producción o tests. Tu salida es 100% documental.

### B. FASE 2: ARQUITECTURA (Rol: Plan Agent)
- **Activación**: Cuando el usuario aprueba el spec.md y pide el plan de implementación.
- **Acción**: Diseña `openspec/changes/<active_change>/plan.md` con análisis de dependencias, "Proposed Changes" (lista de archivos a tocar) y checklist topológico.
- **REGLA DURA**: Queda TERMINANTEMENTE PROHIBIDO codificar. Tu misión es dejar las firmas de métodos y DTOs tan detallados que codificar sea un acto mecánico.

### C. FASE 3: EJECUCIÓN (Rol: Dev Agent)
- **Activación**: Cuando el plan.md está aprobado y se te ordena implementar.
- **Acción**: Escribe el código de producción y los tests automatizados mapeando 1 a 1 los escenarios del spec.md.
- **Inner Loop (Ralph Wiggum)**: Corre los tests en la terminal (`dotnet test`, `pytest`, `npm test`) de forma autónoma. Corrige errores sintácticos y de compilación en caliente hasta que estén en verde.
- **REGLA DURA**: Queda prohibido tocar archivos no declarados en el "Proposed Changes" del plan (No-Touch Rule).

### D. FASE 4: JUICIO (Rol: Verify Agent / Sentinel Judge)
- **Activación**: Cuando el desarrollo finaliza y se pide auditoría.
- **Acción**: Revisa línea por línea el código del Dev Agent en busca de prints de depuración, retornos faltantes o constantes erróneas respecto al spec.md.
- **REGLA DURA**: Si hay el más mínimo fallo, NO intentes arreglar el código. Generá un `rework_ticket.md` y rechazá la entrega con un veredicto "REJECTED".
```

---

## 2. Integración con Cline / Roo-Cline (`.clinerules`)

**Cline** (anteriormente Claude Dev) y **Roo-Cline** son extensiones espectaculares basadas en agentes autónomos con uso intensivo de terminal. 

Para integrarlo:
1. Creamos un archivo `.clinerules` en el proyecto del desarrollador con el mismo contenido estructural que el `.cursorrules`.
2. Como Cline tiene acceso directo a ejecutar comandos CLI sin preguntar, el desarrollador le puede decir directamente:
   > *"Cline, leé el checklist en `openspec/changes/001-prioridades-tareas/tasks.md` y ejecutá las tareas de la Fase 1 y 2 aplicando el Ralph Wiggum Loop autónomamente."*
3. Cline se encargará de ejecutar el linter y los tests de forma nativa en la terminal, auto-corrigiéndose si hay errores sintácticos.

---

## 3. Instalador de Skills Automatizado (`install-skills.sh`)

Para que el desarrollador no tenga que copiar manualmente los archivos `SKILL.md` en su máquina, crearemos un script de instalación en la raíz de FlowForge: `install-skills.sh`.

Este script automatiza el bootstrapping:
```bash
#!/bin/bash
# install-skills.sh - FlowForge Skills Bootstrapper
# Autor: Antigravity + Gentleman Architect

echo "🚀 Iniciando instalación de Skills de EngramFlow..."

# Directorios de destino según el IDE del usuario
COPILOT_SKILLS_DIR="$HOME/.copilot/skills"
LOCAL_AGENT_SKILLS_DIR="./.agents/skills"

# 1. Crear directorios si no existen
mkdir -p "$COPILOT_SKILLS_DIR"
mkdir -p "$LOCAL_AGENT_SKILLS_DIR"

# 2. Sincronizar Skills Core a la carpeta global de VS Code Copilot
echo "📦 Copiando Skills maestros a VS Code Copilot..."
cp -r ./skills/* "$COPILOT_SKILLS_DIR/"

# 3. Sincronizar en la carpeta local del proyecto activo (para CLI y Orquestador)
echo "📦 Sincronizando Skills locales para el proyecto activo..."
cp -r ./skills/* "$LOCAL_AGENT_SKILLS_DIR/"

echo "✅ ¡Instalación completada con éxito! Las skills @forge-arch, @forge-plan, @forge-dev, @forge-verify y @forge-memory ya están activas en tu IDE."
```

---

## 4. Caso de Uso Real: Un Día de Desarrollo con EngramFlow

Para documentar en el repositorio Git y que la comunidad entienda el poder metodológico de FlowForge, incluiremos este **diario de desarrollo** simplificado:

### 📝 Bitácora de un Cambio: Implementando Autenticación JWT
1. **Paso 1**: El desarrollador arranca con el comando `/sdd-new jwt-auth`.
2. **Paso 2**: El **Arch Agent** entra en juego. Analiza el código actual, busca en la memoria neuronal de Engram si hay decisiones sobre seguridad. Encuentra que en 2025 se decidió usar "Cookies HttpOnly con seguridad estricta". Escribe `spec.md` bajo `openspec/changes/001-jwt-auth/` respetando esa decisión. **El Checkpoint ① se aprueba**.
3. **Paso 3**: El **Plan Agent** toma el spec. Escribe el plano de arquitectura en `plan.md`. Define que se creará una clase `JwtMiddleware.cs` y se modificará `Program.cs`. Detalla la firma exacta de verificación del token. **El Checkpoint ② se aprueba**.
4. **Paso 4**: El **Dev Agent** recibe el plano. Empieza a codificar. Escribe `JwtMiddleware.cs`. En el primer intento, olvida importar el namespace de criptografía. Intenta compilar y la consola escupe un error. El **Ralph Wiggum Loop** se activa: lee el error, inyecta el `using System.Security.Cryptography;`, vuelve a compilar... ¡Y pasa en verde!
5. **Paso 5**: El **Verify Agent** audita. Ve que el código funciona y compila, pero detecta que el Dev Agent dejó un `Console.WriteLine("DEBUG TOKEN: " + token)` en el middleware para probar. **RECHAZA** la entrega por contener código basura e inyecta un `rework_ticket.md` con `cycle_count: 1`.
6. **Paso 6**: El Dev Agent lee el ticket de rework, remueve el print sucio y vuelve a entregar. El Verify Agent da el **PASS**.
7. **Paso 7**: El **Memory Agent** extrae el aprendizaje: *"Al implementar middlewares de autenticación, es crítico asegurar que las credenciales no se filtren en logs de consola estándar"*, y lo persiste en la memoria neuronal. **Checkpoint ③ cerrado y mergeado a main**.

---

## 5. El Orquestador: ¿Cómo se define, instala e invoca?

Uno de los puntos más críticos de la metodología es la **orquestación**: quién se encarga de llamar en secuencia a las 5 Skills (`Arch -> Plan -> Dev -> Verify -> Memory`) y cómo se integra esto en tu día a día, especialmente en chats monolíticos o con agentes.

Existen dos paradigmas de orquestación en FlowForge según el cliente que estés usando:

### Paradigma A: Orquestación Guiada por el Humano (VS Code Copilot Chat, Antigravity Chat)
En entornos de chat estándar donde el modelo no puede abrir sub-agentes de forma automática o ejecutar comandos en la terminal por sí solo, **el desarrollador actúa como el director de orquesta (Human-in-the-Loop)**. 
- **Cómo se define**: No requiere instalar ningún software complejo. La orquestación es **documental y dirigida por el estado**.
- **Cómo funciona**:
  1. Le decís al chat: `@forge-arch Quiero crear X feature`. El agente crea el `spec.md` en la carpeta del cambio y se detiene.
  2. Revisás el spec. Si te gusta, le decís al chat: `@forge-plan Generá el plan para este spec`. El planificador escribe `plan.md` y se detiene.
  3. Ejecutás la codificación en tu IDE de manera tradicional o asistido por IA.
  4. Cuando terminás, invocás al revisor: `@forge-verify Auditá el código`. Él te devuelve un "PASS" o te crea el `rework_ticket.md`.
  5. Una vez en verde, invocás: `@forge-memory Cerrá la sesión y guardá el aprendizaje`.
- **Por qué es brillante**: Garantiza que la metodología funcione a la perfección en **CUALQUIER editor de texto** (incluso Notepad++ o Vim) con cualquier chat de IA estándar, ya que el estado no vive en la memoria de la máquina, sino físicamente en los archivos `spec.md`, `plan.md` y `.engram.json`.

### Paradigma B: Orquestación Autónoma (Cursor Composer, Cline, Roo-Cline, Sub-agentes de Antigravity)
En clientes modernos que tienen acceso a la terminal y capacidades de sub-agentes autónomos, el proceso es 100% automático.
- **Cómo se instala**: A través de las reglas del workspace (`.cursorrules` o `.clinerules`) que se copian automáticamente al correr `./install-skills.sh`.
- **Cómo se invoca**: 
  - Simplemente abrís el agente y le das una sola orden inicial: 
    > *"FlowForge, iniciá el cambio '001-prioridades-tareas' para implementar prioridades."*
  - El orquestador del IDE leerá tu orden, detectará que está en Fase 1, aplicará la skill `@forge-arch` para hacer el spec, luego enlazará a `@forge-plan` para el plan, creará el código, correrá los tests en la terminal (`pytest`, `dotnet test`, etc.) usando el **Ralph Wiggum Loop** en caliente para corregirse solo, y finalmente llamará al **Sentinel Judge** `@forge-verify` para auto-auditarse antes de entregarte el resultado final en verde.
- **Cómo se controla**: Todo el progreso se escribe y lee desde el archivo de estado `.engram.json` en la raíz del proyecto, asegurando trazabilidad total en cada micro-paso.

---

## 6. Estrategia de Lanzamiento en GitHub (Open Source)

Para que el repositorio "explote" en visibilidad y sea adoptado por la comunidad:

1. **La marca "Concepts > Code"**:
   Debemos vender FlowForge en el README como la cura definitiva para el "ruido agentic". Los programadores están cansados de que las IAs generen código roto y gasten miles de tokens dando vueltas en círculos. FlowForge ofrece **prolijidad quirúrgica** y control de calidad inquebrantable.
2. **El "Showcase" del Dashboard**:
   El Dashboard Web (Vanilla SPA) será la portada visual de nuestro GitHub. Incluir capturas de pantalla del mapa neuronal interactivo (nodos cian de memoria, enlaces rojos de conflictos) hará que el proyecto luce espectacularmente profesional.
3. **Invitación a Contribuir**:
   Crear una guía de contribuciones (`CONTRIBUTING.md`) invitando a desarrolladores a subir sus propias **Language Skills** (ej: plantillas de testing específicas para Rust, TypeScript, Go, C#) a la base de datos de FlowForge, enriqueciendo el ecosistema de forma colaborativa y orgánica.
