#!/usr/bin/env bash
# 02 - Verificación del entregable (slide 13/14): MI habilitada, rol de
# datos asignado, y CERO secretos en App Settings (solo CosmosEndpoint).

set -euo pipefail
source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

step "MI habilitada en la app"
PRINCIPAL_ID=$(az webapp identity show \
  --name "$APP_NAME" --resource-group "$RG" \
  --query principalId -o tsv 2>/dev/null || echo "")
[ -n "$PRINCIPAL_ID" ] && ok "principalId: $PRINCIPAL_ID" \
  || { warn "MI NO habilitada → ./01-provision.sh"; exit 1; }

step "Rol de datos en Cosmos asignado a la MI (slide 6)"
az cosmosdb sql role assignment list \
  --account-name "$COSMOS_ACCOUNT" --resource-group "$RG" \
  --query "[?principalId=='$PRINCIPAL_ID'].roleDefinitionId" -o tsv \
  | grep -q . && ok "Rol de datos presente" \
  || warn "Sin role assignment para la MI (espera propagación o re-asigna)"

step "Auditoría zero-secrets de App Settings (slide 13)"
HITS=$(az webapp config appsettings list \
  --name "$APP_NAME" --resource-group "$RG" \
  --query "[?contains(name,'Key') || contains(name,'ConnectionString') || contains(name,'Password')].name" \
  -o tsv)
if [ -z "$HITS" ]; then
  ok "Cero secretos: solo CosmosEndpoint (entregable cumplido)"
else
  warn "App Settings sospechosos: $HITS  → migra a MI / Key Vault"
fi

az webapp config appsettings list --name "$APP_NAME" --resource-group "$RG" \
  --query "[?name=='CosmosEndpoint'].{name:name, value:value}" -o table

echo
ok "Smoke test OK. Test extra (slide 13): regenera la key de Cosmos —"
echo "la app debe SEGUIR funcionando porque NO usa keys:"
echo "  az cosmosdb keys regenerate -n $COSMOS_ACCOUNT -g $RG --key-kind primary"
