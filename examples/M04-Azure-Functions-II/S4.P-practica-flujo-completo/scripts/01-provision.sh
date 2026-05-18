#!/usr/bin/env bash
# 01 - Flujo completo: RG + Storage (queue "facturas-generadas" + blob
# "facturas/") + Cosmos DB (origen del Change Feed) + Function App.
#
#   CosmosDbConnection  -> [CosmosDBOutput] paso 1, [CosmosDBTrigger] paso 2
#   AzureWebJobsStorage -> [BlobOutput]+[QueueOutput] paso 2, [QueueTrigger] paso 3

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

step "Resource Group: $RG"
az group create --name "$RG" --location "$LOCATION" \
  --tags "curso=AZ-204" "modulo=M04" "submodulo=S4.P" \
  --output none

step "Storage Account: $STORAGE (runtime + queue + blob)"
az storage account create \
  --name "$STORAGE" --resource-group "$RG" --location "$LOCATION" \
  --sku Standard_LRS --kind StorageV2 \
  --output none

STORAGE_CONN=$(az storage account show-connection-string \
  --name "$STORAGE" --resource-group "$RG" \
  --query connectionString -o tsv)

step "Queue '$QUEUE'"
az storage queue create --name "$QUEUE" \
  --connection-string "$STORAGE_CONN" --output none

step "Blob container '$BLOB_CONTAINER'"
az storage container create --name "$BLOB_CONTAINER" \
  --connection-string "$STORAGE_CONN" --output none

step "Cosmos DB account: $COSMOS (SQL API, serverless)"
az cosmosdb create \
  --name "$COSMOS" --resource-group "$RG" \
  --kind GlobalDocumentDB \
  --capabilities EnableServerless \
  --default-consistency-level Session \
  --locations "regionName=$LOCATION" "failoverPriority=0" "isZoneRedundant=False" \
  --output none

step "Cosmos DB database: $COSMOS_DB"
az cosmosdb sql database create \
  --account-name "$COSMOS" --resource-group "$RG" \
  --name "$COSMOS_DB" --output none

step "Container '$COSMOS_PEDIDOS' (PK=/clienteId)"
az cosmosdb sql container create \
  --account-name "$COSMOS" --resource-group "$RG" \
  --database-name "$COSMOS_DB" \
  --name "$COSMOS_PEDIDOS" \
  --partition-key-path "/clienteId" \
  --output none

step "Function App: $FUNC (Consumption Linux, dotnet-isolated 10)"
az functionapp create \
  --name "$FUNC" --resource-group "$RG" \
  --storage-account "$STORAGE" \
  --consumption-plan-location "$LOCATION" \
  --runtime dotnet-isolated --runtime-version 10 \
  --functions-version 4 --os-type Linux \
  --output none

step "App Settings (CosmosDbConnection wire)"
COSMOS_CONN=$(az cosmosdb keys list \
  --name "$COSMOS" --resource-group "$RG" \
  --type connection-strings \
  --query "connectionStrings[0].connectionString" -o tsv)

az functionapp config appsettings set \
  --name "$FUNC" --resource-group "$RG" \
  --settings \
    "CosmosDbConnection=$COSMOS_CONN" \
    "WEBSITE_TIME_ZONE=Romance Standard Time" \
  --output none

ok "Function App lista en https://$FUNC.azurewebsites.net"
echo
echo "Flujo:"
echo "  POST /api/pedidos  ->  Cosmos ($COSMOS_DB/$COSMOS_PEDIDOS)"
echo "       --Change Feed-->  factura a blob '$BLOB_CONTAINER/' + msg a '$QUEUE'"
echo "       --Queue-------->  NotificarFacturaGenerada (log)"
echo "  GET  /api/estado   ->  creados / facturados / notificados"
echo
echo "Siguiente: ./02-deploy.sh ; luego ./03-smoke-test.sh"
