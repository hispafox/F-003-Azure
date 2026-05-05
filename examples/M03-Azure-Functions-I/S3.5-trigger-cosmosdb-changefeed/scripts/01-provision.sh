#!/usr/bin/env bash
# 01 - RG + Storage + Cosmos DB (cuenta serverless + DB + contenedores) +
# Function App, y wire de CosmosDbConnection en App Settings.
#
# Slide 5  - Lease containers se crean automaticamente en runtime
#            (CreateLeaseContainerIfNotExists = true), por eso aqui solo
#            provisionamos pedidos/ y resumenes-clientes/.
# Slide 11 - El paralelismo del trigger esta limitado por el numero de
#            particiones fisicas. Como usamos serverless, Cosmos asigna
#            particiones automaticamente segun el volumen.
# Slide 22 - Serverless: pagas por RU consumida (ideal para demo). Para
#            produccion considera provisioned o autoscale.

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

step "Resource Group: $RG"
az group create --name "$RG" --location "$LOCATION" \
  --tags "curso=AZ-204" "modulo=M03" "submodulo=S3.5" \
  --output none

step "Storage Account: $STORAGE (runtime de Functions)"
az storage account create \
  --name "$STORAGE" --resource-group "$RG" --location "$LOCATION" \
  --sku Standard_LRS --kind StorageV2 \
  --output none

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

step "Container '$COSMOS_RESUMENES' (PK=/clienteId, output binding)"
# Slide 9 - El materializador escribe aqui via [CosmosDBOutput].
az cosmosdb sql container create \
  --account-name "$COSMOS" --resource-group "$RG" \
  --database-name "$COSMOS_DB" \
  --name "$COSMOS_RESUMENES" \
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
# Slide 13 - El connection string se obtiene del Cosmos provisionado y
# se inyecta como App Setting "CosmosDbConnection" (nombre referenciado
# en los atributos [CosmosDBTrigger] y [CosmosDBOutput]).
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
echo "Cosmos DB:"
echo "  Account:  $COSMOS"
echo "  DB:       $COSMOS_DB"
echo "  Pedidos:  $COSMOS_PEDIDOS  (origen del Change Feed)"
echo "  Resumenes:$COSMOS_RESUMENES (vista materializada via output binding)"
echo "  Leases:   leases-notificaciones, leases-resumenes (los crea runtime)"
echo
echo "Endpoints HTTP de inspeccion:"
echo "  GET  /api/notificaciones[?clienteId=...]"
echo "  GET  /api/resumenes"
echo "  GET  /api/resumenes/{clienteId}"
echo
echo "Siguiente: ./02-deploy.sh"
