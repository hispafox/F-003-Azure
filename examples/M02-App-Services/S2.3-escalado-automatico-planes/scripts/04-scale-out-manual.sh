#!/usr/bin/env bash
# 04 — Scale out manual (slide 4): número de instancias del plan.
# Uso: ./04-scale-out-manual.sh 3

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

COUNT="${1:-}"

if [[ ! "$COUNT" =~ ^[0-9]+$ ]] || (( COUNT < 1 || COUNT > 30 )); then
  echo "Uso: $0 <1-30>"
  exit 1
fi

step "Capacidad actual:"
az appservice plan show \
  --name "$PLAN" --resource-group "$RG" \
  --query "sku.capacity" -o tsv

step "Escalando a $COUNT instancias..."
az appservice plan update \
  --name "$PLAN" --resource-group "$RG" \
  --number-of-workers "$COUNT" \
  --output none
ok "Plan ahora con $COUNT instancia(s)"

echo
echo "Vigila /info para ver cómo cambia instanceId:"
echo "  ./08-watch-instances.sh"
