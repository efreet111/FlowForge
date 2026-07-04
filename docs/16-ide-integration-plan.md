# FlowForge for IDEs: Cursor, Antigravity, VS Code, and OpenCode

> **Version**: 1.1 — Cursor Enterprise Agents analysis + integration plan
> **Date**: 2026-05-27
> **Status**: IDE assets live under `ide/`; parity doc in `ide/shared/`

---

## Part 1: What we learned from Cursor Enterprise Agents

We reviewed `cursor-enterprise-agents` v0.0.5 and mapped patterns directly to FlowForge:

### What we already have (matches)

| FlowForge | Cursor EA | Status |
|-----------|-----------|--------|
| `forge-orchestrator` (CKP table, delegation) | `workflow.mdc` (orchestrator + commands) | ✅ Direct parallel |
| `forge-discovery` + `forge-arch` | `spec-writer` + `project-explorer` | ✅ Same idea |
| `forge-plan` (patterns, security, rollback) | `solution-architect` | ✅ FlowForge is richer |
| `forge-dev` (6 skills) | `dev-agent` (monolithic skill) | ✅ More granular |
| `forge-verify` (5 skills) | `verifier-agent` | ✅ More audit dimensions |
| `forge-memory` (4 skills) | `hu-closer` | ✅ Persist + metrics |
| `forge-teacher` | `user-liaison` (`/explain-more`) | ✅ Toggleable |
| Model table | `model-assignments.mdc` | ✅ In `ide/cursor/rules/` |
| `docs/14` (test cases) | `GUIA-USO.md` + `BEST-PRACTICES.md` | ✅ Documented |

### Gaps we closed in v0.4

| Gap | FlowForge solution | Status |
|-----|-------------------|--------|
| Git safety (no push unless asked) | `git-sin-push.mdc` / parity doc | ✅ |
| IDE install automation | `ide/install.ps1`, `ide/install.sh` | ✅ |
| Orchestrator parity across IDEs | `ide/shared/workflow-orchestrator-parity.md` | ✅ |
| `/flow-plan`, `/flow-rework` commands | Cursor + Antigravity workflows | ✅ |
| OpenCode bundle | `~/.config/opencode/agents/` + `commands/` (legacy: `opencode.flowforge.json`) | ✅ |

### Remaining improvements

| Gap | Cursor EA pattern | Priority |
|-----|-------------------|----------|
| Stack-specific skill references | `verifier-security-gaps/references/stack-*.md` | 🟡 Important |
| Auto-bootstrap of rules | Section in `workflow.mdc` | 🟡 Important |
| Danger-zone checkpoint doc | `human-in-the-loop-checkpoints.md` §2 | 🟢 Nice to have |
| Natural-language intent signals | Signal table in workflow | 🟢 Nice to have |

---

## Part 2: IDE integration model

### Principle: FlowForge is IDE-agnostic

Skills are Markdown files. Each IDE has its own rules and agent format. We **translate** skills into each IDE — we do not fork the methodology per IDE.

```
skills/ (canonical — 31 SKILL.md files)
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
| **Antigravity** | `.agents/rules/*.md` | skills under `.agents/skills/` | `.agents/workflows/flow-*.md` | install script → project |
| **VS Code / Copilot** | `.github/copilot-instructions.md` or `.vscode/` | `.github/agents/*.md` | Chat + agent files | copy or install script |
| **OpenCode** | MCP in `opencode.json` (`type: local`) | `agents/*.md` (orchestrator: `flowforge.md`) | `commands/*.md` | install script → `~/.config/opencode/agents/` + `commands/` |

### IDE path matrix

FlowForge writes agent packs to the directories each IDE actually reads. The matrix below shows the canonical global and project destinations plus the detection signals used by the installer.

| IDE | Global agents | Project agents | Detection |
|-----|---------------|----------------|-----------|
| **Cursor** | `~/.cursor/agents/`, `~/.cursor/rules/`, `~/.cursor/commands/` | `.cursor/agents/`, `.cursor/rules/`, `.cursor/commands/` | Presence of `~/.cursor` |
| **OpenCode** | `~/.config/opencode/agents/`, `~/.config/opencode/commands/` | `.opencode/agents/`, `.kilo/agents/` (mirrored) | Presence of `~/.config/opencode` |
| **GitHub Copilot** | `~/.copilot/agents/*.agent.md`, `~/.copilot/instructions/flowforge.instructions.md` | `.github/agents/*.agent.md`, `.github/copilot-instructions.md` | VS Code extension `github.copilot*` |
| **Kilo Code** | `~/.config/kilo/agents/*.md` (same bundle as OpenCode) | `.kilo/agents/*.md` (duplicated) | VS Code extension `kilocode.*` |
| **Antigravity** | `~/.gemini/antigravity/` (`AGENTS.md`, `rules/`, `workflows/`, `mcp_config.json`) | `.agents/rules/`, `.agents/workflows/`, `AGENTS.md` | `~/.gemini` or `%LOCALAPPDATA%\Google\Gemini` |
| **Claude Desktop** | `~/.config/Claude/claude_desktop_config.json` (MCP only) | — | `%APPDATA%\Claude` / `~/.config/Claude/` |

This matrix is the reference for `flowforge install`, `ide/install.sh`, `ide/install.ps1`, and the CI checks. See [`docs/decisions/ADR-008-ide-installer-path-matrix.md`](docs/decisions/ADR-008-ide-installer-path-matrix.md) for the rationale and detection logic.

### Files per IDE (current layout)

**Cursor** (`ide/cursor/`):

```
ide/cursor/
├── rules/
│   ├── workflow.mdc
│   ├── model-assignments.mdc
│   └── git-sin-push.mdc
├── agents/
│   └── forge-*.md
├── commands/
│   └── flow-start.md, flow-plan.md, flow-dev.md, flow-verify.md, flow-rework.md, flow-close.md, ...
└── compile-agents-from-skills.py
```

**Antigravity** (`ide/antigravity/`):

```
ide/antigravity/
├── rules/
├── workflows/   ← flow-start, flow-plan, flow-dev, flow-verify, flow-rework, flow-close
└── AGENTS.md
```

**VS Code** (`ide/vscode/`):

```
ide/vscode/
├── copilot-instructions.md
└── (see also .github/agents/ templates in docs)
```

---

## Part 3: IDE file architecture

### 3.1 Cursor: `workflow.mdc`

The most important file. Compiles `forge-orchestrator` for Cursor:

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

We do not copy all 31 skills into every IDE (too much context):

1. **workflow** — orchestration + delegation + git safety
2. **agents/*.md** — essential role instructions (compiled from skills when possible)
3. **skills/** — loaded on demand via `AGENTS.md` index

Recompile Cursor agents after skill changes:

```bash
python ide/cursor/compile-agents-from-skills.py
```

### 3.3 Stack reference pattern (planned)

```
skills/forge-dev/security/
├── SKILL.md
└── references/
    ├── stack-dotnet.md
    ├── stack-python.md
    ├── stack-javascript.md
    └── stack-sql.md
```

Documented in [referencias-stack.md](referencias-stack.md); implementation is a follow-up iteration.

---

## Part 4: Installation and usage

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

### Done (v0.4)

- [x] IDE folders: `ide/cursor/`, `ide/antigravity/`, `ide/vscode/`, OpenCode bundle
- [x] `git-sin-push` rule
- [x] `install.ps1` / `install.sh`
- [x] Shared parity doc + rework intake
- [x] Seven `flow-*` commands (including `/flow-plan`, `/flow-rework`)

### Short term

- [ ] End-to-end smoke on a real project (task manager or greenfield)
- [ ] English pass on `ide/README.md` and parity doc
- [ ] Translate remaining `skills/*/SKILL.md` → recompile agents

### Medium term

- [ ] `references/` stack files under security skills
- [ ] Optional rule generator from skills only (no hand-edited drift)

---

## References

- [14-flowforge-complete-reference.md](14-flowforge-complete-reference.md)
- [15-agent-skills-technical-spec.md](15-agent-skills-technical-spec.md)
- [18-replicable-demo-definition.md](18-replicable-demo-definition.md)
- [ide/README.md](../ide/README.md)
- [ide/shared/workflow-orchestrator-parity.md](../ide/shared/workflow-orchestrator-parity.md)
