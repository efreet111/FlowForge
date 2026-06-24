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

> **Recommended for most users.** Downloads the `flowforge` CLI (AOT binary), verifies SHA-256, and launches an **interactive wizard**: choose engram-dotnet, FlowForge IDE skills, target IDEs, local vs sync mode, then confirm. The `alpha` label refers to the binary distribution format, not methodology stability — FlowForge v0.5.0 is production-tested.
>
> After this step, run **`flowforge init <project-path>`** to set up FlowDoc and per-project files in each repository.

**Linux/macOS:**

```bash
curl -fsSL https://raw.githubusercontent.com/efreet111/FlowForge/main/install/install.sh | bash
```

**Windows (PowerShell):**

```powershell
iwr -useb "https://raw.githubusercontent.com/efreet111/FlowForge/main/install/install.ps1" -OutFile $env:TEMP\flowforge-install.ps1
& $env:TEMP\flowforge-install.ps1
```

> Avoid `| iex` on Windows — it can run a cached copy of the script. Saving to a file first always fetches the latest `main`.

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

| IDE | Pack location |
|-----|----------------|
| **Cursor** | `ide/cursor/` → `~/.cursor/` or project `.cursor/` |
| **VS Code** | `ide/vscode/` → `.github/agents/` + `.vscode/copilot-instructions.md` |
| **Antigravity** | `ide/antigravity/` → `.agents/rules` + `.agents/workflows` |
| **OpenCode** | `ide/opencode/opencode.flowforge.json` (merge into your config) |

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

## Contributing

See [`CONTRIBUTING.md`](CONTRIBUTING.md). Security: [`SECURITY.md`](SECURITY.md).

## License

MIT — see [`LICENSE`](LICENSE).
