#!/usr/bin/env bash
# 01 - Verifica el entregable (slide 5/6/11): Easy Auth habilitado,
# acción ante no-autenticado, y que /.auth/me responde. SOLO LECTURA.

set -euo pipefail
source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

step "Easy Auth: estado y acción ante peticiones no autenticadas (slide 5)"
az webapp auth show --name "$APP_NAME" --resource-group "$RG" \
  --query "{Habilitado:platform.enabled, RequireAuth:globalValidation.requireAuthentication, AccionNoAuth:globalValidation.unauthenticatedClientAction, TokenStore:login.tokenStore.enabled}" \
  -o jsonc 2>/dev/null || warn "Sin acceso o Easy Auth no configurado"

EN=$(az webapp auth show --name "$APP_NAME" --resource-group "$RG" \
  --query "platform.enabled" -o tsv 2>/dev/null || echo "")
[ "$EN" = "true" ] && ok "Easy Auth habilitado (slide 5)" \
  || warn "Easy Auth NO habilitado: añádelo desde el portal (slide 5)"

URL="https://$APP_NAME.azurewebsites.net"
step "Prueba sin sesión: $URL/ (sitio web → 302 al login, slide 6)"
CODE=$(curl -s -o /dev/null -w "%{http_code}" "$URL/" 2>/dev/null || echo "000")
echo "  HTTP $CODE"
case "$CODE" in
  302|401) ok "Protegido (sin sesión rechaza/redirige)" ;;
  200)     warn "200 sin sesión: ¿Require authentication está OFF?" ;;
  *)       warn "Respuesta inesperada ($CODE) — ¿app desplegada?" ;;
esac

step "Endpoint integrado /.auth/me (slide 4)"
curl -s -o /dev/null -w "  /.auth/me → HTTP %{http_code}\n" \
  "$URL/.auth/me" 2>/dev/null || warn "  no accesible"

echo
ok "Verificación de Easy Auth completada (solo lectura)"
echo "Recordatorio: cero código de auth — el middleware vive en App"
echo "Service, ANTES de tu app (slide 5). API → Return401; web → 302."
