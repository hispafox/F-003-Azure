#!/usr/bin/env bash
# 05 — Autoscale por CPU (slides 5, 6, 7).
# - min 1, max 5 instancias
# - +1 cuando CPU > 70% durante 5 min
# - -1 cuando CPU < 30% durante 10 min
# - cooldown 5 min para evitar oscilaciones (slide 28)

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

AUTOSCALE_NAME="autoscale-${PLAN}"

step "Creando regla de autoscale '$AUTOSCALE_NAME'"
az monitor autoscale create \
  --resource-group "$RG" \
  --resource "$PLAN" \
  --resource-type Microsoft.Web/serverfarms \
  --name "$AUTOSCALE_NAME" \
  --min-count 1 --max-count 5 --count 1 \
  --output none

step "Regla scale-out: CPU > 70% (5 min) -> +1 instancia"
az monitor autoscale rule create \
  --resource-group "$RG" \
  --autoscale-name "$AUTOSCALE_NAME" \
  --condition "Percentage CPU > 70 avg 5m" \
  --scale out 1 \
  --cooldown 5 \
  --output none

step "Regla scale-in: CPU < 30% (10 min) -> -1 instancia"
az monitor autoscale rule create \
  --resource-group "$RG" \
  --autoscale-name "$AUTOSCALE_NAME" \
  --condition "Percentage CPU < 30 avg 10m" \
  --scale in 1 \
  --cooldown 10 \
  --output none

ok "Autoscale configurado: 1-5 instancias, target 30-70% CPU"
echo
echo "Para disparar el autoscale en clase:"
echo "  ./07-load-test.sh 10        # 10 minutos de carga"
echo "  ./08-watch-instances.sh     # ver cómo crecen las instancias"
