#!/usr/bin/env bash
# 01 — RG + plan F1 (gratis) + Web App.
# Slides 7, 8, 9 — el "hardware" (plan) + la "máquina" (web app).

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

step "Resource Group: $RG"
az group create --name "$RG" --location "$LOCATION" --output none

step "App Service Plan F1 (gratis, Linux)"
az appservice plan create \
  --name "$PLAN" --resource-group "$RG" --location "$LOCATION" \
  --is-linux --sku F1 --output none

step "Web App: $APP (.NET 10 LTS)"
az webapp create \
  --name "$APP" --resource-group "$RG" --plan "$PLAN" \
  --runtime "DOTNETCORE:10.0" --output none

# F1 no permite Always On (slide 8: "la app se duerme tras 20 min sin trafico").
# El healthCheckPath sí se configura (App Service lo ignora amablemente en F1).
step "Health check path = /health"
az webapp config set --name "$APP" -g "$RG" \
  --generic-configurations '{"healthCheckPath": "/health"}' --output none

ok "Recursos provisionados. Coste real: 0 EUR."
echo
echo "URL publica (todavia sin codigo): https://$APP.azurewebsites.net"
echo
echo "Siguiente: ./02-deploy.sh"
