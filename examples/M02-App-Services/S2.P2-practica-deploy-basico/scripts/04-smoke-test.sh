#!/usr/bin/env bash
# 04 — Smoke tests sobre la app desplegada (slide 15).

set -euo pipefail

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

URL="https://${APP}.azurewebsites.net"
TIMEOUT_SEC=30

echo "Smoke tests sobre $URL"
echo

echo -n "  [1/4] Endpoint raiz... "
HTTP_CODE=$(curl -o /dev/null -s -w "%{http_code}" --max-time $TIMEOUT_SEC "$URL/")
if [ "$HTTP_CODE" = "200" ]; then echo "OK"; else echo "FAIL ($HTTP_CODE)"; exit 1; fi

echo -n "  [2/4] Health check... "
HTTP_CODE=$(curl -o /dev/null -s -w "%{http_code}" --max-time $TIMEOUT_SEC "$URL/health")
if [ "$HTTP_CODE" = "200" ]; then echo "OK"; else echo "FAIL ($HTTP_CODE)"; exit 1; fi

echo -n "  [3/4] Saludo (valido)... "
HTTP_CODE=$(curl -o /dev/null -s -w "%{http_code}" --max-time $TIMEOUT_SEC "$URL/saludo/test")
if [ "$HTTP_CODE" = "200" ]; then echo "OK"; else echo "FAIL ($HTTP_CODE)"; exit 1; fi

echo -n "  [4/4] Latencia media (5 requests)... "
TOTAL=0
for _ in 1 2 3 4 5; do
  T=$(curl -o /dev/null -s -w "%{time_total}" --max-time $TIMEOUT_SEC "$URL/health")
  TOTAL=$(awk -v a="$TOTAL" -v b="$T" 'BEGIN{printf "%.3f", a+b}')
done
AVG=$(awk -v t="$TOTAL" 'BEGIN{printf "%.3f", t/5}')
echo "${AVG}s"

echo
ok "Smoke tests completos"
echo
echo "Si la primera respuesta tardo >5 s, fue cold start del plan F1 (slide 11)."
