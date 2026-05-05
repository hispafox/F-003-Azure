#!/usr/bin/env bash
# 01 - RG + Storage + Function App Consumption Linux dotnet-isolated 10.

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

step "Resource Group: $RG"
az group create --name "$RG" --location "$LOCATION" \
  --tags "curso=AZ-204" "modulo=M03" "submodulo=S3.2" \
  --output none

step "Storage Account: $STORAGE"
az storage account create \
  --name "$STORAGE" --resource-group "$RG" --location "$LOCATION" \
  --sku Standard_LRS --kind StorageV2 \
  --output none

step "Function App: $FUNC (Consumption Linux, dotnet-isolated 10)"
# Si --runtime-version 10 falla en tu region, cambia a 8 (ver slide 27 de S3.1)
az functionapp create \
  --name "$FUNC" --resource-group "$RG" \
  --storage-account "$STORAGE" \
  --consumption-plan-location "$LOCATION" \
  --runtime dotnet-isolated --runtime-version 10 \
  --functions-version 4 --os-type Linux \
  --output none

step "App Settings (slide 13: Productos:* viajan como App Settings)"
az functionapp config appsettings set \
  --name "$FUNC" --resource-group "$RG" \
  --settings \
    "Productos__MaxPorPagina=100" \
    "Productos__PorPaginaPorDefecto=20" \
  --output none

ok "Function App lista en https://$FUNC.azurewebsites.net"
echo
echo "Endpoints (tras el deploy):"
echo "  GET  /api/ping                       (Anonymous)"
echo "  GET  /api/productos[?nombre=...]     (Function key)"
echo "  GET  /api/productos/p-001"
echo "  POST /api/productos                  (body JSON)"
echo
echo "Siguiente: ./02-deploy.sh"
