#!/usr/bin/env bash
# 02 - Smoke test: aplica la migración InitialCreate al Azure SQL
# provisionado con `dotnet ef database update` (slide 8 — esto NO es
# "lanzar la app", es la herramienta de migraciones) y verifica que
# quedó aplicada. La API la lanza el alumno con `dotnet run`.

set -euo pipefail
source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

API_PROJ="$SCRIPT_DIR/../src/Sql.Demo.Api/Sql.Demo.Api.csproj"
CONN="$(sql_conn_string)"

if ! command -v dotnet >/dev/null 2>&1; then
  echo "[X] dotnet SDK no encontrado."; exit 1
fi
dotnet tool list -g 2>/dev/null | grep -q dotnet-ef || \
  warn "dotnet-ef no parece instalado globalmente (dotnet tool install -g dotnet-ef)"

step "Aplicando migración InitialCreate (slide 8)"
warn "1ª conexión: la DB serverless puede tardar ~30 s en despertar (slide 5)"
dotnet ef database update \
  --project "$API_PROJ" \
  --connection "$CONN"

step "Verificando migraciones aplicadas"
dotnet ef migrations list \
  --project "$API_PROJ" \
  --connection "$CONN"

echo
ok "Smoke test OK — esquema Productos/Pedidos creado en Azure SQL"
echo "Para probar la API: pon SqlConnection con la cs de 01-provision y"
echo "ejecuta tú  dotnet run --project src/Sql.Demo.Api"
