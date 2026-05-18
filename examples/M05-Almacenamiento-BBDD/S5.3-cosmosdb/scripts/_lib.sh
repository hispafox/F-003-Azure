#!/usr/bin/env bash
# Utilidades compartidas. No ejecutar directamente -- `source ./_lib.sh`.

set -euo pipefail

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[1]:-${BASH_SOURCE[0]}}" )" && pwd )"
ENV_FILE="$SCRIPT_DIR/.env.demo"
[ -f "$ENV_FILE" ] || ENV_FILE="$SCRIPT_DIR/../.env.demo"

if [ ! -f "$ENV_FILE" ]; then
  echo "[X] Falta .env.demo — copia .env.demo.example a .env.demo y rellenalo."
  exit 1
fi

set -a
# shellcheck disable=SC1090
source "$ENV_FILE"
set +a

: "${SUBSCRIPTION_ID:?SUBSCRIPTION_ID no definido en .env.demo}"
: "${LOCATION:?LOCATION no definido}"
: "${RG:?RG no definido}"
: "${COSMOS_ACCOUNT:?COSMOS_ACCOUNT no definido}"
: "${COSMOS_DB:?COSMOS_DB no definido}"
: "${COSMOS_CONTAINER:?COSMOS_CONTAINER no definido}"

if ! command -v az >/dev/null 2>&1; then
  echo "[X] Azure CLI (az) no encontrado. https://aka.ms/InstallAzureCli"
  exit 1
fi

az account set --subscription "$SUBSCRIPTION_ID" >/dev/null

# Connection string con key para los scripts. La app real debería usar
# Managed Identity (sin key) — slide 15 / M05-S5.4; el README lo explica.
cosmos_conn_string() {
  az cosmosdb keys list --type connection-strings \
    --name "$COSMOS_ACCOUNT" --resource-group "$RG" \
    --query "connectionStrings[0].connectionString" -o tsv
}

step() { echo "[>] $*"; }
ok()   { echo "[OK] $*"; }
warn() { echo "[!] $*"; }
confirm() {
  local msg="${1:-Continuar?}"
  read -r -p "$msg [s/N] " resp
  [[ "$resp" =~ ^[sSyY]$ ]] || { echo "Cancelado."; exit 1; }
}
