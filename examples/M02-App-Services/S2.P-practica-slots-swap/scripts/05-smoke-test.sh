#!/usr/bin/env bash
# 05 — Smoke tests sobre la URL solicitada (slide 11).
# Uso: ./05-smoke-test.sh staging       # ejecuta sobre el slot staging
#      ./05-smoke-test.sh production    # ejecuta sobre el slot principal
#      ./05-smoke-test.sh production 2.0 # con versión esperada explícita

set -euo pipefail

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

TARGET="${1:-production}"
EXPECTED_VERSION="${2:-}"

case "$TARGET" in
  production) URL="https://${APP}.azurewebsites.net" ;;
  staging)    URL="https://${APP}-staging.azurewebsites.net" ;;
  *)
    echo "[X] Uso: $0 production|staging [version-esperada]"
    exit 1
    ;;
esac

TIMEOUT_SEC=30

echo "Smoke tests sobre $URL"
echo

# Test 1: /health responde 200
echo -n "  [1/4] Health check... "
HTTP_CODE=$(curl -o /dev/null -s -w "%{http_code}" --max-time $TIMEOUT_SEC "$URL/health")
if [ "$HTTP_CODE" = "200" ]; then
  echo "OK"
else
  echo "FAIL ($HTTP_CODE)"
  exit 1
fi

# Test 2: /warmup responde 200
echo -n "  [2/4] Warmup ping... "
HTTP_CODE=$(curl -o /dev/null -s -w "%{http_code}" --max-time $TIMEOUT_SEC "$URL/warmup")
if [ "$HTTP_CODE" = "200" ]; then
  echo "OK"
else
  echo "FAIL ($HTTP_CODE)"
  exit 1
fi

# Test 3: / devuelve la versión esperada (si se ha pasado)
if [ -n "$EXPECTED_VERSION" ]; then
  echo -n "  [3/4] Versión '$EXPECTED_VERSION'... "
  ACTUAL=$(curl -s --max-time $TIMEOUT_SEC "$URL/" | grep -oE '"version":"[^"]*"' | head -1 | cut -d'"' -f4)
  if [ "$ACTUAL" = "$EXPECTED_VERSION" ]; then
    echo "OK"
  else
    echo "FAIL (esperado $EXPECTED_VERSION, recibido $ACTUAL)"
    exit 1
  fi
else
  echo "  [3/4] (versión no verificada — pasa la versión como segundo argumento)"
fi

# Test 4: latencia media razonable (5 requests)
echo -n "  [4/4] Latencia media (5 requests)... "
TOTAL=0
for _ in 1 2 3 4 5; do
  T=$(curl -o /dev/null -s -w "%{time_total}" --max-time $TIMEOUT_SEC "$URL/")
  TOTAL=$(awk -v a="$TOTAL" -v b="$T" 'BEGIN{printf "%.3f", a+b}')
done
AVG=$(awk -v t="$TOTAL" 'BEGIN{printf "%.3f", t/5}')
echo "${AVG}s"

echo
ok "Smoke tests completos sobre $TARGET"
