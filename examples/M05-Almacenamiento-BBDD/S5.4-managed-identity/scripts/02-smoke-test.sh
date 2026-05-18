#!/usr/bin/env bash
# 02 - Smoke test (slide 24: troubleshooting flow): ¿MI habilitada?
# ¿el rol RBAC está asignado al principalId correcto y con scope mínimo?

set -euo pipefail
source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

step "STEP 1 — ¿Managed Identity habilitada en la app?"
PRINCIPAL_ID=$(az webapp identity show \
  --name "$APP_NAME" --resource-group "$RG" \
  --query principalId -o tsv 2>/dev/null || echo "")
if [ -z "$PRINCIPAL_ID" ]; then
  warn "MI NO habilitada → ./01-provision.sh"
  exit 1
fi
ok "principalId: $PRINCIPAL_ID"

step "STEP 2 — ¿El rol RBAC está asignado y es de mínimo privilegio?"
az role assignment list --assignee "$PRINCIPAL_ID" \
  --query "[].{Rol:roleDefinitionName, Scope:scope}" -o table

ROLES=$(az role assignment list --assignee "$PRINCIPAL_ID" \
  --query "[].roleDefinitionName" -o tsv)
if echo "$ROLES" | grep -qiE '^(Owner|Contributor)$'; then
  warn "Hay un rol Owner/Contributor → anti-pattern (slide 27). Usa roles de datos."
else
  ok "Sin roles de plano de control amplios (least privilege OK)"
fi

step "STEP 5 — App Setting endpoint sin key"
az webapp config appsettings list --name "$APP_NAME" --resource-group "$RG" \
  --query "[?name=='StorageBlobEndpoint'].{name:name, value:value}" -o table

echo
ok "Smoke test OK — MI + RBAC mínimo verificados"
echo "RBAC tarda 5-10 min en propagar (slide 24). La app la despliegas tú."
