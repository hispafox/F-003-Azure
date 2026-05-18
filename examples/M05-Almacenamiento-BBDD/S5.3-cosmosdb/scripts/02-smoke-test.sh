#!/usr/bin/env bash
# 02 - Smoke test: verifica que la cuenta/db/container existen y que la
# partition key es /clienteId. El SQL API de Cosmos no tiene CRUD de
# items por `az` (eso lo hace la API que lanza el alumno con dotnet run).

set -euo pipefail
source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

step "Cuenta: modo serverless + consistencia"
az cosmosdb show --name "$COSMOS_ACCOUNT" --resource-group "$RG" \
  --query "{nombre:name, consistencia:consistencyPolicy.defaultConsistencyLevel, capacidades:capabilities[].name}" \
  -o jsonc

step "Container: partition key e indexación"
PK=$(az cosmosdb sql container show \
  --account-name "$COSMOS_ACCOUNT" --resource-group "$RG" \
  --database-name "$COSMOS_DB" --name "$COSMOS_CONTAINER" \
  --query "resource.partitionKey.paths[0]" -o tsv)
echo "  partition key: $PK"
[ "$PK" = "/clienteId" ] && ok "Partition key correcta (/clienteId, slide 6)" \
  || warn "Partition key inesperada: $PK"

echo
ok "Smoke test OK — cuenta/db/container provisionados"
echo "Para probar la API: pon CosmosDbConnection con la cs de 01-provision"
echo "y ejecuta tú  dotnet run --project src/Cosmos.Demo.Api"
