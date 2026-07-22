# PRD: FlowForge Starter Kit

> **Status:** Decisions Made — Ready for Planning  
> **Priority:** P0 — Critical for adoption  
> **Author:** FlowForge Team  
> **Created:** 2026-01-17  
> **Decisions finalized:** 2026-01-17  
> **Related:** [PRD.md](PRD.md), [ADR-002-scaffold-doc-policy.md](decisions/ADR-002-scaffold-doc-policy.md), [19-project-memory-association-backlog.md](19-project-memory-association-backlog.md)

---

## 1. Problem Statement

### Current Pain Points

**For new users (first-time setup):**
- Multiple installation steps across different scripts (`install.sh`, `ide/install.sh`, `flow-init.sh`)
- No unified "getting started" experience — users must read QUICKSTART.md (314 lines) and manually configure Engram connection
- `.flowforge.json` requires manual editing to set Engram project name, scope, and server URL
- IDE integration is a separate step with its own installer
- No validation that the setup is complete and working

**For existing users (new project):**
- Starting a new project requires re-reading setup docs
- No template for common project types (web app, API, CLI tool, library)
- Project memory association decision (team vs personal) happens implicitly, leading to duplicate projects or accidental team sync (see backlog item)

**For teams:**
- No standardized onboarding for team members joining a FlowForge project
- Each developer configures their environment independently
- No way to share project configuration (Engram project, scope, IDE preferences)

### Impact

- **High friction** for first-time users → low adoption
- **Inconsistent setups** across team members → collaboration issues
- **Duplicate Engram projects** → memory pollution (backlog item)
- **Time waste** → developers spend 30-60 min on setup instead of 5 min

---

## 2. Vision & Goals

### Vision

**"5-minute first flow"** — A new user can go from zero to their first completed FlowForge phase (discovery → spec → plan → dev → verify → memory) in under 5 minutes, with Engram pre-configured and IDE integration working out of the box.

### Goals

| Goal | Metric | Target |
|------|--------|--------|
| Reduce time-to-first-flow | Minutes from install to first `/flow-start` | < 5 min |
| Eliminate manual configuration | % of users who need to edit `.flowforge.json` manually | < 10% |
| Prevent duplicate Engram projects | % of new projects with memory association decision | 100% |
| Standardize team onboarding | Time for a new team member to be productive | < 10 min |
| Increase adoption | % of users who complete first flow | > 80% |

### Non-Goals (v1)

- Multi-repo workspace support (future)
- Cloud-hosted FlowForge instances (future)
- Custom project templates beyond basic types (future)
- Migration from manual setups to starter kit (out of scope)

---

## 3. Target Users

### Primary: New FlowForge Users

- **Profile:** Developer who just heard about FlowForge, wants to try it
- **Technical level:** Comfortable with CLI, Git, and their IDE
- **Goal:** See if FlowForge works for their workflow
- **Pain:** Doesn't want to spend 30 min reading docs before trying

### Secondary: Team Leads

- **Profile:** Tech lead adopting FlowForge for their team
- **Technical level:** Senior developer, cares about consistency
- **Goal:** Standardize onboarding for 5-10 team members
- **Pain:** Each developer configures differently, leading to support overhead

### Tertiary: Existing Users (New Project)

- **Profile:** Already uses FlowForge, starting a new repo
- **Technical level:** Familiar with FlowForge workflow
- **Goal:** Quick setup without re-reading docs
- **Pain:** Forgets exact configuration steps, makes mistakes

---

## 4. User Stories

### Epic 1: First-Time Setup

**US-1.1: Quick Install**
> As a new user, I want to install FlowForge with a single command so I can start using it immediately.

**Acceptance Criteria:**
- [ ] Single command: `curl -sSL https://flowforge.dev/install | bash` (or PowerShell equivalent)
- [ ] Installs `flowforge` binary to `~/.local/bin` (Linux/macOS) or `%LOCALAPPDATA%\flowforge` (Windows)
- [ ] Detects and prompts for IDE (Cursor, VS Code, OpenCode, Antigravity) — or "none"
- [ ] Installs IDE agents + skills automatically
- [ ] Verifies installation with `flowforge --version`
- [ ] Total time: < 2 minutes

**US-1.2: Engram Connection**
> As a new user, I want to configure Engram (local or server) during setup so my memories are stored correctly.

**Acceptance Criteria:**
- [ ] Installer prompts: "Where do you want to store memories?"
  - Option A: Local only (SQLite in `~/.engram/`)
  - Option B: Team server (requires `ENGRAM_SERVER_URL`)
  - Option C: Skip for now (can configure later)
- [ ] If Option B: prompts for server URL, tests connection, stores in `~/.engram/config.json`
- [ ] Generates `mcp_config.json` for IDE integration (Cursor, VS Code, OpenCode)
- [ ] Validates Engram is accessible before proceeding

**US-1.3: First Project**
> As a new user, I want to initialize my first project with sensible defaults so I can start using FlowForge immediately.

**Acceptance Criteria:**
- [ ] Command: `flowforge init` (or `flowforge init my-project`)
- [ ] Prompts for:
  - Project name (defaults to folder name)
  - Project type: Web app / API / CLI tool / Library / Other
  - Engram scope: `team` or `personal` (defaults to `team` if server configured)
  - IDE: Cursor / VS Code / OpenCode / Antigravity / None (auto-detected if possible)
- [ ] Creates:
  - `.flowforge.json` with project config
  - `AGENTS.md` (or `.agents/AGENTS.md` if root already has one)
  - `docs/` directory with FlowDoc v2.0 structure
  - `.ai-work/` directory for session artifacts
  - IDE-specific agent files (if IDE selected)
- [ ] Runs first `/flow-start` automatically (optional, with `--no-start` flag to skip)
- [ ] Prints "Next steps" with links to docs

### Epic 2: Team Onboarding

**US-2.1: Team Project Template**
> As a team lead, I want to create a project template with pre-configured Engram settings so my team starts with consistent configuration.

**Acceptance Criteria:**
- [ ] Command: `flowforge init --template team-web-app`
- [ ] Templates include:
  - `.flowforge.json` with `engram.project`, `engram.scope`, `engram.server_url`
  - `AGENTS.md` with team-specific rules
  - `docs/` with project-specific ADR template
  - `.github/workflows/flowforge-ci.yml` (optional)
- [ ] Templates stored in `~/.flowforge/templates/` (user can create custom ones)
- [ ] Command: `flowforge templates list` shows available templates
- [ ] Command: `flowforge templates create` guides user through template creation

**US-2.2: Team Member Onboarding**
> As a team member joining a FlowForge project, I want to set up my environment automatically so I can start contributing immediately.

**Acceptance Criteria:**
- [ ] Command: `flowforge setup` (run in existing project)
- [ ] Reads `.flowforge.json` from project root
- [ ] Installs IDE agents + skills based on `ide` field
- [ ] Configures Engram connection (prompts for credentials if needed)
- [ ] Validates setup with `flowforge doctor`
- [ ] Total time: < 5 minutes

### Epic 3: Project Memory Association (Backlog Integration)

**US-3.1: First Save Decision**
> As a developer starting a new project, I want to decide whether memories belong to an existing team project, a new team project, or personal work so I don't pollute team memory.

**Acceptance Criteria:**
- [ ] On first `mem_save` in a session (or first save when project has no memories):
  - Orchestrator detects new project (git remote / folder / `ENGRAM_PROJECT`)
  - Searches for similar projects via `mem_search` / `projects list` / `FindSimilar`
  - Presents choices to human:
    - A) Link to existing: `team/flowforge` (if similar found)
    - B) New team project: `team/{detected-name}`
    - C) Personal only: `scope=personal`
    - D) Save once without default (don't ask again this session)
- [ ] Choice persisted in `.flowforge.json` under `engram.project` and `engram.scope`
- [ ] Subsequent saves use chosen project + scope automatically
- [ ] Works with MCP unavailable (fallback: grep `.engram/local_memory/`)

**US-3.2: Project Linking**
> As a developer, I want to link my local project to an existing Engram project so I can reuse memories from previous work.

**Acceptance Criteria:**
- [ ] Command: `flowforge engram link`
- [ ] Lists existing Engram projects (team + personal)
- [ ] User selects project (or types name)
- [ ] Updates `.flowforge.json` with `engram.project` and `engram.scope`
- [ ] Validates link with test save

### Epic 4: Validation & Troubleshooting

**US-4.1: Setup Validation**
> As a user, I want to validate my FlowForge setup so I know everything is working correctly.

**Acceptance Criteria:**
- [ ] Command: `flowforge doctor`
- [ ] Checks:
  - Binary version matches latest release
  - Engram connection (local or server)
  - IDE agents installed correctly
  - Skills accessible (symlinks not broken)
  - `.flowforge.json` valid
  - `.ai-work/` directory exists
- [ ] Prints status table with ✅ / ❌ for each check
- [ ] Suggests fixes for failed checks

**US-4.2: Troubleshooting Guide**
> As a user with a broken setup, I want clear troubleshooting steps so I can fix issues without reading all the docs.

**Acceptance Criteria:**
- [ ] Command: `flowforge troubleshoot`
- [ ] Interactive diagnostic:
  - "What's not working?" (multiple choice)
  - Guides user through fix based on answer
- [ ] Common issues covered:
  - Broken symlinks in `.agents/skills/`
  - Missing Engram config
  - IDE not detecting agents
  - MCP connection failing
- [ ] Links to relevant docs / ADRs

### Epic 5: Migration & Incremental Adoption

**US-5.1: Migration from Manual Setup**
> As an existing FlowForge user with a manual setup, I want step-by-step migration docs so I can move to the starter kit without breaking my current workflow.

**Acceptance Criteria:**
- [ ] Document: `docs/MIGRATION.md` (or section in QUICKSTART)
- [ ] Covers migration from:
  - Manual `.flowforge.json` editing → starter kit auto-config
  - Manual IDE agent installation → `flowforge init` with IDE selection
  - Broken symlinks in `.agents/skills/` → fixed by `install-skills.sh` update
- [ ] Each step is independent (user can stop at any point)
- [ ] Includes validation step (`flowforge doctor`) after each phase

**US-5.2: Incremental Adoption from Other Systems**
> As a developer using another AI workflow (plain Cursor, Cline, Copilot), I want to adopt FlowForge incrementally — starting with FlowDoc, then adding agents, then full flow — so I can migrate at my own pace.

**Acceptance Criteria:**
- [ ] Document: `docs/ADOPTING.md` (or section in QUICKSTART)
- [ ] Defines adoption levels:
  - **Level 0:** No FlowForge (current state)
  - **Level 1:** FlowDoc v2.0 structure only (`docs/` with ADRs, specs)
  - **Level 2:** Add `AGENTS.md` + `.flowforge.json` (agents can read project context)
  - **Level 3:** Add Engram integration (memory persistence)
  - **Level 4:** Full FlowForge flow (`/flow-start` → `/flow-close`)
- [ ] Each level has:
  - Prerequisites (what you need from previous level)
  - Setup steps (commands to run)
  - What you gain (benefits)
  - Example (what a project looks like at this level)
- [ ] User can adopt any level without committing to the next
- [ ] Templates provided for each level (optional starting points)

---

## 5. Requirements

### Functional Requirements

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-1 | Single-command installer (curl | bash) | P0 |
| FR-2 | Interactive setup wizard (Engram, IDE, project) | P0 |
| FR-3 | Project templates (web app, API, CLI, library) | P1 |
| FR-4 | Team onboarding command (`flowforge setup`) | P1 |
| FR-5 | Project memory association gate (first save decision) | P0 |
| FR-6 | Setup validation (`flowforge doctor`) | P1 |
| FR-7 | Troubleshooting guide (`flowforge troubleshoot`) | P2 |
| FR-8 | Template management (`flowforge templates list/create`) | P2 |
| FR-9 | Migration docs (manual setup → starter kit, or other systems → FlowForge) | P1 |

### Non-Functional Requirements

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-1 | Installation time | < 2 minutes (on 10 Mbps connection) |
| NFR-2 | First flow time | < 5 minutes (install → first `/flow-start` complete) |
| NFR-3 | Cross-platform support | Linux, macOS, Windows (PowerShell) |
| NFR-4 | IDE support | Cursor, VS Code, OpenCode, Antigravity |
| NFR-5 | Engram compatibility | Local SQLite + Server (PostgreSQL) |
| NFR-6 | Backward compatibility | Existing manual setups not broken |
| NFR-7 | Documentation | Every command has `--help` with examples |
| NFR-8 | Error handling | Clear error messages with suggested fixes |

---

## 6. Success Criteria

### Quantitative

| Metric | Baseline | Target | Measurement |
|--------|----------|--------|-------------|
| Time-to-first-flow | ~30 min (estimated) | < 5 min | User testing (5 participants) |
| Setup completion rate | ~60% (estimated) | > 90% | Survey post-setup (5 participants) |
| Manual config edits | ~80% of users | < 10% | Survey post-setup |
| Duplicate Engram projects | Unknown | 0 | Engram server logs |
| Team onboarding time | ~30 min | < 10 min | User testing (3 teams) |

### Qualitative

- [ ] New users report "setup was easy" in survey (≥ 4/5 rating)
- [ ] No support tickets about "how to configure Engram"
- [ ] Team leads report "onboarding is standardized"
- [ ] Documentation is only consulted for advanced features, not basic setup

---

## 7. Scope

### MVP (v1.0)

**Must-have:**
- FR-1: Single-command installer
- FR-2: Interactive setup wizard (Engram + IDE)
- FR-5: Project memory association gate
- FR-9: Migration docs (manual setup + incremental adoption)
- NFR-1, NFR-3, NFR-4: Performance, cross-platform, IDE support

**Deliverables:**
- `install/install.sh` (enhanced) + `install/install.ps1`
- `flowforge init` command (enhanced `flow-init.sh`)
- Project memory association logic in orchestrator
- `.flowforge.json` schema update (add `engram.project`, `engram.scope`)
- `flowforge doctor` command (basic checks)
- Updated QUICKSTART.md (simplified, < 100 lines)
- `docs/MIGRATION.md` (manual setup → starter kit)
- `docs/ADOPTING.md` (incremental adoption levels 0-4)

**Timeline:** 2-3 weeks

### v1.1 (Post-MVP)

**Should-have:**
- FR-3: Project templates
- FR-4: Team onboarding command
- FR-6: Enhanced `flowforge doctor` (comprehensive checks)
- NFR-7, NFR-8: Documentation, error handling

**Deliverables:**
- `flowforge templates` command
- `flowforge setup` command
- Template library (3-5 common project types)
- Troubleshooting guide

**Timeline:** 1-2 weeks after MVP

### v1.2 (Future)

**Nice-to-have:**
- FR-7: Interactive troubleshooting
- FR-8: Template creation wizard
- Cloud-hosted FlowForge instances
- Multi-repo workspace support

**Timeline:** TBD based on user feedback

---

## 8. Risks & Mitigations

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Installer breaks on edge cases (old OS, missing deps) | Medium | High | Comprehensive testing matrix (Ubuntu 20.04+, macOS 12+, Windows 10+); fallback to manual instructions |
| Engram server connection fails during setup | Medium | Medium | Graceful degradation: allow "local only" mode; validate connection before proceeding |
| IDE agents conflict with existing user config | Low | Medium | Backup existing config before install; provide `--force` flag; clear rollback instructions |
| Project memory association gate annoys users | Low | High | Make it skippable (Option D); remember choice in `.flowforge.json`; don't repeat unless config changes |
| Templates become outdated | Medium | Low | Version templates with FlowForge; provide `flowforge templates update` command; community contributions |
| Backward compatibility breaks existing setups | Low | High | Detect existing config; prompt before overwriting; provide migration guide |

---

## 9. Dependencies

### Internal

- **Engram MCP tools:** `mem_save`, `mem_search`, `projects list` (already available)
- **FlowForge orchestrator:** Project memory association logic (new, depends on backlog item)
- **IDE packs:** Agent files for Cursor, VS Code, OpenCode, Antigravity (already exist, need parity fix — see Action 1)

### External

- **GitHub Releases:** For binary distribution (already set up)
- **Engram server:** Optional, for team sync (user-provided)
- **IDE CLI tools:** `cursor`, `code`, `opencode` (user-installed)

### Blockers

- **Broken symlinks in `.agents/skills/`** — must fix before starter kit can guarantee skills are accessible (see Action 1 evaluation)
- **Stale `install-skills.sh`** — must update or replace before starter kit can use it

---

## 10. Decisions Made

| # | Question | Decision | Rationale |
|---|----------|----------|-----------|
| 1 | **Telemetry:** Collect opt-in data on setup completion / time-to-first-flow? | ❌ **No telemetry. Use surveys instead.** | Privacy-first approach. Surveys provide qualitative feedback without tracking. |
| 2 | **Default Engram scope:** Should `team` be the default or prompt every time? | ✅ **`team` by default** (current behavior) | Convenience for team workflows. Can be refined later based on user feedback. |
| 3 | **Template storage:** Bundled, registry, or both? | ✅ **Bundled with binary** | Simple, no infrastructure needed. 4-person team, OSS launch imminent. Registry can be added later when community grows. |
| 4 | **IDE auto-detection:** Detect installed IDEs or always prompt? | ✅ **Ask the user** (don't auto-detect) | Simpler, more reliable. Avoids edge cases with IDE detection. |
| 5 | **Migration path:** `flowforge migrate` command or docs only? | ✅ **Docs only for now** (manual migration) | Low user count (4 people). Can walk team through it. Add `flowforge migrate` command later if needed (v1.2). |
| 6 | **Adoption strategy:** All-or-nothing or incremental? | ✅ **Incremental adoption** (5 levels) | Users can adopt FlowDoc first, then agents, then full flow. Reduces friction, allows gradual commitment. |

---

## 11. Appendix

### A. Current Installer Flow (As-Is)

```
1. Download binary: curl -sSL https://flowforge.dev/install | bash
2. Run wizard: flowforge install
3. Configure Engram: edit ~/.engram/config.json manually
4. Initialize project: flowforge init
5. Edit .flowforge.json: set engram.project, engram.scope
6. Install IDE agents: bash ide/install.sh
7. Install skills: bash install-skills.sh
8. Read QUICKSTART.md: 314 lines
9. Try first /flow-start
```

**Pain points:** 9 steps, multiple manual edits, no validation, ~30 min total

### B. Proposed Installer Flow (To-Be)

```
1. Install: curl -sSL https://flowforge.dev/install | bash
2. Wizard:
   - Choose IDE
   - Configure Engram (local / server / skip)
   - Initialize project (name, type, scope)
3. Validate: flowforge doctor
4. Start: flowforge start (or open IDE)
```

**Improvements:** 4 steps, no manual edits, validation included, ~5 min total

### C. `.flowforge.json` Schema Update

```json
{
  "version": "0.5.0",
  "docs_framework": "flowdoc",
  "docs_framework_version": "2.0",
  "engram": {
    "project": "my-app",
    "scope": "team",
    "server_url": "https://engram.example.com"
  },
  "ide": "cursor",
  "teacher_mode": false
}
```

**New fields:** `engram.project`, `engram.scope`, `engram.server_url`, `ide`

### D. Related Documents

- [PRD.md](PRD.md) — Main FlowForge PRD
- [ADR-002-scaffold-doc-policy.md](decisions/ADR-002-scaffold-doc-policy.md) — Scaffold documentation policy
- [ADR-008-ide-installer-path-matrix.md](decisions/ADR-008-ide-installer-path-matrix.md) — IDE installer path matrix
- [19-project-memory-association-backlog.md](19-project-memory-association-backlog.md) — Project memory association backlog
- [QUICKSTART.md](../QUICKSTART.md) — Current quickstart guide

---

## 12. Approval

| Role | Name | Status | Date |
|------|------|--------|------|
| Product Owner | [Pending] | ⏳ | — |
| Tech Lead | [Pending] | ⏳ | — |
| UX Review | [Pending] | ⏳ | — |

---

**Next Steps:**
1. ✅ Review and approve this PRD
2. ✅ Finalize open questions (decisions made — see Section 10)
3. ⏭️ Break down into tasks (`/flow-plan` → plan.md)
4. Implement MVP (2-3 weeks)
5. User testing (5 participants)
6. Iterate based on feedback
7. Release v1.0
