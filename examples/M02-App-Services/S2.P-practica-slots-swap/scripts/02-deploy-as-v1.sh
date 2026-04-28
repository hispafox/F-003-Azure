#!/usr/bin/env bash
# 02 — Configura "v1" (Practica:Version=1.0) y despliega a producción.
# Las settings de "código" (Version, Novedad) van como App Settings normales.
# El sticky lo añadimos en el siguiente paso, cuando ya hay slot.

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

step "Configurando App Settings de v1 en producción"
az webapp config appsettings set \
  --name "$APP" --resource-group "$RG" \
  --settings \
    Practica__Version=1.0 \
    Practica__Novedad="Hello World" \
    WEBSITE_RUN_FROM_PACKAGE=1 \
  --output none

ZIP_PATH="${ZIP_PATH:-./out/app.zip}"
PROJECT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )/.." && pwd )"
PROJECT="$PROJECT_DIR/src/AppService.Practica.Api/AppService.Practica.Api.csproj"
PUBLISH_DIR="$PROJECT_DIR/out/publish"

step "dotnet publish (Release)"
rm -rf "$PUBLISH_DIR" "$PROJECT_DIR/$ZIP_PATH" 2>/dev/null || true
dotnet publish "$PROJECT" -c Release -o "$PUBLISH_DIR" --nologo

step "Empaquetando a $ZIP_PATH"
mkdir -p "$(dirname "$PROJECT_DIR/$ZIP_PATH")"
( cd "$PUBLISH_DIR" && zip -qr "$PROJECT_DIR/$ZIP_PATH" . )

step "Deploy a $APP (slot principal = production)"
az webapp deploy \
  --name "$APP" --resource-group "$RG" \
  --src-path "$PROJECT_DIR/$ZIP_PATH" --type zip --output none

ok "v1 desplegada"
echo
echo "Verifica:"
echo "  curl https://$APP.azurewebsites.net/"
echo "  → version: '1.0', novedad: 'Hello World'"
echo
echo "Siguiente: ./03-upgrade-plan-and-create-slot.sh"
