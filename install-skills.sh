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
    ["Antigravity"]="$HOME/.gemini/antigravity/skills"
)

# ── Step 1: Create destination directories ──────────────
echo -e "\n${CYAN}[1/3] Creando directorios de configuración...${NC}"

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
echo -e "\n${CYAN}[2/3] Instalando ${SKILL_COUNT} Skills (${CORE_COUNT} core + $((SKILL_COUNT - CORE_COUNT)) specialized) de FlowForge...${NC}"

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

# ── Step 3: Permissions & summary ───────────────────────
echo -e "\n${CYAN}[3/3] Configurando permisos...${NC}"
chmod +x ./install-skills.sh

echo -e "\n${GREEN}====================================================${NC}"
echo -e "${GREEN}      ¡INSTALACIÓN COMPLETADA CON ÉXITO! 🎉         ${NC}"
echo -e "${GREEN}====================================================${NC}"
echo -e "Las ${SKILL_COUNT} skills de FlowForge ya están integradas:"
echo ""
echo -e "  ${CYAN}Core Skills (8):${NC}"
echo -e "   - @forge-orchestrator  (Semáforo Maestro CKP-0→4)"
echo -e "   - @forge-discovery     (Fase 0: Contexto)"
echo -e "   - @forge-arch          (Fase 1: Intención)"
echo -e "   - @forge-plan          (Fase 2: Arquitectura)"
echo -e "   - @forge-dev           (Fase 3: Ejecución/TDD)"
echo -e "   - @forge-verify        (Fase 3b: Juicio Hostil)"
echo -e "   - @forge-memory        (Fase 4: Cierre/Memory)"
echo -e "   - @forge-teacher       (Modo Socrático)"
echo ""
echo -e "  ${CYAN}Specialized Skills ($((SKILL_COUNT - CORE_COUNT))):${NC}"
echo -e "   - Security:  forge-arch-security, forge-plan-security,"
echo -e "                forge-dev-security, forge-verify-security,"
echo -e "                forge-discovery-security"
echo -e "   - SOLID:     forge-dev-solid"
echo -e "   - Testing:   forge-dev-testing"
echo -e "   - Refactor:  forge-dev-refactor"
echo -e "   - Perf:      forge-arch-performance, forge-dev-performance,"
echo -e "                forge-verify-performance"
echo -e "   - Patterns:  forge-plan-patterns"
echo -e "   - Migrations:forge-plan-migrations"
echo -e "   - Rollback:  forge-plan-rollback"
echo -e "   - A11y:      forge-arch-a11y, forge-verify-a11y"
echo -e "   - Domain:    forge-arch-domain"
echo -e "   - Complexity:forge-verify-complexity"
echo -e "   - Compliance:forge-discovery-compliance"
echo -e "   - Cost:      forge-discovery-cost"
echo -e "   - Changelog: forge-memory-changelog"
echo -e "   - Knowledge: forge-memory-knowledge"
echo -e "   - Metrics:   forge-memory-metrics"
echo ""

if [ "$FAILED" -gt 0 ]; then
    echo -e "${YELLOW}⚠️  $FAILED destino(s) fallaron. Revisa los errores arriba.${NC}"
else
    echo -e "${GREEN}✅ Todos los destinos sincronizados correctamente.${NC}"
fi

echo ""
echo -e "${CYAN}¡Forja tu flujo de desarrollo con agentes de IA! 🚀${NC}"
echo -e "${CYAN}====================================================${NC}"
