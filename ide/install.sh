#!/usr/bin/env bash
# FlowForge — Installer for Unix (Linux/macOS)
# Usage:
#   Global:   bash install.sh
#   Project:  bash install.sh /path/to/project
#   Remote:   curl -sSL https://raw.githubusercontent.com/efreet111/FlowForge/main/ide/install.sh | bash
#
# Installs shared parity, Cursor, OpenCode bundle, VS Code agents, optional project bundle.

set -euo pipefail

PROJECT_PATH="${1:-}"

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
  FF_REPO="$repo" $py -c "
import os
from pathlib import Path
dest = Path(os.environ['DEST'])
repo = os.environ['FF_REPO']
text = dest.read_text(encoding='utf-8')
if '__FLOWFORGE_REPO__' in text:
    dest.write_text(text.replace('__FLOWFORGE_REPO__', repo), encoding='utf-8')
" DEST="$dest" && echo -e "  ${GREEN}OK${NC} Rutas skills → $repo"
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
  (cd "$FLOWFORGE_REPO" && $py "$compile") && echo -e "  ${GREEN}OK${NC} Agentes Cursor recompilados"
}

install_project() {
  local root="$1"
  if [ ! -d "$root" ]; then
    echo -e "${RED}ProjectPath no existe: $root${NC}"
    return 1
  fi
  echo -e "${GREEN}[*] Instalacion por proyecto: $root${NC}"

  mkdir -p "$root/.agents/rules" "$root/.agents/workflows"
  cp -r "$IDE_DIR/antigravity/rules/"* "$root/.agents/rules/"
  cp -r "$IDE_DIR/antigravity/workflows/"* "$root/.agents/workflows/"
  cp "$IDE_DIR/antigravity/AGENTS.md" "$root/"
  echo -e "  ${GREEN}OK${NC} .agents/"

  install_shared "$root/.flowforge/shared"
  echo -e "  ${GREEN}OK${NC} .flowforge/shared/"

  mkdir -p "$root/.cursor/rules" "$root/.cursor/agents" "$root/.cursor/commands"
  cp "$IDE_DIR/cursor/rules/"*.mdc "$root/.cursor/rules/" 2>/dev/null || true
  cp "$IDE_DIR/cursor/agents/"forge-*.md "$root/.cursor/agents/" 2>/dev/null || true
  cp "$IDE_DIR/cursor/commands/"*.md "$root/.cursor/commands/" 2>/dev/null || true
  echo -e "  ${GREEN}OK${NC} .cursor/"

  mkdir -p "$root/.github/agents" "$root/.vscode"
  cp "$IDE_DIR/vscode/agents/"*.agent.md "$root/.github/agents/" 2>/dev/null || true
  cp "$IDE_DIR/vscode/copilot-instructions.md" "$root/.vscode/" 2>/dev/null || true
  echo -e "  ${GREEN}OK${NC} .github/agents + .vscode/"
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
if [ -d "${HOME}/.config/opencode" ]; then
  echo -e "${GREEN}[OK] OpenCode detectado${NC}"
  mkdir -p "${HOME}/.config/opencode/flowforge/shared"
  cp "$IDE_DIR/opencode/AGENTS.md" "${HOME}/.config/opencode/flowforge/"
  install_shared "${HOME}/.config/opencode/flowforge/shared"
  cp "$IDE_DIR/opencode/opencode.flowforge.json" "${HOME}/.config/opencode/"
  patch_opencode_flowforge_json "${HOME}/.config/opencode/opencode.flowforge.json" "$FLOWFORGE_REPO"
  echo -e "  ${GREEN}OK${NC} ~/.config/opencode/flowforge/"
  echo -e "  ${YELLOW}! Merge manual: agent{} de opencode.flowforge.json → opencode.json o opencode.jsonc${NC}"
  echo -e "  ${YELLOW}! Conserva tus bloques mcp/permission al mergear (no reemplaces todo el archivo)${NC}"
  echo -e "  ${YELLOW}! Modelos opencode-go/*: configura proveedor + API keys en OpenCode${NC}"
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
if [ -d "${HOME}/.vscode" ] || [ -d "${HOME}/.vscode-server" ]; then
  echo -e "${GREEN}[OK] VS Code detectado${NC}"
  mkdir -p "${HOME}/.vscode/agents"
  cp "$IDE_DIR/vscode/copilot-instructions.md" "${HOME}/.vscode/" 2>/dev/null || true
  cp "$IDE_DIR/vscode/agents/"*.agent.md "${HOME}/.vscode/agents/" 2>/dev/null || true
  echo -e "  ${GREEN}OK${NC} ~/.vscode/"
  echo -e "  ${YELLOW}! Repo: bash install.sh /path/to/project${NC}"
  INSTALLED=1
fi

# --- Antigravity hint ---
if [ -d "${HOME}/.gemini" ]; then
  echo -e "${GREEN}[OK] Antigravity detectado${NC}"
  if [ -z "$PROJECT_PATH" ]; then
    echo -e "  ${YELLOW}Usa: bash install.sh .  (desde la raiz del proyecto)${NC}"
  fi
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
