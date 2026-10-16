# `/fforge-init` — initialize or synchronize `AGENTS.md`

A FlowForge skill that writes a project's `AGENTS.md` **without losing what the project already
had**. It runs a 6-step flow, asks the human for every decision, and hands the file work to a
zero-dependency engine.

- **Skill id:** `forge-init` (directory name matters — `install-skills.sh` discovers `skills/forge-*/`)
- **Command:** `/fforge-init`
- **Phase:** on-demand — **no CKP gate**
- **Runtime:** `bash` 4+, `awk`, `sed`, `grep`, `find`, `sha256sum`. **No** node, **no** npm,
  **no** `jq`, **no** network, **no** git (NFR-006).
- **Contract (source of truth):** [`contracts.md`](contracts.md) — C-1 … C-10

## What it does, in one diagram

```
/fforge-init
   │
   ├─ [1/6] DETECTION      ── engine: detect --json
   │                                │
   │                  absent ───────┴─────── present
   │                    │                      │
   ├─ [2a/6] BASE TEMPLATE            ├─ [2b/6] ANALYSIS (sections + line ranges + skills)
   │        engine: recommend --json   │        engine: analyze --json
   │                                   │
   ├─ [3/6] SECTION SELECTION ─────────┘   keep / replace / omit  (nothing dropped implicitly)
   │
   ├─ [4/6] SKILL CLASSIFICATION       forge-* · flowdoc-* · reginald-* → FlowForge
   │        engine: classify --json    everything else → EXTERNAL  → discard/keep/migrate/review
   │                                   ⚠ undecided externals BLOCK the flow (exit 4)
   │
   ├─ [5/6] FRAGMENT SELECTION         stack + functionality + agent fragments
   │        engine: recommend --json   (recommendations are pre-checks; deselection is respected)
   │
   └─ [6/6] MERGE & APPLY              plan.json → preview → confirm → apply
            engine: preview/apply      backup → atomic write → managed block with body hash
```

The 6 steps are described in detail in [`SKILL.md`](SKILL.md); the schemas and invariants behind
them are in [`contracts.md`](contracts.md).

## Flags

| Flag | Effect |
|------|--------|
| `--template <name>` | Skip the step-2a menu and use that base profile: `blank`, `kotlin-android`, `web`, `python` |
| `--no-interactive` | Answer with the engine's recommendations. It does **not** remove the confirmation before the write |
| `--dry-run` | Run every step and the full merge, but create no backup and write nothing (exit 0) |
| `--verbose` | Print a per-section and per-fragment trace |
| `--restore` | Roll back from `.agents.md.backup` and consume the backup |

## Recipes

### See what would change, change nothing

```bash
/fforge-init --dry-run
# or, once a plan exists:
skills/forge-init/scripts/fforge-init.sh preview --config plan.json
```

`--dry-run` and `preview` run the **identical code path** as `apply` (DEC-4): same analysis, same
merge, same managed-block hash. They only skip the backup and the write. That is what makes the
preview trustworthy.

### Roll back

```bash
/fforge-init --restore
# or:
skills/forge-init/scripts/fforge-init.sh restore --root .
```

`restore` copies `.agents.md.backup` over `AGENTS.md` and then **removes the backup** — a successful
restore consumes it, so the next write starts a fresh one. There is nothing to restore after that;
run the flow again to create a new backup.

### Drive the engine directly (scripting / CI)

```bash
E=skills/forge-init/scripts/fforge-init.sh
$E detect    --json                        # {root, exists, path, sizeBytes}
$E analyze   --json > analysis.json        # sections, skills, sha256  (C-2)
$E classify  --from-analysis analysis.json --json
$E recommend --json                        # base + fragments + why      (C-6)
$E preview   --config plan.json --json     # dry run, machine readable
$E apply     --config plan.json            # backup + atomic write
$E apply     --config plan.json --dry-run  # identical path, no write
$E restore   --root .
```

Every subcommand accepts `--json` (CONV-1); the human output mirrors HU-047 §4.

## The backup: where it lives and when it is overwritten

| | |
|---|---|
| Path | **`./.agents.md.backup`** — hidden, project-local (spec NFR-004 / OQ-3) |
| Created | Before the first byte of `AGENTS.md` changes |
| Overwritten | **Only after a fully successful write.** The new content is staged as `.agents.md.backup.tmp` and renamed over the old backup at the very end, so a failure leaves the *previous* backup intact |
| Removed | By a successful `--restore` (PM-5); never by a failed run |
| In `mode: init` | Not created — there was nothing to lose |

> **Why not `~/.flowforge-backups/`?** That is the *installer's* backup root, and the installer owns
> the **global** `~/.config/opencode/AGENTS.md`. This skill owns the **project-local** `./AGENTS.md`,
> so its rollback artifact travels with the project and is discoverable by the developer who ran the
> command. The divergence is deliberate (DEC-5) — see [`contracts.md`](contracts.md) §"Naming".

## Troubleshooting

One row per frozen exit code (C-8). Every failure prints *what* failed, *why*, and *how to fix it*.

| Exit | Symptom | Cause | Fix |
|------|---------|-------|-----|
| `0` | — | success, dry-run, or an idempotent no-op | nothing to do |
| `1` | `unknown argument '--foo' for 'apply'` | a flag the engine does not know, or a missing required argument (`apply` without `--config`) | run `fforge-init.sh help`; `preview`/`apply` always need `--config` |
| `2` | `no AGENTS.md found at '…'` / `no backup found at '…'` | you asked to analyse or restore something that is not there | run `detect` first: if the file is absent, use `mode: init` (step 2a); a restore needs a backup that a successful `apply` created |
| `3` | `escapes the project root` | `--file`/`target` points outside `--root` | keep the target inside the project, or pass the right `--root` |
| `3` | `is a symbolic link` | the target is a symlink | replace it with a regular file (a symlink could be rewritten outside the project) |
| `3` | `is … bytes (limit 1048576)` | the file is larger than 1 MB | split the file or move the bulk elsewhere; 1 MB is the `AgentsMdMerger` guard |
| `3` | `contains N NUL byte(s)` / `contains U+FFFD` | the file is binary or corrupt UTF-8 | restore it from git and re-encode as UTF-8 |
| `3` | `cannot write to '…'` / `is not writable` | directory or file permissions deny the write | `chmod u+w` the directory/file (or run in a project you own) |
| `4` | `⚠ N unresolved external skill(s)` | at least one external skill has no decision (or an explicit `review`) | decide every external skill in step 4; `review` also needs `--allow-review-todos` |
| `5` | `key "…": existing="…" template="…"` | the original and the template disagree on the same list item | ask the human, then add `{ "key": …, "resolution": "existing" \| "template" \| "<literal>" }` to `plan.json.conflicts` |
| `5` | `the managed block has drifted from its recorded hash` | someone hand-edited the generated region | ask the human: `"resolution": "existing"` keeps their version, `"template"` regenerates it (add it under the `managed-block` key in `plan.json.conflicts`) |
| `5` | `contains N managed blocks` | two or more `<!-- fforge-init:begin -->` blocks | keep exactly one block, or delete both to re-adopt the file as hand-written |
| `5` | `the target changed since the plan was created` | the file changed between analysis and apply (another tool, another person) | re-run `analyze` and rebuild `plan.json` |
| `6` | `sections.keep names '…', which the analysis did not find` | a typo, or a plan built against an older file | use one of the section names the error lists |
| `6` | `'X' appears in more than one of keep/replace/omit` | two decisions for one section | keep only one |
| `6` | `invalid mode '…'` / `mode 'init' but … already exists` | `plan.json` does not match the filesystem | `init` requires an absent target, `sync` requires a present one |
| `6` | `unknown base profile '…'` | the profile is not `blank`/`kotlin-android`/`web`/`python` | pick one of the four |
| `6` | `fragment '…' does not exist` | a path outside `templates/fragments/` | run `recommend --json` and use a path it lists |
| `6` | `is not valid JSON` | malformed `plan.json` | re-generate it from `analyze --json` |
| `7` | `could not write …` / `replacing … failed` | the write failed halfway (disk full, I/O error) | free space and retry: the target was restored from the staged backup and the previous backup is intact |

## V1 / V2 boundary for agent fragments

`spec.md` §1 defers deep Claude Code / Cursor support to V2, while FR-009 requires the flow to
support all three agents. The resolution (§6 Resolution 2 of the plan):

| Fragment | V1 status |
|----------|-----------|
| `agent/opencode.md.fragment` | **Full** — subagent-first delegation, `metadata.opencode/autoinvoke: false`, skill loading order, `~/.config/opencode/skills/` |
| `agent/claude-code.md.fragment` | Minimal but valid (≈20 lines, independently combinable) — `CLAUDE.md` pointer rules |
| `agent/cursor.md.fragment` | Minimal but valid — `ide/cursor/agents/` pre-compiled behaviour + `.cursor/rules/*.mdc` precedence |

The two minimal fragments carry an in-place `<!-- V2: … -->` marker so the deferral is discoverable
where it matters. Deep per-agent optimisation is HU-047 §10 Fase 2.

## Extending the skill

Read [`contracts.md`](contracts.md) before touching anything.

- **New fragment** → create `templates/fragments/<kind>/<id>.md.fragment` with the C-6 frontmatter
  (`id`, `kind`, `label`, `detect`, `conflicts`, `recommendedWith`, `version`). Bodies must be inert
  markdown: **no** code fences, **no** placeholders, **no** reference to another fragment (CONV-2),
  and a **distinct H2 heading** so any subset combines cleanly (NFR-005).
- **New base profile** → add it to `templates/base/base.meta.json` → `profiles` (exactly one of the
  four step-2a menu entries may be added; keeping the names stable keeps `plan.json` portable).
- **New engine behaviour** → update `contracts.md` **first**, then `scripts/fforge-init.sh`, then
  `SKILL.md`, then the tests in `tests/fforge-init/run-tests.sh` and
  `tests/FlowForge.Installer.Tests/FforgeInitScriptTests.cs`. The exit-code table is frozen.

## Tests

```bash
# engine behaviour (bash only — same runtime as the engine)
./tests/fforge-init/run-tests.sh            # 212 assertions, TAP-ish summary
./tests/fforge-init/run-tests.sh --verbose  # echo every command it runs

# structural contract tests
dotnet test tests/FlowForge.Installer.Tests --filter FforgeInitScriptTests
```

The harness covers FR-001…FR-009, PM-1…PM-5, the exit-code contract 0–7, the security guards
(path escape, symlink, > 1 MB, NUL), idempotency, byte-exact rollback, failure atomicity and the
NFR-001 timing bounds. It also proves NFR-005 by combining **all 55 fragment pairs** and validating
the resulting markdown independently of the generator.
