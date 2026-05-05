#!/usr/bin/env bash
# 01 - Provisiona los recursos minimos para una Function App.
# Slide 6 - plan Consumption (gratis hasta 1M ejecuciones / 400K GB-s al mes).
# Slide 14 - Functions necesita un Storage Account para metadatos internos.

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

step "Resource Group: $RG"
az group create --name "$RG" --location "$LOCATION" \
  --tags "curso=AZ-204" "modulo=M03" "submodulo=S3.1" \
  --output none

step "Storage Account: $STORAGE (Standard_LRS, requerido por Functions)"
az storage account create \
  --name "$STORAGE" \
  --resource-group "$RG" \
  --location "$LOCATION" \
  --sku Standard_LRS \
  --kind StorageV2 \
  --output none

step "Function App: $FUNC (Consumption, Linux, dotnet-isolated 10)"
# Si --runtime-version 10 falla en tu region, cambia a 8 (LTS estable):
#   --runtime-version 8
az functionapp create \
  --name "$FUNC" \
  --resource-group "$RG" \
  --storage-account "$STORAGE" \
  --consumption-plan-location "$LOCATION" \
  --runtime dotnet-isolated \
  --runtime-version 10 \
  --functions-version 4 \
  --os-type Linux \
  --output none

ok "Function App provisionada"
echo
echo "URL base: https://$FUNC.azurewebsites.net"
echo "Endpoint tras el deploy: https://$FUNC.azurewebsites.net/api/hello?name=Pedro"
echo
echo "Siguiente: ./02-deploy.sh"
