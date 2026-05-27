#!/usr/bin/env bash
# FlowForge — Installer for Unix (Linux/macOS)
# Usage: bash install.sh [--help]
#
# Detects installed IDEs and installs FlowForge rules, agents, and workflows.
# Supports: OpenCode, Cursor, Antigravity, VS Code

set -euo pipefail

FLOWFORGE_REPO="${FLOWFORGE_REPO:-$(cd "$(dirname "$0")/.." && pwd)}"
BACKUP_DIR="${HOME}/.flowforge-backups/$(date +%Y%m%d-%H%M%S)"
INSTALLED=0

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${BLUE}  FlowForge — Instalador para Unix${NC}"
echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo ""
echo -e "Repositorio: ${YELLOW}$FLOWFORGE_REPO${NC}"
echo ""

# --- Detect OpenCode ---
if [ -d "${HOME}/.config/opencode" ]; then
  echo -e "${GREEN}[✓] OpenCode detectado${NC}"
  BACKUP="${BACKUP_DIR}/opencode"
  mkdir -p "$BACKUP"
  [ -f "${HOME}/.config/opencode/opencode.json" ] && cp "${HOME}/.config/opencode/opencode.json" "$BACKUP/" && echo -e "  Backup: ${YELLOW}$BACKUP/opencode.json${NC}"

  # Copy flowforge agents config (merge guide)
  cp "$FLOWFORGE_REPO/ide/opencode/opencode.flowforge.json" "${HOME}/.config/opencode/opencode.flowforge.json"
  echo -e "  ${GREEN}✓${NC} opencode.flowforge.json copiado"

  # Copy command files
  mkdir -p "${HOME}/.config/opencode/commands"
  for cmd in flow-start flow-dev flow-verify flow-close; do
    cp "$FLOWFORGE_REPO/ide/opencode/commands/${cmd}.md" "${HOME}/.config/opencode/commands/" 2>/dev/null || true
  done
  echo -e "  ${GREEN}✓${NC} Comandos /flow-* copiados"
  INSTALLED=1
fi

# --- Detect Cursor ---
if [ -d "${HOME}/.cursor" ]; then
  echo -e "${GREEN}[✓] Cursor detectado${NC}"
  BACKUP="${BACKUP_DIR}/cursor"
  mkdir -p "$BACKUP"

  # Backup existing rules
  [ -f "${HOME}/.cursor/rules/workflow.mdc" ] && cp "${HOME}/.cursor/rules/workflow.mdc" "$BACKUP/"
  [ -f "${HOME}/.cursor/rules/model-assignments.mdc" ] && cp "${HOME}/.cursor/rules/model-assignments.mdc" "$BACKUP/"

  # Rules
  cp "$FLOWFORGE_REPO/ide/cursor/rules/workflow.mdc" "${HOME}/.cursor/rules/"
  cp "$FLOWFORGE_REPO/ide/cursor/rules/model-assignments.mdc" "${HOME}/.cursor/rules/"
  cp "$FLOWFORGE_REPO/ide/cursor/rules/git-sin-push.mdc" "${HOME}/.cursor/rules/"
  echo -e "  ${GREEN}✓${NC} Rules copiadas"

  # Agents
  mkdir -p "${HOME}/.cursor/agents"
  for agent in forge-discovery forge-arch forge-plan forge-dev forge-verify forge-memory; do
    cp "$FLOWFORGE_REPO/ide/cursor/agents/${agent}.md" "${HOME}/.cursor/agents/" 2>/dev/null && echo -e "  ${GREEN}✓${NC} Agent ${agent}.md copiado"
  done
  INSTALLED=1
fi

# --- Detect Antigravity ---
if [ -d "${HOME}/.gemini" ]; then
  echo -e "${GREEN}[✓] Antigravity detectado${NC}"
  echo -e "  ${YELLOW}Nota:${NC} Las reglas de Antigravity se instalan por proyecto."
  echo -e "  Ejecutá en la raíz de tu proyecto:"
  echo -e "    ${BLUE}bash <(curl -sL https://flowforge.dev/install-project.sh)${NC}"
  echo -e "  O copiá manualmente desde ${YELLOW}$FLOWFORGE_REPO/ide/antigravity/${NC}"
  INSTALLED=1
fi

# --- Detect VS Code ---
if [ -d "${HOME}/.vscode" ] || [ -d "${HOME}/.vscode-server" ]; then
  echo -e "${GREEN}[✓] VS Code detectado${NC}"
  mkdir -p "${HOME}/.vscode"
  cp "$FLOWFORGE_REPO/ide/vscode/copilot-instructions.md" "${HOME}/.vscode/"
  echo -e "  ${GREEN}✓${NC} Copilot instructions copiadas"
  INSTALLED=1
fi

# --- Summary ---
echo ""
echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
if [ "$INSTALLED" -eq 1 ]; then
  echo -e "${GREEN}✅ Instalación completada${NC}"
  echo -e "  Backups en: ${YELLOW}$BACKUP_DIR${NC}"
  echo ""
  echo -e "${YELLOW}📋 Próximos pasos:${NC}"
  echo -e "  1. OpenCode: Mergeá opencode.flowforge.json en tu opencode.json"
  echo -e "  2. Reiniciá tu IDE"
  echo -e "  3. Seleccioná el agente 'flowforge'"
  echo -e "  4. Escribí: /flow-start CRUD de tareas"
else
  echo -e "${RED}❌ No se detectó ningún IDE compatible${NC}"
  echo -e "  Instalá primero: OpenCode, Cursor, Antigravity, o VS Code"
fi
echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
