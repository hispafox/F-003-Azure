#!/usr/bin/env bash
# 02 - Crear Storage Account Standard_LRS / StorageV2 (slide 8).
# - Standard_LRS = 3 copias en una zona (lo mas barato)
# - StorageV2 = soporta blobs / queues / tables / files

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

: "${STORAGE:?STORAGE no definido en .env.demo}"

step "Creando Storage Account: $STORAGE"
az storage account create \
  --name "$STORAGE" \
  --resource-group "$RG" \
  --location "$LOCATION" \
  --sku Standard_LRS \
  --kind StorageV2 \
  --output none

ok "Storage Account listo"
echo
step "Detalles:"
az storage account show --name "$STORAGE" --resource-group "$RG" \
  --query "{name:name, sku:sku.name, kind:kind, location:location, accessTier:accessTier}" \
  --output table

echo
echo "Coste real con 0 datos: ~0.02 EUR/mes"
echo "Siguiente: ./03-upload-blob.sh"
