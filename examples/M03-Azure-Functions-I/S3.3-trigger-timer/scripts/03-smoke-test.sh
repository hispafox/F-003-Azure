#!/usr/bin/env bash
# 03 - Smoke test: cubre HTTP triggers + dispara los Timer triggers
# manualmente con la master key (slide 15) y verifica el resultado.

set -euo pipefail
source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

BASE="https://${FUNC}.azurewebsites.net"
API="$BASE/api"
ADMIN="$BASE/admin/functions"
TIMEOUT=60

step "Obteniendo function key + master key"
KEY=$(az functionapp keys list --name "$FUNC" --resource-group "$RG" \
  --query "functionKeys.default" -o tsv)
MASTER=$(az functionapp keys list --name "$FUNC" --resource-group "$RG" \
  --query "masterKey" -o tsv)
[ -n "$KEY" ] || { echo "[X] No se pudo leer la function key"; exit 1; }
[ -n "$MASTER" ] || { echo "[X] No se pudo leer la master key"; exit 1; }

check() {
  local label="$1" url="$2" expected="$3" method="${4:-GET}" body="${5:-}"
  echo -n "  $label "
  args=(-o /tmp/out -s -w "%{http_code}" --max-time $TIMEOUT -X "$method")
  [ -n "$body" ] && args+=(-H "Content-Type: application/json" -d "$body")
  HTTP_CODE=$(curl "${args[@]}" "$url")
  if [ "$HTTP_CODE" = "$expected" ]; then echo "OK"; else
    echo "FAIL (got $HTTP_CODE, expected $expected)"; cat /tmp/out; echo; return 1
  fi
}

echo "Smoke test sobre $BASE"
echo

# ── HTTP triggers (heredados de S3.2) ──
check "[1/7] GET  /api/ping (Anonymous)        " "$API/ping" "200"
check "[2/7] GET  /api/productos               " "$API/productos?code=$KEY" "200"

# ── Timer triggers — disparados manualmente con master key (slide 15) ──
echo
step "Disparando InformeDiario manualmente (slide 15)"
HTTP_CODE=$(curl -o /tmp/out -s -w "%{http_code}" --max-time $TIMEOUT \
  -X POST "$ADMIN/InformeDiario" \
  -H "x-functions-key: $MASTER" \
  -H "Content-Type: application/json" \
  -d '{"input":""}')
if [ "$HTTP_CODE" = "202" ] || [ "$HTTP_CODE" = "200" ]; then
  ok "InformeDiario aceptado ($HTTP_CODE)"
else
  echo "[X] InformeDiario respondio $HTTP_CODE"; cat /tmp/out; exit 1
fi

step "Esperando 5s a que el timer ejecute..."
sleep 5

# ── Verificar que /api/informes refleja el resultado del timer ──
check "[3/7] GET  /api/informes (>=1 informe)  " "$API/informes?code=$KEY" "200"
total=$(grep -oE '"total":[0-9]+' /tmp/out | head -1 | cut -d: -f2)
if [ "${total:-0}" -ge 1 ]; then
  echo "       -> total = $total informes en el catalogo"
else
  echo "[!] /api/informes devuelve total=$total (esperado >=1)"
  echo "    El timer puede tardar mas de 5s en azure - reintenta en unos segundos."
fi

# ── Idempotencia: disparar InformeDiario otra vez no debe duplicar ──
echo
step "Probando idempotencia (slide 12): segundo disparo"
HTTP_CODE=$(curl -o /tmp/out -s -w "%{http_code}" --max-time $TIMEOUT \
  -X POST "$ADMIN/InformeDiario" \
  -H "x-functions-key: $MASTER" \
  -H "Content-Type: application/json" \
  -d '{"input":""}')
[ "$HTTP_CODE" = "202" ] || [ "$HTTP_CODE" = "200" ] || {
  echo "[X] Segundo disparo: $HTTP_CODE"; exit 1; }
sleep 3

check "[4/7] GET  /api/informes (sigue siendo 1)" "$API/informes?code=$KEY" "200"
total2=$(grep -oE '"total":[0-9]+' /tmp/out | head -1 | cut -d: -f2)
if [ "${total2:-0}" = "${total:-0}" ]; then
  echo "       -> total $total2 == $total: idempotencia OK"
else
  echo "[!] total cambio: $total -> $total2 (esperabamos igual)"
fi

# ── Disparar CleanupCadaMinuto manualmente ──
echo
step "Disparando CleanupCadaMinuto manualmente"
HTTP_CODE=$(curl -o /tmp/out -s -w "%{http_code}" --max-time $TIMEOUT \
  -X POST "$ADMIN/CleanupCadaMinuto" \
  -H "x-functions-key: $MASTER" \
  -H "Content-Type: application/json" \
  -d '{"input":""}')
[ "$HTTP_CODE" = "202" ] || [ "$HTTP_CODE" = "200" ] || {
  echo "[X] CleanupCadaMinuto respondio $HTTP_CODE"; exit 1; }
ok "CleanupCadaMinuto aceptado"

check "[5/7] GET  /api/informes/<fecha>         " \
  "$API/informes/$(date -u -d 'yesterday' +%Y-%m-%d 2>/dev/null || date -u -v-1d +%Y-%m-%d)?code=$KEY" "200"

check "[6/7] GET  /api/informes/2030-01-01 (404)" \
  "$API/informes/2030-01-01?code=$KEY" "404"

check "[7/7] GET  /api/informes/no-fecha (400)  " \
  "$API/informes/no-fecha?code=$KEY" "400"

rm -f /tmp/out
echo
ok "Smoke test completado"
echo
echo "Tip: az functionapp log tail --name $FUNC -g $RG  para ver los logs en vivo."
