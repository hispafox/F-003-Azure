#!/usr/bin/env bash
# 01 — Resource Group + App Service Plan F1 + Web App.
# Slides 14, 22, 23 — los tres recursos básicos.

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

step "Resource Group: $RG"
az group create --name "$RG" --location "$LOCATION" \
  --tags "curso=AZ-204" "sesion=M01" "owner=${ASISTENTE:-curso}" \
  --output none

step "App Service Plan: $PLAN (F1, Linux)"
az appservice plan create \
  --name "$PLAN" --resource-group "$RG" --location "$LOCATION" \
  --is-linux --sku F1 --output none

step "Web App: $APP (.NET 10 LTS)"
az webapp create \
  --name "$APP" --resource-group "$RG" --plan "$PLAN" \
  --runtime "DOTNETCORE:10.0" --output none

step "Health check path = /health"
az webapp config set --name "$APP" -g "$RG" \
  --generic-configurations '{"healthCheckPath": "/health"}' --output none

ok "Recursos provisionados. Coste real: 0 EUR."
echo
echo "URL publica (todavia sin codigo): https://$APP.azurewebsites.net"
echo
echo "Siguiente: ./02-deploy.sh"
