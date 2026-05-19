#!/usr/bin/env bash
source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"
cd "$( dirname "${BASH_SOURCE[0]}" )"

while true; do
  echo
  echo "==================================================="
  echo " M06-S6.2 - Entra ID: inventario (solo lectura)"
  echo "==================================================="
  echo " 1) Directorio (tenant/usuarios/grupos — slide 3-5)"
  echo " 2) App Registrations / Service Principals (slide 8-9)"
  echo " 0) Salir"
  echo
  echo " (No crea recursos → no hay cleanup)"
  echo
  read -r -p "Opcion: " opt
  case "$opt" in
    1) ./01-directory-inventory.sh ;;
    2) ./02-app-registrations.sh ;;
    0) exit 0 ;;
    *) echo "Opcion no valida" ;;
  esac
done
