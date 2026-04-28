#!/usr/bin/env bash
# demo.sh — menú interactivo para escenificar la demo completa de monitorización.

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

cd "$( dirname "${BASH_SOURCE[0]}" )"

while true; do
  echo
  echo "============================================"
  echo " M02-S2.5 — Demo de monitorización"
  echo "============================================"
  echo " 1) Provisionar (RG + plan + app + LAW + AI)   slide 11"
  echo " 2) Deploy"
  echo " 3) Conectar Application Insights              slides 11, 20"
  echo " 4) Crear Action Group (notificaciones email)  slide 26"
  echo " 5) Crear alertas (5xx, latencia, CPU)         slides 12, 27"
  echo " 6) Crear Availability test                    slide 19"
  echo " 7) Generar tráfico (5 min)"
  echo " 8) Imprimir queries KQL útiles                slide 16"
  echo " 9) Cleanup (borrar RG)"
  echo " 0) Salir"
  echo
  read -r -p "Opcion: " opt
  case "$opt" in
    1) ./01-provision.sh ;;
    2) ./02-deploy.sh ;;
    3) ./03-configure-app-insights.sh ;;
    4) ./04-create-action-group.sh ;;
    5) ./05-create-alerts.sh ;;
    6) ./06-create-availability-test.sh ;;
    7)
      read -r -p "  duración en minutos [5]: " mins
      mins="${mins:-5}"
      ./07-generate-traffic.sh "$mins"
      ;;
    8) ./08-show-kql-queries.sh ;;
    9) ./09-cleanup.sh ;;
    0) exit 0 ;;
    *) echo "Opción no válida" ;;
  esac
done
