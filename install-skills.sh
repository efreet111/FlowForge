#!/bin/bash
# install-skills.sh - FlowForge Agentic Skills Installer
# Autor: Antigravity + Gentleman Architect
# Licencia: MIT

# Estética visual en terminal
CYAN='\033[0;36m'
GREEN='\033[0;32m'
YELLOW='\033[0;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

echo -e "${CYAN}====================================================${NC}"
echo -e "${CYAN}       FlowForge - Agentic Skills Bootstrapper       ${NC}"
echo -e "${CYAN}====================================================${NC}"
echo ""

# ── Source ──────────────────────────────────────────────
SOURCE_SKILLS_DIR="./skills"

if [ ! -d "$SOURCE_SKILLS_DIR" ]; then
    echo -e "${RED}❌ Error: No se encontró la carpeta './skills' en la raíz.${NC}"
    echo -e "${YELLOW}Por favor, ejecuta este script desde el directorio raíz de FlowForge.${NC}"
    exit 1
fi

echo -e "📂 Detectada carpeta de origen: ${GREEN}$SOURCE_SKILLS_DIR${NC}"

# Count skills (core + specialized SKILL.md files)
SKILL_COUNT=$(find "$SOURCE_SKILLS_DIR" -name "SKILL.md" | wc -l)
CORE_COUNT=$(ls -d "$SOURCE_SKILLS_DIR"/forge-*/SKILL.md 2>/dev/null | wc -l)

# ── Destinations ────────────────────────────────────────
declare -A DESTINATIONS
DESTINATIONS=(
    ["VS Code Copilot"]="$HOME/.copilot/skills"
    ["Proyecto Activo (.agents/skills)"]="./.agents/skills"
    ["OpenCode"]="$HOME/.config/opencode/skills"
    ["Antigravity"]="$HOME/.gemini/config/skills"
)

# ── Step 1: Create destination directories ──────────────
echo -e "\n${CYAN}[1/5] Creando directorios de configuración...${NC}"

for label in "${!DESTINATIONS[@]}"; do
    dir="${DESTINATIONS[$label]}"
    mkdir -p "$dir" 2>/dev/null
    if [ $? -eq 0 ]; then
        echo -e "   - ${GREEN}OK${NC} $label: ${CYAN}$dir${NC}"
    else
        echo -e "   - ${RED}FAILED${NC} $label: no se pudo crear ${CYAN}$dir${NC}"
    fi
done

# ── Step 2: Deliver skills to all destinations ──────────
echo -e "\n${CYAN}[2/5] Instalando ${SKILL_COUNT} Skills (12 core + $((SKILL_COUNT - 12)) specialized) de FlowForge...${NC}"

FAILED=0

for label in "${!DESTINATIONS[@]}"; do
    dir="${DESTINATIONS[$label]}"

    if [ ! -d "$dir" ]; then
        echo -e "   - ${RED}SKIP${NC} $label: directorio no existe."
        FAILED=$((FAILED + 1))
        continue
    fi

    # Local project uses relative symlinks (not copies)
    if [ "$dir" = "./.agents/skills" ]; then
        LINK_OK=0
        for skill_dir in "$SOURCE_SKILLS_DIR"/forge-*/; do
            skill_name=$(basename "$skill_dir")
            # Remove existing entry (symlink, dir, or file)
            rm -rf "$dir/$skill_name" 2>/dev/null
            ln -s "../../skills/$skill_name" "$dir/$skill_name" 2>/dev/null
            if [ $? -ne 0 ]; then
                LINK_OK=1
            fi
        done
        if [ "$LINK_OK" -eq 0 ]; then
            echo -e "   - ${GREEN}OK${NC} $label: symlinks relativos creados."
        else
            echo -e "   - ${RED}FAILED${NC} $label: error al crear symlinks."
            FAILED=$((FAILED + 1))
        fi
    else
        cp -r "$SOURCE_SKILLS_DIR"/* "$dir/" 2>/dev/null
        if [ $? -eq 0 ]; then
            echo -e "   - ${GREEN}OK${NC} $label: ${SKILL_COUNT} skills sincronizadas."
        else
            echo -e "   - ${RED}FAILED${NC} $label: error al copiar skills."
            FAILED=$((FAILED + 1))
        fi
    fi
done

# ── Step 3: FlowDocs upstream skills ────────────────────
echo -e "\n${CYAN}[3/5] Instalando FlowDocs skills (upstream)...${NC}"

FLOWDOC_REPO="https://github.com/crhistianmdz/FlowDocs.git"
FLOWDOC_TMP="/tmp/flowdocs-$(date +%s)"
FLOWDOC_DEST="$HOME/.config/opencode/skills"

# Clone latest (depth=1 for speed)
git clone "$FLOWDOC_REPO" "$FLOWDOC_TMP" --depth=1 2>/dev/null
if [ $? -eq 0 ]; then
    # Copy only the skills directory
    cp -r "$FLOWDOC_TMP/skills/"* "$FLOWDOC_DEST/" 2>/dev/null
    if [ $? -eq 0 ]; then
        FLOWDOC_COUNT=$(find "$FLOWDOC_TMP/skills" -name "SKILL.md" | wc -l)
        echo -e "   - ${GREEN}OK${NC} FlowDocs upstream: ${FLOWDOC_COUNT} skills instaladas."
    else
        echo -e "   - ${YELLOW}WARN${NC} FlowDocs skills copiadas con errores."
    fi
    rm -rf "$FLOWDOC_TMP"
else
    echo -e "   - ${RED}FAILED${NC} FlowDocs: error al clonar (sin internet?). Prosiguiendo..."
    rm -rf "$FLOWDOC_TMP" 2>/dev/null
fi

# ── Step 4: Apply HU-045 autoinvoke config ───────────────────────
echo -e "\n${CYAN}[4/5] HU-045: Configurando subagent-first invocation...${NC}"

# This ensures skills are NOT auto-invoked by OpenCode
# Forcing the orchestrator to use subagents instead
HU045_APPLIED=0
for dest_dir in "${DESTINATIONS[@]}"; do
    if [ -d "$dest_dir" ]; then
        for skill_file in "$dest_dir"/forge-*/SKILL.md "$dest_dir"/forge-*.md; do
            if [ -f "$skill_file" ]; then
                # Check if metadata.opencode/autoinvoke already exists
                if ! grep -q "opencode/autoinvoke: false" "$skill_file"; then
                    # Insert metadata block after the first --- line
                    if grep -q "^---$" "$skill_file"; then
                        sed -i '0,/^---$/a metadata:\n  opencode/autoinvoke: false  # HU-045: Disable auto-invocation to force subagent delegation' "$skill_file" 2>/dev/null
                        HU045_APPLIED=$((HU045_APPLIED + 1))
                    fi
                fi
            fi
        done
    fi
done
if [ "$HU045_APPLIED" -gt 0 ]; then
    echo -e "   - ${GREEN}OK${NC} HU-045: $HU045_APPLIED skills configuradas con autoinvoke: false"
else
    echo -e "   - ${YELLOW}SKIP${NC} HU-045: skills ya tienen la configuración"
fi

# ── Step 5: Permissions & summary ───────────────────────
echo -e "\n${CYAN}[5/5] Configurando permisos...${NC}"
chmod +x ./install-skills.sh

echo -e "\n${GREEN}====================================================${NC}"
echo -e "${GREEN}      ¡INSTALACIÓN COMPLETADA CON ÉXITO! 🎉         ${NC}"
echo -e "${GREEN}====================================================${NC}"
echo -e "Las ${SKILL_COUNT} skills de FlowForge ya están integradas:"
echo ""
echo -e "  ${CYAN}Core Skills (12):${NC}"
echo -e "   - @flowforge            (Semáforo Maestro CKP-0→4)"
echo -e "   - @forge-discovery     (Fase 0: Contexto)"
echo -e "   - @forge-arch          (Fase 1: Intención)"
echo -e "   - @forge-plan          (Fase 2: Arquitectura)"
echo -e "   - @forge-dev           (Fase 3: Ejecución/TDD)"
echo -e "   - @forge-verify        (Fase 3b: Juicio Hostil)"
echo -e "   - @forge-memory        (Fase 4: Cierre/Memory)"
echo -e "   - @forge-teacher       (Modo Socrático)"
echo -e "   - @forge-index-docs     (Indexado de documentación)"
echo -e "   - @forge-onboarding     (Onboarding de proyecto)"
echo -e "   - @forge-memory-contradiction (Detección de contradicciones)"
echo -e "   - @forge-init            (Init/sync de AGENTS.md)"
echo ""
echo -e "  ${CYAN}Specialized Skills ($((SKILL_COUNT - 12))):${NC}"
echo -e "   ${CYAN}Security (5):${NC}"
echo -e "      forge-arch-security, forge-plan-security,"
echo -e "      forge-dev-security, forge-verify-security,"
echo -e "      forge-discovery-security"
echo -e "   ${CYAN}Code Quality (7):${NC}"
echo -e "      forge-dev-solid, forge-dev-testing,"
echo -e "      forge-dev-refactor, forge-dev-performance,"
echo -e "      forge-plan-patterns, forge-verify-complexity,"
echo -e "      forge-verify-performance"
echo -e "   ${CYAN}Infrastructure (7):${NC}"
echo -e "      forge-arch-performance, forge-arch-a11y,"
echo -e "      forge-arch-domain, forge-plan-migrations,"
echo -e "      forge-plan-rollback, forge-discovery-compliance,"
echo -e "      forge-discovery-cost"
echo -e "   ${CYAN}Memory & Metrics (4):${NC}"
echo -e "      forge-memory-changelog, forge-memory-knowledge,"
echo -e "      forge-memory-metrics, forge-verify-a11y"
echo ""
echo -e "  ${CYAN}FlowDocs Skills (upstream, ~/.config/opencode/skills):${NC}"
echo -e "   - Documentación: flowdoc-assist, flowdoc-hu, flowdoc-prd,"
echo -e "                flowdoc-adr, flowdoc-rfc, flowdoc-api,"
echo -e "                flowdoc-db, flowdoc-discover, flowdoc-review"
echo ""

if [ "$FAILED" -gt 0 ]; then
    echo -e "${YELLOW}⚠️  $FAILED destino(s) fallaron. Revisa los errores arriba.${NC}"
else
    echo -e "${GREEN}✅ Todos los destinos sincronizados correctamente.${NC}"
fi

echo ""
echo -e "${CYAN}¡Forja tu flujo de desarrollo con agentes de IA! 🚀${NC}"
echo -e "${CYAN}====================================================${NC}"
