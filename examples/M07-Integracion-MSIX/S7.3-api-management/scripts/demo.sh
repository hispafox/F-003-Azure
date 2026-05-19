#!/usr/bin/env bash
source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"
cd "$( dirname "${BASH_SOURCE[0]}" )"

while true; do
  echo
  echo "==========================================================="
  echo " M07-S7.3 - Azure API Management (solo lectura)"
  echo "==========================================================="
  echo " 1) Inventariar APIM: tier, APIs, products, métricas (slide 3-13)"
  echo " 0) Salir"
  echo
  echo " (No crea recursos → no hay cleanup)"
  echo " ⚠️  Consumption 0 €; Developer/Standard/Premium = €€ — borra al acabar"
  echo
  read -r -p "Opcion: " opt
  case "$opt" in
    1) ./01-verify-apim.sh ;;
    0) exit 0 ;;
    *) echo "Opcion no valida" ;;
  esac
done
