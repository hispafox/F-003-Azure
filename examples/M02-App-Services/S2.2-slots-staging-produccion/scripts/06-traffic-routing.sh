#!/usr/bin/env bash
# 06 — Traffic routing / canary (slide 14).
# Uso: ./06-traffic-routing.sh 10   # 10% de tráfico al slot staging
#      ./06-traffic-routing.sh 0    # quitar el routing (todo a producción)

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

PERCENT="${1:-}"

if [[ ! "$PERCENT" =~ ^[0-9]+$ ]] || (( PERCENT < 0 || PERCENT > 100 )); then
  echo "[X] Uso: $0 <0-100>"
  exit 1
fi

if (( PERCENT == 0 )); then
  step "Eliminando traffic routing (100% al slot principal)"
  az webapp traffic-routing clear \
    --name "$APP" --resource-group "$RG" \
    --output none
  ok "Routing limpio"
else
  step "Enviando $PERCENT% del tráfico a staging"
  az webapp traffic-routing set \
    --name "$APP" --resource-group "$RG" \
    --distribution "staging=$PERCENT" \
    --output none
  ok "Routing configurado: $PERCENT% staging / $((100 - PERCENT))% production"

  echo
  echo "Para forzar siempre el slot principal: añade ?x-ms-routing-name=self a la URL."
fi
