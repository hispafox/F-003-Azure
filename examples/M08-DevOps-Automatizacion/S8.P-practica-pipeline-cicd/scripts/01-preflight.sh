#!/usr/bin/env bash
# 01 - Verifica los prerequisitos de la practica (slide 3) contra Azure
# real. SOLO LECTURA: comprueba que existen las cosas necesarias para
# que el pipeline funcione. No crea nada.

set -euo pipefail
source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

step "Suscripcion activa"
az account show --query "{name:name, id:id, tenantId:tenantId}" -o table

step "Plan S1+ (deployment slots requieren Standard)"
TIER=$(az appservice plan show --name "$PLAN_NAME" -g "$RG" --query sku.tier -o tsv 2>/dev/null || echo "")
NAME=$(az appservice plan show --name "$PLAN_NAME" -g "$RG" --query sku.name -o tsv 2>/dev/null || echo "")
echo "Tier: ${TIER:-NO ENCONTRADO}  SKU: ${NAME:-?}"
case "$TIER" in
  Standard|PremiumV2|PremiumV3) ok "Plan soporta slots (Standard o superior)." ;;
  "") warn "Plan no encontrado en RG=$RG." ;;
  *) warn "Plan tier=$TIER NO soporta slots. Requerido S1+." ;;
esac

step "App Service y slot staging"
az webapp show --name "$APP_NAME" -g "$RG" --query "{state:state, hostNames:hostNames}" -o jsonc 2>&1 \
  || warn "App no encontrada."

az webapp deployment slot list --name "$APP_NAME" -g "$RG" \
  --query "[].{Slot:name, State:state, Url:defaultHostName}" -o table 2>&1 \
  || warn "Sin slots o sin acceso."

step "Despliegues recientes del slot staging"
az webapp deployment list --name "$APP_NAME" -g "$RG" --slot staging \
  --query "[0:3].{Id:id, Status:status, Time:end_time}" -o table 2>&1 \
  || warn "Sin despliegues registrados."

ok "Preflight terminado (solo lectura)."
echo "Si algun bloqueante esta en rojo, corrige antes de empezar la practica."
