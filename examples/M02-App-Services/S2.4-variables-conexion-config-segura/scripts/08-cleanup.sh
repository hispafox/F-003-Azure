#!/usr/bin/env bash
# 08 — Borra el RG entero (incluye plan, app, KV, role assignments).
# Importante: el KV se borra con purge protection desactivado por defecto.
# Para producción, activa --enable-purge-protection en la creación.

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

echo "Esto borrará TODO el Resource Group: $RG"
echo "  - Web App: $APP"
echo "  - Plan: $PLAN"
echo "  - Key Vault: $KV (con sus secretos)"
confirm "¿Estás seguro?"

step "Eliminando $RG (en background)"
az group delete --name "$RG" --yes --no-wait
ok "Eliminación lanzada"

echo
echo "Si más adelante no puedes recrear el KV con el mismo nombre:"
echo "  az keyvault purge --name $KV --location $LOCATION"
