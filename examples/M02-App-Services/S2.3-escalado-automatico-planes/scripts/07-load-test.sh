#!/usr/bin/env bash
# 07 — Genera carga CPU sostenida bombardeando /load/cpu en bucle.
# El objetivo es subir la métrica CpuPercentage del plan por encima del 70%
# durante el tiempo suficiente como para que el autoscale añada instancias.
# Uso: ./07-load-test.sh [duracion_min] [paralelos] [ms_por_request]

source "$( dirname "${BASH_SOURCE[0]}" )/_lib.sh"

DURATION_MIN="${1:-7}"
PARALLEL="${2:-10}"
MS_PER_REQUEST="${3:-2000}"
URL="https://${APP}.azurewebsites.net/load/cpu?ms=${MS_PER_REQUEST}"

echo "URL:        $URL"
echo "Duracion:   $DURATION_MIN min"
echo "Paralelos:  $PARALLEL"
echo
echo "Mientras dura, abre Portal -> $PLAN -> Metrics -> CpuPercentage."
echo "Si tienes autoscale-cpu (./05-autoscale-cpu.sh), las instancias subiran."
echo "Verifica con: ./08-watch-instances.sh"
echo
confirm "Empezar?"

end=$(( $(date +%s) + DURATION_MIN * 60 ))
batch=0
while [ "$(date +%s)" -lt "$end" ]; do
  batch=$((batch + 1))
  for _ in $(seq 1 "$PARALLEL"); do
    curl -s -o /dev/null -w "" "$URL" &
  done
  wait
  remaining=$(( end - $(date +%s) ))
  echo "[batch $batch] $remaining s restantes"
done
ok "Carga finalizada tras $DURATION_MIN min"
