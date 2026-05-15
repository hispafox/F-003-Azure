#!/usr/bin/env bash
# 03 - Smoke test de los 4 triggers (slide 11 de la práctica).
# Toca cada trigger y verifica end-to-end con /api/estado.

set -euo pipefail
source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

BASE="https://${FUNC}.azurewebsites.net"
API="$BASE/api"
TIMEOUT=30

step "Obteniendo function key"
KEY=$(az functionapp keys list --name "$FUNC" --resource-group "$RG" \
  --query "functionKeys.default" -o tsv)
[ -n "$KEY" ] || { echo "[X] No se pudo leer la function key"; exit 1; }

STORAGE_CONN=$(az storage account show-connection-string \
  --name "$STORAGE" --resource-group "$RG" \
  --query connectionString -o tsv)

# ── [1/4] HTTP Trigger ──
step "[1/4] HTTP: GET /api/productos"
curl -s --max-time $TIMEOUT "$API/productos?code=$KEY" | head -c 300; echo

step "    HTTP: POST /api/productos"
curl -s --max-time $TIMEOUT -X POST \
  -H "Content-Type: application/json" \
  -d '{"nombre":"Smoke","precio":99.99}' \
  "$API/productos?code=$KEY" | head -c 200; echo

# ── [2/4] Timer Trigger ──
step "[2/4] Timer: ya está corriendo (cada minuto). Esperando 70s..."
sleep 70
echo "  Verificando con /api/estado"
RESP=$(curl -s --max-time $TIMEOUT "$API/estado?code=$KEY")
TIMER_TICKS=$(echo "$RESP" | grep -oE '"totalEjecuciones":[0-9]+' | grep -oE '[0-9]+' || echo 0)
echo "  Timer ha ejecutado $TIMER_TICKS veces"

# ── [3/4] Blob Trigger ──
step "[3/4] Blob: subiendo test.csv a uploads/"
TS=$(date -u +%s)
TMP_CSV=$(mktemp -t smoke-csv.XXXXXX.csv)
cat > "$TMP_CSV" <<'EOF'
nombre,precio,stock
Smoke-Laptop,999,10
Smoke-Mouse,29,50
EOF
az storage blob upload \
  --connection-string "$STORAGE_CONN" \
  --container-name "$CONTAINER_UPLOADS" \
  --name "smoke-$TS.csv" --file "$TMP_CSV" \
  --overwrite --output none
ok "    CSV subido como smoke-$TS.csv"

echo "    Esperando 60s al Blob trigger (polling en Consumption)..."
sleep 60
EXISTS=$(az storage blob exists \
  --connection-string "$STORAGE_CONN" \
  --container-name "$CONTAINER_RESULTADOS" \
  --name "smoke-$TS-resumen.json" \
  --query exists -o tsv 2>/dev/null || echo false)
if [ "$EXISTS" = "true" ]; then
  ok "    Blob procesado: resultados/smoke-$TS-resumen.json"
else
  warn "    Blob NO procesado todavía. Aumenta espera o revisa logs."
fi
rm -f "$TMP_CSV"

# ── [4/4] Cosmos DB Change Feed Trigger ──
step "[4/4] Cosmos: insertando pedido"
PEDIDO_ID="ped-smoke-$TS"
DOC="{\"id\":\"$PEDIDO_ID\",\"clienteId\":\"cliente-smoke\",\"estado\":\"nuevo\",\"total\":150.00}"
az cosmosdb sql container create-item-or-update \
  --account-name "$COSMOS" --resource-group "$RG" \
  --database-name "$COSMOS_DB" --container-name "$COSMOS_PEDIDOS" \
  --partition-key-value "cliente-smoke" \
  --body "$DOC" \
  --output none 2>/dev/null || warn "(insert manual desde Portal si az falla)"

echo "    Esperando 15s al Change Feed..."
sleep 15
RESP=$(curl -s --max-time $TIMEOUT "$API/estado?code=$KEY")
NOTIFS=$(echo "$RESP" | grep -oE '"totalNotificaciones":[0-9]+' | grep -oE '[0-9]+' || echo 0)
echo "    Cosmos trigger ha anotado $NOTIFS notificaciones en total"

echo
ok "Smoke test completado — los 4 triggers tocados"
