#!/usr/bin/env bash
# 01 - RG + Storage + Function App (Consumption) con Run from Package
# habilitado (slide 4 — despliegue atómico y rollback más fiable).

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

step "Resource Group: $RG"
az group create --name "$RG" --location "$LOCATION" \
  --tags "curso=AZ-204" "modulo=M04" "submodulo=S4.4" \
  --output none

step "Storage Account: $STORAGE"
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

step "App Settings (Run from Package + feature flag inicial APAGADO)"
# Slide 4  — WEBSITE_RUN_FROM_PACKAGE=1: ejecuta desde el zip, despliegue
#            atómico, filesystem read-only.
# Slide 16 — el feature flag arranca en false: deploy seguro, se activa
#            despues de verificar. Apagarlo = rollback sin redeploy.
az functionapp config appsettings set \
  --name "$FUNC" --resource-group "$RG" \
  --settings \
    "WEBSITE_RUN_FROM_PACKAGE=1" \
    "FEATURE_NUEVO_PROCESAMIENTO=false" \
  --output none

ok "Function App lista en https://$FUNC.azurewebsites.net"
echo
echo "Endpoints (tras el deploy):"
echo "  GET  /api/health                 verificacion post-deploy (200/503)"
echo "  GET  /api/version                que build esta vivo + flags"
echo "  GET  /api/v1/productos           contrato v1 {id,nombre,precio}"
echo "  GET  /api/v2/productos           contrato v2 {+moneda,+stock}"
echo "  POST /api/pedidos/procesar       legacy|nuevo segun feature flag"
echo
echo "Siguiente: ./02-deploy.sh ; luego ./05-postdeploy-check.sh"
