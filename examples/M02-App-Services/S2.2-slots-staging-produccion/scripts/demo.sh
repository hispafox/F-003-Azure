#!/usr/bin/env bash
# demo.sh — menú interactivo para escenificar todos los conceptos del submódulo
# durante una clase. Cada opción ejecuta uno de los scripts numerados.

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

cd "$( dirname "${BASH_SOURCE[0]}" )"

while true; do
  echo
  echo "============================================"
  echo " M02-S2.2 — Demo de slots — $APP"
  echo "============================================"
  echo " 1) Provisionar (RG + plan S1 + app + slot)   slide 5"
  echo " 2) Configurar settings (sticky vs travel)    slides 8, 9"
  echo " 3) Deploy a producción"
  echo " 4) Deploy a staging                          slide 6"
  echo " 5) Swap directo staging -> production        slides 10, 11"
  echo " 6) Swap con preview (multi-fase)             slide 12"
  echo " 7) Traffic routing (canary)                  slide 14"
  echo " 8) Proteger staging por IP                   slide 17"
  echo " 9) Cleanup (borrar RG entero)"
  echo " 0) Salir"
  echo
  read -r -p "Opción: " opt
  case "$opt" in
    1) ./01-provision.sh ;;
    2) ./02-configure-settings.sh ;;
    3) ./03-deploy.sh production ;;
    4) ./03-deploy.sh staging ;;
    5) ./04-swap.sh ;;
    6)
      read -r -p "  preview / complete / reset: " phase
      ./05-swap-with-preview.sh "$phase"
      ;;
    7)
      read -r -p "  porcentaje a staging (0-100): " pct
      ./06-traffic-routing.sh "$pct"
      ;;
    8)
      read -r -p "  ip CIDR (o 'open' para quitar): " ipcidr
      ./07-protect-staging.sh "$ipcidr"
      ;;
    9) ./08-cleanup.sh ;;
    0) exit 0 ;;
    *) echo "Opción no válida" ;;
  esac
done
