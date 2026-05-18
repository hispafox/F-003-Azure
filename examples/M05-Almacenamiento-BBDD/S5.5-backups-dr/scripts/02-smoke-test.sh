#!/usr/bin/env bash
# 02 - Walkthrough de recuperación con soft delete (slide 6/19):
# subir → borrar → undelete blob, y borrar → restore container.
# Round-trip real con `az storage` (sin lanzar la app).

set -euo pipefail
source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

CONN="$(conn)"
TS=$(date -u +%s)
TMP=$(mktemp -t s55.XXXXXX.txt)
echo "factura demo $TS" > "$TMP"
BLOB="factura-$TS.txt"

step "[Blob] subir $BLOB"
az storage blob upload --connection-string "$CONN" \
  --container-name facturas --name "$BLOB" --file "$TMP" --overwrite --output none

step "[Blob] borrar $BLOB"
az storage blob delete --connection-string "$CONN" \
  --container-name facturas --name "$BLOB" --output none
EX=$(az storage blob exists --connection-string "$CONN" \
  --container-name facturas --name "$BLOB" --query exists -o tsv)
echo "  existe tras borrar: $EX  (soft-deleted, recuperable 30d)"

step "[Blob] undelete (slide 6/19)"
az storage blob undelete --connection-string "$CONN" \
  --container-name facturas --name "$BLOB" --output none
EX=$(az storage blob exists --connection-string "$CONN" \
  --container-name facturas --name "$BLOB" --query exists -o tsv)
echo "  existe tras undelete: $EX"
[ "$EX" = "true" ] && ok "Blob recuperado con soft delete" \
  || { warn "No se recuperó el blob"; exit 1; }

step "[Container] borrar y restaurar 'facturas' (slide 19)"
az storage container delete --connection-string "$CONN" --name facturas --output none
sleep 5
VER=$(az storage container list --connection-string "$CONN" --include-deleted \
  --query "[?name=='facturas' && deleted].version | [0]" -o tsv 2>/dev/null || echo "")
if [ -n "$VER" ]; then
  az storage container restore --connection-string "$CONN" \
    --name facturas --deleted-version "$VER" --output none
  ok "Container 'facturas' restaurado (version $VER)"
else
  warn "No se encontró versión soft-deleted del container (puede tardar). Reintenta."
fi

rm -f "$TMP"
echo
ok "Walkthrough de recuperación completado (slide 6/19)"
