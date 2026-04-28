#!/usr/bin/env bash
# 07 — Cleanup (slide 82). Borra el RG entero.
# F1 no genera coste, pero limpiar tras la práctica es buena higiene.

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

echo "Esto borrara TODO el Resource Group: $RG"
echo "  - Web App: $APP"
echo "  - Plan: $PLAN"
[ -n "${LAW:-}" ] && echo "  - Log Analytics: $LAW"
[ -n "${APPI:-}" ] && echo "  - Application Insights: $APPI"
echo
echo "ATENCION: el RG '$RG' es el que reutilizaras en M02-S2.P (slots-swap)."
echo "Si vas a hacer la siguiente practica, NO borres todo el RG: borra solo"
echo "la web app y el plan, manteniendo el RG."
echo
read -r -p "Borrar el RG entero (s) o solo la web app y el plan (a)? [s/a/N] " resp

case "$resp" in
  s|S)
    step "Eliminando $RG (en background)"
    az group delete --name "$RG" --yes --no-wait
    ok "Eliminacion lanzada"
    ;;
  a|A)
    step "Borrando solo la web app y el plan"
    az webapp delete --name "$APP" --resource-group "$RG" --output none 2>/dev/null || true
    az appservice plan delete --name "$PLAN" --resource-group "$RG" --yes --output none 2>/dev/null || true
    ok "Web app y plan borrados; RG conservado para M02-S2.P"
    ;;
  *)
    echo "Cancelado."
    ;;
esac
