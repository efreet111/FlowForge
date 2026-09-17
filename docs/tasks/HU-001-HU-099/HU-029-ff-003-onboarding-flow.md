---
hu_id: HU-029
title: "Onboarding Flow"
status: done
category: feature
flowforge_slug: "hu-029-onboarding-flow"
---

# HU-029 — Onboarding Flow

## User Story
As a new team member, I want a single onboarding command that retrieves relevant project memories and knowledge so that I can understand the project context and start contributing within 5 minutes.

## Acceptance Criteria (business-level)
- [x] AC-1: `flowforge onboard` command is available and executable
- [x] AC-2: Command detects the current project automatically
- [x] AC-3: Command retrieves relevant memories from engram server
- [x] AC-4: New developer receives a briefing with 3+ relevant decisions before writing code
- [x] AC-5: Optional: `--project` flag allows specifying a different project
- [x] AC-6: Optional: `--output` flag allows generating ONBOARDING.md

## Scenarios (SDD Spec)
### Happy Path
- [x] **[Onboard new developer with project detection]**
  **GIVEN** a developer is in a project directory with an engram server configured
  **WHEN** they run `flowforge onboard`
  **THEN** the command detects the project, retrieves memories, and displays a briefing
  **🧪 Ref**: FF-003

- [x] **[Onboard with explicit project flag]**
  **GIVEN** a developer wants to preview a different project's onboarding
  **WHEN** they run `flowforge onboard --project "other-project"`
  **THEN** the command retrieves memories for the specified project
  **🧪 Ref**: FF-003

### Edge Cases
- [x] **[No engram server available]**
  **GIVEN** no engram server is detected or reachable
  **WHEN** `flowforge onboard` is run
  **THEN** the command returns a clear error with setup instructions
  **🧪 Ref**: FF-003

- [x] **[Empty memory state]**
  **GIVEN** a project has no memories in engram
  **WHEN** `flowforge onboard` is run
  **THEN** the command displays a friendly message and suggests next steps
  **🧪 Ref**: FF-003

## Context / Notes
**Problem Statement**: New team members have a disjointed experience — they must install FlowForge, setup their IDE, learn the methodology, AND absorb project knowledge. No single flow ties it together.

**Proposed Solution**: A `flowforge onboard` command that:
1. Detects project's engram server
2. Runs onboarding queries against it
3. Surfaces most relevant memories in developer's first session
4. Optionally generates ONBOARDING.md

**Command Design**:
```
flowforge onboard --user "victor@team.dev" [--project "my-project"]
```

**Onboarding Flow**:
- Project detection → Memory retrieval (mem_context, mem_search, mem_timeline) → Briefing generation (recent sessions, top decisions, conventions, patterns) → Output (CLI interactive or ONBOARDING.md)

**Success Criteria**: New developer runs onboard and gets 5-minute briefing, identifies 3+ relevant decisions before writing first line of code.

**Open Questions**:
- One-time command vs guided flow?
- Multiple projects?
- Filter by recency or relevance?
- Include open questions?
- Handle teams without engram?

## Owner & Timeline
- **Owner**: @equipo-FlowForge
- **Target milestone**: v0.7.0
- **Dependencies**: None (no blockers - uses existing engram tools: mem_context, mem_search, mem_timeline, mem_stats)

## Definition of Done
- [x] `flowforge onboard` command implemented
- [x] Project auto-detection works
- [x] Memory retrieval (mem_context, mem_search, mem_timeline) integration complete
- [x] CLI briefing output displays recent sessions, decisions, conventions, patterns
- [x] `--project` flag functional
- [x] Optional: ONBOARDING.md generation
- [x] Error handling for no engram server scenario
- [x] Unit tests for core onboarding logic

## FlowForge
> This section is managed by FlowForge agents. Do not edit manually.
- HU status lifecycle: `draft` → `in-progress` → `done`
- Status is updated by forge-memory at `/flow-close`

---

## Technical Debt (if applicable)
- ENG-480 (quick-capture CLI) and ENG-481 (git hooks) are optional future enhancements and not in scope for v0.7.0
