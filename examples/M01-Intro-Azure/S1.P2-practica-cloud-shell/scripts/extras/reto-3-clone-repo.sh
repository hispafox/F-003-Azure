#!/usr/bin/env bash
# Reto 3 (slide 20) - Clonar el repo Azure-Samples/azure-cli-samples en
# Cloud Shell y explorar su contenido.

source "$( dirname "${BASH_SOURCE[0]}" )/../_lib.sh"

DEST="${HOME}/cli-samples"

if [ -d "$DEST/.git" ]; then
  step "Repo ya existe en $DEST - haciendo git pull"
  ( cd "$DEST" && git pull --rebase --quiet )
else
  step "Clonando https://github.com/Azure-Samples/azure-cli-samples a $DEST"
  git clone --depth 1 https://github.com/Azure-Samples/azure-cli-samples "$DEST"
fi

ok "Repo listo en $DEST"
echo
step "Estructura de primer nivel:"
ls -la "$DEST" | head -25

echo
step "Buscando ejemplos relacionados con App Service..."
find "$DEST" -maxdepth 3 -type d -iname "*app*" 2>/dev/null | head -10

echo
echo "Sigue explorando con:"
echo "  cd $DEST"
echo "  ls"
echo "  cat <subcarpeta>/README.md"
