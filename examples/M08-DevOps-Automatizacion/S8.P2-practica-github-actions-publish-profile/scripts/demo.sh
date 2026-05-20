#!/usr/bin/env bash
source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"
cd "$( dirname "${BASH_SOURCE[0]}" )"

while true; do
  echo
  echo "==========================================================="
  echo " M08-S8.P2 - GitHub Actions + publish profile (solo lectura)"
  echo "==========================================================="
  echo " 1) Descargar publish profile (slide 7)"
  echo " 2) Listar runs del workflow + smoke al / (slide 10/12)"
  echo " 0) Salir"
  echo
  echo " (No aplica cambios -> no hay cleanup)"
  echo
  read -r -p "Opcion: " opt
  case "$opt" in
    1) ./01-publish-profile.sh ;;
    2) ./02-runs.sh ;;
    0) exit 0 ;;
    *) echo "Opcion no valida" ;;
  esac
done
