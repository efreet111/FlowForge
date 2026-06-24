# FlowForge

> **Forja tu flujo de desarrollo con agentes de IA.**
>
> 🇬🇧 [English README](README.md) · 🔥 [`QUICKSTART.es.md`](QUICKSTART.es.md) ([inglés](QUICKSTART.md)) — Empezá en 5 minutos · 📖 [`GLOSSARY.es.md`](GLOSSARY.es.md) ([inglés](GLOSSARY.md)) — Referencia de términos

**Versión:** [`0.5.0`](VERSION.md) · [Changelog](CHANGELOG.md)

FlowForge es una **metodología Agentic SDLC** diseñada para equipos pequeños y medianos (SMB, 2–20 personas). Define cómo integrar agentes de IA en el ciclo de desarrollo con 5 checkpoints formales, 7 agentes, 31 skills especializadas y un protocolo de artefactos versionados.

## Repositorios

| Proyecto | Descripción |
|----------|-------------|
| **FlowForge** (este) | Metodología de desarrollo asistida por agentes |
| **[engram-dotnet](https://github.com/efreet111/engram-dotnet)** | Motor de memoria persistente para agentes (.NET 10) |

## Instalación {#instalacion}

**¿No sabes qué instalador usar?** Ver también [`QUICKSTART.es.md`](QUICKSTART.es.md) — mismos dos caminos, paso a paso.

| Si eres… | Usa |
|----------|-----|
| Nuevo en FlowForge | [Stack installer](#stack-installer-setup-completo) — wizard interactivo, stack completo |
| Solo quieres agentes en tu IDE (sin CLI, sin binario engram) | [Instalación IDE](#instalacion-ide-solo-agentes) — one-liner, salida en consola |
| Integrando en un proyecto existente | [Bundle por proyecto](#bundle-por-proyecto) (`-ProjectPath`) |
| Contribuyendo a FlowForge | [Clone local](#clone-local) |

---

### Stack installer (setup completo) {#stack-installer-setup-completo}

> **Recomendado para la mayoría de usuarios.** Descarga el CLI `flowforge` (binario AOT), verifica SHA-256 y lanza un **wizard interactivo**: elegís engram-dotnet, skills FlowForge, IDEs destino, modo local vs sync, y confirmás. El label `alpha` se refiere al formato binario, no a la estabilidad de la metodología — FlowForge v0.5.0 está probado en producción.
>
> Luego de este paso, ejecutá **`flowforge init <ruta-del-proyecto>`** para configurar FlowDoc y archivos por proyecto en cada repositorio.

**Linux/macOS:**

```bash
curl -fsSL https://raw.githubusercontent.com/efreet111/FlowForge/main/install/install.sh | bash
```

**Windows (PowerShell):**

```powershell
iwr -useb "https://raw.githubusercontent.com/efreet111/FlowForge/main/install/install.ps1" -OutFile $env:TEMP\flowforge-install.ps1
& $env:TEMP\flowforge-install.ps1
```

> En Windows, evitá `| iex` — puede ejecutar una copia en caché. Guardar en archivo primero asegura el script actual de `main`.

Instala en `%LOCALAPPDATA%\Programs\FlowForge\` (Windows) o `~/.local/bin/flowforge` (Linux/macOS). Distribuido vía [GitHub Releases](https://github.com/efreet111/FlowForge/releases).

### Instalación IDE (solo agentes) {#instalacion-ide-solo-agentes}

> **Liviano — sin wizard.** Copia agentes, reglas y comandos `/flow-*` a los IDEs detectados (Cursor, OpenCode, VS Code). Muestra un resumen en consola. **No** instala el CLI `flowforge` ni el binario `engram-dotnet`. Usalo si ya tenés engram configurado o solo necesitás los packs de metodología.

**Linux/macOS:**

```bash
curl -sSL https://raw.githubusercontent.com/efreet111/FlowForge/main/ide/install.sh | bash
```

**Windows (PowerShell):**

```powershell
iex ((New-Object System.Net.WebClient).DownloadString('https://raw.githubusercontent.com/efreet111/FlowForge/main/ide/install.ps1'))
```

### Clone local {#clone-local}

```bash
git clone https://github.com/efreet111/FlowForge.git
cd FlowForge
bash ide/install.sh          # Linux/macOS
# .\ide\install.ps1          # Windows
```

### Inicializar un proyecto (FlowDoc + AGENTS.md) {#inicializar-un-proyecto}

Después de `flowforge install`, ejecutá `flowforge init` una vez por repositorio:

```powershell
# Windows
flowforge init E:\Proyectos\mi-app
```

```bash
# Linux/macOS
flowforge init ~/projects/mi-app
```

Crea `.flowforge.json`, `AGENTS.md`, `docs/` (PRD, HUs, ADRs, templates) y `.ai-work/` dentro del proyecto. Usá `--no-flow-doc` para omitir la estructura `docs/` y crear solo la config y los archivos de agentes.

### Bundle por proyecto {#bundle-por-proyecto}

Antigravity + `.cursor` del proyecto + `.github/agents`:

```powershell
.\ide\install.ps1 -ProjectPath "E:\ruta\a\tu-proyecto"
```

Después: Reload Window en el IDE, agente **`flowforge`** (o reglas FlowForge), y:

```
/flow-start CRUD de tareas
```

Guía completa: [`QUICKSTART.es.md`](QUICKSTART.es.md).

## Metodología

```
FASE 0: DISCOVERY ──── CKP-0 🔴 HARD STOP
FASE 1: INTENCIÓN ──── CKP-1 🟡 spec.md (humano aprueba)
FASE 2: ARQUITECTURA ─ CKP-2 🟡 plan.md (humano aprueba)
FASE 3: EJECUCIÓN ──── Inner loop + CKP-3 🔴 (máx. 3 reworks)
FASE 4: CIERRE ─────── CKP-4 🟢 deploy gate
```

- **7 agentes**: Orchestrator, Discovery, Arch, Plan, Dev, Verify, Memory
- **31 skills** (7 core + especializadas + teacher)
- **Orquestador no codea**: delega; bugs → `rework_ticket.md` → forge-dev

## Integración IDE

| IDE | Ubicación |
|-----|-----------|
| **Cursor** | `ide/cursor/` → `~/.cursor/` o `.cursor/` en el proyecto |
| **VS Code** | `ide/vscode/` → `.github/agents/` |
| **Antigravity** | `ide/antigravity/` → `.agents/` |
| **OpenCode** | `ide/opencode/opencode.flowforge.json` |

Detalle: [`ide/README.md`](ide/README.md)

## Documentación

| Documento | Descripción |
|-----------|-------------|
| [`QUICKSTART.es.md`](QUICKSTART.es.md) | Inicio en 5 minutos |
| [`examples/crud-tareas/`](../examples/crud-tareas/) | Artefactos de ejemplo (spec, plan, verify) |
| [`QUICKSTART.md`](QUICKSTART.md) | Quickstart (English) |
| [`docs/14-flowforge-complete-reference.md`](docs/14-flowforge-complete-reference.md) | Referencia + casos de prueba |
| [`docs/18-replicable-demo-definition.md`](docs/18-replicable-demo-definition.md) | Runbook replicable (sin repo demo obligatorio) |
| [`docs/04-roadmap.md`](docs/04-roadmap.md) | Roadmap y release gate |

## Licencia

MIT — ver [`LICENSE`](LICENSE)
