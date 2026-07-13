#!/usr/bin/env bash
set -euo pipefail
install_log_append() {
  local backup_dir="$1"
  local message="$2"
  local logfile="${backup_dir}/install.log"
  mkdir -p "$backup_dir"
  {
    echo "timestamp=$(date -u +%Y-%m-%dT%H:%M:%SZ)"
    echo "user=${USER:-unknown}"
    echo "sudo_user=${SUDO_USER:-}"
    echo "ran_as=$([ -n "${SUDO_USER:-}" ] && echo sudo || echo user)"
    echo "message=$message"
    echo "---"
  } >> "$logfile"
}
