#!/usr/bin/env bash
# 02 — Publica y despliega la app.

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

ZIP_PATH="${ZIP_PATH:-./out/app.zip}"
PROJECT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )/.." && pwd )"
PROJECT="$PROJECT_DIR/src/AppService.Demo.Api/AppService.Demo.Api.csproj"
PUBLISH_DIR="$PROJECT_DIR/out/publish"

step "Publish $PROJECT"
rm -rf "$PUBLISH_DIR" "$PROJECT_DIR/$ZIP_PATH" 2>/dev/null || true
dotnet publish "$PROJECT" -c Release -o "$PUBLISH_DIR" --nologo

step "Empaquetando a $ZIP_PATH"
mkdir -p "$(dirname "$PROJECT_DIR/$ZIP_PATH")"
( cd "$PUBLISH_DIR" && zip -qr "$PROJECT_DIR/$ZIP_PATH" . )

step "Deploy a $APP"
az webapp deploy \
  --name "$APP" --resource-group "$RG" \
  --src-path "$PROJECT_DIR/$ZIP_PATH" --type zip \
  --output none

ok "Deploy completado"
echo
echo "La app va a fallar al arrancar HASTA que ./04-configure-keyvault.sh se ejecute"
echo "(el AppOptionsValidator rechaza la KV reference si no se resuelve)."
echo "Ejecuta ahora: ./03-configure-app-settings.sh"
