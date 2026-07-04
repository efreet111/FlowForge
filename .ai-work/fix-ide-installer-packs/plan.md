---
plan_version: "1.0"
spec_version: "1.1"
feature_slug: fix-ide-installer-packs
created: 2026-07-03
blocker_resolutions:
  - OQ-1: A — best-effort install both Copilot + Kilo formats when no extension detected
  - OQ-2: A — CI uses directory mocks sufficient
  - OQ-3: A — overwrite on reinstall only, no auto-migration of user-customized agent files
---

# Plan: Fix IDE Installer Packs

## 1. Impact and dependencies

### Existing components modified
- `FlowForgeModule.cs` — Add Kilo detection, fix Copilot paths, add Antigravity install
- `InitCommand.cs` — Verify project-level installs for all 5 IDEs
- `DoctorCommand.cs` — Extend checks for VS Code extensions and all IDE paths
- `PathHelper.cs` — Add Kilo and Antigravity path constants
- `install.sh` — Add Kilo detection parity, Antigravity global install
- `install.ps1` — Add Kilo detection (Windows parity), Antigravity global install

### New dependencies
- None (uses existing Spectre.Console, System.IO)

### External dependencies
- VS Code extension detection requires reading `~/.vscode/extensions/` directory
- Antigravity detection checks `~/.gemini/` directory existence

---

## 2. Naming & path conventions (cross-project)

> **Respuesta a la pregunta del usuario: "¿Es el mismo esquema de nombres en todos los proyectos?"**

### 2.1 Agent logical names — SAME across all IDEs and all projects
Los nombres lógicos de agentes son **canónicos** y universales:

| Rol | Nombre lógico | Descripción |
|-----|---------------|-------------|
| Orchestrator | `forge-orchestrator` | Punto de entrada, coordina checkpoints |
| Discovery | `forge-discovery` | Fase 0, context mapping |
| Architecture | `forge-arch` | Fase 1, especificación |
| Planning | `forge-plan` | Fase 2, planificación técnica |
| Development | `forge-dev` | Fase 3, implementación |
| Verification | `forge-verify` | Fase 3b, auditoría |
| Memory | `forge-memory` | Fase 4, persistencia |

Estos nombres son **inmutables** sin importar el IDE o proyecto.

### 2.2 File naming conventions — DIFFER by IDE (required by each platform)

| IDE | Patrón de archivo | Ejemplo | Notas |
|-----|-------------------|---------|-------|
| **Cursor** | `forge-*.md` | `forge-arch.md` | Sin sufijo .agent |
| **VS Code Copilot** | `forge-*.agent.md` | `forge-arch.agent.md` | Requiere sufijo .agent |
| **OpenCode** | `forge-*.md` + `flowforge.md` | `forge-arch.md`, `flowforge.md` | flowforge.md = orquestador |
| **Kilo Code** | `forge-*.md` + `flowforge.md` | `forge-arch.md`, `flowforge.md` | Igual que OpenCode (fuente de verdad compartida) |
| **Antigravity** | `AGENTS.md` + `rules/*.md` + `workflows/*.md` | — | Layout diferente, no forge-* |

### 2.3 Folder paths per project — SAME structure for every project after `flowforge init`

Todo proyecto inicializado tiene **la misma estructura** sin importar su nombre:

```
<project-root>/
├── .github/agents/           # GitHub Copilot
├── .github/copilot-instructions.md
├── .opencode/agents/          # OpenCode
├── .kilo/agents/               # Kilo Code (copia de OpenCode)
├── .cursor/agents/             # Cursor
├── .cursor/rules/
├── .cursor/commands/
├── .agents/rules/              # Antigravity (proyecto)
├── .agents/workflows/
└── AGENTS.md                   # Antigravity (proyecto, raíz)
```

### 2.4 Global vs project installation

| Tipo | Ubicación | Contenido | Frecuencia |
|------|-----------|-----------|------------|
| **Global** | `~/.cursor/`, `~/.copilot/`, `~/.config/opencode/`, `~/.config/kilo/`, `~/.gemini/antigravity/` | Agentes disponibles para todos los proyectos | Una vez por máquina |
| **Project** | `.cursor/`, `.github/`, `.opencode/`, `.kilo/`, `.agents/` (subdirs) | Agentes específicos del proyecto | Por cada `flowforge init` |

La instalación global se hace desde templates en `ide/` del repositorio FlowForge.

---

## 3. File changes (Proposed Changes)

### Code (C# Installer)

#### [MODIFY] `src/FlowForge.Installer/Modules/FlowForgeModule.cs`
- **Cambios:**
  - Modificar `InstallVsCode()` → ahora instala en `~/.copilot/` (no `~/.vscode/`)
  - Agregar `InstallKilo()` — nuevo método para detectar e instalar Kilo Code
  - Agregar `InstallAntigravity()` — nuevo método para instalar en `~/.gemini/antigravity/`
  - Agregar `HasVsCodeExtension(string prefix)` — detecta extensiones VS Code por prefijo
  - Modificar `WarnIfExistingAgents()` — agregar casos para Kilo y Antigravity

#### [MODIFY] `src/FlowForge.Installer/Commands/InitCommand.cs`
- **Cambios:**
  - Verificar `InstallVsCodeProjectPack()` instala los 4 destinos (Copilot, Kilo, OpenCode, Cursor)
  - Agregar `InstallAntigravityProjectPack()` — instala en `.agents/rules/`, `.agents/workflows/`, `AGENTS.md`

#### [MODIFY] `src/FlowForge.Installer/Commands/DoctorCommand.cs`
- **Cambios:**
  - Agregar checks de detección VS Code: `github.copilot` vs `kilocode.*`
  - Agregar verificación de rutas globales: `~/.copilot/agents/`, `~/.config/kilo/agents/`, `~/.config/opencode/agents/`, `~/.cursor/agents/`, `~/.gemini/antigravity/`
  - Agregar verificación de rutas proyecto (cuando se ejecuta dentro de un proyecto)

#### [MODIFY] `src/FlowForge.Installer/Infrastructure/PathHelper.cs`
- **Cambios:**
  - Agregar constantes: `KiloAgents`, `KiloConfigDir`, `AntigravityDir`, `CopilotAgents`, `CopilotInstructions`

### Scripts (Shell/PowerShell)

#### [MODIFY] `ide/install.sh`
- **Cambios:**
  - Agregar detección Kilo: `kilocode.*` en `~/.vscode/extensions/`
  - Agregar instalación Kilo: copiar desde `ide/opencode/agents/` a `~/.config/kilo/agents/`
  - Agregar instalación Antigravity global: copiar a `~/.gemini/antigravity/`
  - Corregir comentarios "Claude Desktop" → "Antigravity" para `~/.gemini/`

#### [MODIFY] `ide/install.ps1`
- **Cambios:**
  - Agregar detección Kilo en Windows: `kilocode.*` en `$env:USERPROFILE\.vscode\extensions`
  - Agregar instalación Kilo: `%USERPROFILE%\.config\kilo\agents\`
  - Agregar instalación Antigravity global
  - Corregir comentarios "Claude Desktop" → "Antigravity"

### CI

#### [MODIFY] `.github/workflows/test-installer.yml`
- **Cambios:**
  - Agregar pasos de verificación para `~/.copilot/agents/`
  - Agregar pasos de verificación para `~/.config/kilo/agents/`
  - Agregar pasos de verificación para `~/.gemini/antigravity/`

### Tests

#### [NEW] `tests/FlowForge.Installer.Tests/InstallCommandSourceTests.cs` (si no existe)
- Validar que las rutas en C# coinciden con las rutas en scripts

### Documentation

#### [UPDATE] `README.md`
- Sección IDE integration: tabla global + proyecto por IDE
- Distinguir Copilot vs Kilo en VS Code
- Antigravity `~/.gemini/antigravity/` vs `.agents/`
- Quitar `opencode.flowforge.json` como install path principal

#### [UPDATE] `README.es.md`
- Paridad español de lo anterior

#### [UPDATE] `ide/README.md`
- Matriz completa IDE × ruta global × ruta proyecto × formato
- Flujo `flowforge install` vs `ide/install.sh`
- Marcar como **deprecated** merge manual de `opencode.flowforge.json`

#### [UPDATE] `ide/antigravity/AGENTS.md`
- Sección Global (`~/.gemini/antigravity/`) vs Project (`.agents/`)
- Corregir referencias a `.agents/rules/` cuando aplica solo a proyecto

#### [UPDATE] `ide/opencode/AGENTS.md`
- Agents en `~/.config/opencode/agents/` + `.opencode/agents/`
- MCP en `opencode.json` (`type: local`)
- Quitar referencia a `opencode.flowforge.json` como orquestador primario

#### [UPDATE] `CHANGELOG.md`
- Entrada `[Unreleased]`: fix rutas IDE, Kilo, Antigravity≠Claude, OpenCode modelos

#### [UPDATE] `docs/architecture/adr/ADR-001-installer-technology-stack.md`
- Reemplazar "Claude Desktop" por **Antigravity** en lista de IDEs
- Nota MCP manual para Claude Desktop

#### [NEW] `docs/decisions/ADR-008-ide-installer-path-matrix.md`
- Matriz canónica IDE × global × project × MCP × detección
- Antigravity ≠ Claude Desktop

#### [UPDATE] `scripts/docker-pm1-test.sh`
- Comentarios "Claude Desktop" → "Antigravity" para `~/.gemini/`
- Asserts nuevas rutas

#### [UPDATE] `docs/16-ide-integration-plan.md`
- Tabla Antigravity: global vs project
- VS Code split Copilot/Kilo

#### [UPDATE] `docs/09-open-source-integration.md`
- Sección OpenCode: bundle markdown agents, no JSON merge legacy

#### [UPDATE] `GLOSSARY.md` / `GLOSSARY.es.md`
- Entrada Antigravity: rutas `~/.gemini/antigravity/` (global) y `.agents/` (proyecto)

#### [UPDATE] `CONTRIBUTING.md`
- Tabla IDE / paths si menciona solo `.agents/` sin global

#### [UPDATE] `templates/project/QUICKSTART.project.md`
- Estructura post-`flowforge init`: `.github/`, `.opencode/`, `.kilo/`, `.cursor/`, `.agents/`

---

## 4. Contracts and schemas

### 4.1 IDE Detection Contract

```csharp
/// <summary>
/// Detects if a VS Code extension is installed by checking the extensions directory.
/// </summary>
/// <param name="prefix">Extension ID prefix (e.g., "kilocode.", "github.copilot")</param>
/// <returns>true if at least one matching extension directory exists</returns>
public static bool HasVsCodeExtension(string prefix)
{
    var extensionsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".vscode", "extensions");
    if (!Directory.Exists(extensionsDir)) return false;
    
    return Directory.EnumerateDirectories(extensionsDir)
        .Any(d => Path.GetFileName(d).StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
}
```

### 4.2 Path Helper Constants

```csharp
public static class PathHelper
{
    // Copilot (corrected from .vscode)
    public static string CopilotAgents => Path.Combine(Home, ".copilot", "agents");
    public static string CopilotInstructions => Path.Combine(Home, ".copilot", "instructions");
    
    // Kilo Code (new)
    public static string KiloConfigDir => Path.Combine(Home, ".config", "kilo");
    public static string KiloAgents => Path.Combine(KiloConfigDir, "agents");
    
    // OpenCode (existing, verify cleanup legacy)
    public static string OpenCodeConfigDir => Path.Combine(Home, ".config", "opencode");
    public static string OpenCodeAgents => Path.Combine(OpenCodeConfigDir, "agents");
    public static string OpenCodeCommands => Path.Combine(OpenCodeConfigDir, "commands");
    
    // Antigravity (new)
    public static string AntigravityDir => Path.Combine(Home, ".gemini", "antigravity");
    public static string AntigravityRules => Path.Combine(AntigravityDir, "rules");
    public static string AntigravityWorkflows => Path.Combine(AntigravityDir, "workflows");
    
    // Cursor (existing)
    public static string CursorDir => Path.Combine(Home, ".cursor");
    public static string CursorAgents => Path.Combine(CursorDir, "agents");
    public static string CursorRules => Path.Combine(CursorDir, "rules");
    public static string CursorCommands => Path.Combine(CursorDir, "commands");
}
```

### 4.3 Doctor Output Format

```
FlowForge Doctor — diagnóstico del sistema

| Check                    | Estado   | Detalle                    |
|--------------------------|----------|----------------------------|
| flowforge binary         | [green]✓ OK[/]  | /usr/local/bin/flowforge |
| VS Code: github.copilot  | [green]✓ OK[/]  | v1.234.5                 |
| VS Code: kilocode.*      | [red]✗ FAIL[/]  | No detectado             |
| ~/.copilot/agents/       | [green]✓ OK[/]  | 8 archivos               |
| ~/.config/kilo/agents/   | [red]✗ FAIL[/]  | No existe                |
| ~/.config/opencode/...   | [green]✓ OK[/]  | 8 archivos               |
| ~/.cursor/agents/        | [green]✓ OK[/]  | 8 archivos               |
| ~/.gemini/antigravity/   | [yellow]![/]   | No instalado (opcional)  |
```

### 4.4 Project Install Structure

```
{projectRoot}/
├── .github/
│   ├── agents/
│   │   ├── forge-discovery.agent.md
│   │   ├── forge-arch.agent.md
│   │   └── ...
│   └── copilot-instructions.md
├── .kilo/
│   └── agents/
│       ├── forge-discovery.md
│       ├── flowforge.md
│       └── ...
├── .opencode/
│   └── agents/
│       ├── forge-discovery.md
│       ├── flowforge.md
│       └── ...
├── .cursor/
│   ├── agents/
│   │   ├── forge-discovery.md
│   │   └── ...
│   ├── rules/
│   └── commands/
├── .agents/
│   ├── rules/
│   └── workflows/
└── AGENTS.md   (Antigravity project orchestrator)
```

---

## 5. Implementation checklist

### Grupo A: Code (C# Installer)

- [x] **A.1** [FR-001] Modificar `FlowForgeModule.InstallVsCode()` para instalar en `~/.copilot/` en lugar de `~/.vscode/`
  - Cambiar destino: `PathHelper.CopilotAgents` y `PathHelper.CopilotInstructions`
  - Renombrar archivos a patrón `*.agent.md` para Copilot

- [x] **A.2** [FR-002] Implementar `FlowForgeModule.HasVsCodeExtension(string prefix)`
  - Buscar en `~/.vscode/extensions/` por prefijo
  - Usar en detección condicional de Copilot y Kilo

- [x] **A.3** [FR-002] Implementar `FlowForgeModule.InstallKilo()`
  - Origen: `ide/opencode/agents/*.md` (formato compartido)
  - Destino: `~/.config/kilo/agents/`
  - Incluir `flowforge.md` como orquestador

- [x] **A.4** [FR-003] Verificar `FlowForgeModule.InstallOpenCode()` limpia legacy
  - Eliminar `~/.config/opencode/flowforge/` si existe
  - Eliminar `~/.config/opencode/opencode.flowforge.json` raíz antiguo

- [x] **A.5** [FR-009] Implementar `FlowForgeModule.InstallAntigravity()`
  - Destino global: `~/.gemini/antigravity/`
  - Estructura: `AGENTS.md` + `rules/` + `workflows/`
  - No confundir con Claude Desktop

- [x] **A.6** [FR-004] Verificar `InitCommand` instala los 5 destinos de proyecto
  - `.github/agents/` (Copilot)
  - `.kilo/agents/` (Kilo)
  - `.opencode/agents/` (OpenCode)
  - `.cursor/agents/` (Cursor)
  - `.agents/` + `AGENTS.md` (Antigravity)

- [x] **A.7** [FR-005] Extender `DoctorCommand` con checks de extensiones VS Code
  - Detectar `github.copilot` → `[✓]` o `[✗]`
  - Detectar `kilocode.*` → `[✓]` o `[✗]`

- [x] **A.8** [FR-005] Extender `DoctorCommand` con verificación de rutas globales
  - `~/.copilot/agents/`
  - `~/.config/kilo/agents/`
  - `~/.config/opencode/agents/`
  - `~/.cursor/agents/`
  - `~/.gemini/antigravity/`

- [x] **A.9** [FR-005] Extender `DoctorCommand` con verificación de rutas proyecto
  - Detectar si está en un proyecto (existencia de `.git` o `flowforge.yaml`)
  - Verificar subdirectorios `.github/`, `.kilo/`, `.opencode/`, `.cursor/`, `.agents/`

- [x] **A.10** [NFR-001, NFR-002] Implementar backup-then-write en todos los métodos de instalación
  - Si directorio destino contiene archivos no- FlowForge, crear backup timestamped

- [x] **A.11** [NEW] Agregar constantes de ruta en `PathHelper.cs`
  - `CopilotAgents`, `CopilotInstructions`
  - `KiloConfigDir`, `KiloAgents`
  - `AntigravityDir`, `AntigravityRules`, `AntigravityWorkflows`

### Grupo B: Scripts (Shell/PowerShell)

- [x] **B.1** [FR-002, FR-006] Agregar detección Kilo en `install.sh`
  - Líneas 166-168 equivalentes: buscar `kilocode.*` en `~/.vscode/extensions/`

- [x] **B.2** [FR-002] Agregar instalación Kilo en `install.sh`
  - Copiar desde `ide/opencode/agents/` a `~/.config/kilo/agents/`

- [x] **B.3** [FR-009] Agregar instalación Antigravity en `install.sh --global`
  - Detectar `~/.gemini/` existente
  - Instalar en `~/.gemini/antigravity/`

- [x] **B.4** [FR-010] Corregir comentarios en `install.sh`
  - Reemplazar "Claude Desktop" por "Antigravity" para todo `~/.gemini/`

- [x] **B.5** [FR-002, FR-006] Agregar detección Kilo en `install.ps1`
  - Buscar `kilocode.*` en `$env:USERPROFILE\.vscode\extensions`

- [x] **B.6** [FR-002] Agregar instalación Kilo en `install.ps1`
  - Copiar a `%USERPROFILE%\.config\kilo\agents\`

- [x] **B.7** [FR-009] Agregar instalación Antigravity en `install.ps1`
  - Instalar en `~/.gemini/antigravity/` (o `%USERPROFILE%\.gemini\antigravity\`)

- [x] **B.8** [FR-010] Corregir comentarios en `install.ps1`
  - Reemplazar "Claude Desktop" por "Antigravity" para todo `~/.gemini/`

### Grupo C: Doctor Command

- [x] **C.1** [FR-005] Implementar detección de extensiones VS Code en `DoctorCommand`
  - Table row: `VS Code: github.copilot`
  - Table row: `VS Code: kilocode.*`

- [x] **C.2** [FR-005] Implementar verificación de rutas globales
  - Una fila por ruta global, indicando `[✓]` si existe y tiene contenido

- [x] **C.3** [FR-005] Implementar verificación de rutas de proyecto (condicional)
  - Solo mostrar si el directorio actual parece un proyecto FlowForge

### Grupo D: CI

- [x] **D.1** [FR-007] Actualizar `.github/workflows/test-installer.yml`
  - Agregar verificación de `~/.copilot/agents/` (usando mock `mkdir -p`)
  - Agregar verificación de `~/.config/kilo/agents/`
  - Agregar verificación de `~/.gemini/antigravity/`

- [x] **D.2** [FR-007] Agregar test Windows para Kilo parity
  - Job windows-latest que verifica detección e instalación Kilo

### Grupo E: Documentation (según §6 inventory del spec)

- [x] **E.1.1** [FR-008] Update `README.md` — Sección IDE integration
  - Tabla global + proyecto por IDE
  - Distinguir Copilot vs Kilo en VS Code
  - Antigravity paths
  - Quitar `opencode.flowforge.json` como install path principal

- [x] **E.1.2** [FR-008] Update `README.es.md` — Paridad español

- [x] **E.1.3** [FR-008] Update `QUICKSTART.md` + `QUICKSTART.es.md` — Rutas post-install

- [x] **E.1.4** [FR-008] Update `CHANGELOG.md` — Entrada `[Unreleased]`

#### E.2 Guías de integración IDE
- [x] **E.2.1** [FR-008] Update `ide/README.md` — Matriz IDE × ruta global × ruta proyecto × formato

- [x] **E.2.2** [FR-010] Update `ide/antigravity/AGENTS.md` — Global vs Project sections

- [x] **E.2.3** [FR-008] Update `ide/opencode/AGENTS.md` — Quitar `opencode.flowforge.json` legacy

#### E.3 Documentación técnica / metodología
- [x] **E.3.1** [FR-008] Update `docs/16-ide-integration-plan.md` — Tabla Antigravity global vs project

- [x] **E.3.2** [FR-008] Update `docs/09-open-source-integration.md` — OpenCode bundle markdown

- [x] **E.3.3** [FR-010] Update `docs/architecture/adr/ADR-001-installer-technology-stack.md`
  - Reemplazar "Claude Desktop" por **Antigravity**
  - Nota MCP manual para Claude Desktop real

- [x] **E.3.4** [FR-008] Update `GLOSSARY.md` + `GLOSSARY.es.md` — Entrada Antigravity

- [x] **E.3.5** [FR-008] Update `CONTRIBUTING.md` — Tabla IDE / paths

- [x] **E.3.6** [FR-008] Update `templates/project/QUICKSTART.project.md` — Estructura post-init

#### E.4 ADRs y decisiones existentes
- [x] **E.4.1** [FR-008] Update `docs/decisions/ADR-006-opencode-mcp-config.md` — Follow-up agents .md

#### E.5 Documentación nueva (recomendado en spec §6.5)
- [x] **E.5.1** [FR-008, FR-010] Create `docs/decisions/ADR-008-ide-installer-path-matrix.md`
  - Matriz canónica IDE × global × project × MCP × detección
  - Clarificación Antigravity ≠ Claude Desktop

#### E.6 Comentarios en scripts/CI
- [x] **E.6.1** [FR-010] Update `scripts/docker-pm1-test.sh`
  - "Claude Desktop" → "Antigravity" para `~/.gemini/`
  - Asserts nuevas rutas

---

## 6. Partial work notes

Basado en el estado actual del repositorio (git status), hay trabajo parcial en:

### Ya modificado (uncommitted changes):
- `ide/install.ps1` — tiene cambios pendientes, verificar si ya incluye Kilo o es viejo
- `ide/install.sh` — tiene cambios pendientes
- `ide/opencode/agents/flowforge.md` — cambios en agentes
- `ide/opencode/agents/forge-dev.md` — cambios en agentes
- `ide/opencode/agents/forge-plan.md` — cambios en agentes
- `src/FlowForge.Installer/Commands/InitCommand.cs` — posible implementación parcial
- `src/FlowForge.Installer/Commands/InstallCommand.cs` — cambios pendientes
- `src/FlowForge.Installer/Infrastructure/PathHelper.cs` — posibles nuevas constantes
- `src/FlowForge.Installer/Modules/EngramModule.cs` — cambios relacionados
- `src/FlowForge.Installer/Modules/FlowForgeModule.cs` — posible implementación parcial de Kilo o Antigravity

### Tests nuevos (untracked):
- `tests/FlowForge.Installer.Tests/` — directorio completo con tests nuevo
  - `DocumentationTests.cs`
  - `GitHubReleasesClientTests.cs`
  - `InstallCommandSourceTests.cs`
  - `PathHelperTests.cs`
  - `ScriptTests.cs`
  - `.csproj`

**Acción recomendada:** Antes de iniciar desarrollo, revisar el diff de los archivos modificados para entender qué ya está implementado y evitar duplicar trabajo.

---

## 7. Manual tests required (PM-* gates)

| ID | Case / flow | Verificación | Estado |
|----|-------------|------------|--------|
| PM-1 | Cursor global + proyecto | Agentes en `~/.cursor/agents/` y `./test-project/.cursor/agents/` | [ ] |
| PM-2 | Copilot global (VS Code) | Agentes en `~/.copilot/agents/*.agent.md`, instrucciones en `~/.copilot/instructions/` | [ ] |
| PM-3 | Kilo global (VS Code) | Agentes en `~/.config/kilo/agents/*.md` | [ ] |
| PM-4 | OpenCode global | Agentes en `~/.config/opencode/agents/*.md`, sin directorio legacy `flowforge/` | [ ] |
| PM-5 | Proyecto multi-IDE | Existen `.github/agents/`, `.opencode/agents/`, `.kilo/agents/`, `.cursor/agents/` | [ ] |
| PM-6 | Doctor detecta extensiones | Tabla muestra `[✓] github.copilot` o `[✗]` según corresponda | [ ] |
| PM-7 | Windows Kilo parity | Se detecta Kilo, se instala en `%USERPROFILE%\.config\kilo\agents\` | [ ] |
| PM-8 | Antigravity global + project | Agentes en `~/.gemini/antigravity/` y `./test-project/.agents/` | [ ] |
| PM-9 | Docs — matriz rutas | Tabla IDE completa; cero menciones incorrectas Antigravity↔Claude | [ ] |

---

## 8. Definition of done

- [ ] Todos los items de implementación (A.1..E.6.1) completados
- [ ] CI pasa en ubuntu-latest y windows-latest
- [ ] `flowforge doctor` muestra correctamente estado de los 5 IDEs
- [ ] `flowforge init ./test` crea las 5 estructuras de proyecto
- [ ] `install.sh` y `install.ps1` tienen paridad de detección Kilo
- [ ] Cero referencias a "Claude Desktop" junto a `~/.gemini/` en codebase
- [ ] ADR-008 creado con matriz canónica de rutas
- [ ] CHANGELOG.md actualizado con entrada [Unreleased]
- [ ] PM-1..PM-9 completados o documentados con plan de mitigación

---

## 9. Riesgos y mitigaciones

| Riesgo | Impacto | Mitigación |
|--------|---------|------------|
| Cambio de ruta Copilot rompe usuarios existentes | Medio | NFR-001: backup antes de sobrescribir; mensaje claro en CHANGELOG |
| Detección VS Code extensiones lenta | Bajo | NFR-003: caching de resultados; <100ms por prefijo |
| Paridad Windows/Linux inconsistente | Medio | FR-006: tests CI en ambas plataformas; misma lógica C# vs scripts |
| Confusión Antigravity vs Claude Desktop permanece | Alto | FR-010: grep completo; ADR-008 documenta diferencia explícita |
