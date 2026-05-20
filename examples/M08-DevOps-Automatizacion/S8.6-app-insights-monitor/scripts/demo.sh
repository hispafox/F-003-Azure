#!/usr/bin/env bash
source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"
cd "$( dirname "${BASH_SOURCE[0]}" )"

while true; do
  echo
  echo "==========================================================="
  echo " M08-S8.6 - App Insights y Monitor (solo lectura)"
  echo "==========================================================="
  echo " 1) Ejecutar queries KQL canonicas (slide 5/26)"
  echo " 2) Listar alertas + action groups (slide 8)"
  echo " 0) Salir"
  echo
  echo " (No aplica cambios -> no hay cleanup)"
  echo
  read -r -p "Opcion: " opt
  case "$opt" in
    1) ./01-query-kql.sh ;;
    2) ./02-alertas-listar.sh ;;
    0) exit 0 ;;
    *) echo "Opcion no valida" ;;
  esac
done
