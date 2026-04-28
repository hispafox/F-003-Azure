#!/usr/bin/env bash
# 06 — Security defaults (slide 59, opcional pero recomendado).
# - HTTPS only
# - Min TLS 1.2
# - FTPS Disabled (deploy solo por HTTPS)

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

step "HTTPS Only"
az webapp update --name "$APP" -g "$RG" --https-only true --output none

step "Min TLS = 1.2"
az webapp config set --name "$APP" -g "$RG" --min-tls-version 1.2 --output none

step "FTPS Disabled"
az webapp config set --name "$APP" -g "$RG" --ftps-state Disabled --output none

ok "Security defaults aplicados"
echo
echo "Estado final:"
az webapp config show --name "$APP" -g "$RG" \
  --query "{httpsOnly:httpsOnly, minTls:minTlsVersion, ftpsState:ftpsState}" \
  --output table
