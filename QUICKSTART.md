# FlowForge — Quickstart

> **Get started with FlowForge in about 5 minutes.**

🇪🇸 Guía en español: [`QUICKSTART.es.md`](QUICKSTART.es.md) · [`README.es.md`](README.es.md)

---

## 1. Install

FlowForge has **two installers** — pick one:

| | Stack installer (`install/install.*`) | IDE installer (`ide/install.*`) |
|---|--------------------------------------|----------------------------------|
| **Best for** | First-time setup | IDE packs only |
| **UI** | Interactive wizard (pick components + IDEs) | Console log only (no wizard) |
| **Installs** | `flowforge` CLI, optional engram-dotnet, IDE skills (global) | Agents, rules, `/flow-*` commands |
| **Requires** | GitHub Releases download | `git` in PATH (remote mode clones repo) |

Same content as [`README.md` § Install](README.md#install).

> **FlowDoc / per-project scaffolding** is set up separately with `flowforge init <path>` after the Stack installer finishes. See [§ Initialize a project](#initialize-a-project) below.

### Stack installer (full setup, v0.1.0-alpha.2+)

For installing the complete FlowForge stack on your machine — `flowforge` CLI + `engram-dotnet` memory backend + IDE agents + FlowDoc structure.

**Linux/macOS:**

```bash
curl -fsSL https://raw.githubusercontent.com/efreet111/FlowForge/main/install/install.sh | bash
```

**Windows (PowerShell):**

```powershell
iwr -useb "https://raw.githubusercontent.com/efreet111/FlowForge/main/install/install.ps1" -OutFile $env:TEMP\flowforge-install.ps1
powershell -ExecutionPolicy Bypass -File $env:TEMP\flowforge-install.ps1
```

### IDE agents only (lightweight)

For installing just the IDE agent files (no `flowforge` CLI, no engram-dotnet):

**Linux/macOS:**

```bash
curl -sSL https://raw.githubusercontent.com/efreet111/FlowForge/main/ide/install.sh | bash
```

**Windows (PowerShell):**

```powershell
iex ((New-Object System.Net.WebClient).DownloadString('https://raw.githubusercontent.com/efreet111/FlowForge/main/ide/install.ps1'))
```

---

## 2. Initialize a project {#initialize-a-project}

After the Stack installer completes, run `flowforge init` inside each repository where you want FlowForge's full per-project setup:

```powershell
# Windows
flowforge init E:\Proyectos\my-app

# or from inside the project folder
cd E:\Proyectos\my-app
flowforge init .
```

```bash
# Linux/macOS
flowforge init ~/projects/my-app
```

This creates:

| File / folder | Purpose |
|---|---|
| `.flowforge.json` | Project config (activates FlowDoc, sets `docs_framework`, teacher mode, etc.) |
| `AGENTS.md` | Agent guidance for this repo |
| `docs/` | PRD, HUs, ADRs, RFCs, templates |
| `.ai-work/` | Versioned feature artifacts |

**Flags:**

| Flag | Effect |
|------|--------|
| `--no-flow-doc` | Skip `docs/` creation — only `.flowforge.json` + `AGENTS.md` |
| `--yes` / `-y` | Non-interactive (no confirmations) |

> `flowforge init` is the only command that writes into a project directory. The global `flowforge install` only touches `~/.cursor`, `~/.config`, etc.

### FlowDoc on/off and custom paths

FlowForge (methodology + `.ai-work/`) and FlowDoc (`docs/` product layer) are **independent** but designed to work together. Control is in **`.flowforge.json`** at the project root.

| Goal | What to do |
|------|------------|
| **Full setup** (default) | `flowforge init .` — sets `"docs_framework": "flowdoc@1.1"` and default `paths` |
| **FlowForge only** (no FlowDoc) | `flowforge init . --no-flow-doc` — no `docs/`, no `docs_framework`; agents use only `.ai-work/` |
| **Disable later** | Edit `.flowforge.json`: remove `"docs_framework"` or set `"docs_framework": null` |
| **Custom folder layout** | Keep `"docs_framework": "flowdoc@1.1"` and point `paths` to your folders |

Example — custom paths (FlowDoc semantics, your tree):

```json
{
  "docs_framework": "flowdoc@1.1",
  "paths": {
    "prd": "product/requirements.md",
    "backlog": "product/stories",
    "decisions": "product/adr",
    "rfcs": "product/rfc",
    "development": "CONTRIBUTING.md",
    "features": ".ai-work",
    "templates": "product/templates"
  }
}
```

Example — FlowForge only (no product doc layer):

```json
{
  "paths": {
    "features": ".ai-work"
  }
}
```

At `/flow-start`, `forge-discovery` reads this file: if `docs_framework` is missing or `null`, it **skips** the FlowDoc step and proceeds with the FlowForge workflow only.

### Where files live (global vs project)

| What | Command | Default location (Windows) |
|------|---------|----------------------------|
| IDE agents, rules, `/flow-*` commands | `flowforge install` (FlowForge component) | `C:\Users\<you>\.cursor\` |
| FlowForge installer log + config | `flowforge install` | `C:\Users\<you>\.engram\config.json`, `install.log` |
| engram-dotnet binary | `flowforge install` (engram component) | `%LOCALAPPDATA%\Programs\FlowForge\engram.exe` |
| **SQLite memory DB** (engram) | First `mem_save` via MCP | `C:\Users\<you>\.engram\` (see below) |
| Per-project FlowDoc + config | `flowforge init <path>` | `<project>\docs\`, `<project>\.flowforge.json` |
| Per-project feature artifacts | `/flow-start` … `/flow-close` | `<project>\.ai-work\{slug}\` |
| Offline memory fallback (no MCP) | Agent at runtime | `<project>\.engram\local_memory\*.md` |

> **`flowforge init` does not restore `~/.cursor`.** If you deleted `.cursor` to test, run `flowforge install` again (select **FlowForge** + your IDE) or the [IDE installer](README.md#ide-install-agents-only) one-liner.

### engram SQLite — default path and how to change it

When you install **engram-dotnet** via the Stack installer, MCP is configured with:

| Setting | Default | Purpose |
|---------|---------|---------|
| `ENGRAM_DATA_DIR` | `C:\Users\<you>\.engram\` (Windows) · `~/.engram/` (Linux/macOS) | Directory where engram creates its **SQLite database** on first save |
| `ENGRAM_USER` | `<username>@local.dev` | Namespace for personal vs team memories |
| `ENGRAM_SYNC_ENABLED` | `false` (local) or `true` (sync mode) | Offline-first sync to server |

The DB file is created **inside** `ENGRAM_DATA_DIR` by engram-dotnet (not by FlowForge). FlowForge only sets the env var when wiring MCP.

**To use a different SQLite location**, edit the MCP config for your IDE:

| IDE | File to edit |
|-----|----------------|
| **Cursor** | `C:\Users\<you>\.cursor\mcp.json` → `mcpServers.engram.env.ENGRAM_DATA_DIR` |
| **OpenCode** | `C:\Users\<you>\.config\opencode\opencode.json` → `mcp.engram.environment.ENGRAM_DATA_DIR` |

Example (Cursor):

```json
"env": {
  "ENGRAM_DATA_DIR": "D:\\data\\engram",
  "ENGRAM_USER": "you@example.com",
  "ENGRAM_SYNC_ENABLED": "false"
}
```

Restart the IDE after changing MCP env vars.

**Per-project memory name** (which project observations belong to) is separate — set in `<project>/.flowforge.json` under `engram.project`, not in the SQLite path.

See also: [`docs/10-memory-mapping-fallback.md`](docs/10-memory-mapping-fallback.md) (SQLite vs `local_memory` fallback) · [`docs/06-engram-sync-convention.md`](docs/06-engram-sync-convention.md) (sync + env vars).

---

## 3. First command

Reload your IDE, open **Agent mode**, select the **flowforge** orchestrator (or ensure FlowForge rules are active), and send:

```
/flow-start Task CRUD — REST endpoints to create, list, update, and delete tasks. Each task has title, description, status (pending/in-progress/completed), and created-at timestamp.
```

Artifacts land in `.ai-work/{feature-slug}/` (e.g. `task-crud/`).

---

## 4. What happens next

```
/flow-start
  ├── forge-discovery  → context map, risks, prior art
  ├── CKP-0 🔴         → vague requirement? STOP and ask the human
  ├── forge-arch       → writes spec.md (RF/RNF, GWT, PM-* manual tests)
  ├── CKP-1 🟡         → you approve spec.md
/flow-plan
  ├── forge-plan       → writes plan.md (ordered checklist)
  ├── CKP-2 🟡         → you green-light implementation
/flow-dev
  ├── forge-dev        → code + tests (Ralph Wiggum loop until green)
/flow-verify
  ├── forge-verify     → verify-report.md (PASS or rework_ticket.md)
  ├── CKP-3 🔴         → max 3 rework cycles, then escalate
/flow-close
  └── forge-memory     → summary.md if PM-* are done (CKP-4 deploy gate)
```

**Rules that stay the same in every IDE:**

- The **orchestrator does not implement product code** — it delegates.
- **Bug report?** Orchestrator creates `rework_ticket.md` → **forge-dev** fixes it.
- **Dev done** ≠ tests green only: plan checklist `[x]` + manual PM-* in spec + verify PASS before close.

---

## 5. Commands

| Command | Phase |
|---------|--------|
| `/flow-start <feature>` | Discovery → Spec (CKP-0, CKP-1) |
| `/flow-plan` | Plan (CKP-2) |
| `/flow-dev` | Implementation |
| `/flow-verify` | Audit (CKP-3) |
| `/flow-rework` | Bug report → ticket → dev (no orchestrator hotfix) |
| `/flow-close` | Memory + deploy gate (CKP-4) |
| `/flow-status` | Read `.ai-work/` only |

Natural language works too (e.g. “report a bug”, “keep coding”, “close the feature”).

### Commands vs agents (do not mix them up)

| You type | Orchestrator delegates to | Notes |
|----------|---------------------------|--------|
| `/flow-start` | `forge-discovery` → `forge-arch` | Not `@forge-arch` alone (skips CKP-0/1) |
| `/flow-plan` | `forge-plan` | Requires approved `spec.md` |
| `/flow-dev` | `forge-dev` | Requires approved `plan.md` |
| `/flow-verify` | `forge-verify` | Outputs `verify-report.md` |
| `/flow-close` | `forge-memory` | **Not** `/forge-memory` — that name is the agent, not a command |
| `/flow-rework` | creates ticket → `forge-dev` | Bug intake |
| `/flow-status` | orchestrator only | No subagent |

Slash commands are **text conventions** in Agent mode. They appear in autocomplete only after `ide/install.ps1` copies `ide/cursor/commands/` to `.cursor/commands/`. Typing `/flow-close` as a message always works if FlowForge rules are active.

Direct `@forge-*` invocation (legacy) bypasses checkpoints and Memory Curation — prefer `/flow-*` through the orchestrator.

---

## 6. Next steps

- [`docs/14-flowforge-complete-reference.md`](docs/14-flowforge-complete-reference.md) — 7 hands-on test cases
- [`ide/README.md`](ide/README.md) — per-IDE install and parity v0.4
- [`docs/18-replicable-demo-definition.md`](docs/18-replicable-demo-definition.md) — reproducible runbook
- [`docs/04-roadmap.md`](docs/04-roadmap.md) — roadmap and release checklist

---

## Troubleshooting

| Issue | Fix |
|-------|-----|
| **Expected a wizard but got console output only** | You ran the [IDE installer](README.md#ide-install-agents-only) (`ide/install.ps1`). For the interactive wizard, use the [Stack installer](README.md#stack-installer-full-setup) (`install/install.ps1`). |
| **Stack installer 404 on `releases/latest`** | Repo may have only pre-releases (alpha). Update `install/install.ps1` from `main`, or run: `iwr ... -OutFile $env:TEMP\ff-install.ps1; & $env:TEMP\ff-install.ps1 -Version v0.1.0-alpha.2` |
| **404 on `raw.githubusercontent.com` install** | Wrong branch or path (verify the URL points to `main` and the script exists) |
| **`git` not found** | Install Git for Windows or your OS package manager |
| **`dubious ownership` on Windows** | `git config --global --add safe.directory E:/Proyectos/FlowForge` |
| **Orchestrator codes instead of delegating** | Reload IDE; say: “Delegate to forge-dev per workflow — do not patch inline” |
| **`/flow-close` not in autocomplete** | Run `ide/install.ps1 -ProjectPath <repo>`; or type `/flow-close` as plain text — it is not a Cursor built-in |
| **Used `/forge-memory` by mistake** | Use `/flow-close` (command) — `forge-memory` is the agent name, not a slash command |
| **No `@skills` manual load** | Use compiled agents (Cursor) or IDE packs from `ide/install` |

> **Problems?** Open an issue: https://github.com/efreet111/FlowForge
