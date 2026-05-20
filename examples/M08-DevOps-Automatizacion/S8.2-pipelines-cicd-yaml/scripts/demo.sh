#!/usr/bin/env bash
source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"
cd "$( dirname "${BASH_SOURCE[0]}" )"

while true; do
  echo
  echo "==========================================================="
  echo " M08-S8.2 - Pipelines CI/CD YAML (solo lectura)"
  echo "==========================================================="
  echo " 1) Inventariar pipelines + últimas runs + environments"
  echo " 0) Salir"
  echo
  echo " (No lanza ni cancela runs → no hay cleanup)"
  echo
  read -r -p "Opcion: " opt
  case "$opt" in
    1) ./01-inventory-pipelines.sh ;;
    0) exit 0 ;;
    *) echo "Opcion no valida" ;;
  esac
done
