#!/usr/bin/env bash
# 01 - Provision mínimo para Durable Functions (slide 14):
# RG + Storage (Durable persiste historial + colas internas aquí) +
# Function App. SIN Service Bus ni Cosmos — coste ~0.

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

step "Resource Group: $RG"
az group create --name "$RG" --location "$LOCATION" \
  --tags "curso=AZ-204" "modulo=M04" "submodulo=S4.P2" \
  --output none

step "Storage Account: $STORAGE (historial + control-queues de Durable)"
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
echo "Endpoints:"
echo "  POST /api/saludos                inicia el orchestrator (array de nombres)"
echo "  GET  /api/saludos/{instanceId}   consulta estado"
echo
echo "Siguiente: ./02-deploy.sh"
