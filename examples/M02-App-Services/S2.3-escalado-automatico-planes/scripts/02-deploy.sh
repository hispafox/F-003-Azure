#!/usr/bin/env bash
# 02 — Publica la API y la despliega a la web app.

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

ZIP_PATH="${ZIP_PATH:-./out/app.zip}"
PROJECT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )/.." && pwd )"
PROJECT="$PROJECT_DIR/src/AppService.Demo.Api/AppService.Demo.Api.csproj"
PUBLISH_DIR="$PROJECT_DIR/out/publish"

step "Publicando $PROJECT (Release)"
rm -rf "$PUBLISH_DIR" "$PROJECT_DIR/$ZIP_PATH" 2>/dev/null || true
dotnet publish "$PROJECT" -c Release -o "$PUBLISH_DIR" --nologo

step "Empaquetando a $ZIP_PATH"
mkdir -p "$(dirname "$PROJECT_DIR/$ZIP_PATH")"
( cd "$PUBLISH_DIR" && zip -qr "$PROJECT_DIR/$ZIP_PATH" . )
ok "ZIP listo"

step "Desplegando a $APP"
az webapp deploy \
  --name "$APP" --resource-group "$RG" \
  --src-path "$PROJECT_DIR/$ZIP_PATH" --type zip \
  --output none

ok "Deploy completado"
echo
echo "Verifica:"
echo "  curl https://$APP.azurewebsites.net/health"
echo "  curl https://$APP.azurewebsites.net/health/details | jq"
echo "  curl 'https://$APP.azurewebsites.net/load/cpu?ms=500'"
