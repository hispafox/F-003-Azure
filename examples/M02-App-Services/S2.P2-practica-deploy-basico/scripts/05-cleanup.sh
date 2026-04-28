#!/usr/bin/env bash
# 05 — Cleanup (slide 19). Borra el RG entero.
# F1 no genera coste, pero limpiar siempre que termines la practica es
# buena higiene.

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

echo "Esto borrara TODO el Resource Group: $RG"
echo "  - Web App: $APP"
echo "  - Plan: $PLAN"
confirm "Estas seguro?"

step "Eliminando $RG (en background)"
az group delete --name "$RG" --yes --no-wait
ok "Eliminacion lanzada"
