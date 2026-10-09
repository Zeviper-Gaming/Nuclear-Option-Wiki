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
  ambigu      plusieurs candidats, aucune règle ne tranche (souvent des variantes : il faut les stats du jeu)
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


def lire_export(chemin):
    infos = collections.OrderedDict()
    with open(chemin, encoding="utf-8", newline="") as f:
        for r in csv.DictReader(f, delimiter=";"):
            nom = (r.get("info_name") or "").strip()
            if not nom:
                continue
            i = infos.setdefault(nom, {"info_name": nom, "weapon_name": (r.get("weapon_name") or "").strip(),
                                       "mounts": [], "cargo": r.get("cargo") == "1", "troops": r.get("troops") == "1"})
            if r.get("mount_jsonkey"):
                i["mounts"].append(r["mount_jsonkey"].strip())
    return infos


def lire_excel(chemin):
    ws = openpyxl.load_workbook(chemin, data_only=True)["Armes"]
    lignes = []
    for r in list(ws.iter_rows(values_only=True))[2:]:
        if r[0] is None or str(r[0]).strip() == "":
            continue
        nom, source = str(r[0]).strip(), str(r[1]).strip()
        lignes.append({"cle": f"{nom}|{source}", "nom": nom, "source": source})
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
