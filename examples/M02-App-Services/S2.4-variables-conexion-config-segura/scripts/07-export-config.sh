#!/usr/bin/env bash
# 07 — Exporta Application Settings a JSON.
# Slide 13 — útil para replicar config entre entornos. Las KV references
# aparecen en el JSON con su sintaxis @Microsoft.KeyVault(...) — son punteros,
# no secretos en sí, así que es seguro versionar este export.

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

OUTFILE="$(dirname "${BASH_SOURCE[0]}")/../config-export-$(date +%Y%m%d-%H%M%S).json"

step "Exportando Application Settings"
az webapp config appsettings list \
  --name "$APP" --resource-group "$RG" \
  --output json > "$OUTFILE"

ok "Config exportada a $OUTFILE"
echo
echo "Para importar en otro entorno:"
echo "  az webapp config appsettings set --name <otra-app> -g <otro-rg> --settings @\"$OUTFILE\""
