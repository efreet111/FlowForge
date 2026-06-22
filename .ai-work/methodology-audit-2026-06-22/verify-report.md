# Verify Report — Methodology Audit (Judgment Day)

> **Verdict**: ❌ FAIL — 6 CRITICALs confirmed, fixes required before Round 2
> **Date**: 2026-06-22
> **Method**: Judgment Day (adversarial dual/triple review)
> **Judges**: Sonnet 4.6 (A) · Opus 4 (C) · GPT-5.5 (B) — three independent model families
> **Scope**: 20 files — 8 core skills, 6 Cursor agents, workflow.mdc, parity, 3 commands
> **CKP-3**: Cycle 1/3

---

## 1. Audit Scope

| File | Type |
|------|------|
| `skills/forge-orchestrator/SKILL.md` | Core skill |
| `skills/forge-discovery/SKILL.md` | Core skill |
| `skills/forge-arch/SKILL.md` | Core skill |
| `skills/forge-plan/SKILL.md` | Core skill |
| `skills/forge-dev/SKILL.md` | Core skill |
| `skills/forge-verify/SKILL.md` | Core skill |
| `skills/forge-memory/SKILL.md` | Core skill |
| `skills/forge-teacher/SKILL.md` | Core skill |
| `ide/cursor/agents/forge-{discovery,arch,plan,dev,verify,memory}.md` | Cursor adapters (6) |
| `ide/cursor/rules/workflow.mdc` | Runtime rules |
| `ide/shared/workflow-orchestrator-parity.md` | Cross-IDE contract |
| `ide/cursor/commands/flow-{start,verify,close}.md` | Slash commands (3) |
| `AGENTS.md` | Public index |

---

## 2. Confirmed CRITICALs (all 3 judges agree)

### F1 — `rework_ticket.md` location contradiction
- **Severity**: CRITICAL
- **Files**: `skills/forge-verify/SKILL.md` (L82) vs `ide/cursor/agents/forge-verify.md` (L92), `skills/forge-dev/SKILL.md`, `skills/forge-orchestrator/SKILL.md`, `ide/shared/workflow-orchestrator-parity.md`
- **Description**: The verify skill orders creating `rework_ticket.md` **at the repo root**. Every other file (dev skill, orchestrator, parity, memory, Cursor agent) expects it at `.ai-work/{feature-slug}/rework_ticket.md`. Dev only searches `.ai-work/` for the ticket — a ticket written to repo root is never found, the rework loop breaks silently, and CKP-3 never counts cycles.
- **Judges**: Sonnet ✅ · Opus ✅ · GPT ✅
- **Fix**: Change `skills/forge-verify/SKILL.md` to `.ai-work/{feature-slug}/rework_ticket.md`.

### F2 — `rework_ticket.md` incompatible schema
- **Severity**: CRITICAL
- **Files**: `skills/forge-verify/SKILL.md` (L84–99) vs `skills/forge-dev/SKILL.md` (L49, L60–78)
- **Description**: Verify generates a ticket with frontmatter `status: "rejected"` and a `## Failure Reason` structure. Dev enters rework mode only when the ticket body contains `> Status: open`. The two agents use different schemas for the same file — a verify failure produces a ticket that dev does not recognize as an active rework request.
- **Judges**: Sonnet ✅ · Opus ✅ · GPT ✅
- **Fix**: Unify to a single canonical schema; verify must write `status: open` (or equivalent in YAML) to trigger dev rework mode.

### F3 — `verify-report.md` promised but never written
- **Severity**: CRITICAL
- **Files**: `ide/shared/workflow-orchestrator-parity.md` (L15), `ide/cursor/commands/flow-verify.md` (L7) vs `skills/forge-verify/SKILL.md` (L79–81), `ide/cursor/agents/forge-verify.md`
- **Description**: The cross-IDE parity contract and the `/flow-verify` command both declare `verify-report.md` as the output artifact of forge-verify. Neither the skill nor the Cursor agent contain any instruction to write that file. In PASS, verify only emits the word "PASS" inline. The versioned artifact that constitutes proof of audit never materializes.
- **Judges**: Sonnet ✅ · Opus ✅ · GPT ✅
- **Fix**: Add instruction in forge-verify skill and agent to always write `.ai-work/{feature-slug}/verify-report.md` (both PASS and REWORK).

### F4 — `PASS DEGRADADO` / `PENDING` unhandled in orchestrator
- **Severity**: CRITICAL
- **Files**: `skills/forge-verify/SKILL.md` (L40–68) vs `skills/forge-orchestrator/SKILL.md` (L80)
- **Description**: Verify defines three verdicts: PASS, PASS DEGRADADO (no test output), and PENDING (tools unavailable). The orchestrator only branches on PASS vs `rework_ticket.md` present. PENDING has no defined transition — the workflow stalls silently. PASS DEGRADADO may be interpreted as PASS, allowing CKP-4 without green test evidence, directly contradicting verify's own rule "No Green Output = No PASS".
- **Judges**: Sonnet ✅ · Opus ✅ · GPT ✅
- **Fix**: Define PENDING and PASS DEGRADADO transitions in the orchestrator; block CKP-4 for any non-full PASS.

### F5 — CKP-1 BLOCKER gate absent from `workflow.mdc`
- **Severity**: CRITICAL
- **Files**: `skills/forge-orchestrator/SKILL.md` (L38–53) vs `ide/cursor/rules/workflow.mdc` (L191)
- **Description**: The orchestrator skill defines a 3-case protocol for CKP-1: no questions (approve), BLOCKERs present (halt, reject "adelante"), OPTIONAL/FOLLOW-UP only (approve with note). `workflow.mdc` only asks for generic spec approval. In Cursor, a user saying "ok" with open BLOCKERs clears CKP-1 at the rule level, even though `forge-plan` later halts. The human gets false confidence that CKP-1 passed when it effectively didn't.
- **Judges**: Sonnet ✅ · Opus ❌ · GPT ✅
- **Fix**: Port Cases A/B/C BLOCKER logic from orchestrator skill into `workflow.mdc`.

### F6 — `AGENTS.md` vs `workflow.mdc` contradictory instructions
- **Severity**: CRITICAL
- **Files**: `AGENTS.md` (L9–11) vs `ide/cursor/rules/workflow.mdc` (L136–138)
- **Description**: `AGENTS.md` (the public contract for all IDEs) instructs agents to "Load the skill by reading the `SKILL.md` file". `workflow.mdc` (Cursor runtime) explicitly states "NEVER tell the human to load external SKILL files — your instructions are complete below" and calls manual skill loading a "workflow violation". An agent operating in Cursor with both files loaded receives directly contradictory instructions about the same action.
- **Judges**: Sonnet ✅ · Opus ✅ · GPT ❌
- **Fix**: Add a Cursor-specific note to `AGENTS.md` clarifying that in Cursor, agents are pre-compiled — do not load skills manually.

---

## 3. Confirmed WARNINGs (real) — 2-3 judges

| ID | Finding | Files | Judges |
|----|---------|-------|--------|
| W1 | `context-map.md` declared in parity as Phase 0 artifact; discovery only emits it conversationally, never writes to disk | parity L12, discovery SKILL | A+C |
| W2 | Tokens `BLOCKED`/`CLEAR` expected by orchestrator; discovery never emits them — fragile parsing | workflow.mdc L189, discovery SKILL | A+C |
| W3 | PM section heading mismatch: arch writes EN `## 4. Developer manual tests`, verify searches ES `## 4. Pruebas Manuales` | arch SKILL L73, verify SKILL L73 | A+C |
| W4 | Discovery claims verify MUST reject designs missing step 5 (`## Reusable Patterns Found`); verify has no such check | discovery SKILL L66, verify SKILL | A+C+B |
| W5 | `forge-teacher` has no Cursor agent file and no compilation entry — teacher_mode is a dead feature in Cursor | AGENTS.md L103, workflow.mdc | A+C |
| W6 | `teacher_mode` default contradiction: AGENTS loads only if `true`; skill defaults to `true` if unset | AGENTS.md L103, teacher SKILL L39 | A+C+B |
| W7 | Frontmatter `# ---` (commented) in forge-verify and forge-memory instead of `---` — YAML metadata does not parse | verify SKILL L1, memory SKILL L1 | C+B |
| W8 | Checklist items `5.3`/`6.3` referenced in dev and parity; plan template only generates `1.1`, `1.2`, `2.1`, `2.2` | dev SKILL L22–23, plan SKILL L57 | A+C |
| W9 | Memory rework gate has no defined mechanism to detect `open` vs `resolved` — a resolved ticket can permanently block `/flow-close` | memory SKILL L29–30 | A+B |
| W10 | Memory skill allows editing `AGENTS.md` directly in offline fallback — methodology scope creep without CKP | memory SKILL L141 | C+B |
| W11 | Memory deletes `.engram/local_memory/` files after ingestion before confirming versioned persistence | memory SKILL L119 | B only* |
| W12 | `EngramFlow` in forge-memory frontmatter — project is FlowForge, wrong name | memory SKILL L3–4 | A+C |
| W13 | Skill↔agent verbatim duplication without CI parity check — already caused drift in F1 (verify path) | all pairs | A+C+B |
| W14 | `"5 phases"` in workflow.mdc header vs 6-row delegation table (Discovery+Spec+Plan+Dev+Verify+Memory) | workflow.mdc L7, L23–30 | A+C |

*W11 is single-judge but mechanically verifiable.

---

## 4. Suspect findings (1 judge only)

| ID | Finding | Judge | Severity |
|----|---------|-------|----------|
| S1 | forge-arch mutates HU frontmatter before CKP-1 PASS — no rollback if spec rejected | GPT | WARNING real |
| S2 | forge-plan has no preflight for missing spec, ambiguous slug, or unapproved spec | GPT | WARNING theoretical |
| S3 | forge-discovery Cursor agent self-contradicts: preamble forbids loading skills, cross-refs say "load forge-arch" | Sonnet | WARNING real |
| S4 | `rework_count` field in memory curation step 2 vs `cycle_count` field in rework ticket — condition never fires | Sonnet | WARNING real |

---

## 5. Root cause analysis

The 6 CRITICALs share a common root: **`skills/forge-verify/SKILL.md` was never updated to match the rest of the ecosystem**. F1, F2, and F3 all originate there. The file still reflects an older design (repo root paths, `rejected` status, no verify-report artifact) that predates the current `.ai-work/{slug}/` convention. F4 is a verify-side problem that propagated to the orchestrator. F5 and F6 are Cursor adapter drift.

Secondary root cause: **no CI check verifies skill↔agent parity** (W13). The compile script patches some things but doesn't enforce consistency, and it's not run on every change.

---

## 6. Fix plan (proposed — awaiting human approval)

### Batch 1 — CRITICALs (F1–F6)
Target files: `skills/forge-verify/SKILL.md`, `skills/forge-orchestrator/SKILL.md`, `ide/cursor/agents/forge-verify.md`, `ide/cursor/rules/workflow.mdc`, `AGENTS.md`

### Batch 2 — High-impact WARNINGs (W1–W6, W12, W13)
Target files: `skills/forge-discovery/SKILL.md`, `skills/forge-memory/SKILL.md`, `skills/forge-verify/SKILL.md`, `ide/cursor/agents/forge-{discovery,memory,verify}.md`, `ide/shared/workflow-orchestrator-parity.md`

### Batch 3 — Cleanup (W7–W11, W14, Suspects)
Target files: `skills/forge-verify/SKILL.md` (frontmatter), `skills/forge-memory/SKILL.md` (frontmatter, W10, W11), `ide/cursor/rules/workflow.mdc` (W14), `.github/workflows/opencode-smoke.yml` (add skill↔agent parity check)

---

## 7. PM Tests (required before /flow-close)

| ID | Test | Status |
|----|------|--------|
| PM-1 | Run a feature flow end-to-end: verify FAIL produces a ticket that dev recognizes and enters rework | [ ] |
| PM-2 | Run a feature flow: verify PASS produces `verify-report.md` in `.ai-work/{slug}/` | [ ] |
| PM-3 | Open spec with `[BLOCKER]` in Cursor, type "ok" — confirm orchestrator halts and lists blockers | [ ] |
| PM-4 | Confirm teacher_mode activates correctly in Cursor after fix | [ ] |
| PM-5 | Confirm `/flow-close` does not block after ticket is marked resolved | [ ] |

---

*Generated by Judgment Day (adversarial triple-judge review) — 2026-06-22*
*Judges: Sonnet 4.6, Opus 4, GPT-5.5*
