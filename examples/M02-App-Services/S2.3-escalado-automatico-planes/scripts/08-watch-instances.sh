#!/usr/bin/env bash
# 08 — Hace polling al endpoint /info y muestra el instanceId que atiende
# cada petición. Cuando el plan tiene varias instancias, los instanceId
# rotan y se ve el load balancing en directo (slides 4, 10).
# Ctrl+C para parar.

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

URL="https://${APP}.azurewebsites.net/info"

echo "Polling $URL  (Ctrl+C para parar)"
echo

declare -A seen=()
while true; do
  ts=$(date +%H:%M:%S)
  body=$(curl -s "$URL" || echo "")
  instance=$(echo "$body" | grep -oE '"instanceId":"[^"]*"' | head -1 | cut -d'"' -f4)

  if [ -z "$instance" ]; then
    echo "[$ts] (sin respuesta)"
  else
    seen["$instance"]=1
    distinct=${#seen[@]}
    echo "[$ts] instance=$instance  (vistas hasta ahora: $distinct)"
  fi
  sleep 2
done
