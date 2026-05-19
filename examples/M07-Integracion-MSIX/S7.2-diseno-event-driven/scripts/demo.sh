#!/usr/bin/env bash
source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"
cd "$( dirname "${BASH_SOURCE[0]}" )"

while true; do
  echo
  echo "==========================================================="
  echo " M07-S7.2 - Diseño event-driven (solo lectura)"
  echo "==========================================================="
  echo " 1) Inventariar arquitectura: topic+subs, DLQ, Outbox (slide 12)"
  echo " 0) Salir"
  echo
  echo " (No crea recursos → no hay cleanup)"
  echo " ⚠️  Service Bus Standard ~10 €/mes — borra los recursos al acabar"
  echo
  read -r -p "Opcion: " opt
  case "$opt" in
    1) ./01-verify-eventdriven.sh ;;
    0) exit 0 ;;
    *) echo "Opcion no valida" ;;
  esac
done
