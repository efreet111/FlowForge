#!/usr/bin/env bash
# Validates Antigravity source pack: 7 flow-*.md with frontmatter + workflow.md alwaysApply.
set -euo pipefail

REPO_ROOT="${1:-$(cd "$(dirname "$0")/.." && pwd)}"
WF_DIR="$REPO_ROOT/ide/antigravity/workflows"
RULE="$REPO_ROOT/ide/antigravity/rules/workflow.md"

failures=0

workflow_has_frontmatter() {
  local file="$1"
  local name
  name="$(basename "$file")"

  if ! head -1 "$file" | grep -q '^---$'; then
    echo "FAIL: $name — missing frontmatter opening ---"
    return 1
  fi

  if ! awk '
    BEGIN { in_fm=0; has_desc=0; closed=0 }
    NR==1 && /^---$/ { in_fm=1; next }
    in_fm && /^---$/ { closed=1; in_fm=0; next }
    in_fm && /^description:/ {
      if ($0 ~ /^description:[[:space:]]*[^[:space:]]/) has_desc=1
      next
    }
    END { exit !(closed && has_desc) }
  ' "$file"; then
    echo "FAIL: $name — falta description: en frontmatter (linea unica)"
    return 1
  fi

  if grep -q '^# /flow-' "$file" && ! head -1 "$file" | grep -q '^---$'; then
    echo "FAIL: $name — body starts with # /flow- without frontmatter"
    return 1
  fi

  return 0
}

if [ ! -d "$WF_DIR" ]; then
  echo "FAIL: workflows dir missing: $WF_DIR"
  exit 1
fi

wf_count=0
for f in "$WF_DIR"/flow-*.md; do
  [ -f "$f" ] || continue
  wf_count=$((wf_count + 1))
  workflow_has_frontmatter "$f" || failures=$((failures + 1))
done

if [ "$wf_count" -ne 7 ]; then
  echo "FAIL: expected 7 flow-*.md files, found $wf_count"
  failures=$((failures + 1))
fi

if [ ! -f "$RULE" ]; then
  echo "FAIL: missing $RULE"
  failures=$((failures + 1))
elif ! head -1 "$RULE" | grep -q '^---$' || ! grep -q 'alwaysApply:[[:space:]]*true' "$RULE"; then
  echo "FAIL: workflow.md missing alwaysApply: true in frontmatter"
  failures=$((failures + 1))
fi

if [ "$failures" -gt 0 ]; then
  echo "Antigravity pack validation failed ($failures issue(s))"
  exit 1
fi

echo "Antigravity pack validation passed (7 workflows + workflow rule)"
