#!/usr/bin/env bash
# 03 — Configura App Settings sin redesplegar (slide 69, reto 1).
# El "Asistente" hace que el JSON de / muestre tu nombre. Las CURSO_*
# alimentan el endpoint /api/info.

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

: "${ASISTENTE:?ASISTENTE no definido en .env.demo}"

step "Configurando App Settings"
az webapp config appsettings set \
  --name "$APP" --resource-group "$RG" \
  --settings \
    "Asistente=$ASISTENTE" \
    "CURSO_MODULO=1" \
    "CURSO_SESION=Introduccion" \
    "CURSO_FECHA=$(date -u +%Y-%m-%d)" \
  --output none

ok "App Settings actualizados. La app reinicia automaticamente (~30s)."
echo
echo "Tras el reinicio, comprueba:"
echo "  curl https://$APP.azurewebsites.net/"
echo "  -> 'asistente' debe ser '$ASISTENTE'"
echo "  curl https://$APP.azurewebsites.net/api/info"
echo "  -> modulo, sesion, fecha con valores reales"
