#!/usr/bin/env python3
"""Mods ajoutés le 2026-11-10 (une fois) : rôles, description FR et noms d'emplacements des appareils des mods.

Même principe que avions_textes_v3.py : n'écrit une cellule que si elle est VIDE, les saisies de Florian ne sont
jamais écrasées. Descriptions traduites de la description anglaise du jeu (export v0.5.1) ; rôles choisis d'après
la description, et pour le F-4E (description sans indication de rôle) d'après ses emports et ses indices d'IA.
Usage : python tools/avions_textes_mods.py [--classeur data/Avions_wiki.xlsx]
"""
import argparse
import os
import sys

import openpyxl

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import avions_commun as ac  # noqa: E402
from avions_textes_v3 import EMPLACEMENTS_FR as EMPLACEMENTS_V3  # noqa: E402

EMPLACEMENTS_FR = dict(EMPLACEMENTS_V3, **{
    "Forward Bay": "Soute avant",
    "Aft Bay": "Soute arrière",
    "Dorsal": "Support dorsal",
    "Laser C-RAM": "Tourelle laser C-RAM",
    "Inner Wing": "Aile intérieure",
    "Conformal": "Rails conformes",
    "Inner Pylons": "Pylônes intérieurs",
    "Outer Pylons": "Pylônes extérieurs",
    "Left Wingtip Rail": "Rail de saumon gauche",
    "Right Wingtip Rail": "Rail de saumon droit",
    "Belly": "Point ventral",
    "External Gunpods": "Nacelles canon",
    "Centerline Pylon": "Pylône axial",
    "Fuselage Stations": "Points de fuselage",
    "Wing Pylons 2": "Pylônes d'aile 2",
    "Wing Pylons 3": "Pylônes d'aile 3",
    "Wing Pylons 4": "Pylônes d'aile 4",
    "Left Wing Pylon": "Pylône d'aile gauche",
    "Right Wing Pylon": "Pylône d'aile droit",
    "Internal Weapons Bay": "Soute à armes interne",
    "Wing Pylon": "Pylône d'aile",
})

ROLES = {
    "baanish_centurion": ["Bombardier", "Multirôle"],
    "baanish_icarus": ["Intercepteur", "Chasseur"],
    "AH31_Kukri": ["Hélicoptère d'attaque", "Appui aérien rapproché"],
    "MiG21K": ["Intercepteur", "Chasseur", "Multirôle"],
    "F4EPhantom": ["Chasseur", "Multirôle"],
    "Su25_Frogfoot": ["Attaque au sol", "Appui aérien rapproché"],
    "COINJ": ["Contre-insurrection", "Appui aérien rapproché"],
    "kestrel": ["Chasseur", "Supériorité aérienne"],
}

DESCRIPTIONS = {
    "baanish_centurion": (
        "Bombardier multirôle à grande autonomie, doté d'une aile haute télescopique, de deux turbosoufflantes à "
        "réducteur, de radars de nez et de queue et d'une tourelle laser de défense rapprochée. Il emporte des bombes "
        "ou des missiles de croisière dans deux soutes internes, ou un radôme AWACS."),
    "baanish_icarus": (
        "Un Starfighter modernisé : intercepteur monoréacteur à aile en flèche brisée, avec becs de bord d'attaque et "
        "tuyère orientable à iris. Rapide et vif en montée, il perd vite son énergie en virage. Six points d'emport "
        "accueillent des missiles et des charges d'attaque légères."),
    "AH31_Kukri": (
        "L'AH-31 Kukri est un hélicoptère d'attaque moyen bimoteur de 6,2 tonnes, mis en œuvre surtout par la Force de "
        "défense boscalienne pour la défense du territoire et l'appui aérien rapproché. Il associe une suite de capteurs "
        "de nez avancée et un petit radar multimode à un canon rotatif de 20 mm alimenté par un magasin de 1 100 obus, "
        "tirant 1 200 coups par minute. Efficace et percutant pour son prix, il compte sur un blindage robuste et sa "
        "puissance de feu plutôt que sur la furtivité de modèles comme le Chicane."),
    "MiG21K": (
        "Le Mikoyan-Gourevitch MiG-21K « Koshchei » est un intercepteur supersonique de première ligne et un chasseur "
        "d'attaque multirôle. Avec sa voilure delta classique, son rapport poussée/poids élevé et son turboréacteur à "
        "postcombustion, le Koshchei excelle dans l'interception à grande vitesse, l'agilité en combat tournoyant et la "
        "défense des bases aériennes de première ligne."),
    "F4EPhantom": (
        "F-4E Phantom II. Intégration expérimentale associant un extérieur importé et des systèmes de vol natifs adaptés."),
    "Su25_Frogfoot": "Avion d'attaque au sol biréacteur doté de dix pylônes d'aile. Rang 2.",
    "COINJ": (
        "Conçu à partir de la cellule du CI-22, le CI-23 Camel dispose d'un train d'atterrissage robuste et de capacités "
        "de décollage et d'atterrissage courts, qui lui permettent d'opérer depuis des terrains non préparés. S'il manque "
        "de capacité de survie, le CI-23 offre une agilité remarquable et peut emporter un large éventail d'armes, le tout "
        "pour un prix modique."),
    "kestrel": (
        "Petit drone de combat sans pilote mais très agile, il offre un rapport poussée/poids et une tolérance aux G sans "
        "égal, au prix de contre-mesures, de capteurs et d'une autonomie moins performants. Conçu à l'origine pour voler "
        "en totale autonomie, ses systèmes n'étaient pas prévus pour le pilotage à distance : son comportement en vol "
        "peut donc surprendre."),
}


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--classeur", default=ac.CLASSEUR)
    args = ap.parse_args()
    for cle, roles in ROLES.items():
        assert all(r in ac.ROLES for r in roles) and len(roles) <= 3, cle
    wb = openpyxl.load_workbook(args.classeur)
    ws = wb["Avions"]
    ent = [c.value for c in ws[1]]
    ecrits = 0
    for l in ws.iter_rows(min_row=2):
        cle = l[0].value
        for col, val in (("Rôle(s)", " · ".join(ROLES.get(cle, [])) or None), ("Description (FR)", DESCRIPTIONS.get(cle))):
            c = l[ent.index(col)]
            if val and not ac.texte(c.value):
                c.value = val
                ecrits += 1
    ws = wb["Emports"]
    ent = [c.value for c in ws[1]]
    manquants = set()
    for l in ws.iter_rows(min_row=2):
        nom = l[ent.index("Nom (jeu)")].value
        c = l[ent.index("Nom (FR)")]
        if not ac.texte(c.value):
            if nom in EMPLACEMENTS_FR:
                c.value = EMPLACEMENTS_FR[nom]
                ecrits += 1
            else:
                manquants.add(nom)
    wb.save(args.classeur)
    print(f"{ecrits} cellule(s) remplie(s).")
    if manquants:
        print("Noms d'emplacement non traduits :", ", ".join(sorted(str(m) for m in manquants)))


if __name__ == "__main__":
    main()
