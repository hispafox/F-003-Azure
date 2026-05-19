#!/usr/bin/env bash
# 01 - Verifica el entregable de la práctica (slide 11): Easy Auth on,
# App Settings solo con Key Vault References, MI con acceso al KV.
# SOLO LECTURA.

set -euo pipefail
source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

step "Easy Auth habilitado en la app (slide 8)"
az webapp auth show --name "$APP_NAME" --resource-group "$RG" \
  --query "{Habilitado:platform.enabled, AccionNoAuth:globalValidation.unauthenticatedClientAction}" \
  -o jsonc 2>/dev/null || warn "Sin acceso o Easy Auth no configurado"

step "App Settings: ¿solo Key Vault References en los secretos? (slide 7/11)"
HITS=$(az webapp config appsettings list --name "$APP_NAME" --resource-group "$RG" \
  --query "[?(contains(name,'Secret') || contains(name,'ApiKey')) && !starts_with(value, '@Microsoft.KeyVault(')].name" \
  -o tsv 2>/dev/null || echo "")
if [ -z "$HITS" ]; then
  ok "Cero secretos en claro (solo @Microsoft.KeyVault)"
else
  warn "Secretos en claro detectados: $HITS"
fi
az webapp config appsettings list --name "$APP_NAME" --resource-group "$RG" \
  --query "[?starts_with(value, '@Microsoft.KeyVault(')].name" -o table 2>/dev/null \
  | sed 's/^/  ref → /' || true

step "Managed Identity de la app + rol en Key Vault (slide 6)"
PID=$(az webapp identity show --name "$APP_NAME" --resource-group "$RG" \
  --query principalId -o tsv 2>/dev/null || echo "")
[ -n "$PID" ] && ok "MI principalId: $PID" || warn "MI no habilitada"
[ -n "$PID" ] && az role assignment list --assignee "$PID" \
  --query "[?contains(roleDefinitionName,'Key Vault')].roleDefinitionName" -o tsv \
  2>/dev/null | sed 's/^/  rol KV → /' || true

echo
ok "Verificación del entregable completada (solo lectura)"
echo "Prueba manual (slide 10): /health sin token → 200; /api/perfil sin"
echo "token → 401; con token (az account get-access-token) → 200."
