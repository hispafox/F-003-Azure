#!/usr/bin/env bash
source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"
cd "$( dirname "${BASH_SOURCE[0]}" )"

while true; do
  echo
  echo "==================================================="
  echo " M06-S6.5 - Seguridad de datos (solo lectura)"
  echo "==================================================="
  echo " 1) Postura de cifrado: TLS / HTTPS / TDE (slide 3/5/8/14)"
  echo " 0) Salir"
  echo
  echo " (No crea recursos → no hay cleanup)"
  echo
  read -r -p "Opcion: " opt
  case "$opt" in
    1) ./01-data-security-check.sh ;;
    0) exit 0 ;;
    *) echo "Opcion no valida" ;;
  esac
done
