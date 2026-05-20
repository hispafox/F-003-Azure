#!/usr/bin/env bash
# 01 - Descarga el publish profile (slide 7) y lo muestra con la
# password enmascarada. Si quieres subirlo a GitHub Secrets, usa el
# fichero `./publish-profile.xml` que queda en este directorio (ya en
# .gitignore). SOLO LECTURA: no modifica nada en Azure.

set -euo pipefail
source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

step "Descargando publish profile de $APP_NAME"
az webapp deployment list-publishing-profiles \
  --name "$APP_NAME" -g "$RG" --xml > publish-profile.xml \
  || { warn "No se pudo descargar el publish profile."; exit 1; }

ok "publish-profile.xml escrito (no lo subas a git)."

step "Resumen del XML (passwords enmascaradas)"
sed -E 's/userPWD="[^"]*"/userPWD="***MASKED***"/g' publish-profile.xml

echo
echo "Para subirlo como secret de GitHub (slide 8):"
echo "  gh secret set AZURE_WEBAPP_PUBLISH_PROFILE < publish-profile.xml"
echo "Y despues:"
echo "  rm publish-profile.xml   # borralo del local (slide 8)"
