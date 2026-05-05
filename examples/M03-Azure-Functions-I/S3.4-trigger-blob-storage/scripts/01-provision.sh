#!/usr/bin/env bash
# 01 - RG + Storage (con contenedores uploads/ y procesados/) + Function App.
# El Blob trigger del slide 3 lee de uploads/{name}.csv y escribe el resumen
# en procesados/ — separados para evitar loops infinitos (slide 10).

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

step "Resource Group: $RG"
az group create --name "$RG" --location "$LOCATION" \
  --tags "curso=AZ-204" "modulo=M03" "submodulo=S3.4" \
  --output none

step "Storage Account: $STORAGE"
az storage account create \
  --name "$STORAGE" --resource-group "$RG" --location "$LOCATION" \
  --sku Standard_LRS --kind StorageV2 \
  --output none

# El AzureWebJobsStorage del Function App apunta a este Storage. Por tanto
# los contenedores que crea el Blob trigger (uploads/, procesados/) viven
# AQUI. La connection string es la que usa el binding "AzureWebJobsStorage".
step "Contenedores uploads/ y procesados/"
STORAGE_CONN=$(az storage account show-connection-string \
  --name "$STORAGE" --resource-group "$RG" \
  --query connectionString -o tsv)

az storage container create --name uploads \
  --connection-string "$STORAGE_CONN" --output none
az storage container create --name procesados \
  --connection-string "$STORAGE_CONN" --output none

step "Function App: $FUNC (Consumption Linux, dotnet-isolated 10)"
az functionapp create \
  --name "$FUNC" --resource-group "$RG" \
  --storage-account "$STORAGE" \
  --consumption-plan-location "$LOCATION" \
  --runtime dotnet-isolated --runtime-version 10 \
  --functions-version 4 --os-type Linux \
  --output none

step "App Settings"
# Heredados de S3.2/S3.3 + igual configuracion de CRON y zona horaria
az functionapp config appsettings set \
  --name "$FUNC" --resource-group "$RG" \
  --settings \
    "Productos__MaxPorPagina=100" \
    "Productos__PorPaginaPorDefecto=20" \
    "CleanupCron=0 */1 * * * *" \
    "WEBSITE_TIME_ZONE=Romance Standard Time" \
  --output none

ok "Function App lista en https://$FUNC.azurewebsites.net"
echo
echo "Blob trigger:"
echo "  Sube un CSV a uploads/{name}.csv y se procesara automaticamente"
echo "  El resumen se escribira en procesados/{name}-resumen.json"
echo
echo "Endpoints HTTP:"
echo "  GET  /api/imports                 listar imports realizados"
echo "  GET  /api/imports/{archivo}.csv   ver resumen de un import"
echo
echo "Siguiente: ./02-deploy.sh"
