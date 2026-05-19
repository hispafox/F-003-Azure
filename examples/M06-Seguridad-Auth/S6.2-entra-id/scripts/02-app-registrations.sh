#!/usr/bin/env bash
# 02 - App Registrations y Service Principals (slides 8-9, 36): qué apps
# tienen identidad y cuáles podrían tener secretos caducados. SOLO
# LECTURA.

set -euo pipefail
source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

step "App Registrations (slide 8)"
az ad app list --query "[].{Nombre:displayName, AppId:appId, Audiencia:signInAudience}" \
  -o table 2>/dev/null | head -25 \
  || warn "Sin permisos para listar app registrations"

step "Apps con client secret (slide 8: el secret va a Key Vault)"
for appid in $(az ad app list --query "[].appId" -o tsv 2>/dev/null | head -15); do
  N=$(az ad app credential list --id "$appid" \
    --query "length(@)" -o tsv 2>/dev/null || echo 0)
  NAME=$(az ad app show --id "$appid" --query displayName -o tsv 2>/dev/null)
  [ "${N:-0}" -gt 0 ] && warn "  $NAME: $N secret(s) — ¿en Key Vault? ¿caducados?"
done

step "Service Principals propios (slide 9/36 — revisar lifecycle)"
az ad sp list --filter "servicePrincipalType eq 'Application'" \
  --query "[?accountEnabled].{Nombre:displayName, AppId:appId}" \
  -o table 2>/dev/null | head -20 \
  || warn "Sin permisos para listar service principals"

echo
ok "Inventario de identidades de aplicación completado"
echo "Prioridad (slide 10): Managed Identity > SP+cert > SP+secret."
