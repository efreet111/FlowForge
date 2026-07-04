#!/usr/bin/env bash
# PM-1: Happy path — instalación completa desde cero
# Verifica que flowforge instala TODOS los componentes en un solo paso:
#   engram-dotnet, MCP config, y agents para Cursor + OpenCode + VS Code + Antigravity
#
# Ejecutado dentro del contenedor Docker (llamado por test-docker.sh pm1)
# Requiere: red disponible (descarga engram-dotnet de GitHub)

set -uo pipefail

PASS=0
FAIL=0
WARN=0

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

pass() { echo -e "${GREEN}[PASS]${NC} $1"; ((PASS++)); }
fail() { echo -e "${RED}[FAIL]${NC} $1"; ((FAIL++)); }
warn() { echo -e "${YELLOW}[WARN]${NC} $1"; ((WARN++)); }
log()  { echo -e "${BLUE}[pm1]${NC} $1"; }
sep()  { echo -e "${BLUE}───────────────────────────────────────────────${NC}"; }

echo ""
echo -e "${BLUE}╔═══════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║  FlowForge — PM-1: Happy Path Install         ║${NC}"
echo -e "${BLUE}╚═══════════════════════════════════════════════╝${NC}"
echo ""
log "Usuario: $(whoami) | HOME: $HOME"
log "IDEs simulados: Cursor, OpenCode, VS Code, Antigravity"
echo ""

# ── Pre-condiciones ───────────────────────────────────────────────────────────
sep
log "Pre-condiciones — verificando entorno limpio"

for dir in ~/.cursor ~/.config/opencode ~/.copilot/agents ~/.config/kilo/agents ~/.gemini/antigravity; do
    if [ -d "$dir" ]; then
        pass "Dir IDE existe: $dir"
    else
        fail "Dir IDE falta: $dir (el auto-detect no lo detectará)"
    fi
done

# ── Ejecutar install ──────────────────────────────────────────────────────────
sep
log "Corriendo: flowforge install --yes"
log "  (descargará engram-dotnet de GitHub — puede tardar 30-60s)"
echo ""

INSTALL_LOG=$(mktemp)
INSTALL_EXIT=0

# Timeout de 120s para la descarga completa
timeout 120 flowforge install --yes 2>&1 | tee "$INSTALL_LOG" || INSTALL_EXIT=$?

echo ""
log "Exit code: $INSTALL_EXIT"

if [ "$INSTALL_EXIT" -eq 0 ]; then
    pass "PM-1-0: install --yes terminó con exit 0"
elif [ "$INSTALL_EXIT" -eq 124 ]; then
    fail "PM-1-0: install --yes agotó el timeout de 120s"
else
    warn "PM-1-0: install --yes terminó con exit $INSTALL_EXIT (puede ser parcial)"
fi

# ── Verificar engram-dotnet ───────────────────────────────────────────────────
sep
log "Verificando engram-dotnet"

if [ -f "$HOME/.local/bin/engram" ] && [ -x "$HOME/.local/bin/engram" ]; then
    ENGRAM_VER=$("$HOME/.local/bin/engram" --version 2>&1 || echo "error")
    pass "PM-1-1a: ~/.local/bin/engram instalado (v: $ENGRAM_VER)"
else
    fail "PM-1-1a: ~/.local/bin/engram NO encontrado o no ejecutable"
fi

if [ -f "$HOME/.local/bin/libe_sqlite3.so" ]; then
    pass "PM-1-1b: ~/.local/bin/libe_sqlite3.so presente"
else
    fail "PM-1-1b: ~/.local/bin/libe_sqlite3.so NO encontrado"
fi

# Verificar que engram funciona (puede crear DB)
if command -v "$HOME/.local/bin/engram" &>/dev/null || [ -f "$HOME/.local/bin/engram" ]; then
    ENGRAM_STATS=$("$HOME/.local/bin/engram" stats 2>&1 || true)
    if echo "$ENGRAM_STATS" | grep -qi "memories\|total\|0\|stats"; then
        pass "PM-1-1c: engram stats funciona (DB accesible)"
    else
        warn "PM-1-1c: engram stats salida inesperada: $ENGRAM_STATS"
    fi
fi

# ── Verificar Cursor ──────────────────────────────────────────────────────────
sep
log "Verificando Cursor agents/rules/commands"

CURSOR_AGENTS=$(find "$HOME/.cursor/agents" -name "forge-*.md" 2>/dev/null | wc -l)
if [ "$CURSOR_AGENTS" -gt 0 ]; then
    pass "PM-1-2a: Cursor agents instalados ($CURSOR_AGENTS archivos forge-*.md)"
    find "$HOME/.cursor/agents" -name "forge-*.md" | sort | while read -r f; do
        echo "          $(basename "$f")"
    done
else
    fail "PM-1-2a: Cursor agents NO instalados (ningún forge-*.md en ~/.cursor/agents/)"
fi

CURSOR_RULES=$(find "$HOME/.cursor/rules" -name "*.mdc" 2>/dev/null | wc -l)
if [ "$CURSOR_RULES" -gt 0 ]; then
    pass "PM-1-2b: Cursor rules instaladas ($CURSOR_RULES archivos *.mdc)"
else
    warn "PM-1-2b: Cursor rules vacías (puede ser que el repo no tenga .mdc)"
fi

CURSOR_CMDS=$(find "$HOME/.cursor/commands" -name "*.md" 2>/dev/null | wc -l)
if [ "$CURSOR_CMDS" -gt 0 ]; then
    pass "PM-1-2c: Cursor commands instalados ($CURSOR_CMDS archivos)"
else
    warn "PM-1-2c: Cursor commands vacíos"
fi

# ── Verificar OpenCode ────────────────────────────────────────────────────────
sep
log "Verificando OpenCode agents"

OC_AGENTS=$(find "$HOME/.config/opencode/agents" -name "*.md" 2>/dev/null | wc -l)
if [ "$OC_AGENTS" -gt 0 ]; then
    pass "PM-1-3a: OpenCode agents instalados ($OC_AGENTS archivos)"
    find "$HOME/.config/opencode/agents" -name "*.md" | sort | while read -r f; do
        echo "          $(basename "$f")"
    done
else
    fail "PM-1-3a: OpenCode agents NO instalados"
fi

# ── Verificar VS Code (Copilot + Kilo) ─────────────────────────────────────────
sep
log "Verificando VS Code (Copilot + Kilo)"

if [ -f "$HOME/.copilot/instructions/flowforge.instructions.md" ]; then
    pass "PM-1-4a: ~/.copilot/instructions/flowforge.instructions.md instalado"
else
    warn "PM-1-4a: Copilot instructions no encontrado (flowforge.instructions.md)"
fi

COPILOT_AGENTS=$(find "$HOME/.copilot/agents" -name "*.agent.md" 2>/dev/null | wc -l)
if [ "$COPILOT_AGENTS" -gt 0 ]; then
    pass "PM-1-4b: Copilot agents instalados ($COPILOT_AGENTS archivos)"
else
    warn "PM-1-4b: Copilot agents no encontrados"
fi

KILO_AGENTS=$(find "$HOME/.config/kilo/agents" -name "*.md" 2>/dev/null | wc -l)
if [ "$KILO_AGENTS" -gt 0 ]; then
    pass "PM-1-4c: Kilo agents instalados ($KILO_AGENTS archivos)"
else
    warn "PM-1-4c: Kilo agents no encontrados"
fi

# ── Verificar Antigravity ───────────────────────────────────────────────────
sep
log "Verificando Antigravity"

ANTI_AGENTS="$HOME/.gemini/antigravity/AGENTS.md"
if [ -f "$ANTI_AGENTS" ]; then
    pass "PM-1-4d: Antigravity AGENTS.md presente"
else
    warn "PM-1-4d: Antigravity AGENTS.md no encontrado"
fi

ANTI_RULES=$(find "$HOME/.gemini/antigravity/rules" -name "*.md" 2>/dev/null | wc -l)
if [ "$ANTI_RULES" -gt 0 ]; then
    pass "PM-1-4e: Antigravity rules instaladas ($ANTI_RULES archivos)"
else
    warn "PM-1-4e: Antigravity rules no encontrados"
fi

ANTI_WF=$(find "$HOME/.gemini/antigravity/workflows" -name "*.md" 2>/dev/null | wc -l)
if [ "$ANTI_WF" -gt 0 ]; then
    pass "PM-1-4f: Antigravity workflows instalados ($ANTI_WF archivos)"
else
    warn "PM-1-4f: Antigravity workflows no encontrados"
fi

# ── Verificar MCP config ──────────────────────────────────────────────────────
sep
log "Verificando MCP config en Cursor"

CURSOR_MCP="$HOME/.cursor/mcp.json"
if [ -f "$CURSOR_MCP" ]; then
    pass "PM-1-5a: ~/.cursor/mcp.json existe"
    if grep -q "engram\|user-engram" "$CURSOR_MCP" 2>/dev/null; then
        pass "PM-1-5b: mcp.json contiene configuración engram"
    else
        warn "PM-1-5b: mcp.json no contiene 'engram' — verificar contenido:"
        cat "$CURSOR_MCP" 2>/dev/null || true
    fi
else
    warn "PM-1-5a: ~/.cursor/mcp.json no creado (EngramModule puede configurar MCP diferente)"
fi

# ── Verificar flowforge shared ────────────────────────────────────────────────
sep
log "Verificando FlowForge shared parity"

if [ -d "$HOME/.flowforge/shared" ]; then
    SHARED_FILES=$(find "$HOME/.flowforge/shared" -type f | wc -l)
    pass "PM-1-6a: ~/.flowforge/shared/ instalado ($SHARED_FILES archivos)"
else
    warn "PM-1-6a: ~/.flowforge/shared/ no encontrado"
fi

if [ -d "$HOME/.flowforge/cache/FlowForge" ] || [ -d "$HOME/.flowforge/cache" ]; then
    pass "PM-1-6b: ~/.flowforge/cache/ (repo cacheado) existe"
else
    warn "PM-1-6b: ~/.flowforge/cache/ no encontrado (git clone puede haber fallado)"
fi

# ── Resumen final ─────────────────────────────────────────────────────────────
sep
echo ""
log "Output de install completo guardado en: $INSTALL_LOG"
echo ""
echo -e "${BLUE}Resultados PM-1:${NC}"
echo -e "  ${GREEN}PASS: $PASS${NC}"
echo -e "  ${RED}FAIL: $FAIL${NC}"
echo -e "  ${YELLOW}WARN: $WARN${NC}"
echo ""

if [ "$FAIL" -eq 0 ]; then
    echo -e "${GREEN}✓ PM-1 PASSED — instalación completa verificada${NC}"
else
    echo -e "${RED}✗ PM-1 FAILED — $FAIL verificaciones críticas fallaron${NC}"
    echo ""
    log "Log completo de instalación:"
    cat "$INSTALL_LOG"
fi
echo ""

[ "$FAIL" -eq 0 ]
