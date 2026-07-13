#!/usr/bin/env bash
# T-032 — Parity test: bash generate-config.sh output structure
# Full C# vs bash diff requires dotnet + flowforge binary (run in CI when available)
set -euo pipefail

FF_REPO="${1:-$(cd "$(dirname "$0")/.." && pwd)}"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"

for cmd in jq envsubst; do
  command -v "$cmd" >/dev/null 2>&1 || { echo "✗ Missing dependency: $cmd"; exit 1; }
done

HOME_A="$(mktemp -d)"
trap 'rm -rf "$HOME_A"' EXIT

export HOME="$HOME_A"
mkdir -p "$HOME/.config/opencode"

echo "[parity] Running bash generate-config.sh in temp HOME..."
bash "$FF_REPO/ide/opencode/generate-config.sh" "$FF_REPO"

CONFIG="$HOME/.config/opencode/opencode.json"
SIDECAR="$HOME/.config/opencode/.flowforge-managed.json"
RULES="$HOME/.config/opencode/.agents/rules/model-assignments.md"

fail() { echo "✗ $1"; exit 1; }
ok() { echo "✓ $1"; }

[ -f "$CONFIG" ] || fail "opencode.json not created"
[ -f "$SIDECAR" ] || fail "sidecar not created"
[ -f "$RULES" ] || fail "model-assignments.md not created"

# Schema keys
for key in instructions agent provider permission mcp; do
  jq -e ".[\"$key\"]" "$CONFIG" >/dev/null 2>&1 || fail "missing key: $key"
done
ok "required top-level keys present"

# Agent count
AGENT_COUNT=$(jq '.agent | keys | length' "$CONFIG")
[ "$AGENT_COUNT" -ge 8 ] || fail "expected >= 8 agents, got $AGENT_COUNT"
ok "agent count: $AGENT_COUNT"

# Provider opencode-zen only in defaults
PROVIDER_ID=$(jq -r '.provider | keys[0]' "$CONFIG")
[ "$PROVIDER_ID" = "opencode-zen" ] || fail "expected provider opencode-zen, got $PROVIDER_ID"
ok "provider: opencode-zen"

# mcp.engram
TYPE=$(jq -r '.mcp.engram.type' "$CONFIG")
ENABLED=$(jq -r '.mcp.engram.enabled' "$CONFIG")
[ "$TYPE" = "local" ] || fail "mcp.engram.type != local"
[ "$ENABLED" = "true" ] || fail "mcp.engram.enabled != true"
ok "mcp.engram configured"

# No stale models in model-assignments
if grep -qE 'claude-|gpt-|opencode-go/' "$RULES" 2>/dev/null; then
  fail "stale models in model-assignments.md"
fi
ok "model-assignments.md clean"

# PII scan — installed config MAY contain user's $HOME (resolved at install).
# Block only hardcoded third-party PII, not runtime home paths.
if grep -qE '@local\.dev|OPENCODIGO_API_KEY|DEEPSEEK_API_KEY|MINIMAX_API_KEY' "$CONFIG" 2>/dev/null; then
  fail "PII patterns in generated opencode.json"
fi
ok "PII scan pass (no API keys / @local.dev)"

echo ""
echo "✅ Parity smoke test PASS (bash path)"
echo "   Full C# vs bash byte-diff: run in CI with dotnet SDK (T-032 complete gate)"

exit 0
