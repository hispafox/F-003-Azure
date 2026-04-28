#!/usr/bin/env bash
# 05 — Configura los App Settings sensibles como Key Vault References.
# Slides 9, 27 — sintaxis @Microsoft.KeyVault(VaultName=...;SecretName=...).

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

: "${KV:?KV no definido}"

step "Configurando Key Vault references"
az webapp config appsettings set \
  --name "$APP" --resource-group "$RG" \
  --settings \
    "AppOptions__ApiKey=@Microsoft.KeyVault(VaultName=$KV;SecretName=ApiKey)" \
    "AppOptions__ConnectionString=@Microsoft.KeyVault(VaultName=$KV;SecretName=ConnectionString)" \
  --output none

ok "Referencias configuradas"

step "Verificando que App Service resolvió las referencias"
echo "  Esperando 15s a que la app reinicie y resuelva las KV refs..."
sleep 15

# El Portal muestra el estado en Configuration → Application settings.
# Por CLI, az lista los settings; los KV refs aparecen marcados.
az webapp config appsettings list \
  --name "$APP" --resource-group "$RG" \
  --query "[?contains(value, 'Microsoft.KeyVault')].{name:name, value:value}" \
  --output table

echo
echo "Test rápido (NUNCA expone el valor en claro):"
echo "  curl https://$APP.azurewebsites.net/secrets/api-key/check"
echo "  curl https://$APP.azurewebsites.net/connection"
echo "  curl https://$APP.azurewebsites.net/config | jq"
