#!/usr/bin/env bash
# Validates the Antigravity source pack against the REAL contract (ADR-019 Amendment):
#   - rules/*.md .......... YAML frontmatter with a valid `trigger:` (never the inert Cursor key)
#   - agents/*.md ......... 7 native subagents with `name:` + `description:`
#   - workflows/flow-*.md . source files for generated /flow-* skills, single-line `description:`
set -euo pipefail

REPO_ROOT="${1:-$(cd "$(dirname "$0")/.." && pwd)}"
RULES_DIR="$REPO_ROOT/ide/antigravity/rules"
AGENTS_DIR="$REPO_ROOT/ide/antigravity/agents"
WF_DIR="$REPO_ROOT/ide/antigravity/workflows"

ALLOWED_TRIGGERS="always_on model_decision glob manual"
EXPECTED_SUBAGENTS="flowforge forge-discovery forge-arch forge-plan forge-dev forge-verify forge-memory"

failures=0

fail() {
  echo "FAIL: $*" >&2
  failures=$((failures + 1))
}

# Prints the value of a flat `key: value` pair inside the leading YAML frontmatter.
# Returns 1 when the block/key is absent.
frontmatter_value() {
  local file="$1" key="$2"
  awk -v key="$key" '
    BEGIN { in_fm=0; found=0 }
    NR==1 && /^---[[:space:]]*$/ { in_fm=1; next }
    in_fm && /^---[[:space:]]*$/ { exit }
    in_fm {
      line=$0
      sub(/^[[:space:]]+/, "", line)
      if (index(line, key ":") == 1) {
        val=substr(line, length(key)+2)
        sub(/^[[:space:]]*/, "", val)
        sub(/[[:space:]]*$/, "", val)
        print val
        found=1
        exit
      }
    }
    END { exit(found ? 0 : 1) }
  ' "$file"
}

# ── rules ────────────────────────────────────────────────────────────────────
if [ ! -d "$RULES_DIR" ]; then
  fail "rules dir missing: $RULES_DIR"
else
  rule_count=0
  for rule in "$RULES_DIR"/*.md; do
    [ -f "$rule" ] || continue
    rule_count=$((rule_count + 1))
    name="$(basename "$rule")"

    if grep -qE 'always[[:space:]]*Apply' "$rule"; then
      fail "rules/$name — contains the inert Cursor frontmatter key (use 'trigger:')"
    fi

    if ! head -1 "$rule" | grep -q '^---$'; then
      fail "rules/$name — missing frontmatter opening ---"
      continue
    fi

    trigger="$(frontmatter_value "$rule" trigger || true)"
    case "$trigger" in
      "") fail "rules/$name — missing trigger: (rule is silently discarded)" ;;
      *)
        valid=0
        for allowed in $ALLOWED_TRIGGERS; do
          [ "$trigger" = "$allowed" ] && valid=1
        done
        [ "$valid" -eq 1 ] || fail "rules/$name — invalid trigger: '$trigger' (allowed: $ALLOWED_TRIGGERS)"
        ;;
    esac

    if [ "$trigger" = "model_decision" ]; then
      desc="$(frontmatter_value "$rule" description || true)"
      [ -n "$desc" ] || fail "rules/$name — trigger model_decision requires a non-empty description:"
    fi
  done

  [ "$rule_count" -gt 0 ] || fail "no rules/*.md found in $RULES_DIR"
fi

# ── subagents ────────────────────────────────────────────────────────────────
if [ ! -d "$AGENTS_DIR" ]; then
  fail "agents dir missing: $AGENTS_DIR"
else
  for agent in $EXPECTED_SUBAGENTS; do
    file="$AGENTS_DIR/$agent.md"
    if [ ! -f "$file" ]; then
      fail "agents/$agent.md — missing subagent"
      continue
    fi

    name_value="$(frontmatter_value "$file" name || true)"
    desc_value="$(frontmatter_value "$file" description || true)"
    [ -n "$name_value" ] || fail "agents/$agent.md — missing name: in frontmatter"
    [ -n "$desc_value" ] || fail "agents/$agent.md — missing description: in frontmatter"
    [ "$name_value" = "$agent" ] || fail "agents/$agent.md — name: '$name_value' does not match filename"

    if grep -q '^[[:space:]]*tools:' "$file"; then
      fail "agents/$agent.md — must not declare tools: (unmapped tool names hang the subagent)"
    fi
  done
fi

# ── legacy workflow sources (they generate /flow-* skills) ───────────────────
if [ ! -d "$WF_DIR" ]; then
  fail "workflows dir missing: $WF_DIR"
else
  wf_count=0
  for f in "$WF_DIR"/flow-*.md; do
    [ -f "$f" ] || continue
    wf_count=$((wf_count + 1))
    name="$(basename "$f")"

    if ! head -1 "$f" | grep -q '^---$'; then
      fail "workflows/$name — missing frontmatter opening ---"
      continue
    fi

    desc="$(frontmatter_value "$f" description || true)"
    if [ -z "$desc" ]; then
      fail "workflows/$name — missing single-line description: in frontmatter"
    elif [ "${desc:0:1}" = ">" ] || [ "${desc:0:1}" = "|" ]; then
      fail "workflows/$name — description: must be a single line (no '>' / '|' block)"
    fi
  done

  if [ "$wf_count" -ne 7 ]; then
    fail "expected 7 flow-*.md workflow sources, found $wf_count"
  fi
fi

if [ "$failures" -gt 0 ]; then
  echo "Antigravity pack validation FAILED ($failures issue(s))" >&2
  exit 1
fi

echo "Antigravity pack validation passed (rules with trigger:, 7 subagents, 7 workflow sources)"
