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

# Buscar repositorios de skills de copilotos e IDEs
COPILOT_SKILLS_DIR="$HOME/.copilot/skills"
LOCAL_AGENT_SKILLS_DIR="./.agent/skills"

# Determinar origen de las skills
SOURCE_SKILLS_DIR="./skills"

if [ ! -d "$SOURCE_SKILLS_DIR" ]; then
    echo -e "${RED}❌ Error: No se encontró la carpeta './skills' en la raíz.${NC}"
    echo -e "${YELLOW}Por favor, ejecuta este script desde el directorio raíz de FlowForge.${NC}"
    exit 1
fi

echo -e "📂 Detectada carpeta de origen: ${GREEN}$SOURCE_SKILLS_DIR${NC}"

# 1. Crear directorios de destino
echo -e "\n${CYAN}[1/3] Creando directorios de configuración...${NC}"
mkdir -p "$COPILOT_SKILLS_DIR"
mkdir -p "$LOCAL_AGENT_SKILLS_DIR"
echo -e "   - VS Code Copilot Global: ${GREEN}$COPILOT_SKILLS_DIR${NC}"
echo -e "   - Proyecto Activo Local:  ${GREEN}$LOCAL_AGENT_SKILLS_DIR${NC}"

# 2. Sincronizar las skills core de la metodología
echo -e "\n${CYAN}[2/3] Copiando las 7 Skills de EngramFlow...${NC}"

# Copiar a VS Code Copilot
cp -r "$SOURCE_SKILLS_DIR"/* "$COPILOT_SKILLS_DIR/" 2>/dev/null
if [ $? -eq 0 ]; then
    echo -e "   - ${GREEN}OK${NC} Sincronizadas en Copilot Global."
else
    echo -e "   - ${RED}FAILED${NC} Error al copiar a Copilot Global."
fi

# Copiar a local .agent
cp -r "$SOURCE_SKILLS_DIR"/* "$LOCAL_AGENT_SKILLS_DIR/" 2>/dev/null
if [ $? -eq 0 ]; then
    echo -e "   - ${GREEN}OK${NC} Sincronizadas en .agent/skills/ del proyecto."
else
    echo -e "   - ${RED}FAILED${NC} Error al copiar a local .agent."
fi

# 3. Finalizar y dar instrucciones
echo -e "\n${CYAN}[3/3] Configurando permisos...${NC}"
chmod +x ./install-skills.sh

echo -e "\n${GREEN}====================================================${NC}"
echo -e "${GREEN}      ¡INSTALACIÓN COMPLETADA CON ÉXITO! 🎉         ${NC}"
echo -e "${GREEN}====================================================${NC}"
echo -e "Las skills de EngramFlow ya están integradas:"
echo -e "   - @forge-orchestrator (Semáforo Maestro)"
echo -e "   - @forge-discovery    (Fase 0: Contexto)"
echo -e "   - @forge-arch         (Fase 1: Intención)"
echo -e "   - @forge-plan         (Fase 2: Arquitectura)"
echo -e "   - @forge-dev          (Fase 3: Ejecución/TDD)"
echo -e "   - @forge-verify       (Fase 3.5: Juicio Hostil)"
echo -e "   - @forge-memory       (Fase 4: Cierre/Memory)"
echo ""
echo -e "${YELLOW}👉 PRÓXIMO PASO:${NC} Copia el archivo maestro de reglas a tu proyecto:"
echo -e "   - Para Cursor: Copia el contenido de ${CYAN}docs/09-open-source-integration.md${NC} en un archivo ${GREEN}.cursorrules${NC}."
echo -e "   - Para Cline:  Copia el contenido en un archivo ${GREEN}.clinerules${NC}."
echo ""
echo -e "${CYAN}¡Forja tu flujo de desarrollo con agentes de IA! 🚀${NC}"
echo -e "${CYAN}====================================================${NC}"
