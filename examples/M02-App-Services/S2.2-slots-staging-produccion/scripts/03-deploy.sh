#!/usr/bin/env bash
# 03 — Publica la API y la despliega a producción o al slot staging.
# Uso: ./03-deploy.sh production
#      ./03-deploy.sh staging
# Slide 6 — deploy a slot añadiendo --slot.

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

TARGET="${1:-staging}"

if [[ "$TARGET" != "production" && "$TARGET" != "staging" ]]; then
  echo "[X] Uso: $0 production|staging"
  exit 1
fi

ZIP_PATH="${ZIP_PATH:-./out/app.zip}"
PROJECT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )/.." && pwd )"
PROJECT="$PROJECT_DIR/src/AppService.Demo.Api/AppService.Demo.Api.csproj"
PUBLISH_DIR="$PROJECT_DIR/out/publish"

step "Publicando $PROJECT (Release)"
rm -rf "$PUBLISH_DIR" "$(dirname "$ZIP_PATH")/app.zip" 2>/dev/null || true
dotnet publish "$PROJECT" -c Release -o "$PUBLISH_DIR" --nologo

step "Empaquetando a $ZIP_PATH"
mkdir -p "$(dirname "$ZIP_PATH")"
( cd "$PUBLISH_DIR" && zip -qr "$PROJECT_DIR/$ZIP_PATH" . )
ok "ZIP listo"

step "Desplegando a $TARGET"
if [ "$TARGET" = "production" ]; then
  az webapp deploy \
    --name "$APP" --resource-group "$RG" \
    --src-path "$PROJECT_DIR/$ZIP_PATH" --type zip \
    --output none
  URL="https://$APP.azurewebsites.net"
else
  az webapp deploy \
    --name "$APP" --resource-group "$RG" --slot staging \
    --src-path "$PROJECT_DIR/$ZIP_PATH" --type zip \
    --output none
  URL="https://$APP-staging.azurewebsites.net"
fi
ok "Deploy completado en $URL"

echo
echo "Verifica:"
echo "  curl $URL/health"
echo "  curl $URL/version"
echo "  curl $URL/info"
