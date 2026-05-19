#!/usr/bin/env bash
# 03 - Cleanup. Borra el RG entero (slide 16: limpiar siempre).

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

echo "Esto borrara TODO el Resource Group: $RG"
confirm "Continuar?"
step "Eliminando $RG (en background)"
az group delete --name "$RG" --yes --no-wait
ok "Borrado lanzado"
