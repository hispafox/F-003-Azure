#!/usr/bin/env bash
# 06 — Availability test (slide 19): App Insights pinguea /health desde varias
# regiones cada 5 min. Si falla, alerta.

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

: "${AI:?AI no definido}"

URL="https://${APP}.azurewebsites.net/health"

step "Creando availability test 'health-${APP}'"
az monitor app-insights web-test create \
  --resource-group "$RG" \
  --app-insights "$AI" \
  --name "health-${APP}" \
  --web-test-kind ping \
  --locations "emea-nl-ams-azr" "us-il-ch1-azr" "apac-sg-sin-azr" \
  --frequency 300 \
  --timeout 30 \
  --url "$URL" \
  --output none

ok "Availability test creado. Empezará a pinguear en ~5 min."
echo
echo "Verás los resultados en Portal -> $AI -> Availability"
