# FlowForge

> **Forge your AI-agent software development workflow.**
>
> 🇪🇸 [README en español](README.es.md) · 🔥 [`QUICKSTART.md`](QUICKSTART.md) ([español](QUICKSTART.es.md)) — Get started in 5 minutes · 📖 [`GLOSSARY.md`](GLOSSARY.md) ([español](GLOSSARY.es.md)) — Terminology reference

**Version:** [`0.5.0`](VERSION.md) · [Changelog](CHANGELOG.md)

FlowForge is an **Agentic SDLC methodology** for small and mid-size teams (2–20 people). It defines how to integrate AI agents into the software lifecycle with **5 formal checkpoints**, **7 agents**, **31 specialized skills**, and a versioned artifact protocol under `.ai-work/{feature-slug}/`.

## Related projects

| Project | Description |
|---------|-------------|
| **FlowForge** (this repo) | Agent-assisted development methodology + IDE packs |
| **[engram-dotnet](https://github.com/efreet111/engram-dotnet)** | Persistent memory engine for AI agents (.NET 10) |

## Install {#install}

**Not sure which install to use?** See also [`QUICKSTART.md`](QUICKSTART.md) — same two paths, step-by-step.

| If you are… | Use |
|-------------|-----|
| Trying FlowForge for the first time | [Stack installer](#stack-installer-full-setup) — interactive wizard, full stack |
| Adding FlowForge to your IDE only (no CLI, no engram binary) | [IDE install](#ide-install-agents-only) — one-liner, console output |
| Integrating into an existing project | [Per-project bundle](#per-project-bundle) (`-ProjectPath`) |
| Contributing to FlowForge | [Local clone](#local-clone) |

---

### Stack installer (full setup) {#stack-installer-full-setup}

> **Recommended for most users.** Downloads the `flowforge` CLI (AOT binary), verifies SHA-256, and launches the **install wizard** (`flowforge install --yes`):
> - Downloads `engram-dotnet` (persistent memory server)
> - Installs FlowForge IDE agents (skills, rules, `/flow-*` commands)
> - Auto-detects installed IDEs (Cursor, OpenCode, VS Code)
> - Configures MCP for sync (if `ENGRAM_SERVER_URL` is set)
> 
> After this step, run **`flowforge init <project-path>`** to set up FlowDoc and per-project files.

**Linux/macOS:**

```bash
curl -fsSL https://raw.githubusercontent.com/efreet111/FlowForge/main/install/install.sh | bash
```

**Windows (PowerShell):**

```powershell
iwr -useb "https://raw.githubusercontent.com/efreet111/FlowForge/main/install/install.ps1" -OutFile $env:TEMP\flowforge-install.ps1
powershell -ExecutionPolicy Bypass -File $env:TEMP\flowforge-install.ps1
```

> **Already have flowforge?** Run `flowforge install --yes` directly to re-run the wizard in non-interactive mode. Use `--yes` for CI/CD, Docker, or scripts.
>
> On Windows, `& $env:TEMP\flowforge-install.ps1` often fails with *execution of scripts is disabled*. Always use `-ExecutionPolicy Bypass -File` as above.

Installs to `%LOCALAPPDATA%\Programs\FlowForge\` (Windows) or `~/.local/bin/flowforge` (Linux/macOS). Distributed via [GitHub Releases](https://github.com/efreet111/FlowForge/releases).

### IDE install (agents only) {#ide-install-agents-only}

> **Lightweight — no wizard.** Copies agents, rules, and `/flow-*` commands into detected IDEs (Cursor, OpenCode, VS Code). Prints a console summary. Does **not** install the `flowforge` CLI or `engram-dotnet` binary. Use this if you already have engram configured or only need the methodology packs.

**Linux/macOS:**

```bash
curl -sSL https://raw.githubusercontent.com/efreet111/FlowForge/main/ide/install.sh | bash
```

**Windows (PowerShell):**

```powershell
iex ((New-Object System.Net.WebClient).DownloadString('https://raw.githubusercontent.com/efreet111/FlowForge/main/ide/install.ps1'))
```

### Local clone {#local-clone}

```bash
git clone https://github.com/efreet111/FlowForge.git
cd FlowForge
bash ide/install.sh          # Linux/macOS
# .\ide\install.ps1          # Windows
```

### Initialize a project (FlowDoc + AGENTS.md) {#initialize-a-project}

After `flowforge install`, run `flowforge init` once per repository:

```powershell
# Windows
flowforge init E:\Proyectos\my-app
```

```bash
# Linux/macOS
flowforge init ~/projects/my-app
```

Creates `.flowforge.json`, `AGENTS.md`, `docs/` (PRD, HUs, ADRs, templates) and `.ai-work/` inside the project. Use `--no-flow-doc` to skip the `docs/` structure and only create the config and agent files.

### Per-project bundle {#per-project-bundle}

Antigravity + project `.cursor` + `.github/agents`:

```powershell
.\ide\install.ps1 -ProjectPath "C:\path\to\your-app"
```

Then: reload your IDE, select the **flowforge** orchestrator (or enable FlowForge rules), and run:

```
/flow-start Task CRUD — create, list, update, delete tasks with title, description, status, timestamps
```

Full walkthrough: [`QUICKSTART.md`](QUICKSTART.md).

## Methodology (5 phases, 5 checkpoints)

```
PHASE 0: DISCOVERY  ── CKP-0 🔴 HARD STOP (vague requirement → stop)
PHASE 1: INTENT     ── CKP-1 🟡 spec.md (human approves)
PHASE 2: PLAN       ── CKP-2 🟡 plan.md (human green-lights)
PHASE 3: EXECUTION  ── inner loop + CKP-3 🔴 (max 3 rework cycles)
PHASE 4: CLOSE      ── CKP-4 🟢 deploy gate (human decides)
```

| Principle | Detail |
|-----------|--------|
| **7 agents** | Orchestrator, Discovery, Arch, Plan, Dev, Verify, Memory |
| **31 skills** | 7 core + specialized (security, SOLID, performance, …) + optional teacher |
| **Orchestrator coordinates only** | Does not patch product code; bug reports → `rework_ticket.md` → **forge-dev** |
| **Same flow, any IDE** | Cursor, VS Code, Antigravity, OpenCode — see [`ide/README.md`](ide/README.md) |

## IDE integration

FlowForge writes agent packs to the directories each IDE actually reads so that the `flowforge` installer, the shell scripts, and `flowforge init` all keep the global and project layouts in sync. The table below summarizes the canonical destinations and notes the VS Code detection strategy.

| IDE | Global agents | Project agents | Notes |
|-----|---------------|----------------|-------|
| **Cursor** | `~/.cursor/agents/`, `~/.cursor/rules/`, `~/.cursor/commands/` | `.cursor/agents/`, `.cursor/rules/`, `.cursor/commands/` | `flowforge install` / `flowforge init` copy the same files; MCP written to `~/.cursor/mcp.json` |
| **OpenCode** | `~/.config/opencode/agents/`, `~/.config/opencode/commands/` | `.opencode/agents/` | `opencode.json` (or `.jsonc`) holds `mcp.engram` with `type: local`; `opencode.flowforge.json` and `~/.config/opencode/flowforge/` are legacy helpers only |
| **GitHub Copilot** | `~/.copilot/agents/*.agent.md`, `~/.copilot/instructions/flowforge.instructions.md` | `.github/agents/*.agent.md`, `.github/copilot-instructions.md` | Detected via extensions `github.copilot*`; instructions file is normalized with the `applyTo` header |
| **Kilo Code** | `~/.config/kilo/agents/*.md` (same format as OpenCode) | `.kilo/agents/*.md` (duplicated from `.opencode/agents/`) | Detected via extensions `kilocode.*`; FlowForge keeps the directories in sync with OpenCode bundles |
| **Antigravity** | `~/.gemini/config/` (`AGENTS.md`, `rules/`, `workflows/`, `skills/`, `mcp_config.json`) | `.agents/rules/`, `.agents/workflows/`, `.agents/skills/`, `AGENTS.md` | Google Antigravity (not Claude Desktop); global install mirrors project bundle. See [ADR-009](docs/decisions/ADR-009-opencode-antigravity-customizations.md). |
| **Claude Desktop** | `~/.config/Claude/claude_desktop_config.json` (MCP only) | — | Anthropic MCP config only; FlowForge documents it but does not copy agents or rules |

`flowforge install` automatically detects the IDEs listed above (Cursor, OpenCode, VS Code extensions, Antigravity) and installs the matching rows from this matrix. The bootstrap scripts (`ide/install.sh`, `ide/install.ps1`) expose the same destinations and can be used for manual refreshes or per-project bundles when you prefer shell wizards.

`flowforge doctor` now reports `[✓] github.copilot` and `[✓] kilocode.*` alongside the new directories so you can see which VS Code pack ran. See [`docs/decisions/ADR-008-ide-installer-path-matrix.md`](docs/decisions/ADR-008-ide-installer-path-matrix.md) for the canonical matrix and the rationale for separating Antigravity from Claude Desktop.

Shared orchestrator contract: [`ide/shared/workflow-orchestrator-parity.md`](ide/shared/workflow-orchestrator-parity.md)

## Skills overview (31)

| Wave | Count | Focus |
|------|-------|--------|
| Core | 7 | Base flow (orchestrator → memory) |
| OLA 1 | 5 | Security + SOLID |
| OLA 2 | 5 | Quality + patterns |
| OLA 3 | 8 | Infra (CVE, compliance, DDD, migrations, …) |
| OLA 4 | 5 | Metrics, changelog, knowledge graph |
| Cross | 1 | forge-teacher (toggleable) |

## engram-dotnet status

7 shipped features, **258 tests** (verification, promotion, traceability, TTL, doctor, offline sync, advanced integration). Optional MCP memory — methodology works without it.

## Documentation

| Doc | Purpose |
|-----|---------|
| [`QUICKSTART.md`](QUICKSTART.md) / [`QUICKSTART.es.md`](QUICKSTART.es.md) | 5-minute start (EN / ES) |
| [`examples/crud-tareas/`](examples/crud-tareas/) | Sample spec / plan / verify-report |
| [`docs/14-flowforge-complete-reference.md`](docs/14-flowforge-complete-reference.md) | Full reference + 7 hands-on test cases |
| [`docs/06-ai-orchestrator.md`](docs/06-ai-orchestrator.md) | Orchestrator / traffic light |
| [`docs/11-orchestrator-delegation-protocol.md`](docs/11-orchestrator-delegation-protocol.md) | Delegation protocol |
| [`docs/16-ide-integration-plan.md`](docs/16-ide-integration-plan.md) | IDE integration design |
| [`docs/18-replicable-demo-definition.md`](docs/18-replicable-demo-definition.md) | Replicable runbook (no public demo repo required) |
| [`docs/04-roadmap.md`](docs/04-roadmap.md) | Roadmap + release gate |
| [`docs/I18N.md`](docs/I18N.md) | Translation + doc audit tracker |

## Troubleshooting

### MCP fails with "SQLite Error 14: unable to open database file"

Causa probable: `flowforge install` se ejecutó con `sudo`, así que el directorio `~/.engram` y los binarios quedaron con owner `root:root`. SQLite no puede abrir `~/.engram/engram.db` si el usuario actual no tiene permisos.

**Solución rápida:**

```bash
sudo chown -R $USER:$USER ~/.engram ~/.local/bin/engram ~/.local/bin/libe_sqlite3.so ~/.local/bin/flowforge
```

Luego hacé **Reload Window** en Cursor o VS Code para reiniciar el MCP y volver a cargar los agentes.

### Verify the installation with `flowforge doctor`

```bash
flowforge doctor
```

El comando revisa 5 elementos esenciales (binarios, PATH, MCP y conectividad GitHub) y muestra una tabla con [✓] y [✗]. Si algo falla, el detalle sugiere el próximo paso.

Sample output:

```
Check               Estado     Detalle
flowforge binary    ✓ OK
engram binary       ✓ OK
engram en PATH      ✓ OK
MCP configurado     ✓ OK
GitHub reachable    ✗ FAIL     Sin conexión: timeout
```

Exit codes:

- `0` — all checks pass.
- `1` — hard error (exception while gathering data).
- `2` — partial failure (some checks failed).

### Installation timeouts are configurable

| Variable | Default | Description |
|----------|---------|-------------|
| `FLOWFORGE_API_TIMEOUT_SECONDS` | `30` | Timeout for GitHub API calls (version discovery, manifest, release metadata). |
| `FLOWFORGE_DOWNLOAD_TIMEOUT_SECONDS` | `300` | Timeout for downloading release assets (engram binary, native libs). |
| `FLOWFORGE_YES` | — | Set this to force `flowforge install` into headless mode — equivalent to `--yes`, even when a TTY is detected (useful in CI, Docker, scripts). |

The installer and the `flowforge` CLI respect these env vars automatically; bump them when the network is slow or when running inside automation that pipes the script.

## Contributing

See [`CONTRIBUTING.md`](CONTRIBUTING.md). Security: [`SECURITY.md`](SECURITY.md).

## License

MIT — see [`LICENSE`](LICENSE).
