#!/usr/bin/env bash
# 01 - Provision para S4.3: RG + Storage + Service Bus Standard
# (1 cola con dead-lettering habilitado) + Function App.
#
# >>> AVISO COSTE <<<
# Service Bus Standard ~10 EUR/mes fijos. Ejecuta ./04-cleanup.sh cuando
# acabes la demo o se acumula factura por dia.

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

step "Resource Group: $RG"
az group create --name "$RG" --location "$LOCATION" \
  --tags "curso=AZ-204" "modulo=M04" "submodulo=S4.3" \
  --output none

step "Storage Account: $STORAGE (runtime de Functions)"
az storage account create \
  --name "$STORAGE" --resource-group "$RG" --location "$LOCATION" \
  --sku Standard_LRS --kind StorageV2 \
  --output none

step "Service Bus namespace: $SB (Standard tier — ~10 EUR/mes)"
az servicebus namespace create \
  --name "$SB" --resource-group "$RG" --location "$LOCATION" \
  --sku Standard --output none

step "Queue '$SB_QUEUE_PEDIDOS' (con dead-lettering)"
# max-delivery-count 5: tras 5 entregas fallidas, SB mueve el mensaje a
# pedidos-procesar/$deadletterqueue → lo recoge ProcesarDeadLetter.
az servicebus queue create \
  --namespace-name "$SB" --resource-group "$RG" \
  --name "$SB_QUEUE_PEDIDOS" \
  --max-delivery-count 5 \
  --default-message-time-to-live P14D \
  --enable-dead-lettering-on-message-expiration true \
  --output none

SB_CONN=$(az servicebus namespace authorization-rule keys list \
  --namespace-name "$SB" --resource-group "$RG" \
  --name RootManageSharedAccessKey \
  --query primaryConnectionString -o tsv)

step "Function App: $FUNC (Consumption Linux, dotnet-isolated 10)"
az functionapp create \
  --name "$FUNC" --resource-group "$RG" \
  --storage-account "$STORAGE" \
  --consumption-plan-location "$LOCATION" \
  --runtime dotnet-isolated --runtime-version 10 \
  --functions-version 4 --os-type Linux \
  --output none

step "App Settings (ServiceBusConnection wire)"
az functionapp config appsettings set \
  --name "$FUNC" --resource-group "$RG" \
  --settings \
    "ServiceBusConnection=$SB_CONN" \
    "WEBSITE_TIME_ZONE=Romance Standard Time" \
  --output none

ok "Function App lista en https://$FUNC.azurewebsites.net"
echo
echo "Recursos:"
echo "  Service Bus: $SB (Standard)"
echo "    Queue:      $SB_QUEUE_PEDIDOS"
echo "    DLQ:        $SB_QUEUE_PEDIDOS/\$deadletterqueue → ProcesarDeadLetter"
echo
echo "Endpoints:"
echo "  GET /api/estado   contadores (procesados/duplicados/DLQ/poison)"
echo
echo "Siguiente: ./02-deploy.sh"
