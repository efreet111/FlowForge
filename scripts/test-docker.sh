#!/usr/bin/env bash
# FlowForge — Orquestador de tests Docker para PM-* de fix-installer
#
# Uso:
#   bash scripts/test-docker.sh           # build + todos los tests (PM-2..5)
#   bash scripts/test-docker.sh build     # solo build de la imagen
#   bash scripts/test-docker.sh pm1       # PM-1: happy path completo (requiere red)
#   bash scripts/test-docker.sh pm2       # PM-2: timeout con GitHub bloqueado
#   bash scripts/test-docker.sh pm3       # PM-3: FLOWFORGE_YES headless
#   bash scripts/test-docker.sh pm4       # PM-4: flowforge doctor
#   bash scripts/test-docker.sh pm5       # PM-5: banner visible
#   bash scripts/test-docker.sh all       # todos los tests (PM-2..5)
#
# Requisito: Docker daemon corriendo
#   sudo systemctl start docker

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
IMAGE="flowforge-test:local"

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

log()  { echo -e "${BLUE}[docker-test]${NC} $*"; }
ok()   { echo -e "${GREEN}[OK]${NC} $*"; }
warn() { echo -e "${YELLOW}[WARN]${NC} $*"; }
err()  { echo -e "${RED}[ERR]${NC} $*"; }

cd "$REPO_ROOT"

# ── Verificar Docker ─────────────────────────────────────────────────────────
check_docker() {
    if ! docker info > /dev/null 2>&1; then
        err "Docker daemon no está corriendo."
        err "Inicialo con:  sudo systemctl start docker"
        exit 1
    fi
    ok "Docker daemon: activo"
}

# ── Build de imagen ──────────────────────────────────────────────────────────
build_image() {
    log "Construyendo imagen $IMAGE..."
    log "  (primera vez: ~3-5min para descargar SDK + compilar .NET)"
    log "  (sucesivas: ~30s gracias al cache de layers)"
    echo ""
    if ! docker build -f Dockerfile.test -t "$IMAGE" .; then
        err "Build fallido — revisá los errores de compilación arriba"
        exit 1
    fi
    ok "Imagen construida: $IMAGE"
}

# ── Runner base (con --privileged para modificar /etc/hosts en PM-2) ─────────
run() {
    docker run --rm \
        --privileged \
        -u root \
        "$IMAGE" \
        "$@"
}

run_as_testuser() {
    docker run --rm \
        --privileged \
        -u testuser \
        -e HOME=/home/testuser \
        "$IMAGE" \
        "$@"
}

# ── PM-1: Happy path completo ────────────────────────────────────────────────
pm1() {
    echo ""
    echo -e "${BLUE}══ PM-1: Happy Path Install (requiere red) ══${NC}"
    echo ""
    warn "Este test descarga engram-dotnet de GitHub (~10MB). Requiere internet."
    warn "Timeout: 120s para la descarga."
    echo ""
    # Mount the local repo read-only so the installer uses the current working
    # tree templates instead of cloning from GitHub (which would not have the
    # unpushed changes). FLOWFORGE_REPO short-circuits FlowForgeRepoLocator.
    docker run --rm \
        -u testuser \
        -e HOME=/home/testuser \
        -e FLOWFORGE_REPO=/repo-local \
        -e FLOWFORGE_API_TIMEOUT_SECONDS=60 \
        -v "$REPO_ROOT:/repo-local:ro" \
        "$IMAGE" \
        bash /test/run-pm1.sh
}

# ── PM-4: flowforge doctor ───────────────────────────────────────────────────
pm4() {
    echo ""
    echo -e "${BLUE}══ PM-4: flowforge doctor ══${NC}"
    echo ""
    run_as_testuser flowforge doctor || true
    echo ""
    log "Verificar: tabla con checks [✓] y [✗], exit code != 0 si engram no está"
}

# ── PM-5: Banner visible < 5s ────────────────────────────────────────────────
pm5() {
    echo ""
    echo -e "${BLUE}══ PM-5: Banner visible < 5s ══${NC}"
    echo ""
    log "Capturando primeras líneas de 'flowforge install --yes'..."
    # Timeout a 8s para capturar solo el inicio (la descarga puede tardar más)
    run_as_testuser bash -c "timeout 8 flowforge install --yes 2>&1 | head -8 || true"
    echo ""
    log "Verificar: 'FlowForge' y 'Conectando a GitHub' aparecen primero"
}

# ── PM-2: Timeout con GitHub bloqueado ───────────────────────────────────────
pm2() {
    echo ""
    echo -e "${BLUE}══ PM-2: Timeout con GitHub bloqueado ══${NC}"
    echo ""
    log "Bloqueando GitHub + corriendo 'flowforge install --yes'"
    log "Esperado: error claro en < 40s"
    echo ""
    run bash -c "
        echo '127.0.0.1 api.github.com github.com raw.githubusercontent.com' >> /etc/hosts
        START=\$(date +%s)
        timeout 50 flowforge install --yes 2>&1 || true
        END=\$(date +%s)
        echo \"\"
        echo \"Tiempo: \$((END - START))s\"
    "
}

# ── PM-3: FLOWFORGE_YES ──────────────────────────────────────────────────────
pm3() {
    echo ""
    echo -e "${BLUE}══ PM-3: FLOWFORGE_YES headless ══${NC}"
    echo ""
    log "Verificando lógica FLOWFORGE_YES en install.sh (bash trace -x)"
    log "NOTA: Si GitHub es accesible, descargará el binario real (~10MB)"
    echo ""
    docker run --rm \
        --privileged \
        -u root \
        -e FLOWFORGE_YES=1 \
        -e HOME=/root \
        "$IMAGE" \
        bash -x /test/install.sh 2>&1 | head -50 || true
    echo ""
    log "Verificar: '--yes' aparece en el comando final flowforge"
}

# ── Todos los tests en un contenedor ─────────────────────────────────────────
all_tests() {
    echo ""
    echo -e "${BLUE}══ Corriendo todos los PM-* ══${NC}"
    echo ""
    run bash /test/run-tests.sh
}

# ── Main ─────────────────────────────────────────────────────────────────────
echo ""
echo -e "${BLUE}╔═══════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║  FlowForge Docker Tests — fix-installer   ║${NC}"
echo -e "${BLUE}╚═══════════════════════════════════════════╝${NC}"
echo ""

check_docker

TARGET="${1:-all}"

case "$TARGET" in
    build) build_image ;;
    pm1)   build_image && pm1 ;;
    pm2)   build_image && pm2 ;;
    pm3)   build_image && pm3 ;;
    pm4)   build_image && pm4 ;;
    pm5)   build_image && pm5 ;;
    all)   build_image && all_tests ;;
    *)
        echo "Uso: $0 [all|build|pm1|pm2|pm3|pm4|pm5]"
        exit 1
        ;;
esac
