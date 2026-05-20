#!/usr/bin/env bash
# Utilidades compartidas para los scripts de S8.1. Scripts SOLO
# LECTURA: inventarían el Azure DevOps project (repos, branch policies,
# work items, feeds). No crean nada (sin cleanup).
#
# Requiere la extensión `azure-devops` de az CLI:
#   az extension add --name azure-devops

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

: "${ORG_URL:?ORG_URL no definido en .env.demo}"
: "${PROJECT:?PROJECT no definido}"

if ! command -v az >/dev/null 2>&1; then
  echo "[X] Azure CLI (az) no encontrado. https://aka.ms/InstallAzureCli"
  exit 1
fi

if ! az extension list --query "[?name=='azure-devops'].name" -o tsv | grep -q azure-devops; then
  echo "[!] Instalando la extensión az devops (una sola vez)..."
  az extension add --name azure-devops >/dev/null
fi

az devops configure --defaults organization="$ORG_URL" project="$PROJECT" >/dev/null

step() { echo "[>] $*"; }
ok()   { echo "[OK] $*"; }
warn() { echo "[!] $*"; }
