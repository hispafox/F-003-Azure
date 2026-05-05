#!/usr/bin/env bash
# 03 - Smoke test sobre la Function App desplegada.

set -euo pipefail
source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

URL="https://${FUNC}.azurewebsites.net/api/hello"
TIMEOUT=60

echo "Smoke test sobre $URL"
echo
echo -n "  [1/3] Hello sin parametro... "
HTTP_CODE=$(curl -o /tmp/hello1.json -s -w "%{http_code}" --max-time $TIMEOUT "$URL")
if [ "$HTTP_CODE" = "200" ]; then echo "OK"; else echo "FAIL ($HTTP_CODE)"; exit 1; fi

echo -n "  [2/3] Hello con name=Pedro... "
HTTP_CODE=$(curl -o /tmp/hello2.json -s -w "%{http_code}" --max-time $TIMEOUT "$URL?name=Pedro")
if [ "$HTTP_CODE" = "200" ]; then echo "OK"; else echo "FAIL ($HTTP_CODE)"; exit 1; fi

echo -n "  [3/3] Respuesta es JSON valido... "
if grep -q '"mensaje"' /tmp/hello2.json && grep -q '"runtime"' /tmp/hello2.json; then
  echo "OK"
else
  echo "FAIL"
  cat /tmp/hello2.json
  exit 1
fi

echo
ok "Smoke test completado"
echo
echo "Respuesta de muestra:"
cat /tmp/hello2.json
echo
rm -f /tmp/hello1.json /tmp/hello2.json
