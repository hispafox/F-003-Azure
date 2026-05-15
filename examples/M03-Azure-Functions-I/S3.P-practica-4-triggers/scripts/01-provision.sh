#!/usr/bin/env bash
# 01 - Práctica 4 triggers: provisiona RG + Storage (con 2 containers) +
# Cosmos DB (DB + container) + Function App con la connection string de
# Cosmos en App Settings.
#
# Los 4 triggers usan:
#   HTTP            -> sin recurso adicional
#   Timer           -> AzureWebJobsStorage (locks; lo trae el runtime)
#   BlobTrigger     -> uploads/ container (mismo Storage)
#   BlobOutput      -> resultados/ container (mismo Storage)
#   CosmosDBTrigger -> tienda/pedidos + lease container que se crea solo

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

step "Resource Group: $RG"
az group create --name "$RG" --location "$LOCATION" \
  --tags "curso=AZ-204" "modulo=M03" "submodulo=S3.P" \
  --output none

step "Storage Account: $STORAGE (runtime + 2 containers para Blob)"
az storage account create \
  --name "$STORAGE" --resource-group "$RG" --location "$LOCATION" \
  --sku Standard_LRS --kind StorageV2 \
  --output none

STORAGE_CONN=$(az storage account show-connection-string \
  --name "$STORAGE" --resource-group "$RG" \
  --query connectionString -o tsv)

step "Containers '$CONTAINER_UPLOADS' y '$CONTAINER_RESULTADOS'"
az storage container create --name "$CONTAINER_UPLOADS" \
  --connection-string "$STORAGE_CONN" --output none
az storage container create --name "$CONTAINER_RESULTADOS" \
  --connection-string "$STORAGE_CONN" --output none

step "Cosmos DB account: $COSMOS (SQL API, serverless)"
az cosmosdb create \
  --name "$COSMOS" --resource-group "$RG" \
  --kind GlobalDocumentDB \
  --capabilities EnableServerless \
  --default-consistency-level Session \
  --locations "regionName=$LOCATION" "failoverPriority=0" "isZoneRedundant=False" \
  --output none

step "Database '$COSMOS_DB' + container '$COSMOS_PEDIDOS' (PK=/clienteId)"
az cosmosdb sql database create \
  --account-name "$COSMOS" --resource-group "$RG" \
  --name "$COSMOS_DB" --output none
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
echo "Recursos:"
echo "  HTTP trigger    -> /api/productos, /api/estado"
echo "  Timer trigger   -> cada minuto (AzureWebJobsStorage holds the lock)"
echo "  Blob trigger    -> uploads/{nombre}.csv -> resultados/{nombre}-resumen.json"
echo "  Cosmos trigger  -> $COSMOS_DB.$COSMOS_PEDIDOS (lease container auto-creado)"
echo
echo "Siguiente: ./02-deploy.sh"
