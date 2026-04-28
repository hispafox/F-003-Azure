#!/usr/bin/env bash
# 02 — Configura Application settings y SLOT settings (sticky).
# Slides 8 y 9 — settings normales viajan con el código en el swap;
# las marcadas como "slot setting" se quedan en su slot.

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

step "Always On + Health Check + HTTPS only en producción"
az webapp config set \
  --name "$APP" --resource-group "$RG" \
  --always-on true \
  --output none
az webapp update \
  --name "$APP" --resource-group "$RG" \
  --https-only true \
  --output none
az webapp config set \
  --name "$APP" --resource-group "$RG" \
  --generic-configurations '{"healthCheckPath": "/health"}' \
  --output none
ok "Producción: Always On + /health + HTTPS only"

step "Settings normales (viajan con el código tras el swap)"
az webapp config appsettings set \
  --name "$APP" --resource-group "$RG" \
  --settings \
    AppOptions__Greeting="Hola desde producción" \
    AppOptions__AllowedOrigins__0="https://tu-frontend.com" \
  --output none

az webapp config appsettings set \
  --name "$APP" --resource-group "$RG" --slot staging \
  --settings \
    AppOptions__Greeting="Hola desde staging" \
    AppOptions__AllowedOrigins__0="https://staging.tu-frontend.com" \
  --output none
ok "Settings normales configurados"

step "SLOT settings (sticky — no viajan con el swap)"
# Producción
az webapp config appsettings set \
  --name "$APP" --resource-group "$RG" \
  --slot-settings \
    AppOptions__EnvironmentLabel="production" \
    AppOptions__DbConnectionLabel="prod-db" \
    AppOptions__AppInsightsLabel="prod-insights" \
    WEBSITE_RUN_FROM_PACKAGE="1" \
    WEBSITE_SWAP_WARMUP_PING_PATH="/warmup" \
    WEBSITE_SWAP_WARMUP_PING_STATUSES="200" \
  --output none

# Staging
az webapp config appsettings set \
  --name "$APP" --resource-group "$RG" --slot staging \
  --slot-settings \
    AppOptions__EnvironmentLabel="staging" \
    AppOptions__DbConnectionLabel="staging-db" \
    AppOptions__AppInsightsLabel="staging-insights" \
    WEBSITE_RUN_FROM_PACKAGE="1" \
  --output none

step "Always On + /health en el slot staging también"
az webapp config set \
  --name "$APP" --resource-group "$RG" --slot staging \
  --always-on true \
  --output none
az webapp config set \
  --name "$APP" --resource-group "$RG" --slot staging \
  --generic-configurations '{"healthCheckPath": "/health"}' \
  --output none

ok "Sticky settings configuradas"
echo
echo "Tras un swap: code+greeting+allowed-origins viajan, EnvironmentLabel/DbConnection/AppInsights se quedan."
echo "Siguiente: ./03-deploy.sh production  o  ./03-deploy.sh staging"
