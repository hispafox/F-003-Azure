#!/usr/bin/env bash
# 05 — Tres metric alerts típicas (slides 12, 27).
# - Errores 5xx > 5 en 5 min        (severity 1)
# - Latencia media > 3 s en 10 min  (severity 2)
# - CPU del plan > 80% en 15 min    (severity 2)

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

: "${ACTION_GROUP:?ACTION_GROUP no definido}"

APP_ID=$(az webapp show --name "$APP" --resource-group "$RG" --query id -o tsv)
PLAN_ID=$(az appservice plan show --name "$PLAN" --resource-group "$RG" --query id -o tsv)
AG_ID=$(az monitor action-group show --name "$ACTION_GROUP" --resource-group "$RG" --query id -o tsv)

step "Alerta: Http5xx > 5 en 5 min"
az monitor metrics alert create \
  --name "${APP}-http5xx" --resource-group "$RG" \
  --scopes "$APP_ID" \
  --condition "total Http5xx > 5" \
  --window-size 5m --evaluation-frequency 1m \
  --severity 1 \
  --action "$AG_ID" \
  --description "Más de 5 errores 5xx en los últimos 5 minutos" \
  --output none

step "Alerta: AverageResponseTime > 3000 ms en 10 min"
az monitor metrics alert create \
  --name "${APP}-latencia-alta" --resource-group "$RG" \
  --scopes "$APP_ID" \
  --condition "avg AverageResponseTime > 3000" \
  --window-size 10m --evaluation-frequency 5m \
  --severity 2 \
  --action "$AG_ID" \
  --description "Latencia media > 3s durante 10 minutos" \
  --output none

step "Alerta: CpuPercentage del plan > 80% en 15 min"
az monitor metrics alert create \
  --name "${PLAN}-cpu-alta" --resource-group "$RG" \
  --scopes "$PLAN_ID" \
  --condition "avg CpuPercentage > 80" \
  --window-size 15m --evaluation-frequency 5m \
  --severity 2 \
  --action "$AG_ID" \
  --description "CPU del plan > 80% durante 15 minutos" \
  --output none

ok "Tres alertas creadas y conectadas a $ACTION_GROUP"
