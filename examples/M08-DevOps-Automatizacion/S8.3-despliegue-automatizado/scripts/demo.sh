#!/usr/bin/env bash
source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"
cd "$( dirname "${BASH_SOURCE[0]}" )"

while true; do
  echo
  echo "==========================================================="
  echo " M08-S8.3 - Despliegue automatizado (solo lectura)"
  echo "==========================================================="
  echo " 1) Inventario de deploy: slots + último deployment + sticky"
  echo " 0) Salir"
  echo
  echo " (No hace swap ni rollback → no hay cleanup)"
  echo
  read -r -p "Opcion: " opt
  case "$opt" in
    1) ./01-inventory-deploy.sh ;;
    0) exit 0 ;;
    *) echo "Opcion no valida" ;;
  esac
done
