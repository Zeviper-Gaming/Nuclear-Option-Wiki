#!/usr/bin/env bash
# Compile le mod et produit dist/NuclearOptionWiki-<version>.zip, à extraire dans le dossier du jeu.
# Contenu : BepInEx/plugins/NuclearOptionWiki/{NuclearOptionWiki.dll, weapons.json}
set -euo pipefail
RACINE="$(cd "$(dirname "$0")/.." && pwd)"
cd "$RACINE"

VERSION="$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' src/Plugin/NuclearOptionWiki.csproj)"
dotnet build src/Plugin/NuclearOptionWiki.csproj -c Release -nologo -v q

STAGE="$(mktemp -d)"
trap 'rm -rf "$STAGE"' EXIT
DEST="$STAGE/BepInEx/plugins/NuclearOptionWiki"
mkdir -p "$DEST" dist
cp src/Plugin/bin/Release/net472/NuclearOptionWiki.dll "$DEST/"
cp data/weapons.json "$DEST/"

ZIP="$RACINE/dist/NuclearOptionWiki-$VERSION.zip"
rm -f "$ZIP"
(cd "$STAGE" && python3 -c "
import os, sys, zipfile
with zipfile.ZipFile(sys.argv[1], 'w', zipfile.ZIP_DEFLATED) as z:
    for base, _, files in os.walk('BepInEx'):
        for f in sorted(files):
            z.write(os.path.join(base, f))
" "$ZIP")
echo "Paquet : dist/NuclearOptionWiki-$VERSION.zip"
