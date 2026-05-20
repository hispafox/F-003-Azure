#!/usr/bin/env bash
# 01 - bicep build local + az deployment what-if (preview). SOLO
# LECTURA: nunca ejecuta `az deployment group create`.

set -euo pipefail
source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

if [ ! -f "$BICEP_FILE" ]; then
  warn "No existe $BICEP_FILE. Revisa BICEP_FILE en .env.demo."
  exit 1
fi

step "Bicep version (slide 5/13)"
az bicep version

step "bicep build local (compila a ARM JSON, sin tocar Azure)"
az bicep build --file "$BICEP_FILE"
ok "ARM JSON generado: ${BICEP_FILE%.bicep}.json"

step "az deployment group validate (slide 5)"
az deployment group validate \
  --resource-group "$RG" \
  --template-file "$BICEP_FILE" \
  --query "{Provisioning:properties.provisioningState, Validacion:properties.validatedResources[].id}" \
  -o jsonc 2>&1 || warn "Validación con errores."

step "az deployment group what-if (slide 5/14)"
echo "Símbolos: + Create, ~ Modify, - Delete, = NoChange"
echo "Revisa Delete de recursos STATEFUL (Storage, Cosmos, SQL, KV)."
echo ""
az deployment group what-if \
  --resource-group "$RG" \
  --template-file "$BICEP_FILE" || warn "what-if con problemas."

echo
ok "Bicep validado en local (solo lectura)"
echo "Recordatorio: si what-if muestra 'Delete:' algo va mal antes de"
echo "ejecutar (slide 14). Aplica con `az deployment group create`"
echo "solo después de aprobación manual (slide 7)."
