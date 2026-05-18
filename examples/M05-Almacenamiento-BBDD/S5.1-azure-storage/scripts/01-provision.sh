#!/usr/bin/env bash
# 01 - Storage Account StorageV2 (slide 3) + contenedores/cola/share +
# lifecycle policy (slide 5: Hot→Cool 30d, →Archive 180d, borrar 365d).

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

step "Resource Group: $RG"
az group create --name "$RG" --location "$LOCATION" \
  --tags "curso=AZ-204" "modulo=M05" "submodulo=S5.1" \
  --output none

step "Storage Account: $STORAGE (StorageV2, LRS, TLS1.2, sin acceso público)"
az storage account create \
  --name "$STORAGE" --resource-group "$RG" --location "$LOCATION" \
  --sku Standard_LRS --kind StorageV2 \
  --min-tls-version TLS1_2 \
  --allow-blob-public-access false \
  --output none

CONN=$(az storage account show-connection-string \
  --name "$STORAGE" --resource-group "$RG" --query connectionString -o tsv)

step "Blob container 'facturas', Queue 'pedidos', File share 'compartido'"
az storage container create --name facturas --connection-string "$CONN" --output none
az storage queue create --name pedidos --connection-string "$CONN" --output none
az storage share create --name compartido --connection-string "$CONN" --output none

step "Lifecycle policy (slide 5)"
POLICY=$(mktemp)
cat > "$POLICY" <<'JSON'
{
  "rules": [{
    "name": "facturas-tiering",
    "type": "Lifecycle",
    "definition": {
      "filters": { "blobTypes": ["blockBlob"], "prefixMatch": ["facturas/"] },
      "actions": { "baseBlob": {
        "tierToCool":    { "daysAfterModificationGreaterThan": 30 },
        "tierToArchive": { "daysAfterModificationGreaterThan": 180 },
        "delete":        { "daysAfterModificationGreaterThan": 365 }
      }}
    }
  }]
}
JSON
az storage account management-policy create \
  --account-name "$STORAGE" --resource-group "$RG" \
  --policy @"$POLICY" --output none
rm -f "$POLICY"

ok "Storage listo: $STORAGE"
echo
echo "Para la API:"
echo "  StorageConnection = (vacío → Azurite local) o"
echo "  StorageAccountUri = https://$STORAGE.blob.core.windows.net + Managed Identity (S5.4)"
echo
echo "Connection string (NO lo comitees):"
echo "  $CONN"
echo
echo "Siguiente: ./02-smoke-test.sh"
