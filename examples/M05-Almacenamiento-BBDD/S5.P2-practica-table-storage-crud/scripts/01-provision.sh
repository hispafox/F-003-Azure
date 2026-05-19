#!/usr/bin/env bash
# 01 - Storage Account + tabla "productos" + 3 entities iniciales
# (slides 4-5). Connection string con AccountKey (slide 4: simple para
# la práctica; en producción → Managed Identity, M05-S5.P).

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

step "Resource Group: $RG"
az group create --name "$RG" --location "$LOCATION" \
  --tags "curso=AZ-204" "modulo=M05" "submodulo=S5.P2" --output none

step "Storage Account: $STORAGE (Standard_LRS, StorageV2 — lo más barato)"
az storage account create \
  --name "$STORAGE" --resource-group "$RG" --location "$LOCATION" \
  --sku Standard_LRS --kind StorageV2 \
  --min-tls-version TLS1_2 --output none

CONN="$(conn)"

step "Tabla 'productos' (slide 5)"
az storage table create --name productos --connection-string "$CONN" --output none

step "3 entities iniciales (slide 5)"
az storage entity insert --table-name productos --connection-string "$CONN" \
  --entity PartitionKey=electronica RowKey=laptop001 \
  nombre="Laptop Dell" precio=1299.00 stock=5 --output none
az storage entity insert --table-name productos --connection-string "$CONN" \
  --entity PartitionKey=electronica RowKey=monitor001 \
  nombre="Monitor 27" precio=349.00 stock=12 --output none
az storage entity insert --table-name productos --connection-string "$CONN" \
  --entity PartitionKey=accesorios RowKey=teclado001 \
  nombre="Teclado mecanico" precio=89.90 stock=30 --output none

ok "Tabla 'productos' lista con 3 entities en $STORAGE"
echo
echo "Connection string (con AccountKey — NO lo comitees):"
echo "  $CONN"
echo
echo "Configura Storage:ConnectionString en appsettings (o usa Azurite)."
echo "Siguiente: ./02-smoke-test.sh"
