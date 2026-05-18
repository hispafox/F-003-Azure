#!/usr/bin/env bash
source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"
cd "$( dirname "${BASH_SOURCE[0]}" )"

while true; do
  echo
  echo "============================================"
  echo " M05-S5.4 - Managed Identity (sin secretos)"
  echo "============================================"
  echo " 1) Provisionar (App + Storage + MI + rol RBAC mínimo)"
  echo " 2) Smoke test (verificar MI + RBAC)"
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
