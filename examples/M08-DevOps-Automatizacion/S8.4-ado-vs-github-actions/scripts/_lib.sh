#!/usr/bin/env bash
# Utilidades. Scripts SOLO LECTURA: preflight de las dos plataformas
# (Azure DevOps CLI + GitHub CLI). No crea ni modifica nada.

set -euo pipefail

step() { echo "[>] $*"; }
ok()   { echo "[OK] $*"; }
warn() { echo "[!] $*"; }
