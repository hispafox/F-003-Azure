#!/usr/bin/env bash
# 07 — Restringe el slot staging por IP (slide 17).
# Uso: ./07-protect-staging.sh 203.0.113.50/32
#      ./07-protect-staging.sh open        # quita la restricción

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

ARG="${1:-}"

if [ -z "$ARG" ]; then
  echo "Uso: $0 <ip-cidr>|open"
  exit 1
fi

if [ "$ARG" = "open" ]; then
  step "Eliminando restricciones del slot staging"
  az webapp config access-restriction set \
    --name "$APP" --resource-group "$RG" --slot staging \
    --default-action Allow \
    --output none

  # eliminar la regla "solo-permitidos" si existe
  az webapp config access-restriction remove \
    --name "$APP" --resource-group "$RG" --slot staging \
    --rule-name "solo-permitidos" \
    --output none 2>/dev/null || true
  ok "Slot staging vuelve a estar abierto"
else
  step "Permitir solo $ARG en el slot staging"
  az webapp config access-restriction add \
    --name "$APP" --resource-group "$RG" --slot staging \
    --rule-name "solo-permitidos" \
    --action Allow \
    --ip-address "$ARG" \
    --priority 100 \
    --output none

  az webapp config access-restriction set \
    --name "$APP" --resource-group "$RG" --slot staging \
    --default-action Deny \
    --output none
  ok "Slot staging restringido. El resto recibe 403."
fi
