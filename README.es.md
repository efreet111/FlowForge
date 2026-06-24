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

## Instalación

**¿No sabes qué instalador usar?**

| Si eres… | Usa |
|----------|-----|
| Nuevo en FlowForge | [Stack installer](#stack-installer-v010-alpha2) — un comando, todo incluido |
| Solo quieres agregar FlowForge a tu IDE | Instalación IDE (one-liner abajo) |
| Integrando en un proyecto existente | Bundle por proyecto (`-ProjectPath`) |
| Contribuyendo a FlowForge | Clone local |

---

**Instalación IDE** (agentes + reglas + comandos en tu IDE actual):

```bash
# Linux/macOS
curl -sSL https://raw.githubusercontent.com/efreet111/FlowForge/main/ide/install.sh | bash

# Windows (PowerShell)
iex ((New-Object System.Net.WebClient).DownloadString('https://raw.githubusercontent.com/efreet111/FlowForge/main/ide/install.ps1'))
```

**Clone local** (para contribuidores):

```bash
git clone https://github.com/efreet111/FlowForge.git
cd FlowForge
bash ide/install.sh          # Linux/macOS
# .\ide\install.ps1          # Windows
```

Por proyecto (Antigravity + `.cursor` en el repo):

```powershell
.\ide\install.ps1 -ProjectPath "E:\ruta\a\tu-proyecto"
```

Después: Reload Window en el IDE, agente **`flowforge`** (o reglas FlowForge), y:

```
/flow-start CRUD de tareas
```

## Stack installer (v0.1.0-alpha.2+)

> **Recomendado para la mayoría de usuarios.** El label `alpha` se refiere al formato de distribución binaria (compilación AOT), no a la estabilidad de la metodología — FlowForge v0.5.0 está probado en producción. El instalador descarga el binario, verifica SHA-256 y lanza un wizard de configuración.

Para instalar FlowForge como herramienta standalone en tu máquina (CLI + backend de memoria + agentes del IDE + estructura FlowDoc), distribuido vía [GitHub Releases](https://github.com/efreet111/FlowForge/releases).

**Linux/macOS:**

```bash
curl -fsSL https://raw.githubusercontent.com/efreet111/FlowForge/main/install/install.sh | bash
```

**Windows (PowerShell):**

```powershell
iwr -useb https://raw.githubusercontent.com/efreet111/FlowForge/main/install/install.ps1 | iex
```

El bootstrap descarga el binario compilado AOT, verifica SHA-256, instala en `~/.local/bin/flowforge` y lanza un wizard interactivo para seleccionar componentes.

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
