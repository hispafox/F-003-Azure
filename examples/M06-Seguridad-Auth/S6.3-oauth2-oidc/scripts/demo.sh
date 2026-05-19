#!/usr/bin/env bash
source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"
cd "$( dirname "${BASH_SOURCE[0]}" )"

while true; do
  echo
  echo "==================================================="
  echo " M06-S6.3 - OAuth2/OIDC: config (solo lectura)"
  echo "==================================================="
  echo " 1) Config OAuth de App Registrations (slide 5-8/17)"
  echo " 0) Salir"
  echo
  echo " (No crea recursos → no hay cleanup)"
  echo
  read -r -p "Opcion: " opt
  case "$opt" in
    1) ./01-oauth-config.sh ;;
    0) exit 0 ;;
    *) echo "Opcion no valida" ;;
  esac
done
