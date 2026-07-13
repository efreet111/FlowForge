# FlowForge — Inicio rápido

> **Empezá con FlowForge en unos 5 minutos.**

🇬🇧 Guía en inglés: [`QUICKSTART.md`](QUICKSTART.md)

---

## 1. Instalación

FlowForge tiene **dos instaladores** — elegí uno:

| | Stack installer (`curl \| bash`) | Instalador IDE (`curl \| bash`) |
|---|--------------------------------------|----------------------------------|
| **Instala** | CLI `flowforge` + `engram-dotnet` + agentes IDE | Solo agentes IDE |
| **Ejecuta wizard** | ✅ Sí (`flowforge install --yes`) | ❌ No |
| **Ideal para** | Setup inicial, stack completo | Agregar agentes a proyecto existente |
| **Requiere** | Internet (descarga binarios) | `git` en PATH |

> **¿Ya tenés flowforge?** Ejecutá `flowforge install --yes` directamente.
> Usá `--yes` para modo no interactivo (CI/CD, Docker, scripts).

### Instalador de stack (setup completo)

Descarga el CLI `flowforge` y ejecuta el wizard automáticamente.

**Linux/macOS:**

```bash
curl -fsSL https://raw.githubusercontent.com/efreet111/FlowForge/main/install/install.sh | bash
```

**Windows (PowerShell):**

```powershell
iwr -useb "https://raw.githubusercontent.com/efreet111/FlowForge/main/install/install.ps1" -OutFile $env:TEMP\flowforge-install.ps1
powershell -ExecutionPolicy Bypass -File $env:TEMP\flowforge-install.ps1
```

### Solo agentes del IDE

Copia agentes, reglas y comandos `/flow-*` a los IDEs detectados. Sin wizard, sin engram.

**Linux/macOS:**

```bash
curl -sSL https://raw.githubusercontent.com/efreet111/FlowForge/main/ide/install.sh | bash
```

**Windows (PowerShell):**

```powershell
iex ((New-Object System.Net.WebClient).DownloadString('https://raw.githubusercontent.com/efreet111/FlowForge/main/ide/install.ps1'))
```

---

## Rutas posteriores a la instalación

FlowForge escribe los agentes en los directorios que cada IDE realmente lee. Después de ejecutar cualquier instalador, confirmá las ubicaciones con esta matriz:

| IDE | Agentes globales | Agentes por proyecto | Notas |
|-----|------------------|----------------------|-------|
| **Cursor** | `~/.cursor/agents/`, `~/.cursor/rules/`, `~/.cursor/commands/` | `.cursor/agents/`, `.cursor/rules/`, `.cursor/commands/` | MCP en `~/.cursor/mcp.json`. |
| **OpenCode** | `~/.config/opencode/agents/`, `~/.config/opencode/commands/` | `.opencode/agents/`, `.opencode/commands/` | Skills en `{repo}/skills/` (no copiados). Ver [ADR-009](docs/decisions/ADR-009-opencode-antigravity-customizations.md). |
| **GitHub Copilot** | `~/.copilot/agents/*.agent.md`, `~/.copilot/instructions/flowforge.instructions.md` | `.github/agents/*.agent.md`, `.github/copilot-instructions.md` | Detectado por `github.copilot*`. |
| **Kilo Code** | `~/.config/kilo/agents/*.md` (el mismo bundle que OpenCode) | `.kilo/agents/*.md` (duplicado) | Detectado por `kilocode.*`. |
| **Antigravity** | `~/.gemini/config/` (`AGENTS.md`, `rules/`, `workflows/`, `skills/`, `mcp_config.json`) | `.agents/rules/`, `.agents/workflows/`, `.agents/skills/` | Google Antigravity (no Claude Desktop). |
| **Claude Desktop** | `~/.config/Claude/claude_desktop_config.json` (solo MCP) | — | Fuera del alcance de los packs de agentes. |

`flowforge doctor`, `ide/install.sh` e `ide/install.ps1` respetan esta matriz; consultá [`docs/decisions/ADR-008-ide-installer-path-matrix.md`](docs/decisions/ADR-008-ide-installer-path-matrix.md) para la estructura canónica.

## 2. Inicializar un proyecto {#inicializar-un-proyecto}

Luego de instalar el Stack, ejecutá `flowforge init` dentro de cada repositorio donde quieras el setup completo por proyecto:

```powershell
# Windows
flowforge init E:\Proyectos\mi-app

# o desde adentro del proyecto
cd E:\Proyectos\mi-app
flowforge init .
```

```bash
# Linux/macOS
flowforge init ~/projects/mi-app
```

Crea:

| Archivo / carpeta | Propósito |
|---|---|
| `.flowforge.json` | Config del proyecto (activa FlowDoc, `docs_framework`, teacher mode, etc.) |
| `AGENTS.md` | Guía para agentes en este repo |
| `docs/` | PRD, HUs, ADRs, RFCs, templates |
| `.ai-work/` | Artefactos versionados de cada feature |

**Flags:**

| Flag | Efecto |
|------|--------|
| `--no-flow-doc` | Omite la creación de `docs/` — solo `.flowforge.json` + `AGENTS.md` |
| `--yes` / `-y` | No interactivo (sin confirmaciones) |

> `flowforge init` es el único comando que escribe dentro de un directorio de proyecto. El `flowforge install` global solo toca `~/.cursor`, `~/.config`, etc.

### FlowDoc: activar, desactivar y rutas propias

FlowForge (metodología + `.ai-work/`) y FlowDoc (capa `docs/` de producto) son **independientes** pero diseñados para trabajar juntos. El control está en **`.flowforge.json`** en la raíz del proyecto.

| Objetivo | Qué hacer |
|----------|-----------|
| **Setup completo** (default) | `flowforge init .` — define `"docs_framework": "flowdoc"` + `"docs_framework_version": "2.0"` y `paths` por defecto |
| **Solo FlowForge** (sin FlowDoc) | `flowforge init . --no-flow-doc` — sin `docs/`, sin `docs_framework`; los agentes usan solo `.ai-work/` |
| **Desactivar después** | Editá `.flowforge.json`: quitá `"docs_framework"` o poné `"docs_framework": null` |
| **Carpetas propias** | Mantené `"docs_framework": "flowdoc"` + `"docs_framework_version": "2.0"` y apuntá `paths` a tus rutas |

Ejemplo — rutas personalizadas (semántica FlowDoc, tu árbol):

```json
{
  "docs_framework": "flowdoc",
  "docs_framework_version": "2.0",
  "paths": {
    "prd": "producto/requisitos.md",
    "backlog": "producto/historias",
    "decisions": "producto/adr",
    "rfcs": "producto/rfc",
    "development": "CONTRIBUTING.md",
    "features": ".ai-work",
    "templates": "producto/plantillas"
  }
}
```

Ejemplo — solo FlowForge (sin capa de documentación de producto):

```json
{
  "paths": {
    "features": ".ai-work"
  }
}
```

En `/flow-start`, `forge-discovery` lee este archivo: si `docs_framework` no está o es `null`, **omite** el paso FlowDoc y sigue solo con la metodología FlowForge.

### Dónde queda cada cosa (global vs proyecto)

| Qué | Comando | Ubicación por defecto (Windows) |
|-----|---------|----------------------------------|
| Agentes IDE, reglas, comandos `/flow-*` | `flowforge install` (componente FlowForge) | `C:\Users\<vos>\.cursor\` |
| Log y config del instalador FlowForge | `flowforge install` | `C:\Users\<vos>\.engram\config.json`, `install.log` |
| Binario engram-dotnet | `flowforge install` (componente engram) | `%LOCALAPPDATA%\Programs\FlowForge\engram.exe` |
| **Base SQLite de memoria** (engram) | Primer `mem_save` vía MCP | `C:\Users\<vos>\.engram\` (ver abajo) |
| FlowDoc + config por proyecto | `flowforge init <ruta>` | `<proyecto>\docs\`, `<proyecto>\.flowforge.json` |
| Artefactos de feature | `/flow-start` … `/flow-close` | `<proyecto>\.ai-work\{slug}\` |
| Fallback offline (sin MCP) | Agente en runtime | `<proyecto>\.engram\local_memory\*.md` |

> **`flowforge init` no restaura `~/.cursor`.** Si borraste `.cursor` para probar, volvé a ejecutar `flowforge install` (elegí **FlowForge** + tu IDE) o el one-liner del [instalador IDE](README.es.md#instalacion-ide-solo-agentes).

### SQLite de engram — ruta por defecto y cómo cambiarla

Al instalar **engram-dotnet** con el Stack installer, el MCP queda configurado con:

| Variable | Default | Para qué |
|----------|---------|----------|
| `ENGRAM_DATA_DIR` | `C:\Users\<vos>\.engram\` (Windows) · `~/.engram/` (Linux/macOS) | Carpeta donde engram crea la **base SQLite** en el primer save |
| `ENGRAM_USER` | `<usuario>@local.dev` | Namespace personal vs equipo |
| `ENGRAM_SYNC_ENABLED` | `false` (local) o `true` (modo sync) | Sync offline-first con servidor |

El archivo `.db` lo crea **engram-dotnet** dentro de `ENGRAM_DATA_DIR`, no FlowForge. FlowForge solo define la variable de entorno al configurar MCP.

**Para usar otra ubicación de SQLite**, editá la config MCP de tu IDE:

| IDE | Archivo |
|-----|---------|
| **Cursor** | `C:\Users\<vos>\.cursor\mcp.json` → `mcpServers.engram.env.ENGRAM_DATA_DIR` |
| **OpenCode** | `C:\Users\<vos>\.config\opencode\opencode.json` → `mcp.engram.environment.ENGRAM_DATA_DIR` |

Ejemplo (Cursor):

```json
"env": {
  "ENGRAM_DATA_DIR": "D:\\datos\\engram",
  "ENGRAM_USER": "vos@ejemplo.com",
  "ENGRAM_SYNC_ENABLED": "false"
}
```

Reiniciá el IDE después de cambiar variables MCP.

**El nombre de proyecto en memoria** (a qué proyecto pertenecen las observaciones) es aparte — se configura en `<proyecto>/.flowforge.json` bajo `engram.project`, no en la ruta del SQLite.

Ver también: [`docs/10-memory-mapping-fallback.md`](docs/10-memory-mapping-fallback.md) · [`docs/06-engram-sync-convention.md`](docs/06-engram-sync-convention.md).

---

## 3. Primer comando

Recargá el IDE, abrí el **modo Agente**, elegí el orquestador **flowforge** (o asegurate de que las reglas FlowForge estén activas) y enviá:

```
/flow-start CRUD de tareas — endpoints REST para crear, listar, actualizar y borrar tareas. Cada tarea tiene título, descripción, estado (pendiente/en-progreso/completada) y fecha de creación.
```

Los artefactos quedan en `.ai-work/{slug-de-la-feature}/` (por ejemplo `crud-de-tareas/`).

---

## 3. Qué pasa después

```
/flow-start
  ├── forge-discovery  → mapa de contexto, riesgos, trabajo previo
  ├── CKP-0 🔴         → requerimiento vago? PARAR y preguntar al humano
  ├── forge-arch       → escribe spec.md (RF/RNF, GWT, pruebas manuales PM-*)
  ├── CKP-1 🟡         → vos aprobás spec.md
/flow-plan
  ├── forge-plan       → escribe plan.md (checklist ordenado)
  ├── CKP-2 🟡         → vos das luz verde a implementar
/flow-dev
  ├── forge-dev        → código + tests (loop Ralph Wiggum hasta verde)
/flow-verify
  ├── forge-verify     → verify-report.md (PASS o rework_ticket.md)
  ├── CKP-3 🔴         → máximo 3 ciclos de rework, luego escalar
/flow-close
  └── forge-memory     → summary.md si los PM-* están hechos (CKP-4 deploy gate)
```

**Reglas que no cambian en ningún IDE:**

- El **orquestador no implementa código de producto** — delega.
- **¿Reporte de bug?** El orquestador crea `rework_ticket.md` → **forge-dev** lo corrige.
- **Dev terminó** ≠ solo tests en verde: checklist del plan con `[x]` + PM-* manuales del spec + verify PASS antes del cierre.

---

## 4. Comandos

| Comando | Fase |
|---------|------|
| `/flow-start <feature>` | Discovery → Spec (CKP-0, CKP-1) |
| `/flow-plan` | Plan (CKP-2) |
| `/flow-dev` | Implementación |
| `/flow-verify` | Auditoría (CKP-3) |
| `/flow-rework` | Bug → ticket → dev (sin parche inline del orquestador) |
| `/flow-close` | Memoria + deploy gate (CKP-4) |
| `/flow-status` | Solo lectura de `.ai-work/` |

También funciona lenguaje natural (por ejemplo: “reportá un bug”, “seguí codificando”, “cerrá la feature”).

### Comandos vs agentes (no mezclar)

| Vos escribís | El orquestador delega a | Notas |
|--------------|-------------------------|--------|
| `/flow-start` | `forge-discovery` → `forge-arch` | No uses `@forge-arch` solo (salta CKP) |
| `/flow-plan` | `forge-plan` | Requiere `spec.md` aprobado |
| `/flow-dev` | `forge-dev` | Requiere `plan.md` aprobado |
| `/flow-verify` | `forge-verify` | Genera `verify-report.md` |
| `/flow-close` | `forge-memory` | **No** `/forge-memory` — ese nombre es del agente |
| `/flow-rework` | ticket → `forge-dev` | Reporte de bug |
| `/flow-status` | solo orquestador | Lee `.ai-work/` |

Los `/flow-*` son convenciones de texto en modo Agent. Autocomplete requiere `ide/install.ps1`. Escribir `/flow-close` como mensaje funciona igual si las reglas FlowForge están activas.

---

## 5. Próximos pasos

- [`docs/14-flowforge-complete-reference.md`](docs/14-flowforge-complete-reference.md) — 7 casos de prueba hands-on (inglés)
- [`ide/README.md`](ide/README.md) — instalación por IDE y paridad v0.4
- [`docs/18-replicable-demo-definition.md`](docs/18-replicable-demo-definition.md) — runbook replicable
- [`docs/04-roadmap.md`](docs/04-roadmap.md) — roadmap y checklist de release
- [`README.es.md`](README.es.md) — visión general del proyecto en español

---

## Solución de problemas

| Problema | Solución |
|----------|----------|
| **Esperaba wizard pero solo vi salida en consola** | Corriste el [instalador IDE](README.es.md#instalacion-ide-solo-agentes) (`ide/install.ps1`). Para el wizard interactivo, usá el [Stack installer](README.es.md#stack-installer-setup-completo) (`install/install.ps1`). |
| **Stack installer 404 en `releases/latest`** | El repo puede tener solo pre-releases (alpha). Actualizá `install/install.ps1` desde `main`, o: `iwr ... -OutFile $env:TEMP\ff-install.ps1; & $env:TEMP\ff-install.ps1 -Version v0.1.0-alpha.2` |
| **404 al instalar desde `raw.githubusercontent.com`** | Rama o path incorrecto (verificá que la URL apunte a `main` y que el script exista) |
| **No encuentra `git`** | Instalá Git for Windows o el gestor de paquetes de tu SO |
| **`dubious ownership` en Windows** | `git config --global --add safe.directory E:/Proyectos/FlowForge` |
| **El orquestador codea en lugar de delegar** | Recargá el IDE; decí: “Delegá a forge-dev según workflow — no parchees inline” |
| **`/flow-close` no aparece en autocomplete** | Corré `ide/install.ps1 -ProjectPath <repo>`; o escribí `/flow-close` como texto — no es comando nativo de Cursor |
| **Usaste `/forge-memory` por error** | Usá `/flow-close` (comando) — `forge-memory` es el nombre del agente |
| **Hay que cargar `@skills` a mano** | Usá agentes compilados (Cursor) o packs desde `ide/install` |
| **OpenCode no arranca tras instalar FlowForge** | Revisá `~/.local/share/opencode/log/`; no uses texto `file:` con `...` en strings JSON; mergeá solo `agent{}` y conservá `mcp`/`permission` |
| **Subagentes sin modelo en OpenCode** | Configurá proveedor `opencode-go` y API keys, o cambiá `model` en `~/.config/opencode/agents/*.md` (o `.opencode/agents/*.md` por proyecto) a tus modelos disponibles |

> **¿Problemas?** Abrí un issue: https://github.com/efreet111/FlowForge
