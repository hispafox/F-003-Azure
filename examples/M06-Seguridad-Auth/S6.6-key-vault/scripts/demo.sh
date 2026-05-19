#!/usr/bin/env bash
source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"
cd "$( dirname "${BASH_SOURCE[0]}" )"

while true; do
  echo
  echo "==================================================="
  echo " M06-S6.6 - Key Vault: inventario (solo lectura)"
  echo "==================================================="
  echo " 1) Inventario: RBAC/purge/secretos (slide 4-9)"
  echo " 0) Salir"
  echo
  echo " (No crea recursos → no hay cleanup; nunca lee valores)"
  echo
  read -r -p "Opcion: " opt
  case "$opt" in
    1) ./01-kv-inventory.sh ;;
    0) exit 0 ;;
    *) echo "Opcion no valida" ;;
  esac
done
