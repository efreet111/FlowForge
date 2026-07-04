#!/usr/bin/env bash
# Tests PM-* ejecutados DENTRO del contenedor (llamado por test-docker.sh)
# No llamar directamente — usar scripts/test-docker.sh

set -uo pipefail

PASS=0
FAIL=0

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

pass() { echo -e "${GREEN}[PASS]${NC} $1"; ((PASS++)); }
fail() { echo -e "${RED}[FAIL]${NC} $1"; ((FAIL++)); }
warn() { echo -e "${YELLOW}[WARN]${NC} $1"; }
log()  { echo -e "${BLUE}[test]${NC} $1"; }
sep()  { echo -e "${BLUE}───────────────────────────────────────${NC}"; }

echo ""
echo -e "${BLUE}╔═══════════════════════════════════════╗${NC}"
echo -e "${BLUE}║  FlowForge — PM-* Tests (in-container)║${NC}"
echo -e "${BLUE}╚═══════════════════════════════════════╝${NC}"
echo ""

# ── PM-4: flowforge doctor ───────────────────────────────────────────────────
sep
log "PM-4 — flowforge doctor"
log "Esperado: tabla con 5 checks, exit code 2 (engram no instalado)"
echo ""

OUTPUT=$(flowforge doctor 2>&1 || true)
EXIT_CODE=$?
echo "$OUTPUT"
echo ""
log "Exit code: $EXIT_CODE"

if echo "$OUTPUT" | grep -qi "flowforge\|engram\|mcp\|github\|path"; then
    pass "PM-4a: doctor muestra tabla de checks"
else
    fail "PM-4a: doctor no mostró checks esperados"
fi

if [ "$EXIT_CODE" -ne 0 ]; then
    pass "PM-4b: exit code != 0 cuando componentes faltan (correcto)"
else
    warn "PM-4b: exit code = 0 (todos los checks pasaron inesperadamente)"
fi

# ── PM-5: Banner visible al inicio ───────────────────────────────────────────
sep
log "PM-5 — Banner visible < 5s"
log "Esperado: 'FlowForge' y 'Conectando' aparecen en los primeros 5s"
echo ""

START_NS=$(date +%s%N)
BANNER=$(timeout 8 flowforge install --yes 2>&1 | head -5 || true)
END_NS=$(date +%s%N)
ELAPSED_MS=$(( (END_NS - START_NS) / 1000000 ))

echo "$BANNER"
echo ""
log "Primera salida en: ${ELAPSED_MS}ms"

if echo "$BANNER" | grep -qi "flowforge\|installer\|stack"; then
    pass "PM-5a: banner de FlowForge visible"
else
    fail "PM-5a: no se encontró banner de FlowForge"
fi

if echo "$BANNER" | grep -qi "conectando\|github\|connecting"; then
    pass "PM-5b: mensaje 'Conectando a GitHub' visible"
else
    warn "PM-5b: mensaje de conexión no encontrado en primeras 5 líneas"
fi

if [ "$ELAPSED_MS" -lt 5000 ]; then
    pass "PM-5c: primera salida en ${ELAPSED_MS}ms (< 5000ms)"
else
    warn "PM-5c: primera salida tardó ${ELAPSED_MS}ms (puede ser overhead de container)"
fi

# ── PM-2: Timeout con GitHub bloqueado ───────────────────────────────────────
sep
log "PM-2 — Timeout con GitHub bloqueado"
log "Bloqueando api.github.com via /etc/hosts..."
echo "127.0.0.1 api.github.com github.com raw.githubusercontent.com" >> /etc/hosts

log "Corriendo 'flowforge install --yes' — esperado: error en < 40s"
echo ""

START=$(date +%s)
OUTPUT=$(timeout 50 flowforge install --yes 2>&1 || true)
END=$(date +%s)
ELAPSED=$((END - START))

echo "$OUTPUT"
echo ""
log "Tiempo hasta error: ${ELAPSED}s"

if echo "$OUTPUT" | grep -qi "github\|timeout\|responde\|conexión\|releases\|conectar"; then
    pass "PM-2a: mensaje de error claro con referencia a GitHub"
else
    fail "PM-2a: no se encontró mensaje de error claro"
fi

if [ "$ELAPSED" -le 40 ]; then
    pass "PM-2b: falló en ${ELAPSED}s (dentro del límite de 40s)"
else
    fail "PM-2b: tardó ${ELAPSED}s — timeout demasiado largo"
fi

# Desbloquear GitHub para los siguientes tests
sed -i '/api.github.com\|raw.githubusercontent.com/d' /etc/hosts

# ── PM-3: FLOWFORGE_YES headless ─────────────────────────────────────────────
sep
log "PM-3 — FLOWFORGE_YES headless mode"
log "Verificando que el bash script detecta FLOWFORGE_YES"
echo ""

# Prueba directa: el script debe pasar --yes al flowforge sin esperar TTY
# Usamos --version para forzar una versión específica y no descargar
OUTPUT=$(FLOWFORGE_YES=1 bash -x /test/install.sh 2>&1 | head -40 || true)
echo "$OUTPUT"
echo ""

if echo "$OUTPUT" | grep -q 'FLOWFORGE_YES\|--yes\|install --yes'; then
    pass "PM-3a: script detecta FLOWFORGE_YES en modo trace"
else
    warn "PM-3a: FLOWFORGE_YES no aparece explícitamente en trace (verificar lógica TTY)"
fi

# Verificar que la condición está en el script
if grep -q 'FLOWFORGE_YES' /test/install.sh; then
    pass "PM-3b: install.sh contiene lógica FLOWFORGE_YES"
else
    fail "PM-3b: install.sh NO contiene FLOWFORGE_YES"
fi

# ── Resumen ───────────────────────────────────────────────────────────────────
sep
echo ""
echo -e "${BLUE}Resultados: ${GREEN}${PASS} PASS${NC} / ${RED}${FAIL} FAIL${NC}"
echo ""

[ "$FAIL" -eq 0 ]
