#!/usr/bin/env bash
# 01 - Inventaría los pipelines del proyecto + últimas 5 ejecuciones de
# cada uno (slide 21 — troubleshooting). SOLO LECTURA.

set -euo pipefail
source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

step "Pipelines del proyecto (slide 2 — YAML as Code)"
az pipelines list -o table 2>/dev/null || warn "Sin pipelines o sin acceso."

step "Últimas 5 ejecuciones por pipeline (slide 21)"
for ID in $(az pipelines list --query "[].id" -o tsv 2>/dev/null); do
  NAME=$(az pipelines show --id "$ID" --query name -o tsv 2>/dev/null)
  echo ""
  echo "── $NAME (#$ID)"
  az pipelines runs list --pipeline-ids "$ID" --top 5 \
    --query "[].{Id:id, Estado:status, Resultado:result, Branch:sourceBranch, Iniciado:queueTime}" \
    -o table 2>/dev/null || warn "  Sin runs."
done

step "Environments del proyecto (slide 8 — aprobaciones)"
# az pipelines environment requiere `--organization` y proyecto; las
# defaults ya están configuradas en _lib.sh.
az devops invoke --area distributedtask --resource environments \
  --api-version 7.1-preview.1 \
  --query "value[].{Nombre:name, Descripcion:description}" \
  -o table 2>/dev/null || warn "Sin environments o sin acceso."

echo
ok "Inventario de pipelines completado (solo lectura)"
echo "Recordatorio: el pipeline ES código (slide 2) — versiona el YAML"
echo "y revísalo en PRs igual que el resto del proyecto."
