#!/usr/bin/env bash
source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"
cd "$( dirname "${BASH_SOURCE[0]}" )"

while true; do
  echo
  echo "==========================================================="
  echo " M07-S7.1 - Service Bus / Event Grid avanzado (solo lectura)"
  echo "==========================================================="
  echo " 1) Verificar: namespace, topic+subs, filtros SQL, dedup, DLQ"
  echo " 0) Salir"
  echo
  echo " (No crea recursos → no hay cleanup)"
  echo " ⚠️  Service Bus Standard ~10 €/mes — borra el namespace al acabar"
  echo
  read -r -p "Opcion: " opt
  case "$opt" in
    1) ./01-verify-messaging.sh ;;
    0) exit 0 ;;
    *) echo "Opcion no valida" ;;
  esac
done
