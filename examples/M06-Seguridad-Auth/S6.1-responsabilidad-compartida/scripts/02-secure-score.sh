#!/usr/bin/env bash
# 02 - Secure Score real de Defender for Cloud (slide 10) +
# recomendaciones no saludables. SOLO LECTURA.

set -euo pipefail
source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

step "Secure Score de la suscripción (slide 10)"
az security secure-scores list \
  --query "[].{Nombre:displayName, Porcentaje:score.percentage, Actual:score.current, Max:score.max}" \
  -o table 2>/dev/null || warn "Requiere Defender for Cloud / permisos de Security Reader"

step "Top recomendaciones NO saludables (slide 10)"
az security assessment list \
  --query "[?status.code=='Unhealthy'].{Recomendacion:displayName}" \
  -o table 2>/dev/null | head -20 \
  || warn "Sin acceso a assessments (Security Reader necesario)"

step "Activity Log: operaciones 'delete' recientes (slide 12)"
az monitor activity-log list --offset 7d \
  --query "[?contains(operationName.value,'delete')].{Op:operationName.localizedValue, Quien:caller}" \
  -o table 2>/dev/null | head -15 \
  || warn "Sin acceso al Activity Log"

echo
ok "Lectura de Secure Score / recomendaciones completada"
echo "Objetivo (slide 17): Secure Score > 70%. Revísalo cada mes."
