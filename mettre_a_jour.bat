@echo off
rem Met a jour les donnees du wiki (armes et avions) apres une modification des fichiers Excel (sans recompiler le mod).
rem 1. Remplace les fichiers dans data\ (Armes_regroupees.xlsx) et data\sources\ (NO-stats.xlsx, NO_efficacite_armes.xlsx),
rem    ou modifie les colonnes jaunes et la feuille Corrections de data\Avions_wiki.xlsx (enregistre et ferme Excel).
rem 2. Double-clique sur ce fichier. Modifie JEU ci-dessous si le jeu est installe ailleurs.
set JEU=G:\SteamLibrary\steamapps\common\Nuclear Option
cd /d "%~dp0"
python tools\mettre_a_jour.py --jeu "%JEU%"
pause
