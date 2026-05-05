#!/usr/bin/env bash
# 04 - Cleanup. Borra el RG entero (incluye Function App y Storage).

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

echo "Esto borrara TODO el Resource Group: $RG"
echo "  - Function App: $FUNC"
echo "  - Storage Account: $STORAGE"
confirm "Continuar?"

step "Eliminando $RG (en background)"
az group delete --name "$RG" --yes --no-wait
ok "Borrado lanzado"
