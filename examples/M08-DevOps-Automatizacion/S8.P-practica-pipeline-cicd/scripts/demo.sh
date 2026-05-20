#!/usr/bin/env bash
source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"
cd "$( dirname "${BASH_SOURCE[0]}" )"

while true; do
  echo
  echo "==========================================================="
  echo " M08-S8.P - Practica Pipeline CI/CD (solo lectura)"
  echo "==========================================================="
  echo " 1) Preflight contra Azure (plan, slot, deploys, slide 3)"
  echo " 2) Smoke test contra slot staging (slide 5/10)"
  echo " 3) Smoke test contra production (slide 6/10)"
  echo " 0) Salir"
  echo
  echo " (No aplica cambios -> no hay cleanup)"
  echo
  read -r -p "Opcion: " opt
  case "$opt" in
    1) ./01-preflight.sh ;;
    2) ./02-smoke-test.sh staging ;;
    3) ./02-smoke-test.sh production ;;
    0) exit 0 ;;
    *) echo "Opcion no valida" ;;
  esac
done
