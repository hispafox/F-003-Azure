#!/usr/bin/env bash
# 07 - Cleanup: borra el RG entero (slide 15).
# Borrar el RG elimina TODO lo que contiene (storage, blobs, role
# assignments creadas en el scope del storage, etc.).

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

echo "Esto borrara TODO el Resource Group: $RG"
echo "  (incluye storage, blobs y role assignments del storage)"
echo
echo "El Storage de Cloud Shell (cloud-shell-storage-*) NO se borra:"
echo "  es tu almacenamiento persistente para futuras sesiones."
echo
confirm "Continuar?"

step "Lanzando borrado del RG (en background)"
az group delete --name "$RG" --yes --no-wait
ok "Borrado lanzado. Tarda 1-2 min en completar."
echo
echo "Para verificar mas tarde:"
echo "  az group show --name $RG  -> debe devolver 'ResourceGroupNotFound'"
