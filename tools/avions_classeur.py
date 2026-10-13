#!/usr/bin/env python3
"""Régénère data/Avions_wiki.xlsx depuis data/sources/NO-stats.xlsx et le dernier export du mod (data/dump/).

Conservé d'une exécution à l'autre (A16) :
  - feuille « Avions » : colonnes jaunes Rôle(s), Description (FR), Texte perso (par clé d'appareil) ;
  - feuille « Emports » : colonne jaune Nom (FR) (par clé d'appareil et nom d'emplacement du jeu) ;
  - feuille « Corrections » : intégralement, ligne à ligne (la colonne « État » est réécrite).
Toute autre cellule est régénérée. Pour corriger une stat, utiliser la feuille « Corrections ».

Usage : python tools/avions_classeur.py [--sortie chemin.xlsx]
"""
import argparse
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import avions_commun as ac  # noqa: E402

JAUNE = "FFF6C2"
ORANGE = "FFD8A8"
GRIS = "D9D9D9"

AVERTISSEMENT = ("Seules les colonnes jaunes et la feuille Corrections sont conservées quand le classeur est régénéré. "
                 "Pour corriger une stat, utilise la feuille Corrections (une valeur tapée directement ici serait écrasée).")

# (en-tête, champ du modèle ou fonction, largeur)
COLONNES_AVIONS = [
    ("Clé", "cle", 26), ("Appareil", "nom", 20), ("Code", "code", 9), ("Source", "source", 14), ("Type", "type", 26),
    ("Rôle(s)", "roles", 34), ("Description (FR)", "description_fr", 60), ("Texte perso", "texte_perso", 30), ("Tags", "tags", 30),
    ("Taille", "taille", 7), ("Encombrement (m²)", "encombrement_m2", 11), ("Note vitesse", "note_vitesse", 8),
    ("Note charge", "note_charge", 8), ("Note coût", "note_cout", 8), ("Note RCS", "note_rcs", 8),
    ("Rang requis", "rang", 7), ("Coût (M$)", "cout_m", 9), ("Vitesse max. (km/h)", "vitesse_kmh", 10),
    ("Décrochage (km/h)", "decrochage_kmh", 10), ("Maniabilité (0 à 10)", "maniabilite", 10), ("Facteur G max", "g_max", 8),
    ("Masse à vide (kg)", "masse_vide_kg", 10), ("Masse max. (kg)", "masse_max_kg", 10), ("Charge utile (kg)", "charge_utile_kg", 10),
    ("Carburant (kg)", "carburant_kg", 10), ("Poussée (kN)", "poussee_kn", 9), ("Puissance (kW)", "puissance_kw", 9),
    ("Décollage vertical", "decollage_vertical", 9), ("Distance de décollage (m)", "decollage_m", 10),
    ("Vitesse de décollage (m/s)", "decollage_ms", 10), ("Longueur (m)", "longueur_m", 9), ("Envergure (m)", "envergure_m", 9),
    ("Hauteur (m)", "hauteur_m", 9), ("Portée radar (m)", "radar_m", 10), ("Brouilleur", "brouilleur", 9),
    ("Canons fixes", "canons_fixes", 8), ("Leurres IR", "leurres", 8), ("Paillettes", "paillettes", 8),
    ("Palier de blindage", "blindage", 8), ("Signature radar (m²)", "rcs_m2", 11),
    ("Désignateur laser (cibles)", "laser_cibles", 10), ("Désignateur laser (portée m)", "laser_portee_m", 10),
    ("IR 30 % gaz", "ir_30", 8), ("IR plein gaz", "ir_max", 8), ("IR ralenti", "ir_min", 8),
    ("IR plein gaz sans PC", "ir_max_sec", 9), ("Nb emplacements", "nb_emplacements", 9), ("Nb points", "nb_points", 8),
    ("Nb soutes", "nb_soutes", 8), ("Charge max / point (kg)", "charge_max_point_kg", 10),
    ("Indice IA anti-surface", ("role_ia", "role_surface"), 8), ("Indice IA anti-air", ("role_ia", "role_air"), 8),
    ("Indice IA anti-missile", ("role_ia", "role_missile"), 8), ("Indice IA anti-radar", ("role_ia", "role_radar"), 8),
    ("Description (jeu, EN)", "description_jeu", 60), ("Notes", "notes", 50),
]
EDITABLES_AVIONS = {"Rôle(s)", "Description (FR)", "Texte perso", "Tags"}
# Colonnes de stats comparées à l'existant pour repérer une saisie directe (plan, section 8).
STATS_SURVEILLEES = {lib: champ for lib, champ, _ in COLONNES_AVIONS
                     if isinstance(champ, str) and lib not in EDITABLES_AVIONS and lib not in ("Clé", "Notes")}

COLONNES_EMPORTS = [
    ("Clé", 26), ("Appareil", 18), ("Index", 6), ("Nom (jeu)", 26), ("Nom (FR)", 28), ("Affiché", 9), ("Points", 7),
    ("Soute", 7), ("Exclut (noms)", 30), ("Charge max / point (kg)", 11), ("Classe", 7), ("Nb armes compatibles", 9),
    ("Catégories", 30), ("Armes compatibles", 80),
]
COLONNES_CORRECTIONS = [("Clé appareil", 26), ("Emplacement (jeu)", 28), ("Champ", 28), ("Valeur forcée", 14),
                        ("Commentaire", 40), ("État", 50)]


def valeur(a, champ):
    if isinstance(champ, tuple):
        return a[champ[0]].get(champ[1])
    v = a.get(champ)
    if champ == "ir_max" and a["ir_force"]["ir_max"] is not None:
        v = a["ir_force"]["ir_max"]
    if champ == "ir_30" and a["ir_force"]["ir_30"] is not None:
        v = a["ir_force"]["ir_30"]
    if champ == "notes":
        v = " ".join(x for x in [a.get("notes")] + a["notes_auto"] if x) or None
    if isinstance(v, bool):
        return "oui" if v else "non"
    if isinstance(v, list):
        return " · ".join(v) or None
    return ac.entier_si_rond(v)


def oui_non(b):
    return "oui" if b else "non"


def ecrire(modele, existant, chemin, journal=print):
    import openpyxl
    from openpyxl.comments import Comment
    from openpyxl.styles import Alignment, Font, PatternFill
    from openpyxl.worksheet.datavalidation import DataValidation

    jaune = PatternFill("solid", fgColor=JAUNE)
    orange = PatternFill("solid", fgColor=ORANGE)
    gris = PatternFill("solid", fgColor=GRIS)
    gras = Font(bold=True)
    haut = Alignment(vertical="top", wrap_text=True)

    wb = openpyxl.Workbook()
    wb.remove(wb.active)

    # ---- Lisez-moi
    ws = wb.create_sheet("Lisez-moi")
    lignes = lisez_moi(modele)
    for i, l in enumerate(lignes, 1):
        ws.cell(i, 1, l)
    ws["A1"].font = Font(bold=True, color="C00000")
    ws.column_dimensions["A"].width = 140

    # ---- Avions
    ws = wb.create_sheet("Avions")
    for j, (lib, _, larg) in enumerate(COLONNES_AVIONS, 1):
        c = ws.cell(1, j, lib)
        c.font = gras
        c.alignment = Alignment(wrap_text=True, vertical="top")
        if lib in EDITABLES_AVIONS:
            c.fill = jaune
        ws.column_dimensions[openpyxl.utils.get_column_letter(j)].width = larg
    ws["A1"].comment = Comment(AVERTISSEMENT, "Wiki avions")
    for i, a in enumerate(modele["appareils"], 2):
        ancien = existant["stats"].get(a["cle"], {})
        for j, (lib, champ, _) in enumerate(COLONNES_AVIONS, 1):
            v = valeur(a, champ)
            c = ws.cell(i, j, v)
            if lib in EDITABLES_AVIONS:
                c.fill = jaune
                c.alignment = haut
            if isinstance(champ, str) and (champ in a["corriges"] or (champ in ("ir_30", "ir_max") and a["ir_force"][champ] is not None)):
                c.fill = orange
                source = a["sources_corrigees"].get(champ, a.get(champ))
                c.comment = Comment(f"Valeur forcée par la feuille Corrections (valeur d'origine : {ac.entier_si_rond(source)}).", "Wiki avions")
            if lib in STATS_SURVEILLEES and lib in ancien:
                av = ancien[lib]
                if av not in (None, "") and str(ac.entier_si_rond(ac.nombre(av)) if ac.nombre(av) is not None else av) != str(v):
                    journal(f"Attention : {a['cle']} · « {lib} » valait {av} dans le classeur, remplacé par {v}. "
                            f"Pour garder {av}, ajoute dans Corrections : {a['cle']} | (vide) | {lib_correction(lib)} | {av}")
    ws.freeze_panes = "C2"
    ws.auto_filter.ref = ws.dimensions

    # ---- Emports
    ws = wb.create_sheet("Emports")
    for j, (lib, larg) in enumerate(COLONNES_EMPORTS, 1):
        c = ws.cell(1, j, lib)
        c.font = gras
        if lib == "Nom (FR)":
            c.fill = jaune
        ws.column_dimensions[openpyxl.utils.get_column_letter(j)].width = larg
    i = 2
    for a in modele["appareils"]:
        for s in a["emplacements"]:
            affiche = "non (masqué)" if s["masquer"] else ("oui" if s["affiche"] else "non (sans arme)")
            ligne = [a["cle"], a["nom"], s["index"], s["nom_jeu"], s["nom_fr"], affiche, s["points"], oui_non(s["soute"]),
                     ", ".join(s["exclut"]) or None, ac.entier_si_rond(s["charge_kg"]), s["classe"], s["nb_armes"],
                     ", ".join(s["categories"]) or None, ", ".join(s["armes"]) or None]
            for j, v in enumerate(ligne, 1):
                c = ws.cell(i, j, v)
                if COLONNES_EMPORTS[j - 1][0] == "Nom (FR)":
                    c.fill = jaune
                elif not s["affiche"] or s["masquer"]:
                    c.fill = gris
            for champ in s["force"]:
                col = {"points": 7, "soute": 8, "charge_kg": 10, "categories": 13, "nb_armes": 12, "masquer": 6}[champ]
                ws.cell(i, col).fill = orange
            i += 1
    ws.freeze_panes = "E2"
    ws.auto_filter.ref = ws.dimensions

    # ---- Corrections (conservée ligne à ligne)
    ws = wb.create_sheet("Corrections")
    for j, (lib, larg) in enumerate(COLONNES_CORRECTIONS, 1):
        c = ws.cell(1, j, lib)
        c.font = gras
        c.fill = jaune if lib != "État" else gris
        ws.column_dimensions[openpyxl.utils.get_column_letter(j)].width = larg
    for i, (ligne, etat) in enumerate(zip(existant["corrections"], modele["corrections"]), 2):
        for j, v in enumerate(ligne, 1):
            ws.cell(i, j, v)
        ws.cell(i, 6, etat)
    ws.freeze_panes = "A2"

    # ---- Listes (masquée) pour les listes déroulantes de Corrections
    wl = wb.create_sheet("Listes")
    cles = [a["cle"] for a in modele["appareils"]]
    emplacements = sorted({s["nom_jeu"] for a in modele["appareils"] for s in a["emplacements"]})
    champs = [lib for lib, _, _ in ac.CHAMPS_APPAREIL] + [lib for lib, _, _ in ac.CHAMPS_EMPLACEMENT]
    for col, (titre, valeurs) in enumerate((("Clés", cles), ("Emplacements", emplacements), ("Champs", champs),
                                            ("Rôles", ac.ROLES), ("Catégories", ac.CATEGORIES)), 1):
        wl.cell(1, col, titre).font = gras
        for k, v in enumerate(valeurs, 2):
            wl.cell(k, col, v)
    wl.sheet_state = "hidden"
    ws = wb["Corrections"]
    for col, lettre, n in (("A", "A", len(cles)), ("B", "B", len(emplacements)), ("C", "C", len(champs))):
        dv = DataValidation(type="list", formula1=f"=Listes!${lettre}$2:${lettre}${n + 1}", allow_blank=True)
        dv.error = "Choisis une valeur dans la liste."
        dv.showErrorMessage = True
        ws.add_data_validation(dv)
        dv.add(f"{col}2:{col}500")

    wb.active = wb.sheetnames.index("Avions")
    wb.save(chemin)


def lib_correction(lib_colonne):
    """Libellé de la liste « Champ » le plus proche d'un en-tête de la feuille Avions."""
    champ = STATS_SURVEILLEES.get(lib_colonne)
    for lib, c, _ in ac.CHAMPS_APPAREIL:
        if c == champ or (c == "ir_force.ir_30" and champ == "ir_30") or (c == "ir_force.ir_max" and champ == "ir_max"):
            return lib
    return lib_colonne + " (pas corrigeable)"


def lisez_moi(modele):
    s = modele["seuils"]
    e, ch = s["encombrement_m2"], s["charge_kg"]
    return [
        "⚠ " + AVERTISSEMENT,
        "",
        "Classeur du wiki avions (mod NuclearOptionWiki, fiche « appareil » du hangar). Régénéré par tools/avions_classeur.py ; "
        "aircraft.json est produit par tools/avions_vers_json.py (les deux sont lancés par mettre_a_jour.bat).",
        f"Données : data/sources/NO-stats.xlsx (prioritaire, lue seulement) + export du mod {modele['exports'][0]} / {modele['exports'][1]} "
        "(emports, IR, désignateurs laser, clés, valeurs manquantes). OVNI, navires et véhicules exclus (A6, A22).",
        "",
        "Colonnes à remplir (jaunes) :",
        "  • Avions › Rôle(s) : 3 rôles au plus, séparés par « · », pris dans la liste de la feuille Listes (colonne D). Vide = « N/A » en jeu.",
        "  • Avions › Description (FR) : traduction affichée en bas de la fiche. Vide = description anglaise du jeu.",
        "  • Avions › Texte perso : ton texte, affiché en ambre au-dessus des tags.",
        "  • Avions › Tags : tes mots-clés libres, séparés par « · » (ou des virgules), affichés en pastilles juste au-dessus de la description.",
        "  • Emports › Nom (FR) : nom de l'emplacement sur la fiche. Vide = nom anglais du jeu.",
        "  • Corrections : une ligne par valeur à forcer (Clé appareil · Emplacement (jeu), vide pour l'appareil · Champ · Valeur forcée · "
        "Commentaire). La colonne État dit si la ligne est appliquée ou ignorée (et pourquoi). Une valeur forcée est affichée avec « * » "
        "en jeu et surlignée en orange ici ; les notes, tailles et classes sont recalculées avec elle.",
        "    Catégories : liste séparée par des virgules parmi " + ", ".join(f"{c} ({ac.ACRONYMES[c]})" for c in ac.CATEGORIES) + " (nom ou acronyme). OUI/NON pour Soute, Brouilleur, "
        "Décollage vertical, Masquer l'emplacement.",
        "",
        "Règles de calcul :",
        f"  • Taille S/M/H (A7) : encombrement longueur × envergure, tiers égaux en échelle log entre {e['min']} et {e['max']} m² : "
        f"S ≤ {e['s_max']} m², M ≤ {e['m_max']} m², H au-delà.",
        f"  • Classe de charge d'un emplacement (A9) : masse de l'option d'arme la plus lourde (hors cargo, crochets, réservoirs), "
        f"tiers log entre {ch['min']} et {ch['max']} kg : S ≤ {ch['s_max']} kg, M ≤ {ch['m_max']} kg, H au-delà.",
        "  • Emplacements sans arme (crochets…) : listés ici en gris, non affichés sur la fiche (A23).",
        "  • IR (A11) : par moteur, max des moteurs. IR 30 % gaz = IR ralenti + 0,3 × (IR plein gaz sans postcombustion − IR ralenti) : "
        "estimation linéaire. IR plein gaz = avec postcombustion. Les deux notes /5 sont sur la même échelle linéaire, de 0 à l'IR plein gaz "
        "le plus fort de tous les appareils : note = arrondi(5 × IR / IR max), calculée en jeu.",
        "  • Note RCS /5 (A12) : arrondi(log10(RCS) + 4), bornée à 0–5 (10⁻⁴ m² → 0 ; 10 m² → 5).",
        "  • Notes /10 vitesse, charge utile, coût (A14) : rang parmi les appareils du classeur, 1 + 9 × (nb ≤ valeur − 1) / (n − 1). "
        "Agilité = Maniabilité (0 à 10) telle quelle.",
        "  • Charge utile (v3.6) : masse d'armement maximale sur l'ensemble des emplacements (points × arme la plus lourde, cargo exclu), "
        "meilleure combinaison d'emplacements compatibles. Pour un appareil cargo, force la valeur dans Corrections (« Charge utile (kg) »).",
        "  • Masse à vide manquante (A15) : somme des pièces du prefab seulement si elle concorde à ±15 % pour au moins 80 % des appareils ; "
        "sinon N/A (voir la colonne Notes).",
        "",
        "Mise à jour : remplace data/sources/NO-stats.xlsx si besoin, modifie les colonnes jaunes ou Corrections, enregistre, ferme Excel, "
        "puis double-clique sur mettre_a_jour.bat : le classeur et aircraft.json sont régénérés et copiés à côté de la DLL du mod.",
    ]


def main():
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument("--sortie", default=ac.CLASSEUR, help="Classeur à écrire (défaut : data/Avions_wiki.xlsx).")
    ap.add_argument("--classeur", help="Classeur existant dont on garde les colonnes jaunes et Corrections (défaut : la sortie).")
    ap.add_argument("--tags-jeu", help="NuclearOptionWiki_tags.json (tags saisis en jeu, A24) : recopiés dans la colonne Tags, prioritaires.")
    args = ap.parse_args()
    existant = ac.lire_classeur(args.classeur or args.sortie)
    if args.tags_jeu and os.path.exists(args.tags_jeu):
        import json
        with open(args.tags_jeu, encoding="utf-8-sig") as f:
            saisis = json.load(f) or {}
        for cle, tags in saisis.items():
            if tags:
                existant["avions"].setdefault(cle, {})["tags"] = " · ".join(t.strip() for t in tags if t and t.strip())
        print(f"Tags saisis en jeu recopiés : {len(saisis)} appareil(s) ({args.tags_jeu}).")
    modele = ac.construire(existant)
    ecrire(modele, existant, args.sortie)
    a = modele["appareils"]
    vides = [x["code"] for x in a if not x["roles"]]
    print(f"Classeur écrit : {args.sortie} ({len(a)} appareils, {sum(len(x['emplacements']) for x in a)} emplacements, "
          f"{len(existant['corrections'])} correction(s)).")
    if vides:
        print(f"Rôles vides : {', '.join(vides)}")


if __name__ == "__main__":
    main()
