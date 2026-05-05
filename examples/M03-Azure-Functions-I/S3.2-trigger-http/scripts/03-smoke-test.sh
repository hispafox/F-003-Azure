#!/usr/bin/env bash
# 03 - Smoke test: cubre los 5 verbos del CRUD + Ping anonimo.

set -euo pipefail
source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

BASE="https://${FUNC}.azurewebsites.net/api"
TIMEOUT=60

# Para los endpoints "Function" necesitamos la function key (slide 10).
step "Obteniendo function key (default)"
KEY=$(az functionapp keys list --name "$FUNC" --resource-group "$RG" \
  --query "functionKeys.default" -o tsv)
[ -n "$KEY" ] || { echo "[X] No se pudo leer la function key"; exit 1; }

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

check "[1/8] GET  /ping (Anonymous)            " "$BASE/ping" "200"
check "[2/8] GET  /productos                    " "$BASE/productos?code=$KEY" "200"
check "[3/8] GET  /productos/p-001 (existe)     " "$BASE/productos/p-001?code=$KEY" "200"
check "[4/8] GET  /productos/no-existe (404)    " "$BASE/productos/no-existe?code=$KEY" "404"
check "[5/8] POST /productos (valido)           " \
  "$BASE/productos?code=$KEY" "201" "POST" \
  '{"nombre":"Smoke test product","categoria":"libros","precio":9.99,"stock":3}'
check "[6/8] POST /productos (invalido -> 422)  " \
  "$BASE/productos?code=$KEY" "422" "POST" \
  '{"nombre":"X","categoria":"","precio":-1,"stock":-1}'
check "[7/8] PUT  /productos/p-001              " \
  "$BASE/productos/p-001?code=$KEY" "200" "PUT" \
  '{"precio":1099.00}'
check "[8/8] DELETE /productos/p-002            " \
  "$BASE/productos/p-002?code=$KEY" "204" "DELETE"

rm -f /tmp/out
echo
ok "Smoke test completado"
