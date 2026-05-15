#!/usr/bin/env bash
# 01 - RG + Storage + Function App (Consumption). Sin SB ni Cosmos → ~0€.
# El foco de S4.5 es testing/depuración: el deploy es opcional, lo
# importante es `dotnet test`.

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

step "Resource Group: $RG"
az group create --name "$RG" --location "$LOCATION" \
  --tags "curso=AZ-204" "modulo=M04" "submodulo=S4.5" \
  --output none

step "Storage Account: $STORAGE (runtime + container uploads/ del Blob trigger)"
az storage account create \
  --name "$STORAGE" --resource-group "$RG" --location "$LOCATION" \
  --sku Standard_LRS --kind StorageV2 \
  --output none
STORAGE_CONN=$(az storage account show-connection-string \
  --name "$STORAGE" --resource-group "$RG" --query connectionString -o tsv)
az storage container create --name uploads \
  --connection-string "$STORAGE_CONN" --output none

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
echo "Endpoints (tras el deploy):"
echo "  POST /api/pedidos/descuento   descuento escalonado (slide 7)"
echo "  LimpiezaProgramada            Timer cada 5 min (slide 10)"
echo "  ProcesarCsv                   Blob uploads/*.csv (slide 11)"
echo
echo "Siguiente: ./02-deploy.sh ; luego ./03-smoke-test.sh"
echo "Pero lo importante de S4.5 es: dotnet test (la pirámide)."
