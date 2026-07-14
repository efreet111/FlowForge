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

> **Recomendado para la mayoría de usuarios.** Descarga el CLI `flowforge` (binario AOT), verifica SHA-256 y ejecuta el **wizard de instalación** (`flowforge install --yes`):
> - Descarga `engram-dotnet` (servidor de memoria persistente)
> - Instala agentes FlowForge en los IDEs detectados (Cursor, OpenCode, VS Code)
> - Configura MCP para sync (si `ENGRAM_SERVER_URL` está definido)
> 
> Luego de este paso, ejecutá **`flowforge init <ruta-del-proyecto>`** para configurar FlowDoc.

**Linux/macOS:**

```bash
curl -fsSL https://raw.githubusercontent.com/efreet111/FlowForge/main/install/install.sh | bash
```

**Windows (PowerShell):**

```powershell
iwr -useb "https://raw.githubusercontent.com/efreet111/FlowForge/main/install/install.ps1" -OutFile $env:TEMP\flowforge-install.ps1
powershell -ExecutionPolicy Bypass -File $env:TEMP\flowforge-install.ps1
```

> **¿Ya tenés flowforge?** Ejecutá `flowforge install --yes` para reinstalar. Usá `--yes` en CI/CD, Docker o scripts (modo no interactivo).
>
> En Windows, `& $env:TEMP\flowforge-install.ps1` suele fallar con *la ejecución de scripts está deshabilitada*. Usá siempre `-ExecutionPolicy Bypass -File` como arriba.

Instala en `%LOCALAPPDATA%\Programs\FlowForge\` (Windows) o `~/.local/bin/flowforge` (Linux/macOS). Distribuido vía [GitHub Releases](https://github.com/efreet111/FlowForge/releases).

### Instalación IDE (solo agentes) {#instalacion-ide-solo-agentes}

> **Liviano — sin wizard.** Copia agentes, reglas y comandos `/flow-*` a los IDEs detectados (Cursor, OpenCode, VS Code). Muestra un resumen en consola. **No** instala el CLI `flowforge` ni el binario `engram-dotnet`. Usalo si ya tenés engram configurado o solo necesitás los packs de metodología.

> **OpenCode** ahora genera `opencode.json` completo (provider `opencode-zen`, 8 modelos gratis, `mcp.engram` local) y el doctor valida el schema + PII. El warning de entrenamiento aparece en consola. Más detalles en `docs/opencode-installer.md` y la política de PII en `docs/PII-POLICY.md`.

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

FlowForge copia los packs de agentes a los directorios que cada IDE realmente lee. El siguiente cuadro resume los destinos canónicos y las notas clave de detección.

| IDE | Paquete global | Paquete por proyecto | Notas |
|-----|----------------|---------------------|-------|
| **Cursor** | `~/.cursor/agents/`, `~/.cursor/rules/`, `~/.cursor/commands/` | `.cursor/agents/`, `.cursor/rules/`, `.cursor/commands/` | `flowforge install` y `flowforge init` usan los mismos archivos; MCP en `~/.cursor/mcp.json`. |
| **OpenCode** | `~/.config/opencode/agents/`, `~/.config/opencode/commands/` | `.opencode/agents/` | El `opencode.json` local contiene `mcp.engram` con `type: local`; `opencode.flowforge.json` y `~/.config/opencode/flowforge/` son atajos históricos. |
| **GitHub Copilot** | `~/.copilot/agents/*.agent.md`, `~/.copilot/instructions/flowforge.instructions.md` | `.github/agents/*.agent.md`, `.github/copilot-instructions.md` | Detectado por las extensiones `github.copilot*`; el archivo de instrucciones se normaliza con el header `applyTo`. |
| **Kilo Code** | `~/.config/kilo/agents/*.md` (mismo formato OpenCode) | `.kilo/agents/*.md` (duplicado de `.opencode/agents/`) | Detectado por `kilocode.*`; FlowForge sincroniza el bundle con OpenCode. |
| **Antigravity** | `~/.gemini/config/` (`AGENTS.md`, `rules/`, `workflows/`, `skills/`, `mcp_config.json`) | `.agents/rules/`, `.agents/workflows/`, `.agents/skills/`, `.agents/AGENTS.md` | Antigravity de Google (no Claude Desktop); ver [ADR-009](docs/decisions/ADR-009-opencode-antigravity-customizations.md) |
| **Claude Desktop** | `~/.config/Claude/claude_desktop_config.json` (MCP solamente) | — | Solo MCP manual; FlowForge documenta la ruta pero no copia agentes ni reglas. |

`flowforge install` detecta estas IDEs (Cursor, OpenCode, extensiones VS Code, Antigravity) y aplica este mismo esquema. Los scripts `ide/install.sh` y `ide/install.ps1` exponen los mismos destinos y sirven para refrescar la instalación o generar bundles por proyecto.

`flowforge doctor` ahora informa `[✓] github.copilot` y `[✓] kilocode.*` junto a los nuevos directorios para que veas qué pack se instaló. Consultá [`docs/decisions/ADR-008-ide-installer-path-matrix.md`](docs/decisions/ADR-008-ide-installer-path-matrix.md) para la matriz canónica y la separación entre Antigravity y Claude Desktop.

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

## Solución de problemas

### MCP falla con “SQLite Error 14: unable to open database file”

Probable causa: `flowforge install` se ejecutó con `sudo` y `~/.engram` / binarios quedaron con owner `root:root`. SQLite no puede abrir `~/.engram/engram.db` si el usuario actual no tiene permisos de escritura.

**Solución recomendada:**

```bash
sudo chown -R $USER:$USER ~/.engram ~/.local/bin/engram ~/.local/bin/libe_sqlite3.so ~/.local/bin/flowforge
```

Luego hacé **Reload Window** en tu IDE para reiniciar el MCP y recargar los agentes FlowForge.

### Verificar la instalación con `flowforge doctor`

```bash
flowforge doctor
```

El comando ejecuta 5 chequeos clave (binarios, PATH, MCP y conectividad GitHub) y genera una tabla con [✓] y [✗]. Si algo falla, el detalle indica el siguiente paso.

```
Check               Estado     Detalle
flowforge binary    ✓ OK
engram binary       ✓ OK
engram en PATH      ✓ OK
MCP configurado     ✓ OK
GitHub reachable    ✗ FAIL     Sin conexión: timeout
```

Códigos de salida:

- `0` — todo pasó.
- `1` — error grave (excepción durante la inspección).
- `2` — falla parcial (al menos un chequeo falló).

### Timeouts de instalación configurables

| Variable | Default | Descripción |
|----------|---------|-------------|
| `FLOWFORGE_API_TIMEOUT_SECONDS` | `30` | Timeout para llamadas a la API de GitHub (descubrimiento de versiones, manifest). |
| `FLOWFORGE_DOWNLOAD_TIMEOUT_SECONDS` | `300` | Timeout para descargas de assets (engram, librerías nativas). |
| `FLOWFORGE_YES` | — | Si está definida, `flowforge install` corre en modo no interactivo (como `--yes`) incluso si detecta TTY; útil en CI/Docker/scripts. |

El instalador y el CLI respetan estas variables automáticamente: aumentalas cuando la red sea lenta o estés ejecutando el script piped.

## Licencia

MIT — ver [`LICENSE`](LICENSE)
