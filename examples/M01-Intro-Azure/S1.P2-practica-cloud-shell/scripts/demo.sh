#!/usr/bin/env bash
# demo.sh - menu interactivo para escenificar la practica completa.

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

cd "$( dirname "${BASH_SOURCE[0]}" )"

while true; do
  echo
  echo "============================================"
  echo " M01-S1.P2 - Practica Cloud Shell"
  echo "============================================"
  echo " 1) Crear Resource Group + tags        slide 7"
  echo " 2) Crear Storage Account              slide 8"
  echo " 3) Container + upload + download blob slide 9"
  echo " 4) JMESPath cheat-sheet (6 queries)   slide 10"
  echo " 5) Coste con az consumption           slide 11"
  echo " 6) Smoke tests (5 checks)             slide 13"
  echo " 7) Cleanup                             slide 15"
  echo " --- retos opcionales (slide 20) ---"
  echo " 8) Reto 1: varios RGs con tags"
  echo " 9) Reto 2: reporte Markdown"
  echo "10) Reto 3: clonar repo cli-samples"
  echo "11) Reto 4: SAS token de 1 hora"
  echo " 0) Salir"
  echo
  read -r -p "Opcion: " opt
  case "$opt" in
    1)  ./01-provision-rg.sh ;;
    2)  ./02-create-storage.sh ;;
    3)  ./03-upload-blob.sh ;;
    4)  ./04-jmespath-queries.sh ;;
    5)  ./05-show-costs.sh ;;
    6)  ./06-smoke-tests.sh ;;
    7)  ./07-cleanup.sh ;;
    8)  ./extras/reto-1-multiple-rgs.sh ;;
    9)  ./extras/reto-2-markdown-report.sh ;;
    10) ./extras/reto-3-clone-repo.sh ;;
    11) ./extras/reto-4-sas-token.sh ;;
    0)  exit 0 ;;
    *)  echo "Opcion no valida" ;;
  esac
done
