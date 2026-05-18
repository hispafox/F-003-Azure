#!/usr/bin/env bash
# 01 - Cuenta Cosmos DB SERVERLESS (slide 7/21: pago por RU, ≈ 0 € sin
# uso) + database + container "pedidos" con partition key /clienteId
# (slide 6). Consistencia Session por defecto (slide 11).

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

step "Resource Group: $RG"
az group create --name "$RG" --location "$LOCATION" \
  --tags "curso=AZ-204" "modulo=M05" "submodulo=S5.3" \
  --output none

step "Cuenta Cosmos DB serverless: $COSMOS_ACCOUNT (slide 7/21)"
az cosmosdb create \
  --name "$COSMOS_ACCOUNT" --resource-group "$RG" \
  --locations regionName="$LOCATION" failoverPriority=0 isZoneRedundant=false \
  --capabilities EnableServerless \
  --default-consistency-level Session \
  --output none

step "Database: $COSMOS_DB"
az cosmosdb sql database create \
  --account-name "$COSMOS_ACCOUNT" --resource-group "$RG" \
  --name "$COSMOS_DB" --output none

step "Container: $COSMOS_CONTAINER (partition key /clienteId, slide 6)"
# --ttl -1 habilitaría TTL sin default (slide 19). Aquí lo dejamos off:
# los pedidos no caducan; el TTL es para sesiones/logs/cache.
az cosmosdb sql container create \
  --account-name "$COSMOS_ACCOUNT" --resource-group "$RG" \
  --database-name "$COSMOS_DB" --name "$COSMOS_CONTAINER" \
  --partition-key-path "/clienteId" \
  --output none

ok "Cosmos serverless listo: $COSMOS_ACCOUNT/$COSMOS_DB/$COSMOS_CONTAINER"
echo
echo "Connection string (con key — NO lo comitees):"
echo "  $(cosmos_conn_string)"
echo
echo "En producción usa Managed Identity (sin key, slide 15 / M05-S5.4):"
echo "  CosmosDbConnection = AccountEndpoint=https://$COSMOS_ACCOUNT.documents.azure.com:443/"
echo
echo "Siguiente: ./02-smoke-test.sh"
