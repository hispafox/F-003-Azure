#!/usr/bin/env bash
# 01 — Provisiona Resource Group + App Service Plan Standard S1 +
# Web App .NET 10 Linux. Standard S1 es el tier mínimo que soporta autoscale
# (slide 5 — "Requiere tier Standard (S1) o superior").

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
ok "Plan listo"

step "Web App: $APP (.NET 10 LTS)"
az webapp create \
  --name "$APP" \
  --resource-group "$RG" \
  --plan "$PLAN" \
  --runtime "DOTNETCORE:10.0" \
  --output none

step "Always On + Health Check + HTTPS Only"
az webapp config set \
  --name "$APP" --resource-group "$RG" \
  --always-on true --output none
az webapp update \
  --name "$APP" --resource-group "$RG" \
  --https-only true --output none
az webapp config set \
  --name "$APP" --resource-group "$RG" \
  --generic-configurations '{"healthCheckPath": "/health"}' \
  --output none

step "Run from Package + warmup paths (slide 29)"
az webapp config appsettings set \
  --name "$APP" --resource-group "$RG" \
  --settings \
    WEBSITE_RUN_FROM_PACKAGE=1 \
    WEBSITE_WARMUP_PATH=/health \
  --output none

ok "Web App lista en https://$APP.azurewebsites.net"
echo
echo "Siguiente: ./02-deploy.sh"
