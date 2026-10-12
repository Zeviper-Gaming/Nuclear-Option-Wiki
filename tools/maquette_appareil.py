#!/usr/bin/env python3
"""Génère docs/maquettes/fiche-appareil-mfd.html (étape V5) avec les vraies données du classeur et de l'export.

Le gabarit est docs/maquettes/fiche-appareil-mfd.gabarit.html (placeholders __DATA__ et __BG__) ;
le fond du hangar est repris de la maquette de la fiche arme.
Usage : python tools/maquette_appareil.py
"""
import json
import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import avions_commun as ac  # noqa: E402

MAQ = os.path.join(ac.RACINE, "docs", "maquettes")


def note_ir(v, ir_max):
    """A11 amendé v3.3 : même échelle linéaire pour les deux voyants, de 0 à l'IR plein gaz le plus fort."""
    if v is None or not ir_max:
        return None
    return int(max(0, min(5, ac.arrondi(5 * v / ir_max, 0))))


def main():
    m = ac.construire(journal=lambda *_: None)
    apps = m["appareils"]
    ir30 = [a["ir_force"]["ir_30"] if a["ir_force"]["ir_30"] is not None else a["ir_30"] for a in apps]
    irmax = [a["ir_force"]["ir_max"] if a["ir_force"]["ir_max"] is not None else a["ir_max"] for a in apps]
    out = []
    for a, i30, imx in zip(apps, ir30, irmax):
        d = {k: ac.entier_si_rond(a.get(k)) for k in (
            "cle", "nom", "code", "source", "type", "taille", "rang", "cout_m", "vitesse_kmh", "decrochage_kmh", "maniabilite",
            "g_max", "masse_vide_kg", "masse_max_kg", "charge_utile_kg", "carburant_kg", "poussee_kn", "puissance_kw",
            "decollage_vertical", "decollage_m", "decollage_ms", "radar_m", "brouilleur", "canons_fixes", "leurres", "paillettes",
            "blindage", "rcs_m2", "laser_cibles", "laser_portee_m", "note_vitesse", "note_charge", "note_cout", "note_rcs",
            "description_fr", "description_jeu", "texte_perso", "nb_emplacements", "nb_points", "nb_soutes", "charge_max_point_kg")}
        d["type_court"] = ac.type_court(a["type"])
        d["roles"] = [r.strip() for r in (a["roles"] or "").split("·") if r.strip()]
        d["ir30"], d["irmax"] = i30, imx
        top = max(x for x in irmax if x is not None)
        d["note_ir30"], d["note_irmax"] = note_ir(i30, top), note_ir(imx, top)
        d["tags"] = [t.strip() for t in re.split(r"[·,;]", a["tags"] or "") if t.strip()]
        d["corriges"] = a["corriges"]
        d["emplacements"] = [{"nom": s["nom_fr"] or s["nom_jeu"], "points": s["points"], "soute": s["soute"], "classe": s["classe"],
                              "charge": ac.entier_si_rond(s["charge_kg"]), "cats": s["categories"], "nb": s["nb_armes"],
                              "exclut": bool(s["exclut"])}
                             for s in a["emplacements"] if s["affiche"] and not s["masquer"]]
        out.append(d)
    gabarit = open(os.path.join(MAQ, "fiche-appareil-mfd.gabarit.html"), encoding="utf-8").read()
    arme = open(os.path.join(MAQ, "fiche-arme-mfd.html"), encoding="utf-8").read()
    bg = re.search(r"data:image/jpeg;base64,[A-Za-z0-9+/=]+", arme).group()
    html = gabarit.replace("__DATA__", json.dumps(out, ensure_ascii=False)).replace("__BG__", bg)
    html = html.replace("__SEUILS__", json.dumps(m["seuils"], ensure_ascii=False))
    html = html.replace("__ACRO__", json.dumps(ac.ACRONYMES, ensure_ascii=False))
    dest = os.path.join(MAQ, "fiche-appareil-mfd.html")
    open(dest, "w", encoding="utf-8", newline="\n").write(html)
    print(f"{dest} ({len(out)} appareils, {len(html) // 1024} Ko)")


if __name__ == "__main__":
    main()
