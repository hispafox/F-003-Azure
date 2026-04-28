#!/usr/bin/env bash
# demo.sh — menu interactivo para escenificar la practica completa.

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

cd "$( dirname "${BASH_SOURCE[0]}" )"

while true; do
  echo
  echo "============================================"
  echo " M01-S1.P — Practica Hello World"
  echo "============================================"
  echo " 1) Provisionar (RG + plan F1 + web app)        slides 14, 22, 23"
  echo " 2) Deploy (publish + zip + zip deploy)         slide 46"
  echo " 3) Configurar App Settings                     slide 69"
  echo " 4) Smoke tests                                  slide 60"
  echo " 5) Application Insights (opcional)              slides 55-58"
  echo " 6) Security defaults (opcional)                 slide 59"
  echo " 7) Log streaming (Ctrl+C para parar)            slide 52"
  echo " 8) Cleanup                                      slide 82"
  echo " 0) Salir"
  echo
  read -r -p "Opcion: " opt
  case "$opt" in
    1) ./01-provision.sh ;;
    2) ./02-deploy.sh ;;
    3) ./03-app-settings.sh ;;
    4) ./04-smoke-test.sh ;;
    5) ./05-setup-app-insights.sh ;;
    6) ./06-secure-defaults.sh ;;
    7) az webapp log tail --name "$APP" --resource-group "$RG" ;;
    8) ./07-cleanup.sh ;;
    0) exit 0 ;;
    *) echo "Opcion no valida" ;;
  esac
done
