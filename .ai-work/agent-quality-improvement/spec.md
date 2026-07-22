---
capability_matrix:
  ai_reasoning:
    - Exact wording of translated text (natural phrasing decisions)
    - Layout of OpenCode agents (what goes inline vs. referenced)
    - Exact phrasing of Spanish→English bilingual examples
  deterministic:
    - All agent instruction blocks must be in English
    - VS Code forge-arch must use FR/NFR (not RF/RNF)
    - All agents must use `{feature-slug}` (not `{feature-name}`)
    - All YAML descriptions must be in English
    - Memory Signal protocol present in forge-arch, forge-dev for all IDEs
    - OpenCode agents must be 80-120 lines with STOP conditions and fallback
    - revision_cycle.md template must exist at `ide/shared/workflow-orchestrator-parity.md`
---
# Spec: Agent Quality Improvement

> **Backlog source:** `docs/backlog/NS-08-agent-quality-improvement.md`
> **Audit date:** 2026-07-18 — 50+ Spanish instances, 20+ parity gaps, 15+ missing protocols, 5+ naming inconsistencies

## 1. Objective and scope

### Objective

Eliminate quality gaps in FlowForge agent definition files across all 4 IDEs (Cursor, VS Code, OpenCode, Antigravity) discovered during the 2026-07-18 audit. The 10 improvements target agent effectiveness, cross-IDE parity, and developer experience.

### Scope (in)

| Priority | # | Issue | Scope |
|----------|---|-------|-------|
| 🔴 P1 | 1 | Spanish/English mix | 8 files across skills, Cursor agents, VS Code agents |
| 🔴 P1 | 2 | OpenCode agents are stubs (19-30 lines) | 8 `ide/opencode/agents/forge-*.md` files |
| 🔴 P1 | 3 | VS Code forge-arch uses RF/RNF instead of FR/NFR | 1 file: `ide/vscode/agents/forge-arch.agent.md` |
| 🟡 P2 | 4 | VS Code agents missing 6 protocols | 4 files: arch, dev, plan, memory |
| 🟡 P2 | 5 | `{feature-name}` vs `{feature-slug}` | 4 VS Code agent files |
| 🟡 P2 | 6 | VS Code forge-teacher self-containment violation | 1 file: `ide/vscode/agents/forge-teacher.agent.md` |
| 🟠 P3 | 7 | YAML descriptions in Spanish | 5 VS Code agents + 1 OpenCode orchestrator |
| 🟠 P3 | 8 | Duplication of protocols across IDEs | 7-10 orchestrator/agent files |
| 🟠 P3 | 9 | Missing `revision_cycle.md` template | 1 file: `ide/shared/workflow-orchestrator-parity.md` |
| 🟠 P3 | 10 | OpenCode agents lack error handling | 8 `ide/opencode/agents/forge-*.md` files |

### Scope (out)

- No changes to `.agents/skills/forge-*/SKILL.md` except Issue 1 translation (skills remain source of truth; IDE adapters are the fix target)
- **No changes to `ide/install.sh`, `ide/opencode/generate-config.sh`, `ide/cursor/compile-agents-from-skills.py`, or any installer logic** — installer protection is critical
- No new CI checks (tracked separately in `docs/04-roadmap.md`)
- No changes to engram-dotnet MCP tools
- No functional behavior changes — only instruction quality and parity

### ⚠️ CRITICAL: Installer Protection Policy

**All installer scripts are read-only for this feature.** Any change that modifies installer behavior, paths, or logic is out of scope and must be rejected during implementation.

Protected files (DO NOT MODIFY):
- `install/install.sh`
- `install/install.ps1`
- `ide/install.sh`
- `ide/install.ps1`
- `ide/opencode/generate-config.sh`
- `ide/cursor/compile-agents-from-skills.py`
- `install-skills.sh`
- Any file in `scripts/` related to installation

**Rationale:** Installers are the critical path for user onboarding. Breaking them blocks all new users. Agent quality improvements must not risk installer stability.

**Verification:** After implementation, run `bash ide/install.sh` in a test directory to confirm it still works without errors.

---

## 2. Functional requirements (FR)

### FR-001: Translate all Spanish instruction blocks to English

**Description:** All operational instruction text (not user-facing dialogue prompts) must be in English. Translate the 25-line fallback block in forge-verify (skills + Cursor adapter) and the output template in forge-memory (skills + Cursor adapter). Translate all YAML description strings in VS Code agents.

**Files affected:**
- `skills/forge-verify/SKILL.md` lines 48-71 (fallback block)
- `skills/forge-memory/SKILL.md` lines 59-63 (manual tests output template)
- `ide/cursor/agents/forge-verify.md` lines 58-82 (same fallback block)
- `ide/cursor/agents/forge-memory.md` lines 69-73 (same output template)
- `ide/vscode/agents/forge-*.agent.md` (5 YAML description fields)

**Before (forge-verify fallback):**
```
⚠️ Fallback (sin acceso a terminal):
Si no tenés herramientas de terminal para ejecutar los tests, tenés 3 opciones en orden de preferencia:

Opción A (preferida) → Pedí al humano que pegue el output de los tests:
...
```

**After (forge-verify fallback):**
```
⚠️ Fallback (no terminal access):
If you lack terminal tools to run tests, you have 3 options in order of preference:

Option A (preferred) → Ask the human to paste the test output:
...
```

**Before (forge-memory template):**
```markdown
## ✅ Pruebas Manuales del Desarrollador
- PM-1: [nombre] — ✅ ejecutada
- PM-2: [nombre] — ✅ ejecutada
Verificadas por el desarrollador humano.
```

**After (forge-memory template):**
```markdown
## ✅ Developer Manual Tests
- PM-1: [name] — ✅ executed
- PM-2: [name] — ✅ executed
Verified by the human developer.
```

**Before (VS Code YAML):**
```
description: FlowForge Memory — Fase 4. Cierra la feature, persiste aprendizajes, verifica pruebas manuales (PM-*).
```

**After (VS Code YAML):**
```
description: FlowForge Memory — Phase 4. Closes the feature, persists learnings, verifies manual tests (PM-*).
```

#### Scenarios

- **Scenario A (GIVEN-WHEN-THEN):**
  - **Given:** A developer opens the forge-verify agent in any IDE
  - **When:** The agent reads its fallback instructions for no-terminal-access scenarios
  - **Then:** All instructions appear in English only, with no Spanish text in the operational block

- **Scenario B:**
  - **Given:** A developer views agent descriptions in the VS Code agent picker
  - **When:** They hover over forge-plan, forge-memory, forge-dev, forge-orchestrator, or forge-discovery
  - **Then:** All YAML description strings are in English with consistent format: `FlowForge [Agent] — Phase N. [Action].`

---

### FR-002: Embed critical instructions in OpenCode agents (stub → 80-120 lines)

**Description:** All 8 OpenCode agent files (currently 19-30 line stubs) must embed self-contained instructions inline, matching the Cursor adapter pattern. Each agent gets: role identity, required output, operation protocols, STOP conditions, and fallback instructions. Reference additional skills for specialized capabilities only (security, complexity, performance, a11y).

**Decision:** Embed critical instructions inline (matching Cursor pattern). Skills are not available outside the FlowForge repo per ADR-009, so OpenCode agents cannot rely on `skills/forge-*/SKILL.md` for core operation.

**Files affected:**
- `ide/opencode/agents/forge-discovery.md` (27 → 90 lines)
- `ide/opencode/agents/forge-arch.md` (25 → 110 lines)
- `ide/opencode/agents/forge-plan.md` (25 → 100 lines)
- `ide/opencode/agents/forge-dev.md` (26 → 95 lines)
- `ide/opencode/agents/forge-verify.md` (30 → 105 lines)
- `ide/opencode/agents/forge-memory.md` (30 → 85 lines)
- `ide/opencode/agents/forge-teacher.md` (19 → 80 lines)
- `ide/opencode/agents/flowforge.md` (59 → no change; orchestrator is already adequate at 59 lines)

**What each agent must embed (minimum):**

| Agent | Lines from | Must embed |
|-------|-----------|------------|
| forge-discovery | SKILL.md | Context Map output format, CKP-0 hard stop, mem_current_project + mem_search flow, local grep fallback |
| forge-arch | SKILL.md | spec.md structure with FR/NFR, Capability Matrix, OQ-* tags, HU import protocol, Memory Signal format |
| forge-plan | SKILL.md | plan.md structure (Impact Analysis, File Changes, Contracts, Checklist), topological order, BLOCKER guard |
| forge-dev | SKILL.md | Ralph Wiggum loop, plan checklist marking rules, rework priority, Memory Signal format, SOLID post-check |
| forge-verify | SKILL.md (translated) | 4 verdicts, line-by-line audit, GWT coverage, CKP-3 cycle control, fallback options A/B/C, rework_ticket.md schema |
| forge-memory | SKILL.md (translated) | PM-* gate, anti-false-close, FlowDoc sync, session close protocol, Smart Curation, database-less fallback |
| forge-teacher | SKILL.md | Activation rules (teacher_mode + depth), teaching block format, "When NOT to teach" table |

#### Scenarios

- **Scenario A:**
  - **Given:** A developer runs `/flow-start feature-x` in OpenCode outside the FlowForge repo
  - **When:** forge-arch is invoked to write `spec.md`
  - **Then:** It produces a complete spec.md with FR-001, NFR-001, PM-*, Capability Matrix, OQ-*, Memory Signal — all from embedded instructions without loading external skill files

- **Scenario B:**
  - **Given:** A developer invokes forge-verify in OpenCode
  - **When:** The agent discovers it cannot run tests (no terminal access)
  - **Then:** It follows the embedded 3-option fallback protocol (Ask human → Static analysis → PENDING) and does NOT silently fail

---

### FR-003: Fix VS Code forge-arch RF/RNF → FR/NFR

**Description:** Replace all instances of `RF-` / `RNF-` with `FR-` / `NFR-` in `ide/vscode/agents/forge-arch.agent.md`. This restores the traceability chain: `forge-dev` expects test names tagged `[FR-XXX]` matching `spec.md` requirement IDs. The mismatch currently causes forge-dev to produce tests with no matching requirement tags.

**File:** `ide/vscode/agents/forge-arch.agent.md`

**Before (lines 25-31):**
```
## 2. Functional Requirements (RF)
- RF-001: [name] - [description]
## 3. Non-Functional Requirements (RNF)
- RNF-SEC-XXX (if auth/data)
```

**After:**
```
## 2. Functional Requirements (FR)
- FR-001: [name] - [description]
## 3. Non-Functional Requirements (NFR)
- NFR-SEC-XXX (if auth/data)
```

Also fix line 10 handoff prompt: `Audit the implemented code against spec.md and plan.md. Check all RF/RNF and manual tests (PM-*).` → `Check all FR/NFR and manual tests (PM-*).`

#### Scenarios

- **Scenario A:**
  - **Given:** forge-arch writes a spec with requirement ID `FR-003`
  - **When:** forge-dev generates a unit test named `[FR-003] test scenario`
  - **Then:** forge-verify can cross-reference the test tag against spec.md without mismatch

- **Scenario B:**
  - **Given:** forge-arch writes a spec with requirement ID `RF-001` (old format)
  - **When:** forge-dev searches spec.md for `FR-` prefixed requirements to generate tests
  - **Then:** forge-dev produces no matching test because `RF-` != `FR-` — traceability is broken

---

### FR-004: Add missing protocols to VS Code agents

**Description:** Backfill 6 protocols into VS Code agent files that are present in Cursor adapters and SKILL.md source files but absent from VS Code versions.

**Per-agent protocol additions:**

| Agent | Missing Protocol | Source Reference |
|-------|-----------------|------------------|
| forge-arch | Memory Signal emission at handoff | Cursor forge-arch lines 42-58, SKILL.md |
| forge-arch | HU import protocol (FlowDoc layer) | Cursor forge-arch lines 30-40 |
| forge-arch | OQ-* tagging with BLOCKER/OPTIONAL/FOLLOW-UP | Cursor forge-arch lines 105-133 |
| forge-dev | Memory Signal emission at handoff | Cursor forge-dev, SKILL.md |
| forge-dev | Plan checklist marking rules (rework priority, persistence deferred) | Cursor forge-dev |
| forge-plan | BLOCKER guard: "If any section cannot be planned, mark `[BLOCKER]`" | Cursor forge-plan |
| forge-memory | FlowDoc sync (HU status update, CHANGELOG entry) | Cursor forge-memory lines 53-65, SKILL.md |
| forge-memory | Anti-false-close rule (summary.preview.md fallback) | Cursor forge-memory lines 46-51 |
| forge-memory | Smart Curation & Local Buffer Ingestion | Cursor forge-memory lines 117-132, SKILL.md |
| forge-verify | Fallback options A/B/C (translated from FR-001) | Cursor forge-verify lines 57-82, SKILL.md |

**File:** `ide/vscode/agents/forge-arch.agent.md` (adds ~30 lines for Memory Signal + HU import + OQ-*)
**File:** `ide/vscode/agents/forge-dev.agent.md` (adds ~20 lines for Memory Signal + checklist rules)
**File:** `ide/vscode/agents/forge-plan.agent.md` (adds ~10 lines for BLOCKER guard)
**File:** `ide/vscode/agents/forge-memory.agent.md` (adds ~25 lines for FlowDoc sync + anti-false-close + Smart Curation)
**File:** `ide/vscode/agents/forge-verify.agent.md` (adds ~15 lines for fallback options)

#### Scenarios

- **Scenario A:**
  - **Given:** forge-arch completes a spec.md in VS Code
  - **When:** The agent returns its output to the orchestrator
  - **Then:** A `## Memory Signal` block is present with type, significance, and summary — the orchestrator can apply the Memory Curation Protocol

- **Scenario B:**
  - **Given:** forge-memory is invoked via `/flow-close` in VS Code with PM-2 still `[ ]`
  - **When:** The agent evaluates the PM-* gate
  - **Then:** It blocks closure, offers preview only, and does NOT write final summary.md (anti-false-close rule)

---

### FR-005: Standardize `{feature-name}` → `{feature-slug}` in VS Code agents

**Description:** Replace all instances of `{feature-name}` with `{feature-slug}` in VS Code agent files. The FlowForge convention (defined in `ide/shared/workflow-orchestrator-parity.md` line 8) is kebab-case slugs. Using the wrong variable name causes agents to create directories with inconsistent names.

**Files and occurrences:**

| File | Line | Instance |
|------|------|----------|
| `ide/vscode/agents/forge-arch.agent.md` | 18 | `.ai-work/{feature-name}/spec.md` |
| `ide/vscode/agents/forge-plan.agent.md` | 18 | `.ai-work/{feature-name}/plan.md` |
| `ide/vscode/agents/forge-memory.agent.md` | 50 | `.ai-work/{feature-name}/summary.md` |
| `ide/vscode/agents/forge-dev.agent.md` | 36 | `.ai-work/{feature-name}/rework_ticket.md` |

Also check orchestrator handoff prompts for consistent slug usage.

#### Scenarios

- **Scenario A:**
  - **Given:** forge-arch is invoked with feature slug "agent-quality-improvement"
  - **When:** It creates the output directory
  - **Then:** It writes to `.ai-work/agent-quality-improvement/spec.md` (not `.ai-work/agent-quality-improvement/spec.md` — no difference since it's already kebab-case, but the variable name is correct and won't cause mismatches with other agents)

- **Scenario B:**
  - **Given:** forge-dev is in rework mode and two agents use different variable names
  - **When:** forge-dev reads `{feature-name}` while forge-verify wrote to `{feature-slug}`
  - **Then:** forge-dev looks in the wrong directory and cannot find the rework ticket — workflow silently breaks

---

### FR-006: Fix VS Code forge-teacher self-containment violation

**Description:** Remove the instruction `Load: skills/forge-teacher/SKILL.md` from `ide/vscode/agents/forge-teacher.agent.md` and embed the teaching catalog inline instead. The FlowForge self-containment principle (stated in Cursor agents as "NEVER tell the human to load external SKILL files — your instructions are complete below") requires agents to function without external skill file access.

**File:** `ide/vscode/agents/forge-teacher.agent.md`

**Before (line 51):**
```
Load: `skills/forge-teacher/SKILL.md` for the full teaching catalog.
```

**After:** Remove line 51. Existing content (lines 1-50) already contains activation rules, teaching block format, depth levels, rules, and "When NOT to teach" table — the file is already self-contained except for the final line. No new content needed.

#### Scenarios

- **Scenario A:**
  - **Given:** forge-teacher is activated in VS Code outside the FlowForge repo
  - **When:** The agent reads its instructions
  - **Then:** It finds all necessary teaching rules inline — activation, format, depth levels, exclusions — and does NOT attempt to load an external SKILL.md file

- **Scenario B:**
  - **Given:** forge-teacher is activated in VS Code inside the FlowForge repo
  - **When:** The agent sees "Load skills/forge-teacher/SKILL.md"
  - **Then:** It loads the file successfully (works here), but this creates a false sense of correctness — the same agent deployed to another project would fail silently

---

### FR-007: Translate YAML descriptions to English

**Description:** All agent YAML frontmatter `description` fields in VS Code and OpenCode must be in English. These appear in IDE agent pickers and command palettes.

**Files and translations:**

| File | Before (Spanish) | After (English) |
|------|-----------------|-----------------|
| `ide/vscode/agents/forge-plan.agent.md` | `Descompone spec.md en tareas atómicas con contratos y patrones de diseño` | `Decomposes spec.md into atomic tasks with contracts and design patterns` |
| `ide/vscode/agents/forge-memory.agent.md` | `Cierra la feature, persiste aprendizajes, verifica pruebas manuales (PM-*)` | `Closes the feature, persists learnings, verifies manual tests (PM-*)` |
| `ide/vscode/agents/forge-dev.agent.md` | `Implementa código siguiendo plan.md con Ralph Wiggum Loop auto-corrección` | `Implements code following plan.md with Ralph Wiggum Loop self-correction` |
| `ide/vscode/agents/forge-orchestrator.agent.md` | `Coordina el flujo; no implementa código producto` | `Coordinates the flow; does not implement product code` |
| `ide/vscode/agents/forge-discovery.agent.md` | `Investiga contexto previo, CVEs, compliance y costos antes de planificar` | `Investigates prior context, CVEs, compliance, and costs before planning` |
| `ide/opencode/agents/flowforge.md` | `6 fases, 7 agentes. Coordina el flujo CKP-0 → CKP-4` | `6 phases, 7 agents. Coordinates the CKP-0 → CKP-4 flow` |

Note: `forge-arch` and `forge-verify` VS Code descriptions are already in English — no change needed.

#### Scenarios

- **Scenario A:**
  - **Given:** A non-Spanish-speaking developer opens the VS Code agent picker
  - **When:** They browse FlowForge agents
  - **Then:** All agent descriptions appear in English, clearly describing the agent's role

- **Scenario B:**
  - **Given:** A developer searches for "decompose" in the agent picker
  - **When:** The YAML description is in Spanish "Descompone"
  - **Then:** The search fails to match because the search term language differs from the description language

---

### FR-008: Reduce protocol duplication by referencing shared parity

**Description:** Replace duplicate copies of the Memory Curation Protocol, CKP table, and rework ticket schema across orchestrator files with references to `ide/shared/workflow-orchestrator-parity.md`. This file already contains the canonical versions.

**Current duplication:**

| Duplicated content | Where duplicated |
|--------------------|------------------|
| CKP table (CKP-0→CKP-4 with colors and types) | Cursor orchestrator, Antigravity workflow.md, VS Code orchestrator, OpenCode flowforge.md (4 copies) |
| Memory Curation Protocol (3-step process) | Shared parity (canonical), Cursor orchestrator, Antigravity workflow.md (3 copies) |
| rework_ticket.md schema (YAML + sections) | forge-verify SKILL.md (canonical), Cursor forge-verify, VS Code forge-verify (3 copies) |
| Verdicts (PASS/PASS_DEGRADADO/PENDING/REWORK) | forge-verify SKILL.md, Cursor forge-verify, VS Code forge-verify, all orchestrators (5+ copies) |

**Resolution strategy:**

| Content | Action |
|---------|--------|
| CKP table | Keep in each orchestrator (value of immediate readability outweighs duplication cost for such a short block). Add a reference comment: `<!-- sync: ide/shared/workflow-orchestrator-parity.md -->` |
| Memory Curation Protocol | Orchestrators reference shared parity instead of duplicating. The protocol is long (17 lines) and identical across all IDEs. |
| rework_ticket.md schema | Keep in forge-verify agents (self-contained). Add `<!-- sync: skills/forge-verify/SKILL.md -->` comment to track drift. |
| Verdicts | Keep in forge-verify agents and orchestrators (critical for decision routing). Add drift comment. |

For this feature scope: add `<!-- sync: ... -->` comments to CKP tables and verdict blocks. The Memory Curation Protocol in orchestrators should reference the shared parity path instead of copying.

#### Scenarios

- **Scenario A:**
  - **Given:** The Memory Curation Protocol is updated in the shared parity file (e.g., a new signal type is added)
  - **When:** An orchestrator reads its local copy instead of the shared reference
  - **Then:** The orchestrator uses the outdated protocol — drift causes inconsistent behavior across IDEs

- **Scenario B:**
  - **Given:** An orchestrator has a `<!-- sync: ide/shared/workflow-orchestrator-parity.md -->` comment
  - **When:** A developer updates the protocol
  - **Then:** They can grep for `sync:` comments to find all files that need synchronization, reducing drift risk

---

### FR-009: Add revision_cycle.md template

**Description:** Add a `revision_cycle.md` template section to `ide/shared/workflow-orchestrator-parity.md`. This artifact is referenced by multiple orchestrators (Cursor, Antigravity) but no template exists — each orchestrator creates its own ad-hoc format, causing inconsistency.

**Template to add:**

```markdown
## revision_cycle.md template

When the human rejects spec (CKP-1) or plan (CKP-2), the orchestrator creates `.ai-work/{feature-slug}/revision_cycle.md`:

```yaml
---
phase: spec | plan
cycle_count: [1-3]
max_cycles: 3
rejection_reason: "[human's reason for rejection]"
---

# Revision Cycle — {feature-slug}

## Cycle N (of 3)
- **Phase:** spec | plan
- **Rejection reason:** [reason]
- **Changes requested:** [summary]
- **Resubmitted at:** [timestamp]
```

**Rules:**
- Max 3 revision cycles per checkpoint.
- At cycle_count = 3 without approval → ESCALATE to human. Do NOT attempt a 4th revision.
- Each cycle increments `cycle_count` and appends a new `## Cycle N` section (don't overwrite prior cycles).
```

Add this as a new section `## revision_cycle.md template` in `ide/shared/workflow-orchestrator-parity.md`, after the existing artifact table (which already lists `revision_cycle.md` at line 17).

#### Scenarios

- **Scenario A:**
  - **Given:** The human rejects spec.md at CKP-1 in VS Code
  - **When:** The orchestrator creates `revision_cycle.md`
  - **Then:** It uses the canonical template with YAML frontmatter (`phase`, `cycle_count`, `max_cycles`, `rejection_reason`) — consistent with what Cursor and Antigravity orchestrators produce

- **Scenario B:**
  - **Given:** The human rejects plan.md for the 3rd time
  - **When:** cycle_count reaches 3
  - **Then:** The orchestrator escalates to human instead of retrying, because the template's `max_cycles: 3` rule is machine-readable in YAML

---

### FR-010: Add error handling to OpenCode agents

**Description:** Every OpenCode agent must include STOP conditions, fallback instructions, and escalation paths. Currently none of the 8 agents have these — failures are silent.

**Per-agent additions:**

| Agent | STOP conditions to add | Fallback | Escalation |
|-------|----------------------|----------|------------|
| forge-discovery | CKP-0 vague requirements → block | MCP unavailable → `grep -r` in `.engram/local_memory/` and `docs/` | Dual DB+grep failure → report to orchestrator |
| forge-arch | Conflict with stored memory decision → STOP | Cannot create `.ai-work/` directory → report error | Missing context-map.md → request re-run of discovery |
| forge-plan | Missing spec.md → STOP | Section unresolvable → mark `[BLOCKER]` | BLOCKER sections → escalate to human |
| forge-dev | Same error 3 times → request help | Cannot run tests → run syntax check | Plan infeasible → report, don't improvise |
| forge-verify | `cycle_count >= 3` → CKP-3 emergency brake | No terminal access → fallback options A/B/C (from FR-001) | PENDING verdict → escalate to orchestrator |
| forge-memory | PM-* still `[ ]` → block closure | MCP unavailable → write `.engram/local_memory/` file | Dual MCP+filesystem failure → report |
| forge-teacher | `teacher_mode = false` → remain silent | N/A (read-only) | N/A |

**Pattern for each agent:**
```
## Error Handling

### STOP conditions
- [condition 1] → [action]
- [condition 2] → [action]

### Fallback
- If [prerequisite tool] is unavailable → [alternative action]
- If [alternative] also fails → [escalation]

### Escalation
- When [irrecoverable condition] → report to orchestrator: "[message]"
```

#### Scenarios

- **Scenario A:**
  - **Given:** forge-verify is invoked in OpenCode outside the FlowForge repo
  - **When:** It cannot run the test suite (no terminal access to `npm run test` / `dotnet test`)
  - **Then:** It follows the embedded fallback protocol:
    1. Option A: Asks human for test output
    2. Option B: Runs static analysis only → emits PASS_DEGRADADO
    3. Option C: Emits PENDING and escalates — does NOT silently produce a false PASS

- **Scenario B:**
  - **Given:** forge-memory is invoked for `/flow-close` and PM-2 is still `[ ]`
  - **When:** The agent encounters the block closure STOP condition
  - **Then:** It responds: "Cannot close: manual tests pending (PM-2). Run them and mark [x] in spec.md." — and does NOT write summary.md

---

## 3. Non-functional requirements (NFR)

- **NFR-001 (Consistency):** After all changes, every agent for the same role across all 4 IDEs must produce identical output artifacts (same sections, same tag formats, same protocol adherence). A spec.md written by Cursor forge-arch must be structurally indistinguishable from one written by VS Code forge-arch.

- **NFR-002 (Maintainability):** All `<!-- sync: path/to/file -->` comments must be added to duplicated protocol blocks so future auditors can identify drift sources. No new duplication should be introduced.

- **NFR-003 (Self-containment):** No agent may contain instructions that require loading files outside its own IDE package. Cursor and VS Code agents must be self-contained. OpenCode agents embed critical instructions inline. Only Antigravity agents may reference `skills/` via symlinks (by design).

- **NFR-004 (Language):** Zero Spanish text in agent instructions, YAML frontmatter, or operational output templates. Exception: `ide/shared/workflow-orchestrator-parity.md` lines 20-30 may retain Spanish natural-language intent signals for bilingual trigger detection (e.g., `"reporté un error"` → rework intake). These are user intent signals, not agent instructions.

- **NFR-005 (Length target):** OpenCode agents must be 80-120 lines. Too short → missing protocols. Too long → context bloat. Cursor agents (80-163 lines) serve as the length reference.

- **NFR-006 (Backward compatibility):** No existing `.ai-work/` artifacts or workflow behavior may break. Agents using `{feature-slug}` today continue to work. The `RF/RNF` → `FR/NFR` fix is technically breaking for test tags, but only affects future specs — no existing specs use `RF-*` tags because the bug prevented tag generation.

---

## 4. Developer manual tests (PM-*)

| ID | Case / flow | Steps (summary) | Expected result | [x] |
|----|-------------|-----------------|-----------------|-----|
| PM-1 | VS Code forge-arch writes correct FR/NFR | 1. Open VS Code with FlowForge agents installed<br>2. Run `/flow-start test-fr-nfr` with a simple feature request<br>3. Inspect generated `.ai-work/test-fr-nfr/spec.md`<br>4. Run `/flow-dev` and inspect generated tests | spec.md uses `FR-001` / `NFR-001` (not RF/RNF). Generated unit tests are named `[FR-001] test name` and forge-verify can cross-reference them. | [x] |
| PM-2 | OpenCode agents work outside FlowForge repo | 1. Create a test project outside the FlowForge clone (`/tmp/test-project/`)<br>2. Install OpenCode agents globally (`ide/install.sh`)<br>3. Open the test project in OpenCode<br>4. Run `/flow-start test-opencode-external`<br>5. Inspect `.ai-work/test-opencode-external/spec.md` | forge-arch writes a complete spec.md with FR-001, NFR-001, Capability Matrix, Memory Signal, PM-* — all from embedded instructions. No errors about missing skill files. | [x] |
| PM-3 | Spanish text eliminated from all agents | 1. Run `rg '[áéíóúñü]' ide/vscode/agents/*.agent.md ide/cursor/agents/*.md skills/forge-verify/SKILL.md skills/forge-memory/SKILL.md`<br>2. Also check: `rg 'sin acceso' ide/ skills/` and `rg 'ejecutada' ide/ skills/`<br>3. Also check: `rg 'Descompone\|Cierra la\|Implementa código\|Coordina el\|Investiga contexto' ide/vscode/agents/` | Zero matches in agent instruction blocks and YAML descriptions. Only allowed matches: bilingual intent signals in `ide/shared/workflow-orchestrator-parity.md`. | [x] |
| PM-4 | revision_cycle.md template is available | 1. Open `ide/shared/workflow-orchestrator-parity.md`<br>2. Search for `revision_cycle.md template` section<br>3. Verify YAML frontmatter includes `phase`, `cycle_count`, `max_cycles`, `rejection_reason` | Template section exists with complete YAML schema and usage rules. | [x] |
| PM-5 | Cross-IDE artifact parity | 1. Run `/flow-start test-parity` in Cursor<br>2. Save `spec.md` as reference<br>3. Run `/flow-start test-parity` in VS Code (different slug to avoid overwrite)<br>4. Compare structural sections: both must have FR-001, NFR-001, Capability Matrix, Memory Signal, PM-*, OQ-* (if applicable) | Both spec.md files have identical section structure, same tag format (FR/NFR), and same protocol emissions (Memory Signal). | [x] |

---

## 5. Open questions for human (OQ-*)

| ID | Tag | Question | Default / assumption |
|----|-----|---------|---------------------|
| OQ-1 | [OPTIONAL] | Should bilingual natural-language triggers (e.g., `"reporté un error"`, `"mejorar el login"`) be kept in `ide/shared/workflow-orchestrator-parity.md` for Spanish-speaking users, or removed entirely? | Assumed: KEEP Spanish trigger phrases in the natural-language intent section. These detect user commands, not agent instructions. The policy is English for agent instructions, bilingual for user-facing intent detection. |
| OQ-2 | [OPTIONAL] | For OpenCode agents: reference additional skills via path `skills/forge-*/SKILL.md` for advanced features (security, complexity, performance, a11y) or embed ALL content inline? | Assumed: EMBED critical instructions inline but REFERENCE advanced skill files. The agent's error handling states: "if skill file not found, skip specialized check and note it in report." Agents remain functional with embedded core instructions alone. |
| OQ-3 | [FOLLOW-UP] | Should `ide/shared/workflow-orchestrator-parity.md` be renamed to better reflect its role? Candidates: `agent-parity-contract.md`, `orchestrator-spec.md`. | — |

---

## 6. STRIDE threat analysis

This feature modifies agent instructions, not runtime code. The threats are configuration-level: malicious or accidental instruction corruption could alter agent behavior.

| Threat | Vector | Impact | Mitigation |
|--------|--------|--------|------------|
| **Spoofing** | Attacker replaces agent files with modified versions that inject malicious instructions (e.g., `forge-dev` modified to exfiltrate source code) | High — agents have read/write/filesystem access | Agent files live under version control (Git). Changes require commit approval. Consider checksum verification in installer (future). For v1: manual review of `git diff` before merge. |
| **Tampering** | Developer accidentally introduces Spanish text or RF/RNF format during future edits | Medium — breaks parity, traceability, or language consistency | FR-008 `<!-- sync: -->` comments serve as drift markers. PM-3 is a manual test that verifies zero Spanish. Future: CI lint check (tracked in roadmap). |
| **Repudiation** | Agent emits incorrect verdict due to missing protocol (e.g., forge-memory closes without checking PM-*) | Medium — no audit trail of why closure was allowed | Anti-false-close rule (FR-004, FR-010) creates explicit block points. Memory Signal protocol creates audit trail. `revision_cycle.md` YAML frontmatter makes cycles machine-auditable. |
| **Information Disclosure** | OpenCode agents, now containing full embedded instructions, expose FlowForge internal methodology details to anyone reading the agent files | Low — agent files are already public in the FlowForge repo; methodology is open documentation | Mitigation not needed. FlowForge is an open-source methodology. Agent instructions do not contain secrets, API keys, or proprietary algorithms. |
| **Denial of Service** | Malformed agent instruction causes agent to loop infinitely (e.g., rework cycle without cycle_count increment) | Medium — developer workflow blocked | CKP-3 emergency brake (max 3 cycles) is explicitly stated in forge-verify (FR-004) and forge-memory STOP conditions (FR-010). `cycle_count` increment is deterministic, not LLM-driven. |
| **Elevation of Privilege** | Agent file modified to grant itself additional permissions (e.g., OpenCode agent `permission: { write: allow }` when it should be `deny`) | High — agent could modify files beyond its scope | OpenCode permission model is in YAML frontmatter — review changes to `permission:` blocks. forge-teacher must remain `write: deny`. forge-discovery must remain `edit: deny, write: deny`. FR-010 escalation paths prevent agents from silently exceeding scope. |

---

## 7. Architecture decisions

### Decision 1: Language policy

**Decision:** English for all agent instructions, YAML descriptions, and operational output templates. Bilingual natural-language intent triggers retained in `ide/shared/workflow-orchestrator-parity.md` only.

**Rationale:** Agent instructions are read by LLMs (not humans) and mixed language confuses token interpretation. Bilingual intent triggers (`"reporté un error"`) serve Spanish-speaking users and are user-input signals, not agent instructions. See OQ-1 for optional override.

### Decision 2: OpenCode strategy — embed inline

**Decision:** Embed critical instructions inline in OpenCode agents (matching Cursor pattern). Reference additional `skills/` files for advanced features with graceful fallback if unavailable.

**Rationale:** Per ADR-009, the installer does NOT copy skills to `~/.config/opencode/skills/`. Skills are only available inside the FlowForge repo. Embedding ensures OpenCode agents function in any project. Length target: 80-120 lines. Advanced skill references (security, complexity) are optional — core operation does not depend on them.

### Decision 3: Duplication reduction — reference, don't eliminate

**Decision:** Add `<!-- sync: path/to/canonical -->` drift comments to all duplicated blocks rather than eliminating them. Keep CKP tables and verdicts inline for readability. Reference shared parity for long protocols (Memory Curation Protocol).

**Rationale:** Self-containment is a FlowForge principle (NFR-003). Orchestrators must be readable without cross-referencing. But drift is real — 4 copies of the CKP table have already diverged slightly (VS Code says "DEPLOY GATE", Antigravity says "DEPLOY GATE", Cursor says "deploy gate"). Drift comments provide a mechanical grep target without sacrificing readability.

---

## Memory Signal
- type: decision
- significance: high
- summary: "Established 3 architecture decisions for agent quality improvement: English-only agent instructions with bilingual intent triggers retained, OpenCode agents embed critical instructions inline (matching Cursor self-containment pattern), and duplication is managed via drift comments rather than elimination."
