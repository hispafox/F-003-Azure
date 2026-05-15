#!/usr/bin/env bash
# 03 - Smoke test: versionado v1/v2 + feature flag + health/version.
# (Para el check formal de post-deploy usa ./05-postdeploy-check.sh)

set -euo pipefail
source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

BASE="https://${FUNC}.azurewebsites.net/api"
TIMEOUT=60

check() {
  local label="$1" url="$2" expected="$3" method="${4:-GET}" body="${5:-}"
  echo -n "  $label "
  args=(-o /tmp/s44 -s -w "%{http_code}" --max-time $TIMEOUT -X "$method")
  [ -n "$body" ] && args+=(-H "Content-Type: application/json" -d "$body")
  HTTP_CODE=$(curl "${args[@]}" "$url")
  if [ "$HTTP_CODE" = "$expected" ]; then echo "OK"; else
    echo "FAIL (got $HTTP_CODE, expected $expected)"; cat /tmp/s44; echo; return 1
  fi
}

echo "Smoke test sobre $BASE"; echo

check "[1] GET /health                 " "$BASE/health" "200"
check "[2] GET /version                " "$BASE/version" "200"
check "[3] GET /v1/productos           " "$BASE/v1/productos" "200"
check "[4] GET /v2/productos           " "$BASE/v2/productos" "200"
check "[5] GET /v1/productos/p001      " "$BASE/v1/productos/p001" "200"
check "[6] GET /v2/productos/zzz (404) " "$BASE/v2/productos/zzz" "404"
check "[7] POST /pedidos/procesar      " \
  "$BASE/pedidos/procesar" "200" "POST" \
  '{"id":"sm-1","clienteId":"c1","total":200}'

echo
step "Comparar contratos v1 vs v2 (v2 debe traer moneda y stock):"
echo "  v1: $(curl -s --max-time $TIMEOUT "$BASE/v1/productos/p001")"
echo "  v2: $(curl -s --max-time $TIMEOUT "$BASE/v2/productos/p001")"

echo
step "Feature flag: el procesador depende de FEATURE_NUEVO_PROCESAMIENTO"
echo "  Estado actual del flag:"
curl -s --max-time $TIMEOUT "$BASE/version" | head -c 300; echo
echo
echo "  Para activarlo SIN redeploy (slide 16):"
echo "    az functionapp config appsettings set --name $FUNC -g $RG \\"
echo "      --settings FEATURE_NUEVO_PROCESAMIENTO=true"
echo "  y repetir el POST: ahora ProcesadoPor='nuevo' y total con -5%."

rm -f /tmp/s44
echo
ok "Smoke test completado"
