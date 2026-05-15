#!/usr/bin/env bash
source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"
cd "$( dirname "${BASH_SOURCE[0]}" )"

while true; do
  echo
  echo "============================================"
  echo " M04-S4.2 - Durable Functions"
  echo "============================================"
  echo " 1) Provisionar (RG + Storage + Function App)"
  echo " 2) Deploy"
  echo " 3) Smoke test (3 sagas + fan-out de facturas)"
  echo " 4) Log streaming (Ctrl+C para parar)"
  echo " 5) Cleanup"
  echo " 0) Salir"
  echo
  read -r -p "Opcion: " opt
  case "$opt" in
    1) ./01-provision.sh ;;
    2) ./02-deploy.sh ;;
    3) ./03-smoke-test.sh ;;
    4) az functionapp log tail --name "$FUNC" --resource-group "$RG" ;;
    5) ./04-cleanup.sh ;;
    0) exit 0 ;;
    *) echo "Opcion no valida" ;;
  esac
done
