# FlowForge for IDEs: Cursor, Antigravity, VS Code/Copilot, OpenCode, and Kilo

> **Version**: 1.2 — FlowForge v0.5.0 IDE integration
> **Date**: 2026-11-06 (originally based on Cursor Enterprise Agents v0.0.5, 2026-05-27)
> **Status**: Active. IDE assets live under `ide/`; parity doc in `ide/shared/`.

---

## Part 1: What we learned from Cursor Enterprise Agents

We reviewed `cursor-enterprise-agents` v0.0.5 and mapped patterns directly to FlowForge:

### What we already have (matches)

| FlowForge | Cursor EA | Status |
|-----------|-----------|--------|
| `flowforge` (CKP table, delegation) | `workflow.mdc` (orchestrator + commands) | ✅ Direct parallel |
| `forge-discovery` + `forge-arch` | `spec-writer` + `project-explorer` | ✅ Same idea |
| `forge-plan` (patterns, security, rollback) | `solution-architect` | ✅ FlowForge is richer |
| `forge-dev` (6 skills) | `dev-agent` (monolithic skill) | ✅ More granular |
| `forge-verify` (5 skills) | `verifier-agent` | ✅ More audit dimensions |
| `forge-memory` (5 skills) | `hu-closer` | ✅ Persist + metrics |
| `forge-teacher` | `user-liaison` (`/explain-more`) | ✅ Toggleable |
| Model table | `model-assignments.mdc` | ✅ In `ide/cursor/rules/` |
| `docs/14` (test cases) | `GUIA-USO.md` + `BEST-PRACTICES.md` | ✅ Documented |

### Gaps we closed in v0.4–v0.5

| Gap | FlowForge solution | Status |
|-----|-------------------|--------|
| Git safety (no push unless asked) | `git-sin-push.mdc` / parity doc | ✅ |
| IDE install automation | `ide/install.ps1`, `ide/install.sh`, `flowforge install` | ✅ |
| Orchestrator parity across IDEs | `ide/shared/workflow-orchestrator-parity.md` | ✅ |
| `/flow-plan`, `/flow-rework` commands | Cursor commands + Antigravity skills (legacy workflows dead since 2026-10-19) | ✅ |
| OpenCode bundle | `~/.config/opencode/agents/` + `commands/` (legacy: `opencode.flowforge.json`) | ✅ |
| VS Code slash commands | `ide/vscode/prompts/*.prompt.md` (HU-040 / ADR-027) | ✅ |
| Antigravity contract | `trigger:` rules, `/flow-*` as skills, 7 native subagents (HU-050 / ADR-029) | ✅ |
| OpenCode V2 config | `permissions[]`, `mcp.servers`, `agents`/`providers` (HU-043/HU-046) | ✅ |
| Copilot + Kilo destinations | `~/.copilot/agents/`, `~/.config/kilo/agents/` (+ project mirrors) | ✅ |
| context7 MCP for remote Engram | auto-install on non-local profiles (HU-042) | ✅ |

### Remaining improvements

| Gap | Cursor EA pattern | Priority |
|-----|-------------------|----------|
| Stack-specific skill references | `verifier-security-gaps/references/stack-*.md` | 🟡 Partial — only `skills/forge-plan/migrations/references/stack-postgres.md` |
| Auto-bootstrap of rules | Section in `workflow.mdc` | 🟡 Partial — the pre-flight rule is in the parity doc and installer, not generated from skills |
| Compiled Cursor agents drift | `compile-agents-from-skills.py` output is not synced with `skills/` | 🟡 Important — regenerate in a dedicated commit |
| Danger-zone checkpoint doc | `human-in-the-loop-checkpoints.md` §2 | 🟢 Nice to have |
| Natural-language intent signals | Signal table in workflow | ✅ Done — `.agents/rules/workflow.md` has the Intent table |

---

## Part 2: IDE integration model

### Principle: FlowForge is IDE-agnostic

Skills are Markdown files. Each IDE has its own rules and agent format. We **translate** skills into each IDE — we do not fork the methodology per IDE.

```
skills/ (canonical — 35 SKILL.md files)
  │
  ├──→ ide/cursor/      ← .mdc rules + agents/*.md + commands/flow-*.md
  ├──→ ide/antigravity/ ← .agents/rules/ + workflows/
  ├──→ ide/vscode/      ← copilot-instructions.md + .github/agents/
  └──→ ide/opencode/    ← agents/*.md + commands/*.md → ~/.config/opencode/
```

Canonical orchestrator behavior: [`ide/shared/workflow-orchestrator-parity.md`](../ide/shared/workflow-orchestrator-parity.md).

### Format per IDE

| IDE | Rules | Agents | Workflows / commands | Install |
|-----|-------|--------|----------------------|---------|
| **Cursor** | `.cursor/rules/*.mdc` | `.cursor/agents/*.md` | `ide/cursor/commands/flow-*.md` | `ide/install.ps1` / `install.sh` |
| **Antigravity** | `.agents/rules/*.md` (`trigger:`) | native subagents `.agents/agents/*.md` + skills `.agents/skills/` | skills `.agents/skills/flow-*/SKILL.md` (workflows legacy dead since 2026-10-19) | install script → project |
| **VS Code / Copilot** | `.github/copilot-instructions.md` or `.vscode/` | `.github/agents/*.agent.md` | `ide/vscode/prompts/flow-*.prompt.md` (slash commands) | install script → `~/.config/Code/User/prompts/` (Flatpak-aware) |
| **OpenCode** | MCP in `opencode.json` (`type: local`) | `agents/*.md` (orchestrator: `flowforge.md`) | `commands/*.md` | install script → `~/.config/opencode/agents/` + `commands/` |
| **Kilo Code** | OpenCode agent bundle | `.kilo/agents/*.md` | `.kilo/agents/*.md` | `flowforge install` / `ide/install.sh` |

### IDE path matrix

FlowForge writes agent packs to the directories each IDE actually reads. The matrix below shows the canonical global and project destinations plus the detection signals used by the installer.

| IDE | Global agents | Project agents | Detection |
|-----|---------------|----------------|-----------|
| **Cursor** | `~/.cursor/agents/`, `~/.cursor/rules/`, `~/.cursor/commands/` | `.cursor/agents/`, `.cursor/rules/`, `.cursor/commands/` | Presence of `~/.cursor` |
| **OpenCode** | `~/.config/opencode/agents/`, `~/.config/opencode/commands/` | `.opencode/agents/`, `.kilo/agents/` (mirrored) | Presence of `~/.config/opencode` |
| **GitHub Copilot** | `~/.copilot/agents/*.agent.md`, `~/.copilot/instructions/flowforge.instructions.md`, `~/.config/Code/User/prompts/*.prompt.md` (or `~/.var/app/com.visualstudio.code/config/Code/User/prompts/` on Flatpak) | `.github/agents/*.agent.md`, `.github/copilot-instructions.md`, `.github/prompts/*.prompt.md` | VS Code extension `github.copilot*` |
| **Kilo Code** | `~/.config/kilo/agents/*.md` (same bundle as OpenCode) | `.kilo/agents/*.md` (duplicated) | VS Code extension `kilocode.*` |
| **Antigravity** | `~/.gemini/config/` (`AGENTS.md`, `rules/`, `agents/`, `skills/`, `GEMINI.md`, `mcp_config.json`) | `.agents/rules/`, `.agents/agents/`, `.agents/skills/`, `AGENTS.md` | `~/.gemini` — ver [ADR-019](decisions/ADR-019-opencode-antigravity-customizations.md) y [ADR-029](decisions/ADR-029-antigravity-customizations-contract.md) |
| **Claude Desktop** | `~/.config/Claude/claude_desktop_config.json` (MCP only) | — | `%APPDATA%\Claude` / `~/.config/Claude/` |

This matrix is the reference for `flowforge install`, `ide/install.sh`, `ide/install.ps1`, and the CI checks. See [`ADR-008`](decisions/ADR-008-ide-installer-path-matrix.md) for the rationale and detection logic.

> **Copilot/Kilo**: the same VS Code pack drives both. Copilot gets `*.agent.md` + `prompts/`; Kilo gets the OpenCode `agents/*.md` bundle. The installer installs both when it cannot detect which extension is present.

### Files per IDE (current layout)

**Cursor** (`ide/cursor/`):

```
ide/cursor/
├── rules/
│   ├── workflow.mdc
│   ├── model-assignments.mdc
│   └── git-sin-push.mdc
├── agents/
│   └── forge-*.md   ← orchestrator compiled as `forge-orchestrator.md` (legacy filename)
├── commands/
│   └── flow-start.md, flow-plan.md, flow-dev.md, flow-verify.md, flow-rework.md, flow-close.md, ...
├── config/
│   └── agent-models.json
└── compile-agents-from-skills.py
```

**Antigravity** (`ide/antigravity/`):

```
ide/antigravity/
├── rules/       ← trigger: always_on | model_decision | glob | manual
├── agents/      ← 7 native subagents (flowforge, forge-discovery, ...)
├── workflows/   ← source only: converted to skills/flow-*/SKILL.md on install
├── config/      ← agent-models.json
└── AGENTS.md
```

**VS Code** (`ide/vscode/`):

```
ide/vscode/
├── copilot-instructions.md
├── agents/
│   ├── forge-orchestrator.agent.md
│   ├── forge-discovery.agent.md
│   ├── forge-arch.agent.md
│   ├── forge-plan.agent.md
│   ├── forge-dev.agent.md
│   ├── forge-verify.agent.md
│   └── forge-memory.agent.md
├── config/
│   └── agent-models.json
└── prompts/   ← slash commands for the Copilot Chat picker (HU-040)
    ├── flow-start.prompt.md   (agent: forge-orchestrator)
    ├── flow-plan.prompt.md    (agent: forge-orchestrator)
    ├── flow-dev.prompt.md     (agent: forge-dev)
    ├── flow-verify.prompt.md  (agent: forge-verify)
    ├── flow-rework.prompt.md  (agent: forge-orchestrator)
    ├── flow-status.prompt.md  (agent: forge-orchestrator)
    └── flow-close.prompt.md   (agent: forge-memory)
```

**OpenCode** (`ide/opencode/`):

```
ide/opencode/
├── agents/          ← flowforge.md + forge-*.md
├── commands/        ← flow-*.md + fforge-init.md
├── config/          ← agent-models.json
├── templates/       ← opencode.json.tpl, config.schema.json, managed-paths.json, instructions.md
├── lib/             ← atomic-write.sh, install-log.sh, pii-scan.sh, sudo-guard.sh
├── generate-config.sh
└── AGENTS.md
```

---

## Part 3: IDE file architecture

### 3.1 Cursor: `workflow.mdc`

The most important file. Compiles the FlowForge orchestrator (`flowforge`) for Cursor; the compiled file is still named `forge-orchestrator.md` for compatibility.

```markdown
---
alwaysApply: true
description: FlowForge Workflow Orchestrator (CKP-0 → CKP-4)
---

# FlowForge — Workflow Orchestrator

You COORDINATE; you do NOT implement product code in src/ or tests.

## Checkpoints (traffic light)

| ID | Color | Meaning |
|----|-------|---------|
| CKP-0 | 🔴 HARD STOP | Vague requirement → STOP |
| CKP-1 | 🟡 YELLOW | spec.md ready → "Approve or adjust?" |
| CKP-2 | 🟡 YELLOW | plan.md ready → "Green light to code?" |
| CKP-3 | 🔴 EMERGENCY | 3 rework cycles → ESCALATE |
| CKP-4 | 🟢 DEPLOY GATE | Feature done → "Deploy?" |

## Delegation

| Phase | Sub-agent |
|-------|-----------|
| Discovery | forge-discovery |
| Spec | forge-arch |
| Plan | forge-plan |
| Code | forge-dev |
| Verify | forge-verify |
| Memory | forge-memory |
```

### 3.2 Mapping skills to IDE files

We do not copy all 35 skills into every IDE (too much context):

1. **workflow** — orchestration + delegation + git safety
2. **agents/*.md** — essential role instructions (compiled from skills when possible)
3. **skills/** — loaded on demand via `AGENTS.md` index

Recompile Cursor agents after skill changes:

```bash
python ide/cursor/compile-agents-from-skills.py
```

### 3.3 Stack reference pattern (partial)

The broader `references/` layout is still a follow-up. Today the only versioned stack
reference is:

```
skills/forge-plan/migrations/
├── SKILL.md
└── references/
    └── stack-postgres.md
```

The planned `skills/forge-dev/security/references/stack-{dotnet,python,javascript,sql}.md`
set is not implemented yet. `referencias-stack.md` was a session artifact and is not versioned.

---

## Part 4: Installation and usage

### Current CLI path (recommended, v0.5.0)

```bash
flowforge install --yes                 # stack + IDE packs, auto-detect
flowforge update --component flowforge-skills
flowforge doctor                        # verify destinations and MCP config
```

### Legacy shell installers (still supported)

Prefer the installers (global profile + optional project copy):

```powershell
# Windows — global Cursor profile
.\ide\install.ps1

# Project-only overlay
.\ide\install.ps1 -ProjectPath E:\path\to\your-repo
```

```bash
# Linux/macOS
bash ide/install.sh
bash ide/install.sh /path/to/your-repo
```

Manual copy (if needed):

```bash
cp ide/cursor/rules/*.mdc ~/.cursor/rules/
cp ide/cursor/agents/*.md  ~/.cursor/agents/
```

See [`ide/README.md`](../ide/README.md) for Antigravity, VS Code, and OpenCode.

---

## Part 5: Next steps

### Done (v0.4–v0.5)

- [x] IDE folders: `ide/cursor/`, `ide/antigravity/`, `ide/vscode/`, OpenCode bundle
- [x] `git-sin-push` rule
- [x] `install.ps1` / `install.sh` + `flowforge install/update`
- [x] Shared parity doc + rework intake
- [x] Seven `flow-*` commands (including `/flow-plan`, `/flow-rework`)
- [x] VS Code prompts pack (HU-040 / ADR-027)
- [x] Antigravity contract: `trigger:` rules, `/flow-*` skills, 7 native subagents (HU-050 / ADR-029)
- [x] OpenCode V2 config + `permissions[]` (HU-043 / HU-046)
- [x] Copilot + Kilo destinations
- [x] context7 MCP auto-install for remote Engram profiles (HU-042)

### Short term

- [ ] End-to-end smoke on a real project (task manager or greenfield)
- [ ] English pass on `ide/README.md` and parity doc
- [ ] Regenerate `ide/cursor/agents/*.md` after the pending skill changes

### Medium term

- [ ] Complete `references/` stack files under security/migrations skills
- [ ] Optional rule generator from skills only (no hand-edited drift)

---

## References

- [14-flowforge-complete-reference.md](14-flowforge-complete-reference.md)
- [15-agent-skills-technical-spec.md](15-agent-skills-technical-spec.md)
- [18-replicable-demo-definition.md](18-replicable-demo-definition.md)
- [ide/README.md](../ide/README.md)
- [ide/shared/workflow-orchestrator-parity.md](../ide/shared/workflow-orchestrator-parity.md)
- [ADR-008 — IDE installer path matrix](decisions/ADR-008-ide-installer-path-matrix.md)
- [ADR-019 — OpenCode/Antigravity paths](decisions/ADR-019-opencode-antigravity-customizations.md)
- [ADR-027 — VS Code prompts pack](decisions/ADR-027-vscode-prompts-pack.md)
- [ADR-029 — Antigravity customizations contract](decisions/ADR-029-antigravity-customizations-contract.md)
