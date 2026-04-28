#!/usr/bin/env bash
# demo.sh — menú interactivo para escenificar la práctica completa.

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

cd "$( dirname "${BASH_SOURCE[0]}" )"

while true; do
  echo
  echo "============================================"
  echo " M02-S2.P — Práctica de slots y swap"
  echo "============================================"
  echo " 1) Provisionar (RG + plan B1 + app)            slide 4"
  echo " 2) Deploy v1 a producción                      slide 7"
  echo " 3) Upgrade plan B1 -> S1 + crear slot staging  slides 4, 5, 6, 9"
  echo " 4) Deploy v2 al slot staging                   slide 8"
  echo " 5) Smoke test sobre staging                    slide 11"
  echo " 6) Swap staging -> production                  slide 10"
  echo " 7) Smoke test sobre producción tras swap"
  echo " 8) Rollback (swap inverso)                     slide 12"
  echo " 9) Slot diff                                   slide 14"
  echo "10) Cleanup (borrar slot + bajar plan)          slide 13"
  echo " 0) Salir"
  echo
  read -r -p "Opcion: " opt
  case "$opt" in
    1) ./01-provision.sh ;;
    2) ./02-deploy-as-v1.sh ;;
    3) ./03-upgrade-plan-and-create-slot.sh ;;
    4) ./04-deploy-v2-to-staging.sh ;;
    5) ./05-smoke-test.sh staging 2.0 ;;
    6) ./06-swap.sh ;;
    7) ./05-smoke-test.sh production 2.0 ;;
    8) ./07-rollback.sh ;;
    9) ./08-slot-diff.sh ;;
    10) ./09-cleanup.sh ;;
    0) exit 0 ;;
    *) echo "Opcion no valida" ;;
  esac
done
