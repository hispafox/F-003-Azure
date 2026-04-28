#!/usr/bin/env bash
# 09 — Limpia el slot y baja el plan a B1 (no borra el RG entero).
# Slide 13 — coste mínimo si tardas pocas horas en S1.

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

confirm "Eliminar slot staging y bajar plan a B1?"

step "Borrando slot staging"
az webapp deployment slot delete \
  --name "$APP" --resource-group "$RG" --slot staging \
  --output none 2>/dev/null || echo "  (slot ya no existía)"

step "Bajando plan a B1"
az appservice plan update \
  --name "$PLAN" --resource-group "$RG" --sku B1 --output none

ok "Slot eliminado, plan a B1"
echo
echo "Para borrar TODO el RG (cleanup total), ejecuta:"
echo "  az group delete --name $RG --yes --no-wait"
