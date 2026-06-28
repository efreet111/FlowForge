#!/usr/bin/env bash
# FlowForge Stack Installer — bootstrap script (Linux/macOS)
#
# Descarga el binario flowforge desde GitHub Releases y lo ejecuta.
# Uso:
#   curl -fsSL https://raw.githubusercontent.com/efreet111/FlowForge/main/install/install.sh | bash
#   curl -fsSL https://raw.githubusercontent.com/efreet111/FlowForge/main/install/install.sh | bash -s -- --channel beta
#   bash install.sh [--channel stable|beta|nightly] [--version v0.1.0]
#
set -euo pipefail

REPO="efreet111/FlowForge"
CHANNEL="stable"
VERSION=""
INSTALL_DIR="${HOME}/.local/bin"

# ── Args ──────────────────────────────────────────────────────────────────────
while [[ $# -gt 0 ]]; do
  case "$1" in
    --channel)  CHANNEL="$2"; shift 2 ;;
    --version)  VERSION="$2";  shift 2 ;;
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
  FETCH="curl -fsSL"
elif command -v wget >/dev/null 2>&1; then
  FETCH="wget -qO-"
else
  echo "ERROR: Se requiere curl o wget." >&2
  exit 1
fi

# ── Obtener versión desde GitHub ─────────────────────────────────────────────
if [[ -z "$VERSION" ]]; then
  echo "Buscando última versión (canal: ${CHANNEL})..."

  # Usa /releases (list) en lugar de /releases/latest para soportar pre-releases
  RELEASES_URL="https://api.github.com/repos/${REPO}/releases"
  VERSION=$(${FETCH} "$RELEASES_URL" | grep '"tag_name"' | head -1 | sed 's/.*"tag_name":\s*"\([^"]*\)".*/\1/')

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
trap 'rm -f "$TMP_FILE"' EXIT

echo "  Descargando: ${DOWNLOAD_URL}"
${FETCH} "$DOWNLOAD_URL" > "$TMP_FILE"

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

# ── Agregar al PATH si no está ────────────────────────────────────────────────
if ! echo "$PATH" | grep -q "$INSTALL_DIR"; then
  echo ""
  echo "IMPORTANTE: Agregá ${INSTALL_DIR} a tu PATH:"
  echo "  echo 'export PATH=\"\$HOME/.local/bin:\$PATH\"' >> ~/.bashrc"
  echo "  source ~/.bashrc"
fi

# ── Lanzar wizard ─────────────────────────────────────────────────────────────
echo ""
echo "Iniciando wizard de instalación..."
"${INSTALL_DIR}/flowforge" install --yes
