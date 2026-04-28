#!/usr/bin/env bash
# 04 — Swap directo staging -> production.
# Slides 10, 11 — el swap intercambia los punteros del balanceador,
# no copia archivos. Las sticky settings se quedan en su slot.

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

step "Versión actual en producción:"
curl -s "https://$APP.azurewebsites.net/version" | tr ',' '\n' | sed 's/^/  /' || true
echo
step "Versión actual en staging:"
curl -s "https://$APP-staging.azurewebsites.net/version" | tr ',' '\n' | sed 's/^/  /' || true
echo

confirm "¿Hacer swap staging -> production?"

step "Swap en curso..."
az webapp deployment slot swap \
  --name "$APP" --resource-group "$RG" \
  --slot staging --target-slot production \
  --output none
ok "Swap completado"

echo
step "Versión nueva en producción:"
curl -s "https://$APP.azurewebsites.net/version" | tr ',' '\n' | sed 's/^/  /' || true
echo
echo "Para rollback: ./04-swap.sh otra vez (la versión anterior está ahora en staging)."
