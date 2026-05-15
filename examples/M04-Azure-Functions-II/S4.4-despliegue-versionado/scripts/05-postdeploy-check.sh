#!/usr/bin/env bash
# 05 - Verificacion post-deploy (slide 14). El pipeline llamaria a esto
# tras el deploy: si algo falla, aborta el swap / hace rollback.
#
# Comprueba: estado de la app, /health 200, /version coincide con la
# esperada, y los endpoints versionados responden.

set -euo pipefail
source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

API="https://${FUNC}.azurewebsites.net/api"
EXPECTED_VERSION="${1:-}"   # opcional: versión que esperabas desplegar
TIMEOUT=30

echo "=== POST-DEPLOY CHECK: $FUNC ==="

step "1) Estado del Function App"
STATE=$(az functionapp show --name "$FUNC" --resource-group "$RG" --query state -o tsv)
echo "   state=$STATE"
[ "$STATE" = "Running" ] || { echo "[X] La app no esta Running"; exit 1; }

step "2) /api/health (espera 200)"
CODE=$(curl -s -o /tmp/s44_health -w "%{http_code}" --max-time $TIMEOUT "$API/health")
cat /tmp/s44_health; echo
[ "$CODE" = "200" ] || { echo "[X] /health devolvio $CODE (esperado 200)"; exit 1; }
ok "   health OK"

step "3) /api/version"
VER_JSON=$(curl -s --max-time $TIMEOUT "$API/version")
echo "   $VER_JSON"
if [ -n "$EXPECTED_VERSION" ]; then
  if echo "$VER_JSON" | grep -q "\"$EXPECTED_VERSION\""; then
    ok "   version coincide con la esperada ($EXPECTED_VERSION)"
  else
    echo "[X] La version desplegada NO coincide con '$EXPECTED_VERSION'"
    echo "    → el deploy no tomó o se desplegó otro artefacto. ROLLBACK."
    exit 1
  fi
fi

step "4) Endpoints versionados responden"
for path in "v1/productos" "v2/productos"; do
  C=$(curl -s -o /dev/null -w "%{http_code}" --max-time $TIMEOUT "$API/$path")
  echo "   /$path → HTTP $C"
  [ "$C" = "200" ] || { echo "[X] /$path devolvio $C"; exit 1; }
done

rm -f /tmp/s44_health
echo
ok "=== POST-DEPLOY CHECK OK — seguro para promover a produccion ==="
