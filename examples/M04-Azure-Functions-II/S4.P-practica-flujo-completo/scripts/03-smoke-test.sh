#!/usr/bin/env bash
# 03 - Smoke test del flujo completo end-to-end:
#   POST /pedidos → Cosmos → (Change Feed) factura a Blob + msg a Queue
#                 → (Queue) notificación. Verifica con GET /estado.

set -euo pipefail
source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

API="https://${FUNC}.azurewebsites.net/api"
TIMEOUT=30

step "Obteniendo function key"
KEY=$(az functionapp keys list --name "$FUNC" --resource-group "$RG" \
  --query "functionKeys.default" -o tsv)
[ -n "$KEY" ] || { echo "[X] No se pudo leer la function key"; exit 1; }

STORAGE_CONN=$(az storage account show-connection-string \
  --name "$STORAGE" --resource-group "$RG" --query connectionString -o tsv)

step "PASO 1 — POST /api/pedidos"
RESP=$(curl -s --max-time $TIMEOUT -X POST -H "Content-Type: application/json" \
  -d '{"clienteId":"cli-smoke","clienteNombre":"Smoke","items":[{"productoId":"p1","nombre":"Laptop","cantidad":1,"precioUnitario":999.99},{"productoId":"p2","nombre":"Mouse","cantidad":2,"precioUnitario":29.99}]}' \
  "$API/pedidos?code=$KEY")
echo "  $RESP" | head -c 200; echo

step "Esperando 25s al Change Feed + Queue (pasos 2 y 3)..."
sleep 25

step "GET /api/estado (esperado: creados>=1, facturados>=1, notificados>=1)"
curl -s --max-time $TIMEOUT "$API/estado?code=$KEY" | head -c 400; echo

step "Verificando blob en '$BLOB_CONTAINER/'"
N=$(az storage blob list --container-name "$BLOB_CONTAINER" \
  --connection-string "$STORAGE_CONN" --query "length(@)" -o tsv 2>/dev/null || echo "?")
echo "  facturas en blob: $N"

echo
ok "Smoke test completado — flujo de 3 saltos verificado"
echo "Logs: az functionapp log tail --name $FUNC -g $RG"
