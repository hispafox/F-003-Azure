#!/usr/bin/env bash
# 01 - RG + Storage (con queue y container) + Cosmos DB + Function App.
#
# Slide 9  - Las connection strings se inyectan como App Settings con
#            los nombres que referencian los atributos:
#               CosmosDbConnection    -> [CosmosDBInput/Output]
#               AzureWebJobsStorage   -> [QueueOutput], [BlobOutput]
# Slide 19 - Storage Queue 'pedidos-pendientes' (la creamos por idempotencia;
#            Functions tambien la crearia al primer encolado).

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

step "Resource Group: $RG"
az group create --name "$RG" --location "$LOCATION" \
  --tags "curso=AZ-204" "modulo=M03" "submodulo=S3.6" \
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
echo "Recursos:"
echo "  Cosmos DB: $COSMOS / $COSMOS_DB / $COSMOS_PEDIDOS"
echo "  Storage:   $STORAGE"
echo "    Queue:   $QUEUE       (output binding de CrearPedidoFunction)"
echo "    Blob:    $BLOB_CONTAINER (output binding de ExportarPedidoFunction)"
echo
echo "Endpoints HTTP:"
echo "  POST /api/pedidos                        -> MultiResponse (HTTP + Cosmos + Queue)"
echo "  GET  /api/pedidos/{clienteId}/{id}       -> CosmosDBInput por id"
echo "  GET  /api/clientes/{clienteId}/pedidos   -> CosmosDBInput por SqlQuery"
echo "  GET  /api/exportar/{clienteId}/{id}      -> CosmosDBInput + BlobOutput (con DateTime)"
echo
echo "Siguiente: ./02-deploy.sh"
