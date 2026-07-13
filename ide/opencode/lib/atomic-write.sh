#!/usr/bin/env bash
set -euo pipefail
atomic_write() {
  local dest="$1"
  local content="$2"
  local allow_symlink="${3:-0}"
  if [ -L "$dest" ] && [ "$allow_symlink" != "1" ]; then
    echo "✗ $dest es symlink. Usa --allow-symlink." >&2
    exit 2
  fi
  local tmp="${dest}.tmp.$$"
  mkdir -p "$(dirname "$dest")"
  printf '%s' "$content" > "$tmp"
  mv -f "$tmp" "$dest"
  chmod 600 "$dest" 2>/dev/null || true
}
