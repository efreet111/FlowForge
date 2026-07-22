# Plan: Agent Quality Improvement

> **Spec:** `.ai-work/agent-quality-improvement/spec.md`
> **Plan date:** 2026-07-21
> **BLOCKER guard:** ✅ No [BLOCKER] rows in spec §5 (OQ-1 [OPTIONAL], OQ-2 [OPTIONAL], OQ-3 [FOLLOW-UP])

## Summary

- **Total tasks:** 11 (+ 1 validation task)
- **Effort:** ~2.5 days estimated (S=30min, M=2h, L=4h)
- **Phases:** 4 (P1 Critical → P2 High → P3 Medium → Validation)
- **Spec coverage:** FR-001 through FR-010, NFR-001 through NFR-006

## 1. Impact and dependencies

### What changes
- Agent instruction files across 4 IDEs (Cursor, VS Code, OpenCode, Antigravity)
- Source-of-truth skill files (`skills/forge-verify/SKILL.md`, `skills/forge-memory/SKILL.md`)
- Shared parity document (`ide/shared/workflow-orchestrator-parity.md`)

### What does NOT change
- **Installer scripts** — strictly read-only (see §2 below)
- Agent behavior / logic — only instruction text quality and parity
- Engram-dotnet MCP tools
- CI pipeline (tracked separately in roadmap)

### Dependencies
- No new dependencies introduced
- Tasks 2.1 and 3.4 overlap (both modify OpenCode agents) — Task 3.4 adds error handling section to agents already expanded in 2.1
- Task 1.3 and 2.3 both modify VS Code files — 1.3 fixes RF/RNF, 2.3 fixes {feature-name} and self-containment

## 2. ⚠️ CRITICAL: Installer Protection

**The following files are READ-ONLY. Any task that modifies them must be REJECTED.**

| Protected file | Reason |
|---------------|--------|
| `install/install.sh` | Global installer — critical path for onboarding |
| `install/install.ps1` | Windows global installer |
| `ide/install.sh` | IDE-specific installer |
| `ide/install.ps1` | Windows IDE installer |
| `ide/opencode/generate-config.sh` | OpenCode config generator |
| `ide/cursor/compile-agents-from-skills.py` | Cursor agent compiler |
| `install-skills.sh` | Skill installer script |
| Any file in `scripts/` related to installation | Installation infrastructure |

**Verification after each phase:** Run `bash ide/install.sh` in a test directory to confirm no breakage.

## 3. File changes (Proposed Changes)

### Phase 1 files (P1 Critical)
- [MODIFY] `skills/forge-verify/SKILL.md` — Translate Spanish fallback block (lines 48-72)
- [MODIFY] `skills/forge-memory/SKILL.md` — Translate Spanish output template (lines 59-63)
- [MODIFY] `ide/cursor/agents/forge-verify.md` — Translate Spanish fallback block (lines 58-82)
- [MODIFY] `ide/cursor/agents/forge-memory.md` — Translate Spanish output template (lines 69-73)
- [MODIFY] `ide/vscode/agents/forge-arch.agent.md` — RF/RNF → FR/NFR (lines 3, 25-31)
- [MODIFY] `ide/vscode/agents/forge-dev.agent.md` — RF/RNF → FR/NFR in handoff prompt (line 10)
- [MODIFY] `ide/vscode/agents/forge-discovery.agent.md` — RF/RNF → FR/NFR in handoff prompt (line 10)

### Phase 2 files (P2 High)
- [MODIFY] `ide/opencode/agents/forge-discovery.md` — Embed instructions inline (27 → ~90 lines)
- [MODIFY] `ide/opencode/agents/forge-arch.md` — Embed instructions inline (25 → ~110 lines)
- [MODIFY] `ide/opencode/agents/forge-plan.md` — Embed instructions inline (25 → ~100 lines)
- [MODIFY] `ide/opencode/agents/forge-dev.md` — Embed instructions inline (26 → ~95 lines)
- [MODIFY] `ide/opencode/agents/forge-verify.md` — Embed instructions inline (30 → ~105 lines)
- [MODIFY] `ide/opencode/agents/forge-memory.md` — Embed instructions inline (30 → ~85 lines)
- [MODIFY] `ide/opencode/agents/forge-teacher.md` — Embed instructions inline (19 → ~80 lines)
- [MODIFY] `ide/vscode/agents/forge-arch.agent.md` — Add Memory Signal, HU import, OQ-* protocols
- [MODIFY] `ide/vscode/agents/forge-dev.agent.md` — Add Memory Signal, checklist rules
- [MODIFY] `ide/vscode/agents/forge-plan.agent.md` — Add BLOCKER guard
- [MODIFY] `ide/vscode/agents/forge-memory.agent.md` — Add FlowDoc sync, anti-false-close, Smart Curation
- [MODIFY] `ide/vscode/agents/forge-verify.agent.md` — Add fallback options A/B/C
- [MODIFY] `ide/vscode/agents/forge-arch.agent.md` — `{feature-name}` → `{feature-slug}` (line 18)
- [MODIFY] `ide/vscode/agents/forge-plan.agent.md` — `{feature-name}` → `{feature-slug}` (line 18)
- [MODIFY] `ide/vscode/agents/forge-memory.agent.md` — `{feature-name}` → `{feature-slug}` (line 50)
- [MODIFY] `ide/vscode/agents/forge-dev.agent.md` — `{feature-name}` → `{feature-slug}` (line 36)
- [MODIFY] `ide/vscode/agents/forge-teacher.agent.md` — Remove line 51 (self-containment violation)

### Phase 3 files (P3 Medium)
- [MODIFY] `ide/vscode/agents/forge-plan.agent.md` — Translate YAML description (line 3)
- [MODIFY] `ide/vscode/agents/forge-memory.agent.md` — Translate YAML description (line 3)
- [MODIFY] `ide/vscode/agents/forge-dev.agent.md` — Translate YAML description (line 3)
- [MODIFY] `ide/vscode/agents/forge-orchestrator.agent.md` — Translate YAML description (line 3)
- [MODIFY] `ide/vscode/agents/forge-discovery.agent.md` — Translate YAML description (line 3)
- [MODIFY] `ide/opencode/agents/flowforge.md` — Translate YAML description (line 2)
- [MODIFY] Multiple orchestrator/agent files — Add `<!-- sync: ... -->` drift comments
- [MODIFY] `ide/shared/workflow-orchestrator-parity.md` — Add revision_cycle.md template section
- [MODIFY] `ide/opencode/agents/forge-*.md` (7 files) — Add error handling sections (overlaps with Phase 2 Task 2.1)

### Phase 4 files (Validation)
- No file modifications — validation only

## 4. Implementation checklist

---

### Phase 1: P1 Critical — Language & Traceability Fixes

#### Task 1.1: Translate forge-verify Spanish fallback block
**Spec ref:** FR-001, Scenario A
**Description:** Translate the 25-line Spanish fallback block in forge-verify to English. This block provides 3 options (A/B/C) for when the agent lacks terminal access to run tests. The translation must preserve the exact structure: Option A (ask human), Option B (static analysis → PASS_DEGRADADO), Option C (PENDING + escalate).
**Files:**
- MODIFY: `skills/forge-verify/SKILL.md` (lines 48-72) — source of truth
- MODIFY: `ide/cursor/agents/forge-verify.md` (lines 58-82) — Cursor adapter (must match SKILL.md)
- PROTECTED: `install/install.sh`, `ide/install.sh`, `ide/cursor/compile-agents-from-skills.py` (DO NOT TOUCH)
**Acceptance criteria:**
- [x] All Spanish text in fallback block translated to English (lines 48-72 in SKILL.md)
- [x] Cursor adapter (lines 58-82) matches SKILL.md translation exactly
- [x] Structure preserved: Option A/B/C headings, code block with PASS DEGRADADO notation
- [x] No functional behavior changes — same 3 options, same escalation logic
- [x] `rg '[áéíóúñü]' skills/forge-verify/SKILL.md ide/cursor/agents/forge-verify.md` returns zero matches in lines 48-82
**Effort:** S (30 min)
**Dependencies:** None

---

#### Task 1.2: Translate forge-memory Spanish output template
**Spec ref:** FR-001, Scenario A
**Description:** Translate the 5-line Spanish output template in forge-memory to English. This template appears in the session summary when manual tests (PM-*) are verified.
**Files:**
- MODIFY: `skills/forge-memory/SKILL.md` (lines 59-63) — source of truth
- MODIFY: `ide/cursor/agents/forge-memory.md` (lines 69-73) — Cursor adapter (must match SKILL.md)
- PROTECTED: All installer scripts (DO NOT TOUCH)
**Acceptance criteria:**
- [x] `## ✅ Pruebas Manuales del Desarrollador` → `## ✅ Developer Manual Tests`
- [x] `PM-1: [nombre] — ✅ ejecutada` → `PM-1: [name] — ✅ executed`
- [x] `Verificadas por el desarrollador humano.` → `Verified by the human developer.`
- [x] Cursor adapter matches SKILL.md translation exactly
- [x] `rg 'ejecutada\|Pruebas Manuales\|desarrollador humano' skills/forge-memory/SKILL.md ide/cursor/agents/forge-memory.md` returns zero matches
**Effort:** S (20 min)
**Dependencies:** None

---

#### Task 1.3: Fix VS Code RF/RNF → FR/NFR (traceability chain)
**Spec ref:** FR-003, Scenarios A & B
**Description:** Replace all instances of `RF-`/`RNF-` with `FR-`/`NFR-` in VS Code agent files. This restores the traceability chain: forge-dev expects test tags `[FR-XXX]` matching spec.md requirement IDs. Also fix the YAML description in forge-arch that references "RF/RNF".
**Files:**
- MODIFY: `ide/vscode/agents/forge-arch.agent.md` — line 3 (YAML description), lines 25-31 (spec template)
- MODIFY: `ide/vscode/agents/forge-dev.agent.md` — line 10 (handoff prompt: "Check all RF/RNF" → "Check all FR/NFR")
- MODIFY: `ide/vscode/agents/forge-discovery.agent.md` — line 10 (handoff prompt: "Include RF/RNF" → "Include FR/NFR")
- PROTECTED: All installer scripts (DO NOT TOUCH)
**Acceptance criteria:**
- [x] `forge-arch.agent.md` line 3: "RF/RNF" → "FR/NFR" in YAML description
- [x] `forge-arch.agent.md` lines 25-31: `## 2. Functional Requirements (RF)` → `(FR)`, `RF-001` → `FR-001`, `## 3. Non-Functional Requirements (RNF)` → `(NFR)`, `RNF-SEC-XXX` → `NFR-SEC-XXX`
- [x] `forge-dev.agent.md` line 10: "Check all RF/RNF" → "Check all FR/NFR"
- [x] `forge-discovery.agent.md` line 10: "Include RF/RNF" → "Include FR/NFR"
- [x] `rg 'RF-|RNF-' ide/vscode/agents/` returns zero matches (note: `NFR-PERF-XXX` is a false positive — regex matches `RF-` inside `PERF-`; content is correct)
- [x] No other text in these files changed
**Effort:** S (20 min)
**Dependencies:** None

---

**🔵 Phase 1 Validation Gate:** Run `bash ide/install.sh` in a test directory. Verify no errors. Then run `rg '[áéíóúñü]' skills/forge-verify/SKILL.md skills/forge-memory/SKILL.md ide/cursor/agents/forge-verify.md ide/cursor/agents/forge-memory.md` and `rg 'RF-|RNF-' ide/vscode/agents/` to confirm zero matches.

---

### Phase 2: P2 High — OpenCode Embedding & VS Code Protocol Parity

#### Task 2.1: Enhance OpenCode agents (embed instructions inline)
**Spec ref:** FR-002, Scenarios A & B; NFR-005
**Description:** Transform all 7 OpenCode agent stubs (19-30 lines) into self-contained agents (80-120 lines) by embedding critical instructions inline. Each agent gets: role identity, required output format, operation protocols, STOP conditions, and fallback instructions. Reference additional `skills/` files for advanced features only (security, complexity, performance, a11y). The orchestrator (`flowforge.md`, 59 lines) is already adequate — no changes needed.
**Files:**
- MODIFY: `ide/opencode/agents/forge-discovery.md` (27 → ~90 lines) — embed: Context Map format, CKP-0 hard stop, mem_search flow, local grep fallback
- MODIFY: `ide/opencode/agents/forge-arch.md` (25 → ~110 lines) — embed: spec.md structure with FR/NFR, Capability Matrix, OQ-* tags, HU import protocol, Memory Signal format
- MODIFY: `ide/opencode/agents/forge-plan.md` (25 → ~100 lines) — embed: plan.md structure, topological order, BLOCKER guard
- MODIFY: `ide/opencode/agents/forge-dev.md` (26 → ~95 lines) — embed: Ralph Wiggum loop, checklist marking rules, rework priority, Memory Signal, SOLID post-check
- MODIFY: `ide/opencode/agents/forge-verify.md` (30 → ~105 lines) — embed: 4 verdicts, line-by-line audit, GWT coverage, CKP-3 cycle control, fallback A/B/C (translated from Task 1.1), rework_ticket.md schema
- MODIFY: `ide/opencode/agents/forge-memory.md` (30 → ~85 lines) — embed: PM-* gate, anti-false-close, FlowDoc sync, session close, Smart Curation, database-less fallback
- MODIFY: `ide/opencode/agents/forge-teacher.md` (19 → ~80 lines) — embed: activation rules, teaching block format, "When NOT to teach" table
- PROTECTED: All installer scripts (DO NOT TOUCH)
**Source references (read-only):**
- `skills/forge-discovery/SKILL.md` — source for discovery instructions
- `skills/forge-arch/SKILL.md` — source for arch instructions
- `skills/forge-plan/SKILL.md` — source for plan instructions
- `skills/forge-dev/SKILL.md` — source for dev instructions
- `skills/forge-verify/SKILL.md` — source for verify instructions (use translated fallback from Task 1.1)
- `skills/forge-memory/SKILL.md` — source for memory instructions (use translated template from Task 1.2)
- `skills/forge-teacher/SKILL.md` — source for teacher instructions
- `ide/cursor/agents/forge-*.md` — reference for Cursor adapter pattern (match structure)
**Acceptance criteria:**
- [x] Each agent is 80-120 lines (verify with `wc -l`)
- [x] Each agent contains: role identity, required output, protocols, STOP conditions, fallback
- [x] forge-verify agent includes translated fallback (Option A/B/C) — NOT Spanish
- [x] forge-memory agent includes translated PM-* template — NOT Spanish
- [x] forge-arch uses FR/NFR (not RF/RNF) in embedded spec template
- [x] All agents use `{feature-slug}` (not `{feature-name}`)
- [x] Advanced skills referenced by path (`skills/forge-*/SKILL.md`) with graceful fallback: "if skill file not found, skip specialized check"
- [x] `flowforge.md` unchanged (59 lines, already adequate)
- [x] No installer files modified
**Effort:** L (4h)
**Dependencies:** Task 1.1 (translated fallback for forge-verify), Task 1.2 (translated template for forge-memory)

---

#### Task 2.2: Add missing protocols to VS Code agents
**Spec ref:** FR-004, Scenarios A & B
**Description:** Backfill 6 protocols into VS Code agent files that exist in Cursor adapters and SKILL.md sources but are absent from VS Code versions. Each protocol addition is sourced from the corresponding Cursor adapter or SKILL.md file.
**Files:**
- MODIFY: `ide/vscode/agents/forge-arch.agent.md` — add (~30 lines): Memory Signal emission, HU import protocol, OQ-* tagging with BLOCKER/OPTIONAL/FOLLOW-UP
- MODIFY: `ide/vscode/agents/forge-dev.agent.md` — add (~20 lines): Memory Signal emission, plan checklist marking rules (rework priority, persistence deferred)
- MODIFY: `ide/vscode/agents/forge-plan.agent.md` — add (~10 lines): BLOCKER guard ("If any section cannot be planned, mark `[BLOCKER]`")
- MODIFY: `ide/vscode/agents/forge-memory.agent.md` — add (~25 lines): FlowDoc sync (HU status update, CHANGELOG entry), anti-false-close rule (summary.preview.md fallback), Smart Curation & Local Buffer Ingestion
- MODIFY: `ide/vscode/agents/forge-verify.agent.md` — add (~15 lines): Fallback options A/B/C (translated from Task 1.1)
- PROTECTED: All installer scripts (DO NOT TOUCH)
**Source references (read-only):**
- `ide/cursor/agents/forge-arch.md` lines 30-40 (HU import), 42-58 (Memory Signal), 105-133 (OQ-*)
- `ide/cursor/agents/forge-dev.md` (Memory Signal, checklist rules)
- `ide/cursor/agents/forge-plan.md` (BLOCKER guard)
- `ide/cursor/agents/forge-memory.md` lines 46-51 (anti-false-close), 53-65 (FlowDoc sync), 117-132 (Smart Curation)
- `ide/cursor/agents/forge-verify.md` lines 57-82 (fallback A/B/C — use translated version from Task 1.1)
**Acceptance criteria:**
- [x] forge-arch: Memory Signal block present with type/significance/summary fields
- [x] forge-arch: HU import protocol present (FlowDoc layer reference)
- [x] forge-arch: OQ-* tagging with BLOCKER/OPTIONAL/FOLLOW-UP categories
- [x] forge-dev: Memory Signal emission block present
- [x] forge-dev: Plan checklist marking rules (rework priority, persistence deferred)
- [x] forge-plan: BLOCKER guard instruction present
- [x] forge-memory: FlowDoc sync (HU status update + CHANGELOG entry)
- [x] forge-memory: Anti-false-close rule (PM-* gate → block closure, preview only)
- [x] forge-memory: Smart Curation & Local Buffer Ingestion
- [x] forge-verify: Fallback options A/B/C in English (matching Task 1.1 translation)
- [x] No installer files modified
**Effort:** M (2h)
**Dependencies:** Task 1.1 (translated fallback for forge-verify)

---

#### Task 2.3: Fix VS Code naming inconsistencies and self-containment
**Spec ref:** FR-005, FR-006
**Description:** Two sub-tasks: (A) Replace all `{feature-name}` with `{feature-slug}` in VS Code agent files to match the FlowForge convention (kebab-case slugs defined in `ide/shared/workflow-orchestrator-parity.md` line 8). (B) Remove the self-containment violation in forge-teacher that tells the agent to load an external SKILL.md file.
**Files:**
- MODIFY: `ide/vscode/agents/forge-arch.agent.md` — line 18: `{feature-name}` → `{feature-slug}`
- MODIFY: `ide/vscode/agents/forge-plan.agent.md` — line 18: `{feature-name}` → `{feature-slug}`
- MODIFY: `ide/vscode/agents/forge-memory.agent.md` — line 50: `{feature-name}` → `{feature-slug}`
- MODIFY: `ide/vscode/agents/forge-dev.agent.md` — line 36: `{feature-name}` → `{feature-slug}`
- MODIFY: `ide/vscode/agents/forge-teacher.agent.md` — line 51: remove `Load: skills/forge-teacher/SKILL.md for the full teaching catalog.`
- PROTECTED: All installer scripts (DO NOT TOUCH)
**Acceptance criteria:**
- [x] `rg '{feature-name}' ide/vscode/agents/` returns zero matches
- [x] `rg '{feature-slug}' ide/vscode/agents/` returns matches in forge-arch, forge-plan, forge-memory, forge-dev
- [x] `forge-teacher.agent.md` line 51 removed (file now 50 lines)
- [x] forge-teacher agent is fully self-contained (activation rules, format, depth levels, exclusions table all inline)
- [x] No other text in these files changed
- [x] No installer files modified
**Effort:** S (20 min)
**Dependencies:** None (can run in parallel with Task 2.2, but both modify same files — sequential recommended)

---

**🔵 Phase 2 Validation Gate:** Run `bash ide/install.sh` in a test directory. Verify no errors. Then run:
- `wc -l ide/opencode/agents/forge-*.md` — all 7 agents should be 80-120 lines
- `rg '{feature-name}' ide/vscode/agents/` — zero matches
- `rg 'skills/forge-teacher/SKILL.md' ide/vscode/agents/forge-teacher.agent.md` — zero matches

---

### Phase 3: P3 Medium — YAML, Drift, Templates & Error Handling

#### Task 3.1: Translate YAML descriptions to English
**Spec ref:** FR-007, Scenarios A & B
**Description:** Translate all Spanish YAML frontmatter `description` fields in VS Code and OpenCode agents to English. These appear in IDE agent pickers and command palettes.
**Files:**
- MODIFY: `ide/vscode/agents/forge-plan.agent.md` — line 3: `Descompone spec.md en tareas atómicas con contratos y patrones de diseño` → `Decomposes spec.md into atomic tasks with contracts and design patterns`
- MODIFY: `ide/vscode/agents/forge-memory.agent.md` — line 3: `Cierra la feature, persiste aprendizajes, verifica pruebas manuales (PM-*)` → `Closes the feature, persists learnings, verifies manual tests (PM-*)`
- MODIFY: `ide/vscode/agents/forge-dev.agent.md` — line 3: `Implementa código siguiendo plan.md con Ralph Wiggum Loop auto-corrección` → `Implements code following plan.md with Ralph Wiggum Loop self-correction`
- MODIFY: `ide/vscode/agents/forge-orchestrator.agent.md` — line 3: `Coordina el flujo; no implementa código producto` → `Coordinates the flow; does not implement product code`
- MODIFY: `ide/vscode/agents/forge-discovery.agent.md` — line 3: `Investiga contexto previo, CVEs, compliance y costos antes de planificar` → `Investigates prior context, CVEs, compliance, and costs before planning`
- MODIFY: `ide/opencode/agents/flowforge.md` — line 2: `6 fases, 7 agentes. Coordina el flujo CKP-0 → CKP-4` → `6 phases, 7 agents. Coordinates the CKP-0 → CKP-4 flow`
- PROTECTED: All installer scripts (DO NOT TOUCH)
**Note:** `forge-arch` and `forge-verify` VS Code descriptions are already in English — no change needed.
**Acceptance criteria:**
- [x] All 6 YAML descriptions translated to English
- [x] Consistent format: `FlowForge [Agent] — Phase N. [Action].`
- [x] `rg 'Descompone\|Cierra la\|Implementa código\|Coordina el\|Investiga contexto' ide/vscode/agents/` returns zero matches
- [x] `rg '6 fases, 7 agentes' ide/opencode/agents/flowforge.md` returns zero matches
- [x] No installer files modified
**Effort:** S (20 min)
**Dependencies:** None

---

#### Task 3.2: Add drift detection comments to duplicated blocks
**Spec ref:** FR-008, NFR-002
**Description:** Add `<!-- sync: path/to/canonical -->` HTML comments to all duplicated protocol blocks across orchestrator and agent files. This provides a mechanical grep target for future auditors to identify drift sources. Additionally, replace the Memory Curation Protocol in orchestrators with a reference to the shared parity file.
**Files:**
- MODIFY: `ide/cursor/agents/forge-orchestrator.md` — add `<!-- sync: ide/shared/workflow-orchestrator-parity.md -->` to CKP table; replace Memory Curation Protocol with reference
- MODIFY: `ide/vscode/agents/forge-orchestrator.agent.md` — same additions
- MODIFY: `ide/opencode/agents/flowforge.md` — same additions
- MODIFY: `.agents/workflows/flow-verify.md` (Antigravity) — add `<!-- sync: ide/shared/workflow-orchestrator-parity.md -->` to CKP table; replace Memory Curation Protocol with reference
- MODIFY: `ide/cursor/agents/forge-verify.md` — add `<!-- sync: skills/forge-verify/SKILL.md -->` to rework_ticket schema and verdicts
- MODIFY: `ide/vscode/agents/forge-verify.agent.md` — add same sync comments
- MODIFY: `skills/forge-verify/SKILL.md` — add `<!-- canonical: rework_ticket schema, verdicts -->` marker
- PROTECTED: All installer scripts (DO NOT TOUCH)
**Acceptance criteria:**
- [x] `rg '<!-- sync:' ide/ .agents/` returns matches in all orchestrator and forge-verify files
- [x] Memory Curation Protocol in orchestrators references shared parity instead of duplicating
- [x] CKP tables retain inline content (readability) but have sync comment
- [x] Verdict blocks retain inline content but have sync comment
- [x] No installer files modified
**Effort:** M (1.5h)
**Dependencies:** None

---

#### Task 3.3: Add revision_cycle.md template to shared parity
**Spec ref:** FR-009, Scenarios A & B
**Description:** Add a `## revision_cycle.md template` section to `ide/shared/workflow-orchestrator-parity.md` after the existing artifact table (line 17). The template includes YAML frontmatter (`phase`, `cycle_count`, `max_cycles`, `rejection_reason`) and usage rules (max 3 cycles, escalation at cycle 3).
**Files:**
- MODIFY: `ide/shared/workflow-orchestrator-parity.md` — insert new section after line 17 (after the artifact table)
- PROTECTED: All installer scripts (DO NOT TOUCH)
**Content to insert (from spec FR-009):**
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
**Acceptance criteria:**
- [x] `## revision_cycle.md template` section exists in `ide/shared/workflow-orchestrator-parity.md`
- [x] YAML frontmatter includes `phase`, `cycle_count`, `max_cycles`, `rejection_reason`
- [x] Rules section includes: max 3 cycles, escalation at 3, append-only cycle history
- [x] Template placed after artifact table (after line 17)
- [x] No installer files modified
**Effort:** S (20 min)
**Dependencies:** None

---

#### Task 3.4: Add error handling to OpenCode agents
**Spec ref:** FR-010, Scenarios A & B
**Description:** Add a `## Error Handling` section to each of the 7 OpenCode agent files (already expanded in Task 2.1). Each section includes: STOP conditions, fallback paths, and escalation rules. This task adds the error handling section to the agents that were expanded in Task 2.1 — the section should be integrated into the existing agent structure, not appended as an afterthought.
**Files:**
- MODIFY: `ide/opencode/agents/forge-discovery.md` — add: CKP-0 STOP, MCP→grep fallback, dual failure escalation
- MODIFY: `ide/opencode/agents/forge-arch.md` — add: memory conflict STOP, directory error report, missing context-map escalation
- MODIFY: `ide/opencode/agents/forge-plan.md` — add: missing spec.md STOP, section→BLOCKER fallback, BLOCKER escalation
- MODIFY: `ide/opencode/agents/forge-dev.md` — add: same-error-3x STOP, no-test→syntax fallback, infeasible plan report
- MODIFY: `ide/opencode/agents/forge-verify.md` — add: cycle_count≥3 CKP-3 brake, no-terminal→fallback A/B/C, PENDING escalation
- MODIFY: `ide/opencode/agents/forge-memory.md` — add: PM-* `[ ]` closure block, MCP→filesystem fallback, dual failure report
- MODIFY: `ide/opencode/agents/forge-teacher.md` — add: teacher_mode=false silent STOP
- PROTECTED: All installer scripts (DO NOT TOUCH)
**Pattern for each agent:**
```markdown
## Error Handling

### STOP conditions
- [condition] → [action]

### Fallback
- If [tool] unavailable → [alternative]
- If [alternative] also fails → [escalation]

### Escalation
- When [irrecoverable] → report to orchestrator: "[message]"
```
**Acceptance criteria:**
- [x] All 7 agents have `## Error Handling` section with STOP/Fallback/Escalation subsections
- [x] forge-verify fallback references Option A/B/C (English, matching Task 1.1)
- [x] forge-memory STOP condition blocks closure when PM-* still `[ ]`
- [x] forge-teacher STOP condition: `teacher_mode = false` → remain silent
- [x] Agents remain 80-120 lines after adding error handling (verify with `wc -l`)
- [x] No installer files modified
**Effort:** M (1.5h)
**Dependencies:** Task 2.1 (agents must be expanded first before adding error handling sections)

---

**🔵 Phase 3 Validation Gate:** Run `bash ide/install.sh` in a test directory. Verify no errors. Then run:
- `rg 'Descompone\|Cierra la\|Implementa código\|Coordina el\|Investiga contexto' ide/` — zero matches
- `rg '<!-- sync:' ide/ .agents/` — matches in expected files
- `rg 'revision_cycle.md template' ide/shared/workflow-orchestrator-parity.md` — match found
- `wc -l ide/opencode/agents/forge-*.md` — all 7 agents still 80-120 lines

---

### Phase 4: Validation

#### Task 4.1: Run installer validation + manual tests (PM-1 through PM-5)
**Spec ref:** PM-1 through PM-5, NFR-001 through NFR-006
**Description:** Final validation phase. Run the installer to confirm no breakage, then execute all 5 developer manual tests from spec.md §4.
**Files:**
- No files modified — validation only
- PROTECTED: All installer scripts are RUN but NOT MODIFIED
**Acceptance criteria:**
- [x] `bash ide/install.sh` completes without errors in a test directory
- [x] **PM-1:** VS Code forge-arch writes FR/NFR (not RF/RNF) — verify with test feature
- [x] **PM-2:** OpenCode agents work outside FlowForge repo — verify spec.md has FR-001, NFR-001, Capability Matrix, Memory Signal, PM-*
- [x] **PM-3:** Zero Spanish text in agent instructions: `rg '[áéíóúñü]' ide/vscode/agents/*.agent.md ide/cursor/agents/*.md skills/forge-verify/SKILL.md skills/forge-memory/SKILL.md` returns zero matches (except allowed bilingual intent signals in shared parity)
- [x] **PM-4:** revision_cycle.md template exists in shared parity with complete YAML schema
- [x] **PM-5:** Cross-IDE artifact parity — spec.md from Cursor and VS Code have identical structure (FR/NFR, Capability Matrix, Memory Signal, PM-*, OQ-*)
- [x] All NFRs verified: consistency (NFR-001), maintainability (NFR-002), self-containment (NFR-003), language (NFR-004), length target (NFR-005), backward compatibility (NFR-006)
**Effort:** M (2h)
**Dependencies:** All previous tasks (1.1 through 3.4) must be complete

---

## 5. Execution Order

```
Phase 1 (P1 Critical) — ~1h 10min
├── Task 1.1: Translate forge-verify Spanish (S, 30min)
├── Task 1.2: Translate forge-memory Spanish (S, 20min)
├── Task 1.3: Fix VS Code RF/RNF → FR/NFR (S, 20min)
└── 🔵 Validation Gate: run installer + grep checks

Phase 2 (P2 High) — ~6h 20min
├── Task 2.1: Enhance OpenCode agents (L, 4h) — depends on 1.1, 1.2
├── Task 2.2: Add VS Code protocols (M, 2h) — depends on 1.1
├── Task 2.3: Fix naming + self-containment (S, 20min)
└── 🔵 Validation Gate: run installer + wc -l + grep checks

Phase 3 (P3 Medium) — ~3h 50min
├── Task 3.1: Translate YAML descriptions (S, 20min)
├── Task 3.2: Add drift detection comments (M, 1.5h)
├── Task 3.3: Add revision_cycle.md template (S, 20min)
├── Task 3.4: Add OpenCode error handling (M, 1.5h) — depends on 2.1
└── 🔵 Validation Gate: run installer + grep checks

Phase 4 (Validation) — ~2h
└── Task 4.1: Installer validation + PM-1 through PM-5 (M, 2h)
```

**Total estimated effort:** ~13h 20min (~2.5 working days)

## 6. Risk Mitigation

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| Accidentally modify installer script | Low | Critical | Each task explicitly lists PROTECTED files; review `git diff` before each commit; validation gate after each phase |
| Break agent functionality by changing logic | Low | High | Only translate text and add protocols — don't change agent behavior; test with sample flow after each phase |
| Installer fails after changes | Low | Critical | Run `bash ide/install.sh` after each phase (3 validation gates); rollback last phase if broken |
| OpenCode agents exceed 120 lines | Medium | Medium | Target 80-120 lines per agent; use `wc -l` after Task 2.1; trim verbose sections if needed |
| Translation introduces ambiguity | Low | Medium | Use spec.md before/after examples as exact reference; keep technical terms (PASS_DEGRADADO, rework_ticket.md) unchanged |
| VS Code protocol additions break handoff prompts | Low | Medium | Only add new sections — don't modify existing handoff YAML structure; test handoff after Task 2.2 |
| Drift comments become outdated | Medium | Low | This is a known tradeoff (Decision 3 in spec); comments are grep targets, not automated checks; future CI lint tracked in roadmap |
| Task 3.4 pushes agents over 120 lines | Medium | Medium | Integrate error handling into existing structure (don't just append); if over 120, consolidate verbose sections |
| Spanish text in shared parity bilingual triggers flagged by PM-3 | Low | Low | PM-3 explicitly excludes `ide/shared/workflow-orchestrator-parity.md` bilingual intent signals (NFR-004 exception) |

## 7. Contracts and schemas

No new contracts or schemas introduced. This feature modifies instruction text only.

**Existing schemas referenced (read-only):**
- `rework_ticket.md` schema — canonical in `skills/forge-verify/SKILL.md`
- `revision_cycle.md` template — to be added in Task 3.3 to `ide/shared/workflow-orchestrator-parity.md`
- Capability Matrix format — canonical in `skills/forge-arch/SKILL.md`
- Memory Signal format — canonical in `skills/forge-arch/SKILL.md` and `skills/forge-dev/SKILL.md`

## 8. Checklist summary (for forge-dev)

```
Phase 1: P1 Critical
[x] 1.1 Translate forge-verify Spanish fallback (skills + Cursor)
[x] 1.2 Translate forge-memory Spanish template (skills + Cursor)
[x] 1.3 Fix VS Code RF/RNF → FR/NFR (arch + dev + discovery)
[x] 🔵 Phase 1 validation gate

Phase 2: P2 High
[x] 2.1 Enhance OpenCode agents (7 stubs → 80-120 lines)
[x] 2.2 Add missing protocols to VS Code agents (5 files)
[x] 2.3 Fix VS Code {feature-name} → {feature-slug} + forge-teacher self-containment
[x] 🔵 Phase 2 validation gate

Phase 3: P3 Medium
[x] 3.1 Translate YAML descriptions to English (6 files)
[x] 3.2 Add <!-- sync: --> drift comments to duplicated blocks
[x] 3.3 Add revision_cycle.md template to shared parity
[x] 3.4 Add error handling to OpenCode agents (7 files)
[x] 🔵 Phase 3 validation gate

Phase 4: Validation
[x] 4.1 Installer validation + PM-1 through PM-5 manual tests
```

---

## Memory Signal
- type: pattern
- significance: high
- summary: "All 11 tasks implemented across 4 phases. Key patterns: (1) rg regex 'RF-' matches substring in 'NFR-PERF-' — acceptance criteria regex had false positive; content was correct. (2) OpenCode agent embedding: consolidated STOP/Fallback/Escalation under single '## Error Handling' with '###' subsections to stay within 80-120 line target. (3) 'El Semáforo' Spanish nickname in VS Code orchestrator was out-of-scope for Task 1.1-1.2 but caught by PM-3 — fixed in Phase 4. (4) All 3 validation gates passed: installer exit 0, model validation PASS, zero Spanish in scoped files."
