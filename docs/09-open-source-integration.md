# IDE Integration and Open Source Launch — Guide

> **Status**: Ready to publish (methodology **v0.4.0**)
> **Scope**: FlowForge (orchestration + IDE packs)
> **Version**: See [`VERSION.md`](../VERSION.md)

FlowForge is not only Markdown theory. It ships **installable IDE packs** (`ide/`) and a shared orchestrator contract so the same flow works in Cursor, VS Code, Antigravity, and OpenCode.

---

## 1. Current integration model (v0.4)

Canonical orchestrator behavior:

```text
ide/shared/workflow-orchestrator-parity.md
```

| IDE | Install | Orchestrator | Skills |
|-----|---------|--------------|--------|
| **Cursor** | `ide/install.ps1` / `install.sh` | `.cursor/rules/workflow.mdc` | Compiled `ide/cursor/agents/*.md` |
| **OpenCode** | Bundle → `~/.config/opencode/agents/` + `commands/` | Primary agent + subagents | `{file:./skills/.../SKILL.md}` |
| **Antigravity** | Project `.agents/` | `rules/workflow.md` | On-demand via workflows |
| **VS Code** | `.github/agents/` + copilot instructions | `forge-orchestrator.agent.md` | Embedded / agent files |

Artifacts live under **`.ai-work/{feature-slug}/`** (not legacy `openspec/changes/` paths).

```bash
# Recommended install (global + optional project)
bash ide/install.sh
bash ide/install.sh /path/to/your-app
```

See [`ide/README.md`](../ide/README.md) and [`16-ide-integration-plan.md`](16-ide-integration-plan.md).

---

## 2. Cursor (`.cursor/rules` + agents)

Cursor uses:

- `workflow.mdc` — CKP-0→4, delegation, rework intake (orchestrator does not patch `src/`)
- `git-sin-push.mdc` — no push without explicit request
- `model-assignments.mdc` — model hints per subagent
- `ide/cursor/commands/flow-*.md` — `/flow-start`, `/flow-plan`, `/flow-dev`, etc.
- `ide/cursor/agents/forge-*.md` — compiled from `skills/`:

```bash
python ide/cursor/compile-agents-from-skills.py
```

Legacy `.cursorrules` in the repo root is **deprecated** in favor of `.mdc` rules under `ide/cursor/`.

---

## 3. OpenCode (primary origin of this project)

`ide/install.sh` copies the bundle directly into `~/.config/opencode/agents/` and `~/.config/opencode/commands/`, and patches `opencode.json`/`.jsonc` with `mcp.engram.type = "local"` plus `enabled: true`. `opencode.flowforge.json` and `~/.config/opencode/flowforge/` remain available for migrating legacy installs, but FlowForge now prefers writing straight into the `agents/` directory.

Typical smoke after Linux setup:

1. `bash ide/install.sh`
2. Open a project, run `/flow-start <feature>`
3. Confirm subagents delegate and artifacts appear under `.ai-work/`

Adjust models to your provider (free Asian tiers, etc.) in OpenCode config — FlowForge is model-agnostic.

---

## 4. VS Code / GitHub Copilot

Per-project install copies:

- `.github/agents/*.md`
- `.vscode/copilot-instructions.md` (from `ide/vscode/`)

The orchestrator agent includes **Fix Rework** handoff → `rework_ticket.md` → forge-dev.

---

## 5. Antigravity

Workflows under `.agents/workflows/` mirror slash commands:

- `flow-start.md`, `flow-plan.md`, `flow-dev.md`, `flow-verify.md`, `flow-rework.md`, `flow-close.md`

Rules under `.agents/rules/` align with the shared parity doc.

---

## 6. Orchestration paradigms

### A. Human-guided (any chat IDE)

The human invokes phases explicitly:

1. `/flow-start` or natural language → discovery + spec (CKP-0, CKP-1)
2. `/flow-plan` → plan (CKP-2)
3. `/flow-dev` → implementation
4. `/flow-verify` → `verify-report.md`
5. `/flow-close` → memory (CKP-4)

State is **on disk** (`spec.md`, `plan.md`, `.ai-work/`), not only in chat memory.

### B. Autonomous (Cursor Agent, OpenCode subagents, Cline-style)

One initial command; the orchestrator delegates subagents and enforces checkpoints. On bugs, the orchestrator writes `rework_ticket.md` and delegates **forge-dev** — it does not hotfix product code inline.

---

## 7. Example day: JWT middleware (narrative)

1. `/flow-start JWT auth for API`
2. **Discovery** → context map; **CKP-0** pass
3. **Arch** → `spec.md` + PM-*; **CKP-1** human approves
4. **Plan** → `plan.md`; **CKP-2** green light
5. **Dev** → Ralph Wiggum until tests green; marks plan checklist
6. **Verify** → finds debug log of token → `rework_ticket.md` cycle 1
7. **Dev** fixes; **Verify** → PASS → `verify-report.md`
8. **Memory** → summary; **CKP-4** deploy decision

Sample real artifacts: [`examples/crud-tareas/`](../examples/crud-tareas/).

---

## 8. GitHub launch strategy

1. **Concepts over code** — README sells checkpoint discipline and delegation contract, not “another agent framework.”
2. **Quickstart in two languages** — [`QUICKSTART.md`](../QUICKSTART.md), [`QUICKSTART.es.md`](../QUICKSTART.es.md)
3. **Example artifacts** — `examples/` so adopters see real `spec.md` / `plan.md` / `verify-report.md`
4. **Contributing** — invite stack-specific skill references under `skills/*/references/` ([`CONTRIBUTING.md`](../CONTRIBUTING.md))

Optional demo repo is **not required** if docs + examples are sufficient ([`18-replicable-demo-definition.md`](18-replicable-demo-definition.md)).

---

## References

- [`14-flowforge-complete-reference.md`](14-flowforge-complete-reference.md)
- [`15-agent-skills-technical-spec.md`](15-agent-skills-technical-spec.md)
- [`I18N.md`](I18N.md)
