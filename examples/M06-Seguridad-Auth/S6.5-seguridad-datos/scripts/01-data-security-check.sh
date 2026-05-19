#!/usr/bin/env bash
# 01 - Postura de cifrado en tránsito/reposo (slides 3, 5, 8, 14):
# min-TLS, HTTPS-only, TDE. SOLO LECTURA.

set -euo pipefail
source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

S=()
[ -n "${RG:-}" ] && S=(--resource-group "$RG")

step "Storage: min-TLS y acceso solo HTTPS (slide 3/5/14)"
az storage account list "${S[@]}" \
  --query "[].{Nombre:name, MinTLS:minimumTlsVersion, SoloHttps:supportsHttpsTrafficOnly}" \
  -o table 2>/dev/null || warn "Sin permisos o sin storage accounts"

step "App Services: HTTPS-only y min-TLS (slide 3/14)"
az webapp list "${S[@]}" \
  --query "[].{Nombre:name, HttpsOnly:httpsOnly}" -o table 2>/dev/null \
  || warn "Sin permisos o sin web apps"

step "SQL Servers: min-TLS (slide 3)"
az sql server list "${S[@]}" \
  --query "[].{Nombre:name, MinTLS:minimalTlsVersion}" -o table 2>/dev/null \
  || warn "Sin permisos o sin SQL servers"

step "Azure SQL: TDE (cifrado at-rest, slide 8 — debe estar Enabled)"
for srv in $(az sql server list "${S[@]}" --query "[].name" -o tsv 2>/dev/null); do
  RGS=$(az sql server list --query "[?name=='$srv'].resourceGroup" -o tsv)
  for db in $(az sql db list --server "$srv" -g "$RGS" \
      --query "[?name!='master'].name" -o tsv 2>/dev/null); do
    ST=$(az sql db tde show --server "$srv" -g "$RGS" --database "$db" \
      --query status -o tsv 2>/dev/null || echo "?")
    echo "  $srv/$db → TDE: $ST"
  done
done

echo
ok "Postura de cifrado revisada (solo lectura)"
echo "Objetivo (slide 14): HTTPS forzado, TLS 1.2 mín, TDE on, CMK si"
echo "lo exige regulación, CORS con orígenes explícitos."
