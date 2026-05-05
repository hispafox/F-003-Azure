#!/usr/bin/env bash
# 01 - RG + Storage + Function App Consumption Linux dotnet-isolated 10
# + App Settings con CronExpression configurable y WEBSITE_TIME_ZONE.

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

step "Resource Group: $RG"
az group create --name "$RG" --location "$LOCATION" \
  --tags "curso=AZ-204" "modulo=M03" "submodulo=S3.3" \
  --output none

step "Storage Account: $STORAGE"
az storage account create \
  --name "$STORAGE" --resource-group "$RG" --location "$LOCATION" \
  --sku Standard_LRS --kind StorageV2 \
  --output none

step "Function App: $FUNC (Consumption Linux, dotnet-isolated 10)"
# Si --runtime-version 10 falla en tu region, cambia a 8.
az functionapp create \
  --name "$FUNC" --resource-group "$RG" \
  --storage-account "$STORAGE" \
  --consumption-plan-location "$LOCATION" \
  --runtime dotnet-isolated --runtime-version 10 \
  --functions-version 4 --os-type Linux \
  --output none

step "App Settings"
# - Productos:* alimenta los HTTP triggers heredados de S3.2.
# - CleanupCron es el CRON dinamico del timer (slide 5 - "%CronExpression%").
#   "0 */1 * * * *" = cada minuto. Cambialo en runtime sin redeploy.
# - WEBSITE_TIME_ZONE configura la zona horaria (slide 6). Sin esto, los
#   CRON se interpretan en UTC y "diariamente a las 9" = "10 Madrid invierno".
#   En Linux Consumption Plan no siempre se respeta, pero es lo correcto.
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
echo "Endpoints HTTP (tras el deploy):"
echo "  GET  /api/ping                       (Anonymous)"
echo "  GET  /api/productos                  (Function key)"
echo "  GET  /api/informes                   (Function key, ver resultado del timer)"
echo
echo "Timers programados (corren automaticamente):"
echo "  CleanupCadaMinuto                    cada minuto (CleanupCron)"
echo "  InformeDiario                        diario 06:00 hora Madrid"
echo
echo "Siguiente: ./02-deploy.sh"
