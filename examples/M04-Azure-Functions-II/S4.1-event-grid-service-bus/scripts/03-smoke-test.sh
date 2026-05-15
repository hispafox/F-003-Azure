#!/usr/bin/env bash
# 03 - Smoke test S4.1: toca los 4 caminos y verifica via /api/estado.
#   1) POST /pedidos        → encolado + topic notificacion
#   2) (consumer)           → procesado por el SB queue trigger
#   3) (consumer)           → notificado por el SB topic+subscription
#   4) Sube .pdf y .csv     → EG dispara ClasificarArchivo → encola

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

# ── [1/4] HTTP → SB Queue + Topic ──
step "[1/4] POST /api/pedidos → cola + topic"
RESP=$(curl -s --max-time $TIMEOUT -X POST \
  -H "Content-Type: application/json" \
  -d '{"clienteId":"cliente-smoke","clienteEmail":"smoke@test.com","total":150.00,"notas":"smoke"}' \
  "$API/pedidos?code=$KEY")
echo "  Respuesta: $RESP" | head -c 200; echo

# Esperar a que el SB queue trigger y el topic trigger procesen
step "[2-3/4] Esperando 30s a los SB triggers (queue + topic)..."
sleep 30
RESP=$(curl -s --max-time $TIMEOUT "$API/estado?code=$KEY")
echo "  /api/estado:"
echo "  $RESP" | head -c 500; echo

# ── [4/4] Event Grid: subir blobs .pdf y .csv ──
TS=$(date -u +%s)
TMP_PDF=$(mktemp -t smoke.XXXXXX.pdf)
TMP_CSV=$(mktemp -t smoke.XXXXXX.csv)
echo "fake-pdf-content" > "$TMP_PDF"
echo "nombre,precio" > "$TMP_CSV"
echo "smoke,1.0" >> "$TMP_CSV"

step "[4/4] Subiendo factura-$TS.pdf y data-$TS.csv a $CONTAINER_UPLOADS/"
az storage blob upload --connection-string "$STORAGE_CONN" \
  --container-name "$CONTAINER_UPLOADS" \
  --name "factura-$TS.pdf" --file "$TMP_PDF" --output none
az storage blob upload --connection-string "$STORAGE_CONN" \
  --container-name "$CONTAINER_UPLOADS" \
  --name "data-$TS.csv" --file "$TMP_CSV" --output none
rm -f "$TMP_PDF" "$TMP_CSV"

step "Esperando 30s al Event Grid + clasificacion..."
sleep 30
RESP=$(curl -s --max-time $TIMEOUT "$API/estado?code=$KEY")
echo "  /api/estado final:"
echo "  $RESP" | head -c 600; echo

ok "Smoke test completado"
echo
echo "Si algun contador no se incremento, revisa:"
echo "  az functionapp log tail --name $FUNC -g $RG"
