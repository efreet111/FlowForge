# FlowForge — Orchestrator parity (all IDEs)

> Shared source: include or copy into Cursor, Antigravity, VS Code, and OpenCode.
> The orchestrator **coordinates**; it does not implement product code except as listed below.

## Artifacts (per feature)

Folder: `.ai-work/{feature-slug}/` (kebab-case, no `FLOW-` prefix).

| File | Agent | Notes |
|------|--------|--------|
| `context-map.md` | forge-discovery | Phase 0 output |
| `spec.md` | forge-arch | Includes PM-* (manual tests) |
| `plan.md` | forge-plan | Checklist `[ ]` / `[x]` — marked by **forge-dev** |
| `verify-report.md` | forge-verify | Always written — verdicts: PASS · PASS_DEGRADADO · PENDING · REWORK |
| `rework_ticket.md` | verify → dev | `status: "open"` takes priority over plan; `status: "resolved"` does NOT block close |
| `revision_cycle.md` | orchestrator | CKP-1/CKP-2 rejections (max 3) |
| `summary.md` | forge-memory | Only when PM-* are complete |

## Natural-language intent

| Signals | Action |
|---------|--------|
| "start feature", "new feature", `/flow-start` | forge-discovery → forge-arch |
| "build the plan", `/flow-plan` | forge-plan |
| "implement", "keep coding", `/flow-dev` | forge-dev |
| "verify", "audit", `/flow-verify` | forge-verify |
| "close feature", `/flow-close` | forge-memory |
| "reported a bug", "there is a bug", "failed", "does not meet spec" | **Rework intake** → forge-dev |
| "what phase", `/flow-status` | orchestrator reads `.ai-work/` only |

## Rework intake (bug report) — orchestrator does NOT fix code

On bug, regression, or failed manual test:

**Allowed inline (orchestrator):**

1. Resolve `feature-slug` (active folder under `.ai-work/` or ask).
2. Create/update `.ai-work/{feature-slug}/rework_ticket.md` with:
   - **Expected** / **Actual**
   - Reproduction steps
   - Evidence (logs, screenshots, request/response)
   - Environment
   - Severity (P0–P3)
   - YAML frontmatter: `cycle_count` (0 on create; +1 after each failed attempt), `status: "open"` (set to `"resolved"` when fixed)
3. **Delegate** to `forge-dev` with the report and ticket path.

**Forbidden inline:**

- Edit `src/**`, `tests/**`, `docs/**`, dashboards, or metrics as a "quick fix".
- Write `verify-report.md` — delegate to `forge-verify`.

If the issue is spec↔code misalignment without a runtime bug: `forge-verify` writes `rework_ticket.md` → then `forge-dev`.

**CKP-3:** if `cycle_count >= 3` in `rework_ticket.md`, escalate to the human. Do not attempt a 4th cycle.

## Memory Curation Protocol

The orchestrator applies this protocol after receiving a handoff from `forge-arch`
or `forge-dev`. No other agents emit Memory Signal.

### Memory Signal format (emitted by forge-arch and forge-dev)

```markdown
## Memory Signal
- type: decision | bugfix | config | pattern | none
- significance: high | low
- summary: "One line describing what occurred"
```

### Orchestrator 3-step process

```
STEP 1 — Eligible type?
  type == none → SKIP
  type in {decision, bugfix, config, pattern} → continue

STEP 2 — Was there friction?
  significance == high → continue
  revision_cycle >= 1 → continue
  cycle_count >= 2 (from rework_ticket.md frontmatter) → continue
  none → SKIP

STEP 3 — Already in Engram?
  mem_search(query=summary, limit=1) → recent match → SKIP
  no match → mem_save(title, type, content, topic_key)
  MCP error → write .engram/local_memory/obs-<timestamp>.md
```

### Session close

`forge-memory` MUST call `mem_session_summary` before CKP-4 in every `/flow-close`.
Not optional. If MCP is unavailable → write `obs-<timestamp>-session-close.md`.

---

## Close without PM-* (CKP-4)

- If `forge-memory` reports PM-* still `[ ]`, **do not** close the feature.
- Instruct: run PM-*, mark `[x]` in `spec.md`, retry `/flow-close`.
- Only on explicit **"close preview"**: draft `summary.preview.md` with a NOT-CLOSED warning.

## Dev done (definition)

`dev` is not "done" with green tests alone. Requires:

- `plan.md` checklist marked by **forge-dev** (persistence and PM-* items deferred — see dev skill rules).
- PM-* marked in `spec.md` before `/flow-close`.
- `verify-report.md` with PASS when applicable.

Optional projects may expose a sync script (e.g. `npm run flow-metrics`); it is **backup**, not a substitute for agent checklist marks.

## Orchestrator: inline only

- `/flow-status` (read `.ai-work/`)
- CKP messages (approve spec/plan, deploy)
- Create `rework_ticket.md` / `revision_cycle.md` (metadata, not product code)
- Delegation trace table (agent, requested model, effective model)
- Brief clarifications to the human

Everything else → **delegate** to the phase subagent.

## Skill↔Agent parity (W13)

Each core skill in `skills/forge-*/SKILL.md` has a compiled Cursor adapter in `ide/cursor/agents/forge-*.md`. These must stay in sync. Currently there is **no automated CI check** for this parity — drift caused the F1/F2/F3 CRITICALs. Until a CI parity check is implemented (tracked in `docs/04-roadmap.md`), any change to a skill MUST also update its corresponding adapter in the same commit.

## Mandatory delegation

- Model issues change **which model** the subagent uses, not **whether** to delegate.
- If delegation fails twice (timeout, spawn): report to the human. Do not take dev/verify inline unless: *"continue inline for this step only"*.
- Do not ask the human to load `SKILL.md` manually; the IDE must use compiled agents/rules or `{file:skills/...}`.

## forge-dev: rework priority

If `rework_ticket.md` (or legacy `rework.md`) exists with **open** status:

1. Read the ticket first.
2. Priority over the rest of the plan.
3. Reproduce (automated test when possible), fix, green tests.
4. Update ticket to resolved with change summary.
5. Mark corresponding items in `plan.md`.
