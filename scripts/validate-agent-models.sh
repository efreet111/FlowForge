#!/usr/bin/env bash
# validate-agent-models.sh — Validate all agent-models.json files for schema compliance
# Usage: bash scripts/validate-agent-models.sh [REPO_ROOT]
# Exit 0 on success, non-zero on failure with descriptive error messages.
#
# Checks per file:
#   (a) Valid JSON   (b) $schema present   (c) 9 agent keys
#   (d) model/fallback in provider.models   (e) active_tier   (f) provider metadata

set -euo pipefail

command -v jq >/dev/null 2>&1 || { echo "ERROR: jq required" >&2; exit 1; }

REPO_ROOT="${1:-.}"
REPO_ROOT="$(cd "$REPO_ROOT" && pwd)"

IDE_LIST="opencode cursor antigravity vscode"
ERRORS=0
FILES_CHECKED=0

validate_file() {
  local file="$1"
  local ide="$2"
  local file_errors=0

  if [ ! -f "$file" ]; then
    echo "[$ide] FAIL: File not found: $file"
    return 1
  fi

  # (a) Valid JSON
  if ! jq empty "$file" 2>/dev/null; then
    echo "[$ide] FAIL: Invalid JSON in $file"
    return 1
  fi

  # All structural checks in a single jq call
  local errors
  errors="$(jq -r '
    .provider.models as $pm |
    [
      # (b) $schema
      (if (."$schema" // "") == "" then "Missing $schema" else empty end),

      # (d) Model refs in agents block
      (.agents | to_entries[] |
        .key as $agent | .value as $obj |
        (if ($obj.model // "") != "" and ([$pm[] | select(. == $obj.model)] | length == 0)
         then $agent + ".model=" + $obj.model + " not in provider.models"
         else empty end),
        (if ($obj.fallback // "") != "" and ([$pm[] | select(. == $obj.fallback)] | length == 0)
         then $agent + ".fallback=" + $obj.fallback + " not in provider.models"
         else empty end)
      ),

      # (d) Model refs in tiers
      ((.tiers // {}) | to_entries[] |
        .key as $tier | (.value.agents // {}) | to_entries[] |
        .key as $agent | .value as $obj |
        (if ($obj.model // "") != "" and ([$pm[] | select(. == $obj.model)] | length == 0)
         then "tiers." + $tier + "." + $agent + ".model=" + $obj.model + " not in provider.models"
         else empty end),
        (if ($obj.fallback // "") != "" and ([$pm[] | select(. == $obj.fallback)] | length == 0)
         then "tiers." + $tier + "." + $agent + ".fallback=" + $obj.fallback + " not in provider.models"
         else empty end)
      ),

      # (e) active_tier
      (if (.active_tier // "") == "" then "Missing active_tier" else empty end),

      # (f) Provider fields
      (if (.provider.id // "") == "" then "Missing provider.id" else empty end),
      (if (.provider.name // "") == "" then "Missing provider.name" else empty end),
      (if (.provider.docs_url // "") == "" then "Missing provider.docs_url" else empty end)
    ] | .[]
  ' "$file" 2>&1)" || {
    echo "[$ide] FAIL: jq error processing $file"
    return 1
  }

  # Check required agent keys separately (simpler jq)
  for agent in forge-orchestrator forge-discovery forge-arch forge-plan forge-dev forge-verify forge-memory forge-teacher default; do
    if ! jq -e ".agents[\"$agent\"]" "$file" >/dev/null 2>&1; then
      echo "[$ide] FAIL: Missing agent key '$agent' in $file"
      file_errors=$((file_errors + 1))
    fi
  done

  if [ -n "$errors" ]; then
    while IFS= read -r line; do
      [ -z "$line" ] && continue
      echo "[$ide] FAIL: $line in $file"
      file_errors=$((file_errors + 1))
    done <<< "$errors"
  fi

  if [ "$file_errors" -eq 0 ]; then
    local agent_count provider_id active_tier
    agent_count="$(jq '.agents | length' "$file")"
    provider_id="$(jq -r '.provider.id' "$file")"
    active_tier="$(jq -r '.active_tier' "$file")"
    echo "[$ide] OK: $agent_count agents, provider=$provider_id, tier=$active_tier"
  fi

  return "$file_errors"
}

echo "=== FlowForge agent-models.json validator ==="
echo ""

for ide in $IDE_LIST; do
  file="$REPO_ROOT/ide/$ide/config/agent-models.json"
  FILES_CHECKED=$((FILES_CHECKED + 1))
  if ! validate_file "$file" "$ide"; then
    ERRORS=$((ERRORS + 1))
  fi
done

echo ""
echo "=== Summary ==="
echo "Files checked: $FILES_CHECKED"

if [ "$ERRORS" -gt 0 ]; then
  echo "Result: FAIL ($ERRORS file(s) with errors)"
  exit 1
else
  echo "Result: PASS (all $FILES_CHECKED files valid)"
  exit 0
fi
