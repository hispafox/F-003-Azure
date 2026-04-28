#!/usr/bin/env bash
# Reto 4 (slide 20) - Generar una SAS (Shared Access Signature) de usuario
# con permiso de lectura sobre el blob saludo.txt durante 1 hora.

source "$( dirname "${BASH_SOURCE[0]}" )/../_lib.sh"

: "${STORAGE:?STORAGE no definido}"

# Cross-platform expiry (GNU date vs BSD date en Mac)
if EXPIRY=$(date -u -d "1 hour" +%Y-%m-%dT%H:%MZ 2>/dev/null); then
  :
else
  EXPIRY=$(date -u -v+1H +%Y-%m-%dT%H:%MZ)
fi

step "Generando user-delegation SAS (--as-user) con permiso 'r' hasta $EXPIRY"
SAS=$(az storage blob generate-sas \
  --account-name "$STORAGE" \
  --container-name pruebas \
  --name saludo.txt \
  --permissions r \
  --expiry "$EXPIRY" \
  --auth-mode login \
  --as-user \
  --output tsv)

if [ -z "$SAS" ]; then
  warn "No se pudo generar la SAS. Posibles causas:"
  echo "  - Falta el blob 'saludo.txt' (ejecuta primero ./03-upload-blob.sh)"
  echo "  - El usuario actual no tiene rol 'Storage Blob Delegator' sobre el storage"
  exit 1
fi

URL="https://${STORAGE}.blob.core.windows.net/pruebas/saludo.txt?${SAS}"

ok "SAS generada (valida hasta $EXPIRY)"
echo
echo "URL temporal:"
echo "  $URL"
echo
echo "Pruebala (deberia devolver el contenido del blob sin auth adicional):"
echo "  curl '$URL'"
echo
echo "Por que esto importa:"
echo "  - SAS = acceso temporal y limitado a un blob/container"
echo "  - --as-user = ligada a TU identidad (no a las keys del storage)"
echo "  - Caducidad obligatoria (no perpetuas)"
echo "  - Patron tipico para compartir un fichero con un cliente externo"
