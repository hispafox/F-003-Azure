#!/usr/bin/env bash
# 06 — Swap staging -> production (slide 10).
# Antes del swap, el warmup ping configurado en 03 se dispara automáticamente.

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

step "Versión actual en producción:"
curl -s "https://${APP}.azurewebsites.net/" | grep -oE '"version":"[^"]*"' || true
echo
step "Versión actual en staging:"
curl -s "https://${APP}-staging.azurewebsites.net/" | grep -oE '"version":"[^"]*"' || true
echo

confirm "Hacer swap staging -> production?"

step "Ejecutando swap (warmup ping a /warmup en staging)"
az webapp deployment slot swap \
  --name "$APP" --resource-group "$RG" \
  --slot staging --target-slot production \
  --output none

ok "Swap completado"
echo
step "Producción ahora:"
curl -s "https://${APP}.azurewebsites.net/" | grep -oE '"version":"[^"]*"|"nota_entorno":"[^"]*"' || true
echo
step "Staging ahora:"
curl -s "https://${APP}-staging.azurewebsites.net/" | grep -oE '"version":"[^"]*"|"nota_entorno":"[^"]*"' || true
echo
echo "Si todo está bien, ejecuta:  ./05-smoke-test.sh production 2.0"
echo "Si algo falla, rollback:     ./07-rollback.sh"
