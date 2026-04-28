#!/usr/bin/env bash
# 07 — Genera tráfico realista para llenar el dashboard de App Insights.
# Mezcla: peticiones normales, peticiones con orders (custom metrics),
# errores 500, exceptions y dependency-fails.
# Uso: ./07-generate-traffic.sh [duracion_min]

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

DURATION_MIN="${1:-5}"
BASE="https://${APP}.azurewebsites.net"

echo "URL base: $BASE"
echo "Duración: $DURATION_MIN min"
echo "Endpoints que se van a llamar:"
echo "  - GET  /                 (frecuente, casos buenos)"
echo "  - GET  /info             (frecuente)"
echo "  - GET  /health           (frecuente)"
echo "  - POST /demo/orders      (custom metrics)"
echo "  - GET  /demo/error?type=500           (~5% del tráfico)"
echo "  - GET  /demo/error?type=exception     (~3% del tráfico)"
echo "  - GET  /demo/error?type=dependency-fail (~2% del tráfico)"
echo
confirm "Empezar?"

end=$(( $(date +%s) + DURATION_MIN * 60 ))
batch=0
while [ "$(date +%s)" -lt "$end" ]; do
  batch=$((batch + 1))

  # 5 peticiones rápidas a / e /info
  for _ in 1 2 3 4 5; do
    curl -s -o /dev/null "$BASE/" &
    curl -s -o /dev/null "$BASE/info" &
    curl -s -o /dev/null "$BASE/health" &
  done

  # 2 órdenes
  curl -s -o /dev/null -X POST "$BASE/demo/orders" \
    -H "Content-Type: application/json" \
    -d '{"sku":"SKU-A","quantity":2,"unitPrice":12.5,"priority":"normal"}' &
  curl -s -o /dev/null -X POST "$BASE/demo/orders" \
    -H "Content-Type: application/json" \
    -d '{"sku":"SKU-B","quantity":1,"unitPrice":99,"priority":"high"}' &

  # Errores con baja probabilidad
  if (( batch % 10 == 0 )); then
    curl -s -o /dev/null "$BASE/demo/error?type=500" &
  fi
  if (( batch % 15 == 0 )); then
    curl -s -o /dev/null "$BASE/demo/error?type=exception" &
  fi
  if (( batch % 20 == 0 )); then
    curl -s -o /dev/null "$BASE/demo/error?type=dependency-fail" &
  fi

  wait
  remaining=$(( end - $(date +%s) ))
  echo "[batch $batch] $remaining s restantes"
done

ok "Tráfico generado. Ve a Portal -> $AI -> Live Metrics o Application Map."
