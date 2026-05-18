#!/usr/bin/env bash
# 01 - El flujo de la slide 5/8: App Service + Storage, habilitar
# Managed Identity SYSTEM-ASSIGNED en la app, y darle el rol RBAC
# MÍNIMO sobre el Storage (slide 23: least privilege, scope = la cuenta,
# rol "Storage Blob Data Reader" — NO Contributor/Owner).

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

step "Resource Group: $RG"
az group create --name "$RG" --location "$LOCATION" \
  --tags "curso=AZ-204" "modulo=M05" "submodulo=S5.4" --output none

step "Storage Account: $STORAGE (StorageV2, sin acceso público, TLS1.2)"
az storage account create \
  --name "$STORAGE" --resource-group "$RG" --location "$LOCATION" \
  --sku Standard_LRS --kind StorageV2 \
  --min-tls-version TLS1_2 --allow-blob-public-access false \
  --output none

step "App Service Plan F1 (gratis) + Web App: $APP_NAME"
az appservice plan create \
  --name "plan-$APP_NAME" --resource-group "$RG" \
  --sku F1 --is-linux --output none
az webapp create \
  --name "$APP_NAME" --resource-group "$RG" \
  --plan "plan-$APP_NAME" --runtime "DOTNETCORE:10.0" \
  --https-only true --output none

step "Habilitar Managed Identity system-assigned (slide 5)"
az webapp identity assign \
  --name "$APP_NAME" --resource-group "$RG" --output none
PRINCIPAL_ID=$(az webapp identity show \
  --name "$APP_NAME" --resource-group "$RG" --query principalId -o tsv)
echo "  principalId: $PRINCIPAL_ID"

STORAGE_ID=$(az storage account show \
  --name "$STORAGE" --resource-group "$RG" --query id -o tsv)

step "Rol RBAC mínimo: 'Storage Blob Data Reader' scope=cuenta (slide 23)"
az role assignment create \
  --assignee "$PRINCIPAL_ID" \
  --role "Storage Blob Data Reader" \
  --scope "$STORAGE_ID" --output none

step "App Setting solo con la URL (sin key, slide 8)"
az webapp config appsettings set \
  --name "$APP_NAME" --resource-group "$RG" \
  --settings "StorageBlobEndpoint=https://$STORAGE.blob.core.windows.net" \
  --output none

ok "MI lista: $APP_NAME → $STORAGE (sin secretos)"
echo
echo "La app despliégala tú (no la lanzamos). La RBAC tarda ~5-10 min"
echo "en propagar (slide 24). En local: 'az login' y DefaultAzureCredential"
echo "usa tu identidad; en Azure usará la MI."
echo
echo "Siguiente: ./02-smoke-test.sh"
