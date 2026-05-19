#!/usr/bin/env bash
# 01 - Inventaría la arquitectura event-driven de referencia (slide 12):
# topic + suscripciones (fan-out), Change Feed como Outbox (slide 11) y
# contadores de DLQ por suscripción (slide 17). SOLO LECTURA.

set -euo pipefail
source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

step "Service Bus namespace + SKU (slide 12)"
az servicebus namespace show --name "$SB_NAMESPACE" --resource-group "$RG" \
  --query "{Nombre:name, Sku:sku.name, Estado:status}" -o jsonc 2>/dev/null \
  || warn "Namespace no encontrado o sin acceso"
[ "$(az servicebus namespace show --name "$SB_NAMESPACE" --resource-group "$RG" \
  --query "sku.name" -o tsv 2>/dev/null || echo)" = "Standard" ] \
  && warn "Standard ~10 €/mes FIJOS — borra el namespace al acabar"

step "Topic '$SB_TOPIC' y fan-out de suscripciones (slide 3/12/17)"
az servicebus topic subscription list --namespace-name "$SB_NAMESPACE" \
  --resource-group "$RG" --topic-name "$SB_TOPIC" \
  --query "[].{Suscripcion:name, Activos:countDetails.activeMessageCount, DLQ:countDetails.deadLetterMessageCount}" \
  -o table 2>/dev/null || warn "Topic sin suscripciones o sin acceso"

DLQ_TOTAL=$(az servicebus topic subscription list --namespace-name "$SB_NAMESPACE" \
  --resource-group "$RG" --topic-name "$SB_TOPIC" \
  --query "sum([].countDetails.deadLetterMessageCount)" -o tsv 2>/dev/null || echo "0")
if [ "${DLQ_TOTAL:-0}" != "0" ] && [ "${DLQ_TOTAL:-0}" != "None" ]; then
  warn "Hay $DLQ_TOTAL mensajes en DLQ: revisar/compensar (slide 17)"
else
  ok "Sin mensajes en DLQ"
fi

step "Cosmos DB como Outbox vía Change Feed (slide 11) — read model"
az cosmosdb sql database list --account-name "$COSMOS_ACCOUNT" \
  --resource-group "$RG" --query "[].id" -o tsv 2>/dev/null \
  || warn "Cuenta Cosmos no encontrada o sin acceso"

echo
ok "Inventario event-driven completado (solo lectura)"
echo "Recordatorio: Correlation ID en todos los mensajes (slide 9),"
echo "idempotencia en cada consumidor (slide 10), Outbox con Change Feed"
echo "(slide 11) y máx 3-4 saltos por cadena (slide 20.1)."
