#!/usr/bin/env python3
"""Produit data/aircraft.json (schéma 1, plan 7.1) depuis NO-stats.xlsx, le dernier export du mod et Avions_wiki.xlsx.

Les corrections (feuille « Corrections », A20) sont appliquées en premier ; notes, tailles, classes et seuils sont
calculés ensuite sur les valeurs corrigées. Les emports, l'IR et les désignateurs laser sont lus en jeu : le JSON
ne porte pour eux que les traductions et les valeurs forcées.

Usage : python tools/avions_vers_json.py [--classeur data/Avions_wiki.xlsx] [--sortie data/aircraft.json]
"""
import argparse
import json
import os
import re
import sys
from datetime import date

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import avions_commun as ac  # noqa: E402

SORTIE = os.path.join(ac.RACINE, "data", "aircraft.json")

CHAMPS_JSON = [
    "nom", "code", "source", "type", "taille", "encombrement_m2", "longueur_m", "envergure_m", "roles", "tags", "description_fr",
    "texte_perso", "rang", "cout_m", "vitesse_kmh", "decrochage_kmh", "maniabilite", "g_max", "masse_vide_kg", "masse_max_kg",
    "charge_utile_kg", "carburant_kg", "poussee_kn", "puissance_kw", "decollage_vertical", "decollage_m", "decollage_ms",
    "radar_m", "brouilleur", "canons_fixes", "leurres", "paillettes", "blindage", "rcs_m2", "laser_cibles",
    "note_vitesse", "note_charge", "note_cout", "note_rcs",
]
# Lus en jeu : le JSON ne les porte que s'ils sont corrigés (sinon null, le mod lit la valeur en jeu).
LUS_EN_JEU = {"laser_cibles"}


def propre(v):
    if isinstance(v, float):
        return ac.entier_si_rond(round(v, 6))
    return v


def appareil_json(a):
    d = {}
    for champ in CHAMPS_JSON:
        if champ in ("roles", "tags"):
            v = [r.strip() for r in re.split(r"[·,;]", a[champ] or "") if r.strip()]
        elif champ in LUS_EN_JEU and champ not in a["corriges"]:
            v = None
        else:
            v = a.get(champ)
        d[champ] = propre(v)
    d["corriges"] = sorted(c for c in a["corriges"] if not c.startswith("ir_force."))
    d["ir_force"] = {k: propre(v) for k, v in a["ir_force"].items()}
    forces = {}
    for s in a["emplacements"]:
        if s["force"]:
            f = {k: None for k in ("points", "soute", "charge_kg", "categories", "nb_armes")}
            f["masquer"] = False
            for k, v in s["force"].items():
                f[k] = propre(v)
            forces[s["nom_jeu"]] = f
    d["emports_forces"] = forces
    return d


def construire_json(modele):
    apps = modele["appareils"]
    return {
        "schema": 1,
        "genere_le": date.today().isoformat(),
        "source": "Avions_wiki.xlsx",
        "seuils": {k: {kk: propre(vv) for kk, vv in v.items()} if v else None for k, v in modele["seuils"].items()},
        "appareils": {a["cle"]: appareil_json(a) for a in apps},
        "emplacements_fr": {a["cle"]: {s["nom_jeu"]: s["nom_fr"] for s in a["emplacements"] if s["nom_fr"]} for a in apps},
        "alias": {a["nom"]: a["cle"] for a in apps if a["nom"]},
    }


def main():
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument("--classeur", default=ac.CLASSEUR)
    ap.add_argument("--sortie", default=SORTIE)
    args = ap.parse_args()
    if not os.path.exists(args.classeur):
        sys.exit(f"{args.classeur} introuvable : lance d'abord tools/avions_classeur.py.")
    modele = ac.construire(ac.lire_classeur(args.classeur))
    data = construire_json(modele)
    noms = list(data["alias"])
    doublons = {n for n in noms if noms.count(n) > 1}
    if doublons:
        print("Attention : noms en double (alias ambigus) :", ", ".join(sorted(doublons)))
    with open(args.sortie, "w", encoding="utf-8", newline="\n") as f:
        json.dump(data, f, ensure_ascii=False, indent=1, sort_keys=False)
        f.write("\n")
    appliquees = sum(1 for e in modele["corrections"] if e == "appliquée")
    print(f"{args.sortie} : {len(data['appareils'])} appareils, {appliquees}/{len(modele['corrections'])} correction(s) appliquée(s).")


if __name__ == "__main__":
    main()
