#!/usr/bin/env bash
# 03 - Smoke test S4.P2: arranca la orquestación de saludos (fan-out/
# fan-in) y comprueba que termina Completed con un saludo por nombre.

set -euo pipefail
source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

API="https://${FUNC}.azurewebsites.net/api"
TIMEOUT=30

step "Obteniendo function key"
KEY=$(az functionapp keys list --name "$FUNC" --resource-group "$RG" \
  --query "functionKeys.default" -o tsv)
[ -n "$KEY" ] || { echo "[X] No se pudo leer la function key"; exit 1; }

step "POST /api/saludos  [\"Ana\",\"Luis\",\"Marta\",\"Pedro\"]"
RESP=$(curl -s --max-time $TIMEOUT -X POST \
  -H "Content-Type: application/json" \
  -d '["Ana","Luis","Marta","Pedro"]' \
  "$API/saludos?code=$KEY")
echo "  $RESP" | head -c 200; echo
ID=$(echo "$RESP" | grep -oE '"instanceId":"[^"]+"' | head -1 | cut -d'"' -f4)
[ -n "$ID" ] || { echo "[X] No se obtuvo instanceId"; exit 1; }

step "Esperando 15s a que el orquestador complete..."
sleep 15

step "Estado (esperado runtimeStatus=Completed, output con 4 saludos):"
curl -s --max-time $TIMEOUT "$API/saludos/$ID?code=$KEY" | head -c 500; echo

echo
step "Lista vacía → 400"
CODE=$(curl -s -o /dev/null -w "%{http_code}" --max-time $TIMEOUT -X POST \
  -H "Content-Type: application/json" -d '[]' "$API/saludos?code=$KEY")
echo "  HTTP $CODE (esperado 400)"

echo
ok "Smoke test completado"
echo "Logs: az functionapp log tail --name $FUNC -g $RG"
