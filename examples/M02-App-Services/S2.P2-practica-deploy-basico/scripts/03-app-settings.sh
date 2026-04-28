#!/usr/bin/env bash
# 03 — Configura App Settings sin redesplegar (slide 14).
# El cambio se aplica tras un reinicio automatico (~10-30 s).

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

step "Configurando Saludo__Base y Saludo__MaxLength"
az webapp config appsettings set \
  --name "$APP" --resource-group "$RG" \
  --settings \
    "Saludo__Base=Hola desde Azure App Service," \
    "Saludo__MaxLength=80" \
  --output none

ok "App Settings actualizados (la app reinicia sola)"
echo
echo "Tras unos segundos, comprueba:"
echo "  curl https://$APP.azurewebsites.net/saludo/Pedro"
echo "  -> El 'mensaje' debe empezar con 'Hola desde Azure App Service, Pedro'"
