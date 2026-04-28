#!/usr/bin/env bash
# 01 — RG + plan S1 + Web App .NET 10 + Log Analytics + Application Insights.
# Slide 11 — Application Insights workspace-based (recomendado desde 2024).

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

: "${LAW:?LAW (Log Analytics workspace) no definido}"
: "${AI:?AI (Application Insights) no definido}"

step "Resource Group: $RG"
az group create --name "$RG" --location "$LOCATION" --output none

step "Log Analytics workspace: $LAW"
az monitor log-analytics workspace create \
  --workspace-name "$LAW" --resource-group "$RG" --location "$LOCATION" \
  --output none

WORKSPACE_ID=$(az monitor log-analytics workspace show \
  --workspace-name "$LAW" --resource-group "$RG" --query id -o tsv)

step "Application Insights (workspace-based): $AI"
az monitor app-insights component create \
  --app "$AI" --resource-group "$RG" --location "$LOCATION" \
  --workspace "$WORKSPACE_ID" \
  --output none

step "App Service Plan + Web App"
az appservice plan create \
  --name "$PLAN" --resource-group "$RG" --location "$LOCATION" \
  --is-linux --sku S1 --output none

az webapp create \
  --name "$APP" --resource-group "$RG" --plan "$PLAN" \
  --runtime "DOTNETCORE:10.0" --output none

step "Always On + HTTPS Only + healthCheckPath=/health"
az webapp config set --name "$APP" -g "$RG" --always-on true --output none
az webapp update --name "$APP" -g "$RG" --https-only true --output none
az webapp config set --name "$APP" -g "$RG" \
  --generic-configurations '{"healthCheckPath": "/health"}' --output none

ok "Recursos provisionados"
echo
echo "Siguiente: ./02-deploy.sh"
