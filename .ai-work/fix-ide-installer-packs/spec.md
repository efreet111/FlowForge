---
capability_matrix:
  ai_reasoning:
    - UX decision: mostrar advertencia vs error cuando no se detecta IDE objetivo (instalación best-effort)
    - UX decision: comportamiento del instalador cuando VS Code tiene ambas extensiones (Kilo + Copilot) instaladas
    - UX decision: formato del informe de `flowforge doctor` para diagnóstico multi-IDE
    - UX decision: cómo documentar dualidad de layouts Antigravity (global ~/.gemini/ vs proyecto .agents/)
  deterministic:
    - Las rutas de instalación son inmutables por plataforma (Cursor: ~/.cursor/, OpenCode: ~/.config/opencode/, Copilot: ~/.copilot/, Kilo: ~/.config/kilo/, Antigravity: ~/.gemini/antigravity/)
    - Antigravity (Google IDE) usa ~/.gemini/ — NUNCA Claude Desktop
    - Claude Desktop usa ~/.config/Claude/ (Linux) — MCP únicamente, sin agentes ni rules
    - La detección de extensiones VS Code debe verificar el directorio ~/.vscode/extensions/ por prefijos específicos
    - El instalador nunca debe sobrescribir archivos sin respaldo previo (patrón backup-then-write)
    - Los agentes de OpenCode son fuente de verdad para Kilo Code (comparten formato)
---

# Spec: Fix IDE Installer Packs

## 1. Objective and scope

### Objective
Corregir el instalador multi-IDE para garantizar que los "packs" de agentes queden ubicados en las rutas donde cada IDE realmente los lee. Actualmente los usuarios de Kilo Code, GitHub Copilot y OpenCode experimentan comportamientos inconsistentes porque los agentes se instalan en rutas que las extensiones no utilizan.

### Scope (In)
- **Copilot**: Instalar agentes en `~/.copilot/agents/` (global) y `.github/agents/` (proyecto), no en `~/.vscode/agents/`
- **Kilo Code**: Instalar agentes en `~/.config/kilo/agents/` (global) y `.kilo/agents/` (proyecto), leyendo desde fuentes OpenCode
- **OpenCode**: Garantizar instalación en `~/.config/opencode/agents/` (global) y `.opencode/agents/` (proyecto), con parcheo correcto de `opencode.flowforge.json`
- **Cursor**: Mantener paridad como referencia (ya funciona en `~/.cursor/agents/`)
- **Antigravity**: Instalar agentes en `~/.gemini/antigravity/` (global: `AGENTS.md` + `rules/` + `workflows/`) y proyecto (`.agents/rules/` + `.agents/workflows/` + `AGENTS.md` root)
- **Corregir nomenclatura**: Eliminar todas las referencias que confunden `~/.gemini` con Claude Desktop — es Antigravity (Google IDE), no Anthropic
- **Extender `flowforge doctor`**: Detectar qué extensión VS Code está instalada (Kilo vs Copilot) y verificar rutas globales
- **Paridad Windows**: Actualizar `install.ps1` para detección de Kilo Code igual que `install.sh`
- **CI**: Actualizar tests para validar nuevas rutas objetivo
- **Documentación**: Actualizar CHANGELOG.md, README, y ADR-001 con rutas correctas por IDE; corregir `scripts/docker-pm1-test.sh`

### Scope (Out)
- Cambiar el formato de los archivos de agentes (mantener compatibilidad)
- Agregar nuevos agentes o modificar contenido de agentes existentes
- Implementar migración automática de modelos antiguos (qwen3.5 → qwen3.7)
- Soporte para nuevos IDEs no listados
- Cambiar lógica de detección de IDEs basada en procesos en ejecución

---

## 2. Functional requirements (FR)

### FR-001: Instalación global de GitHub Copilot (corrección de ruta)
El instalador debe colocar agentes en la ruta correcta para GitHub Copilot, no en `.vscode/`.

- **Scenario A (Global Copilot):** Given VS Code con extensión `github.copilot` instalada, When ejecuta `flowforge install`, Then los agentes se copian a `~/.copilot/agents/*.agent.md` y las instrucciones a `~/.copilot/instructions/flowforge.instructions.md`

- **Scenario B (Sin Copilot):** Given VS Code instalado pero sin extensión `github.copilot`, When ejecuta `flowforge install`, Then el instalador omite la instalación Copilot (no crea `~/.copilot/`) y muestra mensaje informativo

### FR-002: Instalación global de Kilo Code (nueva funcionalidad)
El instalador debe detectar y configurar agentes para Kilo Code cuando está presente.

- **Scenario A (Kilo detectado):** Given VS Code con extensión `kilocode.*` instalada, When ejecuta `flowforge install`, Then los agentes se copian a `~/.config/kilo/agents/*.md` (formato OpenCode)

- **Scenario B (Best-effort sin VS Code):** Given no hay VS Code instalado, When ejecuta `flowforge install`, Then el instalador crea `~/.config/kilo/agents/` con agentes (best-effort para futura instalación de Kilo)

- **Scenario C (Paridad Windows):** Given Windows con Kilo Code instalado, When ejecuta `install.ps1`, Then detecta `kilocode.*` en `%USERPROFILE%\.vscode\extensions` e instala en `%USERPROFILE%\.config\kilo\agents\`

### FR-003: Instalación global de OpenCode (consolidación)
El instalador debe consolidar la instalación OpenCode global, limpiando enfoques antiguos.

- **Scenario A (Instalación limpia):** Given OpenCode instalado, When ejecuta `flowforge install`, Then los agentes se copian a `~/.config/opencode/agents/*.md` y comandos a `~/.config/opencode/commands/*.md`

- **Scenario B (Limpieza legacy):** Given existe directorio `~/.config/opencode/flowforge/` o archivo `opencode.flowforge.json` antiguo, When ejecuta instalación, Then se eliminan los archivos legacy (no se crea `flowforge/` ni JSON en raíz de opencode)

### FR-004: Instalación a nivel proyecto con `flowforge init`
El comando `init` debe instalar agentes en rutas de proyecto correctas para cada IDE.

- **Scenario A (Copilot project):** Given ejecución de `flowforge init <ruta>`, Then crea `.github/agents/*.agent.md` y `.github/copilot-instructions.md`

- **Scenario B (OpenCode project):** Given ejecución de `flowforge init <ruta>`, Then crea `.opencode/agents/*.md` en el proyecto

- **Scenario C (Kilo project):** Given ejecución de `flowforge init <ruta>`, Then crea `.kilo/agents/*.md` (duplicado desde OpenCode para máxima compatibilidad)

- **Scenario D (Cursor project):** Given ejecución de `flowforge init <ruta>`, Then crea `.cursor/agents/*.md`, `.cursor/rules/*.mdc`, y `.cursor/commands/*.md`

### FR-005: Extensión de `flowforge doctor` para diagnóstico multi-IDE
El comando doctor debe reportar estado detallado de instalación por IDE.

- **Scenario A (Detección VS Code):** Given `flowforge doctor`, Then detecta si VS Code tiene `github.copilot` o `kilocode.*` y reporta `[✓]` o `[✗]` para cada extensión encontrada

- **Scenario B (Rutas globales):** Given `flowforge doctor`, Then verifica existencia de `~/.copilot/agents/`, `~/.config/kilo/agents/`, `~/.config/opencode/agents/`, `~/.cursor/agents/` y reporta estado

- **Scenario C (Rutas proyecto):** Given `flowforge doctor` ejecutado dentro de un proyecto, Then verifica `.github/agents/`, `.opencode/agents/`, `.kilo/agents/`, `.cursor/agents/` y reporta estado

### FR-006: Paridad de detección entre scripts y código C#
La lógica de detección de extensiones VS Code debe ser idéntica en `install.sh`, `install.ps1` y `FlowForgeModule.cs`.

- **Scenario A (Detección Kilo shell):** Given `install.sh`, Then busca `kilocode.*` en `~/.vscode/extensions/`

- **Scenario B (Detección Kilo C#):** Given `FlowForgeModule.HasVsCodeExtension()`, Then busca `kilocode.` en `~/.vscode/extensions/`

- **Scenario C (Detección Kilo PowerShell):** Given `install.ps1`, Then busca `kilocode.*` en `$env:USERPROFILE\.vscode\extensions`

### FR-007: CI actualizado para validar rutas IDE
Los tests de CI deben verificar las nuevas rutas de instalación.

- **Scenario A (Test Linux):** Given CI en ubuntu-latest, When ejecuta flujo de instalación, Then verifica que existen agentes en `~/.copilot/agents/` (simulado) y `~/.config/kilo/agents/`

- **Scenario B (Test Windows):** Given CI en windows-latest, When ejecuta flujo de instalación, Then verifica que existen agentes en `%USERPROFILE%\.copilot\agents\` (simulado)

### FR-008: Documentación actualizada
CHANGELOG, README, guías de IDE y ADRs deben reflejar las rutas correctas por IDE, la separación Copilot/Kilo/Antigravity/Claude Desktop, y eliminar referencias obsoletas (`~/.vscode/agents/`, `opencode.flowforge.json` como flujo principal).

- **Scenario A (README rutas):** Given usuario lee `README.md` o `README.es.md`, Then encuentra tabla con rutas globales y de proyecto para cada IDE soportado (incl. Kilo y Antigravity)

- **Scenario B (CHANGELOG entrada):** Given release v0.1.0-alpha.x, Then incluye entrada documentando corrección de rutas IDE y fix Antigravity vs Claude Desktop

- **Scenario C (ide/README matriz):** Given usuario lee `ide/README.md`, Then encuentra matriz IDE × ruta global × ruta proyecto × formato de agente, sin instrucciones legacy de merge `opencode.flowforge.json` como camino principal

- **Scenario D (Sección 6 del spec):** Given implementador lee `spec.md` sección **Documentation impact**, Then tiene lista cerrada de archivos `.md` a tocar con tipo de cambio esperado

### FR-009: Antigravity global + project install parity
El instalador debe soportar Antigravity (Google IDE) en ambos modos: global y proyecto, con paridad entre shell scripts y C#.

- **Scenario A (Global install C#):** Given `flowforge install`, Then `FlowForgeModule.InstallAntigravity()` copia agentes a `~/.gemini/antigravity/AGENTS.md`, `~/.gemini/antigravity/rules/`, `~/.gemini/antigravity/workflows/`

- **Scenario B (Global install shell):** Given `install.sh --global`, Then detecta Antigravity e instala en `~/.gemini/antigravity/` (no solo hint/mensaje)

- **Scenario C (Global install PowerShell):** Given `install.ps1`, Then detecta Antigravity e instala en `~/.gemini/antigravity/` (no solo hint)

- **Scenario D (Project install init):** Given `flowforge init <ruta>`, Then crea `.agents/rules/`, `.agents/workflows/`, y `AGENTS.md` en raíz del proyecto para Antigravity

- **Scenario E (Project install shell):** Given `install.sh` sin --global en proyecto, Then crea `.agents/` estructura para Antigravity

### FR-010: Corregir referencias que confunden Antigravity con Claude Desktop
Toda documentación, scripts y ADRs que mencionan incorrectamente `~/.gemini` como "Claude Desktop" deben corregirse a "Antigravity".

- **Scenario A (ADR-001 corregido):** Given lee `docs/architecture/adr/ADR-001-installer-technology-stack.md`, Then tabla de IDEs lista Antigravity con rutas `~/.gemini/antigravity/`, NO Claude Desktop

- **Scenario B (docker-pm1-test.sh corregido):** Given lee `scripts/docker-pm1-test.sh`, Then comentarios y mensajes dicen "Antigravity" para `~/.gemini/`, NO "Claude Desktop"

- **Scenario C (install.sh sin confusión):** Given lee `install.sh`, Then ninguna línea menciona "Claude Desktop" junto a "~/.gemini"

- **Scenario D (install.ps1 sin confusión):** Given lee `install.ps1`, Then ninguna línea menciona "Claude Desktop" junto a "~/.gemini"

- **Scenario E (AGENTS.md documenta ambos layouts):** Given lee `ide/antigravity/AGENTS.md`, Then documenta explícitamente layout global (`~/.gemini/antigravity/`) y layout proyecto (`.agents/`)

---

## 3. Non-functional requirements (NFR)

### NFR-001: Backwards compatibility
El instalador no debe destruir configuraciones personalizadas del usuario. Usar respaldo antes de sobrescribir.

### NFR-002: Non-destructive installation
Si un directorio de destino contiene archivos que no son de FlowForge (no coinciden con `forge-*.md` ni `*.agent.md`), el instalador debe hacer backup antes de cualquier operación.

### NFR-003: Performance
La detección de extensiones VS Code debe tomar < 100ms por prefijo de extensión (búsqueda en directorio).

### NFR-004: Cross-platform
Los cambios deben funcionar en Linux, macOS y Windows. Las rutas deben usar `Path.Combine` en C# y `$HOME`/`$env:USERPROFILE` en scripts.

### NFR-005: Clear error messages
Si la instalación falla por permisos u otro error, el mensaje debe incluir la ruta exacta que falló y sugerencia de corrección.

---

## 4. Capability Matrix

| Requirement | Component | Implementation notes |
|-------------|-----------|---------------------|
| FR-001 | `FlowForgeModule.cs` `InstallVsCode()` | Cambiar destino de `~/.vscode/` a `~/.copilot/` mediante `PathHelper.CopilotAgents` |
| FR-002 | `FlowForgeModule.cs` + `install.sh/.ps1` | Agregar detección `kilocode.*` e instalar desde `ide/opencode/agents/` a `~/.config/kilo/agents/` |
| FR-003 | `FlowForgeModule.cs` `InstallOpenCode()` | Mantener comportamiento actual, asegurar limpieza de legacy |
| FR-004 | `InitCommand.cs` `InstallVsCodeProjectPack()` | Ya implementado parcialmente, verificar que instala los 4 destinos |
| FR-005 | `DoctorCommand.cs` | Extender lista de checks con detección de extensiones y verificación de rutas por IDE |
| FR-006 | `install.ps1` | Agregar detección Kilo similar a `install.sh` (líneas 166-168) |
| FR-007 | `.github/workflows/test-installer.yml` | Agregar pasos de verificación para `~/.copilot/` y `~/.config/kilo/` |
| FR-008 | Ver **§6 Documentation impact** | Inventario cerrado de `.md` a actualizar; matriz rutas en README + ide/README |
| FR-009 | `FlowForgeModule.cs` + `install.sh/.ps1` + `InitCommand.cs` | Implementar `InstallAntigravity()` global en `~/.gemini/antigravity/` y proyecto en `.agents/`; paridad shell/C# |
| FR-010 | `docs/architecture/adr/ADR-001-*.md`, `scripts/docker-pm1-test.sh`, `install.sh`, `install.ps1`, `ide/antigravity/AGENTS.md` | Reemplazar "Claude Desktop" por "Antigravity" para todo lo referente a `~/.gemini/`; documentar dualidad de layouts |

---

## 5. Developer manual tests (required — mark [x] before /flow-close)

| ID | Case / flow | Steps (summary) | Expected result | [ ] |
|----|-------------|-----------------|-----------------|-----|
| PM-1 | Cursor global + proyecto | 1. `flowforge install` (con Cursor instalado)<br>2. `flowforge init ./test-project` | Agentes en `~/.cursor/agents/` y `./test-project/.cursor/agents/` | [ ] |
| PM-2 | Copilot global (VS Code) | 1. Instalar VS Code + Copilot<br>2. `flowforge install` | Agentes en `~/.copilot/agents/*.agent.md`, instrucciones en `~/.copilot/instructions/` | [ ] |
| PM-3 | Kilo global (VS Code) | 1. Instalar VS Code + Kilo Code<br>2. `flowforge install` | Agentes en `~/.config/kilo/agents/*.md` | [ ] |
| PM-4 | OpenCode global | 1. Instalar OpenCode<br>2. `flowforge install` | Agentes en `~/.config/opencode/agents/*.md`, sin directorio legacy `flowforge/` | [ ] |
| PM-5 | Proyecto multi-IDE | 1. `flowforge init ./my-project`<br>2. Verificar estructura | Existen `.github/agents/`, `.opencode/agents/`, `.kilo/agents/`, `.cursor/agents/` | [ ] |
| PM-6 | Doctor detecta extensiones | 1. Instalar Copilot<br>2. `flowforge doctor` | Tabla muestra `[✓] github.copilot` o `[✗]` según corresponda | [ ] |
| PM-7 | Windows Kilo parity | 1. En Windows, instalar VS Code + Kilo<br>2. `install.ps1` | Se detecta Kilo, se instala en `%USERPROFILE%\.config\kilo\agents\` | [ ] |
| PM-8 | Antigravity global + project | 1. `flowforge install` (detecta Antigravity)<br>2. `flowforge init ./test-project` | Agentes en `~/.gemini/antigravity/` y `./test-project/.agents/` | [ ] |
| PM-9 | Docs — matriz rutas | 1. Abrir `README.md` + `ide/README.md`<br>2. Buscar `~/.vscode/agents` y `Claude Desktop` + `~/.gemini` | Tabla IDE completa; cero menciones incorrectas Antigravity↔Claude | [ ] |

---

## 6. Documentation impact (inventario cerrado)

Archivos de documentación que **esta feature debe tocar**. El plan (`plan.md`) usará esta lista como checklist; no agregar otros `.md` sin actualizar el spec.

### 6.1 Entrada principal (usuario final)

| Archivo | Tipo de cambio | Qué actualizar |
|---------|----------------|----------------|
| `README.md` | **UPDATE** | Sección *IDE integration*: tabla global + proyecto por IDE; distinguir **GitHub Copilot** vs **Kilo Code** en VS Code; Antigravity `~/.gemini/antigravity/` vs `.agents/`; quitar `opencode.flowforge.json` como install path principal |
| `README.es.md` | **UPDATE** | Paridad ES de lo anterior |
| `QUICKSTART.md` | **UPDATE** | Rutas post-`flowforge install` / `flowforge init` si difieren del README |
| `QUICKSTART.es.md` | **UPDATE** | Paridad ES |
| `CHANGELOG.md` | **UPDATE** | Entrada `[Unreleased]`: fix rutas IDE, Kilo, Antigravity≠Claude, OpenCode modelos |

### 6.2 Guías de integración IDE

| Archivo | Tipo de cambio | Qué actualizar |
|---------|----------------|----------------|
| `ide/README.md` | **UPDATE** | Matriz completa OpenCode / Cursor / Antigravity / VS Code (Copilot + Kilo); flujo `flowforge install` vs `ide/install.sh`; eliminar o marcar **deprecated** merge manual de `opencode.flowforge.json` |
| `ide/antigravity/AGENTS.md` | **UPDATE** | Secciones **Global** (`~/.gemini/antigravity/`) vs **Project** (`.agents/`); corregir referencias a `.agents/rules/` cuando aplica solo a proyecto |
| `ide/opencode/AGENTS.md` | **UPDATE** | Agents en `~/.config/opencode/agents/` + `.opencode/agents/`; MCP en `opencode.json`; quitar referencia a `opencode.flowforge.json` como orquestador primario |

### 6.3 Documentación técnica / metodología

| Archivo | Tipo de cambio | Qué actualizar |
|---------|----------------|----------------|
| `docs/16-ide-integration-plan.md` | **UPDATE** | Tabla Antigravity: global vs project; VS Code split Copilot/Kilo; alinear con installer C# |
| `docs/09-open-source-integration.md` | **UPDATE** | Sección OpenCode: bundle markdown agents, no JSON merge legacy |
| `docs/architecture/adr/ADR-001-installer-technology-stack.md` | **UPDATE** | Reemplazar "Claude Desktop" por **Antigravity** en lista de IDEs; nota MCP manual para Claude Desktop |
| `GLOSSARY.md` | **UPDATE** | Entrada Antigravity: rutas `~/.gemini/antigravity/` (global) y `.agents/` (proyecto) |
| `GLOSSARY.es.md` | **UPDATE** | Paridad ES |
| `CONTRIBUTING.md` | **UPDATE** | Tabla IDE / paths si menciona solo `.agents/` sin global |
| `templates/project/QUICKSTART.project.md` | **UPDATE** | Estructura post-`flowforge init`: `.github/`, `.opencode/`, `.kilo/`, `.cursor/`, `.agents/` |

### 6.4 ADRs y decisiones existentes

| Archivo | Tipo de cambio | Qué actualizar |
|---------|----------------|----------------|
| `docs/decisions/ADR-006-opencode-mcp-config.md` | **UPDATE** (follow-up) | Follow-up: documentar que agents son `.md` en `agents/`, no solo JSON; MCP `type: local` |
| `docs/decisions/README.md` | **OPTIONAL** | Índice si se crea ADR nuevo (ver §6.5) |

### 6.5 Documentación nueva (opcional, plan decide)

| Archivo | Tipo de cambio | Qué crear |
|---------|----------------|-----------|
| `docs/decisions/ADR-008-ide-installer-path-matrix.md` | **CREATE** (recomendado) | Matriz canónica IDE × global × project × MCP × detección; Antigravity ≠ Claude Desktop |

### 6.6 Comentarios en scripts/CI (no `.md`, pero FR-010)

| Archivo | Tipo de cambio | Qué actualizar |
|---------|----------------|----------------|
| `scripts/docker-pm1-test.sh` | **UPDATE** | Comentarios "Claude Desktop" → **Antigravity** para `~/.gemini`; asserts nuevas rutas |
| `.github/workflows/test-installer.yml` | **UPDATE** | Verificar `~/.copilot/`, `~/.config/kilo/`, `~/.gemini/antigravity/` |

### 6.7 Explícitamente fuera de scope (no tocar)

| Archivo | Motivo |
|---------|--------|
| `.ai-work/fix-installer/*`, `.ai-work/stack-installer/*` | Artefactos históricos de features cerradas |
| `docs/12-engram-tool-reference.md` | Sin relación con rutas IDE |
| `skills/**/SKILL.md` | Contenido metodológico, no paths de install |
| `ide/cursor/**` | Sin cambios de ruta (referencia OK) |

### 6.8 Matriz canónica (referencia para todos los docs)

| IDE | Global | Proyecto | MCP / notas |
|-----|--------|----------|-------------|
| **Cursor** | `~/.cursor/agents/` + `rules/` + `commands/` | `.cursor/agents/` + `rules/` + `commands/` | `~/.cursor/mcp.json` |
| **OpenCode** | `~/.config/opencode/agents/` | `.opencode/agents/` | merge en `opencode.json` (`type: local`) |
| **GitHub Copilot** | `~/.copilot/agents/` + `instructions/` | `.github/agents/` + `copilot-instructions.md` | vía VS Code / Copilot |
| **Kilo Code** | `~/.config/kilo/agents/` | `.kilo/agents/` | extensión VS Code; formato OpenCode |
| **Antigravity** | `~/.gemini/antigravity/` | `.agents/rules/` + `workflows/` + `AGENTS.md` | `~/.gemini/antigravity/mcp_config.json` |
| **Claude Desktop** | — | — | MCP manual: `~/.config/Claude/claude_desktop_config.json` |

---

## 5. Open questions for human (OQ-*)

| ID | Tag | Question | Default / assumption |
|----|-----|---------|---------------------|
| OQ-1 | [BLOCKER] | ¿Cuál es el comportamiento deseado cuando VS Code no tiene ni Copilot ni Kilo instalados? ¿Debe instalar AMBOS formatos (Copilot en `~/.copilot/` y Kilo en `~/.config/kilo/`) como best-effort, u omitir ambos? | Default actual: instala ambos. Confirmar si este comportamiento es correcto o debe cambiar. |
| OQ-2 | [BLOCKER] | ¿Qué criterios de aceptación debe cumplir el CI para validar las nuevas rutas? ¿Tests en máquinas reales con VS Code instalado o mocks de directorios es suficiente? | Actualmente CI crea directorios mock (`mkdir -p ~/.vscode`). Definir si se requiere test adicional. |
| OQ-3 | [BLOCKER] | ¿El instalador debe migrar automáticamente modelos antiguos (qwen3.5 → qwen3.7) en agentes existentes del usuario, o solo instalar nuevos agentes y dejar los existentes sin modificar? | Implicación: migración automática puede romper configuraciones personalizadas. |
| OQ-4 | [OPTIONAL] | ¿Debe `flowforge doctor` ofrecer un flag `--fix` para reparar automáticamente las rutas incorrectas detectadas (copiar desde ubicación correcta), o solo reportar y sugerir comandos manuales? | Assumed: solo reportar; fixes manuales para evitar side-effects no deseados. |
| OQ-5 | [OPTIONAL] | ¿Agregar telemetría básica de qué IDEs fueron detectados y en qué rutas se escribieron los agentes (para diagnóstico de errores futuros)? | Assumed: no agregar telemetría en este scope; mantener logging local en `~/.engram/install.log`. |
| OQ-6 | [FOLLOW-UP] | ¿Soporte futuro para cuando Kilo Code tenga su propio formato de agente diferente a OpenCode? | — |
| OQ-7 | [OPTIONAL] | ¿Unificar AGENTS.md de Antigravity para documentar ambos layouts (global `~/.gemini/antigravity/` y proyecto `.agents/`) en un solo archivo, o mantener separación por contexto de uso? | Assumed: documentar ambos layouts en `ide/antigravity/AGENTS.md` con secciones claras "Global Install" vs "Project Install" |

---

> **Spec version:** 1.1  
> **Derived from:** Context Map from forge-discovery (fix-ide-installer-packs analysis)  
> **Key files read:** `FlowForgeModule.cs`, `InitCommand.cs`, `DoctorCommand.cs`, `install.sh`, `install.ps1`, `PathHelper.cs`, `test-installer.yml`  
> **Changelog spec:** v1.1 — §6 Documentation impact (inventario cerrado de `.md` a tocar)

## Memory Signal
- type: decision
- significance: high
- summary: "Centralizing IDE agent paths: Copilot → ~/.copilot/, Kilo → ~/.config/kilo/, OpenCode → ~/.config/opencode/, Cursor → ~/.cursor/, Antigravity → ~/.gemini/antigravity/ (NOT Claude Desktop). Clarified that Claude Desktop uses ~/.config/Claude/ for MCP only. Added FR-009 for Antigravity global (~/.gemini/antigravity/) and project (.agents/) install parity across C#, shell, and PowerShell. Added FR-010 to fix all docs/CI references that incorrectly labeled ~/.gemini as Claude Desktop."
