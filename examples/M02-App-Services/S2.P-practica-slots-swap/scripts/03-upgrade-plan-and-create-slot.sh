#!/usr/bin/env bash
# 03 — Sube el plan B1 -> S1 (slide 4), crea slot staging (slide 5),
# configura sticky settings en ambos slots (slide 6) y warmup ping (slide 9).

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

step "Upgrade del plan a Standard S1 (necesario para slots)"
az appservice plan update \
  --name "$PLAN" --resource-group "$RG" \
  --sku S1 --output none

step "Creando slot 'staging'"
az webapp deployment slot create \
  --name "$APP" --resource-group "$RG" \
  --slot staging --output none

step "Slot setting (sticky) en producción"
az webapp config appsettings set \
  --name "$APP" --resource-group "$RG" \
  --slot-settings \
    Practica__NotaEntorno="Entorno de producción" \
    ASPNETCORE_ENVIRONMENT=Production \
    WEBSITE_SWAP_WARMUP_PING_PATH=/warmup \
    WEBSITE_SWAP_WARMUP_PING_STATUSES=200 \
  --output none

step "Slot setting (sticky) en staging"
az webapp config appsettings set \
  --name "$APP" --resource-group "$RG" --slot staging \
  --slot-settings \
    Practica__NotaEntorno="Entorno de staging — solo QA" \
    ASPNETCORE_ENVIRONMENT=Staging \
    WEBSITE_RUN_FROM_PACKAGE=1 \
  --output none

step "Always On + healthCheckPath en el slot staging"
az webapp config set --name "$APP" -g "$RG" --slot staging \
  --always-on true --output none
az webapp config set --name "$APP" -g "$RG" --slot staging \
  --generic-configurations '{"healthCheckPath": "/health"}' --output none

ok "Plan en S1, slot staging listo, sticky settings aplicadas"
echo
echo "Siguiente: ./04-deploy-v2-to-staging.sh"
