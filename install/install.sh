#!/usr/bin/env bash
# FlowForge Stack Installer — bootstrap script (Linux/macOS)
#
# Downloads the flowforge binary from GitHub Releases and executes it.
# Usage:
#   curl -fsSL https://raw.githubusercontent.com/efreet111/FlowForge/main/install/install.sh | bash
#   curl -fsSL https://raw.githubusercontent.com/efreet111/FlowForge/main/install/install.sh | bash -s -- --channel beta
#   bash install.sh [--channel stable|beta|nightly] [--version v0.1.0] [--diagnose]
set -euo pipefail

REPO="efreet111/FlowForge"
CHANNEL="stable"
VERSION=""
INSTALL_DIR="${HOME}/.local/bin"

YELLOW='\033[1;33m'
NC='\033[0m'

DIAGNOSE_ONLY=false
spinner_pid=""

start_wget_spinner() {
  if [ -n "${spinner_pid:-}" ]; then
    return
  fi
  (
    while true; do
      echo "  Descargando flowforge ${VERSION}..."
      sleep 5
    done
  ) &
  spinner_pid=$!
}

stop_wget_spinner() {
  if [ -n "${spinner_pid:-}" ]; then
    kill "$spinner_pid" >/dev/null 2>&1 || true
    wait "$spinner_pid" >/dev/null 2>&1 || true
    spinner_pid=""
  fi
}

run_diagnose() {
  local failures=0
  echo "FlowForge installer diagnostic report"
  echo ""

  if command -v curl >/dev/null 2>&1; then
    echo "  ✓ curl disponible"
  else
    echo "  ✗ curl no disponible"
    failures=$((failures + 1))
  fi

  if command -v wget >/dev/null 2>&1; then
    echo "  ✓ wget disponible"
  else
    echo "  ✗ wget no disponible"
    failures=$((failures + 1))
  fi

  mkdir -p "$INSTALL_DIR" >/dev/null 2>&1 || true
  local diagnose_file
  diagnose_file="$INSTALL_DIR/.flowforge-diagnose"
  if touch "$diagnose_file" >/dev/null 2>&1; then
    rm -f "$diagnose_file" >/dev/null 2>&1 || true
    echo "  ✓ $INSTALL_DIR escribible"
  else
    echo "  ✗ No se puede escribir en $INSTALL_DIR"
    failures=$((failures + 1))
  fi

  if command -v curl >/dev/null 2>&1; then
    if curl -sSf https://api.github.com >/dev/null 2>&1; then
      echo "  ✓ GitHub reachable"
    else
      echo "  ✗ GitHub no responde (curl)"
      failures=$((failures + 1))
    fi
  elif command -v wget >/dev/null 2>&1; then
    if wget -qO- https://api.github.com >/dev/null 2>&1; then
      echo "  ✓ GitHub reachable"
    else
      echo "  ✗ GitHub no responde (wget)"
      failures=$((failures + 1))
    fi
  else
    echo "  ✗ No hay curl ni wget para verificar GitHub"
    failures=$((failures + 1))
  fi

  echo ""
  if [ "$failures" -ne 0 ]; then
    echo "Diagnóstico completado con errores."
    return 2
  fi

  echo "Diagnóstico completado. Todo listo."
  return 0
}

# ── Args ──────────────────────────────────────────────────────────────────────
while [[ $# -gt 0 ]]; do
  case "$1" in
    --channel)  CHANNEL="$2"; shift 2 ;;
    --version)  VERSION="$2";  shift 2 ;;
    --diagnose) DIAGNOSE_ONLY=true; shift ;;
    *) echo "Argumento desconocido: $1" >&2; exit 1 ;;
  esac
done

# ── Detect platform ──────────────────────────────────────────────────────────
OS="$(uname -s)"
ARCH="$(uname -m)"

case "$OS" in
  Linux)  PLATFORM="linux" ;;
  Darwin) PLATFORM="macos" ;;
  *)
    echo "ERROR: Plataforma no soportada: $OS" >&2
    echo "Descargá el binario manualmente: https://github.com/${REPO}/releases" >&2
    exit 1
    ;;
esac

case "$ARCH" in
  x86_64)  ARCH_SLUG="x64" ;;
  aarch64|arm64) ARCH_SLUG="arm64" ;;
  *)
    echo "WARN: Arquitectura $ARCH no verificada. Intentando con x64." >&2
    ARCH_SLUG="x64"
    ;;
esac

BINARY_NAME="flowforge-${PLATFORM}-${ARCH_SLUG}"
if [[ "$ARCH_SLUG" == "x64" && "$PLATFORM" == "linux" ]]; then
  BINARY_NAME="flowforge-linux-x64"
fi

# ── Detect curl/wget ──────────────────────────────────────────────────────────
if command -v curl >/dev/null 2>&1; then
  FETCH_CMD="curl"
  FETCH="curl -fSL --progress-bar"
elif command -v wget >/dev/null 2>&1; then
  FETCH_CMD="wget"
  FETCH="wget -qO-"
else
  echo "ERROR: Se requiere curl o wget." >&2
  exit 1
fi

if [ "$DIAGNOSE_ONLY" = true ]; then
  run_diagnose
  exit $?
fi

# ── Obtener versión desde GitHub ─────────────────────────────────────────────
if [[ -z "$VERSION" ]]; then
  echo "Buscando última versión (canal: ${CHANNEL})..."

  # Usa /releases (list) en lugar de /releases/latest para soportar pre-releases
  RELEASES_URL="https://api.github.com/repos/${REPO}/releases"
  VERSION=$(${FETCH} "$RELEASES_URL" | grep '"tag_name"' | head -1 | sed 's/.*"tag_name":\s*"\([^"\]*\)".*/\1/')

  if [[ -z "$VERSION" ]]; then
    echo "ERROR: No se pudo obtener la versión desde GitHub." >&2
    echo "Intentá: bash install.sh --version v0.1.0-alpha.1" >&2
    exit 1
  fi
fi

echo "Instalando FlowForge ${VERSION}..."

# ── Descargar binario ─────────────────────────────────────────────────────────
DOWNLOAD_URL="https://github.com/${REPO}/releases/download/${VERSION}/${BINARY_NAME}"
TMP_FILE="$(mktemp)"
trap 'rm -f "$TMP_FILE"; stop_wget_spinner' EXIT

echo "  Descargando: ${DOWNLOAD_URL}"
if [ "$FETCH_CMD" = "wget" ]; then
  start_wget_spinner
fi
${FETCH} "$DOWNLOAD_URL" > "$TMP_FILE"
stop_wget_spinner

# ── Verificar SHA-256 (si está disponible) ───────────────────────────────────
CHECKSUM_URL="${DOWNLOAD_URL}.sha256"
EXPECTED_SHA=$(${FETCH} "$CHECKSUM_URL" 2>/dev/null | awk '{print $1}' || true)
if [[ -n "$EXPECTED_SHA" ]]; then
  ACTUAL_SHA=$(sha256sum "$TMP_FILE" | awk '{print $1}')
  if [[ "$EXPECTED_SHA" != "$ACTUAL_SHA" ]]; then
    echo "ERROR: SHA-256 no coincide. Descarga corrupta." >&2
    echo "  Esperado: $EXPECTED_SHA" >&2
    echo "  Obtenido: $ACTUAL_SHA" >&2
    exit 1
  fi
  echo "  SHA-256 OK"
fi

# ── Instalar ──────────────────────────────────────────────────────────────────
mkdir -p "$INSTALL_DIR"
install -m 755 "$TMP_FILE" "${INSTALL_DIR}/flowforge"
echo "  Instalado: ${INSTALL_DIR}/flowforge"

if [ -n "${SUDO_USER:-}" ]; then
  TARGETS=("${INSTALL_DIR}/flowforge" "${HOME}/.local/bin/engram" "${HOME}/.local/bin/libe_sqlite3.so" "${HOME}/.engram")
  for target in "${TARGETS[@]}"; do
    chown "$SUDO_USER:$SUDO_USER" "$target" 2>/dev/null || true
  done
  echo -e "  ${YELLOW}!${NC} Ajustando permisos para usuario: $SUDO_USER"
fi

# ── Agregar al PATH si no está ────────────────────────────────────────────────
if ! echo "$PATH" | grep -q "$INSTALL_DIR"; then
  echo ""
  echo "IMPORTANTE: Agregá ${INSTALL_DIR} a tu PATH:"
  echo "  echo '\'export PATH=\"$HOME/.local/bin:\$PATH\"' >> ~/.bashrc"
  echo "  source ~/.bashrc"
fi

# ── Lanzar wizard ─────────────────────────────────────────────────────────────
# curl | bash entrega el script por stdin (pipe). Hay que detectar headless ANTES
# de exec </dev/tty: si no, bash puede terminar el script justo después de exec
# y nunca llega a flowforge install (síntoma: se queda en "Instalado: .../flowforge").
INSTALL_HEADLESS=false
if [ -n "${FLOWFORGE_YES:-}" ] || ! [ -t 0 ]; then
  INSTALL_HEADLESS=true
fi

# Reconectar stdin al TTY solo en invocación interactiva directa (bash install.sh).
if [ "$INSTALL_HEADLESS" = false ] && [ -t 1 ] && [ -e /dev/tty ]; then
  exec </dev/tty 2>/dev/null || true
fi

echo ""
echo "Iniciando wizard de instalación..."
if [ "$INSTALL_HEADLESS" = true ]; then
  "${INSTALL_DIR}/flowforge" install --yes
else
  "${INSTALL_DIR}/flowforge" install
fi

if [ -n "${SUDO_USER:-}" ]; then
  echo -e "  ${YELLOW}!${NC} Instalado via sudo. Para el MCP, también ejecutá:"
  echo -e "    sudo chown -R ${SUDO_USER}:${SUDO_USER} ~/.engram ~/.local/bin/engram ~/.local/bin/libe_sqlite3.so ~/.local/bin/flowforge"
fi
