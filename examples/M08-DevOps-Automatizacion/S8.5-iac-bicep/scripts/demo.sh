#!/usr/bin/env bash
source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"
cd "$( dirname "${BASH_SOURCE[0]}" )"

while true; do
  echo
  echo "==========================================================="
  echo " M08-S8.5 - IaC con Bicep (solo lectura)"
  echo "==========================================================="
  echo " 1) bicep build + az validate + az what-if (preview, slide 5/14)"
  echo " 0) Salir"
  echo
  echo " (No aplica cambios → no hay cleanup)"
  echo
  read -r -p "Opcion: " opt
  case "$opt" in
    1) ./01-validate-bicep.sh ;;
    0) exit 0 ;;
    *) echo "Opcion no valida" ;;
  esac
done
