#!/usr/bin/env python3
"""Convertit data/Armes_regroupees.xlsx (+ data/correspondance.csv s'il existe) en data/weapons.json.

Usage : python3 tools/xlsx_vers_json.py [--xlsx data/Armes_regroupees.xlsx] [--out data/weapons.json]

- Feuille « Armes » : ligne 1 = groupes, ligne 2 = en-têtes, données à partir de la ligne 3.
- Toute valeur vide, « N/A » ou texte dans une colonne numérique devient null.
- Notes calculées (plan, décisions D10 et D13) :
    note_portee_log  = 1 + 9 × (log10(portée_m) − 2) / 4, bornée à [1 ; 10], 1 décimale
    note_portee_rang = 1 + 9 × (nb de portées ≤ p − 1) / (n − 1), ex æquo en haut du groupe
    note_rcs         = arrondi(5 × (log10(rcs) + 3) / 3), borné à [0 ; 5]
  Les arrondis se font « au demi supérieur » (comme MidpointRounding.AwayFromZero en C#).
- Clé d'une arme : WeaponInfo.name si la correspondance la donne (statut auto ou manuel),
  sinon la clé provisoire « Arme|Source » avec "cle_provisoire": true.
- Le fichier produit est déterministe : pas de date, mais l'empreinte SHA-256 de l'Excel.
"""
import argparse
import csv
import hashlib
import json
import math
import os
import sys

import openpyxl

RACINE = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# Index des colonnes de la feuille « Armes » (0 = colonne A).
COL = {
    "nom": 0, "source": 1, "type": 2, "guidage": 3, "penetration": 4, "explosif_kg": 5,
    "cout_m": 6, "explosif_par_m": 7, "portee_min_m": 8, "portee_max_m": 9, "vitesse_ms": 10,
    "note_vitesse": 11, "approche": 12, "laser_requis": 13, "tire_et_oublie": 14,
}
AIR_SOL = ["vehicules_legers", "chars", "batiments_legers", "gros_batiments", "bunker",
           "navire_leger", "navire_moyen", "porte_avions"]          # colonnes P à W
AIR_AIR = ["petit_avion", "moyen_avion", "gros_avion"]               # colonnes X à Z
COL_AGILITE, COL_RCS, COL_IR, COL_LASER, COL_DIFF = 26, 27, 28, 29, 30
EN_TETES_ATTENDUS = {0: "Arme", 1: "Source", 9: "Portée max (m)", 11: "Vitesse indicielle (/10)",
                     15: "Véhicules légers", 23: "Petit avion", 27: "Signature radar (RCS wiki, valeur brute)",
                     30: "Difficulté à toucher (site AA complet)"}
STATUTS_MAPPES = {"auto", "manuel"}


def arrondi(x, decimales=0):
    """Arrondi au demi supérieur (évite l'arrondi bancaire de round())."""
    f = 10 ** decimales
    return math.floor(abs(x) * f + 0.5) / f * (1 if x >= 0 else -1)


def nombre(v):
    if isinstance(v, bool):
        return None
    if isinstance(v, (int, float)):
        if isinstance(v, float) and math.isnan(v):
            return None
        return int(v) if float(v).is_integer() else float(v)
    return None


def texte(v):
    if v is None:
        return None
    s = str(v).strip()
    return None if s in ("", "N/A") else s


def oui_non(v):
    s = texte(v)
    if s is None:
        return None
    s = s.lower()
    return True if s in ("oui", "o", "yes", "1", "vrai") else False if s in ("non", "n", "no", "0", "faux") else None


def note_entiere(v):
    n = nombre(v)
    return None if n is None else int(arrondi(n))


def note_portee_log(p):
    if p is None or p <= 0:
        return None
    return arrondi(min(10.0, max(1.0, 1 + 9 * (math.log10(p) - 2) / 4)), 1)


def note_rcs(rcs):
    if rcs is None:
        return None
    if rcs <= 0:
        return 0
    return int(min(5, max(0, arrondi(5 * (math.log10(rcs) + 3) / 3))))


def lire_correspondance(chemin):
    """Renvoie {cle_excel: (info_name, [mount_jsonkeys])} pour les lignes mappées."""
    resultat = {}
    if not os.path.exists(chemin):
        return resultat
    with open(chemin, encoding="utf-8", newline="") as f:
        for ligne in csv.DictReader(f, delimiter=";"):
            if (ligne.get("statut") or "").strip() not in STATUTS_MAPPES:
                continue
            info = (ligne.get("info_name") or "").strip()
            if not info:
                continue
            mounts = [m.strip() for m in (ligne.get("mount_jsonkeys") or "").split(",") if m.strip()]
            resultat[ligne["excel_cle"].strip()] = (info, mounts)
    return resultat


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--xlsx", default=os.path.join(RACINE, "data", "Armes_regroupees.xlsx"))
    ap.add_argument("--correspondance", default=os.path.join(RACINE, "data", "correspondance.csv"))
    ap.add_argument("--out", default=os.path.join(RACINE, "data", "weapons.json"))
    args = ap.parse_args()

    ws = openpyxl.load_workbook(args.xlsx, data_only=True)["Armes"]
    lignes = list(ws.iter_rows(values_only=True))
    en_tetes = lignes[1]
    for i, attendu in EN_TETES_ATTENDUS.items():
        if (en_tetes[i] or "").strip() != attendu:
            sys.exit(f"Colonne {i + 1} : en-tête « {en_tetes[i]} », attendu « {attendu} ». Structure de l'Excel modifiée ?")
    donnees = [r for r in lignes[2:] if texte(r[0])]

    portees = sorted(p for p in (nombre(r[COL["portee_max_m"]]) for r in donnees) if p is not None and p > 0)
    n = len(portees)

    def note_portee_rang(p):
        if p is None or p <= 0 or n < 2:
            return None
        inferieures_ou_egales = sum(1 for x in portees if x <= p)
        return arrondi(1 + 9 * (inferieures_ou_egales - 1) / (n - 1), 1)

    correspondance = lire_correspondance(args.correspondance)
    armes, alias = {}, {}
    for r in donnees:
        cle_excel = f"{texte(r[COL['nom']])}|{texte(r[COL['source']])}"
        portee = nombre(r[COL["portee_max_m"]])
        rcs = nombre(r[COL_RCS])
        arme = {
            "excel": cle_excel,
            "nom": texte(r[COL["nom"]]),
            "source": texte(r[COL["source"]]),
            "type": texte(r[COL["type"]]),
            "guidage": texte(r[COL["guidage"]]),
            "approche": texte(r[COL["approche"]]),
            "penetration": nombre(r[COL["penetration"]]),
            "explosif_kg": nombre(r[COL["explosif_kg"]]),
            "cout_m": nombre(r[COL["cout_m"]]),
            "explosif_par_m": None if nombre(r[COL["explosif_par_m"]]) is None else arrondi(nombre(r[COL["explosif_par_m"]]), 1),
            "portee_min_m": nombre(r[COL["portee_min_m"]]),
            "portee_max_m": portee,
            "vitesse_ms": nombre(r[COL["vitesse_ms"]]),
            "note_vitesse": nombre(r[COL["note_vitesse"]]),
            "note_portee_log": note_portee_log(portee),
            "note_portee_rang": note_portee_rang(portee),
            "laser_requis": oui_non(r[COL["laser_requis"]]),
            "tire_et_oublie": oui_non(r[COL["tire_et_oublie"]]),
            "air_sol": {k: note_entiere(r[15 + i]) for i, k in enumerate(AIR_SOL)},
            "air_air": {k: note_entiere(r[23 + i]) for i, k in enumerate(AIR_AIR)},
            "defense": {
                "agilite": note_entiere(r[COL_AGILITE]),
                "rcs_m2": rcs,
                "note_rcs": note_rcs(rcs),
                "note_ir": note_entiere(r[COL_IR]),
                "resistance_laser": note_entiere(r[COL_LASER]),
                "difficulte_toucher": note_entiere(r[COL_DIFF]),
            },
        }
        if cle_excel in correspondance:
            cle, mounts = correspondance[cle_excel]
            for m in mounts:
                alias[m] = cle
        else:
            cle = cle_excel
            arme["cle_provisoire"] = True
        if cle in armes:
            sys.exit(f"Clé en double : {cle} (lignes « {armes[cle]['excel']} » et « {cle_excel} »). Vérifier correspondance.csv.")
        armes[cle] = arme

    with open(args.xlsx, "rb") as f:
        empreinte = hashlib.sha256(f.read()).hexdigest()
    sortie = {
        "schema": 1,
        "source": os.path.basename(args.xlsx),
        "source_sha256": empreinte,
        "armes": dict(sorted(armes.items())),
        "alias": dict(sorted(alias.items())),
    }
    with open(args.out, "w", encoding="utf-8", newline="\n") as f:
        json.dump(sortie, f, ensure_ascii=False, indent=1)
        f.write("\n")
    provisoires = sum(1 for a in armes.values() if a.get("cle_provisoire"))
    print(f"{len(armes)} armes écrites dans {os.path.relpath(args.out, RACINE)} "
          f"({len(armes) - provisoires} clés définitives, {provisoires} provisoires, {len(alias)} alias).")


if __name__ == "__main__":
    main()
