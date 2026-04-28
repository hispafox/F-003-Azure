#!/usr/bin/env bash
# 04 - Demuestra JMESPath: el lenguaje de queries del CLI (slide 10).
# Lanza 6 ejemplos progresivos sobre los recursos del RG.

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

echo "============================================================"
echo " JMESPath cheat-sheet sobre el RG: $RG"
echo "============================================================"

step "[1/6] Vista compacta en tabla (--output table)"
az resource list -g "$RG" --output table

echo
step "[2/6] Solo nombres ([].name)"
az resource list -g "$RG" --query "[].name" -o tsv

echo
step "[3/6] Solo Storage Accounts ([?type=='Microsoft.Storage/storageAccounts'])"
az resource list -g "$RG" \
  --query "[?type=='Microsoft.Storage/storageAccounts'].{name:name, location:location}" \
  --output table

echo
step "[4/6] Filtrar por tag del proyecto (.tags.proyecto)"
az group list \
  --query "[?tags.proyecto=='curso-az204'].{name:name, propietario:tags.propietario}" \
  --output table

echo
step "[5/6] Recursos por tipo (count by type)"
az resource list -g "$RG" --query "[].type" -o tsv | sort | uniq -c | sort -rn

echo
step "[6/6] Proyeccion custom: nombres + provider + tags"
az resource list -g "$RG" \
  --query "[].{name:name, type:type, provider:type | split(@, '/')[0]}" \
  --output table

echo
echo "============================================================"
echo " Patrones JMESPath mas usados (tabla rapida):"
echo "============================================================"
cat <<'EOF'

  []                          todos los elementos
  [0]                         primer elemento
  [].name                     lista de nombres
  [?prop=='val']              filtro por igualdad
  [?contains(name, 'x')]      filtro por substring
  [].{X:propA, Y:propB}       proyeccion custom
  length(@)                   contar elementos
  sort_by([], &name)          ordenar por nombre
  not_null(@)                 omitir nulls

  Documentacion oficial: https://jmespath.org/
  Probador online:        https://jmespath.org/

EOF
