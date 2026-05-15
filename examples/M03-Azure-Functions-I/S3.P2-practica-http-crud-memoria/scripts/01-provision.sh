#!/usr/bin/env bash
# 01 - Provision minimo (slide 10): RG + Storage (lo pide el runtime) +
# Function App. NO hay Cosmos ni containers de blob — esta practica es
# HTTP-only con CRUD en memoria.

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

step "Resource Group: $RG"
az group create --name "$RG" --location "$LOCATION" \
  --tags "curso=AZ-204" "modulo=M03" "submodulo=S3.P2" \
  --output none

step "Storage Account: $STORAGE (obligatorio para AzureWebJobsStorage)"
az storage account create \
  --name "$STORAGE" --resource-group "$RG" --location "$LOCATION" \
  --sku Standard_LRS --kind StorageV2 \
  --output none

step "Function App: $FUNC (Consumption Linux, dotnet-isolated 10)"
az functionapp create \
  --name "$FUNC" --resource-group "$RG" \
  --storage-account "$STORAGE" \
  --consumption-plan-location "$LOCATION" \
  --runtime dotnet-isolated --runtime-version 10 \
  --functions-version 4 --os-type Linux \
  --output none

ok "Function App lista en https://$FUNC.azurewebsites.net"
echo
echo "5 endpoints CRUD:"
echo "  GET    /api/productos"
echo "  GET    /api/productos/{id}"
echo "  POST   /api/productos"
echo "  PUT    /api/productos/{id}"
echo "  DELETE /api/productos/{id}"
echo
echo "Siguiente: ./02-deploy.sh"
