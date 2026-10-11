#!/usr/bin/env python3
"""Convertit data/Armes_regroupees.xlsx (+ data/sources/*.xlsx et data/correspondance.csv) en data/weapons.json.

Sources complémentaires (facultatives) : data/sources/NO-stats.xlsx et data/sources/NO_efficacite_armes.xlsx.
Les armes de NO-stats absentes d'Armes_regroupees y sont ajoutées, et la note « Hélico » est reprise pour toutes.

Usage : python3 tools/xlsx_vers_json.py [--xlsx data/Armes_regroupees.xlsx] [--out data/weapons.json]

- Feuille « Armes » : ligne 1 = groupes, ligne 2 = en-têtes, données à partir de la ligne 3.
- Toute valeur vide, « N/A » ou texte dans une colonne numérique devient null.
- Notes calculées (plan, décisions D10 et D13) :
    note_portee_log  = 1 + 9 × (log10(portée_m) − 2) / 4, bornée à [1 ; 10], 1 décimale
    note_portee_rang = 1 + 9 × (nb de portées ≤ p − 1) / (n − 1), ex æquo en haut du groupe
    note_explosif_rang, note_penetration_rang, note_explosif_par_m_rang : même formule sur les valeurs > 0 ;
                       valeur 0 → note 0 (plan v1.1, D24)
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
STATUTS_MAPPES = {"auto", "manuel", "a_valider"}


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
    """
    Renvoie {cle_excel: (info_principale, [alias])} pour les lignes mappées. Une ligne Excel peut avoir
    plusieurs assets du jeu (même arme sur plusieurs plateformes) : le premier donne la clé, les autres
    noms d'assets et tous les jsonKey d'emport deviennent des alias.
    """
    resultat = {}
    if not os.path.exists(chemin):
        return resultat
    with open(chemin, encoding="utf-8", newline="") as f:
        for ligne in csv.DictReader(f, delimiter=";"):
            if (ligne.get("statut") or "").strip() not in STATUTS_MAPPES:
                continue
            info = (ligne.get("info_name") or "").strip()
            cle_excel = (ligne.get("excel_cle") or "").strip()
            if not info or not cle_excel:
                continue
            mounts = [m.strip() for m in (ligne.get("mount_jsonkeys") or "").split(",") if m.strip()]
            if cle_excel not in resultat:
                resultat[cle_excel] = (info, list(mounts))
            else:
                resultat[cle_excel][1].extend([info] + mounts)
    return resultat


SOURCES = os.path.join(RACINE, "data", "sources")


def _feuille(chemin, nom, ligne_entete):
    lignes = list(openpyxl.load_workbook(chemin, read_only=True, data_only=True)[nom].iter_rows(values_only=True))
    return lignes[ligne_entete + 1:]


def lignes_completes(xlsx, stats=None, efficacite=None):
    """
    Lignes au format de la feuille « Armes » d'Armes_regroupees.xlsx (31 colonnes), complétées par :
    - les armes de NO-stats.xlsx absentes d'Armes_regroupees (ligne reconstruite à partir de NO-stats,
      « Notes efficacité » et « Vulnérabilité défenses ») ;
    - la note « Hélico » de « Notes efficacité », pour toutes les armes (renvoyée à part : {cle: note}).
    Renvoie (lignes, notes_helico, cles_ajoutees). Sans fichiers sources : seulement Armes_regroupees.
    """
    stats = stats or os.path.join(SOURCES, "NO-stats.xlsx")
    efficacite = efficacite or os.path.join(SOURCES, "NO_efficacite_armes.xlsx")
    ws = openpyxl.load_workbook(xlsx, data_only=True)["Armes"]
    lignes = list(ws.iter_rows(values_only=True))
    en_tetes = lignes[1]
    for i, attendu in EN_TETES_ATTENDUS.items():
        if (en_tetes[i] or "").strip() != attendu:
            sys.exit(f"Colonne {i + 1} : en-tête « {en_tetes[i]} », attendu « {attendu} ». Structure de l'Excel modifiée ?")
    donnees = [list(r) for r in lignes[2:] if texte(r[0])]
    helico, ajoutees = {}, []
    if not (os.path.exists(stats) and os.path.exists(efficacite)):
        return donnees, helico, ajoutees

    notes = {(texte(r[0]), texte(r[1])): r for r in _feuille(efficacite, "Notes efficacité", 7) if texte(r[0])}
    defenses = {(texte(r[0]), texte(r[1])): r for r in _feuille(efficacite, "Vulnérabilité défenses", 7) if texte(r[0])}
    for cle, n in notes.items():
        helico[f"{cle[0]}|{cle[1]}"] = note_entiere(n[13])

    existantes = {(texte(r[0]), texte(r[1])) for r in donnees}
    for st in _feuille(stats, "Armes", 0):
        cle = (texte(st[1]), texte(st[0]))
        if not cle[0] or cle in existantes:
            continue
        n = notes.get(cle) or [None] * 16
        v = defenses.get(cle) or [None] * 30
        vitesse = nombre(st[16]) or nombre(st[12]) or nombre(st[11])
        ligne = [st[1], st[0], st[2], st[3], st[7], st[6], st[30], st[31], st[18], st[19], vitesse, None, st[4], None, None,
                 n[4], n[5], n[6], n[7], n[8], n[9], n[10], n[11], n[12], n[14], n[15],
                 None, v[5], v[8], v[13], v[17]]
        donnees.append(ligne)
        ajoutees.append(f"{cle[0]}|{cle[1]}")
    return donnees, helico, ajoutees


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--xlsx", default=os.path.join(RACINE, "data", "Armes_regroupees.xlsx"))
    ap.add_argument("--correspondance", default=os.path.join(RACINE, "data", "correspondance.csv"))
    ap.add_argument("--out", default=os.path.join(RACINE, "data", "weapons.json"))
    args = ap.parse_args()

    donnees, notes_helico, ajoutees = lignes_completes(args.xlsx)

    # Indice de vitesse /10 des lignes ajoutées depuis NO-stats : même formule que la feuille « Vitesse (calcul) »
    # (1 + 9 × rang centile parmi toutes les vitesses, ex æquo en haut du groupe).
    vitesses = sorted(v for v in (nombre(r[COL["vitesse_ms"]]) for r in donnees) if v is not None and v > 0)
    for r in donnees:
        v = nombre(r[COL["vitesse_ms"]])
        if nombre(r[COL["note_vitesse"]]) is None and v and len(vitesses) > 1:
            r[COL["note_vitesse"]] = arrondi(1 + 9 * (sum(1 for x in vitesses if x <= v) - 1) / (len(vitesses) - 1), 1)

    portees = sorted(p for p in (nombre(r[COL["portee_max_m"]]) for r in donnees) if p is not None and p > 0)
    n = len(portees)

    def note_portee_rang(p):
        if p is None or p <= 0 or n < 2:
            return None
        inferieures_ou_egales = sum(1 for x in portees if x <= p)
        return arrondi(1 + 9 * (inferieures_ou_egales - 1) / (n - 1), 1)

    def noteur_rang(colonne):
        """Note /10 au rang (plan v1.1, D24) : 1 + 9 × rang centile parmi les valeurs > 0 ; 0 → 0 ; absent → null."""
        valeurs = sorted(v for v in (nombre(r[colonne]) for r in donnees) if v is not None and v > 0)
        def note(v):
            if v is None:
                return None
            if v <= 0:
                return 0
            if len(valeurs) < 2:
                return 10
            return arrondi(1 + 9 * (sum(1 for x in valeurs if x <= v) - 1) / (len(valeurs) - 1), 1)
        return note

    note_explosif_rang = noteur_rang(COL["explosif_kg"])
    note_penetration_rang = noteur_rang(COL["penetration"])
    note_explosif_par_m_rang = noteur_rang(COL["explosif_par_m"])

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
            "note_explosif_rang": note_explosif_rang(nombre(r[COL["explosif_kg"]])),
            "note_penetration_rang": note_penetration_rang(nombre(r[COL["penetration"]])),
            "note_explosif_par_m_rang": note_explosif_par_m_rang(nombre(r[COL["explosif_par_m"]])),
            "laser_requis": oui_non(r[COL["laser_requis"]]),
            "tire_et_oublie": oui_non(r[COL["tire_et_oublie"]]),
            "air_sol": {k: note_entiere(r[15 + i]) for i, k in enumerate(AIR_SOL)},
            "air_air": dict({k: note_entiere(r[23 + i]) for i, k in enumerate(AIR_AIR)},
                            helico=notes_helico.get(f"{texte(r[COL['nom']])}|{texte(r[COL['source']])}")),
            "defense": {
                "agilite": note_entiere(r[COL_AGILITE]),
                "rcs_m2": rcs,
                "note_rcs": note_rcs(rcs),
                "note_ir": note_entiere(r[COL_IR]),
                "resistance_laser": note_entiere(r[COL_LASER]),
                "difficulte_toucher": note_entiere(r[COL_DIFF]),
            },
        }
        if cle_excel in ajoutees:
            arme["origine"] = "NO-stats"
        if cle_excel in correspondance:
            cle, autres = correspondance[cle_excel]
            for a in autres:
                if a != cle:
                    alias[a] = cle
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
    if ajoutees:
        print(f"{len(ajoutees)} arme(s) ajoutée(s) depuis data/sources/NO-stats.xlsx : " + ", ".join(ajoutees))
    print(f"{len(armes)} armes écrites dans {os.path.relpath(args.out, RACINE)} "
          f"({len(armes) - provisoires} clés définitives, {provisoires} provisoires, {len(alias)} alias).")


if __name__ == "__main__":
    main()
