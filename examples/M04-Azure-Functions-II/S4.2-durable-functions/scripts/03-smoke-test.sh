#!/usr/bin/env bash
# 03 - Smoke test S4.2: arranca 3 sagas y verifica su estado final.
#   - pedido normal       → completado
#   - pedido total .99    → compensado (saga)
#   - pedido > 5000 + reject → rechazado
# Más el fan-out/fan-in de facturas.

set -euo pipefail
source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

API="https://${FUNC}.azurewebsites.net/api"
TIMEOUT=30

step "Obteniendo function key"
KEY=$(az functionapp keys list --name "$FUNC" --resource-group "$RG" \
  --query "functionKeys.default" -o tsv)
[ -n "$KEY" ] || { echo "[X] No se pudo leer la function key"; exit 1; }

# Helper: arranca una saga y devuelve el instanceId
iniciar() {
  local body="$1"
  curl -s --max-time $TIMEOUT -X POST \
    -H "Content-Type: application/json" -d "$body" \
    "$API/pedidos/procesar?code=$KEY" \
    | grep -oE '"instanceId":"[^"]+"' | head -1 | cut -d'"' -f4
}

estado() {
  curl -s --max-time $TIMEOUT "$API/pedidos/estado/$1?code=$KEY"
}

TS=$(date -u +%s)

step "[1] Pedido normal (1200€) → esperado: completado"
ID1=$(iniciar "{\"id\":\"ped-$TS-1\",\"clienteId\":\"c-A\",\"clienteEmail\":\"a@b.c\",\"total\":1200.00,\"items\":[{\"sku\":\"S1\",\"cantidad\":1,\"precioUnitario\":1200}]}")
echo "  instanceId=$ID1"

step "[2] Pedido .99 (99.99€) → esperado: compensado (saga)"
ID2=$(iniciar "{\"id\":\"ped-$TS-2\",\"clienteId\":\"c-B\",\"clienteEmail\":\"b@b.c\",\"total\":99.99,\"items\":[{\"sku\":\"S2\",\"cantidad\":1,\"precioUnitario\":99.99}]}")
echo "  instanceId=$ID2"

step "[3] Pedido > 5000 (8500€) → esperado: esperando-aprobacion"
ID3=$(iniciar "{\"id\":\"ped-$TS-3\",\"clienteId\":\"c-C\",\"clienteEmail\":\"c@b.c\",\"total\":8500.00,\"items\":[{\"sku\":\"S3\",\"cantidad\":1,\"precioUnitario\":8500}]}")
echo "  instanceId=$ID3"

step "Esperando 20s a que las orquestaciones avancen..."
sleep 20

echo
step "Estado [1] (esperado runtimeStatus=Completed, output completado):"
estado "$ID1" | head -c 400; echo
echo
step "Estado [2] (esperado output compensado):"
estado "$ID2" | head -c 400; echo
echo
step "Estado [3] (esperado runtimeStatus=Running, customStatus esperando-aprobacion):"
estado "$ID3" | head -c 400; echo

echo
step "Aprobando el pedido [3]..."
curl -s --max-time $TIMEOUT -X POST \
  -H "Content-Type: application/json" -d '{"aprobado":true}' \
  "$API/pedidos/$ID3/aprobar?code=$KEY" | head -c 200; echo
sleep 15
step "Estado [3] tras aprobar (esperado Completed):"
estado "$ID3" | head -c 400; echo

echo
step "[4] Fan-out/fan-in: lote de 4 facturas (1 inválida)"
LOTE=$(curl -s --max-time $TIMEOUT -X POST \
  -H "Content-Type: application/json" \
  -d '[{"id":"f1","clienteId":"c1","importe":100},{"id":"f2","clienteId":"c1","importe":250},{"id":"f3","clienteId":"c2","importe":0},{"id":"f4","clienteId":"c2","importe":75.5}]' \
  "$API/facturas/lote?code=$KEY")
echo "  $LOTE" | head -c 200; echo
LOTE_ID=$(echo "$LOTE" | grep -oE '"instanceId":"[^"]+"' | head -1 | cut -d'"' -f4)
sleep 15
step "Estado del lote (esperado total=4, exitosas=3, fallidas=1, importeTotal=425.5):"
estado "$LOTE_ID" | head -c 500; echo

echo
ok "Smoke test completado — revisa los outputs arriba"
echo "Si algo no cuadra: az functionapp log tail --name $FUNC -g $RG"
