# `/fforge-init` — Contracts (single source of truth)

> **This file is the frozen interface.** `SKILL.md` (the agent's instructions) and
> `scripts/fforge-init.sh` (the deterministic engine) both cite it. If they disagree,
> the engine is wrong unless this file says otherwise — and if this file is wrong, fix
> *this file first*, then both consumers.

**Status:** frozen at CKP-2 (plan `fforge-init`, HU-047).
**Version:** 1.0.0 — see [`SKILL.md`](SKILL.md) frontmatter.

## Consumers

| Contract | Consumed by | Where |
|----------|-------------|-------|
| C-1 Skill frontmatter | `install-skills.sh` (glob + autoinvoke step), OpenCode skill loader | `SKILL.md`, this file |
| C-2 Section model | `analyze`, `preview`, `apply` | `scripts/fforge-init.sh` |
| C-3 Merge config (`plan.json`) | `preview`, `apply`, the agent building the plan | `scripts/fforge-init.sh` |
| C-4 Classification | `classify`, `SKILL.md` step 4 | `scripts/fforge-init.sh` |
| C-5 Managed-block markers | `apply`, `preview` (drift), `restore` | `scripts/fforge-init.sh` |
| C-6 Fragment manifest | `recommend`, `SKILL.md` step 5, fragment authors | `templates/fragments/**` |
| C-7 Base template + placeholders | `apply`, the agent selecting a base | `templates/base/**` |
| C-7a Skills union | `apply` | `scripts/fforge-init.sh` |
| C-7b Title rule | `apply` | `scripts/fforge-init.sh` |
| C-8 Engine CLI + exit codes | `SKILL.md`, `README.md`, `tests/fforge-init/run-tests.sh` | `scripts/fforge-init.sh` |
| C-9 Backup / rollback | `apply`, `restore`, `README.md` | `scripts/fforge-init.sh` |
| C-10 Source-changed guard / reproducibility | `apply`, `preview` | `scripts/fforge-init.sh` |

---

## C-1. Skill frontmatter contract

```yaml
---
name: forge-init
description: Initialize or synchronize AGENTS.md in a project — 6-step interactive flow with
  semantic merge, skill classification (FlowForge vs external), fragment composition,
  backup/restore rollback and dry-run.
trigger: "fforge-init, init AGENTS.md, inicializar AGENTS.md, sync AGENTS.md, adopt FlowForge in project"
version: "1.0.0"
metadata:
  opencode/autoinvoke: false   # required by install-skills.sh Step 4 (HU-045)
---
```

- `name` MUST equal the directory name (`forge-init`) — the `install-skills.sh` copy loop and OpenCode both resolve by directory name (CONV-3).
- The slash command is `/fforge-init` while the skill id is `forge-init`. **This divergence is intentional** — see Naming below.

---

## C-2. Section model — output of `analyze`

Adopts the `MarkdownSection` shape already used by `src/FlowForge.Installer/Update/AgentsMdMerger.cs`.

```json
{
  "path": "./AGENTS.md",
  "exists": true,
  "sizeBytes": 4096,
  "sha256": "9f2c…",
  "lineCount": 88,
  "sections": [
    {
      "id": "s01",
      "name": "Overview",
      "level": 2,
      "startLine": 1,
      "endLine": 15,
      "empty": false,
      "managed": false,
      "skills": []
    },
    {
      "id": "s04",
      "name": "Skills",
      "level": 2,
      "startLine": 27,
      "endLine": 40,
      "empty": false,
      "managed": false,
      "skills": [
        { "name": "forge-dev",   "line": 30, "origin": "flowforge", "migration": null },
        { "name": "my-linter",   "line": 55, "origin": "external",  "migration": null }
      ]
    }
  ],
  "externalSkills": ["my-linter"]
}
```

**Invariants**

- `startLine`/`endLine` are 1-based inclusive; `endLine >= startLine - 1` (an empty section has `endLine == startLine`).
- `empty === true` ⇔ the section body between heading and next heading of equal-or-higher level contains no non-whitespace, non-comment text.
- `managed === true` ⇔ the section lies inside a `fforge-init` managed block (C-5).
- `origin` ∈ `{"flowforge", "external"}` and is decided **only** by prefix match (C-4). No heuristics in the payload; heuristic *suggestions* live in `SKILL.md`, never in the JSON.

**Clarifications (frozen at implementation, CKP-2 approved):**

1. **`startLine` is the heading line**; `endLine` is the last line of the section body that is not
   blank (i.e. trailing blank lines before the next heading are *not* part of the range). An empty
   section therefore has `endLine == startLine`.
2. **Fenced code blocks are opaque.** A line matching `^[ \t]*(```|~~~)` starts a fence; lines inside
   the fence are never treated as headings. This is Risk 1 in `spec.md` §0.
3. **Preamble.** Content before the first heading is exposed as a synthetic section named `Preamble`
   with `level: 0`. It exists only if that region has at least one non-blank line. A file with no
   headings at all is a single `Preamble` section.
4. **Duplicate sibling headings.** The first occurrence of a heading text keeps its plain `name`; the
   *n*-th occurrence (n ≥ 2) is exposed as `"<name> (n-1)"` so that `sections.keep|replace|omit`
   can address each one unambiguously.
5. **Skill-name extraction.** A skill name is a backticked token taken from a table row (the line
   contains `|`) or a list item (`^[ \t]*([-*+]|[0-9]+\.)[ \t]`), excluding fenced code blocks, which
   matches `^[a-z][a-z0-9]*(-[a-z0-9]+)+$` and is ≤ 40 characters. That grammar deliberately
   excludes file paths (`skills/forge-dev/SKILL.md`), commands (`/flow-start`), flags (`--dry-run`)
   and dotted filenames (`opencode.json`). Single-word lowercase names without a dash are **not**
   detected — a documented trade-off in favour of eliminating false positives.
6. **HTML comments are not content** for `empty` detection, but they *are* preserved verbatim when
   their line is copied.
7. **Skills are attributed to the innermost section.** A parent section (for example the level-1
   title, which spans the whole file) never absorbs the skills of its child subsections: the scan for
   backticked names stops at the first child heading of a deeper level.
8. **`endLine` is the last non-blank line of the body**, so trailing blank lines that merely separate
   two sections belong to neither of them and are normalised during composition.

---

## C-3. Merge config — `plan.json`, the AI↔engine seam

```json
{
  "schemaVersion": 1,
  "mode": "init",
  "root": "/abs/path/to/project",
  "target": "./AGENTS.md",
  "base": "kotlin-android",
  "originalSha256": "9f2c…",
  "sections": {
    "keep":    ["Overview", "Build & Test Commands"],
    "replace": ["Tech Stack"],
    "omit":    ["Documentation"]
  },
  "externalSkills": [
    { "name": "legacy-tooling", "decision": "migrate", "migrateTo": "forge-dev-tooling" },
    { "name": "my-linter",      "decision": "keep",    "migrateTo": null }
  ],
  "fragments": [
    "stack/android.md.fragment",
    "functionality/testing.md.fragment",
    "functionality/compose.md.fragment",
    "agent/opencode.md.fragment"
  ],
  "conflicts": [
    { "key": "build", "existing": "gradle", "template": "maven", "resolution": "existing" }
  ]
}
```

**Invariants (enforced by `apply`; violation ⇒ exit 6)**

- `mode` ∈ `{"init", "sync"}`. `init` requires the target to be absent; `sync` requires it to be present.
- Every name in `sections.*` MUST exist in the analysis. Unknown names ⇒ exit 6 (typo guard — never silently drop a user's section selection).
- A name MUST NOT appear in more than one of `keep`/`replace`/`omit` ⇒ exit 6.
- Every `externalSkills[].decision` ∈ `{"discard", "keep", "migrate", "review"}`.
  - `decision: "migrate"` requires a non-null `migrateTo` ⇒ exit 6.
  - Any `decision: "review"` ⇒ `apply` refuses with **exit 4** unless `--allow-review-todos` is passed (FR-005 Scenario B: unresolved externals block the flow).
- `fragments` paths MUST resolve under `templates/fragments/`. Any `..` or absolute path ⇒ exit 3 (path-escape guard).
- `conflicts` MUST account for every conflict `preview` reported. Omitting one ⇒ exit 5.
- `originalSha256` MUST match the current target's hash, or `apply` refuses with exit 5 (guards against applying a plan derived from a file that changed underneath).
- `resolution` ∈ `{"existing", "template"}` or a literal replacement string. Empty/whitespace ⇒ exit 6.

**Clarifications (frozen at implementation):**

1. `schemaVersion` MUST be `1` ⇒ else exit 6.
2. `base` MUST be a key of `templates/base/base.meta.json` → `profiles` ⇒ else exit 6.
3. `sections` is optional (absent ⇒ all three lists are empty).
4. `sections.*` names are matched case-insensitively after trimming; the first match wins for
   duplicate headings (see C-2 clarification 4 for how duplicates are addressed).
5. **Conflict keys** are derived from the overlapping `key: value` list items of the *same-named*
   section in the original and in the rendered base. Key = lowercased trimmed text before the first
   `:`; a value is *concrete* when it is non-empty, contains no `__`, and is not one of
   `(fill in)`, `TODO`, `TBD`, `N/A`. A conflict is reported only when **both** values are concrete
   and differ (so the `blank` profile never produces spurious conflicts).
6. **`mode: init` with an existing target** ⇒ exit 6 (state mismatch, not a silent overwrite).
   **`mode: sync` with an absent target** ⇒ exit 2.
7. `externalSkills` entries for skills that the analysis did not find are allowed but reported as a
   warning (`unknown external skill`); they never create content.
8. **Per-section outcomes** reported in `preview --json` / `apply --json` and the verbose trace:
   `copied` (original bytes preserved outside the block) · `replaced` (template version wins) ·
   `filled` (an empty original section was filled from the template) · `omitted` (dropped, explicitly
   or because it was empty with no counterpart) · `merged` (the engine-composed `Skills` section) ·
   `managed` (engine-owned, regenerated) · `scaffold` (a base-required section survives an `omit` as
   an empty heading plus `<!-- omitted by /fforge-init -->`).
9. **`omit` also suppresses the template counterpart** — otherwise the section the user asked to drop
   would come back from the base. A section listed in `base.meta.json` → `requiredSections` is the only
   exception (it survives as a scaffold).
10. **A section named `Skills` is always engine-owned** (C-7a): it is regenerated from the
    classification decisions instead of being copied. If the user keeps it explicitly, their section
    is preserved verbatim outside the block and the union entries it does not already contain are
    appended to it.
11. **Sections the user never mentions are preserved** when the base has no counterpart (nothing is
    deleted without an explicit decision), and superseded when the base has one (otherwise the output
    would carry the section twice).

---

## C-4. Classification contract — deterministic, no AI in the decision

```
FLOWFORGE  ⇔ skill name matches one of:  ^forge-   ^flowdoc-   ^reginald-
EXTERNAL   ⇔ everything else (including user-authored skills with no prefix)
```

Static migration map (HU-047 §5.5) — advisory only, never auto-applied:

| External | Suggested FlowForge equivalent |
|----------|-------------------------------|
| `sdd-explore` | `forge-discovery` |
| `sdd-init` | `forge-init` |
| `sdd-new` | `forge-arch` |
| `sdd-continue` | `forge-dev` |
| `sdd-ff` | `forge-dev` |
| `sdd-apply` | `forge-dev` |
| `sdd-verify` | `forge-verify` |
| `sdd-archive` | `forge-memory` |

The recommended option is exactly two: `1 = discard`, `2 = keep`, `3 = migrate`, `4 = review` (as in HU-047). The recommendation shown to the user is advisory text; **DEC-1 rule: nothing is written without an explicit user decision.**

`classify` output shape:

```json
{ "skills": [ { "name": "sdd-explore", "origin": "external", "migration": "forge-discovery" } ] }
```

**Clarifications (frozen at implementation):**

1. **`flowforge` is FlowForge.** The prefix list has a blind spot for the one core skill whose name
   carries no dash after `flow`: the exact name `flowforge` is classified `flowforge` alongside
   `^forge-`, `^flowdoc-` and `^reginald-`, and everything else is `external`. Without this rule the
   FlowForge orchestrator would be reported as an inherited external skill and the user would be
   asked to decide about it.
2. A skill that appears in the analysis but is absent from `plan.json.externalSkills` is **not**
   dropped: it is retained in the generated skills list (nothing disappears without an explicit
   `discard`) **and** it blocks the flow with exit 4 (see C-8.7).

---

## C-5. Managed-block marker contract — idempotent re-sync

Mirrors the installer's `<!-- gentle-ai:flowforge -->` + `<!-- hash: … -->` precedent.

```markdown
<!-- fforge-init:begin v=1 hash=<sha256-of-body> -->
…generated body…
<!-- fforge-init:end -->
```

**Invariants**

- Exactly one managed block per file. Zero or >1 ⇒ exit 5 (ambiguous ownership).
- `hash` is the SHA-256 of the block body (marker lines excluded).
- Re-running `sync` on an unchanged file with an identical plan ⇒ **no-op** (nothing written, no backup churn, exit 0). This is the `/fforge-init` analogue of `MergeAction.IdempotentNoOp`.
- Content **outside** the managed block is never touched by a `sync` unless the user explicitly selected that section in `sections.keep|replace` — this is what makes the skill safe to re-run on a hand-edited `AGENTS.md`.
- A hash mismatch means the user hand-edited the generated region: the engine reports drift and **asks**, never silently overwrites (echoes `ConflictKind.Drift`).

**Clarifications (frozen at implementation):**

1. **Zero blocks is legal** and means "this file has never been generated by `/fforge-init`" — the
   first adoption of a hand-written `AGENTS.md`. **>1 block ⇒ exit 5** (ambiguous ownership).
   Only a *hash mismatch inside exactly one block* is drift.
2. `hash` is the SHA-256 (lowercase hex) of the exact bytes between the begin marker's terminating
   newline and the first byte of the end-marker line, with LF line endings.
3. **Drift is reported as a conflict**, key `managed-block`, so it flows through the same
   `plan.json.conflicts` resolution channel as any other conflict:
   - `resolution: "template"` ⇒ regenerate the block (the engine's version wins).
   - `resolution: "existing"` ⇒ keep the on-disk block verbatim; the write then becomes a no-op.
   `preview` reports the drift conflict and exits 5 while it is unresolved.
4. The **H1 title** and the whole base skeleton live *inside* the managed block. Sections the user
   chose to keep live *outside* it, verbatim (see C-7b).

---

## C-6. Fragment manifest contract — self-describing fragments

Each `.md.fragment` carries its own YAML frontmatter. **No central registry** (NFR-006: nothing external, nothing to keep in sync).

```yaml
---
id: android
kind: stack                 # stack | functionality | agent
label: "Android / Kotlin"
detect:                     # all patterns are relative to project root
  - build.gradle.kts
  - settings.gradle.kts
  - AndroidManifest.xml
  - "**/*.kt"
conflicts: [web, python, database]   # mutually-exclusive fragment ids
recommendedWith: [compose]
version: 1
---
<markdown body, injected verbatim>
```

**Invariants (NFR-005)**

- A fragment body MUST NOT reference, include, or require another fragment.
- Any subset may be selected in any order; empty selection is valid (base alone).
- `kind` determines the step-5 menu group only — it has no effect on the merge.
- Fragment bodies MUST be inert markdown: no shell, no template placeholders other than the base's shared vocabulary, nothing executable.

**Clarifications (frozen at implementation):**

1. `detect` supports two forms: a block list (`- pattern`) and an inline list (`[a, b]`). Both are
   parsed identically. `detect: []` means "never auto-recommended" (used by `security`).
2. A `detect` pattern with no glob metacharacter is an exact path relative to the project root; a
   pattern containing `*`, `?` or `[` is resolved with `find` on its last path segment, pruning
   `.git`, `node_modules`, `.agents`, `.ai-work` and `.venv`.
3. Fragment bodies MUST NOT contain fenced code blocks (CONV-2 — a shell block in a fragment is a
   review failure). Commands are shown as inline code inside list items.
4. Every fragment MUST use a **distinct H2 heading** so that any subset combines into a
   duplicate-heading-free document (NFR-005 cross-product check, asserted by the test harness).
5. Fragment `id`s are unique across *all* `kind` groups (asserted by the C# contract test).

---

## C-7. Base template contract

```markdown
# AGENTS.md — __PROJECT_NAME__
… __STACK_LANG__ / __STACK_FRAMEWORK__ / __STACK_TESTS__ / __STACK_VERSION__ …

<!-- fforge-init:fragments -->

<!-- Generated by /fforge-init (FlowForge __FLOWFORGE_VERSION__) on __DATE__ -->
```

- Placeholder names are **identical** to `templates/project/AGENTS.md.template` where they overlap. Adding a new placeholder requires updating both files.
- `<!-- fforge-init:fragments -->` is the single injection anchor. Fragments are concatenated with exactly one blank line between them. Missing anchor in a user-kept section ⇒ fragments append to the managed block end.
- Unknown placeholders MUST remain literal (never rendered as empty) so an incomplete substitution is visible rather than silent.

**Clarifications (frozen at implementation):**

1. `templates/base/base.meta.json` carries the profile table:

   ```json
   {
     "id": "agents-md-base",
     "label": "AGENTS.md base template",
     "version": 1,
     "placeholders": ["__PROJECT_NAME__", "__DATE__", "__FLOWFORGE_VERSION__", "__STACK_LANG__", "__STACK_FRAMEWORK__", "__STACK_TESTS__", "__STACK_VERSION__"],
     "recommendedFragments": [],
     "requiredSections": ["AI Agent Rules"],
     "profiles": {
       "blank":          { "placeholders": { … }, "recommendedFragments": [], "skills": [] },
       "kotlin-android": { "placeholders": { … }, "recommendedFragments": [ … ], "skills": [ … ] },
       "web":            { … },
       "python":         { … }
     }
   }
   ```

   The four profile names are exactly the step-2a menu (`blank`, `kotlin-android`, `web`, `python`).
2. Substitution covers only the keys in `profiles.<name>.placeholders` plus `__PROJECT_NAME__`,
   `__DATE__` and `__FLOWFORGE_VERSION__`. Every other `__TOKEN__` stays literal.
3. `requiredSections` lists base sections that survive `sections.omit` as an **empty scaffold**
   (heading retained, body replaced by `<!-- omitted by /fforge-init -->`). Omitting a non-required
   section removes it.
4. The footer date is reproducible: when `SOURCE_DATE_EPOCH` is set (reproducible-builds
   convention), `__DATE__` is derived from it as `YYYY-MM-DD`; otherwise it is the current UTC date.
   The test harness sets it so that re-runs are byte-identical.

---

## C-7a. Skills union (FR-007 Scenario C)

The generated `## Skills` section body is **computed**, not copied:

```
final_skills = dedupe(base_profile_skills ∪ retained_original_skills)
```

- `base_profile_skills` = `profiles.<base>.skills` from `base.meta.json`.
- `retained_original_skills` = skills found in the analysis, in first-seen document order, filtered by
  (a) `origin == "flowforge"` → always retained, or `origin == "external"` → retained iff
  `decision ∈ {keep, migrate}` (or `review` under `--allow-review-todos`, which also emits
  `<!-- TODO: revisar -->`); **and** (b) the skill's section is *not* preserved verbatim outside the
  managed block (it is already in the file).
- Dedupe is **case-insensitive on the trimmed name**, preserving first-seen order (base first,
  then original additions). No name is ever emitted twice.
- Each emitted entry is one bullet: `` - `name` `` — plus ` — external skill retained as-is` for
  external skills.
- If the user explicitly keeps a section named `Skills`, the base's computed Skills section is
  suppressed like any other kept section, and any union entry that is missing from the kept body is
  appended to that kept section (never overwriting the user's bytes).
- An **empty** original section named `Skills` that the user keeps is *filled*: the engine-owned
  section is emitted with the computed union (there is nothing to preserve from an empty heading).

---

## C-7b. Title rule

- The base's H1 (`# AGENTS.md — __PROJECT_NAME__`) is emitted **unless** the original file has a
  level-1 heading that survives outside the managed block, in which case the user's title wins and
  the base's H1 is dropped (prevents two H1s; never duplicates a title).
- In `mode: init` the base's H1 is always emitted.

---

## C-8. Engine CLI contract

```bash
fforge-init.sh detect    [--root <dir>] [--json]
fforge-init.sh analyze   [--file <path>] [--json]
fforge-init.sh classify  [--from-analysis <path.json>] [--names <csv>] [--json]
fforge-init.sh recommend [--root <dir>] [--json]
fforge-init.sh preview   --config <plan.json> [--json]
fforge-init.sh apply     --config <plan.json> [--dry-run] [--allow-review-todos] [--verbose]
fforge-init.sh restore   [--root <dir>] [--dry-run]
fforge-init.sh version
fforge-init.sh help
```

**Exit codes (frozen — SKILL.md, README and tests all cite this table)**

| Code | Meaning | Example |
|------|---------|---------|
| `0` | Success (including a dry-run and an idempotent no-op) | PM-4 |
| `1` | Usage error / unknown flag / missing required arg | `apply` without `--config` |
| `2` | Target not found | `analyze` with no `AGENTS.md`; `restore` with no backup |
| `3` | Permission denied, read-only FS, or path-escape rejection | FR-001 Scenario B |
| `4` | Unresolved external skills (flow blocked) | PM-3 |
| `5` | Unresolved merge conflict / ambiguous managed block / source changed under the plan | FR-007 Scenario D |
| `6` | Invalid `plan.json` (schema or invariant violation) | typo'd section name |
| `7` | Write failed mid-apply (backup preserved, target restored) | disk full |

**Every code ≥ 1 MUST print an actionable message on stderr** naming (a) what failed, (b) why, (c) how to fix — that is NFR-002, and it is a contract, not a nicety. The engine's canonical shape is:

```
fforge-init.sh: ERROR (exit N) — <what failed>
  Why: <why it failed>
  Fix: <how to fix it>
```

Blocking contract (FR-005): the engine MUST NOT apply while any `review` decision is open. It reports `⚠ N unresolved external skill(s)` and exits 4 before touching the file.

**Clarifications (frozen at implementation):**

1. `detect` on an **absent** target is a **success** (exit 0, `"exists": false`) — FR-002 Scenario B.
   `analyze` on an absent target is exit 2 (there is nothing to analyse).
2. Every subcommand accepts `--json` (CONV-1). Human output mirrors HU-047 §4's box-drawing style;
   the machine output is what the test harness asserts on.
3. `preview` and `apply` share one code path; `--dry-run` only skips the backup and the write
   (DEC-4). `preview` never writes and never creates a backup.
4. `--root` defaults to the current working directory; the target defaults to `<root>/AGENTS.md`.
5. Security guards, all ⇒ exit 3: `--file`/`target` resolving outside `--root`; a symlinked target;
   a target larger than 1 MB; NUL bytes or `U+FFFD` in the target.
6. No `eval`, no `source` of plan data, no shell interpolation of plan values; JSON values are only
   ever treated as data (values are escaped before being written into JSON, never into a command).
7. **Exit 4 covers two cases**, because FR-005 is about *unresolved* decisions:
   an external skill found in the file with **no** decision entry (always blocks), and a decision
   recorded as `review` (blocks unless `--allow-review-todos` is passed). `--allow-review-todos`
   never relaxes an undecided skill. The message reports the total unresolved count, e.g.
   `⚠ 1 unresolved external skill(s)` for 2 detected / 1 decided (PM-3).
8. `recommend` accepts `--agent <opencode|claude-code|cursor|none>` (default `opencode`, the agent
   this skill ships for) so the caller can pre-select the right agent fragment (FR-006 Scenario C).
9. `analyze` accepts `--root` as well as `--file`, so a caller never needs to `cd` into the project.
10. `preview` accepts `--allow-review-todos` as well as `apply`, so a plan can be previewed exactly as
    it will be applied (without it, a plan whose decisions include an explicit `review` blocks the
    preview with exit 4 too — the same rule, the same code path).
11. In every blocking path, `--json` emits a machine-readable payload on stdout **before** the
    actionable message on stderr, so the agent can parse what to ask the user about:
    `{"action":"blocked","reason":"unresolved-external-skills"|"unresolved-conflicts"|"managed-block-drift",…}`.
    Exit codes are unchanged.

---

## C-9. Backup / rollback contract

| Concern | Contract |
|---------|----------|
| Backup path | `<root>/.agents.md.backup` (hidden, project-local) — **spec NFR-004 / OQ-3 default** |
| When created | Before the first byte of the target is modified |
| Overwrite rule | The new backup is staged as `.agents.md.backup.tmp` and **renamed over** `.agents.md.backup` **only after** a fully successful write. A failure therefore leaves the *previous* backup intact (NFR-004: "overwritten only on successful completion") |
| Write order | stage `.AGENTS.md.tmp` → fsync/close → `mv` over target (atomic) |
| On apply failure | Auto-restore target from the staged backup, remove `.tmp`, exit 7 |
| `restore` | Copies `.agents.md.backup` → `AGENTS.md`, then **removes the backup** (PM-5) |
| `--dry-run` | No backup created, nothing written, exit 0 (PM-4) |
| `restore` with no backup | Exit 2, actionable message |

**Clarifications (frozen at implementation):**

1. The staged write file is `<root>/AGENTS.md.tmp`; the staged backup is
   `<root>/.agents.md.backup.tmp`. Both are removed on both success and failure paths.
2. In `mode: init` (target absent) **no backup is created** — there is nothing to lose.
3. Idempotent re-run (byte-identical output) writes nothing at all: no backup, no `.tmp` residue.
4. `restore` on a read-only target directory ⇒ exit 3, backup preserved.
5. **No `fsync`.** bash has no portable fsync primitive and the plan's POSIX runtime list does not
   include one; the guarantee comes from writing a complete staged file and then `mv`-ing it over the
   target, which is atomic on the same filesystem. A crash therefore leaves either the old or the new
   file, never a half-written one. Writers that need a durability barrier beyond that should call
   `sync` themselves after a successful `apply`.
6. **The staged write is `<root>/AGENTS.md.tmp`** — created in the target's own directory so that the
   final `mv` stays on one filesystem (a cross-filesystem `mv` is a copy, not an atomic rename).

---

## C-10. Source-changed guard and reproducibility

- `originalSha256` in `plan.json` MUST equal the SHA-256 of the target as it is on disk when the plan
  is created AND when it is applied. This is re-checked immediately before the write, so a file that
  changed between `preview` and `apply` cannot be clobbered (⇒ exit 5).
- Exit 5's message names the two hashes and tells the agent to re-run `analyze`/`preview` to
  regenerate the plan.
- Determinism: for identical inputs and identical `SOURCE_DATE_EPOCH`, `apply` produces byte-identical
  output. This is what makes the idempotency and "apply twice" tests meaningful.

---

## Naming — don't "fix" this back to `fforge-init`

`spec.md` §5 contains no `[BLOCKER]` rows. Three items needed a ruling; all three are **flag-for-CKP-2** items, not silent assumptions:

| # | Inconsistency | Resolution |
|---|---------------|------------|
| 1 | **Naming.** HU-047 titles the directory `skills/fforge-init/`; spec.md and the request both say the skill installs to `~/.config/opencode/skills/forge-init/`. | Source dir `skills/forge-init/` (mandatory — CONV-3 glob), skill id `forge-init`, user-facing command `/fforge-init` (as spec.md writes it throughout). |
| 2 | **FR-009 scope.** §1 Scope defers "Claude Code / Cursor agent fragments" to V2, but FR-009 requires the flow to support all three, and HU-047 step 5 shows all three in the menu. | §1 governs V1 scope for **depth**; FR-009 governs **flow completeness**. V1 ships `opencode.md.fragment` fully, plus minimal-but-valid `claude-code.md.fragment` and `cursor.md.fragment` (≈20 lines each, independently combinable) so step 5's menu and FR-009 Scenarios B/C are honest rather than dead options. Deep per-agent optimization stays V2. **Reversible:** dropping the two V2 fragments is a one-task deletion if the human prefers strict §1. |
| 3 | **Backup location.** Spec NFR-004 says `.agents.md.backup`; the repo's existing `BackupManager` uses `~/.flowforge-backups/`. | Spec wins (DEC-5). The installer's `BackupManager` targets the *global* `~/.config/opencode/AGENTS.md`; this skill targets the *project-local* `./AGENTS.md`, so a project-local rollback artifact travelling with the project is deliberate, not an oversight. |

**Adopted `[OPTIONAL]` / `[FOLLOW-UP]` defaults (spec §5)**

| ID | Default adopted | Note |
|----|-----------------|------|
| OQ-1 | Project root only — `./AGENTS.md`. Subdirectory `AGENTS.md` files are out of scope for V1. | `--root` exists for testing/fixtures, not for multi-file support. |
| OQ-2 | No `flowdoc-assist` integration in V1. | Reinforces NFR-006 self-containment. |
| OQ-3 | `.agents.md.backup` (hidden). | Frozen in C-9 and asserted by PM-2/PM-5. |
