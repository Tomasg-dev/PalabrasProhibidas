#!/usr/bin/env bash
# Compila el mod y arma el zip para Thunderstore/Gale en dist/.
# Uso:  scripts/package.sh
set -euo pipefail

raiz="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$raiz"
source scripts/idioma.sh

version="$(grep -oP '"version_number":\s*"\K[^"]+' thunderstore/manifest.json)"
nombre="$(grep -oP '"name":\s*"\K[^"]+' thunderstore/manifest.json)"

# La versión del manifiesto y la del código tienen que coincidir.
if ! grep -q "public const string Version = \"$version\"" "src/$nombre/Plugin.cs"; then
  echo "La versión de thunderstore/manifest.json ($version) no coincide con la de Plugin.cs" >&2
  exit 1
fi

scripts/obtener-terceros.sh
dotnet build "src/$nombre" -c Release

temporal="$(mktemp -d)"
trap 'rm -rf "$temporal"' EXIT
cp thunderstore/manifest.json thunderstore/icon.png thunderstore/README.md thunderstore/CHANGELOG.md THIRD_PARTY_NOTICES.md "$temporal/"
cp "src/$nombre/bin/Release/netstandard2.1/$nombre.dll" "$temporal/"
# Vosk: el envoltorio de C#, la DLL nativa con sus tres dependencias, y el modelo de voz.
cp terceros/vosk/lib/netstandard2.0/Vosk.dll "$temporal/"
cp terceros/vosk/build/lib/win-x64/*.dll "$temporal/"
# El modelo va como un único zip: los gestores de mods aplanan las carpetas y Vosk necesita
# su estructura intacta. El plugin lo descomprime la primera vez en BepInEx/cache/<nombre del mod>.
(cd terceros && zip -q -r -0 "$temporal/$modelo.zip" "$modelo")

mkdir -p dist
salida="dist/$nombre-$version.zip"
rm -f "$salida"
(cd "$temporal" && zip -q -r "$raiz/$salida" .)
echo "Paquete creado: $salida ($(du -h "$salida" | cut -f1))"
