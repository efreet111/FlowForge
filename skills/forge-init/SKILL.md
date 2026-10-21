---
name: forge-init
description: Initialize or synchronize AGENTS.md in a project — 6-step interactive flow with
  semantic merge, skill classification (FlowForge vs external), fragment composition,
  backup/restore rollback and dry-run.
trigger: "fforge-init, init AGENTS.md, inicializar AGENTS.md, sync AGENTS.md, adopt FlowForge in project"
version: "1.0.0"
changelog:
  - date: "2026-10-16"
    note: "Initial version — 6-step flow (detection, template/analysis, section selection, skill classification, fragment selection, merge & apply) backed by the zero-dependency engine scripts/fforge-init.sh"
metadata:
  opencode/autoinvoke: false   # required by install-skills.sh Step 4 (HU-045)
---

# FlowForge: Init AGENTS.md Agent (On-Demand — No CKP Gate)

You are the **INIT AGENT**. Your job is to initialize or synchronize `AGENTS.md` in a project so it
adopts FlowForge **without losing project-specific configuration**.

> **Your role**: you are invoked on-demand by the human (`/fforge-init`). You are **not** gated by a
> CKP checkpoint, but you are gated by *the user's decisions*: nothing is written without them.
>
> **The engine does the file work.** `scripts/fforge-init.sh` owns every deterministic operation
> (parsing, merging, hashing, backup, atomic write). You own the dialogue, the judgement calls and
> the plan. Never hand-edit `AGENTS.md` yourself: build `plan.json` and let the engine apply it.

Contract source of truth: [`contracts.md`](contracts.md). If this file and the engine disagree, the
engine is wrong unless `contracts.md` says otherwise — fix `contracts.md` first.

## Purpose

- **First run (`mode: init`)**: turn a project that has no `AGENTS.md` into one that documents its
  stack, its agent rules, its commands and its sources of truth — from a base template plus
  fragments.
- **Later runs (`mode: sync`)**: refresh the generated region of an existing `AGENTS.md` while
  preserving the sections the human chose to keep, classify the skills that were inherited from
  other tools, and never touch what nobody asked to change.

## When to use

| Signal | Example |
|--------|---------|
| A project has no `AGENTS.md` | "set up FlowForge here" |
| A project has an `AGENTS.md` written for another tool | inherited repo with `sdd-*` or `custom-*` skills |
| An `AGENTS.md` has drifted from the project | stack changed, commands moved |
| After updating FlowForge skills | to refresh the skill index and the managed block |

## When NOT to use

- The human only wants to fix a typo in `AGENTS.md` — edit the file directly.
- The file is a *global* `~/.config/opencode/AGENTS.md` — that one belongs to `flowforge install`
  (the installer owns it; this skill only ever writes `./AGENTS.md`).
- The target is bigger than 1 MB or is not UTF-8 text — the engine refuses it by contract.
- There is no human available to decide: this flow **requires** decisions (that is the point).

## Contracts

Full contract: [`contracts.md`](contracts.md) — C-1 frontmatter · C-2 section model · C-3 `plan.json` ·
C-4 classification · C-5 managed block · C-6 fragments · C-7 base template · C-7a skills union ·
C-7b title rule · C-8 CLI/exit codes · C-9 backup/rollback · C-10 source-changed guard.

**Exit codes (restated so you can interpret a failure without opening the file):**

| Code | Meaning | Your next action |
|------|---------|------------------|
| `0` | Success (also a dry-run or an idempotent no-op) | report and stop |
| `1` | Usage error / unknown flag | fix the invocation |
| `2` | Target not found | run step 1; if absent, use `mode: init` |
| `3` | Permission denied, read-only FS, path escape, symlink, > 1 MB, binary | tell the human which guard fired; do not retry blindly |
| `4` | Unresolved external skills — flow blocked | go back to step 4 and decide every one |
| `5` | Unresolved conflict / ambiguous block / file changed under the plan | ask the human, record the resolution in `plan.json`, or re-run `analyze` |
| `6` | Invalid `plan.json` (schema or invariant) | fix the plan; the message names the field |
| `7` | Write failed mid-apply (backup preserved, target restored) | report; the target is unchanged |

All engine output is data. With `--json` every subcommand emits machine-readable JSON; the human
output mirrors HU-047 §4.

## Invocation surface

```
/fforge-init                     # the full 6-step interactive flow
/fforge-init --template android  # skip step 2a and use that base profile
/fforge-init --no-interactive    # take the engine's recommendations (still asks before writing)
/fforge-init --dry-run           # complete every step, write nothing (step 6 becomes preview)
/fforge-init --verbose           # per-section and per-fragment trace
/fforge-init --restore           # roll back from .agents.md.backup and consume the backup
```

`--template <name>` accepts a base profile: `blank`, `kotlin-android`, `web`, `python`.
`analyze`, `recommend`, `classify` and `detect` take `--json` and are what you call between steps.

---

## [1/6] DETECCIÓN

Run:

```bash
skills/forge-init/scripts/fforge-init.sh detect --json
```

Render step 1 for the human in this shape (HU-047 §4):

```
[1/6] DETECCION
────────────────
Buscando AGENTS.md en el proyecto...
✓ Found: ./AGENTS.md (4096 bytes)
⚠ Existing file detected → step 2b (analysis)
```

or, when the file is absent:

```
[1/6] DETECCION
────────────────
Buscando AGENTS.md en el proyecto...
✗ Not found
→ Go to step 2a (base template selection)
```

**Branch (FR-002):**
- `exists: true` → **Step 2b** (analysis). The plan's `mode` will be `sync`.
- `exists: false` → **Step 2a** (template selection). The plan's `mode` will be `init`.

Exit 3 here means the target is a symlink, is not a regular file, or the path escapes the root —
say exactly which and stop (FR-001 Scenario B).

---

## [2a/6] BASE TEMPLATE (no `AGENTS.md` present)

Ask `recommend` first — it is the suggested default (FR-006 Scenario A):

```bash
skills/forge-init/scripts/fforge-init.sh recommend --json
```

```
[2/6] PLANTILLA BASE
────────────────────
No se detectó AGENTS.md existente.

Selecciona una plantilla base:
  [1] Blank (solo estructura mínima)
  [2] Kotlin/Android (base + android fragment)
  [3] JavaScript/TypeScript (base + web fragment)
  [4] Python (base + python fragment)

> _
```

Map the answer to the profile name: `1 → blank`, `2 → kotlin-android`, `3 → web`, `4 → python`.
With `--template <name>` the answer is given and you skip this question. Then continue with **Step 3**
(there are no existing sections to select, so `sections.*` stays empty) — the analysis sub-step is
skipped because there is nothing to analyse.

---

## [2b/6] ANALYSIS OF THE EXISTING `AGENTS.md`

Run:

```bash
skills/forge-init/scripts/fforge-init.sh analyze --json > /tmp/fforge-analysis.json
```

Render every section with its line range, marking empty sections and sections that contain skills
outside FlowForge (FR-003 Scenarios A–C):

```
[2/6] ANÁLISIS DE AGENTS.md EXISTENTE
──────────────────────────────────────
  [✓] Overview                 (lines 1-15)
  [✓] Tech Stack               (lines 17-25)
  [✓] Skills                   ← ⚠ EXTERNAL (non-FlowForge)
  [ ] Commands                 (empty)
  [✓] Build & Test Commands    (lines 45-60)

⚠ 2 external skill(s) found in 1 section(s) — step 4 must decide each one:
  - Skills (lines 27-40) — inherited skill: 'legacy-tooling'
```

Keep the JSON file: you will need `sha256` (for `originalSha256`) and the section names (for
`sections.keep|replace|omit`). Sections with `"managed": true` are inside the block this skill
generated — they are engine-owned, never offer them as "keep".

---

## [3/6] SELECCIÓN DE SECCIONES A MANTENER

```
[3/6] SELECCIÓN DE SECCIONES
─────────────────────────────
Marca las secciones a mantener del AGENTS.md original.

  [x] Overview                  → Copiar
  [ ] Tech Stack                → Descartar (se reemplaza por la plantilla)
  [x] Build & Test Commands     → Copiar

  [A] Seleccionar todo
  [N] Deseleccionar todo
  [?] Help

> _
```

Write the answers into `sections`:

- `keep` — copy the original bytes verbatim (outside the managed block).
- `replace` — drop the original and use the template version.
- `omit` — drop it entirely; a section the base requires (`AI Agent Rules`) survives as an empty
  scaffold.

**Engine defaults you must explain to the human, not hide:**

- A section that the human did **not** mention and that has **no** template counterpart is
  **preserved** — nothing is deleted without an explicit decision.
- A section that the human did not mention and that **has** a template counterpart is replaced
  (otherwise the file would carry the section twice).
- A section named `Skills` is always regenerated from step 4's decisions (C-7a) — do not offer it as
  "keep"; offer its *skills* in step 4 instead.

---

## [4/6] CLASIFICACIÓN DE SKILLS

Classify with the engine (deterministic, prefix match — the JSON is the contract):

```bash
skills/forge-init/scripts/fforge-init.sh classify --from-analysis /tmp/fforge-analysis.json --json
```

`forge-*`, `flowdoc-*` and `reginald-*` (and the exact name `flowforge`) → **FLOWFORGE**; everything
else → **EXTERNAL**.

For each external skill, show the four options (HU-047 §4) and record the answer:

```
[4/6] SKILLS EXTERNAS DETECTADAS
─────────────────────────────────
Skill: 'legacy-tooling'

  ¿Pertenece a FlowForge? NO
  Migración sugerida: 'legacy-tooling' → (sin equivalente)

  Acciones disponibles:
    [1] Descartar completamente        → discard
    [2] Mantener sin modificar         → keep
    [3] Migrar a skill FlowForge       → migrate (+ migrateTo)
    [4] Revisar contenido manualmente  → review

> _
```

When the static map (C-4) has an equivalent, show it as the suggestion for option 3 — e.g.
`sdd-explore → forge-discovery`. **Never auto-apply a migration** (DEC-1 rule): `migrate` is written
only because the human chose it.

### 🔴 Blocking rule (FR-005)

**The flow MUST NOT advance while any external skill is undecided.**

- Show `⚠ N unresolved external skill(s)` and stay in step 4.
- An external skill with **no** decision count as unresolved, and so does an explicit `review` unless
  the human accepts a TODO marker (`--allow-review-todos`).
- The engine enforces this: `apply` exits **4** before touching the file. If you ever see exit 4,
  you skipped a decision — go back to step 4, do not look for a flag to bypass it.

---

## [5/6] SELECCIÓN DE FRAGMENTOS

Pre-select from `recommend --json` (`base`, and each fragment's `recommended`/`reason`):

```
[5/6] SELECCIÓN DE FRAGMENTOS
──────────────────────────────
PLANTILLA BASE: kotlin-android

STACK DETECTADO: Android/Kotlin
  [✓] android.md.fragment (recomendado)
  [ ] kotlin.md.fragment        → conflicts with android

FUNCIONALIDAD:
  [ ] testing.md.fragment
  [ ] security.md.fragment      (opt-in, nunca recomendado)
  [✓] compose.md.fragment (detectado: Jetpack Compose)

AGENTE DESTINO:
  [✓] opencode.md.fragment (agente actual)
  [ ] claude-code.md.fragment
  [ ] cursor.md.fragment

  [A] Seleccionar recomendados
  [N] Ninguno

> _
```

- **Deselection is respected** (FR-006 Scenario D): whatever the human unchecks simply does not go
  into `plan.json.fragments`. Never re-add a recommendation the human removed.
- Fragments are independent and combinable in any order (NFR-005): an empty selection is valid.
- Paths are relative to `templates/fragments/` (e.g. `stack/android.md.fragment`).

---

## [6/6] MERGE & APLICACIÓN

### 6.1 Build `plan.json`

```json
{
  "schemaVersion": 1,
  "mode": "sync",
  "root": "/abs/path/to/project",
  "target": "./AGENTS.md",
  "base": "kotlin-android",
  "originalSha256": "<sha256 from analyze --json>",
  "sections": { "keep": ["Overview"], "replace": ["Tech Stack"], "omit": [] },
  "externalSkills": [
    { "name": "my-linter", "decision": "keep", "migrateTo": null }
  ],
  "fragments": ["stack/android.md.fragment", "agent/opencode.md.fragment"],
  "conflicts": []
}
```

In `mode: init` there is no target: `originalSha256` is empty and `sections` are empty.

### 6.2 Preview, then confirm

```bash
skills/forge-init/scripts/fforge-init.sh preview --config <plan.json>
```

- If it exits **5** with `key "<K>": existing=… template=…`, the same list item has two different
  values in the original and in the template. **Ask the human for each key** and add an entry to
  `plan.json.conflicts`: `{"key": "<K>", "existing": "…", "template": "…", "resolution": "existing"|"template"|"<literal>"}`.
- If it exits **5** because of `managed-block` drift, the generated region was edited by hand. Ask:
  keep their version (`"resolution": "existing"`) or regenerate (`"resolution": "template"`).
- If it exits **5** because the file changed since the analysis, re-run `analyze` and rebuild the plan.

Print the **decision summary before asking for confirmation** (NFR-003):

```
────────────────────────────────────────────
Mode:               sync
Target:             ./AGENTS.md
Base template:      kotlin-android
Sections:           copied replaced
Detected conflicts: test runner(existing='JUnit 4', template='JUnit 5')
Fragments:          stack/android.md.fragment agent/opencode.md.fragment
Skills kept:        `flowforge` `forge-dev` `my-linter`
Backup:             .agents.md.backup (created before the first byte changes)
────────────────────────────────────────────
```

Then ask explicitly: *"Apply these changes to ./AGENTS.md? A backup will be written to
`.agents.md.backup` first. [y/N]"*

> **Never skip this confirmation** — not with `--no-interactive`, and **never** when the target
> already exists. `--no-interactive` replaces the *questions* with the recommendations; it does not
> replace the *consent*. The only silent path is an idempotent no-op (the file already matches the
> plan), which reports `no-change` and writes nothing.

### 6.3 Apply

```bash
skills/forge-init/scripts/fforge-init.sh apply --config <plan.json> --verbose
```

Report the engine's outcome verbatim:

```
[6/6] APLICACIÓN
────────────────
  ✓ Copied 2 section(s) from the original
  ✓ Applied base template: kotlin-android
  ✓ Applied 2 fragment(s)

AGENTS.md generado exitosamente.
Saved to: ./AGENTS.md
Backup:   .agents.md.backup (restore with: /fforge-init --restore)
```

After a successful write, offer to verify the output with `forge-verify` (independent markdown
validation) — the generator never certifies its own output.

---

## Dry-run

`--dry-run` (or a plan whose preview you want to inspect) runs **the identical code path** as
`apply` — DEC-4 — and skips exactly two things: the backup and the write.

| | `--dry-run` | `apply` |
|---|---|---|
| Detects, analyses, classifies, merges | yes | yes |
| Computes the managed-block hash | yes | yes |
| Prints the diff and the backup path | yes | yes |
| Creates `.agents.md.backup` | **no** | yes (sync only) |
| Writes `AGENTS.md` | **no** | yes |
| Exit code | `0` | `0` / `4` / `5` / `6` / `7` |

Pre-flight it whenever the human wants to see the change before agreeing to it.

## Rollback

The escape hatch, available at any time after a write (NFR-004):

```bash
/fforge-init --restore                      # engine: fforge-init.sh restore
```

- The backup lives at **`./.agents.md.backup`** (hidden, project-local — spec NFR-004 / OQ-3).
  It is *not* `~/.flowforge-backups/`, which belongs to the installer's global `AGENTS.md` (DEC-5).
- It is created **before the first byte changes** and is overwritten **only after** a fully
  successful write: a failed run leaves the previous backup intact.
- `restore` copies the backup over `AGENTS.md` and then **removes the backup** — a successful
  restore consumes it.
- `restore` with no backup exits **2**; `restore --dry-run` reports what it would do and writes
  nothing.
- `mode: init` creates no backup: there was nothing to lose.

## Error handling

| Exit | Plain-language diagnosis | What you do |
|------|--------------------------|-------------|
| `1` | "I don't understand one of the arguments." | re-read the usage in `contracts.md` C-8 and retry |
| `2` | "There is no `AGENTS.md` at that path (or no backup to restore)." | run step 1; use `mode: init` when the file is absent |
| `3` | "The file (or its directory) cannot be read/written by me." — the message names the guard: permission, read-only FS, path escape, symlink, > 1 MB, binary content | quote the guard to the human; ask them to fix permissions, replace the symlink, or split the file. Never retry the same call unchanged |
| `4` | "External skills are still undecided." | return to step 4; decide every one (or accept TODOs with `--allow-review-todos`) |
| `5` | "A merge conflict / an ambiguous block / the file changed under the plan." | ask the human, write the `resolution` into `plan.json.conflicts`, or re-run `analyze` + rebuild the plan |
| `6` | "`plan.json` is invalid." — the message names the field or invariant | fix the plan (often a typo'd section name or a section in two buckets) |
| `7` | "The write failed halfway; the target was restored from the staged backup and the previous backup is intact." | report the disk/permission problem; nothing was lost |

Every failure prints *what* failed, *why*, and *how to fix it* (NFR-002). Relay all three parts —
never paraphrase away the "Fix:" line.

## Degradation (no bash available)

The engine needs `bash` 4+, `awk`, `sed`, `grep`, `find` and `sha256sum` — all POSIX, no network, no
Node, no `jq`. On a machine without them (rare: Windows without Git Bash):

1. Say so **up front**, before starting the flow — never fail mid-way.
2. Perform the same deterministic steps with your native file tools, in this order:
   detect → read and list the sections with line ranges → classify by prefix → merge (keep verbatim /
   replace from the base template / drop) → inject the fragments at `<!-- fforge-init:fragments -->`
   → wrap the generated region in the managed markers → compute the body hash → write the backup
   **before** the file → write atomically (stage + rename).
3. Mark the result clearly as degraded: the hash and the byte-exact guarantees of C-5/C-9 could not
   be verified by the engine, so tell the human to re-run `/fforge-init` on a machine with bash as
   soon as one is available.

Degraded mode is a fallback, never a shortcut: the same decisions, the same plan, the same markers.

## Quick reference — engine calls per step

| Step | Command |
|------|---------|
| 1 | `fforge-init.sh detect --json` |
| 2a | `fforge-init.sh recommend --json` |
| 2b | `fforge-init.sh analyze --json` |
| 4 | `fforge-init.sh classify --from-analysis … --json` |
| 5 | `fforge-init.sh recommend --json` |
| 6 | `fforge-init.sh preview --config …` → `apply --config …` |
| rollback | `fforge-init.sh restore` |

## Memory Signal

- **type**: pattern
- **significance**: medium
- **summary**: `/fforge-init` is a hybrid skill: a markdown SKILL.md owns the 6-step dialogue and all
  judgement (section selection, skill classification, fragment choice, conflict resolution), while a
  zero-dependency bash engine (`scripts/fforge-init.sh`) owns every deterministic file operation
  behind a frozen `plan.json` (contracts.md C-3). The generated region of `AGENTS.md` is delimited by
  `<!-- fforge-init:begin v=1 hash=… -->` / `<!-- fforge-init:end -->` so re-runs are idempotent, a
  hand edit is reported as drift instead of being overwritten, and `--dry-run` runs the identical
  code path as `apply`. Undecided external skills block the flow with exit 4 (FR-005); the backup is
  project-local `.agents.md.backup` and is consumed by a successful `--restore`.
- **topics**: [forge-init, agents-md, skill-architecture, semantic-merge, plan-json-seam]
