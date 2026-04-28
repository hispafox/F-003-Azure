#!/usr/bin/env bash
# demo.sh — menu interactivo para escenificar la practica.

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

cd "$( dirname "${BASH_SOURCE[0]}" )"

while true; do
  echo
  echo "============================================"
  echo " M02-S2.P2 — Practica de deploy basico"
  echo "============================================"
  echo " 1) Provisionar (RG + plan F1 + web app)        slides 7, 8, 9"
  echo " 2) Deploy (publish + zip + zip deploy)         slide 10"
  echo " 3) Configurar App Settings                     slide 14"
  echo " 4) Smoke tests                                  slide 15"
  echo " 5) Log streaming (Ctrl+C para parar)            slide 12"
  echo " 6) Cleanup (borrar RG)                          slide 19"
  echo " 0) Salir"
  echo
  read -r -p "Opcion: " opt
  case "$opt" in
    1) ./01-provision.sh ;;
    2) ./02-deploy.sh ;;
    3) ./03-app-settings.sh ;;
    4) ./04-smoke-test.sh ;;
    5) az webapp log tail --name "$APP" --resource-group "$RG" ;;
    6) ./05-cleanup.sh ;;
    0) exit 0 ;;
    *) echo "Opcion no valida" ;;
  esac
done
