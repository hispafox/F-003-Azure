#!/usr/bin/env bash
# demo.sh — menú interactivo para escenificar la demo de autoscale en clase.

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

cd "$( dirname "${BASH_SOURCE[0]}" )"

while true; do
  echo
  echo "============================================"
  echo " M02-S2.3 — Demo de escalado — $APP"
  echo "============================================"
  echo " 1) Provisionar (RG + plan S1 + app)            slide 5"
  echo " 2) Deploy"
  echo " 3) Scale up (cambiar SKU)                      slide 3"
  echo " 4) Scale out manual (N instancias)             slide 4"
  echo " 5) Autoscale por CPU (1-5, 30-70%)             slides 5, 6, 7"
  echo " 6) Anadir perfil horario al autoscale          slides 8, 23"
  echo " 7) Generar carga (load test)                   disparar autoscale"
  echo " 8) Vigilar instanceId en directo               slides 4, 10"
  echo " 9) Cleanup (borrar RG)"
  echo " 0) Salir"
  echo
  read -r -p "Opcion: " opt
  case "$opt" in
    1) ./01-provision.sh ;;
    2) ./02-deploy.sh ;;
    3)
      read -r -p "  SKU (B1/S1/S2/P1V3): " sku
      ./03-scale-up.sh "$sku"
      ;;
    4)
      read -r -p "  numero de instancias (1-30): " n
      ./04-scale-out-manual.sh "$n"
      ;;
    5) ./05-autoscale-cpu.sh ;;
    6) ./06-autoscale-schedule.sh ;;
    7)
      read -r -p "  duracion en minutos [7]: " mins
      mins="${mins:-7}"
      ./07-load-test.sh "$mins"
      ;;
    8) ./08-watch-instances.sh ;;
    9) ./09-cleanup.sh ;;
    0) exit 0 ;;
    *) echo "Opcion no valida" ;;
  esac
done
