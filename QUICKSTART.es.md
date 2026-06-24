# FlowForge — Inicio rápido

> **Empezá con FlowForge en unos 5 minutos.**

🇬🇧 Guía en inglés: [`QUICKSTART.md`](QUICKSTART.md)

---

## 1. Instalación

FlowForge tiene **dos instaladores** — elegí uno:

| | Stack installer (`install/install.*`) | Instalador IDE (`ide/install.*`) |
|---|--------------------------------------|----------------------------------|
| **Ideal para** | Setup inicial completo | Solo packs del IDE |
| **UI** | Wizard interactivo (componentes + IDEs) | Solo log en consola (sin wizard) |
| **Instala** | CLI `flowforge`, engram-dotnet opcional, skills IDE (global) | Agentes, reglas, comandos `/flow-*` |
| **Requiere** | Descarga desde GitHub Releases | `git` en PATH (modo remoto clona el repo) |

Mismo contenido que [`README.es.md` § Instalación](README.es.md#instalacion).

> **FlowDoc / scaffolding por proyecto** se configura por separado con `flowforge init <ruta>` después de que el Stack installer finalice. Ver [§ Inicializar un proyecto](#inicializar-un-proyecto) más abajo.

### Instalador de stack (setup completo, v0.1.0-alpha.2+)

Para instalar el stack completo de FlowForge en tu máquina — CLI `flowforge` + backend de memoria `engram-dotnet` + agentes del IDE + estructura FlowDoc.

**Linux/macOS:**

```bash
curl -fsSL https://raw.githubusercontent.com/efreet111/FlowForge/main/install/install.sh | bash
```

**Windows (PowerShell):**

```powershell
iwr -useb "https://raw.githubusercontent.com/efreet111/FlowForge/main/install/install.ps1" -OutFile $env:TEMP\flowforge-install.ps1
& $env:TEMP\flowforge-install.ps1
```

### Solo agentes del IDE (liviano)

Para instalar solo los archivos de agentes del IDE (sin CLI `flowforge`, sin `engram-dotnet`):

**Linux/macOS:**

```bash
curl -sSL https://raw.githubusercontent.com/efreet111/FlowForge/main/ide/install.sh | bash
```

**Windows (PowerShell):**

```powershell
iex ((New-Object System.Net.WebClient).DownloadString('https://raw.githubusercontent.com/efreet111/FlowForge/main/ide/install.ps1'))
```

---

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
| `--no-flowdoc` | Omite la creación de `docs/` — solo `.flowforge.json` + `AGENTS.md` |
| `--yes` / `-y` | No interactivo (sin confirmaciones) |

> `flowforge init` es el único comando que escribe dentro de un directorio de proyecto. El `flowforge install` global solo toca `~/.cursor`, `~/.config`, etc.

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
| **Subagentes sin modelo en OpenCode** | Configurá proveedor `opencode-go` y API keys, o cambiá `model` en `opencode.flowforge.json` a tus modelos disponibles |

> **¿Problemas?** Abrí un issue: https://github.com/efreet111/FlowForge
