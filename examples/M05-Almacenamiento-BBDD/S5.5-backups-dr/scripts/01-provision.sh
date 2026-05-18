#!/usr/bin/env bash
# 01 - Storage StorageV2 + protección contra borrado (slide 6/19):
# blob soft delete (30d) + versioning + container soft delete (30d).

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

step "Resource Group: $RG"
az group create --name "$RG" --location "$LOCATION" \
  --tags "curso=AZ-204" "modulo=M05" "submodulo=S5.5" --output none

step "Storage Account: $STORAGE (StorageV2, TLS1.2, sin acceso público)"
az storage account create \
  --name "$STORAGE" --resource-group "$RG" --location "$LOCATION" \
  --sku Standard_LRS --kind StorageV2 \
  --min-tls-version TLS1_2 --allow-blob-public-access false --output none

step "Soft delete de blobs (30 días) + versioning + soft delete de containers (slide 6/19)"
az storage account blob-service-properties update \
  --account-name "$STORAGE" --resource-group "$RG" \
  --enable-delete-retention true --delete-retention-days 30 \
  --enable-container-delete-retention true --container-delete-retention-days 30 \
  --enable-versioning true \
  --output none

step "Container 'facturas'"
CONN="$(conn)"
az storage container create --name facturas --connection-string "$CONN" --output none

ok "Protección lista en $STORAGE (soft delete 30d + versioning)"
echo
echo "Recomendación 3-2-1 (slide 11): + GRS/GZRS y copia en otra región."
echo "Siguiente: ./02-smoke-test.sh  (walkthrough de recuperación, slide 19)"
