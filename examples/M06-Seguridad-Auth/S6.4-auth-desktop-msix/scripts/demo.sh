#!/usr/bin/env bash
source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"
cd "$( dirname "${BASH_SOURCE[0]}" )"

while true; do
  echo
  echo "==================================================="
  echo " M06-S6.4 - Auth desktop/MSIX: config (solo lectura)"
  echo "==================================================="
  echo " 1) Config apps cliente público / redirect URIs (slide 4/7/11)"
  echo " 0) Salir"
  echo
  echo " (No crea recursos → no hay cleanup)"
  echo
  read -r -p "Opcion: " opt
  case "$opt" in
    1) ./01-desktop-app-config.sh ;;
    0) exit 0 ;;
    *) echo "Opcion no valida" ;;
  esac
done
