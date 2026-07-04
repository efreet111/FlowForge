#!/usr/bin/env bash
# FlowForge — Reinstalación limpia en máquina local
#
# Uso:
#   bash scripts/reinstall-local.sh           # backup + compile + reinstall
#   bash scripts/reinstall-local.sh --restore # restaurar último backup
#   bash scripts/reinstall-local.sh --verify  # solo verificar instalación actual
#
# Requiere: Docker corriendo  (sudo systemctl start docker)

set -uo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
BACKUP_DIR="$HOME/.flowforge-backup/$(date +%Y%m%d-%H%M%S)"
IMAGE="flowforge-reinstall:local"
NEW_BINARY="/tmp/flowforge-new"

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m'

log()    { echo -e "${BLUE}[reinstall]${NC} $*"; }
ok()     { echo -e "${GREEN}[✓]${NC} $*"; }
warn()   { echo -e "${YELLOW}[!]${NC} $*"; }
err()    { echo -e "${RED}[✗]${NC} $*"; }
header() { echo -e "\n${CYAN}══ $* ══${NC}\n"; }

# ── Modo --verify ─────────────────────────────────────────────────────────────
verify_install() {
    header "Verificación de instalación FlowForge"

    # Binarios
    for bin in engram libe_sqlite3.so flowforge; do
        if [ -f "$HOME/.local/bin/$bin" ]; then
            SIZE=$(du -sh "$HOME/.local/bin/$bin" | cut -f1)
            ok "$bin ($SIZE)"
        else
            err "$bin → NO encontrado en ~/.local/bin/"
        fi
    done

    echo ""
    log "Versiones:"
    "$HOME/.local/bin/engram" --version 2>/dev/null && true
    "$HOME/.local/bin/flowforge" --version 2>/dev/null && true

    echo ""
    log "Agents Cursor:"
    find "$HOME/.cursor/agents" -name "forge-*.md" 2>/dev/null | sort | while read -r f; do
        ok "  $(basename "$f")"
    done || warn "Ningún agent Cursor"

    echo ""
    log "Agents OpenCode:"
    find "$HOME/.config/opencode/agents" -name "*.md" 2>/dev/null | sort | while read -r f; do
        ok "  $(basename "$f")"
    done || warn "Ningún agent OpenCode"

    echo ""
    log "MCP Config (Cursor):"
    if [ -f "$HOME/.cursor/mcp.json" ]; then
        cat "$HOME/.cursor/mcp.json" | python3 -m json.tool 2>/dev/null || cat "$HOME/.cursor/mcp.json"
    else
        warn "~/.cursor/mcp.json no encontrado"
    fi

    echo ""
    log "Sync status:"
    SYNC_URL=$(cat "$HOME/.cursor/mcp.json" 2>/dev/null | python3 -c "import sys,json; d=json.load(sys.stdin); print(d['mcpServers']['engram']['env'].get('ENGRAM_SERVER_URL',''))" 2>/dev/null || echo "")
    SYNC_ENABLED=$(cat "$HOME/.cursor/mcp.json" 2>/dev/null | python3 -c "import sys,json; d=json.load(sys.stdin); print(d['mcpServers']['engram']['env'].get('ENGRAM_SYNC_ENABLED','false'))" 2>/dev/null || echo "false")

    if [ "$SYNC_ENABLED" = "true" ] && [ -n "$SYNC_URL" ]; then
        ok "Modo: offline-first sync → $SYNC_URL"
        HEALTH=$(curl -s --connect-timeout 3 "$SYNC_URL/health" 2>/dev/null || echo "SIN RESPUESTA")
        if echo "$HEALTH" | grep -q '"status":"ok"'; then
            ok "Servidor sync: ONLINE ($HEALTH)"
        else
            warn "Servidor sync: OFFLINE o inaccesible"
            warn "  Respuesta: $HEALTH"
            warn "  engram funcionará en modo local (SQLite) hasta que vuelva a conectar"
        fi
    else
        ok "Modo: local only (SQLite, sin sync)"
    fi

    echo ""
    log "engram DB stats:"
    "$HOME/.local/bin/engram" stats 2>&1 || warn "engram stats falló"

    echo ""
    log "flowforge doctor:"
    "$HOME/.local/bin/flowforge" doctor 2>&1 || true
}

# ── Modo --restore ─────────────────────────────────────────────────────────────
restore_backup() {
    LATEST=$(ls -dt "$HOME/.flowforge-backup"/*/ 2>/dev/null | head -1)
    if [ -z "$LATEST" ]; then
        err "No hay backups disponibles en ~/.flowforge-backup/"
        exit 1
    fi
    warn "Restaurando desde: $LATEST"
    read -rp "¿Confirmar restauración? (s/N): " CONFIRM
    [ "$CONFIRM" != "s" ] && { log "Cancelado."; exit 0; }

    [ -d "$LATEST/bin" ] && cp -f "$LATEST/bin/"* "$HOME/.local/bin/" 2>/dev/null && ok "Binarios restaurados"
    [ -d "$LATEST/cursor-agents" ] && cp -f "$LATEST/cursor-agents/"* "$HOME/.cursor/agents/" 2>/dev/null && ok "Cursor agents restaurados"
    [ -f "$LATEST/mcp.json" ] && cp -f "$LATEST/mcp.json" "$HOME/.cursor/mcp.json" && ok "mcp.json restaurado"
    [ -f "$LATEST/engram-config.json" ] && cp -f "$LATEST/engram-config.json" "$HOME/.engram/config.json" && ok "engram config restaurado"
    ok "Restauración completa desde $LATEST"
}

# ── Backup ────────────────────────────────────────────────────────────────────
make_backup() {
    header "Backup de instalación actual"
    mkdir -p "$BACKUP_DIR/bin" "$BACKUP_DIR/cursor-agents" "$BACKUP_DIR/opencode-agents"

    for f in engram libe_sqlite3.so flowforge; do
        [ -f "$HOME/.local/bin/$f" ] && cp "$HOME/.local/bin/$f" "$BACKUP_DIR/bin/" && ok "Backup: $f"
    done

    [ -d "$HOME/.cursor/agents" ] && cp -r "$HOME/.cursor/agents/." "$BACKUP_DIR/cursor-agents/" && ok "Backup: cursor agents"
    [ -d "$HOME/.config/opencode/agents" ] && cp -r "$HOME/.config/opencode/agents/." "$BACKUP_DIR/opencode-agents/" 2>/dev/null && ok "Backup: opencode agents"
    [ -f "$HOME/.cursor/mcp.json" ] && cp "$HOME/.cursor/mcp.json" "$BACKUP_DIR/" && ok "Backup: mcp.json"
    [ -f "$HOME/.engram/config.json" ] && cp "$HOME/.engram/config.json" "$BACKUP_DIR/engram-config.json" && ok "Backup: engram config"

    # IMPORTANTE: NO borramos engram.db — la base de datos de memorias
    warn "engram.db NO se borra (tus memorias están seguras en ~/.engram/engram.db)"
    log "Backup guardado en: $BACKUP_DIR"
    log "Restaurar con: bash scripts/reinstall-local.sh --restore"
}

# ── Compilar nuevo binario vía Docker ─────────────────────────────────────────
compile_binary() {
    header "Compilando nuevo flowforge vía Docker"

    if ! docker info > /dev/null 2>&1; then
        err "Docker no está corriendo."
        err "Inicialo con:  sudo systemctl start docker"
        exit 1
    fi

    log "Building imagen de compilación..."
    cd "$REPO_ROOT"
    docker build -f Dockerfile.test -t "$IMAGE" . 2>&1
    if [ $? -ne 0 ]; then
        err "Build fallido — revisá los errores arriba"
        exit 1
    fi

    log "Extrayendo binario compilado..."
    docker create --name flowforge-extract "$IMAGE" > /dev/null
    docker cp flowforge-extract:/usr/local/bin/flowforge "$NEW_BINARY"
    docker rm flowforge-extract > /dev/null
    chmod +x "$NEW_BINARY"

    EXTRACTED_SIZE=$(du -sh "$NEW_BINARY" | cut -f1)
    ok "Binario extraído: $NEW_BINARY ($EXTRACTED_SIZE)"
}

# ── Limpiar instalación actual ────────────────────────────────────────────────
clean_install() {
    header "Limpiando instalación actual"

    # Binarios
    for f in engram libe_sqlite3.so flowforge; do
        rm -f "$HOME/.local/bin/$f" && warn "Removido: ~/.local/bin/$f"
    done

    # Agents (solo los de FlowForge, no otros)
    find "$HOME/.cursor/agents" -name "forge-*.md" -delete 2>/dev/null && warn "Removidos: cursor agents forge-*"
    find "$HOME/.cursor/rules" -name "*.mdc" -delete 2>/dev/null && warn "Removidas: cursor rules"
    find "$HOME/.cursor/commands" -name "flow*.md" -delete 2>/dev/null && warn "Removidos: cursor commands"
    find "$HOME/.config/opencode/agents" -name "*.md" -delete 2>/dev/null && warn "Removidos: opencode agents"

    # NO borrar: ~/.engram/engram.db, ~/.cursor/mcp.json (los reconfigura el install)
    ok "Limpieza completada — engram.db y mcp.json preservados para reinstalación"
}

# ── Reinstalar con nuevo binario ──────────────────────────────────────────────
run_install() {
    header "Reinstalando con nuevo binario"

    log "Copiando nuevo flowforge a ~/.local/bin/..."
    cp "$NEW_BINARY" "$HOME/.local/bin/flowforge"
    chmod +x "$HOME/.local/bin/flowforge"
    ok "Nuevo flowforge en ~/.local/bin/flowforge"

    echo ""
    log "Corriendo: flowforge install --yes"
    log "(Descargará engram-dotnet de GitHub si no está — puede tardar 30s)"
    echo ""

    "$HOME/.local/bin/flowforge" install --yes
    INSTALL_EXIT=$?

    echo ""
    if [ "$INSTALL_EXIT" -eq 0 ]; then
        ok "Instalación completada con exit 0"
    else
        warn "Instalación terminó con exit $INSTALL_EXIT (puede ser parcial)"
    fi
}

# ── Main ──────────────────────────────────────────────────────────────────────
echo ""
echo -e "${CYAN}╔═══════════════════════════════════════════════╗${NC}"
echo -e "${CYAN}║  FlowForge — Reinstalación Local              ║${NC}"
echo -e "${CYAN}╚═══════════════════════════════════════════════╝${NC}"
echo ""

MODE="${1:---full}"

case "$MODE" in
    --verify)
        verify_install
        ;;
    --restore)
        restore_backup
        ;;
    --full|"")
        make_backup
        compile_binary
        clean_install

        echo ""
        warn "Punto de no retorno: la instalación anterior ya fue removida."
        warn "Para restaurar el backup: bash scripts/reinstall-local.sh --restore"
        echo ""
        read -rp "¿Continuar con la reinstalación? (s/N): " CONFIRM
        if [ "$CONFIRM" != "s" ]; then
            log "Cancelado. Tu backup está en: $BACKUP_DIR"
            exit 0
        fi

        run_install

        echo ""
        header "Verificación post-instalación"
        verify_install
        ;;
    *)
        echo "Uso: $0 [--full|--verify|--restore]"
        exit 1
        ;;
esac
