#!/usr/bin/env bash
# FlowForge — Installer for Unix (Linux/macOS)
# Usage:
#   Global:   bash install.sh
#   Project:  bash install.sh /path/to/project
#   Remote:   curl -sSL https://raw.githubusercontent.com/efreet111/FlowForge/main/ide/install.sh | bash
#
# Installs shared parity, Cursor, OpenCode bundle, VS Code agents, optional project bundle.

set -euo pipefail

PROJECT_PATH=""
while [[ $# -gt 0 ]]; do
  case "$1" in
    --ide)
      shift 2
      ;;
    --yes|--no-engram)
      shift
      ;;
    *)
      PROJECT_PATH="$1"
      shift
      ;;
  esac
done

# ── Locate FlowForge repo ─────────────────────────────────────────────────
if [ -z "${FLOWFORGE_REPO:-}" ]; then
  SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd 2>/dev/null || echo "")"
  if [ -n "$SCRIPT_DIR" ] && [ -f "$SCRIPT_DIR/../AGENTS.md" ] && grep -q "FlowForge" "$SCRIPT_DIR/../AGENTS.md" 2>/dev/null; then
    FLOWFORGE_REPO="$(cd "$SCRIPT_DIR/.." && pwd)"
    echo "Modo local: $FLOWFORGE_REPO"
  else
    echo "Modo remoto: descargando FlowForge..."
    TEMP_DIR="$(mktemp -d 2>/dev/null || mktemp -d -t flowforge-install)"
    trap 'rm -rf "$TEMP_DIR"' EXIT
    git clone --depth 1 https://github.com/efreet111/FlowForge.git "$TEMP_DIR"
    FLOWFORGE_REPO="$TEMP_DIR"
  fi
fi

IDE_DIR="$FLOWFORGE_REPO/ide"
BACKUP_DIR="${HOME}/.flowforge-backups/$(date +%Y%m%d-%H%M%S)"
GLOBAL_SHARED="${HOME}/.flowforge/shared"
INSTALLED=0
AGENT_MODELS_JSON="$IDE_DIR/opencode/templates/agent-models.json"
OPENCODE_TEMPLATE="$IDE_DIR/opencode/templates/opencode.json.tpl"
PROVIDER="${FLOWFORGE_PROVIDER:-opencode-zen}"
if command -v jq >/dev/null 2>&1 && [ -f "$OPENCODE_TEMPLATE" ]; then
  if ! jq -e --arg provider "$PROVIDER" '.provider[$provider]' "$OPENCODE_TEMPLATE" >/dev/null 2>&1; then
    if jq -e '.provider["opencode-zen"]' "$OPENCODE_TEMPLATE" >/dev/null 2>&1; then
      PROVIDER="opencode-zen"
    else
      PROVIDER="$(jq -r '.provider | keys[0] // \"opencode-zen\"' "$OPENCODE_TEMPLATE" 2>/dev/null || echo "opencode-zen")"
    fi
  fi
fi
format_model_reference() {
  local raw="$1"
  if [[ "$raw" == */* ]]; then
    printf '%s' "$raw"
  else
    printf '%s/%s' "$PROVIDER" "$raw"
  fi
}

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

install_shared() {
  local dest="$1"
  mkdir -p "$dest"
  cp -r "$IDE_DIR/shared/"* "$dest/" 2>/dev/null || true
}

patch_opencode_flowforge_json() {
  local dest="$1"
  local repo="$2"
  if [ ! -f "$dest" ]; then return 0; fi
  if ! command -v python3 >/dev/null 2>&1 && ! command -v python >/dev/null 2>&1; then
    echo -e "  ${YELLOW}! Python no encontrado; rutas skills sin parchear en opencode.flowforge.json${NC}"
    echo -e "  ${YELLOW}  Reemplaza __FLOWFORGE_REPO__ manualmente por: $repo${NC}"
    return 0
  fi
  local py=python3
  command -v python3 >/dev/null 2>&1 || py=python
  FF_REPO="$repo" DEST="$dest" $py -c "
import os
from pathlib import Path
dest = Path(os.environ['DEST'])
repo = os.environ['FF_REPO']
text = dest.read_text(encoding='utf-8')
if '__FLOWFORGE_REPO__' in text:
    dest.write_text(text.replace('__FLOWFORGE_REPO__', repo), encoding='utf-8')
" && echo -e "  ${GREEN}OK${NC} Rutas skills → $repo"
}

compile_cursor_agents() {
  local compile="$IDE_DIR/cursor/compile-agents-from-skills.py"
  if [ ! -f "$compile" ]; then return 0; fi
  if ! command -v python3 >/dev/null 2>&1 && ! command -v python >/dev/null 2>&1; then
    echo -e "  ${YELLOW}! Python no encontrado; agentes sin recompilar${NC}"
    return 0
  fi
  local py=python3
  command -v python3 >/dev/null 2>&1 || py=python
  if (cd "$FLOWFORGE_REPO" && $py "$compile"); then
    echo -e "  ${GREEN}OK${NC} Agentes Cursor recompilados"
  else
    # Non-fatal: an optional pack must never abort the whole installer.
    echo -e "  ${YELLOW}! compile-agents falló; se conservan los agentes Cursor existentes${NC}"
  fi
}

install_antigravity_skills() {
  local dest="$1"
  mkdir -p "$dest"
  # ADR-019 Amendment: install EVERY skill dir with a SKILL.md — including the
  # `flowforge` orchestrator skill, which was previously skipped.
  for skill_dir in "$FLOWFORGE_REPO/skills"/*; do
    [ -d "$skill_dir" ] || continue
    [ -f "$skill_dir/SKILL.md" ] || continue
    local name
    name="$(basename "$skill_dir")"
    local target="$dest/$name"
    rm -rf "$target" 2>/dev/null || true
    if [[ "$FLOWFORGE_REPO" == /tmp/* ]] || [[ "$FLOWFORGE_REPO" == *flowforge-install* ]]; then
      cp -a "$skill_dir" "$target"
    else
      ln -sfn "$skill_dir" "$target"
    fi
  done
}

# Strip a leading YAML frontmatter block (used for ~/.gemini/GEMINI.md, which
# must not carry frontmatter — the whole file is always_on).
strip_yaml_frontmatter() {
  awk '
    BEGIN { in_fm=0; done=0; started=0 }
    !done && NR==1 && /^---[[:space:]]*$/ { in_fm=1; next }
    !done && in_fm && /^---[[:space:]]*$/ { in_fm=0; done=1; next }
    !in_fm {
      if (!started && $0 ~ /^[[:space:]]*$/) next
      started=1
      print
    }
  ' "$1"
}

# Extract the single-line description: value from a workflow file's frontmatter.
workflow_description() {
  awk '
    BEGIN { in_fm=0 }
    NR==1 && /^---[[:space:]]*$/ { in_fm=1; next }
    in_fm && /^---[[:space:]]*$/ { exit }
    in_fm && /^description:/ { sub(/^description:[[:space:]]*/, ""); print; exit }
  ' "$1"
}

# ADR-019 Amendment: legacy workflows are dead (2026-10-19). Generate one skill
# per ide/antigravity/workflows/flow-*.md: <dest>/<flow-name>/SKILL.md.
generate_antigravity_command_skills() {
  local dest="$1"
  local src="${2:-$IDE_DIR/antigravity/workflows}"
  [ -d "$src" ] || return 0
  mkdir -p "$dest"
  for wf in "$src"/flow-*.md; do
    [ -f "$wf" ] || continue
    local name desc target_dir
    name="$(basename "$wf" .md)"
    desc="$(workflow_description "$wf")"
    [ -n "$desc" ] || desc="FlowForge command $name"
    target_dir="$dest/$name"
    rm -rf "$target_dir" 2>/dev/null || true
    mkdir -p "$target_dir"
    {
      echo "---"
      echo "name: $name"
      echo "description: $desc"
      echo "---"
      echo ""
      strip_yaml_frontmatter "$wf"
    } > "$target_dir/SKILL.md"
  done
}

cleanup_legacy_antigravity_pack() {
  local leg="${HOME}/.gemini/antigravity"
  rm -f "$leg/AGENTS.md" 2>/dev/null || true
  rm -rf "$leg/rules" "$leg/workflows" 2>/dev/null || true
  rm -f "${HOME}/.gemini/config/skills.json" 2>/dev/null || true
}

# HU-042: Auto-install context7 MCP for non-local Engram profiles
install_context7_mcp() {
  # Profile detection: env var, ENGRAM_PROFILE, or config file (3-tier)
  local is_non_local=0

  # 1. ENGRAM_SERVER_URL (scripts/CI)
  if [ -n "${ENGRAM_SERVER_URL:-}" ]; then
    echo -e "  ${GREEN}✓${NC} Non-local profile detected via ENGRAM_SERVER_URL"
    is_non_local=1
  # 2. ENGRAM_PROFILE != "local"
  elif [ "${ENGRAM_PROFILE:-local}" != "local" ]; then
    echo -e "  ${GREEN}✓${NC} Non-local profile detected via ENGRAM_PROFILE=${ENGRAM_PROFILE}"
    is_non_local=1
  # 3. ~/.engram/config.json with sync config
  elif [ -f "${HOME}/.engram/config.json" ] && grep -q '"sync"' "${HOME}/.engram/config.json" 2>/dev/null; then
    echo -e "  ${GREEN}✓${NC} Non-local profile detected via ~/.engram/config.json"
    is_non_local=1
  fi

  if [ "$is_non_local" -eq 0 ]; then
    echo -e "  ${YELLOW}→${NC} Local profile — skipping context7 MCP"
    return 0
  fi

  # Check if opencode is installed
  if ! command -v opencode >/dev/null 2>&1; then
    echo -e "  ${YELLOW}!${NC} opencode not found — skipping context7 MCP"
    return 0
  fi

  # Idempotency: check if already installed
  if opencode mcp list 2>/dev/null | grep -q "context7"; then
    echo -e "  ${GREEN}✓${NC} context7 MCP already installed"
    return 0
  fi

  echo -e "  ${YELLOW}→${NC} Installing context7 MCP..."
  if opencode mcp add context7 --url https://mcp.context7.com/mcp --global 2>/dev/null; then
    echo -e "  ${GREEN}OK${NC} context7 MCP installed"
  else
    echo -e "  ${YELLOW}!${NC} context7 MCP installation failed — manual fix required:"
    echo -e "     ${BLUE}opencode mcp add context7 --url https://mcp.context7.com/mcp --global${NC}"
  fi
}

generate_antigravity_model_assignments() {
  local dest="$1"
  local json_file="$IDE_DIR/antigravity/config/agent-models.json"
  if ! command -v jq >/dev/null 2>&1 || [ ! -f "$json_file" ]; then return 0; fi

  # Reads a field from ide/antigravity/config/agent-models.json, tolerating the
  # per-provider V2 schema (`"model": { "<provider>": "..." }`) and the
  # `flowforge` → `forge-orchestrator` rename in the JSON.
  antigravity_agent_field() {
    local agent="$1" field="$2" value
    value="$(jq -r --arg a "$agent" --arg f "$field" '
      (.agents[$a] // {})[$f]
      | if . == null then ""
        elif type == "object" then (to_entries | sort_by(.key) | .[0].value // "")
        else tostring end
    ' "$json_file")"
    if [ -z "$value" ] && [ "$agent" = "flowforge" ]; then
      value="$(jq -r --arg a "forge-orchestrator" --arg f "$field" '
        (.agents[$a] // {})[$f]
        | if . == null then ""
          elif type == "object" then (to_entries | sort_by(.key) | .[0].value // "")
          else tostring end
      ' "$json_file")"
    fi
    printf '%s' "$value"
  }

  local tmp_rules
  tmp_rules="$(mktemp "${dest}/model-assignments.md.XXXXXX")"
  {
    echo "---"
    echo "trigger: model_decision"
    echo "description: Model assignments for FlowForge agents (Antigravity — Gemini models)."
    echo "---"
    echo ""
    echo "## Model Assignments"
    echo ""
    echo "The orchestrator reads this table at session start. When delegating, use the preferred model."
    echo ""
    echo "| Agent | Preferred Model | Fallback | Mode | Purpose |"
    echo "|-------|----------------|----------|------|---------|"
    for agent in flowforge forge-discovery forge-arch forge-plan forge-dev forge-verify forge-memory forge-teacher default; do
      local model fallback mode purpose
      model="$(antigravity_agent_field "$agent" model)"
      fallback="$(antigravity_agent_field "$agent" fallback)"
      mode="$(antigravity_agent_field "$agent" mode)"
      purpose="$(antigravity_agent_field "$agent" purpose)"
      echo "| $agent | \`$model\` | \`$fallback\` | $mode | $purpose |"
    done
    echo ""
    echo "<!-- Generated by FlowForge installer from ide/antigravity/config/agent-models.json -->"
  } > "$tmp_rules"
  mv "$tmp_rules" "${dest}/model-assignments.md"
}

migrate_legacy_antigravity_workflows() {
  local cfg="${HOME}/.gemini/config"
  # ADR-019 Amendment: both legacy workflow dirs are dead — migrate to skills.
  for legacy in "$cfg/workflows" "$cfg/global_workflows"; do
    [ -d "$legacy" ] || continue
    for f in "$legacy"/flow-*.md; do
      [ -f "$f" ] || continue
      local name desc dest
      name="$(basename "$f" .md)"
      dest="$cfg/skills/$name"
      # A dangling symlink at the destination (legacy install pointing at a moved
      # clone) makes `mkdir -p` fail with EEXIST and aborts the whole installer.
      if [ -L "$dest" ] || { [ -e "$dest" ] && [ ! -d "$dest" ]; }; then
        rm -rf "$dest" 2>/dev/null || true
      fi
      if [ ! -f "$dest/SKILL.md" ]; then
        desc="$(workflow_description "$f")"
        [ -n "$desc" ] || desc="FlowForge command $name"
        mkdir -p "$dest"
        {
          echo "---"
          echo "name: $name"
          echo "description: $desc"
          echo "---"
          echo ""
          strip_yaml_frontmatter "$f"
        } > "$dest/SKILL.md"
      fi
      rm -f "$f" 2>/dev/null || true
    done
    rmdir "$legacy" 2>/dev/null || true
  done
}

install_antigravity_global() {
  local cfg="${HOME}/.gemini/config"
  mkdir -p "$cfg/rules" "$cfg/skills" "$cfg/agents"
  mkdir -p "$cfg/.agents/rules" "$cfg/.agents/skills" "$cfg/.agents/agents"
  migrate_legacy_antigravity_workflows
  cp "$IDE_DIR/antigravity/AGENTS.md" "$cfg/" 2>/dev/null || true
  cp "$IDE_DIR/antigravity/AGENTS.md" "$cfg/.agents/" 2>/dev/null || true
  cp "$IDE_DIR/antigravity/rules/"*.md "$cfg/rules/" 2>/dev/null || true
  cp "$IDE_DIR/antigravity/rules/"*.md "$cfg/.agents/rules/" 2>/dev/null || true
  cp "$IDE_DIR/antigravity/agents/"*.md "$cfg/agents/" 2>/dev/null || true
  cp "$IDE_DIR/antigravity/agents/"*.md "$cfg/.agents/agents/" 2>/dev/null || true
  install_antigravity_skills "$cfg/skills"
  install_antigravity_skills "$cfg/.agents/skills"
  generate_antigravity_command_skills "$cfg/skills" "$IDE_DIR/antigravity/workflows"
  generate_antigravity_command_skills "$cfg/.agents/skills" "$IDE_DIR/antigravity/workflows"
  generate_antigravity_model_assignments "$cfg/rules"
  generate_antigravity_model_assignments "$cfg/.agents/rules"
  if [ -f "$IDE_DIR/antigravity/rules/workflow.md" ]; then
    mkdir -p "${HOME}/.gemini"
    # GEMINI.md must NOT have frontmatter — the whole file is always_on.
    strip_yaml_frontmatter "$IDE_DIR/antigravity/rules/workflow.md" > "${HOME}/.gemini/GEMINI.md" 2>/dev/null || true
  fi
  cleanup_legacy_antigravity_pack
}

install_project() {
  local root="$1"
  if [ ! -d "$root" ]; then
    echo -e "${RED}ProjectPath no existe: $root${NC}"
    return 1
  fi
  echo -e "${GREEN}[*] Instalacion por proyecto: $root${NC}"

  mkdir -p "$root/.agents/rules" "$root/.agents/agents" "$root/.agents/skills"
  cp -r "$IDE_DIR/antigravity/rules/"* "$root/.agents/rules/"
  cp -r "$IDE_DIR/antigravity/agents/"* "$root/.agents/agents/"
  generate_antigravity_model_assignments "$root/.agents/rules"
  cp "$IDE_DIR/antigravity/AGENTS.md" "$root/.agents/AGENTS.md"
  install_antigravity_skills "$root/.agents/skills"
  generate_antigravity_command_skills "$root/.agents/skills" "$IDE_DIR/antigravity/workflows"
  if [ ! -f "$root/AGENTS.md" ]; then
    cp "$IDE_DIR/antigravity/AGENTS.md" "$root/"
  fi
  echo -e "  ${GREEN}OK${NC} .agents/ (rules + agents + skills + AGENTS.md en raíz solo si no existía)"

  install_shared "$root/.flowforge/shared"
  echo -e "  ${GREEN}OK${NC} .flowforge/shared/"

  mkdir -p "$root/.cursor/rules" "$root/.cursor/agents" "$root/.cursor/commands"
  cp "$IDE_DIR/cursor/rules/"*.mdc "$root/.cursor/rules/" 2>/dev/null || true
  cp "$IDE_DIR/cursor/agents/"forge-*.md "$root/.cursor/agents/" 2>/dev/null || true
  cp "$IDE_DIR/cursor/commands/"*.md "$root/.cursor/commands/" 2>/dev/null || true
  echo -e "  ${GREEN}OK${NC} .cursor/"

  mkdir -p "$root/.github/agents" "$root/.opencode/agents" "$root/.opencode/commands" "$root/.kilo/agents"
  cp "$IDE_DIR/vscode/agents/"*.agent.md "$root/.github/agents/" 2>/dev/null || true
  cp "$IDE_DIR/vscode/copilot-instructions.md" "$root/.github/" 2>/dev/null || true
  cp "$IDE_DIR/opencode/agents/"*.md "$root/.opencode/agents/" 2>/dev/null || true
  cp "$IDE_DIR/opencode/commands/"*.md "$root/.opencode/commands/" 2>/dev/null || true
  cp "$IDE_DIR/opencode/agents/"*.md "$root/.kilo/agents/" 2>/dev/null || true
  # HU-040 / ADR-011 — Copilot Chat slash commands into the project scope.
  if [ -d "$IDE_DIR/vscode/prompts" ]; then
    mkdir -p "$root/.github/prompts"
    cp "$IDE_DIR/vscode/prompts/"*.prompt.md "$root/.github/prompts/" 2>/dev/null || true
  fi
  echo -e "  ${GREEN}OK${NC} .github/agents + .github/prompts + .opencode/agents + .opencode/commands + .kilo/agents"
}

echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}  FlowForge - Instalador Unix${NC}"
echo -e "${BLUE}========================================${NC}"
echo ""

# Global shared parity
install_shared "$GLOBAL_SHARED"
echo -e "${GREEN}[OK]${NC} Paridad global: $GLOBAL_SHARED"

compile_cursor_agents

# --- OpenCode ---
# Generates opencode.json (not just copy agents). See ide/opencode/generate-config.sh
if [ -d "${HOME}/.config/opencode" ] || mkdir -p "${HOME}/.config/opencode" 2>/dev/null; then
  echo -e "${GREEN}[OK] OpenCode detectado${NC}"

  # Backup existing config
  OC_BACKUP="${BACKUP_DIR}/opencode"
  mkdir -p "$OC_BACKUP"
  [ -f "${HOME}/.config/opencode/opencode.json" ] && \
    cp "${HOME}/.config/opencode/opencode.json" "$OC_BACKUP/" 2>/dev/null || true
  [ -f "${HOME}/.config/opencode/opencode.jsonc" ] && \
    cp "${HOME}/.config/opencode/opencode.jsonc" "$OC_BACKUP/" 2>/dev/null || true

  # Generate opencode.json + model-assignments.md + sidecar
  if command -v flowforge >/dev/null 2>&1; then
    flowforge install --ide opencode --yes 2>/dev/null && \
      echo -e "  ${GREEN}OK${NC} opencode.json via flowforge binary" || \
      echo -e "  ${YELLOW}! flowforge install failed; trying bash fallback${NC}"
  fi
  if [ ! -f "${HOME}/.config/opencode/opencode.json" ] || \
     ! command -v flowforge >/dev/null 2>&1; then
    if [ -f "$IDE_DIR/opencode/generate-config.sh" ]; then
      bash "$IDE_DIR/opencode/generate-config.sh" "$FLOWFORGE_REPO" && \
        echo -e "  ${GREEN}OK${NC} opencode.json via bash generate-config.sh"
    else
      echo -e "  ${RED}✗ generate-config.sh not found${NC}"
    fi
    if ! command -v flowforge >/dev/null 2>&1; then
      echo -e "  ${YELLOW}! Instalar binario flowforge recomendado para futuras actualizaciones${NC}"
    fi
  fi

  # Copy agents (strip .tpl, patch model: from agent-models.json)
  mkdir -p "${HOME}/.config/opencode/agents"
  if [ -d "$IDE_DIR/opencode/templates/agents" ]; then
    for tpl in "$IDE_DIR/opencode/templates/agents/"*.md.tpl; do
      [ -f "$tpl" ] || continue
      dest_name="$(basename "$tpl" .tpl)"
      dest="${HOME}/.config/opencode/agents/${dest_name}"
      cp "$tpl" "$dest"
      agent_key="${dest_name%.md}"
      # HU-041 fix: agent-models.json migrated to a per-provider schema in commit 2637562
      # ({"agents":{"forge-dev":{"opencode-zen":"...","opencode-go":"..."}}}) while older
      # caches use ({"tiers":{"<tier>":{"agents":{"forge-dev":{"model":"..."}}}}}).
      # Probe both shapes, then validate the result is a single-line token before sed.
      # If neither matches, we skip the rewrite entirely and leave the template's
      # model: line untouched — never clobber a working assignment with garbage.
      if command -v jq >/dev/null 2>&1 && [ -f "$IDE_DIR/opencode/config/agent-models.json" ]; then
        models_file="$IDE_DIR/opencode/config/agent-models.json"
        provider="${FLOWFORGE_PROVIDER:-opencode-zen}"
        model=""
        # Shape A (current repo schema): .agents.<key>.<provider>
        model=$(jq -r --arg k "$agent_key" --arg p "$provider" \
          '.agents[$k][$p] // empty' "$models_file" 2>/dev/null || echo "")
        # Shape B (legacy cache schema): .tiers.<active_tier>.agents.<key>.model
        if [ -z "$model" ]; then
          model=$(jq -r --arg k "$agent_key" \
            '.tiers[.active_tier].agents[$k].model // empty' "$models_file" 2>/dev/null || echo "")
        fi
        # Shape C (legacy flat schema): .agents.<key>.model
        if [ -z "$model" ]; then
          model=$(jq -r --arg k "$agent_key" \
            '.agents[$k].model // empty' "$models_file" 2>/dev/null || echo "")
        fi
        # Only rewrite when we resolved a clean single-line token (no spaces, quotes,
        # braces or newlines). Anything else means we mis-parsed the schema — bail out
        # and preserve whatever the template already had.
        case "$model" in
          ''|*[[:space:]]*|*'{'*|*'}'*|*'"'* )
            model="" ;;
        esac
        if [ -n "$model" ]; then
          sed -i "s|^model:.*|model: ${provider}/${model}|" "$dest" 2>/dev/null || true
        fi
      fi
    done
  elif [ -d "$IDE_DIR/opencode/agents" ]; then
    cp "$IDE_DIR/opencode/agents/"*.md "${HOME}/.config/opencode/agents/" 2>/dev/null || true
  fi

  # Copy commands
  mkdir -p "${HOME}/.config/opencode/commands"
  [ -d "$IDE_DIR/opencode/commands" ] && \
    cp "$IDE_DIR/opencode/commands/"*.md "${HOME}/.config/opencode/commands/" 2>/dev/null || true

  # Cleanup legacy paths
  [ -d "${HOME}/.config/opencode/flowforge" ] && rm -rf "${HOME}/.config/opencode/flowforge"
  [ -f "${HOME}/.config/opencode/opencode.flowforge.json" ] && rm -f "${HOME}/.config/opencode/opencode.flowforge.json"

  echo -e "  ${GREEN}OK${NC} ~/.config/opencode/ (config + agents + commands)"
  echo -e "  ${YELLOW}⚠ Free Zen models may use your data for training. See docs/PII-POLICY.md${NC}"
  
  # HU-042: Auto-install context7 MCP for non-local profiles
  install_context7_mcp
  
  INSTALLED=1
fi

# --- Cursor ---
if [ -d "${HOME}/.cursor" ]; then
  echo -e "${GREEN}[OK] Cursor detectado${NC}"
  BACKUP="${BACKUP_DIR}/cursor"
  mkdir -p "$BACKUP" "${HOME}/.cursor/rules" "${HOME}/.cursor/agents" "${HOME}/.cursor/commands"
  [ -f "${HOME}/.cursor/rules/workflow.mdc" ] && cp "${HOME}/.cursor/rules/workflow.mdc" "$BACKUP/"
  cp "$IDE_DIR/cursor/rules/"*.mdc "${HOME}/.cursor/rules/" 2>/dev/null || true
  cp "$IDE_DIR/cursor/agents/"forge-*.md "${HOME}/.cursor/agents/" 2>/dev/null || true
  cp "$IDE_DIR/cursor/commands/"*.md "${HOME}/.cursor/commands/" 2>/dev/null || true
  echo -e "  ${GREEN}OK${NC} ~/.cursor/rules + agents + commands"
  INSTALLED=1
fi

# --- VS Code ---
# Detect the prompts directory for the GitHub Copilot Chat picker.
# Supports both native installs (Linux/macOS/Windows — $HOME/.config/Code/User/prompts)
# and Flatpak installs ($HOME/.var/app/com.visualstudio.code/config/Code/User/prompts).
# HU-040 / ADR-011. Bash 3.2+ POSIX-compliant.
detect_vscode_prompts_path() {
  if [ -d "${HOME}/.var/app/com.visualstudio.code" ]; then
    printf '%s' "${HOME}/.var/app/com.visualstudio.code/config/Code/User/prompts"
  else
    printf '%s' "${HOME}/.config/Code/User/prompts"
  fi
}

if [ -d "${HOME}/.vscode" ] || [ -d "${HOME}/.vscode-server" ]; then
  echo -e "${GREEN}[OK] VS Code detectado${NC}"
  HAS_COPILOT=0
  HAS_KILO=0
  if ls "${HOME}/.vscode/extensions"/github.copilot* &>/dev/null; then HAS_COPILOT=1; fi
  if ls "${HOME}/.vscode/extensions"/kilocode.* &>/dev/null; then HAS_KILO=1; fi

  BEST_EFFORT=0
  if [ "$HAS_COPILOT" -eq 0 ] && [ "$HAS_KILO" -eq 0 ]; then
    BEST_EFFORT=1
  fi

  if [ "$HAS_COPILOT" -eq 1 ] || { [ "$HAS_COPILOT" -eq 0 ] && [ "$HAS_KILO" -eq 0 ]; }; then
    mkdir -p "${HOME}/.copilot/agents" "${HOME}/.copilot/instructions"
    cp "$IDE_DIR/vscode/agents/"*.agent.md "${HOME}/.copilot/agents/" 2>/dev/null || true
    if [ -f "$IDE_DIR/vscode/copilot-instructions.md" ]; then
      {
        echo "---"
        echo "applyTo: '**'"
        echo "---"
        cat "$IDE_DIR/vscode/copilot-instructions.md"
      } > "${HOME}/.copilot/instructions/flowforge.instructions.md"
    fi
    echo -e "  ${GREEN}OK${NC} GitHub Copilot → ~/.copilot/agents/"

    # HU-040 / ADR-011 — Copilot Chat slash commands (prompts pack).
    # Detects Flatpak sandbox path automatically; backs up existing prompts dir
    # before overwriting (T-B7 — NFR-2 atomicity + rollback safety).
    if [ -d "$IDE_DIR/vscode/prompts" ]; then
      VSCODE_PROMPTS_DEST="$(detect_vscode_prompts_path)"
      # T-B7: backup preflight of the destination prompts dir.
      if [ -d "$VSCODE_PROMPTS_DEST" ]; then
        mkdir -p "${BACKUP_DIR}/vscode-prompts"
        cp -r "$VSCODE_PROMPTS_DEST/." "${BACKUP_DIR}/vscode-prompts/" 2>/dev/null || true
      fi
      mkdir -p "$VSCODE_PROMPTS_DEST"
      cp "$IDE_DIR/vscode/prompts/"*.prompt.md "$VSCODE_PROMPTS_DEST/" 2>/dev/null || true
      echo -e "  ${GREEN}OK${NC} GitHub Copilot prompts → $VSCODE_PROMPTS_DEST"
    fi
  fi

  if [ "$HAS_KILO" -eq 1 ] || [ "$HAS_COPILOT" -eq 0 ]; then
    mkdir -p "${HOME}/.config/kilo/agents"
    cp "$IDE_DIR/opencode/agents/"*.md "${HOME}/.config/kilo/agents/" 2>/dev/null || true
    echo -e "  ${GREEN}OK${NC} Kilo Code → ~/.config/kilo/agents/"
  fi

  if [ "$BEST_EFFORT" -eq 1 ]; then
    echo -e "  ${YELLOW}! No se detectó GitHub Copilot ni Kilo Code — instalados ambos formatos${NC}"
  fi

  echo -e "  ${YELLOW}! Repo: bash install.sh /path/to/project${NC}"
  INSTALLED=1
fi

# --- Antigravity ---
if [ -d "${HOME}/.gemini" ]; then
  echo -e "${GREEN}[OK] Antigravity detectado${NC}"
  install_antigravity_global
  echo -e "  ${GREEN}OK${NC} ~/.gemini/config/ (AGENTS + rules + agents + skills)"
  INSTALLED=1
fi

# --- Project bundle ---
if [ -n "$PROJECT_PATH" ]; then
  install_project "$(cd "$PROJECT_PATH" && pwd)"
fi

echo ""
echo -e "${BLUE}========================================${NC}"
if [ "$INSTALLED" -eq 1 ]; then
  echo -e "${GREEN}Instalacion completada${NC}"
  [ -d "$BACKUP_DIR" ] && echo -e "  Backups: ${YELLOW}$BACKUP_DIR${NC}"
  echo ""
  echo -e "${YELLOW}Proximos pasos:${NC}"
  echo "  1. Reload Window en el IDE"
  echo "  2. Proyecto: bash install.sh <ruta-repo>"
  echo "  3. /flow-start <feature>  o  /flow-rework"
  echo ""
  echo "Paridad: $GLOBAL_SHARED/workflow-orchestrator-parity.md"
else
  echo -e "${RED}No se detecto IDE compatible${NC}"
fi
echo -e "${BLUE}========================================${NC}"
