#!/usr/bin/env bash
# 04 — Despliega "v2" al slot staging (slide 7-8).
# Cambia Practica:Version y Practica:Novedad solo en staging. Producción no
# se toca: sigue con la v1.

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

step "Settings de v2 en el slot staging (no sticky — viajarán con el swap)"
az webapp config appsettings set \
  --name "$APP" --resource-group "$RG" --slot staging \
  --settings \
    Practica__Version=2.0 \
    Practica__Novedad="Slots de despliegue funcionando" \
  --output none

ZIP_PATH="${ZIP_PATH:-./out/app.zip}"
PROJECT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )/.." && pwd )"

if [ ! -f "$PROJECT_DIR/$ZIP_PATH" ]; then
  step "ZIP no existe; haciendo dotnet publish"
  PROJECT="$PROJECT_DIR/src/AppService.Practica.Api/AppService.Practica.Api.csproj"
  PUBLISH_DIR="$PROJECT_DIR/out/publish"
  rm -rf "$PUBLISH_DIR"
  dotnet publish "$PROJECT" -c Release -o "$PUBLISH_DIR" --nologo
  mkdir -p "$(dirname "$PROJECT_DIR/$ZIP_PATH")"
  ( cd "$PUBLISH_DIR" && zip -qr "$PROJECT_DIR/$ZIP_PATH" . )
fi

step "Deploy del ZIP al slot staging"
az webapp deploy \
  --name "$APP" --resource-group "$RG" --slot staging \
  --src-path "$PROJECT_DIR/$ZIP_PATH" --type zip --output none

ok "v2 desplegada en staging"
echo
echo "Verifica los dos slots por separado:"
echo "  curl https://$APP-staging.azurewebsites.net/   → version: '2.0'"
echo "  curl https://$APP.azurewebsites.net/           → version: '1.0' (sin cambios)"
echo
echo "Siguiente: ./05-smoke-test.sh staging"
