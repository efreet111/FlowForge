---
description: Initialize or synchronize AGENTS.md in this project (6-step flow, semantic merge, backup/rollback)
agent: flowforge
---
Initialize or synchronize `AGENTS.md` for: $ARGUMENTS

Load the skill `skills/forge-init/SKILL.md` (contract: `skills/forge-init/contracts.md`) and run its
6-step flow. The engine `skills/forge-init/scripts/fforge-init.sh` performs every file operation —
never hand-edit `AGENTS.md` yourself.

Flow:
1. `detect --json` → existing file? → step 2b (analyze) : step 2a (base template)
2. 2a `recommend --json` for the base profile; 2b `analyze --json` and show every section with its line range
3. Section selection: keep / replace / omit (nothing is deleted without an explicit decision)
4. `classify --from-analysis … --json` — decide every external skill (discard/keep/migrate/review). **Undecided externals block the flow (exit 4).**
5. Fragment selection from `recommend --json` (deselection is respected)
6. Build `plan.json`, run `preview`, show the diff + backup path, ask for confirmation, then `apply`

Flags: `--template <blank|kotlin-android|web|python>`, `--no-interactive`, `--dry-run`, `--verbose`,
`--restore` (roll back from `.agents.md.backup`).

Never skip the confirmation prompt, and never bypass an exit 4/5 by editing the file by hand.
