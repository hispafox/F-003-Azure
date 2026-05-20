#!/usr/bin/env bash
source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"
cd "$( dirname "${BASH_SOURCE[0]}" )"

while true; do
  echo
  echo "==========================================================="
  echo " M08-S8.1 - Azure DevOps Repos/Boards/Artifacts (solo lectura)"
  echo "==========================================================="
  echo " 1) Inventariar: repos, branch policies, work items, feeds"
  echo " 0) Salir"
  echo
  echo " (No crea recursos → no hay cleanup)"
  echo
  read -r -p "Opcion: " opt
  case "$opt" in
    1) ./01-inventory-devops.sh ;;
    0) exit 0 ;;
    *) echo "Opcion no valida" ;;
  esac
done
