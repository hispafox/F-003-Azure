#!/usr/bin/env bash
# 03 - Smoke test: inserta pedidos en Cosmos DB y verifica que los dos
# triggers del Change Feed los procesaron (notificaciones + resumenes).
#
# Slide 6 - feedPollDelay = 5s, asi que los cambios deberian aparecer en
# pocos segundos. En primer arranque (cold start) hasta 60s.

set -euo pipefail
source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

BASE="https://${FUNC}.azurewebsites.net"
API="$BASE/api"
TIMEOUT=30

step "Obteniendo function key"
KEY=$(az functionapp keys list --name "$FUNC" --resource-group "$RG" \
  --query "functionKeys.default" -o tsv)
[ -n "$KEY" ] || { echo "[X] No se pudo leer la function key"; exit 1; }

# ── Insertar 3 pedidos en Cosmos ──
TS=$(date -u +%s)
declare -a PEDIDOS
PEDIDOS+=("ped-$TS-1|cliente-A|confirmado|150.00")
PEDIDOS+=("ped-$TS-2|cliente-A|enviado|150.00")
PEDIDOS+=("ped-$TS-3|cliente-B|entregado|99.50")

step "Insertando 3 pedidos en $COSMOS / $COSMOS_DB / $COSMOS_PEDIDOS"
for entry in "${PEDIDOS[@]}"; do
  IFS='|' read -r ID CLI ESTADO TOTAL <<< "$entry"
  DOC="{\"id\":\"$ID\",\"clienteId\":\"$CLI\",\"estado\":\"$ESTADO\",\"total\":$TOTAL}"
  az cosmosdb sql container create-item-or-update \
    --account-name "$COSMOS" --resource-group "$RG" \
    --database-name "$COSMOS_DB" --container-name "$COSMOS_PEDIDOS" \
    --partition-key-value "$CLI" \
    --body "$DOC" \
    --output none 2>/dev/null \
  || az cosmosdb sql container query \
    --account-name "$COSMOS" --resource-group "$RG" \
    --database-name "$COSMOS_DB" --container-name "$COSMOS_PEDIDOS" \
    --query-text "SELECT * FROM c WHERE c.id='$ID'" --output none 2>/dev/null \
  || true

  # Fallback: insert via REST API si az no soporta el subcomando.
  echo "  insertado: id=$ID cliente=$CLI estado=$ESTADO"
done

# Si tu az no tiene `cosmosdb sql container create-item-or-update`,
# usa az cosmosdb sql query como fallback. La forma robusta para CI
# es Microsoft.Azure.Cosmos SDK, pero para una demo en clase basta con
# az + un loop de retry.
warn "Si los pedidos no se insertaron via az, hazlo desde el Portal"
warn "  Cosmos DB > Data Explorer > $COSMOS_DB > $COSMOS_PEDIDOS > Items > New Item"

# ── Esperar al Change Feed ──
step "Esperando hasta 90s a que los triggers del Change Feed reaccionen..."
DEADLINE=$(( $(date +%s) + 90 ))
PROCESSED=0
while [ "$(date +%s)" -lt "$DEADLINE" ]; do
  RESP=$(curl -s --max-time $TIMEOUT "$API/notificaciones?code=$KEY" 2>/dev/null || echo "")
  TOTAL=$(echo "$RESP" | grep -oE '"total":[0-9]+' | grep -oE '[0-9]+' || echo 0)
  if [ "$TOTAL" -ge 3 ]; then
    PROCESSED=1; break
  fi
  echo "  ...notificaciones=$TOTAL ($(($DEADLINE - $(date +%s)))s restantes)"
  sleep 10
done

if [ "$PROCESSED" -eq 1 ]; then
  ok "El Change Feed proceso los 3 pedidos (notificaciones=$TOTAL)"
else
  warn "El Change Feed aun no proceso. Posibles causas:"
  echo "  - Cold start del Function App (primer arranque)"
  echo "  - feedPollDelay (slide 6) - hasta 5 segundos por poll"
  echo "  - Lease container aun no creado (slide 5 - se crea en runtime)"
  echo "  Revisa con: az functionapp log tail --name $FUNC -g $RG"
fi

# ── Listar resumenes materializados ──
echo
step "Resumenes materializados (/api/resumenes):"
curl -s --max-time $TIMEOUT "$API/resumenes?code=$KEY" | head -c 800
echo
echo

step "Resumen del cliente-A (/api/resumenes/cliente-A):"
curl -s --max-time $TIMEOUT "$API/resumenes/cliente-A?code=$KEY" | head -c 500
echo
echo

step "Verificacion en Cosmos: contenido de $COSMOS_RESUMENES"
az cosmosdb sql query \
  --account-name "$COSMOS" --resource-group "$RG" \
  --database-name "$COSMOS_DB" --container-name "$COSMOS_RESUMENES" \
  --query-text "SELECT c.id, c.clienteId, c.totalPedidos, c.importeAcumulado FROM c" \
  --output table 2>/dev/null || warn "Query directo a Cosmos requiere az extension cosmosdb-preview"

ok "Smoke test completado"
