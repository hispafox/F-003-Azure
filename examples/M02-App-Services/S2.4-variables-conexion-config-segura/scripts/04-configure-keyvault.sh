#!/usr/bin/env bash
# 04 — Habilita Managed Identity en la web app, le asigna rol "Key Vault
# Secrets User" sobre el KV, y guarda los dos secrets que la app espera.
# Slides 9, 25 — RBAC para Key Vault, MI System-Assigned.

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

: "${KV:?KV no definido}"

step "Habilitando Managed Identity en $APP"
az webapp identity assign --name "$APP" --resource-group "$RG" --output none
APP_MI=$(az webapp identity show --name "$APP" --resource-group "$RG" --query principalId -o tsv)
echo "  MI principalId: $APP_MI"

step "Asignando rol 'Key Vault Secrets User' al MI"
KV_ID=$(az keyvault show --name "$KV" --query id -o tsv)
az role assignment create \
  --assignee "$APP_MI" \
  --role "Key Vault Secrets User" \
  --scope "$KV_ID" \
  --output none

# Le damos al usuario que ejecuta el script permisos para escribir secrets,
# si no los tiene ya. "Key Vault Secrets Officer" cubre lectura+escritura.
USER_ID=$(az ad signed-in-user show --query id -o tsv 2>/dev/null || echo "")
if [ -n "$USER_ID" ]; then
  step "Asegurando que tu usuario puede crear secrets ($USER_ID)"
  az role assignment create \
    --assignee "$USER_ID" \
    --role "Key Vault Secrets Officer" \
    --scope "$KV_ID" \
    --output none 2>/dev/null || true
  # Esperamos un poco a que la asignación de rol propague
  echo "  Esperando 30s a que RBAC propague..."
  sleep 30
fi

step "Creando secrets de demo en $KV"
RANDOM_KEY="demo-api-key-$(openssl rand -hex 12)"
RANDOM_PASS=$(openssl rand -hex 8)
CONN_STR="Server=tcp:demo-sql.database.windows.net,1433;Database=demo;User ID=admin;Password=Demo${RANDOM_PASS}!;Encrypt=true"

az keyvault secret set \
  --vault-name "$KV" --name "ApiKey" --value "$RANDOM_KEY" \
  --tags rotationPolicy=90days owner=demo \
  --output none

az keyvault secret set \
  --vault-name "$KV" --name "ConnectionString" --value "$CONN_STR" \
  --tags rotationPolicy=90days owner=demo \
  --output none

ok "Key Vault listo con secrets ApiKey y ConnectionString"
echo
echo "Siguiente: ./05-configure-keyvault-references.sh"
