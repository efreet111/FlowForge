#!/usr/bin/env bash
# =============================================================================
# tests/fforge-init/run-tests.sh — automated harness for the /fforge-init engine
#
# Runtime: bash + awk + sed + grep + find + sha256sum + cmp. No node, no jq,
# no python (NFR-006 — same runtime the engine itself requires).
#
#   ./tests/fforge-init/run-tests.sh            # TAP-ish summary
#   ./tests/fforge-init/run-tests.sh --verbose  # show every command it runs
#
# Every test is tagged with the requirement it proves: [FR-0NN], [PM-N],
# [NFR-NNN] or a contracts.md section id. Exit code 0 = all green.
# =============================================================================
set -u

TESTS_DIR=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)
REPO_ROOT=$(cd -- "$TESTS_DIR/../.." && pwd)
ENGINE="${FFORGE_ENGINE:-$REPO_ROOT/skills/forge-init/scripts/fforge-init.sh}"
FRAG_DIR="$REPO_ROOT/skills/forge-init/templates/fragments"
BASE_DIR="$REPO_ROOT/skills/forge-init/templates/base"
SKILL_DIR="$REPO_ROOT/skills/forge-init"

VERBOSE=0
[ "${1:-}" = "--verbose" ] && VERBOSE=1

PASS=0
FAIL=0
TMPROOT=$(mktemp -d "${TMPDIR:-/tmp}/fforge-init-tests.XXXXXX")
export SOURCE_DATE_EPOCH=1700000000   # deterministic __DATE__ (contracts.md C-7.4)

cleanup() { rm -rf "$TMPROOT"; }
trap cleanup EXIT

RC=0
OUT=""
ERR=""

# ---------------------------------------------------------------------------
# harness plumbing
# ---------------------------------------------------------------------------

run_engine() {
  [ "$VERBOSE" = "1" ] && printf '#  $ fforge-init.sh %s\n' "$*" >&2
  local outf errf
  outf=$(mktemp) errf=$(mktemp)
  "$ENGINE" "$@" >"$outf" 2>"$errf"
  RC=$?
  OUT=$(cat "$outf")
  ERR=$(cat "$errf")
  rm -f "$outf" "$errf"
}

ok() {
  PASS=$((PASS + 1))
  printf 'ok %d - %s\n' "$((PASS + FAIL))" "$1"
}

not_ok() {
  FAIL=$((FAIL + 1))
  printf 'not ok %d - %s\n' "$((PASS + FAIL))" "$1"
  printf '#    %s\n' "$2"
}

assert_rc() { # $1 expected, $2 name
  if [ "$RC" = "$1" ]; then ok "$2"; else not_ok "$2" "expected exit $1, got $RC | stderr: $ERR"; fi
}

assert_eq() { # $1 expected, $2 actual, $3 name
  if [ "$1" = "$2" ]; then ok "$3"; else not_ok "$3" "expected '$1', got '$2'"; fi
}

assert_out_has() { # $1 needle, $2 name
  case "$OUT" in
    *"$1"*) ok "$2" ;;
    *) not_ok "$2" "stdout does not contain '$1'" ;;
  esac
}

assert_err_has() { # $1 needle, $2 name
  case "$ERR" in
    *"$1"*) ok "$2" ;;
    *) not_ok "$2" "stderr does not contain '$1'" ;;
  esac
}

assert_file_has() { # $1 file, $2 needle, $3 name
  if grep -qF -- "$2" "$1" 2>/dev/null; then ok "$3"; else not_ok "$3" "file $1 does not contain '$2'"; fi
}

assert_file_lacks() { # $1 file, $2 needle, $3 name
  if grep -qF -- "$2" "$1" 2>/dev/null; then not_ok "$3" "file $1 unexpectedly contains '$2'"; else ok "$3"; fi
}

assert_exists() { # $1 path, $2 name
  if [ -e "$1" ]; then ok "$2"; else not_ok "$2" "$1 does not exist"; fi
}

assert_absent() { # $1 path, $2 name
  if [ -e "$1" ]; then not_ok "$2" "$1 unexpectedly exists"; else ok "$2"; fi
}

assert_same_bytes() { # $1 a, $2 b, $3 name
  if cmp -s "$1" "$2"; then ok "$3"; else not_ok "$3" "$1 and $2 differ"; fi
}

assert_count() { # $1 file, $2 needle, $3 expected count, $4 name
  local n
  n=$(grep -oF -- "$2" "$1" 2>/dev/null | wc -l | tr -d ' ')
  if [ "$n" = "$3" ]; then ok "$4"; else not_ok "$4" "expected $3 occurrence(s) of '$2', found $n"; fi
}

assert_line_count() { # $1 file, $2 exact line, $3 expected count, $4 name
  local n
  n=$(grep -cxF -- "$2" "$1" 2>/dev/null | tr -d ' ')
  if [ "$n" = "$3" ]; then ok "$4"; else not_ok "$4" "expected $3 exact line(s) '$2', found $n"; fi
}

# NFR-002: every exit >= 1 explains what/why/how on stderr
assert_actionable_error() { # $1 code, $2 name
  local missing=""
  case "$ERR" in
    *"ERROR (exit $1)"*) ;;
    *) missing="no ERROR (exit $1) header" ;;
  esac
  case "$ERR" in *"Why:"*) ;; *) missing="$missing; no 'Why:'" ;; esac
  case "$ERR" in *"Fix:"*) ;; *) missing="$missing; no 'Fix:'" ;; esac
  if [ -z "$missing" ]; then ok "$2"; else not_ok "$2" "$missing | stderr: $ERR"; fi
}

json_section_field() { # $1 file, $2 section index (0-based), $3 field
  awk -v want="$2" -v field="$3" '
    /^    \{$/ { n++; next }
    (n - 1) == want && index($0, "      \"" field "\": ") == 1 {
      v = substr($0, 11 + length(field))
      sub(/,$/, "", v)
      gsub(/^"/, "", v); gsub(/"$/, "", v)
      print v
      exit
    }
  ' "$1"
}

json_scalar() { # $1 file, $2 "key"  (top-level scalar)
  awk -v key="$2" '
    index($0, "  \"" key "\": ") == 1 {
      v = substr($0, 6 + length(key))
      sub(/,$/, "", v)
      gsub(/^"/, "", v); gsub(/"$/, "", v)
      print v
      exit
    }
  ' "$1"
}

# validate_markdown <file> -> prints a space-separated list of problems
validate_markdown() {
  awk '
    /^[ \t]*```/ { fences++ }
    /^#+[^ \t#]/ { printf("heading-without-space ") }
    /^## / { h = $0; n[h]++; if (n[h] > 1) printf("duplicate-heading ") }
    /^<!--/ { opens++ }
    /-->/ { closes++ }
    END {
      if (fences % 2 != 0) printf("unbalanced-fences ")
      if (opens != closes) printf("unbalanced-comments ")
    }
  ' "$1"
}

# ---------------------------------------------------------------------------
# fixtures
# ---------------------------------------------------------------------------

mk() { mktemp -d "$TMPROOT/${1}.XXXXXX"; }

fixture_android() {
  local d; d=$(mk "${1:-android}")
  : > "$d/build.gradle.kts"
  : > "$d/settings.gradle.kts"
  mkdir -p "$d/app/src/main" "$d/app/src/test"
  : > "$d/app/src/main/AndroidManifest.xml"
  : > "$d/app/src/main/MainScreen.kt"
  : > "$d/app/src/test/MainTest.kt"
  printf '%s' "$d"
}

fixture_existing() { # $1 = dir name
  local d; d=$(mk "$1")
  cat > "$d/AGENTS.md" <<'MD'
# AGENTS.md — Legacy Project

Preamble text before the first section heading.

## Overview

The legacy overview that must survive a merge.
Second line of the overview.

## Tech Stack

- Language: Kotlin
- Test runner: JUnit 4

## Skills

| Skill | Why |
|-------|-----|
| `my-linter` | house rules |
| `forge-dev` | phase 3 |

## House Rules

- Never push to main.
MD
  printf '%s' "$d"
}

fixture_analysis() { # $1 = dir name -> a small, hand-verifiable file
  local d; d=$(mk "$1")
  cat > "$d/AGENTS.md" <<'MD'
# Title

## Overview

body line

## Empty One

## Tech Stack

- key: value

## Skills

| S |
|---|
| `forge-dev` |
| `my-linter` |
| `flowdoc-hu` |
| `reginald-persona` |
| `skills/forge-dev/SKILL.md` |

## Fenced

```markdown
## Not A Heading
```

## Skills

- `sdd-explore`
MD
  printf '%s' "$d"
}

plan_write() { # $1 = file, $2 = sha, $3 = mode, $4 = root, $5 = base, $6 = json extras body
  cat > "$1" <<JSON
{
  "schemaVersion": 1,
  "mode": "$3",
  "root": "$4",
  "target": "./AGENTS.md",
  "base": "$5",
  "originalSha256": "$2",
  $6
  "fragments": [],
  "conflicts": []
}
JSON
}

sha_of() { sha256sum "$1" | awk '{print $1}'; }

# ===========================================================================
# [C-8] contracts / CLI surface
# ===========================================================================
printf '# fforge-init engine tests — %s\n' "$ENGINE"

run_engine version
assert_rc 0 "[C-8] version exits 0"
assert_out_has "fforge-init.sh" "[C-8] version prints the engine name"

run_engine help
assert_rc 0 "[C-8] help exits 0"
assert_out_has "EXIT CODES" "[C-8] help documents the exit-code table"

run_engine bogus-command
assert_rc 1 "[C-8] unknown subcommand exits 1"
assert_actionable_error 1 "[NFR-002] unknown subcommand explains what/why/how"

run_engine detect --nope
assert_rc 1 "[C-8] unknown flag exits 1"
assert_actionable_error 1 "[NFR-002] unknown flag explains what/why/how"

# ===========================================================================
# [FR-002] detection
# ===========================================================================
d=$(fixture_existing detect_present)
run_engine detect --root "$d" --json
assert_rc 0 "[FR-002-A] detect on a project with AGENTS.md exits 0"
assert_out_has '"exists": true' "[FR-002-A] detect reports exists=true"
assert_out_has '"path": "./AGENTS.md"' "[FR-002-A] detect reports the project-relative path"
assert_eq "$(wc -c < "$d/AGENTS.md" | tr -d ' ')" "$(printf '%s\n' "$OUT" | awk '/"sizeBytes"/ { gsub(/[,]/, "", $2); print $2 }')" "[FR-002-A] detect reports the file size"

run_engine detect --root "$d"
assert_rc 0 "[FR-002-A] detect human output exits 0"
assert_out_has "✓ Found: ./AGENTS.md" "[FR-002-A] detect prints '✓ Found: ./AGENTS.md'"

d_empty=$(mk empty_project)
run_engine detect --root "$d_empty" --json
assert_rc 0 "[FR-002-B] detect without AGENTS.md is a success (exit 0)"
assert_out_has '"exists": false' "[FR-002-B] detect reports exists=false"
assert_out_has '"sizeBytes": 0' "[FR-002-B] detect reports sizeBytes=0"

run_engine detect --root "$d_empty"
assert_out_has "✗ Not found" "[FR-002-B] detect prints '✗ Not found'"
assert_out_has "step 2a" "[FR-002-B] detect routes to step 2a (template selection)"

# ===========================================================================
# [FR-003] section display / analysis
# ===========================================================================
d_an=$(fixture_analysis analysis)
run_engine analyze --root "$d_an" --json
AN_JSON="$TMPROOT/analysis.json"
printf '%s\n' "$OUT" > "$AN_JSON"
assert_rc 0 "[FR-003-A] analyze exits 0"
assert_out_has '"exists": true' "[FR-003-A] analyze reports exists=true"
assert_eq "1" "$(json_section_field "$AN_JSON" 0 "level")" "[FR-003-A] the first section is the level-1 title"
assert_eq "Title" "$(json_section_field "$AN_JSON" 0 "name")" "[FR-003-A] the title section is named 'Title'"
assert_eq "1" "$(json_section_field "$AN_JSON" 0 "startLine")" "[FR-003-A] the title starts at line 1"
assert_eq "Overview" "$(json_section_field "$AN_JSON" 1 "name")" "[FR-003-A] the second section is 'Overview'"
assert_eq "3" "$(json_section_field "$AN_JSON" 1 "startLine")" "[FR-003-A] Overview starts at its heading line"
assert_eq "5" "$(json_section_field "$AN_JSON" 1 "endLine")" "[FR-003-A] Overview ends at its last content line"
assert_eq "Empty One" "$(json_section_field "$AN_JSON" 2 "name")" "[FR-003-B] the empty section is detected"
assert_eq "true" "$(json_section_field "$AN_JSON" 2 "empty")" "[FR-003-B] the empty section is flagged empty=true"
assert_eq "7" "$(json_section_field "$AN_JSON" 2 "startLine")" "[FR-003-B] an empty section spans one line"
assert_eq "7" "$(json_section_field "$AN_JSON" 2 "endLine")" "[FR-003-B] an empty section has endLine == startLine"
assert_eq "false" "$(json_section_field "$AN_JSON" 6 "empty")" "[FR-003-A] a fenced code block counts as section content"
assert_out_has '"name": "Skills (1)"' "[C-2.4] a duplicate heading is disambiguated"
assert_out_has '"externalSkills":' "[FR-003-C] analyze reports the external skills array"
assert_out_has '"my-linter"' "[FR-003-C] analyze flags my-linter as external"
assert_out_has '"sdd-explore"' "[FR-003-C] skills are extracted from list items too"

run_engine analyze --root "$d_an"
assert_rc 0 "[FR-003] analyze human output exits 0"
assert_out_has "(empty)" "[FR-003-B] human output marks empty sections as (empty)"
assert_out_has "EXTERNAL (non-FlowForge)" "[FR-003-C] human output flags EXTERNAL (non-FlowForge)"
assert_out_has "lines 3-5" "[FR-003-A] human output prints line ranges"

d_fence=$(mk fenced)
printf '# T\n\n```\n## Not a heading\n```\n\n## Real\n\nbody\n' > "$d_fence/AGENTS.md"
run_engine analyze --root "$d_fence" --json
printf '%s\n' "$OUT" > "$TMPROOT/fence.json"
assert_eq "Real" "$(json_section_field "$TMPROOT/fence.json" 1 "name")" "[C-2.2] heading-like lines inside a fence do not split sections"
assert_eq "7" "$(json_section_field "$TMPROOT/fence.json" 1 "startLine")" "[C-2.2] the section after the fence keeps its true line number"

d_pre=$(mk preamble)
printf 'free text before headings\n\n# Title\n\n## A\n\nbody\n' > "$d_pre/AGENTS.md"
run_engine analyze --root "$d_pre" --json
printf '%s\n' "$OUT" > "$TMPROOT/pre.json"
assert_eq "Preamble" "$(json_section_field "$TMPROOT/pre.json" 0 "name")" "[C-2.3] content before the first heading becomes a Preamble section"
assert_eq "0" "$(json_section_field "$TMPROOT/pre.json" 0 "level")" "[C-2.3] the Preamble section has level 0"

d_none=$(mk nofile)
run_engine analyze --root "$d_none"
assert_rc 2 "[C-8] analyze on a missing file exits 2"
assert_actionable_error 2 "[NFR-002] analyze without a target explains what/why/how"

# ===========================================================================
# [FR-004] classification
# ===========================================================================
run_engine classify --names forge-dev,my-linter,sdd-explore,flowdoc-hu,reginald-persona,custom-linter --json
assert_rc 0 "[FR-004] classify exits 0"
for name in forge-dev flowdoc-hu reginald-persona; do
  case "$OUT" in
    *"{ \"name\": \"$name\", \"origin\": \"flowforge\""*) ok "[FR-004-A] $name is classified as flowforge" ;;
    *) not_ok "[FR-004-A] $name is classified as flowforge" "got: $OUT" ;;
  esac
done
for name in my-linter custom-linter sdd-explore; do
  case "$OUT" in
    *"{ \"name\": \"$name\", \"origin\": \"external\""*) ok "[FR-004-B] $name is classified as external" ;;
    *) not_ok "[FR-004-B] $name is classified as external" "got: $OUT" ;;
  esac
done
case "$OUT" in
  *'"name": "sdd-explore", "origin": "external", "migration": "forge-discovery"'*) ok "[FR-004-C] sdd-explore suggests forge-discovery" ;;
  *) not_ok "[FR-004-C] sdd-explore suggests forge-discovery" "got: $OUT" ;;
esac

run_engine classify --from-analysis "$AN_JSON" --json
assert_rc 0 "[FR-004] classify reads an analysis file"
assert_out_has '"name": "forge-dev"' "[C-4] classify --from-analysis extracts skill names without the JSON type prefix"
assert_out_has '"name": "flowdoc-hu"' "[C-4] classify --from-analysis sees the skills in the table"

run_engine classify
assert_rc 1 "[C-8] classify without input exits 1"
assert_actionable_error 1 "[NFR-002] classify without input explains what/why/how"

# ===========================================================================
# [FR-006] fragment recommendation
# ===========================================================================
d_android=$(fixture_android android_project)
run_engine recommend --root "$d_android" --json
REC="$TMPROOT/recommend.json"
printf '%s\n' "$OUT" > "$REC"
assert_rc 0 "[FR-006] recommend exits 0"
assert_out_has '"base": "kotlin-android"' "[FR-006-A] an Android project recommends the kotlin-android base"
line_for() { printf '%s\n' "$OUT" | grep -F -- "\"id\": \"$1\"" | head -1; }
rec_line() {
  line_for "$1" | sed 's/.*"recommended": \(true\|false\).*/\1/'
}
assert_eq "true" "$(rec_line android)" "[FR-006-A] android.md.fragment is pre-selected"
assert_eq "true" "$(rec_line compose)" "[FR-006-B] Jetpack Compose is detected and pre-selected"
assert_eq "true" "$(rec_line opencode)" "[FR-006-C] the current agent (opencode) is pre-selected"
assert_eq "false" "$(rec_line security)" "[FR-006] security is never auto-recommended"
assert_eq "false" "$(rec_line kotlin)" "[C-6] mutually exclusive stack fragments are not both recommended"
assert_out_has '"label": "Android / Kotlin"' "[FR-006-A] the recommendation carries the fragment label"

d_python=$(mk python_project)
: > "$d_python/pyproject.toml"
: > "$d_python/main.py"
run_engine recommend --root "$d_python" --json
assert_out_has '"base": "python"' "[FR-006-A] a Python project recommends the python base"

run_engine recommend --root "$d_empty" --json
assert_out_has '"base": "blank"' "[FR-006-A] an unrecognised project recommends blank"

# ===========================================================================
# [FR-007] merge — keep / replace / union / conflicts
# ===========================================================================
merge_setup() { # $1 = fixture dir name ; echoes the dir
  local d
  d=$(fixture_existing "$1")
  printf '%s' "$d"
}

# --- keep + replace + skills union (PM-2) --------------------------------
d=$(merge_setup merge_ok)
SHA=$(sha_of "$d/AGENTS.md")
cat > "$d/plan.json" <<JSON
{
  "schemaVersion": 1,
  "mode": "sync",
  "root": "$d",
  "target": "./AGENTS.md",
  "base": "kotlin-android",
  "originalSha256": "$SHA",
  "sections": { "keep": ["Overview"], "replace": ["Tech Stack"], "omit": [] },
  "externalSkills": [ { "name": "my-linter", "decision": "keep", "migrateTo": null } ],
  "fragments": [ "stack/android.md.fragment", "functionality/testing.md.fragment" ],
  "conflicts": [ { "key": "test runner", "existing": "JUnit 4", "template": "JUnit 5", "resolution": "existing" } ]
}
JSON
cp "$d/AGENTS.md" "$TMPROOT/merge_before.md"
run_engine apply --config "$d/plan.json"
assert_rc 0 "[FR-007] a merge with a resolved conflict applies cleanly"
assert_exists "$d/.agents.md.backup" "[PM-2] a backup is created"
assert_file_has "$d/AGENTS.md" "The legacy overview that must survive a merge." "[FR-007-A] a kept section is copied from the original"
assert_file_has "$d/AGENTS.md" "Second line of the overview." "[FR-007-A] the whole kept section is copied, not just the first line"
assert_file_has "$d/AGENTS.md" "Language / runtime: Kotlin" "[FR-007-B] the replaced section uses the template version"
assert_file_has "$d/AGENTS.md" "Test runner: JUnit 4" "[FR-007-D] the conflict resolution 'existing' keeps the original value"
assert_file_has "$d/AGENTS.md" "my-linter" "[PM-2] a retained external skill appears in the output"
assert_file_has "$d/AGENTS.md" "House Rules" "[FR-007] an unmentioned section without a template counterpart survives"
assert_file_lacks "$d/AGENTS.md" "| Skill | Why |" "[FR-007-A] a kept section is not duplicated from the template"
assert_line_count "$d/AGENTS.md" '- `my-linter` — external skill retained as-is' 1 "[FR-007-C] the skills union emits no duplicate"
assert_line_count "$d/AGENTS.md" '- `forge-dev`' 1 "[FR-007-C] a skill present in both lists is emitted once"
run_engine analyze --root "$d" --json
printf '%s\n' "$OUT" > "$TMPROOT/merged.json"
assert_rc 0 "[FR-007] the merged file is analysable"
assert_out_has '"managed": true' "[C-5] the generated region is inside a managed block"

BLOCK_HASH=$(grep -o 'fforge-init:begin v=1 hash=[0-9a-f]*' "$d/AGENTS.md" | head -1 | sed 's/.*hash=//')
RECOMPUTED=$(awk '
  /fforge-init:begin/ { body = 1; next }
  /fforge-init:end/ { body = 0 }
  body { printf "%s\n", $0 }
' "$d/AGENTS.md" | sha256sum | awk '{print $1}')
assert_eq "$RECOMPUTED" "$BLOCK_HASH" "[C-5] the recorded hash matches the block body"
assert_eq "1" "$(grep -c 'fforge-init:begin' "$d/AGENTS.md" | tr -d ' ')" "[C-5] exactly one managed block is written"

# --- idempotency (contracts.md C-5) --------------------------------------
cp "$d/plan.json" "$TMPROOT/plan_merge.json"
SHA2=$(sha_of "$d/AGENTS.md")
sed -i "s/\"originalSha256\": \"[a-f0-9]*\"/\"originalSha256\": \"$SHA2\"/" "$d/plan.json"
cp "$d/AGENTS.md" "$TMPROOT/after_run1.md"
BACKUP_BEFORE=$(sha_of "$d/.agents.md.backup")
run_engine apply --config "$d/plan.json" --json
assert_rc 0 "[NFR-004] re-applying an identical plan exits 0"
assert_out_has '"action": "no-change"' "[C-5] an identical re-run is an idempotent no-op"
assert_same_bytes "$TMPROOT/after_run1.md" "$d/AGENTS.md" "[C-5] the second run writes nothing"
assert_eq "$BACKUP_BEFORE" "$(sha_of "$d/.agents.md.backup")" "[NFR-004] the second run causes no backup churn"
assert_absent "$d/AGENTS.md.tmp" "[C-9] no staged write file is left behind"
assert_absent "$d/.agents.md.backup.tmp" "[C-9] no staged backup file is left behind"

# --- conflict without a resolution (FR-007 Scenario D) -------------------
d=$(merge_setup merge_conflict)
SHA=$(sha_of "$d/AGENTS.md")
cat > "$d/plan.json" <<JSON
{
  "schemaVersion": 1,
  "mode": "sync",
  "root": "$d",
  "target": "./AGENTS.md",
  "base": "kotlin-android",
  "originalSha256": "$SHA",
  "sections": { "keep": ["Overview"], "replace": [], "omit": [] },
  "externalSkills": [ { "name": "my-linter", "decision": "keep", "migrateTo": null } ],
  "fragments": [],
  "conflicts": []
}
JSON
cp "$d/AGENTS.md" "$TMPROOT/conflict_before.md"
run_engine apply --config "$d/plan.json"
assert_rc 5 "[FR-007-D] an unresolved conflict exits 5"
assert_err_has 'key "test runner"' "[FR-007-D] the conflict names the key, the existing and the template value"
assert_actionable_error 5 "[NFR-002] a conflict explains what/why/how"
assert_same_bytes "$TMPROOT/conflict_before.md" "$d/AGENTS.md" "[FR-007-D] no byte is written while a conflict is open"
assert_absent "$d/.agents.md.backup" "[FR-007-D] no backup is created while a conflict is open"
run_engine preview --config "$d/plan.json" --json
assert_rc 5 "[T-010] preview --json exits 5 while a conflict is open"
assert_out_has '"reason": "unresolved-conflicts"' "[T-010] preview --json names the blocking reason"
assert_out_has '"key": "test runner"' "[T-010] preview --json lists every unresolved conflict"
assert_out_has '"resolution": null' "[T-010] an unresolved conflict carries a null resolution"

# --- omit + fill (contracts.md C-3 / T-009) ------------------------------
d=$(merge_setup merge_omit)
SHA=$(sha_of "$d/AGENTS.md")
cat > "$d/plan.json" <<JSON
{
  "schemaVersion": 1,
  "mode": "sync",
  "root": "$d",
  "target": "./AGENTS.md",
  "base": "blank",
  "originalSha256": "$SHA",
  "sections": { "keep": [], "replace": [], "omit": ["House Rules"] },
  "externalSkills": [ { "name": "my-linter", "decision": "keep", "migrateTo": null } ],
  "fragments": [],
  "conflicts": []
}
JSON
run_engine apply --config "$d/plan.json"
assert_rc 0 "[FR-007] an explicit omit applies cleanly"
assert_file_lacks "$d/AGENTS.md" "Never push to main." "[FR-007] an omitted section is dropped"
assert_file_has "$d/AGENTS.md" "Tech Stack" "[T-009] an unmentioned section with a template counterpart is replaced"

d=$(merge_setup merge_fill)
cat > "$d/AGENTS.md" <<'MD'
# Fill demo

## Overview

kept overview

## Skills

## Tech Stack

- Language: Kotlin
MD
SHA=$(sha_of "$d/AGENTS.md")
cat > "$d/plan.json" <<JSON
{
  "schemaVersion": 1,
  "mode": "sync",
  "root": "$d",
  "target": "./AGENTS.md",
  "base": "blank",
  "originalSha256": "$SHA",
  "sections": { "keep": ["Skills"], "replace": [], "omit": ["Tech Stack"] },
  "externalSkills": [],
  "fragments": [],
  "conflicts": []
}
JSON
run_engine apply --config "$d/plan.json" --json
assert_rc 0 "[T-009] filling an empty/kept section applies cleanly"
assert_out_has '"outcome": "filled"' "[T-009] an empty kept section present in the base is reported as filled"
assert_file_lacks "$d/AGENTS.md" "## Tech Stack" "[T-009] an omitted non-required section is dropped (original and template)"
assert_file_has "$d/AGENTS.md" "## AI Agent Rules" "[C-7.3] a section the base requires survives an omit"
assert_line_count "$d/AGENTS.md" '- `flowforge`' 1 "[C-7a] the filled Skills section carries the base profile skills"

# ===========================================================================
# [FR-005] / [PM-3] blocking on unresolved external skills
# ===========================================================================
d=$(merge_setup blocking)
SHA=$(sha_of "$d/AGENTS.md")
cat > "$d/AGENTS.md" <<'MD'
# Blocking demo

## Skills

- `my-linter` — house rules
- `legacy-tooling` — inherited

## Notes

- keep me
MD
SHA=$(sha_of "$d/AGENTS.md")
cat > "$d/plan.json" <<JSON
{
  "schemaVersion": 1,
  "mode": "sync",
  "root": "$d",
  "target": "./AGENTS.md",
  "base": "blank",
  "originalSha256": "$SHA",
  "sections": { "keep": [], "replace": [], "omit": [] },
  "externalSkills": [ { "name": "my-linter", "decision": "keep", "migrateTo": null } ],
  "fragments": [],
  "conflicts": []
}
JSON
cp "$d/AGENTS.md" "$TMPROOT/blocking_before.md"
run_engine apply --config "$d/plan.json"
assert_rc 4 "[FR-005-B] an undecided external skill blocks the flow (exit 4)"
assert_err_has "⚠ 1 unresolved external skill(s)" "[PM-3] the block message names the count"
assert_actionable_error 4 "[NFR-002] the block explains what/why/how"
assert_same_bytes "$TMPROOT/blocking_before.md" "$d/AGENTS.md" "[PM-3] nothing is written while the flow is blocked"
assert_absent "$d/.agents.md.backup" "[PM-3] no backup is created while the flow is blocked"
run_engine apply --config "$d/plan.json" --json
assert_rc 4 "[PM-3] apply --json still exits 4 while the flow is blocked"
assert_out_has '"reason": "unresolved-external-skills"' "[PM-3] the machine-readable payload names the blocking reason"
assert_out_has '"undecided": 1' "[PM-3] the payload separates undecided from review decisions"

run_engine apply --config "$d/plan.json" --allow-review-todos
assert_rc 4 "[FR-005-B] --allow-review-todos only relaxes an explicit 'review' decision"

cat > "$d/plan.json" <<JSON
{
  "schemaVersion": 1,
  "mode": "sync",
  "root": "$d",
  "target": "./AGENTS.md",
  "base": "blank",
  "originalSha256": "$SHA",
  "sections": { "keep": [], "replace": [], "omit": [] },
  "externalSkills": [
    { "name": "my-linter", "decision": "keep", "migrateTo": null },
    { "name": "legacy-tooling", "decision": "review", "migrateTo": null }
  ],
  "fragments": [],
  "conflicts": []
}
JSON
run_engine apply --config "$d/plan.json"
assert_rc 4 "[FR-005-B] an explicit 'review' decision also blocks the flow"
run_engine apply --config "$d/plan.json" --allow-review-todos --json
assert_rc 0 "[FR-005] --allow-review-todos permits an explicit 'review' decision"
assert_file_has "$d/AGENTS.md" "TODO: revisar" "[FR-005] the TODO marker is emitted for a reviewed skill"
assert_file_has "$d/AGENTS.md" "legacy-tooling" "[FR-005] a reviewed skill is still retained"

# ===========================================================================
# [PM-1] fresh init
# ===========================================================================
d=$(fixture_android init_fresh)
cat > "$d/plan.json" <<JSON
{
  "schemaVersion": 1,
  "mode": "init",
  "root": "$d",
  "target": "./AGENTS.md",
  "base": "kotlin-android",
  "sections": { "keep": [], "replace": [], "omit": [] },
  "externalSkills": [],
  "fragments": [ "stack/android.md.fragment", "functionality/compose.md.fragment", "agent/opencode.md.fragment" ],
  "conflicts": []
}
JSON
run_engine apply --config "$d/plan.json"
assert_rc 0 "[PM-1] a fresh init exits 0"
assert_exists "$d/AGENTS.md" "[PM-1] AGENTS.md is created"
assert_file_has "$d/AGENTS.md" "Language / runtime: Kotlin" "[PM-1] the Android template content is present"
assert_file_has "$d/AGENTS.md" "Android / Kotlin conventions" "[PM-1] the android fragment is injected"
assert_file_has "$d/AGENTS.md" "Jetpack Compose conventions" "[FR-009-A] the compose fragment is injected"
assert_file_has "$d/AGENTS.md" "OpenCode agent instructions" "[FR-009-A] the opencode fragment is injected"
assert_absent "$d/.agents.md.backup" "[C-9] init mode creates no backup (there was nothing to lose)"
assert_eq "" "$(validate_markdown "$d/AGENTS.md")" "[FR-008-B] the generated markdown is balanced and duplicate-free"
assert_file_has "$d/AGENTS.md" "init_fresh" "[C-7] __PROJECT_NAME__ is substituted with the project directory name"
assert_file_lacks "$d/AGENTS.md" "__STACK_LANG__" "[C-7] known placeholders are substituted"
assert_file_lacks "$d/AGENTS.md" "__FLOWFORGE_VERSION__" "[C-7] the FlowForge version placeholder is substituted"

# init with an existing target must refuse (state mismatch)
run_engine apply --config "$d/plan.json"
assert_rc 6 "[C-3] mode=init against an existing target exits 6"
assert_actionable_error 6 "[NFR-002] the init/exists mismatch explains what/why/how"

# ===========================================================================
# [FR-009] multi-agent fragments
# ===========================================================================
d_init=$(mk agents)
cat > "$d_init/plan.json" <<JSON
{
  "schemaVersion": 1,
  "mode": "init",
  "root": "$d_init",
  "target": "./AGENTS.md",
  "base": "blank",
  "sections": { "keep": [], "replace": [], "omit": [] },
  "externalSkills": [],
  "fragments": [ "agent/claude-code.md.fragment", "agent/cursor.md.fragment" ],
  "conflicts": []
}
JSON
run_engine apply --config "$d_init/plan.json"
assert_rc 0 "[FR-009-B] a claude-code fragment can be applied"
assert_file_has "$d_init/AGENTS.md" "Claude Code agent instructions" "[FR-009-B] the Claude Code fragment is injected"
assert_file_has "$d_init/AGENTS.md" "Cursor agent instructions" "[FR-009-C] the Cursor fragment is injected"
assert_eq "" "$(validate_markdown "$d_init/AGENTS.md")" "[FR-009] combining agent fragments keeps valid markdown"

# ===========================================================================
# [PM-4] dry-run
# ===========================================================================
d=$(merge_setup dryrun)
SHA=$(sha_of "$d/AGENTS.md")
cat > "$d/plan.json" <<JSON
{
  "schemaVersion": 1,
  "mode": "sync",
  "root": "$d",
  "target": "./AGENTS.md",
  "base": "kotlin-android",
  "originalSha256": "$SHA",
  "sections": { "keep": ["Overview"], "replace": ["Tech Stack"], "omit": [] },
  "externalSkills": [ { "name": "my-linter", "decision": "keep", "migrateTo": null } ],
  "fragments": [],
  "conflicts": [ { "key": "test runner", "existing": "JUnit 4", "template": "JUnit 5", "resolution": "existing" } ]
}
JSON
cp "$d/AGENTS.md" "$TMPROOT/dryrun_before.md"
run_engine apply --config "$d/plan.json" --dry-run
assert_rc 0 "[PM-4] a dry-run exits 0"
assert_out_has "DRY-RUN" "[PM-4] the dry-run says nothing was written"
assert_same_bytes "$TMPROOT/dryrun_before.md" "$d/AGENTS.md" "[PM-4] the dry-run leaves AGENTS.md byte-identical"
assert_absent "$d/.agents.md.backup" "[PM-4] no backup is created by a dry-run"
assert_absent "$d/AGENTS.md.tmp" "[PM-4] no staged write file is left behind"
assert_absent "$d/.agents.md.backup.tmp" "[PM-4] no staged backup is left behind"

run_engine preview --config "$d/plan.json"
assert_rc 0 "[FR-008] preview exits 0"
assert_out_has "+++" "[NFR-003] preview prints a real diff"
assert_out_has ".agents.md.backup" "[NFR-003] preview shows the backup path before the write"
assert_same_bytes "$TMPROOT/dryrun_before.md" "$d/AGENTS.md" "[NFR-003] preview never writes"

# preview and apply must agree (DEC-4)
PREVIEW_JSON="$TMPROOT/preview.json"
run_engine preview --config "$d/plan.json" --json
printf '%s\n' "$OUT" > "$PREVIEW_JSON"
assert_rc 0 "[C-8] preview --json exits 0"
PREVIEW_HASH=$(printf '%s\n' "$OUT" | awk '/"managedBlockHash"/ { gsub(/[",]/, "", $2); print $2 }')
run_engine apply --config "$d/plan.json" --json
APPLY_HASH=$(printf '%s\n' "$OUT" | awk '/"managedBlockHash"/ { gsub(/[",]/, "", $2); print $2 }')
assert_eq "$PREVIEW_HASH" "$APPLY_HASH" "[DEC-4] preview and apply compose the identical body"

# ===========================================================================
# [PM-5] rollback
# ===========================================================================
d=$(merge_setup rollback)
SHA=$(sha_of "$d/AGENTS.md")
cat > "$d/plan.json" <<JSON
{
  "schemaVersion": 1,
  "mode": "sync",
  "root": "$d",
  "target": "./AGENTS.md",
  "base": "web",
  "originalSha256": "$SHA",
  "sections": { "keep": [], "replace": [], "omit": [] },
  "externalSkills": [ { "name": "my-linter", "decision": "keep", "migrateTo": null } ],
  "fragments": [],
  "conflicts": [ { "key": "test runner", "existing": "JUnit 4", "template": "Vitest", "resolution": "existing" } ]
}
JSON
cp "$d/AGENTS.md" "$TMPROOT/rollback_original.md"
run_engine apply --config "$d/plan.json"
assert_rc 0 "[PM-5] the merge before a rollback applies cleanly"
assert_exists "$d/.agents.md.backup" "[PM-5] the backup exists before the restore"
run_engine restore --root "$d"
assert_rc 0 "[PM-5] restore exits 0"
assert_same_bytes "$TMPROOT/rollback_original.md" "$d/AGENTS.md" "[PM-5] restore recovers the original byte-for-byte"
assert_absent "$d/.agents.md.backup" "[PM-5] a successful restore removes the backup"

run_engine restore --root "$d"
assert_rc 2 "[PM-5] restore without a backup exits 2"
assert_actionable_error 2 "[NFR-002] restore without a backup explains what/why/how"

d=$(merge_setup rollback_dry)
run_engine apply --config "$d/plan.json" >/dev/null 2>&1 || true
SHA=$(sha_of "$d/AGENTS.md")
printf '{"schemaVersion":1,"mode":"sync","root":"%s","target":"./AGENTS.md","base":"web","originalSha256":"%s","sections":{"keep":[],"replace":[],"omit":[]},"externalSkills":[],"fragments":[],"conflicts":[]}\n' "$d" "$SHA" > "$d/plan.json"
run_engine apply --config "$d/plan.json" >/dev/null 2>&1 || true
if [ -f "$d/.agents.md.backup" ]; then
  cp "$d/.agents.md.backup" "$TMPROOT/keep_backup"
  cp "$d/AGENTS.md" "$TMPROOT/before_restore_dry.md"
  run_engine restore --root "$d" --dry-run
  assert_rc 0 "[C-9] restore --dry-run exits 0"
  assert_same_bytes "$TMPROOT/before_restore_dry.md" "$d/AGENTS.md" "[C-9] restore --dry-run writes nothing"
  assert_exists "$d/.agents.md.backup" "[C-9] restore --dry-run keeps the backup"
fi

# ===========================================================================
# [NFR-004] failure atomicity
# ===========================================================================
d=$(merge_setup readonly)
SHA=$(sha_of "$d/AGENTS.md")
cat > "$d/plan.json" <<JSON
{
  "schemaVersion": 1,
  "mode": "sync",
  "root": "$d",
  "target": "./AGENTS.md",
  "base": "web",
  "originalSha256": "$SHA",
  "sections": { "keep": [], "replace": [], "omit": [] },
  "externalSkills": [ { "name": "my-linter", "decision": "keep", "migrateTo": null } ],
  "fragments": [],
  "conflicts": [ { "key": "test runner", "existing": "JUnit 4", "template": "Vitest", "resolution": "existing" } ]
}
JSON
cp "$d/AGENTS.md" "$TMPROOT/readonly_before.md"
printf 'previous backup\n' > "$d/.agents.md.backup"
cp "$d/.agents.md.backup" "$TMPROOT/previous_backup"
chmod 500 "$d"
run_engine apply --config "$d/plan.json"
chmod 700 "$d"
assert_rc 3 "[FR-001-B] a read-only target directory exits 3"
assert_actionable_error 3 "[NFR-002] a permission failure explains what/why/how"
assert_same_bytes "$TMPROOT/readonly_before.md" "$d/AGENTS.md" "[NFR-004] the target is untouched after a permission failure"
assert_same_bytes "$TMPROOT/previous_backup" "$d/.agents.md.backup" "[NFR-004] the pre-existing backup survives a failed run"

# staged-write failure => exit 7, target and previous backup intact
d=$(merge_setup stagefail)
SHA=$(sha_of "$d/AGENTS.md")
cat > "$d/plan.json" <<JSON
{
  "schemaVersion": 1,
  "mode": "sync",
  "root": "$d",
  "target": "./AGENTS.md",
  "base": "web",
  "originalSha256": "$SHA",
  "sections": { "keep": [], "replace": [], "omit": [] },
  "externalSkills": [ { "name": "my-linter", "decision": "keep", "migrateTo": null } ],
  "fragments": [],
  "conflicts": [ { "key": "test runner", "existing": "JUnit 4", "template": "Vitest", "resolution": "existing" } ]
}
JSON
cp "$d/AGENTS.md" "$TMPROOT/stagefail_before.md"
printf 'previous backup\n' > "$d/.agents.md.backup"
cp "$d/.agents.md.backup" "$TMPROOT/stagefail_backup"
mkdir -p "$d/AGENTS.md.tmp"     # makes the staged write fail deterministically
run_engine apply --config "$d/plan.json"
assert_rc 7 "[C-9] a failed staged write exits 7"
assert_actionable_error 7 "[NFR-002] a write failure explains what/why/how"
assert_same_bytes "$TMPROOT/stagefail_before.md" "$d/AGENTS.md" "[C-9] the target survives a failed write"
assert_same_bytes "$TMPROOT/stagefail_backup" "$d/.agents.md.backup" "[C-9] the previous backup survives a failed write"
assert_absent "$d/.agents.md.backup.tmp" "[C-9] the staged backup is cleaned up after a failure"
rmdir "$d/AGENTS.md.tmp" 2>/dev/null || true

# ===========================================================================
# [C-8.5] security guards
# ===========================================================================
run_engine analyze --root "$d_an" --file /etc/hostname
assert_rc 3 "[C-8.5] a --file outside --root is rejected with exit 3"
assert_err_has "escapes the project root" "[C-8.5] the escape message names the root"

d_link=$(mk symlink_target)
printf 'x\n' > "$TMPROOT/real_agents.md"
ln -s "$TMPROOT/real_agents.md" "$d_link/AGENTS.md"
run_engine detect --root "$d_link"
assert_rc 3 "[C-8.5] a symlinked target is refused"

d_big=$(mk big)
awk 'BEGIN { for (i = 0; i < 20000; i++) print "0123456789012345678901234567890123456789012345678901234567890123456789" }' > "$d_big/AGENTS.md"
run_engine analyze --root "$d_big"
assert_rc 3 "[C-8.5] a target larger than 1 MB is refused"

d_bin=$(mk binary)
printf 'text\000more\n' > "$d_bin/AGENTS.md"
run_engine analyze --root "$d_bin"
assert_rc 3 "[C-8.5] NUL bytes make the target binary and refused"

d_esc=$(mk esc)
SHA=$(sha_of "$d_esc/AGENTS.md" 2>/dev/null || echo "")
printf '# T\n' > "$d_esc/AGENTS.md"
SHA=$(sha_of "$d_esc/AGENTS.md")
cat > "$d_esc/plan.json" <<JSON
{
  "schemaVersion": 1,
  "mode": "sync",
  "root": "$d_esc",
  "target": "./AGENTS.md",
  "base": "blank",
  "originalSha256": "$SHA",
  "sections": { "keep": [], "replace": [], "omit": [] },
  "externalSkills": [],
  "fragments": [ "../../../etc/passwd" ],
  "conflicts": []
}
JSON
run_engine apply --config "$d_esc/plan.json"
assert_rc 3 "[C-3] a fragment path containing '..' is rejected with exit 3"

# ===========================================================================
# [C-3] plan.json validation
# ===========================================================================
d_val=$(fixture_existing validate)
SHA=$(sha_of "$d_val/AGENTS.md")
plan_extra() { # $1 file, $2 sha, $3 mode, $4 base, $5 extras
  cat > "$1" <<JSON
{
  "schemaVersion": 1,
  "mode": "$3",
  "root": "$d_val",
  "target": "./AGENTS.md",
  "base": "$4",
  "originalSha256": "$2",
  $5,
  "externalSkills": [ { "name": "my-linter", "decision": "keep", "migrateTo": null } ]
}
JSON
}
plan_extra "$d_val/p1.json" "$SHA" sync blank '"sections": { "keep": ["Overveiw"], "replace": [], "omit": [] }'
run_engine apply --config "$d_val/p1.json"
assert_rc 6 "[C-3] a typo'd section name exits 6"
assert_err_has "which the analysis did not find" "[C-3] the typo error names the offending section"

plan_extra "$d_val/p2.json" "$SHA" sync blank '"sections": { "keep": ["Overview"], "replace": ["Overview"], "omit": [] }'
run_engine apply --config "$d_val/p2.json"
assert_rc 6 "[C-3] a section in two buckets exits 6"

cat > "$d_val/p3.json" <<JSON
{
  "schemaVersion": 1,
  "mode": "sync",
  "root": "$d_val",
  "target": "./AGENTS.md",
  "base": "blank",
  "originalSha256": "$SHA",
  "sections": { "keep": [], "replace": [], "omit": [] },
  "externalSkills": [ { "name": "my-linter", "decision": "maybe", "migrateTo": null } ],
  "fragments": [],
  "conflicts": []
}
JSON
run_engine apply --config "$d_val/p3.json"
assert_rc 6 "[C-3] an invalid skill decision exits 6"

cat > "$d_val/p4.json" <<JSON
{
  "schemaVersion": 1,
  "mode": "sync",
  "root": "$d_val",
  "target": "./AGENTS.md",
  "base": "blank",
  "originalSha256": "$SHA",
  "sections": { "keep": [], "replace": [], "omit": [] },
  "externalSkills": [ { "name": "my-linter", "decision": "migrate" } ],
  "fragments": [],
  "conflicts": []
}
JSON
run_engine apply --config "$d_val/p4.json"
assert_rc 6 "[C-3] 'migrate' without migrateTo exits 6"

plan_extra "$d_val/p5.json" "$SHA" sync blank '"fragments": []'
sed -i 's/"schemaVersion": 1/"schemaVersion": 2/' "$d_val/p5.json"
run_engine apply --config "$d_val/p5.json"
assert_rc 6 "[C-3] an unsupported schemaVersion exits 6"

plan_extra "$d_val/p6.json" "$SHA" sync no-such-profile '"sections": { "keep": [], "replace": [], "omit": [] }'
run_engine apply --config "$d_val/p6.json"
assert_rc 6 "[C-3] an unknown base profile exits 6"

plan_extra "$d_val/p7.json" "0000000000000000000000000000000000000000000000000000000000000000" sync blank '"sections": { "keep": [], "replace": [], "omit": [] }'
run_engine apply --config "$d_val/p7.json"
assert_rc 5 "[C-10] a stale originalSha256 exits 5"
assert_err_has "the target changed since the plan was created" "[C-10] the stale-hash error says what changed"

printf '{ "schemaVersion": 1, ' > "$d_val/p8.json"
run_engine apply --config "$d_val/p8.json"
assert_rc 6 "[C-3] malformed JSON exits 6"
assert_actionable_error 6 "[NFR-002] malformed JSON explains what/why/how"

run_engine apply --config "$TMPROOT/does-not-exist.json"
assert_rc 6 "[C-8] a missing plan file exits 6"

# ===========================================================================
# [C-5] managed-block ownership
# ===========================================================================
d_two=$(mk twoblocks)
cat > "$d_two/AGENTS.md" <<'MD'
# Two blocks

<!-- fforge-init:begin v=1 hash=aaaa -->
one
<!-- fforge-init:end -->

<!-- fforge-init:begin v=1 hash=bbbb -->
two
<!-- fforge-init:end -->
MD
SHA=$(sha_of "$d_two/AGENTS.md")
printf '{"schemaVersion":1,"mode":"sync","root":"%s","target":"./AGENTS.md","base":"blank","originalSha256":"%s","sections":{"keep":[],"replace":[],"omit":[]},"externalSkills":[],"fragments":[],"conflicts":[]}\n' "$d_two" "$SHA" > "$d_two/plan.json"
cp "$d_two/AGENTS.md" "$TMPROOT/two_before.md"
run_engine apply --config "$d_two/plan.json"
assert_rc 5 "[C-5] more than one managed block exits 5"
assert_same_bytes "$TMPROOT/two_before.md" "$d_two/AGENTS.md" "[C-5] an ambiguous file is never modified"

# drift: a hand-edited managed block
d=$(fixture_android drift)
cat > "$d/plan.json" <<JSON
{
  "schemaVersion": 1,
  "mode": "init",
  "root": "$d",
  "target": "./AGENTS.md",
  "base": "blank",
  "sections": { "keep": [], "replace": [], "omit": [] },
  "externalSkills": [],
  "fragments": [],
  "conflicts": []
}
JSON
run_engine apply --config "$d/plan.json" >/dev/null 2>&1
assert_rc 0 "[C-5] the first run creates a managed block"
sed -i 's/- The human reviews and commits; the agent proposes./- HAND EDITED./' "$d/AGENTS.md"
SHA=$(sha_of "$d/AGENTS.md")
printf '{"schemaVersion":1,"mode":"sync","root":"%s","target":"./AGENTS.md","base":"blank","originalSha256":"%s","sections":{"keep":[],"replace":[],"omit":[]},"externalSkills":[],"fragments":[],"conflicts":[]}\n' "$d" "$SHA" > "$d/plan.json"
cp "$d/AGENTS.md" "$TMPROOT/drift_before.md"
run_engine apply --config "$d/plan.json"
assert_rc 5 "[C-5] a hand-edited managed block is reported as drift and refused"
assert_actionable_error 5 "[NFR-002] drift explains what/why/how"
assert_same_bytes "$TMPROOT/drift_before.md" "$d/AGENTS.md" "[C-5] the hand-edited region is never overwritten silently"
run_engine apply --config "$d/plan.json" --json
assert_rc 5 "[C-5] drift with --json exits 5"
assert_out_has '"reason": "managed-block-drift"' "[C-5] the drift payload names the reason and both hashes"

printf '{"schemaVersion":1,"mode":"sync","root":"%s","target":"./AGENTS.md","base":"blank","originalSha256":"%s","sections":{"keep":[],"replace":[],"omit":[]},"externalSkills":[],"fragments":[],"conflicts":[{"key":"managed-block","existing":"x","template":"y","resolution":"existing"}]}\n' "$d" "$SHA" > "$d/plan.json"
run_engine apply --config "$d/plan.json" --json
assert_rc 0 "[C-5] drift resolved as 'existing' is a no-op"
assert_same_bytes "$TMPROOT/drift_before.md" "$d/AGENTS.md" "[C-5] 'existing' keeps the hand-edited region"

# ===========================================================================
# [FR-008-B] the base alone is a valid document (NFR-005 self-sufficiency)
# ===========================================================================
d_base=$(mk baseonly)
printf '{"schemaVersion":1,"mode":"init","root":"%s","target":"./AGENTS.md","base":"blank","sections":{"keep":[],"replace":[],"omit":[]},"externalSkills":[],"fragments":[],"conflicts":[]}\n' "$d_base" > "$d_base/plan.json"
run_engine apply --config "$d_base/plan.json"
assert_rc 0 "[NFR-005] the base template alone renders (empty fragment selection is valid)"
assert_eq "" "$(validate_markdown "$d_base/AGENTS.md")" "[NFR-005] base-alone output is valid markdown"
assert_file_has "$d_base/AGENTS.md" "## Overview" "[NFR-005] the base-alone document has the Overview scaffold"
assert_file_has "$d_base/AGENTS.md" "## AI Agent Rules" "[NFR-005] the base-alone document has the AI Agent Rules scaffold"
assert_file_has "$d_base/AGENTS.md" "## Build & Test Commands" "[NFR-005] the base-alone document has the Build & Test scaffold"
assert_file_has "$d_base/AGENTS.md" "## Documentation" "[NFR-005] the base-alone document references the documentation layer"

# every fragment pair combines into a duplicate-heading-free document
frag_paths=()
while IFS= read -r f; do frag_paths+=("${f#"$FRAG_DIR"/}"); done < <(find "$FRAG_DIR" -name '*.md.fragment' | LC_ALL=C sort)
nfrag=${#frag_paths[@]}
pairs_checked=0
for ((i = 0; i < nfrag; i++)); do
  for ((j = i + 1; j < nfrag; j++)); do
    d_pair=$(mk "pair_${i}_${j}")
    printf '{"schemaVersion":1,"mode":"init","root":"%s","target":"./AGENTS.md","base":"blank","sections":{"keep":[],"replace":[],"omit":[]},"externalSkills":[],"fragments":["%s","%s"],"conflicts":[]}\n' \
      "$d_pair" "${frag_paths[$i]}" "${frag_paths[$j]}" > "$d_pair/plan.json"
    run_engine apply --config "$d_pair/plan.json" >/dev/null 2>&1
    if [ "$RC" != "0" ]; then
      not_ok "[NFR-005] fragments ${frag_paths[$i]} + ${frag_paths[$j]} combine" "exit $RC: $ERR"
    elif [ -n "$(validate_markdown "$d_pair/AGENTS.md")" ]; then
      not_ok "[NFR-005] fragments ${frag_paths[$i]} + ${frag_paths[$j]} combine" "markdown problems: $(validate_markdown "$d_pair/AGENTS.md")"
    else
      pairs_checked=$((pairs_checked + 1))
    fi
  done
done
assert_eq "$((nfrag * (nfrag - 1) / 2))" "$pairs_checked" "[NFR-005] all $((nfrag * (nfrag - 1) / 2)) fragment pairs combine cleanly"

# fragments are inert: no fenced code blocks, no cross-references, unique ids
frag_problems=""
frag_ids=""
while IFS= read -r f; do
  id=$(awk 'NR == 1 && /^---/ { fm = 1; next } fm && /^---/ { exit } fm && /^id:/ { print $2 }' "$f")
  frag_ids="$frag_ids $id"
  if grep -qE '^[ \t]*```' "$f"; then frag_problems="$frag_problems $id:fence"; fi
  if grep -q '\.md\.fragment' "$f"; then frag_problems="$frag_problems $id:crossref"; fi
done < <(find "$FRAG_DIR" -name '*.md.fragment')
dup_ids=$(printf '%s\n' $frag_ids | sort | uniq -d)
assert_eq "" "$dup_ids" "[C-6.5] every fragment id is unique"
assert_eq "" "$frag_problems" "[CONV-2] every fragment is inert markdown (no fences, no cross-references)"

# base placeholders must stay inside the shared vocabulary
shared_vocab="__PROJECT_NAME__ __DATE__ __FLOWFORGE_VERSION__ __STACK_LANG__ __STACK_FRAMEWORK__ __STACK_TESTS__ __STACK_VERSION__"
base_tokens=$(grep -oE '__[A-Z_]+__' "$BASE_DIR/AGENTS.md.base" | sort -u | tr '\n' ' ')
unknown=""
for tok in $base_tokens; do
  case " $shared_vocab " in
    *" $tok "*) ;;
    *) unknown="$unknown $tok" ;;
  esac
done
assert_eq "" "$(printf '%s' "$unknown" | tr -d ' ')" "[C-7] the base template uses only the shared placeholder vocabulary"

# [C-1] skill frontmatter
skill_name=$(awk 'NR == 1 && /^---/ { fm = 1; next } fm && /^---/ { exit } fm && /^name:/ { print $2 }' "$SKILL_DIR/SKILL.md")
assert_eq "forge-init" "$skill_name" "[C-1] SKILL.md declares name: forge-init"
assert_file_has "$SKILL_DIR/SKILL.md" "opencode/autoinvoke: false" "[C-1] SKILL.md disables auto-invocation (HU-045)"
globbed=$(cd "$REPO_ROOT" && ls -d skills/forge-*/SKILL.md 2>/dev/null | grep -c '^skills/forge-init/SKILL.md$')
assert_eq "1" "$globbed" "[CONV-3] skills/forge-init matches the install-skills.sh forge-*/ glob"
shipped_problems=""
for required in SKILL.md contracts.md scripts/fforge-init.sh templates/base/AGENTS.md.base templates/base/base.meta.json; do
  [ -f "$SKILL_DIR/$required" ] || shipped_problems="$shipped_problems missing:$required"
done
for required in stack/android stack/kotlin stack/web stack/python functionality/testing functionality/security functionality/database functionality/compose agent/opencode agent/claude-code agent/cursor; do
  [ -f "$FRAG_DIR/$required.md.fragment" ] || shipped_problems="$shipped_problems missing:$required"
done
assert_eq "" "$shipped_problems" "[C-1] the skill ships SKILL.md, contracts.md, the engine, the base template and all 11 fragments"
assert_exists "$SKILL_DIR/contracts.md" "[C-8] contracts.md is shipped with the skill"
assert_exists "$BASE_DIR/base.meta.json" "[C-7] base.meta.json is shipped with the skill"

# ===========================================================================
# [NFR-001] performance
# ===========================================================================
d_perf=$(fixture_android perf)
start=$(date +%s%N)
run_engine detect --root "$d_perf" --json
run_engine analyze --root "$d_perf" --json
end=$(date +%s%N)
detect_ms=$(((end - start) / 1000000))
if [ "$detect_ms" -lt 2000 ]; then
  ok "[NFR-001] detection + analysis completes in < 2 s (${detect_ms} ms)"
else
  not_ok "[NFR-001] detection + analysis completes in < 2 s" "took ${detect_ms} ms"
fi

printf '{"schemaVersion":1,"mode":"init","root":"%s","target":"./AGENTS.md","base":"kotlin-android","sections":{"keep":[],"replace":[],"omit":[]},"externalSkills":[],"fragments":["stack/android.md.fragment","functionality/testing.md.fragment","functionality/compose.md.fragment","agent/opencode.md.fragment"],"conflicts":[]}\n' "$d_perf" > "$d_perf/plan.json"
start=$(date +%s%N)
run_engine apply --config "$d_perf/plan.json"
end=$(date +%s%N)
apply_ms=$(((end - start) / 1000000))
assert_rc 0 "[NFR-001] the merge + apply path runs"
if [ "$apply_ms" -lt 5000 ]; then
  ok "[NFR-001] merge + application completes in < 5 s (${apply_ms} ms)"
else
  not_ok "[NFR-001] merge + application completes in < 5 s" "took ${apply_ms} ms"
fi

# ===========================================================================
# summary
# ===========================================================================
printf '\n# 1..%d\n' "$((PASS + FAIL))"
printf '# passed %d, failed %d\n' "$PASS" "$FAIL"
if [ "$FAIL" -gt 0 ]; then
  printf '# %s\n' "FAILURES PRESENT"
  exit 1
fi
printf '# all green\n'
exit 0
