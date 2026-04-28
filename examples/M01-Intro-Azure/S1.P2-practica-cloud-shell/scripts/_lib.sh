#!/usr/bin/env bash
# Utilidades compartidas para los scripts de la practica Cloud Shell.
# No ejecutar directamente -- se incluye con `source ./_lib.sh`.

set -euo pipefail

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[1]:-${BASH_SOURCE[0]}}" )" && pwd )"
ENV_FILE="$SCRIPT_DIR/.env.demo"

# Si no existe en scripts/, prueba en el padre (consistente con otros ejemplos)
[ -f "$ENV_FILE" ] || ENV_FILE="$SCRIPT_DIR/../.env.demo"

if [ ! -f "$ENV_FILE" ]; then
  echo "[X] Falta .env.demo"
  echo "    Copia scripts/.env.demo.example a scripts/.env.demo y rellenalo."
  exit 1
fi

set -a
# shellcheck disable=SC1090
source "$ENV_FILE"
set +a

: "${SUBSCRIPTION_ID:?SUBSCRIPTION_ID no definido en .env.demo}"
: "${LOCATION:?LOCATION no definido}"
: "${RG:?RG no definido}"

if ! command -v az >/dev/null 2>&1; then
  echo "[X] Azure CLI (az) no encontrado."
  echo "    En Cloud Shell viene preinstalado; en local: https://aka.ms/InstallAzureCli"
  exit 1
fi

az account set --subscription "$SUBSCRIPTION_ID" >/dev/null

step()    { echo "[>] $*"; }
ok()      { echo "[OK] $*"; }
warn()    { echo "[!] $*"; }
confirm() {
  local msg="${1:-Continuar?}"
  read -r -p "$msg [s/N] " resp
  [[ "$resp" =~ ^[sSyY]$ ]] || { echo "Cancelado."; exit 1; }
}
