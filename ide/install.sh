#!/usr/bin/env bash
# FlowForge — Installer for Unix (Linux/macOS)
# Usage:
#   Local:  bash install.sh
#   Remote: curl -sSL https://raw.githubusercontent.com/efreet111/FlowForge/main/ide/install.sh | bash
#
# Detects installed IDEs and installs FlowForge rules, agents, and workflows.
# Supports: OpenCode, Cursor, Antigravity, VS Code

set -euo pipefail

# ── Remote mode detection ─────────────────────────────────────────────────
# If FLOWFORGE_REPO is not set and we're not inside the FlowForge repo,
# clone it temporarily.
if [ -z "${FLOWFORGE_REPO:-}" ]; then
  SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd 2>/dev/null || echo "")"
  if [ -n "$SCRIPT_DIR" ] && [ -f "$SCRIPT_DIR/../AGENTS.md" ] && grep -q "FlowForge" "$SCRIPT_DIR/../AGENTS.md" 2>/dev/null; then
    # Running locally from the repo
    FLOWFORGE_REPO="$(cd "$SCRIPT_DIR/.." && pwd)"
    echo -e "📦 Modo local: $FLOWFORGE_REPO"
  else
    # Running remotely — clone repo
    echo -e "🌐 Modo remoto: descargando FlowForge..."
    # mktemp portability (Linux/macOS)
    if command -v mktemp >/dev/null 2>&1; then
      TEMP_DIR="$(mktemp -d 2>/dev/null || mktemp -d -t flowforge-install)"
    else
      echo -e "❌ Error: mktemp no está disponible en este sistema."
      exit 1
    fi
    trap 'rm -rf "$TEMP_DIR"' EXIT
    git clone --depth 1 https://github.com/efreet111/FlowForge.git "$TEMP_DIR" 2>/dev/null || {
      echo -e "❌ Error: No se pudo clonar el repositorio. Verificá tu conexión a internet."
      exit 1
    }
    FLOWFORGE_REPO="$TEMP_DIR"
    echo -e "✅ Repositorio descargado temporalmente en $TEMP_DIR"
  fi
fi

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

# --- Detect OpenCode ──────────────────────────────────────────────────────
if [ -d "${HOME}/.config/opencode" ]; then
  echo -e "${GREEN}[✓] OpenCode detectado${NC}"
  BACKUP="${BACKUP_DIR}/opencode"
  mkdir -p "$BACKUP"
  [ -f "${HOME}/.config/opencode/opencode.json" ] && cp "${HOME}/.config/opencode/opencode.json" "$BACKUP/" && echo -e "  Backup: ${YELLOW}$BACKUP/opencode.json${NC}"

  cp "$FLOWFORGE_REPO/ide/opencode/opencode.flowforge.json" "${HOME}/.config/opencode/opencode.flowforge.json"
  echo -e "  ${GREEN}✓${NC} opencode.flowforge.json copiado"

  mkdir -p "${HOME}/.config/opencode/commands"
  for cmd in flow-start flow-dev flow-verify flow-close; do
    [ -f "$FLOWFORGE_REPO/ide/opencode/commands/${cmd}.md" ] && cp "$FLOWFORGE_REPO/ide/opencode/commands/${cmd}.md" "${HOME}/.config/opencode/commands/" || true
  done
  echo -e "  ${GREEN}✓${NC} Comandos /flow-* copiados"
  echo -e "  ${YELLOW}⚠${NC} Después de instalar, mergeá opencode.flowforge.json en tu opencode.json"
  echo -e "     O seguí las instrucciones en: https://github.com/efreet111/FlowForge"
  INSTALLED=1
fi

# --- Detect Cursor ─────────────────────────────────────────────────────────
if [ -d "${HOME}/.cursor" ]; then
  echo -e "${GREEN}[✓] Cursor detectado${NC}"
  BACKUP="${BACKUP_DIR}/cursor"
  mkdir -p "$BACKUP"
  mkdir -p "${HOME}/.cursor/rules" "${HOME}/.cursor/agents"

  # Backup
  [ -f "${HOME}/.cursor/rules/workflow.mdc" ] && cp "${HOME}/.cursor/rules/workflow.mdc" "$BACKUP/"
  [ -f "${HOME}/.cursor/rules/model-assignments.mdc" ] && cp "${HOME}/.cursor/rules/model-assignments.mdc" "$BACKUP/"
  [ -f "${HOME}/.cursor/rules/git-sin-push.mdc" ] && cp "${HOME}/.cursor/rules/git-sin-push.mdc" "$BACKUP/"

  # Rules
  cp "$FLOWFORGE_REPO/ide/cursor/rules/workflow.mdc" "${HOME}/.cursor/rules/" 2>/dev/null || echo -e "  ${YELLOW}⚠ workflow.mdc no encontrado${NC}"
  cp "$FLOWFORGE_REPO/ide/cursor/rules/model-assignments.mdc" "${HOME}/.cursor/rules/" 2>/dev/null || true
  cp "$FLOWFORGE_REPO/ide/cursor/rules/git-sin-push.mdc" "${HOME}/.cursor/rules/" 2>/dev/null || true
  echo -e "  ${GREEN}✓${NC} Rules copiadas"

  # Agents
  for agent in forge-discovery forge-arch forge-plan forge-dev forge-verify forge-memory; do
    [ -f "$FLOWFORGE_REPO/ide/cursor/agents/${agent}.md" ] && cp "$FLOWFORGE_REPO/ide/cursor/agents/${agent}.md" "${HOME}/.cursor/agents/" && echo -e "  ${GREEN}✓${NC} Agent ${agent} copiado"
  done
  INSTALLED=1
fi

# --- Detect Antigravity ────────────────────────────────────────────────────
if [ -d "${HOME}/.gemini" ]; then
  echo -e "${GREEN}[✓] Antigravity detectado${NC}"
  echo -e "  ${YELLOW}Nota:${NC} Las reglas se instalan por proyecto."
  echo -e "  Creá la carpeta .agents/ en la raíz de tu proyecto con:"
  echo -e ""
  echo -e "    ${BLUE}mkdir -p .agents/rules .agents/workflows${NC}"
  echo -e "    ${BLUE}cp -r ${FLOWFORGE_REPO}/ide/antigravity/rules/* .agents/rules/${NC}"
  echo -e "    ${BLUE}cp -r ${FLOWFORGE_REPO}/ide/antigravity/workflows/* .agents/workflows/${NC}"
  echo -e "    ${BLUE}cp ${FLOWFORGE_REPO}/ide/antigravity/AGENTS.md .${NC}"
  echo -e "    ${BLUE}cp -r ${FLOWFORGE_REPO}/.agents/skills .agents/skills${NC}  (si querés las skills copiadas)"
  echo ""
  INSTALLED=1
fi

# --- Detect VS Code ───────────────────────────────────────────────────────
if [ -d "${HOME}/.vscode" ] || [ -d "${HOME}/.vscode-server" ]; then
  echo -e "${GREEN}[✓] VS Code detectado${NC}"
  mkdir -p "${HOME}/.vscode/agents"
  [ -f "$FLOWFORGE_REPO/ide/vscode/copilot-instructions.md" ] && cp "$FLOWFORGE_REPO/ide/vscode/copilot-instructions.md" "${HOME}/.vscode/"
  for agent in "$FLOWFORGE_REPO/ide/vscode/agents/"*.agent.md; do
    [ -f "$agent" ] && cp "$agent" "${HOME}/.vscode/agents/" && echo -e "  ${GREEN}✓${NC} Agent $(basename $agent) copiado"
  done
  echo -e "  ${YELLOW}Nota:${NC} Para agentes por proyecto, copiá ide/vscode/agents/ a tu-proyecto/.github/agents/"
  echo -e "  Y asegurate de que tu proyecto tenga git init + al menos 1 commit."
  INSTALLED=1
fi

# --- Summary ─────────────────────────────────────────────────────────────
echo ""
echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
if [ "$INSTALLED" -eq 1 ]; then
  echo -e "${GREEN}✅ Instalación completada${NC}"
  [ -d "$BACKUP_DIR" ] && echo -e "  Backups en: ${YELLOW}$BACKUP_DIR${NC}"
  echo ""
  echo -e "${YELLOW}📋 Próximos pasos:${NC}"
  echo -e "  1. Reiniciá tu IDE"
  echo -e "  2. Seleccioná el agente 'flowforge'"
  echo -e "  3. Escribí: /flow-start CRUD de tareas"
  echo ""
  echo -e "${YELLOW}📖 Documentación completa:${NC}"
  echo -e "  https://github.com/efreet111/FlowForge"
else
  echo -e "${RED}❌ No se detectó ningún IDE compatible${NC}"
  echo -e "  Instalá primero: OpenCode, Cursor, Antigravity, o VS Code"
fi
echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
