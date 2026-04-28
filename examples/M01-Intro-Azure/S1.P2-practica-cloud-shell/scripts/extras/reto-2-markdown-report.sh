#!/usr/bin/env bash
# Reto 2 (slide 20) - Generar un reporte en Markdown con RGs, recursos y coste.

source "$( dirname "${BASH_SOURCE[0]}" )/../_lib.sh"

OUTPUT="$( cd "$( dirname "${BASH_SOURCE[0]}" )/.." && pwd )/azure-report.md"

step "Generando reporte en $OUTPUT"

START_MONTH="$(date -u +%Y-%m-01)"
TODAY="$(date -u +%Y-%m-%d)"

{
  echo "# Azure Resource Report"
  echo ""
  echo "_Generado: $(date -u +%Y-%m-%dT%H:%M:%SZ)_"
  echo ""
  echo "## Resource Groups"
  echo ""
  echo "| Nombre | Region | Tags |"
  echo "|---|---|---|"
  az group list \
    --query "[].{name:name, loc:location, tags:tags}" \
    --output tsv \
    | awk -F'\t' '{
        tags = ""
        for (i = 3; i <= NF; i++) tags = tags (tags ? "; " : "") $i
        printf "| %s | %s | %s |\n", $1, $2, (tags ? tags : "_(sin tags)_")
      }'
  echo ""
  echo "## Recursos en \`$RG\`"
  echo ""
  echo "| Nombre | Tipo | Region |"
  echo "|---|---|---|"
  az resource list -g "$RG" \
    --query "[].{n:name, t:type, l:location}" \
    --output tsv \
    | awk -F'\t' '{printf "| %s | %s | %s |\n", $1, $2, $3}'
  echo ""
  echo "## Coste (mes actual)"
  echo ""
  TOTAL=$(az consumption usage list \
    --start-date "$START_MONTH" \
    --end-date "$TODAY" \
    --query "[].pretaxCost" -o tsv 2>/dev/null \
    | awk '{s+=$1} END {printf "%.2f", s}')
  echo "Total estimado (sin IVA): **${TOTAL} EUR**"
  echo ""
  echo "_Periodo: $START_MONTH a $TODAY_"
} > "$OUTPUT"

ok "Reporte generado: $OUTPUT"
echo
echo "Para ver:"
echo "  cat $OUTPUT"
echo "Para borrar:"
echo "  rm $OUTPUT"
