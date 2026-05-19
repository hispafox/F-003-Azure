#!/usr/bin/env bash
# 01 - Config OAuth de las App Registrations (slides 5-8, 17): redirect
# URIs, sign-in audience, id_token issuance, permisos. SOLO LECTURA.

set -euo pipefail
source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

FILTER=()
[ -n "${APP_DISPLAY_NAME:-}" ] && FILTER=(--display-name "$APP_DISPLAY_NAME")

step "Redirect URIs y audiencia (slide 8/17)"
az ad app list "${FILTER[@]}" \
  --query "[].{Nombre:displayName, Audiencia:signInAudience, WebRedirects:web.redirectUris, SpaRedirects:spa.redirectUris}" \
  -o jsonc 2>/dev/null | head -60 \
  || warn "Sin permisos para listar app registrations"

step "Concesiones de permisos OAuth2 (delegated/application — slide 11)"
for appid in $(az ad app list "${FILTER[@]}" --query "[].appId" -o tsv 2>/dev/null | head -10); do
  NAME=$(az ad app show --id "$appid" --query displayName -o tsv 2>/dev/null)
  echo "  · $NAME ($appid)"
  az ad app permission list --id "$appid" \
    --query "[].{API:resourceAppId, Permisos:length(resourceAccess)}" \
    -o tsv 2>/dev/null | sed 's/^/      /' || true
done

step "Flujos deprecados: ¿alguna app permite id_token implicit? (slide 5)"
az ad app list "${FILTER[@]}" \
  --query "[?web.implicitGrantSettings.enableIdTokenIssuance].displayName" \
  -o tsv 2>/dev/null | sed 's/^/  [!] implicit habilitado: /' \
  || ok "Ninguna app con implicit id_token (bien)"

echo
ok "Inspección de config OAuth completada (solo lectura)"
echo "Recordatorio (slide 5): Auth Code + PKCE para apps con usuario;"
echo "Client Credentials para servicios. Implicit/ROPC deprecados."
