#!/usr/bin/env bash
# 08 — Diff de App Settings entre producción y staging (slide 14).
# Útil cuando algo va raro post-swap y necesitas saber qué se diferencia.
# Requiere `jq` y `diff` (vienen con Git Bash).

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

if ! command -v jq >/dev/null 2>&1; then
  echo "[X] jq no encontrado. Instálalo o ejecuta los az manualmente."
  exit 1
fi

TMP_PROD=$(mktemp)
TMP_STAGING=$(mktemp)

step "Exportando settings de producción"
az webapp config appsettings list --name "$APP" -g "$RG" \
  --output json > "$TMP_PROD"

step "Exportando settings de staging"
az webapp config appsettings list --name "$APP" -g "$RG" --slot staging \
  --output json > "$TMP_STAGING"

step "Diff producción vs staging:"
echo
diff <(jq -r '.[] | "\(.name)=\(.value)"' "$TMP_PROD" | sort) \
     <(jq -r '.[] | "\(.name)=\(.value)"' "$TMP_STAGING" | sort) || true

rm -f "$TMP_PROD" "$TMP_STAGING"

echo
echo "Líneas con < pertenecen sólo a producción; líneas con > sólo a staging."
echo "Si una variable que esperabas igual aparece distinta, es un bug de"
echo "configuración (¿está marcada como sticky cuando no debería?)."
