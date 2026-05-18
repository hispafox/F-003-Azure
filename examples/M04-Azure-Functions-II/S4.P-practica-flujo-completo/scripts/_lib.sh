#!/usr/bin/env bash
# Utilidades compartidas para los scripts del ejemplo.
# No ejecutar directamente -- se incluye con `source ./_lib.sh`.

set -euo pipefail

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[1]:-${BASH_SOURCE[0]}}" )" && pwd )"
ENV_FILE="$SCRIPT_DIR/.env.demo"
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
: "${STORAGE:?STORAGE no definido (cuenta requerida por el runtime de Functions)}"
: "${FUNC:?FUNC no definido}"
: "${COSMOS:?COSMOS no definido (nombre de la cuenta de Cosmos DB)}"
: "${COSMOS_DB:=tienda}"
: "${COSMOS_PEDIDOS:=pedidos}"
: "${QUEUE:=facturas-generadas}"
: "${BLOB_CONTAINER:=facturas}"

if ! command -v az >/dev/null 2>&1; then
  echo "[X] Azure CLI (az) no encontrado. https://aka.ms/InstallAzureCli"
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
