#!/usr/bin/env bash
# 02 - Smoke test contra el slot indicado (slide 5/10). Verifica:
#   - HTTP 200 en /health
#   - latencia media en N requests
# Devuelve exit 0 si pasa, 1 si no. SOLO LECTURA.

set -euo pipefail
source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

SLOT="${1:-staging}"
N="${2:-10}"
THRESHOLD_LATENCY="${3:-2.0}"  # segundos

if [ "$SLOT" = "production" ] || [ "$SLOT" = "prod" ]; then
  URL="https://${APP_NAME}.azurewebsites.net"
else
  URL="https://${APP_NAME}-${SLOT}.azurewebsites.net"
fi

step "Smoke test: $URL/health (N=$N)"
CODE=$(curl -s -o /dev/null -w "%{http_code}" --max-time 10 "$URL/health" || echo "000")
echo "HTTP code: $CODE"

if [ "$CODE" != "200" ]; then
  warn "Health check distinto de 200 -> rollback necesario (slide 10)."
  exit 1
fi

step "Latencia media en $N requests"
TOTAL=0
for _ in $(seq 1 "$N"); do
  T=$(curl -s -o /dev/null -w "%{time_total}" --max-time 10 "$URL/" || echo "9.9")
  TOTAL=$(echo "$TOTAL + $T" | bc -l)
done
AVG=$(echo "scale=3; $TOTAL / $N" | bc -l)
echo "Latencia media: ${AVG}s (umbral ${THRESHOLD_LATENCY}s)"

if (( $(echo "$AVG > $THRESHOLD_LATENCY" | bc -l) )); then
  warn "Latencia ${AVG}s supera el umbral ${THRESHOLD_LATENCY}s -> rollback (slide 10)."
  exit 1
fi

ok "Smoke test OK: HTTP 200 y latencia ${AVG}s."
