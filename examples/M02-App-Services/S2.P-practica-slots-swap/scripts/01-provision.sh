#!/usr/bin/env bash
# 01 — RG + plan B1 + Web App.
# Empezamos en B1 (sin slots) para que la práctica EMPIECE como una app
# normal y veas el momento de "subir a S1 cuando necesitas slots".

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

step "Resource Group: $RG"
az group create --name "$RG" --location "$LOCATION" --output none

step "App Service Plan B1 Linux"
az appservice plan create \
  --name "$PLAN" --resource-group "$RG" --location "$LOCATION" \
  --is-linux --sku B1 --output none

step "Web App .NET 10"
az webapp create \
  --name "$APP" --resource-group "$RG" --plan "$PLAN" \
  --runtime "DOTNETCORE:10.0" --output none

step "Always On + HTTPS Only + healthCheckPath=/health"
az webapp config set --name "$APP" -g "$RG" --always-on true --output none
az webapp update --name "$APP" -g "$RG" --https-only true --output none
az webapp config set --name "$APP" -g "$RG" \
  --generic-configurations '{"healthCheckPath": "/health"}' --output none

ok "Web App lista (en plan B1, sin slots todavía)"
echo
echo "Siguiente: ./02-deploy-as-v1.sh"
