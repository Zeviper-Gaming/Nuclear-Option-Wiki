#!/usr/bin/env python3
"""Propose data/correspondance.csv : ligne Excel « Arme|Source » ↔ nom d'asset WeaponInfo du jeu.

Usage : python3 tools/correspondance.py data/dump/<export>.csv [--xlsx data/Armes_regroupees.xlsx]

Entrées : l'export du mod (option « Exporter la liste des armes ») et l'Excel.
Sortie  : data/correspondance.csv (UTF-8, « ; »), colonnes
          excel_cle ; info_name ; mount_jsonkeys ; statut ; commentaire

Statuts :
  auto        nom identique et sans ambiguïté, ou ambiguïté levée par la source
              (jeu de base ↔ asset sans préfixe Aryx ; table SOURCES_SURES pour les sources de mods)
  a_valider   ambiguïté levée par le nom de code d'avion Aryx (table AVIONS_ARYX) : à confirmer par Florian
  manuel      décision de Florian (jamais écrasée : les lignes « manuel » du fichier existant sont conservées)
  ambigu      plusieurs candidats, aucune règle ne tranche, y compris les stats (export v0.3.0 et plus)
  introuvable ligne Excel sans arme correspondante dans l'export
  absent_excel   arme montable en hangar absente de l'Excel
  hors_hangar    arme du jeu sans emport d'avion (véhicules, navires) absente de l'Excel

xlsx_vers_json.py utilise les statuts auto, a_valider et manuel. Une même ligne Excel peut
apparaître plusieurs fois : la première donne la clé, les suivantes deviennent des alias.
"""
import argparse
import collections
import csv
import os
import re

import openpyxl

RACINE = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SORTIE = os.path.join(RACINE, "data", "correspondance.csv")
COLONNES = ["excel_cle", "info_name", "mount_jsonkeys", "statut", "commentaire"]

PREFIXES_MODS = ("Aryx_", "kar_", "MC260_", "truk_", "HLT", "P_Trisurface", "PB_")

# Source Excel → préfixes d'assets. Noms de code d'avions validés par Florian le 2026-10-09.
SOURCES_SURES = {
    "Aryx Weaponry Pack": ("Aryx_Wep_", "Aryx_ClusterBomb1", "Aryx_SwivelGunpod"),
    "Aryx OA-27 Cavalier": ("Aryx_PropAttacker1",),
    "Aryx RAH-72": ("Aryx_LightHeli1",),
    "Aryx F-99 Shrike": ("Aryx_LightFighter1",),
    "Aryx MC-260 Chimera": ("Aryx_MC260", "MC260_"),
    "Aryx NAVEX": ("Aryx_Gunboat", "Aryx_Supercarrier", "Aryx_Destroyer", "Aryx_Frigate"),
    "FS-3 Ternion": ("P_Trisurface1",),
}
# Hypothèses non confirmées (aucun cas ne les utilise pour l'instant).
AVIONS_ARYX = {
    "Aryx FS-41 Eclipse": ("Aryx_Interceptor1",),
    "Aryx F-16M": ("Aryx_KingRaptor",),
}


def nom_de_base(nom):
    return re.sub(r"\s*\(variante \d+\)$", "", nom or "").strip()


def num(v):
    if isinstance(v, bool):
        return None
    if isinstance(v, (int, float)):
        return float(v)
    try:
        return float(str(v).replace(",", "."))
    except (TypeError, ValueError):
        return None


def stats_export(r):
    """Stats d'une ligne d'export (colonnes ajoutées en v0.3.0 ; absentes des exports plus anciens)."""
    if "penetration" not in r:
        return None
    vi, vmax = num(r.get("vitesse_initiale")), num(r.get("vitesse_max"))
    # Excel (feuille « Vitesse (calcul) ») : munitions propulsées = vitesse max ;
    # armes à feu = vitesse initiale, plafonnée par la vitesse max fixée si elle est plus basse.
    if r.get("missile") == "1" or r.get("bomb") == "1" or (r.get("autodirecteur") or "").strip():
        vitesse = vmax
    else:
        vitesse = min(vi, vmax) if vi and vmax and vmax > 0 else (vi or vmax)
    return {"penetration": num(r.get("penetration")), "explosif": num(r.get("explosif")),
            "cout": num(r.get("cout_par_coup")), "portee": num(r.get("portee_max")), "vitesse": vitesse}


# Écart absolu toléré par stat, en plus de 2 % : l'Excel arrondit l'explosif à 0,01 kg (0,015 → 0,01).
TOLERANCE_ABS = {"penetration": 0.5, "explosif": 0.006, "cout": 0.0, "portee": 0.5, "vitesse": 0.5}


def proches(a, b, cle):
    if a is None or b is None:
        return None
    return abs(a - b) <= max(0.02 * max(abs(a), abs(b)), TOLERANCE_ABS.get(cle, 0.0))


def score_stats(excel, cand):
    """(nombre de stats égales, nombre de stats comparables) entre une ligne Excel et un asset du jeu."""
    if not cand.get("stats"):
        return 0, 0
    egal = total = 0
    for k, v in excel["stats"].items():
        r = proches(v, cand["stats"].get(k), k)
        if r is None:
            continue
        total += 1
        egal += 1 if r else 0
    return egal, total


def departager(excel, cands):
    """Candidats dont toutes les stats comparables (au moins 3) sont égales à celles de la ligne Excel."""
    parfaits = []
    for c in cands:
        egal, total = score_stats(excel, c)
        if total >= 3 and egal == total:
            parfaits.append(c)
    return parfaits


def lire_export(chemin):
    infos = collections.OrderedDict()
    with open(chemin, encoding="utf-8", newline="") as f:
        for r in csv.DictReader(f, delimiter=";"):
            nom = (r.get("info_name") or "").strip()
            if not nom:
                continue
            i = infos.setdefault(nom, {"info_name": nom, "weapon_name": (r.get("weapon_name") or "").strip(),
                                       "mounts": [], "cargo": r.get("cargo") == "1", "troops": r.get("troops") == "1",
                                       "stats": stats_export(r)})
            if r.get("mount_jsonkey"):
                i["mounts"].append(r["mount_jsonkey"].strip())
    return infos


def lire_excel(chemin):
    from xlsx_vers_json import lignes_completes  # même liste d'armes que le convertisseur (sources comprises)
    donnees, _, _ = lignes_completes(chemin)
    lignes = []
    for r in donnees:
        if r[0] is None or str(r[0]).strip() == "":
            continue
        nom, source = str(r[0]).strip(), str(r[1]).strip()
        lignes.append({"cle": f"{nom}|{source}", "nom": nom, "source": source, "contenu": tuple(r[2:]), "stats": {
            "penetration": num(r[4]), "explosif": num(r[5]), "cout": num(r[6]), "portee": num(r[9]), "vitesse": num(r[10])}})
    return lignes


def lire_manuel(chemin):
    if not os.path.exists(chemin):
        return []
    with open(chemin, encoding="utf-8", newline="") as f:
        return [r for r in csv.DictReader(f, delimiter=";") if (r.get("statut") or "").strip() == "manuel"]


def filtrer(cands, prefixes=None, sans_mod=False):
    if sans_mod:
        return [c for c in cands if not c["info_name"].startswith(PREFIXES_MODS)]
    return [c for c in cands if c["info_name"].startswith(prefixes)]


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("export")
    ap.add_argument("--xlsx", default=os.path.join(RACINE, "data", "Armes_regroupees.xlsx"))
    args = ap.parse_args()

    infos = lire_export(args.export)
    excel = lire_excel(args.xlsx)
    manuel = lire_manuel(SORTIE)
    deja = {r["excel_cle"] for r in manuel}
    pris = {r["info_name"] for r in manuel if r.get("info_name")}

    par_nom = collections.defaultdict(list)
    for i in infos.values():
        par_nom[i["weapon_name"]].append(i)
    nb_par_nom_excel = collections.Counter(nom_de_base(l["nom"]) for l in excel)

    sortie = [dict(r) for r in manuel]
    for l in excel:
        if l["cle"] in deja:
            continue
        base = nom_de_base(l["nom"])
        cands = [c for c in par_nom.get(base, []) if c["info_name"] not in pris]
        statut, choix, note = "ambigu", [], ""

        if not cands:
            statut, note = "introuvable", "aucune arme de ce nom dans l'export"
        elif base != l["nom"] or l["nom"].startswith("???"):
            note = "variante : à départager par les stats du jeu (export enrichi)"
        elif nb_par_nom_excel[base] == 1:
            # Une seule ligne Excel pour ce nom : toutes les versions du jeu (avion, tourelle, mods KAR/BOTE…)
            # sont la même arme et partagent la fiche.
            statut, choix = "auto", cands
            if len(cands) > 1:
                note = "même arme sur plusieurs plateformes"
        else:
            if l["source"] == "Jeu de base":
                f = [c for c in cands if not c["info_name"].startswith("Aryx_")]
                if f:
                    statut, choix, note = "auto", f, "levée par la source (jeu de base)"
            elif l["source"] in SOURCES_SURES:
                f = filtrer(cands, SOURCES_SURES[l["source"]])
                if len(f) >= 1:
                    statut, choix, note = "auto", f, f"levée par la source ({l['source']})"
            elif l["source"] in AVIONS_ARYX:
                f = filtrer(cands, AVIONS_ARYX[l["source"]])
                if f:
                    statut, choix = "a_valider", f
                    note = "nom de code supposé : " + " / ".join(AVIONS_ARYX[l["source"]])
        if statut == "ambigu" and cands:
            f = departager(l, cands)
            if f:
                statut, choix = "auto", f
                note = (note.split(" ;")[0] + " ; " if note else "") + f"départagée par les stats ({len(f)} asset(s))"
        if statut == "ambigu" and not note:
            note = "candidats : " + ", ".join(c["info_name"] for c in cands)
        elif statut == "ambigu":
            note += " ; candidats : " + ", ".join(c["info_name"] for c in cands)

        if choix:
            for c in choix:
                pris.add(c["info_name"])
                sortie.append({"excel_cle": l["cle"], "info_name": c["info_name"], "mount_jsonkeys": ",".join(c["mounts"]),
                               "statut": statut, "commentaire": note})
        else:
            sortie.append({"excel_cle": l["cle"], "info_name": "", "mount_jsonkeys": "", "statut": statut, "commentaire": note})

    # Asset absent de la correspondance mais portant le nom d'une ligne Excel : si ses stats sont identiques
    # à une seule ligne de ce nom, c'est la même arme (ex. version KAR d'un canon du jeu de base).
    par_base = collections.defaultdict(list)
    for l in excel:
        par_base[nom_de_base(l["nom"])].append(l)
    for i in infos.values():
        if i["info_name"] in pris or not i["weapon_name"] or i["weapon_name"] not in par_base:
            continue
        egales = [l for l in par_base[i["weapon_name"]] if i in departager(l, [i])]
        # Plusieurs lignes Excel identiques (stats ET notes, seule la source change) : n'importe laquelle convient.
        if len(egales) > 1 and len({l["contenu"] for l in egales}) == 1:
            egales = egales[:1]
        if len(egales) == 1:
            pris.add(i["info_name"])
            sortie.append({"excel_cle": egales[0]["cle"], "info_name": i["info_name"], "mount_jsonkeys": ",".join(i["mounts"]),
                           "statut": "auto", "commentaire": "même nom et mêmes stats (alias)"})

    for i in infos.values():
        if i["info_name"] in pris or not i["weapon_name"]:
            continue
        nature = "cargaison" if i["cargo"] else "troupes" if i["troops"] else ""
        sortie.append({"excel_cle": "", "info_name": i["info_name"], "mount_jsonkeys": ",".join(i["mounts"]),
                       "statut": "absent_excel" if i["mounts"] else "hors_hangar",
                       "commentaire": " ".join(x for x in (i["weapon_name"], f"({nature})" if nature else "") if x)})

    with open(SORTIE, "w", encoding="utf-8", newline="") as f:
        w = csv.DictWriter(f, fieldnames=COLONNES, delimiter=";", lineterminator="\n")
        w.writeheader()
        w.writerows(sortie)
    compte = collections.Counter(r["statut"] for r in sortie)
    print(f"{os.path.relpath(SORTIE, RACINE)} : " + ", ".join(f"{k} {v}" for k, v in sorted(compte.items())))


if __name__ == "__main__":
    main()
