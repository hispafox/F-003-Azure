#!/usr/bin/env bash
# Utilidades compartidas. No ejecutar directamente -- `source ./_lib.sh`.
# Scripts SOLO LECTURA: inventarían una instancia APIM existente. No
# crean nada (sin cleanup).
#
# COSTE: APIM Consumption 0 € base (1M/mes gratis). Developer ~40 €/mes,
# Standard ~550 €/mes, Premium ~2200 €/mes. Borra los tiers de pago
# desde el Portal al acabar.

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
: "${RG:?RG no definido}"
: "${APIM_NAME:?APIM_NAME no definido}"

if ! command -v az >/dev/null 2>&1; then
  echo "[X] Azure CLI (az) no encontrado. https://aka.ms/InstallAzureCli"
  exit 1
fi

az account set --subscription "$SUBSCRIPTION_ID" >/dev/null

step() { echo "[>] $*"; }
ok()   { echo "[OK] $*"; }
warn() { echo "[!] $*"; }
