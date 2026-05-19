#!/usr/bin/env bash
# 01 - Inventario del tenant (slides 3-5, 16): tenant, usuarios (member
# vs guest), grupos. SOLO LECTURA.

set -euo pipefail
source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

step "Tenant actual (slide 3)"
az account show --query "{Tenant:tenantId, Usuario:user.name}" -o jsonc

step "Usuarios: member vs guest (slide 4)"
az ad user list --query "[].{Nombre:displayName, UPN:userPrincipalName, Tipo:userType}" \
  -o table 2>/dev/null | head -25 \
  || warn "Sin permisos de lectura del directorio (Directory Readers)"

step "Grupos de seguridad (slide 5)"
az ad group list --query "[].{Nombre:displayName, Mail:mailNickname}" \
  -o table 2>/dev/null | head -20 \
  || warn "Sin permisos para listar grupos"

step "Cuentas Guest (B2B) — revisar trimestralmente (slide 22)"
az ad user list --filter "userType eq 'Guest'" \
  --query "[].{Nombre:displayName, UPN:userPrincipalName}" -o table 2>/dev/null \
  || warn "Sin permisos o sin invitados"

echo
ok "Inventario del directorio completado (solo lectura)"
echo "Patrón recomendado (slide 5): permisos a GRUPOS, no a usuarios."
