#!/usr/bin/env bash
# 06 — Rota el ApiKey en Key Vault. App Service refresca KV references con
# cierta latencia (cache ~5-10 min). Para forzar refresh inmediato, reinicia
# la app. Slide 26 — patrón de rotación.

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

: "${KV:?KV no definido}"

NEW_VALUE="rotated-$(date +%s)-$(openssl rand -hex 12)"

step "Antes de rotar (fingerprint actual):"
curl -s "https://$APP.azurewebsites.net/secrets/api-key/check" | tr ',' '\n' | grep -E '"(fingerprint|length|source)"' || true

step "Rotando ApiKey en KV (nuevo valor: ${NEW_VALUE:0:24}...)"
az keyvault secret set --vault-name "$KV" --name "ApiKey" --value "$NEW_VALUE" --output none
ok "Secret rotado"

read -r -p "¿Reiniciar la app para que recoja el nuevo valor inmediatamente? [s/N] " resp
if [[ "$resp" =~ ^[sSyY]$ ]]; then
  step "Reiniciando $APP"
  az webapp restart --name "$APP" --resource-group "$RG" --output none
  echo "  Esperando 20s a que la app vuelva..."
  sleep 20

  step "Después de rotar (fingerprint nuevo):"
  curl -s "https://$APP.azurewebsites.net/secrets/api-key/check" | tr ',' '\n' | grep -E '"(fingerprint|length|source)"' || true
fi
