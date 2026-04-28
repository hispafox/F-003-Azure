#!/usr/bin/env bash
# 05 — Swap multi-fase (slide 12).
# Uso: ./05-swap-with-preview.sh preview   # aplica config de producción a staging
#      ./05-swap-with-preview.sh complete  # confirma el swap
#      ./05-swap-with-preview.sh reset     # cancela y revierte la config

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

ACTION="${1:-}"

case "$ACTION" in
  preview)
    step "Fase 1 — Preview: aplicando config de producción a staging"
    az webapp deployment slot swap \
      --name "$APP" --resource-group "$RG" \
      --slot staging --target-slot production \
      --action preview \
      --output none
    ok "Staging ahora tiene la config de producción (sin redirigir tráfico)."
    echo
    echo "Verifica:"
    echo "  curl https://$APP-staging.azurewebsites.net/info"
    echo "  → stickyToSlot.environmentLabel debería ser 'production'"
    echo
    echo "Cuando estés satisfecho: ./05-swap-with-preview.sh complete"
    echo "Si algo falla:           ./05-swap-with-preview.sh reset"
    ;;
  complete)
    confirm "¿Completar el swap?"
    step "Fase 2 — Completing"
    az webapp deployment slot swap \
      --name "$APP" --resource-group "$RG" \
      --slot staging --target-slot production \
      --action swap \
      --output none
    ok "Swap completado"
    ;;
  reset)
    step "Cancelando swap en preview"
    az webapp deployment slot swap \
      --name "$APP" --resource-group "$RG" \
      --slot staging --target-slot production \
      --action reset \
      --output none
    ok "Reset hecho. Staging vuelve a su config original."
    ;;
  *)
    echo "Uso: $0 preview|complete|reset"
    exit 1
    ;;
esac
