#!/usr/bin/env bash
# 06 - Smoke tests: 5 checks sobre el RG y el storage (slide 13).
# Si algun test falla, salta a 1 y muestra exactamente donde.

set -euo pipefail

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

: "${STORAGE:?STORAGE no definido}"

failed=0

echo "Smoke tests sobre RG=$RG storage=$STORAGE"
echo

# Test 1 -- RG existe y esta Succeeded
echo -n "  [1/5] Resource Group provisioned... "
STATE=$(az group show --name "$RG" --query "properties.provisioningState" -o tsv 2>/dev/null || true)
if [ "$STATE" = "Succeeded" ]; then echo "OK"; else echo "FAIL ($STATE)"; failed=$((failed+1)); fi

# Test 2 -- al menos 3 tags
echo -n "  [2/5] Tags configurados (>= 3)... "
TAGS_COUNT=$(az group show --name "$RG" --query "tags | length(@)" -o tsv 2>/dev/null || echo "0")
if [ "${TAGS_COUNT:-0}" -ge 3 ]; then echo "OK ($TAGS_COUNT tags)"; else echo "FAIL ($TAGS_COUNT)"; failed=$((failed+1)); fi

# Test 3 -- storage account existe
echo -n "  [3/5] Storage Account existe... "
SKU=$(az storage account show --name "$STORAGE" -g "$RG" --query "sku.name" -o tsv 2>/dev/null || true)
if [ -n "$SKU" ]; then echo "OK ($SKU)"; else echo "FAIL"; failed=$((failed+1)); fi

# Test 4 -- container 'pruebas' existe
echo -n "  [4/5] Container 'pruebas'... "
EXISTS=$(az storage container exists \
  --name pruebas \
  --account-name "$STORAGE" \
  --auth-mode login \
  --query "exists" -o tsv 2>/dev/null || echo "false")
if [ "$EXISTS" = "true" ]; then echo "OK"; else echo "FAIL (ejecuta 03-upload-blob.sh)"; failed=$((failed+1)); fi

# Test 5 -- blob 'saludo.txt' existe
echo -n "  [5/5] Blob 'saludo.txt'... "
B_EXISTS=$(az storage blob exists \
  --container-name pruebas \
  --name saludo.txt \
  --account-name "$STORAGE" \
  --auth-mode login \
  --query "exists" -o tsv 2>/dev/null || echo "false")
if [ "$B_EXISTS" = "true" ]; then echo "OK"; else echo "FAIL"; failed=$((failed+1)); fi

echo
if [ $failed -eq 0 ]; then
  ok "Todos los smoke tests pasaron"
else
  warn "$failed test(s) fallaron"
  exit 1
fi
