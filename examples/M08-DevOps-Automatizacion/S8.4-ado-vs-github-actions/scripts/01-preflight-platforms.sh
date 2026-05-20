#!/usr/bin/env bash
# 01 - Preflight de plataformas: ¿tengo `az` con extensión devops y
# `gh` configurados? SOLO LECTURA — no toca configuración.

set -euo pipefail
source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

step "Azure CLI + extensión azure-devops (slide 4)"
if command -v az >/dev/null 2>&1; then
  az version --query "\"azure-cli\"" -o tsv 2>/dev/null | xargs -I{} echo "  az version: {}"
  if az extension list --query "[?name=='azure-devops'].version" -o tsv 2>/dev/null | grep -q .; then
    ok "Extensión azure-devops instalada."
  else
    warn "Falta la extensión azure-devops. Instala con: az extension add --name azure-devops"
  fi
  az account show --query "{Tenant:tenantId, Suscripcion:name}" -o jsonc 2>/dev/null \
    || warn "az no logueado. Ejecuta: az login"
else
  warn "az CLI no encontrado. https://aka.ms/InstallAzureCli"
fi

echo ""
step "GitHub CLI (slide 5)"
if command -v gh >/dev/null 2>&1; then
  gh --version 2>/dev/null | head -1 | xargs -I{} echo "  {}"
  if gh auth status >/dev/null 2>&1; then
    ok "gh autenticado."
  else
    warn "gh sin autenticar. Ejecuta: gh auth login"
  fi
else
  warn "gh CLI no encontrado. https://cli.github.com"
fi

echo ""
step "Recomendación rápida"
echo "  Si solo tienes az+devops → ADO."
echo "  Si solo tienes gh        → GitHub Actions."
echo "  Si tienes ambos          → posible Híbrido (slide 8)."
echo "  Usa el endpoint POST /plataforma/elegir para una decisión guiada."
