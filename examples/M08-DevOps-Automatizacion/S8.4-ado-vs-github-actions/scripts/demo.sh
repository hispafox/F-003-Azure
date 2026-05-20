#!/usr/bin/env bash
source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"
cd "$( dirname "${BASH_SOURCE[0]}" )"

while true; do
  echo
  echo "==========================================================="
  echo " M08-S8.4 - ADO vs GitHub Actions (solo lectura)"
  echo "==========================================================="
  echo " 1) Preflight: ¿tengo az+devops y gh listos?"
  echo " 0) Salir"
  echo
  echo " (No crea recursos → no hay cleanup)"
  echo
  read -r -p "Opcion: " opt
  case "$opt" in
    1) ./01-preflight-platforms.sh ;;
    0) exit 0 ;;
    *) echo "Opcion no valida" ;;
  esac
done
