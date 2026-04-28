#!/usr/bin/env bash
# 01 — Provisiona Resource Group + App Service Plan Standard S1 +
# Web App .NET 10 Linux + slot staging.
# Slides 4 (tier mínimo Standard para slots) y 5 (crear slot).

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

step "Resource Group: $RG ($LOCATION)"
az group create --name "$RG" --location "$LOCATION" --output none
ok "Resource Group listo"

step "App Service Plan: $PLAN (Standard S1, Linux)"
az appservice plan create \
  --name "$PLAN" \
  --resource-group "$RG" \
  --location "$LOCATION" \
  --is-linux \
  --sku S1 \
  --output none
ok "Plan listo (Standard S1 — habilita slots, slide 4)"

step "Web App: $APP (.NET 10 LTS)"
az webapp create \
  --name "$APP" \
  --resource-group "$RG" \
  --plan "$PLAN" \
  --runtime "DOTNETCORE:10.0" \
  --output none
ok "Web App lista en https://$APP.azurewebsites.net"

step "Slot staging"
az webapp deployment slot create \
  --name "$APP" \
  --resource-group "$RG" \
  --slot staging \
  --output none
ok "Slot staging listo en https://$APP-staging.azurewebsites.net"

echo
echo "Siguiente: ./02-configure-settings.sh"
