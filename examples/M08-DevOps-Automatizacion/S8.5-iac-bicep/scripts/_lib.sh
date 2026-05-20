#!/usr/bin/env bash
# Utilidades S8.5. SOLO LECTURA: bicep build local + what-if (preview)
# contra un rg existente. NO ejecuta `az deployment group create`.

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

: "${SUBSCRIPTION_ID:?SUBSCRIPTION_ID no definido}"
: "${RG:?RG no definido}"
: "${BICEP_FILE:?BICEP_FILE no definido}"

if ! command -v az >/dev/null 2>&1; then
  echo "[X] Azure CLI (az) no encontrado. https://aka.ms/InstallAzureCli"
  exit 1
fi

az account set --subscription "$SUBSCRIPTION_ID" >/dev/null

# Bicep CLI viene con az; si no está instalado, az lo instala bajo
# demanda (`az bicep install`).
if ! az bicep version >/dev/null 2>&1; then
  echo "[!] Instalando bicep (una vez)..."
  az bicep install >/dev/null
fi

step() { echo "[>] $*"; }
ok()   { echo "[OK] $*"; }
warn() { echo "[!] $*"; }
