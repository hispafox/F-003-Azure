#!/usr/bin/env bash
# 02 - Inventario de alertas y action groups en el RG (slide 8). SOLO
# LECTURA: lista lo que ya existe, no crea nada.

set -euo pipefail
source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

step "Reglas de metric-alert en $RG"
az monitor metrics alert list -g "$RG" \
  --query "[].{Name:name, Severity:severity, Window:windowSize, Frequency:evaluationFrequency, Enabled:enabled}" \
  -o table 2>&1 || warn "Sin metric alerts (o sin acceso)."

echo
step "Reglas scheduled-query (KQL-based, slide 18)"
az monitor scheduled-query list -g "$RG" \
  --query "[].{Name:name, Severity:severity, Enabled:enabled}" \
  -o table 2>&1 || warn "Sin scheduled-queries."

echo
step "Action Groups (canales de notificacion)"
az monitor action-group list -g "$RG" \
  --query "[].{Name:name, ShortName:groupShortName, Enabled:enabled}" \
  -o table 2>&1 || warn "Sin action groups."

ok "Inventario completado (solo lectura)"
