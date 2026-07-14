#!/usr/bin/env bash
set -euo pipefail
sudo_guard_check() {
  local no_sudo="${1:-0}"
  if [ -n "${SUDO_USER:-}" ] && [ "$no_sudo" = "1" ]; then
    echo "✗ Correr sin sudo: 'flowforge install --ide opencode --yes'" >&2
    exit 3
  fi
  if [ -n "${SUDO_USER:-}" ]; then
    echo "⚠ Detectado sudo (SUDO_USER=$SUDO_USER). Se aplicará chown post-write." >&2
  fi
}
sudo_guard_chown() {
  local path="$1"
  if [ -n "${SUDO_USER:-}" ] && [ -e "$path" ]; then
    chown "$SUDO_USER:$SUDO_USER" "$path" 2>/dev/null || true
  fi
}
