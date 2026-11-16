#!/usr/bin/env bash
# Descarga Vosk (paquete oficial de NuGet) y el modelo pequeño de voz del idioma del mod en terceros/.
# Solo hace falta una vez. Uso:  scripts/obtener-terceros.sh
set -euo pipefail

raiz="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
source "$raiz/scripts/idioma.sh"
mkdir -p "$raiz/terceros"
cd "$raiz/terceros"

version_vosk="0.3.38"

if [ ! -f vosk/lib/netstandard2.0/Vosk.dll ]; then
  curl -L --fail -o vosk.nupkg "https://api.nuget.org/v3-flatcontainer/vosk/$version_vosk/vosk.$version_vosk.nupkg"
  unzip -q -o vosk.nupkg 'lib/*' 'build/lib/win-x64/*' -d vosk
fi

if [ ! -f "$modelo/am/final.mdl" ]; then
  curl -L --fail -o modelo.zip "https://alphacephei.com/vosk/models/$modelo.zip"
  unzip -q -o modelo.zip
  rm -f modelo.zip
fi

echo "Listo: terceros/vosk y el modelo $modelo"
