#!/usr/bin/env bash
# Reto 1 (slide 20) - Crear varios RGs con tags distintos y filtrar por tag.

source "$( dirname "${BASH_SOURCE[0]}" )/../_lib.sh"

SUFFIX="${USER:-curso}"

for env in dev qa prod; do
  step "Creando rg-test-${env}-${SUFFIX}"
  az group create \
    --name "rg-test-${env}-${SUFFIX}" \
    --location "$LOCATION" \
    --tags "entorno=${env}" "owner=${SUFFIX}" "tipo=reto1" \
    --output none
done

echo
step "Filtrando solo los de prod"
az group list --query "[?tags.entorno=='prod' && tags.tipo=='reto1']" --output table

echo
step "Filtrando los de este reto (tipo='reto1')"
az group list --query "[?tags.tipo=='reto1'].{name:name, env:tags.entorno}" --output table

echo
confirm "Limpiar los 3 RGs creados?"
for env in dev qa prod; do
  az group delete --name "rg-test-${env}-${SUFFIX}" --yes --no-wait --output none
done
ok "Borrado lanzado en background"
