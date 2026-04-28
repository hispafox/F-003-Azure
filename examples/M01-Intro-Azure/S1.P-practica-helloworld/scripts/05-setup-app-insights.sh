#!/usr/bin/env bash
# 05 — Application Insights workspace-based (slides 55-58, opcional).
# Crea Log Analytics + AI + conecta la web app via App Setting. Es
# instrumentación auto-attach (no requiere tocar el código).

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

: "${LAW:?LAW no definido en .env.demo}"
: "${APPI:?APPI no definido en .env.demo}"

step "Log Analytics workspace: $LAW"
az monitor log-analytics workspace create \
  --resource-group "$RG" --workspace-name "$LAW" --location "$LOCATION" \
  --output none

WORKSPACE_ID=$(az monitor log-analytics workspace show \
  --workspace-name "$LAW" --resource-group "$RG" --query id -o tsv)

step "Application Insights (workspace-based): $APPI"
az monitor app-insights component create \
  --app "$APPI" --resource-group "$RG" --location "$LOCATION" \
  --workspace "$WORKSPACE_ID" --application-type web \
  --output none

CONN=$(az monitor app-insights component show \
  --app "$APPI" --resource-group "$RG" --query connectionString -o tsv)

step "Conectando la web app al AI"
az webapp config appsettings set \
  --name "$APP" --resource-group "$RG" \
  --settings \
    "APPLICATIONINSIGHTS_CONNECTION_STRING=$CONN" \
    "ApplicationInsightsAgent_EXTENSION_VERSION=~3" \
  --output none

step "Reiniciando la web app para que cargue el agente"
az webapp restart --name "$APP" --resource-group "$RG" --output none

ok "Application Insights conectado"
echo
echo "Telemetria tarda ~2-3 min en aparecer."
echo "Portal -> $APPI -> Live Metrics para ver requests en directo."
