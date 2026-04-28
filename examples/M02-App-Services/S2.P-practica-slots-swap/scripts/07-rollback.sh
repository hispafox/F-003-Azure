#!/usr/bin/env bash
# 07 — Rollback = swap inverso (slide 12). El slot anterior aún tiene la
# versión vieja, así que un nuevo swap restaura producción en segundos.

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

confirm "Rollback (swap inverso staging <-> production)?"

step "Swap inverso"
az webapp deployment slot swap \
  --name "$APP" --resource-group "$RG" \
  --slot staging --target-slot production \
  --output none

ok "Rollback completado"
echo
step "Producción ahora:"
curl -s "https://${APP}.azurewebsites.net/" | grep -oE '"version":"[^"]*"' || true
echo
echo "Si el rollback es DEFINITIVO (la v2 estaba mal), borra el slot:"
echo "  az webapp deployment slot delete --name $APP -g $RG --slot staging"
