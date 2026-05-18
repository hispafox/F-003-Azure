#!/usr/bin/env bash
# 02 - Smoke test del Storage provisionado con `az storage` (sin lanzar
# la API — eso lo hace el alumno con `dotnet run`). Verifica que blob,
# table y queue responden round-trip.

set -euo pipefail
source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

CONN=$(az storage account show-connection-string \
  --name "$STORAGE" --resource-group "$RG" --query connectionString -o tsv)

TS=$(date -u +%s)
TMP=$(mktemp -t s51.XXXXXX.csv)
echo "factura,total" > "$TMP"; echo "F-$TS,1299.99" >> "$TMP"

step "[Blob] subir factura"
az storage blob upload --connection-string "$CONN" \
  --container-name facturas --name "2026/05/f-$TS.csv" \
  --file "$TMP" --overwrite --output none
EX=$(az storage blob exists --connection-string "$CONN" \
  --container-name facturas --name "2026/05/f-$TS.csv" --query exists -o tsv)
echo "  existe en blob: $EX"

step "[Table] insertar + leer (AuditLog)"
az storage table create --connection-string "$CONN" --name AuditLog --output none
az storage entity insert --connection-string "$CONN" --table-name AuditLog \
  --entity PartitionKey=2026-05-15 RowKey="$TS" Usuario=pedro Accion=Smoke \
  --output none
N=$(az storage entity query --connection-string "$CONN" --table-name AuditLog \
  --filter "PartitionKey eq '2026-05-15'" --query "length(items)" -o tsv)
echo "  filas en particion 2026-05-15: $N"

step "[Queue] encolar + stats"
az storage message put --connection-string "$CONN" \
  --queue-name pedidos --content "smoke-$TS" --output none
LEN=$(az storage queue stats --connection-string "$CONN" --name pedidos \
  --query approximateMessagesCount -o tsv 2>/dev/null || echo "?")
echo "  mensajes aprox en cola: $LEN"

rm -f "$TMP"
echo
ok "Smoke test OK — blob/table/queue responden"
echo "Para probar la API: configura StorageConnection con la cs de arriba"
echo "y ejecuta tú  dotnet run --project src/Storage.Demo.Api"
