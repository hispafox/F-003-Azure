#!/usr/bin/env bash
# 01 - Inventaría una instancia APIM (slides 3-8, 13): tier, APIs
# importadas, products/suscripciones y métricas clave. SOLO LECTURA.

set -euo pipefail
source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

step "APIM: tier y estado (slide 3/31 — nunca Developer en prod)"
az apim show --name "$APIM_NAME" --resource-group "$RG" \
  --query "{Nombre:name, Tier:sku.name, Estado:provisioningState, Gateway:gatewayUrl}" \
  -o jsonc 2>/dev/null || warn "APIM no encontrado o sin acceso"

TIER=$(az apim show --name "$APIM_NAME" --resource-group "$RG" \
  --query "sku.name" -o tsv 2>/dev/null || echo "")
case "$TIER" in
  Consumption) ok "Consumption — 0 € base (1M llamadas/mes gratis)" ;;
  Developer)   warn "Developer: SIN SLA — no usar en producción (slide 31.1)" ;;
  Basic|Standard|StandardV2|Premium) ok "$TIER — apto para producción" ;;
  *)           warn "No se pudo leer el tier" ;;
esac

step "APIs publicadas y su path/backend (slide 4)"
az apim api list --service-name "$APIM_NAME" --resource-group "$RG" \
  --query "[].{Api:displayName, Path:path, Backend:serviceUrl, SubReq:subscriptionRequired}" \
  -o table 2>/dev/null || warn "Sin APIs o sin acceso"

step "Products + suscripciones (slide 8/31.2 — granularidad)"
az apim product list --service-name "$APIM_NAME" --resource-group "$RG" \
  --query "[].{Product:displayName, SubReq:subscriptionRequired, Aprobacion:approvalRequired}" \
  -o table 2>/dev/null || warn "Sin products o sin acceso"

step "Métricas clave de la última hora (slide 13)"
APIM_ID=$(az apim show --name "$APIM_NAME" --resource-group "$RG" --query id -o tsv 2>/dev/null || echo "")
if [ -n "$APIM_ID" ]; then
  az monitor metrics list --resource "$APIM_ID" \
    --metric "Requests" "UnauthorizedRequests" \
    --interval PT1H --aggregation Total -o table 2>/dev/null \
    || warn "Sin métricas o sin acceso"
fi

echo
ok "Inventario de APIM completado (solo lectura)"
echo "Recordatorio: subscription key (la app) + JWT OAuth2 (el usuario),"
echo "AMBOS (slide 8); rate-limit por subscription/IP (slide 9/31.4);"
echo "config como código con Bicep, no cambios manuales (slide 31.10)."
