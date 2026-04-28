#!/usr/bin/env bash
# 06 — Perfil horario para el autoscale (slides 8, 23).
# Lun-Vie 09:00-19:00 Romance Standard Time (España):
# min 2, max 8, default 3 instancias.
# El perfil por métricas sigue activo dentro de este intervalo.

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

AUTOSCALE_NAME="autoscale-${PLAN}"

step "Añadiendo perfil 'horario-laboral' a $AUTOSCALE_NAME"
az monitor autoscale profile create \
  --resource-group "$RG" \
  --autoscale-name "$AUTOSCALE_NAME" \
  --name "horario-laboral" \
  --min-count 2 --max-count 8 --count 3 \
  --recurrence week Mon Tue Wed Thu Fri \
  --start 09:00 --end 19:00 \
  --timezone "Romance Standard Time" \
  --output none

ok "Perfil horario configurado"
echo
echo "Resultado:"
echo "  - Lun-Vie 09:00-19:00: min 2 / max 8 / start 3"
echo "  - Resto del tiempo: usa el perfil por defecto del autoscale (1-5)"
