#!/usr/bin/env bash
# 99 - Cosmos DB emulator opcional (slide 13).
#
# El emulador se arranca en Docker y expone el endpoint en
# https://localhost:8081/. Usa la AccountKey publica del emulador
# (la misma que ya esta en local.settings.json.example).
#
# NO ejecutes esto si quieres apuntar al Cosmos real provisionado en
# 01-provision.sh - es solo para desarrollo offline.

set -euo pipefail

CONTAINER_NAME="cosmos-emulator-m03-s35"

case "${1:-up}" in
  up)
    if docker ps --format '{{.Names}}' | grep -q "^$CONTAINER_NAME$"; then
      echo "[OK] $CONTAINER_NAME ya esta corriendo"
      exit 0
    fi
    echo "[>] Arrancando emulador en Docker (puede tardar 1-2 minutos)..."
    docker run -d --name "$CONTAINER_NAME" \
      -p 8081:8081 -p 10250-10255:10250-10255 \
      -e AZURE_COSMOS_EMULATOR_PARTITION_COUNT=10 \
      -e AZURE_COSMOS_EMULATOR_ENABLE_DATA_PERSISTENCE=false \
      mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:vnext-preview
    echo "[>] Esperando a que el emulador este listo..."
    for i in {1..60}; do
      if curl -s -k "https://localhost:8081/_explorer/index.html" > /dev/null 2>&1; then
        echo "[OK] Emulador arriba en https://localhost:8081/"
        echo "     Explorer: https://localhost:8081/_explorer/index.html"
        echo "     Connection string: ya configurado en local.settings.json.example"
        exit 0
      fi
      sleep 5
    done
    echo "[!] El emulador no respondio en 5 minutos."
    echo "    Logs: docker logs $CONTAINER_NAME"
    exit 1
    ;;
  down)
    docker rm -f "$CONTAINER_NAME" 2>/dev/null || true
    echo "[OK] Emulador detenido"
    ;;
  status)
    docker ps --filter "name=$CONTAINER_NAME"
    ;;
  *)
    echo "Uso: $0 [up|down|status]"
    exit 1
    ;;
esac
