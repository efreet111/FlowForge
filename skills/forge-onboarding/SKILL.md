# forge-onboarding Skill

**Phase**: Cross-cutting (applies to all phases)
**CKP**: N/A (user-facing command, not a workflow phase)
**Trigger**: User is new to the project, returning after a break, or switching projects

---

## Purpose

This skill teaches AI agents how to help users leverage the `flowforge onboard` command to get up to speed with a project quickly. The command generates a briefing from engram memories (decisions, patterns, blockers) that would otherwise take hours to discover manually.

---

## When to Use

### Detection Signals

Apply this skill when you observe any of these signals:

| Signal | Example User Input | Action |
|--------|-------------------|--------|
| **New user** | "How do I get started?" / "What should I know?" | Suggest running `flowforge onboard` |
| **Long break** | User hasn't worked on this project in >7 days | Suggest `flowforge onboard` to refresh context |
| **Project switch** | User is switching to a different project in the same team | Suggest `flowforge onboard --project <name>` |
| **No ONBOARDING.md** | No `ONBOARDING.md` exists in the repo | Suggest generating one with `--output` |
| **Major feature** | User is about to start a large feature | Suggest reviewing decisions first |

### When NOT to Use

- User is in the middle of a task (don't interrupt flow)
- User explicitly says "I know this project" or "skip onboarding"
- User has run `flowforge onboard` recently (check for recent ONBOARDING.md timestamp)
- Solo dev working on their own project (less critical, but still useful after breaks)

---

## What is `flowforge onboard`?

The `flowforge onboard` command generates a first-day briefing from engram memories. It shows:

1. **Recent Activity** — Last 5 sessions and 20 observations from the project
2. **Key Architectural Decisions** — Top 10 decisions (`type=decision`)
3. **Team Conventions & Patterns** — Reusable patterns (`type=pattern`)
4. **Known Blockers / Gotchas** — Issues to avoid (`type=bugfix` / `type=manual`)

The command is **read-only** (no side effects) and supports both interactive and CI-safe modes.

---

## How to Help Users

### Step 1: Detect the Need

Check if the user is:
- New to the project (first session)
- Returning after a break (>7 days)
- Switching to a different project
- About to start a major feature

### Step 2: Suggest the Command

**Template response**:
```
Before we start, you might want to run `flowforge onboard` to get a briefing
from the team's memories. It will show you:
- Recent activity (last sessions and observations)
- Key architectural decisions (type=decision)
- Team conventions and patterns (type=pattern)
- Known blockers and gotchas

Run: flowforge onboard --project <project-name>

Or export to ONBOARDING.md for the team:
flowforge onboard --project <project-name> --output ONBOARDING.md
```

### Step 3: Help Interpret the Output

After the user runs the command, help them understand:

| Section | What it Means | Action |
|---------|---------------|--------|
| **Recent Activity** | What the team has been working on | Understand current state |
| **Key Decisions** | Architectural choices made | Follow these patterns |
| **Conventions** | Team coding standards | Adhere to these |
| **Blockers** | Known issues to avoid | Don't repeat these mistakes |

### Step 4: Offer Drill-Down

If the user wants to explore a specific decision or pattern:
```
You can drill down into any item by typing its number in the interactive prompt.
For example, type "1" to see the full content of the first decision.
Type "1t" to see the timeline (context around that decision).
```

### Step 5: Suggest Export (Optional)

If the team doesn't have an ONBOARDING.md yet:
```
You can export the briefing to ONBOARDING.md for the team:
flowforge onboard --project <project-name> --output ONBOARDING.md

This file can be committed to the repo so new team members can read it.
```

---

## Command Reference

### Basic Usage

```bash
# Interactive briefing (detects project from .flowforge.json or git)
flowforge onboard

# Specify project
flowforge onboard --project flowforge

# Export to markdown
flowforge onboard --project flowforge --output ONBOARDING.md

# CI-safe mode (no interactive prompts)
flowforge onboard --no-interactive --limit 5
```

### Flags

| Flag | Type | Default | Description |
|------|------|---------|-------------|
| `--project <name>` | string | auto-detected | Override project name |
| `--user <handle>` | string | from config | Display-only user name |
| `--scope team\|personal` | string | team | Memory scope |
| `--output <path>` | string | none | Export to ONBOARDING.md |
| `--limit <n>` | int | 10 | Max items per section (1..20) |
| `--no-interactive` | bool | false | CI-safe mode |

### Exit Codes

| Code | Meaning |
|------|---------|
| 0 | Success |
| 1 | Runtime error (ambiguous project in non-interactive mode) |
| 2 | Pre-check failure (engram binary missing, config unreadable) |

---

## Prerequisites

For `flowforge onboard` to work:

1. **engram installed**: `flowforge install` must have been run
2. **sync configured**: `~/.engram/config.json` must have `sync.user` set
3. **memories captured**: The project must have some decisions/patterns in engram

If any prerequisite is missing, the command will exit with code 2 and show actionable hints.

---

## Example Session

**User**: "How do I get started with this project?"

**Agent**:
```
Before we start, you might want to run `flowforge onboard` to get a briefing
from the team's memories. It will show you:
- Recent activity (last sessions and observations)
- Key architectural decisions (type=decision)
- Team conventions and patterns (type=pattern)
- Known blockers and gotchas

Run: flowforge onboard

This will detect the project automatically from .flowforge.json or git.
```

**User**: (runs the command, sees the briefing)

**Agent**:
```
Great! Now you have a good overview of the project. Let me highlight the key points:

1. **Decision #754**: PM-* manual tests were deferred as tech debt — this is intentional, not a bug.
2. **Pattern #753**: When merging CHANGELOG conflicts, keep both entries (don't drop one).
3. **No blockers found** — the project is in good shape.

Want to drill down into any of these? Type the number in the interactive prompt.
```

---

## Troubleshooting

### "No memories found for team/<project>"

**Cause**: The project has no decisions/patterns captured yet.

**Solution**:
1. Run `flowforge install` to set up the project
2. Start capturing decisions using the FlowForge methodology
3. Re-run `flowforge onboard` after a few sessions

### "engram binary — ✗ FAIL"

**Cause**: engram is not installed.

**Solution**: Run `flowforge install` to install engram-dotnet.

### "engram config — ✗ FAIL"

**Cause**: `~/.engram/config.json` is missing or unreadable.

**Solution**: Run `flowforge install` or `flowforge config set sync.user <email>`.

### "Ambiguous project: pass --project to disambiguate"

**Cause**: Multiple child git repos detected, can't determine which project.

**Solution**: Specify the project explicitly: `flowforge onboard --project <name>`.

---

## Related Documentation

- **Technical reference**: [`docs/installer-baseline.md`](../../docs/installer-baseline.md)
- **Feature spec**: [`docs/backlog/FF-003-onboarding-flow/spec.md`](../../docs/backlog/FF-003-onboarding-flow/spec.md)
- **Complete reference**: [`docs/14-flowforge-complete-reference.md`](../../docs/14-flowforge-complete-reference.md)

---

## Memory Signal

- **type**: pattern
- **significance**: medium
- **summary**: forge-onboarding skill teaches agents to suggest `flowforge onboard` when users are new, returning after a break, or switching projects. The command generates a briefing from engram memories (decisions, patterns, blockers) to accelerate onboarding. Agents should help users interpret the output and optionally export to ONBOARDING.md for the team.
