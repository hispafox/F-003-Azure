#!/usr/bin/env bash
# 01 - Posture check de la capa RED (slide 7): los fallos de
# configuración más comunes y caros (slide 4). SOLO LECTURA.

set -euo pipefail
source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

SCOPE_ARGS=()
[ -n "${RG:-}" ] && SCOPE_ARGS=(--resource-group "$RG")

step "Storage Accounts: ¿acceso público / firewall abierto? (slide 4/7)"
az storage account list "${SCOPE_ARGS[@]}" \
  --query "[].{Nombre:name, PublicBlob:allowBlobPublicAccess, FirewallDefault:networkRuleSet.defaultAction, TLS:minimumTlsVersion}" \
  -o table || warn "Sin permisos o sin storage accounts"

step "SQL Servers: regla de firewall 0.0.0.0 (abierto al mundo, slide 4)"
for srv in $(az sql server list "${SCOPE_ARGS[@]}" --query "[].name" -o tsv 2>/dev/null); do
  RG_SRV=$(az sql server list --query "[?name=='$srv'].resourceGroup" -o tsv)
  ABIERTO=$(az sql server firewall-rule list --server "$srv" -g "$RG_SRV" \
    --query "[?startIpAddress=='0.0.0.0' && endIpAddress=='255.255.255.255'].name" -o tsv)
  [ -n "$ABIERTO" ] && warn "  $srv: REGLA ABIERTA 0.0.0.0-255.255.255.255" \
    || ok "  $srv: sin regla totalmente abierta"
done

step "App Services: ¿HTTPS forzado? (slide 8)"
az webapp list "${SCOPE_ARGS[@]}" \
  --query "[].{Nombre:name, HttpsOnly:httpsOnly}" -o table 2>/dev/null \
  || warn "Sin permisos o sin web apps"

echo
ok "Posture check completado (solo lectura)"
echo "Compara con el checklist: GET /seguridad/secure-score (slide 17)"
