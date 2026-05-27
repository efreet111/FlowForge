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
3. Installs Cursor (`~/.cursor`), OpenCode (`~/.config/opencode/flowforge/`), VS Code (`~/.vscode`)
4. With a project path: `.agents/`, `.cursor/`, `.github/agents/`, `.flowforge/shared/`

### Manual install

**OpenCode:** merge `agent{}` from `opencode.flowforge.json` into `~/.config/opencode/opencode.json`. The bundle uses `{file:./flowforge/shared/workflow-orchestrator-parity.md}`.

**Cursor:**

```bash
cp ide/cursor/rules/*.mdc ~/.cursor/rules/
cp ide/cursor/agents/*.md  ~/.cursor/agents/
cp ide/cursor/commands/*.md ~/.cursor/commands/
```

**Antigravity (per project):** `bash install.sh <project>` or copy `ide/antigravity/` to `.agents/`.

**VS Code:** `install.sh <project>` copies to `.github/agents/` and `.vscode/copilot-instructions.md`.

## What each IDE gets

| Component | OpenCode | Cursor | Antigravity | VS Code |
|-----------|----------|--------|-------------|---------|
| Workflow orchestrator | `opencode.flowforge.json` | `rules/workflow.mdc` | `rules/workflow.md` | copilot-instructions + agents |
| Model assignments | inline in agent config | `rules/model-assignments.mdc` | `rules/model-assignments.md` | embedded |
| Git safety | `permission.bash` in opencode.json | `rules/git-sin-push.mdc` | `rules/git-sin-push.md` | embedded |
| Agent instructions | `{file:...}` → `skills/*/SKILL.md` | `agents/forge-*.md` | via workflow | embedded |
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
