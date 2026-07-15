# FlowForge — IDE integration files

Ready-to-use packs for **OpenCode, Cursor, Antigravity, and VS Code**.

> Spanish onboarding: [`../QUICKSTART.es.md`](../QUICKSTART.es.md) · English: [`../QUICKSTART.md`](../QUICKSTART.md)

## Cross-IDE parity (v0.4.0)

Shared orchestrator contract (all IDEs must align):

```text
ide/shared/workflow-orchestrator-parity.md
```

| Rule | Cursor | Antigravity | VS Code | OpenCode |
|------|--------|-------------|---------|----------|
| Orchestrator does not code product | `cursor/rules/workflow.mdc` | `antigravity/rules/workflow.md` | `vscode/agents/forge-orchestrator.agent.md` | bundle + shared file |
| Rework → `rework_ticket.md` | ✅ | ✅ + `workflows/flow-rework.md` | ✅ Fix Rework handoff | ✅ in prompt |
| PM-* gate on close | ✅ | ✅ | ✅ | ✅ (skills + parity) |
| `verify-report.md` (not cert-report) | ✅ | ✅ | ✅ | ✅ |
| `.ai-work/{slug}/` kebab-case | ✅ | ✅ | ✅ | ✅ |
| Dev marks plan checklist | compiled agents | on-demand skills | `.github/agents` | `{file:skills}` |

**Recompile Cursor agents** after skill changes:

```bash
python ide/cursor/compile-agents-from-skills.py
```

## IDE matrix (canonical paths)

FlowForge escribe agentes en los mismos destinos que la tabla canónica; la matriz que sigue explica qué rutas maneja cada IDE y qué formato requiere.

| IDE | Global | Proyecto | Formato |
|-----|--------|----------|---------|
| **Cursor** | `~/.cursor/agents/`, `~/.cursor/rules/`, `~/.cursor/commands/` | `.cursor/agents/`, `.cursor/rules/`, `.cursor/commands/` | Markdown para agents y comandos, `.mdc` para reglas. |
| **OpenCode** | `~/.config/opencode/agents/`, `~/.config/opencode/commands/` | `.opencode/agents/` | Markdown + `opencode.json` (`mcp.engram.type = "local"`). El directorio `~/.config/opencode/flowforge/` y `opencode.flowforge.json` son rutas legacy. |
| **GitHub Copilot** | `~/.copilot/agents/*.agent.md`, `~/.copilot/instructions/flowforge.instructions.md` | `.github/agents/*.agent.md`, `.github/copilot-instructions.md` | `*.agent.md`; el archivo de instrucciones incluye el header `applyTo`. Detectado por `github.copilot*`. |
| **Kilo Code** | `~/.config/kilo/agents/*.md` (el mismo bundle de OpenCode) | `.kilo/agents/*.md` (duplicado) | Markdown OpenCode; detectado por `kilocode.*`. |
| **Antigravity** | `~/.gemini/config/` (`AGENTS.md`, `rules/`, `global_workflows/`, `skills/`, `mcp_config.json`) | `.agents/rules/`, `.agents/workflows/`, `.agents/skills/`, `AGENTS.md` | Markdown de reglas + workflows con frontmatter `description:`; no es Claude Desktop. Ver [ADR-011](../docs/decisions/ADR-011-opencode-antigravity-customizations.md). |
| **Claude Desktop** | `~/.config/Claude/claude_desktop_config.json` (config MCP) | — | MCP JSON manual (Anthropic). |

El instalador `flowforge install` detecta estas IDEs y aplica la misma matriz; `docker-pm1-test.sh` verifica las rutas globales para `~/.copilot/agents/`, `~/.config/kilo/agents/` y `~/.gemini/config/` para mantener la paridad Linux. Los scripts `ide/install.sh` / `ide/install.ps1` exponen las mismas carpetas y permiten instalaciones manuales o bundles por proyecto.

`flowforge doctor` reporta `[✓] github.copilot` y `[✓] kilocode.*` junto a los directorios nuevos, y el reporte del doctor es la fuente de verdad para la detección de VS Code. Para más detalles, consultá [`docs/decisions/ADR-008-ide-installer-path-matrix.md`](../docs/decisions/ADR-008-ide-installer-path-matrix.md).

## Quick install (recommended)

### Windows (PowerShell)

```powershell
cd ide
.\install.ps1

# Per project (Antigravity + .cursor + .github/agents):
.\install.ps1 -ProjectPath "E:\Projects\my-app"
```

### Linux / macOS

```bash
cd ide
bash install.sh

# Per project:
bash install.sh /path/to/my-app
```

The installer:

1. Copies `ide/shared/` → `~/.flowforge/shared/` (orchestrator parity)
2. Recompiles Cursor agents from `skills/` (if Python is available)
3. Installs Cursor (`~/.cursor/`), OpenCode (`~/.config/opencode/agents/` + `commands/`), VS Code (Copilot → `~/.copilot/agents/` & instructions, Kilo → `~/.config/kilo/agents/`), and Antigravity (`~/.gemini/config/`)
4. With a project path: `.agents/`, `.cursor/`, `.github/agents/`, `.flowforge/shared/`

### Manual install

**OpenCode:** run `install.sh`; it copies `ide/opencode/agents/*.md` → `~/.config/opencode/agents/` and `ide/opencode/commands/*.md` → `~/.config/opencode/commands/`. Patch your `opencode.json` (type `local`) to include the `mcp.engram` block—do **not** rely on `opencode.flowforge.json` or `~/.config/opencode/flowforge/` as the primary path. Those legacy files exist only to help update older installs; keep your custom `mcp` / `permission` nodes intact and avoid placeholder `file:` entries with ellipsis.

**Cursor:**

```bash
cp ide/cursor/rules/*.mdc ~/.cursor/rules/
cp ide/cursor/agents/*.md  ~/.cursor/agents/
cp ide/cursor/commands/*.md ~/.cursor/commands/
```

**Antigravity (per project):** `bash install.sh <project>` or copy `ide/antigravity/` to `.agents/` (`rules/`, `workflows/`, `AGENTS.md`).

**VS Code:** `install.sh <project>` copies to `.github/agents/` and writes `~/.copilot/instructions/flowforge.instructions.md` plus `.github/copilot-instructions.md`.

## What each IDE gets

| Component | OpenCode | Cursor | Antigravity | VS Code |
|-----------|----------|--------|-------------|---------|
| Workflow orchestrator | `agents/flowforge.md` + MCP in `opencode.json` | `rules/workflow.mdc` | `rules/workflow.md` | copilot-instructions + agents |
| Model assignments | inline in `agents/*.md` frontmatter | `rules/model-assignments.mdc` | `rules/model-assignments.md` | embedded |
| Git safety | `permission.bash` in opencode.json | `rules/git-sin-push.mdc` | `rules/git-sin-push.md` | embedded |
| Agent instructions | compiled `agents/forge-*.md` | `agents/forge-*.md` | via workflow | `.github/agents/*.agent.md` |
| Shared parity doc | `flowforge/shared/...` | ref in workflow | in rules | `{file:../shared/...}` |
| Slash commands | rules / chat | `.cursor/commands/flow-*.md` | `workflows/flow-*.md` | chat + agents |
| MCP (Engram) | native engram MCP | project MCP config | project MCP | project MCP |

## OpenCode detail

FlowForge uses **6 subagents** (`mode: subagent`, hidden) plus optional teacher:

| Subagent | Suggested model | Skills loaded |
|----------|-----------------|---------------|
| `forge-discovery` | fast tier | core + security + compliance + cost |
| `forge-arch` | reasoning tier | core + security + performance + a11y + domain |
| `forge-plan` | reasoning tier | core + security + patterns + migrations + rollback |
| `forge-dev` | coding tier | core + security + solid + testing + performance + refactor |
| `forge-verify` | reasoning tier | core + security + complexity + performance + a11y |
| `forge-memory` | fast tier | core + metrics + changelog + knowledge |
| `forge-teacher` | fast tier | teacher (toggleable) |

Adjust models to your `opencode-go/` provider. After switching to Linux, run a smoke: install bundle → `/flow-start` on an empty project.

## Full methodology

Install files are compilations. All **31 skills** live in `skills/`; see [`docs/14-flowforge-complete-reference.md`](../docs/14-flowforge-complete-reference.md).

Methodology version: see [`VERSION.md`](../VERSION.md).
