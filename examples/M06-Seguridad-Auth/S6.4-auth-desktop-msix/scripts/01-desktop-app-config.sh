#!/usr/bin/env bash
# 01 - Config de las App Registrations de cliente público (desktop/MSIX,
# slides 4, 7, 11): redirect URIs nativos, broker plugin, fallback
# public client. SOLO LECTURA.

set -euo pipefail
source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

FILTER=()
[ -n "${APP_DISPLAY_NAME:-}" ] && FILTER=(--display-name "$APP_DISPLAY_NAME")

step "Apps de cliente público + redirect URIs nativos (slide 4/7)"
az ad app list "${FILTER[@]}" \
  --query "[?isFallbackPublicClient || length(publicClient.redirectUris) > \`0\`].{Nombre:displayName, PublicClient:isFallbackPublicClient, NativeRedirects:publicClient.redirectUris}" \
  -o jsonc 2>/dev/null | head -60 \
  || warn "Sin permisos para listar app registrations"

step "¿Tienen el redirect URI del broker (WAM/MSIX)? (slide 7/11)"
for appid in $(az ad app list "${FILTER[@]}" --query "[].appId" -o tsv 2>/dev/null | head -15); do
  NAME=$(az ad app show --id "$appid" --query displayName -o tsv 2>/dev/null)
  URIS=$(az ad app show --id "$appid" \
    --query "publicClient.redirectUris" -o tsv 2>/dev/null || echo "")
  echo "$URIS" | grep -q "microsoft.aad.brokerplugin/$appid" \
    && ok "  $NAME: broker URI OK" \
    || warn "  $NAME: falta ms-appx-web://microsoft.aad.brokerplugin/$appid (WAM)"
  echo "$URIS" | grep -q "urn:ietf:wg:oauth:2.0:oob" \
    && warn "  $NAME: usa 'oob' (legacy, NO recomendado — slide 7)"
done

echo
ok "Inspección de apps desktop completada (solo lectura)"
echo "Recordatorio (slide 3/4): WAM mejor en Windows; system browser"
echo "multiplataforma; embedded solo aceptable; desktop = cliente público."
