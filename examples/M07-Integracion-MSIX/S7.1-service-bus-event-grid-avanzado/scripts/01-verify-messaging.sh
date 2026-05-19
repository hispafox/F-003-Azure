#!/usr/bin/env bash
# 01 - Verifica el entregable (slides 3-4, 10, 19): namespace + tier,
# topic con suscripciones y sus reglas/filtros SQL, deduplicación en la
# cola y contadores de DLQ. SOLO LECTURA (no crea ni borra nada).

set -euo pipefail
source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

step "Namespace + SKU (slide 17/23 — Standard es lo mínimo para topics)"
az servicebus namespace show --name "$SB_NAMESPACE" --resource-group "$RG" \
  --query "{Nombre:name, Sku:sku.name, Estado:status}" -o jsonc 2>/dev/null \
  || warn "Namespace no encontrado o sin acceso"

SKU=$(az servicebus namespace show --name "$SB_NAMESPACE" --resource-group "$RG" \
  --query "sku.name" -o tsv 2>/dev/null || echo "")
case "$SKU" in
  Standard|Premium) ok "SKU $SKU — soporta topics, suscripciones y filtros SQL" ;;
  Basic) warn "SKU Basic: NO soporta topics/filtros (slide 17). Sube a Standard" ;;
  *)     warn "No se pudo leer el SKU" ;;
esac
[ "$SKU" = "Standard" ] && warn "Recordatorio: Standard ~10 €/mes FIJOS — borra el namespace al acabar"

step "Topic '$SB_TOPIC' y sus suscripciones (slide 3)"
az servicebus topic subscription list --namespace-name "$SB_NAMESPACE" \
  --resource-group "$RG" --topic-name "$SB_TOPIC" \
  --query "[].{Suscripcion:name, Activos:countDetails.activeMessageCount, DLQ:countDetails.deadLetterMessageCount}" \
  -o table 2>/dev/null || warn "Topic sin suscripciones o sin acceso"

step "Reglas/filtros SQL por suscripción (slide 4 — filtrado en el broker)"
for SUB in $(az servicebus topic subscription list --namespace-name "$SB_NAMESPACE" \
  --resource-group "$RG" --topic-name "$SB_TOPIC" --query "[].name" -o tsv 2>/dev/null); do
  echo "  · $SUB:"
  az servicebus topic subscription rule list --namespace-name "$SB_NAMESPACE" \
    --resource-group "$RG" --topic-name "$SB_TOPIC" --subscription-name "$SUB" \
    --query "[].{Regla:name, Filtro:sqlFilter.sqlExpression}" -o table 2>/dev/null \
    || warn "    sin reglas o sin acceso"
done

step "Deduplicación en la cola '$SB_QUEUE' (slide 10)"
az servicebus queue show --namespace-name "$SB_NAMESPACE" --resource-group "$RG" \
  --name "$SB_QUEUE" \
  --query "{Dedup:requiresDuplicateDetection, Ventana:duplicateDetectionHistoryTimeWindow, DLQ:countDetails.deadLetterMessageCount}" \
  -o jsonc 2>/dev/null || warn "Cola no encontrada o sin acceso"

echo
ok "Verificación de mensajería completada (solo lectura)"
echo "Recordatorio: los filtros SQL se evalúan en el BROKER (slide 4);"
echo "monitoriza la DLQ (alerta si > 10, slide 19/31) y usa Managed"
echo "Identity en vez de connection strings (anti-pattern 5, slide 31)."
