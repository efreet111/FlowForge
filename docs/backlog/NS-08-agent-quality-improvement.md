# Backlog — Agent Quality Improvement

> **Status:** Proposed (not implemented)  
> **Priority:** P1 — High (affects agent effectiveness and cross-IDE parity)  
> **Created:** 2026-07-18  
> **Related:** Agent audit report (2026-07-18), `ide/shared/workflow-orchestrator-parity.md`

---

## Problem

A comprehensive audit of all FlowForge agent definition files across 4 IDEs (Cursor, VS Code, OpenCode, Antigravity) revealed **significant quality gaps** that affect agent effectiveness and user experience:

### Critical Issues

1. **Mixed language (Spanish/English):** 50+ instances of Spanish text in agent instructions, YAML descriptions, and operational blocks. This confuses agents and breaks consistency.

2. **OpenCode agents are stubs:** 19-30 line files that only say "Load skill on-demand" but skills aren't available outside the FlowForge repo. Severely degraded experience.

3. **VS Code forge-arch uses RF/RNF instead of FR/NFR:** Breaks traceability chain (forge-dev expects `[FR-XXX]` test tags).

### High-Priority Gaps

4. **VS Code agents missing protocols:** Memory Signal, HU import, BLOCKER guard, OQ-* tagging, FlowDoc sync, fallback options.

5. **Naming inconsistencies:** `{feature-name}` vs `{feature-slug}` across VS Code agents.

6. **VS Code forge-teacher violates self-containment:** Says "Load SKILL.md" which breaks FlowForge principle.

### Medium-Priority Issues

7. **YAML descriptions in Spanish:** Metadata strings shown in IDE agent pickers should be English.

8. **Duplication:** Memory Curation Protocol, rework ticket schema, CKP table duplicated 4-7 times across IDEs.

9. **Missing revision_cycle.md template:** Not provided in any IDE.

10. **OpenCode agents lack error handling:** No STOP conditions, fallback instructions, or escalation paths.

---

## Impact

| Issue | Impact | Severity |
|-------|--------|----------|
| Mixed language | Agents may misinterpret instructions | 🔴 Critical |
| OpenCode stubs | Non-functional outside FlowForge repo | 🔴 Critical |
| RF/RNF mismatch | Breaks test traceability | 🔴 Critical |
| Missing protocols | Inconsistent behavior across IDEs | 🟡 High |
| Naming inconsistencies | Confusion and errors | 🟡 High |
| Self-containment violation | Agents can't function without skills | 🟡 High |
| Spanish descriptions | Confuses non-Spanish users | 🟠 Medium |
| Duplication | Drift risk, maintenance burden | 🟠 Medium |
| Missing templates | Inconsistent artifacts | 🟠 Medium |
| No error handling | Agents fail silently | 🟠 Medium |

---

## Proposed Solution

### Phase 1: Critical Fixes (1-2 days)

1. **Translate all Spanish instruction blocks to English**
   - Files: `skills/forge-verify/SKILL.md`, `skills/forge-memory/SKILL.md`, `ide/cursor/agents/forge-verify.md`, `ide/cursor/agents/forge-memory.md`
   - Action: Translate 25-line fallback block in forge-verify, output template in forge-memory
   - Policy: English instructions + bilingual examples in separate "Natural language triggers (Spanish)" subsection

2. **Bring OpenCode agents to parity with Cursor/Skills**
   - Files: All 8 `ide/opencode/agents/forge-*.md` files
   - Action: Embed critical instructions inline (like Cursor does) OR make installer copy skills to `~/.config/opencode/skills/`
   - Target: 80-120 lines per agent (vs current 19-30)

3. **Fix VS Code forge-arch RF/RNF → FR/NFR**
   - File: `ide/vscode/agents/forge-arch.agent.md`
   - Action: Replace `RF-001` → `FR-001`, `RNF` → `NFR`
   - Verify: Check traceability with forge-dev test tags

### Phase 2: High-Priority Gaps (2-3 days)

4. **Add missing protocols to VS Code agents**
   - forge-arch: Add Memory Signal, HU import, OQ-* tagging
   - forge-dev: Add Memory Signal, plan checklist marking rules
   - forge-plan: Add BLOCKER guard
   - forge-memory: Add FlowDoc sync, Smart Curation, anti-false-close
   - forge-verify: Add fallback options A/B/C

5. **Standardize `{feature-name}` → `{feature-slug}`**
   - Files: All `ide/vscode/agents/*.agent.md`
   - Action: Global find/replace

6. **Fix VS Code forge-teacher self-containment**
   - File: `ide/vscode/agents/forge-teacher.agent.md`
   - Action: Remove "Load SKILL.md" instruction, embed catalog inline

### Phase 3: Medium-Priority Improvements (1-2 days)

7. **Translate YAML descriptions to English**
   - Files: All VS Code agent descriptions, OpenCode orchestrator description
   - Action: Translate metadata strings

8. **Reduce duplication**
   - Action: Reference `ide/shared/workflow-orchestrator-parity.md` instead of copying
   - Files: All orchestrator implementations

9. **Add revision_cycle.md template**
   - File: `ide/shared/workflow-orchestrator-parity.md`
   - Action: Add template, reference from all orchestrators

10. **Add error handling to OpenCode agents**
    - Files: All 8 `ide/opencode/agents/forge-*.md`
    - Action: Add STOP conditions, fallback instructions, escalation paths

---

## Acceptance Criteria

- [ ] All agent instructions in English (zero Spanish in operational blocks)
- [ ] OpenCode agents are 80-120 lines with embedded instructions
- [ ] VS Code forge-arch uses FR/NFR (not RF/RNF)
- [ ] All VS Code agents have Memory Signal protocol
- [ ] All agents use `{feature-slug}` (not `{feature-name}`)
- [ ] VS Code forge-teacher is self-contained
- [ ] All YAML descriptions in English
- [ ] Orchestrators reference shared parity (no duplication)
- [ ] revision_cycle.md template exists
- [ ] OpenCode agents have error handling

---

## Effort Estimate

| Phase | Tasks | Effort |
|-------|-------|--------|
| Phase 1 (Critical) | 3 tasks | 1-2 days |
| Phase 2 (High) | 3 tasks | 2-3 days |
| Phase 3 (Medium) | 4 tasks | 1-2 days |
| **Total** | **10 tasks** | **4-7 days** |

---

## Dependencies

- **None** — can start immediately
- **Blocks:** Starter Kit PRD (agent quality affects onboarding experience)

---

## Audit Source

Full audit report available in conversation history (2026-07-18). Key findings:

- **50+ Spanish instances** across all IDEs
- **20+ parity gaps** between IDEs
- **15+ missing protocols** in VS Code/OpenCode
- **5+ naming inconsistencies**

---

## References

- Agent audit: Conversation 2026-07-18 (forge-explore task)
- Shared parity: `ide/shared/workflow-orchestrator-parity.md`
- Cursor agents (reference): `ide/cursor/agents/forge-*.md`
- Skills (source of truth): `skills/forge-*/SKILL.md`
