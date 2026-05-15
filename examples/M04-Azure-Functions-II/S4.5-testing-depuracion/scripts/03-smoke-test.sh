#!/usr/bin/env bash
# 03 - Smoke test del endpoint de descuento (slide 7). El valor real de
# S4.5 está en `dotnet test` (la pirámide), no aquí.

set -euo pipefail
source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

BASE="https://${FUNC}.azurewebsites.net/api"
TIMEOUT=60

check() {
  local label="$1" body="$2" expected="$3"
  echo -n "  $label "
  CODE=$(curl -o /tmp/s45 -s -w "%{http_code}" --max-time $TIMEOUT -X POST \
    -H "Content-Type: application/json" -d "$body" "$BASE/pedidos/descuento")
  if [ "$CODE" = "$expected" ]; then echo "OK ($CODE)"; else
    echo "FAIL (got $CODE, expected $expected)"; cat /tmp/s45; echo; return 1
  fi
}

echo "Smoke test sobre $BASE"; echo

check "[1] total 500 (10% → 450)   " '{"id":"s1","clienteId":"c1","total":500}' "200"
check "[2] total 50  (0% → 50)     " '{"id":"s2","clienteId":"c1","total":50}'  "200"
check "[3] body inválido           " '{"id":"","total":-1}'                      "400"
check "[4] JSON malformado         " '{ roto'                                    "400"

echo
step "Respuesta de [1] (debe traer descuento=50, totalFinal=450):"
curl -s --max-time $TIMEOUT -X POST -H "Content-Type: application/json" \
  -d '{"id":"s1","clienteId":"c1","total":500}' "$BASE/pedidos/descuento"
echo

rm -f /tmp/s45
echo
ok "Smoke test completado"
