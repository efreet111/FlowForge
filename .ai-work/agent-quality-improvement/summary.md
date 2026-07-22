# Summary — Agent Quality Improvement

> **Closed:** 2026-07-21  
> **Verdict:** ✅ PASS — Ready for CKP-4 (Deploy Gate)  
> **PM tests:** 5/5 ✅ executed and verified by human developer  
> **Rework cycles:** 2 (Cycle 1 false-resolved; Cycle 2 correctly applied and verified)  
> **Installer integrity:** ✅ Zero protected files touched

---

## 1. What was implemented

10 improvements across 3 priority tiers, covering all 4 IDEs (Cursor, VS Code, OpenCode, Antigravity):

### 🔴 Priority 1 — Critical (3 tasks)

| # | Issue | Resolution | Files affected |
|---|-------|------------|----------------|
| 1 | **Spanish/English mix** in forge-verify fallback + forge-memory template | Translated 25-line fallback block (Options A/B/C) and 5-line PM-* template to English in both SKILL.md sources and Cursor adapters | `skills/forge-verify/SKILL.md`, `skills/forge-memory/SKILL.md`, `ide/cursor/agents/forge-verify.md`, `ide/cursor/agents/forge-memory.md` |
| 2 | **OpenCode agents are stubs** (19-30 lines) | All 7 agents expanded to 80-120 lines with embedded role identity, required output, operation protocols, STOP conditions, and fallback instructions | 7 `ide/opencode/agents/forge-*.md` files |
| 3 | **VS Code RF/RNF → FR/NFR** | Fixed traceability chain — forge-arch, forge-dev, forge-discovery handoff prompts now use correct FR/NFR prefixes | `ide/vscode/agents/forge-arch.agent.md`, `forge-dev.agent.md`, `forge-discovery.agent.md` |

### 🟡 Priority 2 — High (3 tasks)

| # | Issue | Resolution | Files affected |
|---|-------|------------|----------------|
| 4 | **VS Code agents missing 6 protocols** | Backfilled: Memory Signal, HU import, OQ-\* tagging (forge-arch); Memory Signal + checklist rules (forge-dev); BLOCKER guard (forge-plan); FlowDoc sync + anti-false-close + Smart Curation (forge-memory); Fallback A/B/C (forge-verify) | 5 `ide/vscode/agents/forge-*.agent.md` files |
| 5 | **`{feature-name}` vs `{feature-slug}`** | Standardized to kebab-case slug across all 4 VS Code agent files | 4 `ide/vscode/agents/forge-*.agent.md` files |
| 6 | **forge-teacher self-containment violation** | Removed "Load SKILL.md" instruction — agent now fully self-contained (49 lines) | `ide/vscode/agents/forge-teacher.agent.md` |

### 🟠 Priority 3 — Medium (4 tasks)

| # | Issue | Resolution | Files affected |
|---|-------|------------|----------------|
| 7 | **YAML descriptions in Spanish** | 6 descriptions translated to English with consistent format | 5 VS Code + 1 OpenCode agent files |
| 8 | **Protocol duplication across IDEs** | Added `<!-- sync: path/to/canonical -->` drift comments on all duplicated blocks; Memory Curation Protocol replaced with shared parity reference in all orchestrators + SKILL.md source | 7 files across all IDEs |
| 9 | **Missing `revision_cycle.md` template** | Added complete template with YAML frontmatter (`phase`, `cycle_count`, `max_cycles`, `rejection_reason`) + usage rules | `ide/shared/workflow-orchestrator-parity.md` |
| 10 | **OpenCode agents lack error handling** | Added `## Error Handling` section (STOP/Fallback/Escalation) to all 7 agents | 7 `ide/opencode/agents/forge-*.md` files |

### 🔄 Rework Cycle 2 (Critical fix)

**Problem (Cycle 1):** The Cursor orchestrator fix was marked `status: "resolved"` but `git diff HEAD` confirmed zero changes. Root cause: the Cursor agent is compiled from `skills/forge-orchestrator/SKILL.md` via `compile-agents-from-skills.py` — the SKILL.md source was never modified.

**Fix (Cycle 2):**
1. Applied `<!-- sync: ide/shared/workflow-orchestrator-parity.md -->` before CKP table in `skills/forge-orchestrator/SKILL.md`
2. Replaced 29-line inline Memory Curation Protocol (STEP 1/2/3) with 5-line reference paragraph
3. Recompiled via `bash ide/install.sh` → 155-line `forge-orchestrator.md` with 2 sync comments

All 8 close criteria from `rework_ticket.md` satisfied.

---

## 2. Developer Manual Tests

| PM | Test | Status |
|----|------|--------|
| PM-1 | VS Code forge-arch writes correct FR/NFR | ✅ Executed |
| PM-2 | OpenCode agents work outside FlowForge repo | ✅ Executed |
| PM-3 | Spanish text eliminated from all agents | ✅ Executed |
| PM-4 | revision_cycle.md template is available | ✅ Executed |
| PM-5 | Cross-IDE artifact parity | ✅ Executed |

*Verified by the human developer.*

---

## 3. Key Learnings

### Patterns established

1. **Self-containment principle reinforced**: All agents across all 4 IDEs now function without external skill file dependencies. OpenCode agents embed critical instructions inline (80-120 lines). VS Code and Cursor agents are self-contained by design.

2. **Drift detection via `<!-- sync: -->` comments**: All 4 orchestrators + SKILL.md source now have HTML comments marking the canonical source for duplicated protocol blocks. Future auditors can `rg '<!-- sync:'` to find files needing synchronization.

3. **Standardized error handling**: Every OpenCode agent follows the three-part structure: `### STOP conditions` (when to block), `### Fallback` (alternative paths), `### Escalation` (when to escalate). This prevents silent failures.

4. **revision_cycle.md template**: Standardized YAML frontmatter (`phase`, `cycle_count`, `max_cycles`, `rejection_reason`) ensures machine-auditable revision history across all orchestrators.

### Bugs discovered and fixed

5. **REWORK-01: False resolution pattern** — The Cycle 1 rework was marked resolved without any code changes. Lesson: always verify fixes via `git diff HEAD` or file content checks, not by rework_ticket.md status alone. Valuable insight for the CKP-3 cycle control mechanism.

6. **Traceability chain was silently broken in VS Code**: `RF-`/`RNF-` (wrong) vs `FR-`/`NFR-` (correct) caused forge-dev to generate test tags with no matching requirements. forge-verify had nothing to cross-reference. This bug existed since the initial VS Code agent creation and was never caught because no one tested the full traceability chain.

### What to remember for future features

7. **Cursor agents are compiled, not authorable**: Changes to Cursor agents must go through `skills/forge-*/SKILL.md` (source of truth) + recompile via `bash ide/install.sh`. Direct edits to compiled `.md` files in `ide/cursor/agents/` will be overwritten.

8. **regex false-positive trap**: `rg 'RF-|RNF-'` matches the substring in `NFR-PERF-XXX`. Acceptance criteria must account for regex edge cases in code that has similar-looking identifiers.

9. **Installer protection works**: Zero protected files were modified across 24 changed files. The explicit protected-files list in `plan.md` and validation gates after each phase kept this safe.

---

## 4. Metrics

| Metric | Value |
|--------|-------|
| **Total files modified** | 24 |
| **Lines added** | 719 |
| **Lines removed** | 171 |
| **FR coverage** | 10/10 ✅ |
| **NFR compliance** | 6/6 ✅ |
| **Plan tasks** | 11/11 ✅ |
| **PM tests** | 5/5 ✅ |
| **Rework cycles** | 2 (max 3) |
| **Installer integrity** | ✅ Zero protected files touched |

### NFR Compliance

| NFR | Description | Status |
|-----|-------------|--------|
| NFR-001 (Consistency) | Identical output artifacts across all 4 IDEs | ✅ PASS |
| NFR-002 (Maintainability) | `<!-- sync: -->` comments on duplicated blocks | ✅ PASS |
| NFR-003 (Self-containment) | No agent requires external file loading | ✅ PASS |
| NFR-004 (Language) | Zero Spanish in agent instructions | ✅ PASS |
| NFR-005 (Length target) | OpenCode agents 80-120 lines | ✅ PASS |
| NFR-006 (Backward compatibility) | No existing artifacts break | ✅ PASS |

---

## 5. Open Items (non-blocking)

| Item | Type | Details |
|------|------|---------|
| VS Code orchestrator handoff says "RF/RNF" | 🟢 Minor | Line 15 in `forge-orchestrator.agent.md` — out of scope for FR-003 (only forge-arch, forge-dev, forge-discovery targeted). Fix in future quality pass. |
| Normalize `canonical:` → `sync:` | 🟢 Minor | Cursor forge-verify uses `<!-- canonical: ... -->` instead of `<!-- sync: ... -->`. Functionally equivalent but inconsistent. |
| context-map.md not created | 🟢 Minor | Not produced for this feature (quality improvement pass with no new product code). Feature scope is fully self-documenting. |
| CI lint for Spanish detection | 🔵 Future | Tracked in `docs/04-roadmap.md` — automated Spanish detection + drift comment validation |

---

## 6. Memory Signal

- **type:** pattern
- **significance:** high
- **summary:** "Established 4 durable patterns for agent quality: (1) self-containment — every agent across 4 IDEs now embeds critical instructions inline; (2) drift detection via `<!-- sync: -->` HTML comments on all duplicated protocol blocks; (3) standardized error handling (STOP/Fallback/Escalation) across all OpenCode agents; (4) `revision_cycle.md` template with YAML frontmatter for machine-auditable revision history. Key bug: VS Code RF/RNF silently broke the traceability chain since creation. Key operational lesson: Cursor agents are compiled from SKILL.md — direct edits to `ide/cursor/agents/*.md` are ephemeral."

---

## 7. Closure Status

| Gate | Status |
|------|--------|
| 🔴 PM-* all [x] | ✅ Pass — 5/5 executed by human |
| 🟡 Rework resolved | ✅ Pass — status: resolved, cycle 2/3 |
| 🟢 All artifacts in place | ✅ Pass — spec.md, plan.md, verify-report.md, rework_ticket.md, summary.md |
| 🔵 CHANGELOG updated | ✅ Pass — entries added under [Unreleased] |
| 🟢 **CKP-4 ready** | ✅ **Ready for Deploy Gate — human decides** |
