#!/usr/bin/env bash
source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"
cd "$( dirname "${BASH_SOURCE[0]}" )"

while true; do
  echo
  echo "============================================"
  echo " M05-S5.5 - Backups y DR (soft delete)"
  echo "============================================"
  echo " 1) Provisionar (Storage + soft delete + versioning)"
  echo " 2) Walkthrough de recuperación (borrar/undelete, slide 19)"
  echo " 3) Cleanup"
  echo " 0) Salir"
  echo
  read -r -p "Opcion: " opt
  case "$opt" in
    1) ./01-provision.sh ;;
    2) ./02-smoke-test.sh ;;
    3) ./03-cleanup.sh ;;
    0) exit 0 ;;
    *) echo "Opcion no valida" ;;
  esac
done
