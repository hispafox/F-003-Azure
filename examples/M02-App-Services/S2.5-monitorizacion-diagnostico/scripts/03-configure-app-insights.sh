#!/usr/bin/env bash
# 03 — Configura APPLICATIONINSIGHTS_CONNECTION_STRING en App Service.
# El Program.cs activa OpenTelemetry + UseAzureMonitor solo si esta variable
# está presente, así que esto "enchufa" toda la telemetría.

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

: "${AI:?AI no definido}"

step "Obteniendo connection string de $AI"
CONN=$(az monitor app-insights component show \
  --app "$AI" --resource-group "$RG" \
  --query connectionString -o tsv)

if [ -z "$CONN" ]; then
  echo "[X] No pude leer connectionString de $AI"
  exit 1
fi

step "Configurando App Settings de $APP"
az webapp config appsettings set \
  --name "$APP" --resource-group "$RG" \
  --settings "APPLICATIONINSIGHTS_CONNECTION_STRING=$CONN" \
  --output none

# El Profiler y Snapshot Debugger se habilitan via App Settings clásicas.
# Slide 29 — opcional pero útil para diagnóstico avanzado.
az webapp config appsettings set \
  --name "$APP" --resource-group "$RG" \
  --settings \
    APPINSIGHTS_PROFILERFEATURE_VERSION="1.0.0" \
    APPINSIGHTS_SNAPSHOTFEATURE_VERSION="1.0.0" \
    DiagnosticServices_EXTENSION_VERSION="~3" \
  --output none

ok "App Insights conectado. La app reinicia y empieza a emitir telemetría."
echo
echo "Verifica en Portal -> $AI -> Live Metrics:"
echo "  https://portal.azure.com/#@/resource/subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RG/providers/microsoft.insights/components/$AI/quickPulse"
