#!/usr/bin/env bash
# 04 — Action Group (slide 26): cómo se notifica cuando salta una alerta.
# Aquí lo dejamos en email; en una empresa real sumarías SMS, voice, webhooks.

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

: "${ACTION_GROUP:?ACTION_GROUP no definido}"
: "${NOTIFY_EMAIL:?NOTIFY_EMAIL no definido}"

step "Creando Action Group $ACTION_GROUP"
az monitor action-group create \
  --name "$ACTION_GROUP" \
  --resource-group "$RG" \
  --short-name "demo-s25" \
  --action email "primary" "$NOTIFY_EMAIL" \
  --output none

ok "Action Group creado. $NOTIFY_EMAIL recibirá las alertas."
