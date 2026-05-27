# FlowForge

> **Forge your AI-agent software development workflow.**
>
> 🇪🇸 [README en español](README.es.md) · 🔥 [`QUICKSTART.md`](QUICKSTART.md) — Get started in 5 minutes

FlowForge is an **Agentic SDLC methodology** for small and mid-size teams (2–20 people). It defines how to integrate AI agents into the software lifecycle with **5 formal checkpoints**, **7 agents**, **31 specialized skills**, and a versioned artifact protocol under `.ai-work/{feature-slug}/`.

## Related projects

| Project | Description |
|---------|-------------|
| **FlowForge** (this repo) | Agent-assisted development methodology + IDE packs |
| **[engram-dotnet](https://github.com/efreet111/engram-dotnet)** | Persistent memory engine for AI agents (.NET 10) |

## Install

**Public repo** (one-liner):

```bash
# Linux/macOS
curl -sSL https://raw.githubusercontent.com/efreet111/FlowForge/main/ide/install.sh | bash

# Windows (PowerShell)
iex ((New-Object System.Net.WebClient).DownloadString('https://raw.githubusercontent.com/efreet111/FlowForge/main/ide/install.ps1'))
```

**Private repo** (local install):

```bash
git clone https://github.com/efreet111/FlowForge.git
cd FlowForge
bash ide/install.sh          # Linux/macOS
# .\ide\install.ps1          # Windows
```

**Per-project bundle** (Antigravity + `.cursor` + `.github/agents`):

```powershell
.\ide\install.ps1 -ProjectPath "C:\path\to\your-app"
```

Then: reload your IDE, select the **flowforge** orchestrator (or enable FlowForge rules), and run:

```
/flow-start Task CRUD — create, list, update, delete tasks with title, description, status, timestamps
```

See [`QUICKSTART.md`](QUICKSTART.md) for the full walkthrough.

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
| [`QUICKSTART.md`](QUICKSTART.md) | 5-minute start |
| [`docs/14-flowforge-complete-reference.md`](docs/14-flowforge-complete-reference.md) | Full reference + 7 test cases |
| [`docs/16-ide-integration-plan.md`](docs/16-ide-integration-plan.md) | IDE integration design |
| [`docs/18-replicable-demo-definition.md`](docs/18-replicable-demo-definition.md) | Replicable runbook (no public demo repo required) |
| [`docs/04-roadmap.md`](docs/04-roadmap.md) | Roadmap + release gate |
| [`docs/I18N.md`](docs/I18N.md) | Translation + doc audit tracker |

## Contributing

See [`CONTRIBUTING.md`](CONTRIBUTING.md). Security: [`SECURITY.md`](SECURITY.md).

## License

MIT — see [`LICENSE`](LICENSE).
