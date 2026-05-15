#!/usr/bin/env bash
# 03 - Smoke test del MultiResponse (slide 6) y del pipeline export (slide 7).
# Verifica los 4 endpoints HTTP y comprueba los efectos en Cosmos + Queue + Blob.

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

# ── POST /api/pedidos (MultiResponse: HTTP 201 + Cosmos + Queue) ──
step "POST /api/pedidos (cliente-A, 150 EUR)"
RESP=$(curl -s --max-time $TIMEOUT -X POST \
  -H "Content-Type: application/json" \
  -d '{"clienteId":"cliente-A","total":150.00,"notas":"smoke test"}' \
  "$API/pedidos?code=$KEY")

PEDIDO_ID=$(echo "$RESP" | grep -oE '"id":"[^"]+"' | head -1 | cut -d'"' -f4)
[ -n "$PEDIDO_ID" ] || { echo "[X] POST falló: $RESP"; exit 1; }
ok "Pedido creado: $PEDIDO_ID"

# ── Verifica Cosmos ──
sleep 2
step "Verificando Cosmos DB"
COUNT=$(az cosmosdb sql query \
  --account-name "$COSMOS" --resource-group "$RG" \
  --database-name "$COSMOS_DB" --container-name "$COSMOS_PEDIDOS" \
  --query-text "SELECT VALUE COUNT(1) FROM c WHERE c.id = '$PEDIDO_ID'" \
  --output tsv 2>/dev/null || echo "?")
echo "  documentos con id=$PEDIDO_ID: $COUNT"

# ── Verifica Queue ──
step "Verificando Queue '$QUEUE'"
QUEUE_LEN=$(az storage queue stats --name "$QUEUE" \
  --connection-string "$STORAGE_CONN" \
  --query approximateMessagesCount -o tsv 2>/dev/null || echo "?")
echo "  mensajes aproximados en la cola (probable que ya hayan sido consumidos): $QUEUE_LEN"

# ── GET /api/pedidos/{cliente}/{id}: CosmosDBInput por id ──
step "GET /api/pedidos/cliente-A/$PEDIDO_ID"
curl -s --max-time $TIMEOUT "$API/pedidos/cliente-A/$PEDIDO_ID?code=$KEY" | head -c 400
echo

# ── GET /api/clientes/{cliente}/pedidos: CosmosDBInput por SqlQuery ──
step "GET /api/clientes/cliente-A/pedidos"
curl -s --max-time $TIMEOUT "$API/clientes/cliente-A/pedidos?code=$KEY" | head -c 600
echo

# ── GET /api/exportar/{cliente}/{id}: CosmosDBInput + BlobOutput ──
step "GET /api/exportar/cliente-A/$PEDIDO_ID"
curl -s --max-time $TIMEOUT "$API/exportar/cliente-A/$PEDIDO_ID?code=$KEY" | head -c 400
echo

# ── Verifica que el blob se materializo en exports/{yyyy-MM-dd}/... ──
TODAY=$(date -u +%Y-%m-%d)
BLOB_NAME="$TODAY/pedido-cliente-A-$PEDIDO_ID.json"
step "Verificando blob exports/$BLOB_NAME"
EXISTS=$(az storage blob exists \
  --connection-string "$STORAGE_CONN" \
  --container-name "$BLOB_CONTAINER" \
  --name "$BLOB_NAME" \
  --query exists -o tsv 2>/dev/null || echo "false")
if [ "$EXISTS" = "true" ]; then
  ok "BlobOutput escribio el JSON en exports/$BLOB_NAME"
else
  warn "Blob NO encontrado. Revisa logs con: az functionapp log tail --name $FUNC -g $RG"
fi

# ── POST con body inválido: debe devolver 400 y NO crear nada ──
echo
step "POST /api/pedidos con body invalido (debe devolver 400, NO crear en Cosmos)"
CODE=$(curl -s --max-time $TIMEOUT -X POST \
  -H "Content-Type: application/json" \
  -d '{"clienteId":"","total":-1}' \
  -o /dev/null -w "%{http_code}" \
  "$API/pedidos?code=$KEY")
echo "  HTTP $CODE (esperado 400)"

ok "Smoke test completado"
