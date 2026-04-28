#!/usr/bin/env bash
# 03 — Scale up (vertical): cambiar el SKU del plan a uno con más recursos.
# Slide 3 — zero-downtime, instantáneo.
# Uso: ./03-scale-up.sh S1
#      ./03-scale-up.sh P1V3
#      ./03-scale-up.sh B1   (bajar de plan también vale)

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

NEW_SKU="${1:-}"

if [ -z "$NEW_SKU" ]; then
  echo "Uso: $0 <SKU>"
  echo "  Ejemplos: B1, S1, S2, S3, P1V3, P2V3, P3V3"
  exit 1
fi

step "SKU actual:"
az appservice plan show \
  --name "$PLAN" --resource-group "$RG" \
  --query "{name:name, sku:sku.name, tier:sku.tier, capacity:sku.capacity}" \
  --output table

confirm "Cambiar SKU a $NEW_SKU?"

step "Actualizando plan a $NEW_SKU..."
az appservice plan update \
  --name "$PLAN" --resource-group "$RG" \
  --sku "$NEW_SKU" \
  --output none
ok "SKU actualizado"

step "SKU nuevo:"
az appservice plan show \
  --name "$PLAN" --resource-group "$RG" \
  --query "{name:name, sku:sku.name, tier:sku.tier, capacity:sku.capacity}" \
  --output table
