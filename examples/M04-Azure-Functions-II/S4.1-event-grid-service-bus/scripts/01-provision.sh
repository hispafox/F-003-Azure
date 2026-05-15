#!/usr/bin/env bash
# 01 - Provision para S4.1: RG + Storage (con container uploads) +
# Service Bus Standard (con 3 queues + 1 topic + 1 subscription) +
# Function App + Event Grid subscription a BlobCreated.
#
# >>> AVISO COSTE <<<
# Service Bus Standard ~10 EUR/mes fijos. Ejecuta ./04-cleanup.sh cuando
# acabes la demo o se acumula factura por dia.

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

step "Resource Group: $RG"
az group create --name "$RG" --location "$LOCATION" \
  --tags "curso=AZ-204" "modulo=M04" "submodulo=S4.1" \
  --output none

step "Storage Account: $STORAGE"
az storage account create \
  --name "$STORAGE" --resource-group "$RG" --location "$LOCATION" \
  --sku Standard_LRS --kind StorageV2 \
  --output none

STORAGE_CONN=$(az storage account show-connection-string \
  --name "$STORAGE" --resource-group "$RG" \
  --query connectionString -o tsv)
STORAGE_ID=$(az storage account show \
  --name "$STORAGE" --resource-group "$RG" \
  --query id -o tsv)

step "Container '$CONTAINER_UPLOADS' (origen del Event Grid)"
az storage container create --name "$CONTAINER_UPLOADS" \
  --connection-string "$STORAGE_CONN" --output none

step "Service Bus namespace: $SB (Standard tier — ~10 EUR/mes)"
az servicebus namespace create \
  --name "$SB" --resource-group "$RG" --location "$LOCATION" \
  --sku Standard --output none

step "Queues: $SB_QUEUE_PEDIDOS, $SB_QUEUE_FACTURAS, $SB_QUEUE_IMPORTS"
for q in "$SB_QUEUE_PEDIDOS" "$SB_QUEUE_FACTURAS" "$SB_QUEUE_IMPORTS"; do
  az servicebus queue create \
    --namespace-name "$SB" --resource-group "$RG" \
    --name "$q" \
    --max-delivery-count 5 \
    --default-message-time-to-live P14D \
    --enable-dead-lettering-on-message-expiration true \
    --output none
done

step "Topic: $SB_TOPIC_EVENTOS + subscription $SB_SUB_NOTIFICACIONES"
az servicebus topic create \
  --namespace-name "$SB" --resource-group "$RG" \
  --name "$SB_TOPIC_EVENTOS" --output none

az servicebus topic subscription create \
  --namespace-name "$SB" --resource-group "$RG" \
  --topic-name "$SB_TOPIC_EVENTOS" \
  --name "$SB_SUB_NOTIFICACIONES" \
  --max-delivery-count 5 \
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

# La suscripción de Event Grid se crea DESPUÉS del primer deploy
# (necesita la system key de la función). Lo hace ./02-deploy.sh.

ok "Function App lista en https://$FUNC.azurewebsites.net"
echo
echo "Recursos:"
echo "  Storage:     $STORAGE  (container '$CONTAINER_UPLOADS' → fuente del EG trigger)"
echo "  Service Bus: $SB       (Standard, queues + topic + subscription)"
echo "    Queue pedidos:  $SB_QUEUE_PEDIDOS"
echo "    Queue facturas: $SB_QUEUE_FACTURAS"
echo "    Queue imports:  $SB_QUEUE_IMPORTS"
echo "    Topic eventos:  $SB_TOPIC_EVENTOS"
echo "    Subscription:   $SB_SUB_NOTIFICACIONES"
echo
echo "Siguiente: ./02-deploy.sh (también creará la suscripción de Event Grid)"
