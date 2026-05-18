#!/usr/bin/env bash
# 01 - Servidor lógico SQL (slide 3) + Azure SQL Database SERVERLESS
# (slide 5: auto-pausa a los 60 min, min 0.5 vCore → ≈ 0 € parado) +
# reglas de firewall (servicios Azure + tu IP).

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

step "Resource Group: $RG"
az group create --name "$RG" --location "$LOCATION" \
  --tags "curso=AZ-204" "modulo=M05" "submodulo=S5.2" \
  --output none

step "Servidor lógico SQL: $SQL_SERVER (no es una VM — endpoint de gestión, slide 2)"
az sql server create \
  --name "$SQL_SERVER" --resource-group "$RG" --location "$LOCATION" \
  --admin-user "$SQL_ADMIN" --admin-password "$SQL_PASSWORD" \
  --output none

step "Base de datos SERVERLESS: $SQL_DB (GP_Gen5, auto-pause 60 min, slide 5)"
az sql db create \
  --server "$SQL_SERVER" --resource-group "$RG" --name "$SQL_DB" \
  --edition GeneralPurpose --family Gen5 --capacity 2 \
  --compute-model Serverless \
  --auto-pause-delay 60 \
  --min-capacity 0.5 \
  --backup-storage-redundancy Local \
  --output none

step "Firewall: permitir servicios de Azure (0.0.0.0)"
az sql server firewall-rule create \
  --server "$SQL_SERVER" --resource-group "$RG" \
  --name AllowAzureServices \
  --start-ip-address 0.0.0.0 --end-ip-address 0.0.0.0 \
  --output none

if [ "${MY_IP:-0.0.0.0}" != "0.0.0.0" ]; then
  step "Firewall: permitir tu IP ($MY_IP)"
  az sql server firewall-rule create \
    --server "$SQL_SERVER" --resource-group "$RG" \
    --name MiOficina \
    --start-ip-address "$MY_IP" --end-ip-address "$MY_IP" \
    --output none
else
  warn "MY_IP=0.0.0.0 → no se añadió regla para tu IP (ponla en .env.demo)"
fi

ok "Azure SQL serverless listo: $SQL_SERVER/$SQL_DB"
echo
echo "Connection string (SQL auth — NO lo comitees):"
echo "  $(sql_conn_string)"
echo
echo "En producción usa Managed Identity (sin password, slide 6/20):"
echo "  Server=tcp:${SQL_SERVER}.database.windows.net,1433;Database=${SQL_DB};Authentication=Active Directory Default;Encrypt=true;"
echo
echo "Siguiente: ./02-smoke-test.sh (aplica la migración InitialCreate)"
