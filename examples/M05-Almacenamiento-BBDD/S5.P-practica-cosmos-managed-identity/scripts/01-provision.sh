#!/usr/bin/env bash
# 01 - El flujo de la práctica (slides 4-7): Cosmos serverless +
# db/container + App Service con Managed Identity + rol RBAC de datos en
# Cosmos + App Setting con SOLO la URL (sin key).

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

step "Resource Group: $RG"
az group create --name "$RG" --location "$LOCATION" \
  --tags "curso=AZ-204" "modulo=M05" "submodulo=S5.P" --output none

step "Paso 1 — Cosmos DB serverless + db 'tienda' + container 'productos' (/categoria)"
az cosmosdb create \
  --name "$COSMOS_ACCOUNT" --resource-group "$RG" \
  --locations regionName="$LOCATION" failoverPriority=0 isZoneRedundant=false \
  --capabilities EnableServerless --default-consistency-level Session --output none
az cosmosdb sql database create \
  --account-name "$COSMOS_ACCOUNT" --resource-group "$RG" --name tienda --output none
az cosmosdb sql container create \
  --account-name "$COSMOS_ACCOUNT" --resource-group "$RG" \
  --database-name tienda --name productos \
  --partition-key-path "/categoria" --output none

step "Paso 2 — App Service F1 + Managed Identity system-assigned"
az appservice plan create \
  --name "plan-$APP_NAME" --resource-group "$RG" --sku F1 --is-linux --output none
az webapp create \
  --name "$APP_NAME" --resource-group "$RG" \
  --plan "plan-$APP_NAME" --runtime "DOTNETCORE:10.0" --https-only true --output none
az webapp identity assign --name "$APP_NAME" --resource-group "$RG" --output none
PRINCIPAL_ID=$(az webapp identity show \
  --name "$APP_NAME" --resource-group "$RG" --query principalId -o tsv)
echo "  principalId: $PRINCIPAL_ID"

step "Paso 3 — RBAC: 'Cosmos DB Built-in Data Contributor' a la MI (slide 6)"
az cosmosdb sql role assignment create \
  --account-name "$COSMOS_ACCOUNT" --resource-group "$RG" \
  --role-definition-name "Cosmos DB Built-in Data Contributor" \
  --principal-id "$PRINCIPAL_ID" --scope "/" --output none

step "Paso 4 — App Setting: SOLO la URL del endpoint (sin key, slide 7)"
COSMOS_ENDPOINT=$(az cosmosdb show \
  --name "$COSMOS_ACCOUNT" --resource-group "$RG" \
  --query documentEndpoint -o tsv)
az webapp config appsettings set \
  --name "$APP_NAME" --resource-group "$RG" \
  --settings "CosmosEndpoint=$COSMOS_ENDPOINT" --output none

ok "Listo: $APP_NAME → $COSMOS_ACCOUNT con Managed Identity (cero keys)"
echo
echo "RBAC tarda 5-10 min en propagar (slide 20). Despliega la app tú."
echo "Endpoint configurado: $COSMOS_ENDPOINT"
echo "Siguiente: ./02-smoke-test.sh (auditoría zero-secrets, slide 13)"
