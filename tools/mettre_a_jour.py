#!/usr/bin/env python3
"""Met à jour les données du wiki après une modification des fichiers Excel, sans recompiler le mod.

Usage :
    python tools/mettre_a_jour.py --jeu "G:\\SteamLibrary\\steamapps\\common\\Nuclear Option"
    python tools/mettre_a_jour.py --jeu "<dossier du jeu>" --export "<chemin d'un export CSV>"
    python tools/mettre_a_jour.py                     (régénère seulement data/weapons.json)

Étapes :
1. Si un export du mod est disponible (--export, ou le plus récent BepInEx/config/NuclearOptionWiki_armes_v*.csv
   du dossier du jeu), il est copié dans data/dump/ et la correspondance Excel ↔ jeu est recalculée.
2. data/weapons.json est régénéré depuis data/Armes_regroupees.xlsx et data/sources/*.xlsx.
3. Avec --jeu : weapons.json est copié à côté de chaque NuclearOptionWiki.dll trouvée sous BepInEx/plugins.
   Le mod le lit au prochain lancement du jeu (il est prioritaire sur les données intégrées à la DLL).

Prérequis : Python 3 et openpyxl (pip install openpyxl).
"""
import argparse
import glob
import os
import shutil
import subprocess
import sys
from datetime import date

RACINE = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUTILS = os.path.join(RACINE, "tools")


def lancer(script, *args):
    print(f"\n> {script} {' '.join(args)}")
    subprocess.run([sys.executable, os.path.join(OUTILS, script), *args], check=True)


def dernier_export(jeu):
    fichiers = glob.glob(os.path.join(jeu, "BepInEx", "config", "NuclearOptionWiki_armes_v*.csv"))
    return max(fichiers, key=os.path.getmtime) if fichiers else None


def main():
    ap = argparse.ArgumentParser(description="Met à jour weapons.json du wiki d'armes (sans recompiler le mod).")
    ap.add_argument("--jeu", help="Dossier du jeu Nuclear Option (contenant BepInEx).")
    ap.add_argument("--export", help="Export CSV du mod (option F1 « Exporter la liste des armes »).")
    args = ap.parse_args()

    try:
        import openpyxl  # noqa: F401
    except ImportError:
        sys.exit("Il manque openpyxl : lance « pip install openpyxl » puis recommence.")

    export = args.export or (dernier_export(args.jeu) if args.jeu else None)
    if export:
        cible = os.path.join(RACINE, "data", "dump", f"{date.today().isoformat()}_{os.path.basename(export)}")
        os.makedirs(os.path.dirname(cible), exist_ok=True)
        shutil.copy2(export, cible)
        print(f"Export utilisé : {export}")
        lancer("correspondance.py", cible)
    else:
        print("Aucun export du jeu fourni : la correspondance existante (data/correspondance.csv) est conservée.")

    lancer("xlsx_vers_json.py")
    source = os.path.join(RACINE, "data", "weapons.json")

    if args.jeu:
        dlls = glob.glob(os.path.join(args.jeu, "BepInEx", "plugins", "**", "NuclearOptionWiki.dll"), recursive=True)
        if not dlls:
            sys.exit("NuclearOptionWiki.dll introuvable sous BepInEx/plugins : vérifie le dossier du jeu (--jeu).")
        for dll in dlls:
            dest = os.path.join(os.path.dirname(dll), "weapons.json")
            shutil.copy2(source, dest)
            print(f"Copié : {dest}")
        if len(dlls) > 1:
            print("Attention : plusieurs copies du mod trouvées. N'en garde qu'une seule.")
        print("\nTerminé. Relance le jeu : le journal BepInEx doit afficher « [Wiki] Données lues depuis … weapons.json ».")
    else:
        print(f"\nTerminé : {source}. Copie ce fichier à côté de NuclearOptionWiki.dll (ou relance avec --jeu).")


if __name__ == "__main__":
    main()
