#!/usr/bin/env bash
# 03 - Smoke test S4.3: envía mensajes a la cola y verifica el flujo de
# resiliencia vía /api/estado.
#   1) mensaje OK            → procesado
#   2) mismo id otra vez     → duplicado saltado (idempotencia)
#   3) JSON malformado       → dead-letter → ProcesarDeadLetter lo descarta

set -euo pipefail
source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

API="https://${FUNC}.azurewebsites.net/api"
TIMEOUT=30

step "Obteniendo function key"
KEY=$(az functionapp keys list --name "$FUNC" --resource-group "$RG" \
  --query "functionKeys.default" -o tsv)
[ -n "$KEY" ] || { echo "[X] No se pudo leer la function key"; exit 1; }

SB_CONN=$(az servicebus namespace authorization-rule keys list \
  --namespace-name "$SB" --resource-group "$RG" \
  --name RootManageSharedAccessKey \
  --query primaryConnectionString -o tsv)

# az servicebus no envía mensajes; usamos la REST API de Service Bus con
# un token SAS derivado del connection string sería complejo en bash.
# Lo más simple para la demo: indicar al alumno que envíe desde el Portal
# (Service Bus Explorer) y aquí solo verificamos el estado.
TS=$(date -u +%s)

warn "Envía estos mensajes a la cola '$SB_QUEUE_PEDIDOS' desde el Portal"
warn "(Service Bus → $SB → Queues → $SB_QUEUE_PEDIDOS → Service Bus Explorer → Send):"
echo
echo "  [1] OK:        {\"id\":\"ped-$TS\",\"clienteId\":\"c\",\"clienteEmail\":\"a@b.c\",\"total\":100}"
echo "  [2] Duplicado: (reenvía EXACTAMENTE el mismo mensaje [1])"
echo "  [3] Malo:      { roto json"
echo
read -r -p "Pulsa ENTER cuando los hayas enviado..."

step "Esperando 20s a que el trigger procese..."
sleep 20

step "Estado del flujo de resiliencia (/api/estado):"
curl -s --max-time $TIMEOUT "$API/estado?code=$KEY" | head -c 800
echo
echo
echo "Esperado tras [1]+[2]+[3]:"
echo "  procesados >= 1"
echo "  duplicadosSaltados >= 1   (idempotencia, slide 10)"
echo "  enviadosADeadLetter >= 1  (JSON malo, permanente)"
echo "  poisonProcesados >= 1     (ProcesarDeadLetter lo clasificó/descartó)"
echo
ok "Smoke test: revisa los contadores arriba"
echo "Logs en vivo: az functionapp log tail --name $FUNC -g $RG"
