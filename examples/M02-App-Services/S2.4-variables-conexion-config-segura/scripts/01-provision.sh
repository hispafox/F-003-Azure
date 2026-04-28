#!/usr/bin/env bash
# 01 — RG + plan S1 + Web App .NET 10 + Key Vault con RBAC.
# Slide 25 — el KV se crea con --enable-rbac-authorization para usar
# Azure roles (no las legacy access policies).

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

: "${KV:?KV no definido en .env.demo}"

step "Resource Group: $RG"
az group create --name "$RG" --location "$LOCATION" --output none

step "App Service Plan: $PLAN (Standard S1, Linux)"
az appservice plan create \
  --name "$PLAN" --resource-group "$RG" --location "$LOCATION" \
  --is-linux --sku S1 --output none

step "Web App: $APP (.NET 10 LTS)"
az webapp create \
  --name "$APP" --resource-group "$RG" --plan "$PLAN" \
  --runtime "DOTNETCORE:10.0" --output none

step "Key Vault con RBAC: $KV"
az keyvault create \
  --name "$KV" --resource-group "$RG" --location "$LOCATION" \
  --enable-rbac-authorization true \
  --output none

step "Configuración base de la web app (Always On + HTTPS Only + /health)"
az webapp config set --name "$APP" -g "$RG" --always-on true --output none
az webapp update --name "$APP" -g "$RG" --https-only true --output none
az webapp config set --name "$APP" -g "$RG" \
  --generic-configurations '{"healthCheckPath": "/health"}' --output none

ok "Recursos provisionados"
echo
echo "Siguiente: ./02-deploy.sh"
