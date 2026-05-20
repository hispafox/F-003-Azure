#!/usr/bin/env bash
# 01 - Inventaría la postura de deploy de una Web App (slides 3, 4, 8):
# slots existentes, último deploy, health checks configurados.
# SOLO LECTURA — no hace swap ni rollback.

set -euo pipefail
source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

step "Slots de la Web App '$APP_NAME' (slide 3 — swap zero-downtime)"
az webapp deployment slot list --name "$APP_NAME" --resource-group "$RG" \
  --query "[].{Slot:name, Estado:state, Hostname:defaultHostName}" \
  -o table 2>/dev/null || warn "Sin slots o sin acceso."

step "Health check configurado en la app (slide 9)"
az webapp config show --name "$APP_NAME" --resource-group "$RG" \
  --query "{HealthCheckPath:healthCheckPath, AlwaysOn:alwaysOn}" \
  -o jsonc 2>/dev/null || warn "Sin acceso."

step "Últimos 3 deployments (kudu)"
az webapp deployment list --name "$APP_NAME" --resource-group "$RG" \
  --query "[0:3].{Id:id, Estado:status, Activo:active, Tiempo:received_time}" \
  -o table 2>/dev/null || warn "Sin deployments registrados."

step "Sticky settings (slide 14 — protección de staging)"
az webapp config appsettings list-slot-settings --name "$APP_NAME" \
  --resource-group "$RG" \
  --query "{Settings:appSettingNames, ConnStrings:connectionStringNames}" \
  -o jsonc 2>/dev/null || warn "Sin sticky settings o sin acceso."

echo
ok "Inventario de deploy completado (solo lectura)"
echo "Recordatorio: smoke test post-deploy + auto-rollback si falla"
echo "(slide 9). Connection strings sticky en staging (slide 14)."
