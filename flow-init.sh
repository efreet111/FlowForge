#!/usr/bin/env bash
# FlowForge — flow-init (Unix/macOS)
# Scaffolds a new project with FlowDoc docs structure + FlowForge IDE packs.
#
# Usage:
#   bash flow-init.sh /path/to/new-project [ProjectName]
#   bash flow-init.sh /path/to/new-project "My App" --force
#
# What it creates:
#   AGENTS.md, .flowforge.json, docs/ (PRD, DEVELOPMENT, tasks/, architecture/,
#   templates/), .ai-work/, QUICKSTART.project.md
#   Then runs ide/install.sh -ProjectPath to install IDE packs.
#
# Related:
#   ADR-004: docs/decisions/ADR-004-flowdoc-integration.md
#   ADR-002: docs/decisions/ADR-002-scaffold-doc-policy.md

set -euo pipefail

# ── Args ──────────────────────────────────────────────────────────────────────
PROJECT_PATH="${1:-}"
PROJECT_NAME="${2:-}"
FORCE=0
for arg in "$@"; do [[ "$arg" == "--force" ]] && FORCE=1; done

if [ -z "$PROJECT_PATH" ]; then
  echo "Usage: bash flow-init.sh <project-path> [ProjectName] [--force]"
  exit 1
fi

# ── Locate FlowForge repo ─────────────────────────────────────────────────────
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd 2>/dev/null || echo "")"
if [ -f "$SCRIPT_DIR/AGENTS.md" ] && grep -q "FlowForge" "$SCRIPT_DIR/AGENTS.md" 2>/dev/null; then
  FLOWFORGE_REPO="$SCRIPT_DIR"
else
  echo "ERROR: Could not locate FlowForge repo. Run this script from the FlowForge directory."
  exit 1
fi

TEMPLATES="$FLOWFORGE_REPO/templates/project"
FLOWFORGE_VERSION="$(cat "$FLOWFORGE_REPO/VERSION.md" 2>/dev/null | tr -d '[:space:]' || echo '0.4.1')"
TODAY="$(date +%Y-%m-%d)"

# ── Colors ────────────────────────────────────────────────────────────────────
RED='\033[0;31m'; GREEN='\033[0;32m'; YELLOW='\033[1;33m'; BLUE='\033[0;34m'; NC='\033[0m'

echo -e "${BLUE}========================================"
echo -e "  FlowForge — flow-init"
echo -e "========================================${NC}"
echo ""

# ── Validate target directory ─────────────────────────────────────────────────
if [ -d "$PROJECT_PATH" ]; then
  # Check if non-empty (ignore .git)
  NON_GIT="$(ls -A "$PROJECT_PATH" 2>/dev/null | grep -v '^\.git$' | head -1 || true)"
  if [ -n "$NON_GIT" ] && [ "$FORCE" -eq 0 ]; then
    echo -e "${RED}ERROR: Directory is not empty: $PROJECT_PATH${NC}"
    echo -e "Use --force to scaffold into an existing directory."
    exit 1
  fi
else
  mkdir -p "$PROJECT_PATH"
  echo -e "${GREEN}[OK]${NC} Created directory: $PROJECT_PATH"
fi

TARGET="$(cd "$PROJECT_PATH" && pwd)"

# ── Derive project name ───────────────────────────────────────────────────────
if [ -z "$PROJECT_NAME" ]; then
  PROJECT_NAME="$(basename "$TARGET")"
fi
echo -e "${GREEN}[OK]${NC} Project name: $PROJECT_NAME"
echo -e "${GREEN}[OK]${NC} Target: $TARGET"
echo ""

# ── Helper: replace placeholders in a file ────────────────────────────────────
replace_placeholders() {
  local file="$1"
  if [ ! -f "$file" ]; then return; fi
  # Use Python if available for reliable UTF-8 in-place substitution
  if command -v python3 >/dev/null 2>&1 || command -v python >/dev/null 2>&1; then
    local py=python3
    command -v python3 >/dev/null 2>&1 || py=python
    PROJECT_NAME="$PROJECT_NAME" FLOWFORGE_VERSION="$FLOWFORGE_VERSION" TODAY="$TODAY" \
    FILE="$file" $py -c "
import os
from pathlib import Path
f = Path(os.environ['FILE'])
text = f.read_text(encoding='utf-8')
text = text.replace('__PROJECT_NAME__', os.environ['PROJECT_NAME'])
text = text.replace('__FLOWFORGE_VERSION__', os.environ['FLOWFORGE_VERSION'])
text = text.replace('__DATE__', os.environ['TODAY'])
text = text.replace('__PROJECT_PATH__', os.environ.get('TARGET', ''))
# Stack placeholders — leave as-is for user to fill
f.write_text(text, encoding='utf-8')
" TARGET="$TARGET"
  else
    # Fallback: sed (may have issues with special chars)
    sed -i "s/__PROJECT_NAME__/$PROJECT_NAME/g; s/__FLOWFORGE_VERSION__/$FLOWFORGE_VERSION/g; s/__DATE__/$TODAY/g" "$file"
  fi
}

# ── Copy templates ────────────────────────────────────────────────────────────
echo -e "${BLUE}[*] Copying templates...${NC}"

# AGENTS.md from template
cp "$TEMPLATES/AGENTS.md.template" "$TARGET/AGENTS.md"
replace_placeholders "$TARGET/AGENTS.md"
echo -e "  ${GREEN}OK${NC} AGENTS.md"

# .flowforge.json from template
cp "$TEMPLATES/.flowforge.json.template" "$TARGET/.flowforge.json"
replace_placeholders "$TARGET/.flowforge.json"
echo -e "  ${GREEN}OK${NC} .flowforge.json"

# QUICKSTART.project.md
cp "$TEMPLATES/QUICKSTART.project.md" "$TARGET/QUICKSTART.project.md"
replace_placeholders "$TARGET/QUICKSTART.project.md"
echo -e "  ${GREEN}OK${NC} QUICKSTART.project.md"

# docs/ structure
mkdir -p \
  "$TARGET/docs/architecture/adr" \
  "$TARGET/docs/architecture/rfc" \
  "$TARGET/docs/tasks" \
  "$TARGET/docs/templates"

for f in PRD.md DEVELOPMENT.md; do
  cp "$TEMPLATES/docs/$f" "$TARGET/docs/$f"
  replace_placeholders "$TARGET/docs/$f"
done
echo -e "  ${GREEN}OK${NC} docs/PRD.md + docs/DEVELOPMENT.md"

cp "$TEMPLATES/docs/tasks/HU-001-example.md" "$TARGET/docs/tasks/"
replace_placeholders "$TARGET/docs/tasks/HU-001-example.md"
echo -e "  ${GREEN}OK${NC} docs/tasks/HU-001-example.md"

for tmpl in HU-template.md adr-template.md rfc-template.md; do
  cp "$TEMPLATES/docs/templates/$tmpl" "$TARGET/docs/templates/"
done
echo -e "  ${GREEN}OK${NC} docs/templates/ (HU, ADR, RFC templates)"

# .gitkeep files for empty tracked folders
touch "$TARGET/docs/architecture/adr/.gitkeep"
touch "$TARGET/docs/architecture/rfc/.gitkeep"
echo -e "  ${GREEN}OK${NC} docs/architecture/ (adr/ + rfc/)"

# .ai-work/
mkdir -p "$TARGET/.ai-work"
touch "$TARGET/.ai-work/.gitkeep"
echo -e "  ${GREEN}OK${NC} .ai-work/"

echo ""

# ── Install IDE packs (non-fatal) ─────────────────────────────────────────────
echo -e "${BLUE}[*] Installing FlowForge IDE packs...${NC}"
INSTALL_SCRIPT="$FLOWFORGE_REPO/ide/install.sh"
if [ -f "$INSTALL_SCRIPT" ]; then
  bash "$INSTALL_SCRIPT" "$TARGET" || echo -e "  ${YELLOW}! IDE installer finished with warnings (see above). Templates are intact.${NC}"
else
  echo -e "  ${YELLOW}! ide/install.sh not found — run it manually:${NC}"
  echo -e "  ${YELLOW}  bash $FLOWFORGE_REPO/ide/install.sh $TARGET${NC}"
fi

echo ""
echo -e "${BLUE}========================================"
echo -e "${GREEN}flow-init complete!${NC}"
echo ""
echo -e "${YELLOW}Next steps:${NC}"
echo "  1. Edit docs/PRD.md        — describe your product"
echo "  2. Edit AGENTS.md          — fill in your stack"
echo "  3. Edit .flowforge.json    — set engram project name if needed"
echo "  4. Create your first HU:"
echo "       cp docs/templates/HU-template.md docs/tasks/HU-001-your-feature.md"
echo "  5. Reload your IDE, then run:"
echo "       /flow-start HU-001-your-feature"
echo ""
echo "  See QUICKSTART.project.md for the full guide."
echo -e "========================================"
echo -e "  Project: ${GREEN}$PROJECT_NAME${NC}"
echo -e "  Path:    ${GREEN}$TARGET${NC}"
echo -e "  FlowDoc: ${GREEN}flowdoc@1.1${NC} (embedded)"
echo -e "${BLUE}========================================${NC}"
