#!/usr/bin/env bash
# 03 - Smoke test: sube un CSV a uploads/ y espera al Blob trigger.

set -euo pipefail
source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

BASE="https://${FUNC}.azurewebsites.net"
API="$BASE/api"
TIMEOUT=60

step "Obteniendo function key"
KEY=$(az functionapp keys list --name "$FUNC" --resource-group "$RG" \
  --query "functionKeys.default" -o tsv)
[ -n "$KEY" ] || { echo "[X] No se pudo leer la function key"; exit 1; }

step "Connection string del Storage"
STORAGE_CONN=$(az storage account show-connection-string \
  --name "$STORAGE" --resource-group "$RG" \
  --query connectionString -o tsv)

# ── Generar un CSV de prueba ──
TS=$(date -u +%s)
CSV_NAME="smoketest-$TS"
TMP_CSV=$(mktemp -t smoketest.XXXXXX.csv)
cat > "$TMP_CSV" <<'EOF'
nombre,categoria,precio,stock
Libro AZ-204,libros,29.99,50
Mug Azure,merchandising,9.50,200
Stickers,merchandising,2.00,500
EOF

step "Subiendo $CSV_NAME.csv a uploads/"
az storage blob upload \
  --connection-string "$STORAGE_CONN" \
  --container-name uploads \
  --name "$CSV_NAME.csv" \
  --file "$TMP_CSV" \
  --overwrite \
  --output none
ok "CSV subido"

# El Blob trigger en plan Consumption hace POLLING (slide 4): puede tardar
# de 10 segundos a varios minutos en detectar el blob nuevo.
step "Esperando hasta 90s a que el Blob trigger procese el CSV..."
DEADLINE=$(( $(date +%s) + 90 ))
PROCESSED=0
while [ "$(date +%s)" -lt "$DEADLINE" ]; do
  HTTP_CODE=$(curl -o /tmp/out -s -w "%{http_code}" --max-time $TIMEOUT \
    "$API/imports/$CSV_NAME.csv?code=$KEY")
  if [ "$HTTP_CODE" = "200" ]; then
    PROCESSED=1; break
  fi
  echo "  ...esperando ($(($DEADLINE - $(date +%s)))s restantes)"
  sleep 10
done

if [ "$PROCESSED" -eq 1 ]; then
  ok "Blob trigger detecto y proceso $CSV_NAME.csv"
  echo
  echo "Resumen:"
  cat /tmp/out
else
  warn "Blob trigger no proceso en 90s. Posibles causas:"
  echo "  - Cold start largo"
  echo "  - Polling tardio (slide 4 - hasta 10 min en Consumption)"
  echo "  Revisa con: az functionapp log tail --name $FUNC -g $RG"
  rm -f "$TMP_CSV"
  exit 1
fi

# ── Verificar que el blob de salida se creo en procesados/ ──
echo
step "Verificando procesados/$CSV_NAME-resumen.json"
EXISTS=$(az storage blob exists \
  --connection-string "$STORAGE_CONN" \
  --container-name procesados \
  --name "$CSV_NAME-resumen.json" \
  --query exists -o tsv)
if [ "$EXISTS" = "true" ]; then
  ok "Output blob escrito"
else
  warn "Output blob NO encontrado. El Blob trigger ejecutó pero el output binding falló."
fi

# ── Idempotencia del summary: GET /api/imports lista todos ──
echo
step "Lista de todos los imports (/api/imports):"
curl -s --max-time $TIMEOUT "$API/imports?code=$KEY" | head -c 500
echo

rm -f "$TMP_CSV" /tmp/out
echo
ok "Smoke test completado"
