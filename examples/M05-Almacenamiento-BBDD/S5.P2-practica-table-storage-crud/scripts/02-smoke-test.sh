#!/usr/bin/env bash
# 02 - Smoke test del Table Storage con `az storage entity` (sin lanzar
# la app — eso lo hace el alumno con `dotnet run`). CRUD round-trip
# sobre una entity de prueba (slide 13).

set -euo pipefail
source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

CONN="$(conn)"
TS=$(date -u +%s)
RK="smoke-$TS"

step "[Create] insertar entity de prueba ($RK)"
az storage entity insert --table-name productos --connection-string "$CONN" \
  --entity PartitionKey=smoketest RowKey="$RK" \
  nombre="Test" precio=1.0 stock=1 --output none

step "[Read] leer la entity"
N=$(az storage entity show --table-name productos --connection-string "$CONN" \
  --partition-key smoketest --row-key "$RK" --query nombre -o tsv)
echo "  nombre leido: $N"

step "[Query] filtro OData PartitionKey eq 'electronica'"
C=$(az storage entity query --table-name productos --connection-string "$CONN" \
  --filter "PartitionKey eq 'electronica'" --query "length(items)" -o tsv)
echo "  entities en 'electronica': $C"

step "[Delete] borrar la entity de prueba"
az storage entity delete --table-name productos --connection-string "$CONN" \
  --partition-key smoketest --row-key "$RK" --output none

echo
ok "Smoke test OK — CRUD de Table Storage responde"
echo "Para probar la API: configura Storage:ConnectionString y"
echo "ejecuta tú  dotnet run --project src/Tables.Demo.Api"
