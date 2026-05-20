#!/usr/bin/env bash
# 01 - Ejecuta las queries KQL canonicas (slide 5/26) contra el recurso
# de App Insights configurado. SOLO LECTURA: no aplica cambios.

set -euo pipefail
source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

run_query() {
  local nombre="$1"
  local query="$2"
  step "$nombre (slide 5/26)"
  az monitor app-insights query \
    --app "$APP_INSIGHTS_NAME" -g "$RG" \
    --analytics-query "$query" \
    -o table 2>&1 || warn "Query con problemas (¿hay datos en la ventana?)"
  echo
}

run_query "Top 10 endpoints mas lentos (P95, ultimas 24h)" \
  "requests | where timestamp > ago(24h) | summarize p95=percentile(duration,95), count_=count() by name | where count_ > 100 | order by p95 desc | take 10"

run_query "Tasa de errores por hora (ultimos 7d)" \
  "requests | where timestamp > ago(7d) | summarize total=count(), errores=countif(resultCode >= 500) by bin(timestamp, 1h) | extend tasaError = round(errores * 100.0 / total, 2) | where tasaError > 0"

run_query "Excepciones agrupadas por tipo (ultimas 24h)" \
  "exceptions | where timestamp > ago(24h) | summarize count_=count() by type, outerMessage | order by count_ desc | take 10"

run_query "Dependencias lentas (> 1s, ultimas 24h)" \
  "dependencies | where timestamp > ago(24h) | where duration > 1000 | summarize avgDur=avg(duration), count_=count() by target, type, name | order by avgDur desc | take 10"

ok "Queries KQL ejecutadas (solo lectura)"
echo "Recordatorio: si no hay datos, el recurso es nuevo o no tiene"
echo "trafico. Asegurate de que App Insights esta enlazado en la app."
