# FF-003: Onboarding Flow Integration

**Status**: 🟢 Ready  
**Priority**: P1  
**Effort**: M (1-2 days)  
**Origin**: FF-IDEA-003  

---

## Problem Statement

New team members joining a FlowForge-using team have a disjointed experience: they install FlowForge, set up the IDE, learn the methodology, AND need to absorb project knowledge. No single flow ties it together.

## Proposed Solution

A `flowforge onboard` command (or guided flow) that:

1. Detects the project's engram server
2. Runs onboarding queries against it
3. Surfaces the most relevant memories in the developer's first session
4. Optionally, the Orchestrator suggests "based on past decisions, here's what you should know about this codebase"

### Command Design

```bash
flowforge onboard --user "victor@team.dev" [--project "my-project"]
```

### Onboarding Flow

1. **Project detection**: Identify current project (working directory → `.flowforge.json` → project name)
2. **Memory retrieval**:
   - `mem_context` → recent sessions summary
   - `mem_search` with project keywords → relevant decisions, conventions, patterns
   - `mem_timeline` → recent activity in the project
3. **Briefing generation**:
   - Last 5 sessions relevant to the project
   - Top 10 architectural decisions (type: `decision`)
   - Team conventions (type: `convention`)
   - Reusable patterns (type: `pattern`)
4. **Output**:
   - Interactive CLI briefing (show memories, allow drill-down)
   - Optional: Generate `ONBOARDING.md` with summary

## Success Criteria

- A new developer runs `flowforge onboard` and gets a 5-minute briefing
- They identify 3+ relevant past decisions before writing their first line of code
- FlowForge's adoption story includes "onboarding is now 2 weeks faster"
- Qualitative feedback: "I feel up to speed faster"

## Dependencies

### engram-dotnet (no blockers)

- `mem_context` — already exists
- `mem_search` — already exists
- `mem_timeline` — already exists
- `mem_stats` — already exists (for project overview)

### FlowForge (no blockers)

- Existing team-mode setup
- Orchestrator agent's onboarding skill (or new one)

### Optional (recommended but not required)

- **ENG-480** (Quick-capture CLI) — so teams have more memories to surface
- **ENG-481** (Git hooks integration) — so memories are captured automatically

## Effort Breakdown

| Component | Effort | Notes |
|-----------|--------|-------|
| Command scaffolding (`flowforge onboard`) | S (2-4 hours) | CLI parsing, help text |
| Project detection logic | S (2-4 hours) | Read `.flowforge.json`, detect project name |
| Memory retrieval orchestration | M (4-8 hours) | Call `mem_context`, `mem_search`, `mem_timeline`, aggregate results |
| Briefing generation (CLI output) | M (4-8 hours) | Format memories, interactive drill-down |
| Optional: `ONBOARDING.md` generation | S (2-4 hours) | Markdown template, write file |
| Testing + documentation | S (2-4 hours) | Unit tests, user guide |
| **Total** | **M (1-2 days)** | Implementable today |

## Open Questions

1. Should the onboarding be a one-time command (`flowforge onboard`) or a guided flow in the Orchestrator (`/flow-onboard`)?
2. How to handle multiple projects (developer works on 3 FlowForge projects)?
3. Should we filter memories by recency (last 30 days) or relevance (all-time)?
4. Should the briefing include "open questions" or "known issues" from past sessions?
5. How to handle teams that don't use engram (no memories to surface)?

## Implementation Plan

### Phase 1: Command + Project Detection (S)

```csharp
// Pseudocode
public class OnboardCommand
{
    public async Task Execute(string user, string? project)
    {
        var projectConfig = LoadProjectConfig(); // .flowforge.json
        var projectName = project ?? projectConfig.Engram.Project;
        
        AnsiConsole.MarkupLine($"[blue]Onboarding to project: {projectName}[/]");
        AnsiConsole.MarkupLine($"[blue]User: {user}[/]");
        
        // Retrieve memories
        var context = await engramClient.MemContext();
        var decisions = await engramClient.MemSearch($"project:{projectName} type:decision");
        var conventions = await engramClient.MemSearch($"project:{projectName} type:convention");
        var patterns = await engramClient.MemSearch($"project:{projectName} type:pattern");
        
        // Generate briefing
        GenerateBriefing(context, decisions, conventions, patterns);
    }
}
```

### Phase 2: Memory Retrieval + Aggregation (M)

- Call `mem_context` → get recent sessions
- Call `mem_search` with filters:
  - `project:{name} type:decision` → architectural decisions
  - `project:{name} type:convention` → team conventions
  - `project:{name} type:pattern` → reusable patterns
- Aggregate results, rank by recency + relevance

### Phase 3: Briefing Generation (M)

```
╭──────────────────────────────────────────────────────────╮
│  FlowForge Onboarding: MyProject                         │
│  User: victor@team.dev                                   │
╰──────────────────────────────────────────────────────────╯

## Recent Activity (last 5 sessions)
- 2026-08-13: Implemented update mechanism (PR #24)
- 2026-08-12: Fixed installer regression (rework cycle 2)
- 2026-08-11: Designed onboarding flow (FF-003)
- ...

## Key Architectural Decisions (top 10)
1. [DECISION] Use C# .NET AOT for installer (ADR-001)
2. [DECISION] Manifest-based compatibility checks (ADR-002)
3. [DECISION] Sidecar managed-paths for IDE configs (ADR-016)
4. ...

## Team Conventions
- [CONVENTION] All JSON via source-gen (no reflection)
- [CONVENTION] Atomic writes for config files
- [CONVENTION] Backup before overwrite
- ...

## Reusable Patterns
- [PATTERN] Merge quirúrgico de MCP configs (JsonNode surgical replacement)
- [PATTERN] Health-check post-update (binary --version, MCP parse, doctor subset)
- ...

Press [Enter] to exit, or type a decision number to drill down:
```

### Phase 4: Optional ONBOARDING.md Generation (S)

```markdown
# Onboarding: MyProject

> Generated: 2026-08-14
> User: victor@team.dev

## Recent Activity
- ...

## Key Decisions
1. ...

## Conventions
- ...

## Patterns
- ...

## Next Steps
1. Read ADR-001 (installer architecture)
2. Review spec.md from last 3 features
3. Run `flowforge status` to check current state
```

## Risk Assessment

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| Team doesn't use engram (no memories) | Medium | High | Detect early, show message: "No memories found. Start by running `flowforge install` and using the methodology." |
| Too many memories (overwhelming) | Low | Medium | Limit to top 10 per category, allow drill-down |
| Memories are outdated | Medium | Medium | Show timestamps, warn if > 6 months old |
| Developer doesn't run onboarding | Low | Low | Document in README, suggest in `/flow-start` |

## Recommended Path

1. **Implement now** — no blockers, high value for teams
2. **Start with CLI command** (`flowforge onboard`) — simpler than Orchestrator integration
3. **Add Orchestrator integration later** — `/flow-onboard` as a skill
4. **Monitor usage** — if teams use it, enhance with more filters (by module, by timeframe)

## Related Features

- **FF-001** (Dev agent code-aware) — onboarding could surface code-aware memories (if ENG-484 is ready)
- **FF-002** (Decision extraction) — onboarding benefits from captured decisions
- **FF-005** (Contradiction detection) — onboarding could warn about contradictory decisions

---

**Next step**: Implement. Create `/flow-start ff-003-onboarding-flow` when ready.
