#!/usr/bin/env bash
# 03 — Application Settings normales (no sticky) + slot settings (sticky).
# Slides 4, 6, 8, 9 — uso de "__" como separador de secciones, sticky por entorno.

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

step "Application Settings — viajan con el código tras un swap"
az webapp config appsettings set \
  --name "$APP" --resource-group "$RG" \
  --settings \
    AppOptions__Greeting="Hola desde Azure" \
    AppOptions__Version=1.0.0 \
    AppOptions__ExternalApiBaseUrl=https://api.github.com \
    AppOptions__RequestTimeoutSeconds=30 \
    "FeatureManagement__NewUI=false" \
  --output none

step "Slot settings — sticky por entorno"
az webapp config appsettings set \
  --name "$APP" --resource-group "$RG" \
  --slot-settings \
    AppOptions__EnvironmentLabel=production \
    AppOptions__DbConnectionLabel=prod-db \
    AppOptions__AppInsightsLabel=prod-insights \
    WEBSITE_RUN_FROM_PACKAGE=1 \
  --output none

ok "App settings configurados (todavía SIN secrets — eso va en 04 y 05)"
echo
echo "Siguiente: ./04-configure-keyvault.sh"
