#!/usr/bin/env bash
# 02 - Lista los runs recientes del workflow `Deploy to Azure Web App`
# (slide 10). Requiere gh CLI autenticado contra el repo. SOLO LECTURA.

set -euo pipefail
source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

if ! command -v gh >/dev/null 2>&1; then
  warn "GitHub CLI (gh) no encontrado. https://cli.github.com/"
  exit 1
fi

: "${GH_REPO:?GH_REPO no definido (formato user/repo)}"

step "Ultimos runs en $GH_REPO"
gh run list --repo "$GH_REPO" --limit 10 \
  --json status,conclusion,name,displayTitle,createdAt,databaseId \
  -q '.[] | "\(.databaseId)  \(.status)  \(.conclusion // "-")  \(.name)  \(.createdAt)  \(.displayTitle)"' \
  2>&1 || warn "No se pudieron listar runs (¿gh login?)"

echo
step "Smoke contra la URL publica"
URL="https://${APP_NAME}.azurewebsites.net"
CODE=$(curl -s -o /dev/null -w "%{http_code}" --max-time 10 "$URL/" || echo "000")
echo "GET $URL -> HTTP $CODE"
if [ "$CODE" = "200" ]; then
  ok "App responde 200."
else
  warn "App devolvio HTTP $CODE (¿F1 dormido por inactividad?)."
fi
