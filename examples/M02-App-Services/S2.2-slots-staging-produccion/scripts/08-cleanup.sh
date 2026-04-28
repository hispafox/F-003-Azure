#!/usr/bin/env bash
# 08 — Borra el Resource Group completo (incluye plan, app, slots, todo).

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

echo "Esto borrará TODO el Resource Group: $RG"
confirm "¿Estás seguro?"

step "Eliminando $RG (en background)"
az group delete --name "$RG" --yes --no-wait
ok "Eliminación lanzada (puede tardar varios minutos)"
