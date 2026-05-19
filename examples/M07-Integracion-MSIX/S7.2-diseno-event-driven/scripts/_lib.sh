#!/usr/bin/env bash
# Utilidades compartidas. No ejecutar directamente -- `source ./_lib.sh`.
# Scripts SOLO LECTURA: inventarían la arquitectura event-driven de
# referencia (slide 12). No crean nada (sin cleanup).
#
# COSTE: Service Bus Standard ~10 €/mes FIJOS. Cosmos serverless ≈ 0 €.
# Borra los recursos desde el Portal al acabar la práctica.

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
: "${SB_NAMESPACE:?SB_NAMESPACE no definido}"
: "${SB_TOPIC:?SB_TOPIC no definido}"
: "${COSMOS_ACCOUNT:?COSMOS_ACCOUNT no definido}"

if ! command -v az >/dev/null 2>&1; then
  echo "[X] Azure CLI (az) no encontrado. https://aka.ms/InstallAzureCli"
  exit 1
fi

az account set --subscription "$SUBSCRIPTION_ID" >/dev/null

step() { echo "[>] $*"; }
ok()   { echo "[OK] $*"; }
warn() { echo "[!] $*"; }
