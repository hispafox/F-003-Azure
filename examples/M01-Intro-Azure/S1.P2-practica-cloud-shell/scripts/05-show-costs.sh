#!/usr/bin/env bash
# 05 - Consultar costes con az consumption (slide 11).
# Funciona con la suscripcion activa. Para suscripciones EA / MCA / partner
# las consultas pueden requerir permisos extras.

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

START_MONTH="$(date -u +%Y-%m-01)"
TODAY="$(date -u +%Y-%m-%d)"

echo "============================================================"
echo " Resumen de coste para la suscripcion activa"
echo "============================================================"
echo "Periodo: $START_MONTH a $TODAY"
echo

step "Consumo del mes hasta ahora (top 10 lineas mas caras)"
if ! az consumption usage list \
  --start-date "$START_MONTH" \
  --end-date "$TODAY" \
  --query "sort_by([], &pretaxCost)[-10:].{servicio:meterCategory, recurso:instanceName, coste:pretaxCost}" \
  --output table 2>/dev/null; then
  warn "No pude leer az consumption (suscripcion sin permisos o tipo no soportado)."
  echo "    Esto es normal en suscripciones de cliente sin Cost Management Reader."
  echo "    Alternativa: Portal -> Cost Management + Billing"
  exit 0
fi

echo
step "Total acumulado (suma simple)"
TOTAL=$(az consumption usage list \
  --start-date "$START_MONTH" \
  --end-date "$TODAY" \
  --query "[].pretaxCost" -o tsv 2>/dev/null \
  | awk '{s+=$1} END {printf "%.2f", s}')
echo "  Total mes actual: $TOTAL EUR (aprox, sin IVA)"

echo
step "Coste por servicio (top 5)"
az consumption usage list \
  --start-date "$START_MONTH" \
  --end-date "$TODAY" \
  --query "[].{servicio:meterCategory, coste:pretaxCost}" \
  --output tsv 2>/dev/null \
  | awk -F'\t' '{costs[$1]+=$2} END {for (s in costs) printf "  %-30s %.2f EUR\n", s, costs[s]}' \
  | sort -t' ' -k2 -rn \
  | head -5

echo
echo "Lo que NO ves aqui (solo en Portal):"
echo "  - Forecasting de coste futuro"
echo "  - Recomendaciones de Advisor (savings plans)"
echo "  - Dashboards visuales"
echo "  Portal -> Cost Management + Billing"
