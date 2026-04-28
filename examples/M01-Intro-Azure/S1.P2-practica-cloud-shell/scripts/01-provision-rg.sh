#!/usr/bin/env bash
# 01 - Crear Resource Group con tags (slide 7).
# Tags = metadatos para governance (filtrar costes, identificar duenos,
# politicas automaticas, limpiezas masivas).

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

step "Creando Resource Group: $RG"
az group create --name "$RG" --location "$LOCATION" --output none

step "Anadiendo tags (proyecto, entorno, propietario, fecha-creacion)"
az group update --name "$RG" \
  --tags \
    "proyecto=curso-az204" \
    "entorno=practica-cloud-shell" \
    "propietario=${USER:-$(whoami)}" \
    "fecha-creacion=$(date -u +%Y-%m-%d)" \
  --output none

ok "Resource Group listo"
echo
step "Estado actual:"
az group show --name "$RG" \
  --query "{name:name, location:location, state:properties.provisioningState, tags:tags}" \
  --output table

echo
echo "Siguiente: ./02-create-storage.sh"
