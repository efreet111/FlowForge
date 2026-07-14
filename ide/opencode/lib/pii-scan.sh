#!/usr/bin/env bash
set -euo pipefail
PII_PATTERNS="$(printf '%s|%s|%s|%s|%s' \
  "$(printf '\\x2fhome\\x2f[a-z]+\\x2f')" \
  "$(printf '@local\\x2edev')" \
  "$(printf 'OPENCODIGO\\x5fAPI\\x5fKEY')" \
  "$(printf 'DEEPSEEK\\x5fAPI\\x5fKEY')" \
  "$(printf 'MINIMAX\\x5fAPI\\x5fKEY')")"
pii_scan_file() {
  local f="$1"
  if grep -qE "$PII_PATTERNS" "$f" 2>/dev/null; then
    echo "✗ PII detectada en: $f" >&2
    grep -nE "$PII_PATTERNS" "$f" >&2
    return 2
  fi
  return 0
}
pii_scan_string() {
  local s="$1"
  local label="${2:-string}"
  if echo "$s" | grep -qE "$PII_PATTERNS"; then
    echo "✗ PII detectada en: $label" >&2
    return 2
  fi
  return 0
}
