#!/usr/bin/env bash
# 02 - Publish + zip + deploy, y ALTA de la subscription de Event Grid
# que dirige BlobCreated del container 'uploads/' al webhook de la función.
#
# La suscripción se crea TRAS el deploy porque necesita la system key del
# Function App, que solo existe cuando hay funciones desplegadas.

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

ZIP_PATH="${ZIP_PATH:-./out/app.zip}"
PROJECT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )/.." && pwd )"
PROJECT="$PROJECT_DIR/src/AzureFunctions.Demo/AzureFunctions.Demo.csproj"
PUBLISH_DIR="$PROJECT_DIR/out/publish"

step "dotnet publish (Release)"
rm -rf "$PUBLISH_DIR" "$PROJECT_DIR/$ZIP_PATH" 2>/dev/null || true
dotnet publish "$PROJECT" -c Release -o "$PUBLISH_DIR" --nologo

step "Empaquetando a $ZIP_PATH"
mkdir -p "$(dirname "$PROJECT_DIR/$ZIP_PATH")"
( cd "$PUBLISH_DIR" && zip -qr "$PROJECT_DIR/$ZIP_PATH" . )

step "Deploy a $FUNC"
az functionapp deployment source config-zip \
  --name "$FUNC" --resource-group "$RG" \
  --src "$PROJECT_DIR/$ZIP_PATH" \
  --output none

ok "Deploy completado"

# ── Event Grid subscription ──
echo
step "Obteniendo system key del Function App para el webhook de Event Grid..."
sleep 10  # darle margen al runtime para registrar la función
SYSTEM_KEY=$(az functionapp keys list \
  --name "$FUNC" --resource-group "$RG" \
  --query "systemKeys.eventgrid_extension" -o tsv 2>/dev/null || echo "")

if [ -z "$SYSTEM_KEY" ]; then
  warn "No se pudo obtener la system key 'eventgrid_extension'. Reintenta tras 30s con ./02-deploy.sh"
  warn "Si persiste, crea la subscription a mano desde el Portal apuntando al endpoint:"
  echo "  https://$FUNC.azurewebsites.net/runtime/webhooks/eventgrid?functionName=ClasificarArchivo&code=<system-key>"
  exit 0
fi

WEBHOOK="https://$FUNC.azurewebsites.net/runtime/webhooks/eventgrid?functionName=ClasificarArchivo&code=$SYSTEM_KEY"

STORAGE_ID=$(az storage account show \
  --name "$STORAGE" --resource-group "$RG" \
  --query id -o tsv)

step "Creando suscripción de Event Grid → ClasificarArchivo"
az eventgrid event-subscription create \
  --name "sub-blob-uploads-s41" \
  --source-resource-id "$STORAGE_ID" \
  --endpoint "$WEBHOOK" \
  --endpoint-type webhook \
  --included-event-types Microsoft.Storage.BlobCreated \
  --subject-begins-with "/blobServices/default/containers/$CONTAINER_UPLOADS/" \
  --output none

ok "Subscription Event Grid creada"
echo "Verifica con ./03-smoke-test.sh"
