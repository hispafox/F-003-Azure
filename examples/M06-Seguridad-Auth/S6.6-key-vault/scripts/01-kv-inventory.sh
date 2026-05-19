#!/usr/bin/env bash
# 01 - Inventario de Key Vault (slides 4-9): modo RBAC, purge
# protection, secretos (SOLO nombres, jamás valores) y caducidades.
# SOLO LECTURA.

set -euo pipefail
source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

VAULTS=$(az keyvault list --query "[].name" -o tsv 2>/dev/null || echo "")
[ -n "${KV_NAME:-}" ] && VAULTS="$KV_NAME"
[ -z "$VAULTS" ] && { warn "Sin Key Vaults o sin permisos"; exit 0; }

for kv in $VAULTS; do
  step "Key Vault: $kv — modo de acceso y protección (slide 5)"
  az keyvault show --name "$kv" \
    --query "{RBAC:properties.enableRbacAuthorization, PurgeProtection:properties.enablePurgeProtection, SoftDelete:properties.enableSoftDelete}" \
    -o jsonc 2>/dev/null || warn "  sin acceso a $kv"

  RBAC=$(az keyvault show --name "$kv" \
    --query "properties.enableRbacAuthorization" -o tsv 2>/dev/null || echo "")
  [ "$RBAC" = "true" ] && ok "  RBAC habilitado (recomendado, slide 5)" \
    || warn "  Usa Access Policies (legacy): migra a RBAC (slide 5)"

  step "  Secretos (SOLO nombres) + caducidad (slide 8-9)"
  az keyvault secret list --vault-name "$kv" \
    --query "[].{Nombre:name, Habilitado:attributes.enabled, Expira:attributes.expires}" \
    -o table 2>/dev/null | head -25 \
    || warn "  sin permiso de lectura de secretos (Key Vault Secrets User)"
done

echo
ok "Inventario de Key Vault completado (solo lectura, sin valores)"
echo "Regla (slide 2): si es secreto y no puede ser MI → Key Vault."
echo "Rotación (slide 9): Event Grid SecretNearExpiry 30 días antes."
