#!/usr/bin/env bash
# 03 - Smoke test del CRUD HTTP (slide 14 de la practica).

set -euo pipefail
source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

BASE="https://${FUNC}.azurewebsites.net/api"
TIMEOUT=30

step "Obteniendo function key"
KEY=$(az functionapp keys list --name "$FUNC" --resource-group "$RG" \
  --query "functionKeys.default" -o tsv)
[ -n "$KEY" ] || { echo "[X] No se pudo leer la function key"; exit 1; }

echo "Smoke tests sobre $BASE"

# [1/5] GET listar
echo -n "  [1/5] GET /productos ... "
CODE=$(curl -o /dev/null -s -w "%{http_code}" --max-time $TIMEOUT \
  "$BASE/productos?code=$KEY")
[ "$CODE" = "200" ] && echo "OK ($CODE)" || { echo "FAIL ($CODE)"; exit 1; }

# [2/5] POST crear
echo -n "  [2/5] POST /productos ... "
RESP=$(curl -s --max-time $TIMEOUT -X POST \
  -H "Content-Type: application/json" \
  -d '{"nombre":"Smoke","precio":1.0,"stock":1}' \
  "$BASE/productos?code=$KEY")
NEW_ID=$(echo "$RESP" | grep -oE '"id":"[^"]+"' | head -1 | cut -d'"' -f4)
[ -n "$NEW_ID" ] && echo "OK (id=$NEW_ID)" || { echo "FAIL ($RESP)"; exit 1; }

# [3/5] GET por id
echo -n "  [3/5] GET /productos/$NEW_ID ... "
CODE=$(curl -o /dev/null -s -w "%{http_code}" --max-time $TIMEOUT \
  "$BASE/productos/$NEW_ID?code=$KEY")
[ "$CODE" = "200" ] && echo "OK" || { echo "FAIL ($CODE)"; exit 1; }

# [4/5] PUT actualizar
echo -n "  [4/5] PUT /productos/$NEW_ID ... "
CODE=$(curl -o /dev/null -s -w "%{http_code}" --max-time $TIMEOUT \
  -X PUT \
  -H "Content-Type: application/json" \
  -d '{"nombre":"SmokeUpd","precio":2.0,"stock":2}' \
  "$BASE/productos/$NEW_ID?code=$KEY")
[ "$CODE" = "200" ] && echo "OK" || { echo "FAIL ($CODE)"; exit 1; }

# [5/5] DELETE
echo -n "  [5/5] DELETE /productos/$NEW_ID ... "
CODE=$(curl -o /dev/null -s -w "%{http_code}" --max-time $TIMEOUT \
  -X DELETE "$BASE/productos/$NEW_ID?code=$KEY")
[ "$CODE" = "204" ] && echo "OK" || { echo "FAIL ($CODE)"; exit 1; }

echo
ok "Smoke tests pasados (5/5)"
