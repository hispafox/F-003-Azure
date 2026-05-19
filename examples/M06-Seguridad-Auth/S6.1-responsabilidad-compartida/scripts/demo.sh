#!/usr/bin/env bash
source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"
cd "$( dirname "${BASH_SOURCE[0]}" )"

while true; do
  echo
  echo "==================================================="
  echo " M06-S6.1 - Seguridad: posture check (solo lectura)"
  echo "==================================================="
  echo " 1) Posture check (storage/sql/https — slide 4/7)"
  echo " 2) Secure Score + recomendaciones (slide 10/12)"
  echo " 0) Salir"
  echo
  echo " (No crea recursos → no hay cleanup)"
  echo
  read -r -p "Opcion: " opt
  case "$opt" in
    1) ./01-posture-check.sh ;;
    2) ./02-secure-score.sh ;;
    0) exit 0 ;;
    *) echo "Opcion no valida" ;;
  esac
done
