#!/usr/bin/env bash
# 01 - Inventaría el Azure DevOps project (slides 3, 5, 9, 13).
# SOLO LECTURA — no crea ni modifica nada.

set -euo pipefail
source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

step "Repos del proyecto (slide 3 — multi-repo / monorepo)"
az repos list -o table 2>/dev/null || warn "Sin repos o sin acceso."

step "Branch policies en main por cada repo (slide 5)"
for REPO_ID in $(az repos list --query "[].id" -o tsv 2>/dev/null); do
  REPO_NAME=$(az repos show --repository "$REPO_ID" --query name -o tsv 2>/dev/null)
  echo ""
  echo "── Repo: $REPO_NAME"
  az repos policy list --repository-id "$REPO_ID" --branch main \
    --query "[].{Tipo:type.displayName, Bloqueante:isBlocking, Habilitada:isEnabled}" \
    -o table 2>/dev/null || warn "  Sin policies en main."
done

step "Work items activos del usuario actual (slide 9/11)"
az boards query --wiql \
  "SELECT [System.Id], [System.Title], [System.WorkItemType], [System.State] \
   FROM WorkItems WHERE [System.AssignedTo] = @Me AND [System.State] <> 'Closed' \
   ORDER BY [System.ChangedDate] DESC" \
  --query "[].{Id:id, Tipo:fields.\"System.WorkItemType\", Estado:fields.\"System.State\", Titulo:fields.\"System.Title\"}" \
  -o table 2>/dev/null || warn "Sin work items o sin acceso."

step "Feeds de Artifacts del proyecto (slide 13)"
az artifacts feed list --query "[].{Nombre:name, Descripcion:description}" \
  -o table 2>/dev/null || warn "Sin feeds o sin permisos."

echo
ok "Inventario de DevOps completado (solo lectura)"
echo "Recordatorio: branch policies mínimas en main = RequiredReviewers"
echo "+ BuildExitoso + ResolucionDeComentarios + NoPushDirecto (slide 5)."
