#!/usr/bin/env bash
# 03 - Crear container, subir y descargar un blob (slide 9).
# Usa --auth-mode login (RBAC) en lugar de keys del Storage. Es lo
# recomendado en produccion. Si te falla con AuthorizationPermissionMismatch,
# este script asigna automaticamente el rol "Storage Blob Data Contributor".

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

: "${STORAGE:?STORAGE no definido}"

step "Verificando rol RBAC del usuario actual sobre el Storage"
USER_ID=$(az ad signed-in-user show --query id -o tsv 2>/dev/null || echo "")
if [ -z "$USER_ID" ]; then
  warn "No pude resolver el principal del usuario actual; saltando check de rol."
else
  STORAGE_ID=$(az storage account show --name "$STORAGE" --resource-group "$RG" --query id -o tsv)
  ROLE_EXISTS=$(az role assignment list \
    --assignee "$USER_ID" \
    --scope "$STORAGE_ID" \
    --query "[?roleDefinitionName=='Storage Blob Data Contributor'].id" \
    --output tsv 2>/dev/null | head -1)

  if [ -z "$ROLE_EXISTS" ]; then
    warn "Falta el rol 'Storage Blob Data Contributor'. Asignandolo..."
    az role assignment create \
      --assignee "$USER_ID" \
      --role "Storage Blob Data Contributor" \
      --scope "$STORAGE_ID" \
      --output none
    echo "    Esperando 30s a que RBAC propague..."
    sleep 30
  else
    ok "Rol RBAC ya asignado"
  fi
fi

step "Creando container 'pruebas'"
az storage container create \
  --name pruebas \
  --account-name "$STORAGE" \
  --auth-mode login \
  --output none

step "Generando archivo de prueba"
TMP=$(mktemp -t cloudshell-saludo.XXXXXX)
{
  echo "Hola desde Cloud Shell!"
  echo "Fecha: $(date -u +%Y-%m-%dT%H:%M:%SZ)"
  echo "Subido por: ${USER:-$(whoami)}"
} > "$TMP"

step "Subiendo blob 'saludo.txt'"
az storage blob upload \
  --account-name "$STORAGE" \
  --container-name pruebas \
  --name saludo.txt \
  --file "$TMP" \
  --auth-mode login \
  --overwrite \
  --output none

step "Listando blobs"
az storage blob list \
  --account-name "$STORAGE" \
  --container-name pruebas \
  --auth-mode login \
  --query "[].{name:name, size:properties.contentLength, type:properties.blobType}" \
  --output table

step "Descargando blob para verificar"
DEST=$(mktemp -t cloudshell-download.XXXXXX)
az storage blob download \
  --account-name "$STORAGE" \
  --container-name pruebas \
  --name saludo.txt \
  --file "$DEST" \
  --auth-mode login \
  --output none

echo
echo "Contenido descargado:"
echo "---"
cat "$DEST"
echo "---"

rm -f "$TMP" "$DEST"

ok "Upload + download verificados"
echo
echo "Siguiente: ./04-jmespath-queries.sh"
