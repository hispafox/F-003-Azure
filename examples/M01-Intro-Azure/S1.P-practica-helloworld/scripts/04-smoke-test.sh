#!/usr/bin/env bash
# 04 — Smoke tests sobre la app desplegada (slide 60).
# Verifica los 5 endpoints de la práctica.

set -euo pipefail

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

URL="https://${APP}.azurewebsites.net"
TIMEOUT_SEC=30

echo "Smoke tests sobre $URL"
echo

check() {
  local label="$1"
  local path="$2"
  local expected="$3"
  echo -n "  $label "
  HTTP_CODE=$(curl -o /dev/null -s -w "%{http_code}" --max-time $TIMEOUT_SEC "$URL$path")
  if [ "$HTTP_CODE" = "$expected" ]; then
    echo "OK"
  else
    echo "FAIL ($HTTP_CODE, esperado $expected)"
    return 1
  fi
}

check "[1/5] /                    " "/"                      "200"
check "[2/5] /health               " "/health"               "200"
check "[3/5] /api/info             " "/api/info"             "200"
check "[4/5] /api/echo (sin msg)   " "/api/echo"             "400"
check "[5/5] /api/echo (con msg)   " "/api/echo?msg=test"    "200"

echo
echo -n "  [bonus] latencia media (5 requests)... "
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
echo "Si la primera respuesta tardo >5s, fue cold start del plan F1 (slide 16, 49)."
