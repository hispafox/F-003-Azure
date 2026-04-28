#!/usr/bin/env bash
# demo.sh — menú interactivo para escenificar Key Vault + config segura.

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

cd "$( dirname "${BASH_SOURCE[0]}" )"

while true; do
  echo
  echo "============================================"
  echo " M02-S2.4 — Demo de configuración segura"
  echo "============================================"
  echo " 1) Provisionar (RG + plan + app + KV)        slide 25"
  echo " 2) Deploy"
  echo " 3) Configurar App Settings (sin secrets)     slides 4, 6, 8"
  echo " 4) Configurar Key Vault (MI + roles + secrets)  slides 9, 25"
  echo " 5) App Settings con KV references             slide 9"
  echo " 6) Rotar ApiKey en KV                          slide 26"
  echo " 7) Exportar config a JSON                       slide 13"
  echo " 8) Cleanup (borrar RG)"
  echo " 0) Salir"
  echo
  read -r -p "Opcion: " opt
  case "$opt" in
    1) ./01-provision.sh ;;
    2) ./02-deploy.sh ;;
    3) ./03-configure-app-settings.sh ;;
    4) ./04-configure-keyvault.sh ;;
    5) ./05-configure-keyvault-references.sh ;;
    6) ./06-rotate-secret.sh ;;
    7) ./07-export-config.sh ;;
    8) ./08-cleanup.sh ;;
    0) exit 0 ;;
    *) echo "Opcion no valida" ;;
  esac
done
