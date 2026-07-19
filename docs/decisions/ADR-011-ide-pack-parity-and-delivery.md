# ADR-011: IDE Pack Parity & Skill Delivery Pipeline

> **Status:** Proposed  
> **Date:** 2026-01-17  
> **Deciders:** FlowForge Team  
> **Related:** [PRD-starter-kit.md](../PRD-starter-kit.md), [ADR-008-ide-installer-path-matrix.md](ADR-008-ide-installer-path-matrix.md)

---

## Context

FlowForge supports 4 IDEs: **Cursor**, **VS Code**, **OpenCode**, and **Antigravity**. Each IDE has its own "pack" — a set of agent definition files, rules, workflows, and skills that enable the FlowForge workflow in that IDE.

### Current State (Problems Identified)

**Problem 1: Agent Prompt Parity Gap**

| IDE | Agent Quality | Lines per Agent | Examples | Gap |
|-----|---------------|-----------------|----------|-----|
| **Cursor** | ✅ Rich | 70-162 | Yes | Best pack — self-contained agents with templates |
| **VS Code** | 🟡 Medium | 39-103 | Some | Could add more concrete output templates |
| **OpenCode** | 🔴 Thin stubs | 19-30 | No | Agents are just pointers to skills |
| **Antigravity** | 🔴 No per-agent files | N/A | N/A | Only rules + workflow stubs |

**Impact:** Users on OpenCode and Antigravity get a degraded experience. Agents can't function without loading external skills, but the skill delivery pipeline is broken (see Problem 2).

**Problem 2: Skill Delivery Pipeline Broken**

1. **Broken symlinks:** `.agents/skills/` contains 8 symlinks pointing to `/home/victor/Documentos/...` but the repo lives at `/mnt/86FC44B0FC449BF5/...`. Any agent trying to load skills via these symlinks will fail.

2. **Stale installer:** `install-skills.sh` references:
   - Old project name "EngramFlow" (should be "FlowForge")
   - Deprecated `.cursorrules` / `.clinerules` approach
   - Non-existent docs (`docs/09-open-source-integration.md`)

3. **Incomplete skill coverage:** `.agents/skills/` only symlinks 8 core skills. The 23 specialized sub-skills (e.g., `forge-arch/security/SKILL.md`) are not separately accessible.

**Impact:** OpenCode and Antigravity agents can't load skills, making them non-functional outside the FlowForge repo itself.

---

## Decision

### Decision 1: Define IDE Pack Parity Contract

**All 4 IDE packs must provide equivalent functionality**, even if implementation differs.

**Parity requirements:**

| Component | Minimum Standard | Notes |
|-----------|------------------|-------|
| **Agent definitions** | Each agent must be self-contained OR have guaranteed skill access | Cursor: self-contained. Others: must fix skill delivery first. |
| **Core skills (7)** | All accessible via symlinks or bundled | Fix broken symlinks, update installer |
| **Specialized skills (24)** | All accessible via parent directory | Don't need individual symlinks — agents load via `skills/forge-X/security/SKILL.md` |
| **Workflows** | All `/flow-*` commands available | Already parity across all packs |
| **Rules** | Core rules (workflow, model-assignments, git-sin-push) present | Already parity |

**Implementation approach:**

- **Cursor:** No changes needed (already rich). Keep as reference implementation.
- **VS Code:** Add concrete output templates to agents (medium priority).
- **OpenCode:** Enhance agents from thin stubs to medium-detail (include output templates, operational rules). OR fix skill delivery so stubs work.
- **Antigravity:** Add per-agent definition files (currently missing). OR fix skill delivery so rules+workflows work.

**Trade-off:** We can either (A) make all agents self-contained like Cursor, or (B) fix skill delivery so thin stubs work. **Decision: Option B for OpenCode/Antigravity** — fix the delivery pipeline first, then enhance agents only if needed. This avoids duplicating skill content across 4 IDE packs.

### Decision 2: Fix Skill Delivery Pipeline

**Immediate fixes (MVP — must-do before Starter Kit):**

1. **Fix broken symlinks:**
   ```bash
   # Remove old symlinks
   rm -rf .agents/skills/*
   
   # Recreate with correct paths
   for skill in skills/*/; do
     ln -s "../../$skill" ".agents/skills/$(basename "$skill")"
   done
   ```
   
   **OR** replace symlinks with a copy step in `install-skills.sh` (more portable, no path issues).

2. **Update `install-skills.sh`:**
   - Replace "EngramFlow" → "FlowForge"
   - Remove `.cursorrules` / `.clinerules` references (deprecated)
   - Remove reference to non-existent `docs/09-open-source-integration.md`
   - Add support for all 4 IDEs (currently only handles Cursor + Copilot)
   - Copy all 31 skills (not just 8 core)

3. **Add skill validation:**
   - `flowforge doctor` (from Starter Kit PRD) should check:
     - All 31 skills accessible
     - No broken symlinks
     - Skills match expected version

**Long-term (post-MVP):**

- Consider bundling skills with binary (like templates in Starter Kit PRD)
- Add `flowforge skills update` command to refresh skills without reinstalling

### Decision 3: Agent Enhancement Strategy

**After fixing skill delivery, evaluate if agents need enhancement:**

| IDE | Current State | After Skill Fix | Enhancement Needed? |
|-----|---------------|-----------------|-----------------------|
| **Cursor** | Rich, self-contained | Works | No — keep as reference |
| **VS Code** | Medium detail | Works | Maybe — add output templates if users report issues |
| **OpenCode** | Thin stubs | Works (can load skills) | Maybe — enhance if skill loading is too slow or unreliable |
| **Antigravity** | No per-agent files | Works (rules + workflows) | Maybe — add per-agent files if users need more guidance |

**Decision:** Fix delivery first, then monitor user feedback. Only enhance agents if:
- Users report confusion or errors
- Skill loading is unreliable
- Specific IDE limitations require it

**Rationale:** Avoids premature optimization. Cursor's rich agents are a luxury, not a requirement. Thin stubs + working skill delivery = functional FlowForge.

---

## Consequences

### Positive

✅ **Parity across IDEs:** All users get equivalent FlowForge experience regardless of IDE  
✅ **Fixed skill delivery:** OpenCode and Antigravity agents become functional  
✅ **Simplified maintenance:** No need to duplicate skill content across 4 IDE packs  
✅ **Clear upgrade path:** Fix delivery → test → enhance only if needed  
✅ **Blocks resolved:** Starter Kit PRD can proceed (was blocked by broken symlinks)

### Negative

⚠️ **Thin stubs may feel limited:** OpenCode/Antigravity users won't have rich output templates unless we enhance later  
⚠️ **Skill loading overhead:** Agents must load skills at runtime (slight delay vs. self-contained)  
⚠️ **Symlink fragility:** Symlinks can break if repo moves (mitigated by `flowforge doctor` check)

### Neutral

🔷 **Cursor remains reference:** Other IDEs will lag behind Cursor in agent richness unless enhanced  
🔷 **Maintenance burden:** Must keep 4 IDE packs in sync (mitigated by parity contract)

---

## Implementation Notes

### Phase 1: Fix Skill Delivery (MVP — 2-3 days)

**Task 1.1: Fix broken symlinks**
```bash
# Option A: Recreate symlinks with correct paths
cd .agents/skills
rm -f *
for skill in ../../skills/*/; do
  ln -s "$skill" "$(basename "$skill")"
done

# Option B: Replace symlinks with copies (more portable)
# Update install-skills.sh to copy instead of symlink
```

**Task 1.2: Update `install-skills.sh`**
- Find/replace "EngramFlow" → "FlowForge"
- Remove `.cursorrules` / `.clinerules` logic
- Add support for OpenCode (`~/.config/opencode/skills/`) and Antigravity (`~/.gemini/antigravity/skills/`)
- Copy all 31 skills (not just 8 core)

**Task 1.3: Add skill validation to `flowforge doctor`**
- Check all 31 skills accessible
- Verify no broken symlinks
- Compare skill versions with expected

### Phase 2: Test Parity (1 week)

**Task 2.1: Test each IDE pack**
- Install FlowForge fresh on each IDE
- Run `/flow-start` → `/flow-close` on a sample project
- Verify all skills load correctly
- Document any issues

**Task 2.2: Gather user feedback**
- Ask 4-person team to test their preferred IDE
- Collect reports on confusion, errors, or missing features
- Prioritize enhancements based on feedback

### Phase 3: Enhance Agents (if needed — post-MVP)

**Task 3.1: Enhance OpenCode agents (if feedback warrants)**
- Add output templates to each agent
- Include operational rules (not just "load skill")
- Target: 50-80 lines per agent (VS Code level)

**Task 3.2: Add Antigravity per-agent files (if feedback warrants)**
- Create `ide/antigravity/agents/forge-*.md` for each agent
- Include role description, operational rules, output templates
- Target: 50-80 lines per agent

**Task 3.3: Enhance VS Code agents (if feedback warrants)**
- Add concrete output templates (spec.md, plan.md examples)
- Target: Match Cursor's level of detail (100+ lines)

---

## Migration Path

**For existing users:**

No migration needed. This ADR fixes bugs and improves quality — no breaking changes.

**For new users (via Starter Kit):**

`flowforge init` will:
1. Install IDE agents (all 4 packs or user-selected)
2. Run `install-skills.sh` (fixed version)
3. Validate with `flowforge doctor`
4. All skills accessible out of the box

---

## References

- [PRD-starter-kit.md](../PRD-starter-kit.md) — Starter Kit PRD (depends on this ADR)
- [ADR-008-ide-installer-path-matrix.md](ADR-008-ide-installer-path-matrix.md) — IDE installer paths
- [ide/README.md](../../ide/README.md) — IDE integration matrix
- [skills/](../../skills/) — All 31 skills
- [.agents/skills/](../../.agents/skills/) — Broken symlinks (to be fixed)
- [install-skills.sh](../../install-skills.sh) — Stale installer (to be updated)

---

## Approval

| Role | Name | Status | Date |
|------|------|--------|------|
| Tech Lead | [Pending] | ⏳ | — |
| IDE Pack Maintainer | [Pending] | ⏳ | — |

---

**Next Steps:**
1. Review and approve this ADR
2. Implement Phase 1 (fix skill delivery) — 2-3 days
3. Test Phase 2 (parity validation) — 1 week
4. Decide Phase 3 (agent enhancements) based on feedback
