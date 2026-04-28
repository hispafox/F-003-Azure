#!/usr/bin/env bash
# 09 — Borra el RG entero (incluye plan, app, App Insights, Log Analytics,
# alertas, action group, availability tests).

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

echo "Esto borrará TODO el Resource Group: $RG"
echo "  - Web App: $APP"
echo "  - Plan: $PLAN"
echo "  - Application Insights: $AI"
echo "  - Log Analytics: $LAW"
echo "  - Action Group: $ACTION_GROUP"
echo "  - Alertas y availability tests asociados"
confirm "¿Estás seguro?"

step "Eliminando $RG (en background)"
az group delete --name "$RG" --yes --no-wait
ok "Eliminación lanzada"
