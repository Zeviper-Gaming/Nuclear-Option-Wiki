# État du projet — Wiki avions et hélicos (Nuclear Option)

Slug : wiki-avions-no · Mis à jour : 2026-10-12 · Statut : en cours

## Reprise
- Phase en cours : 2 Plan — décisions de conception D-A à D-E prises ; en attente des DLL du jeu pour la partie technique
- Modèle conseillé : Opus 5.5, effort high
- Prochaine action exacte : dès réception de `Assembly-CSharp.dll` (et d'une capture du hangar), décompiler (ilspycmd) pour trouver : l'événement de changement / survol d'appareil dans `AircraftSelectionMenu`, la structure `AircraftDefinition` / `AircraftParameters`, `HardpointSet` / `Hardpoint` (taille, soute interne, armes compatibles), l'IR des moteurs d'avion, la localisation éventuelle des textes. Puis rédiger `docs/planification/plan-wiki-avions-no.md` (format Markdown, dans le dépôt).
- Pour reprendre : nouvelle conversation sur le bon modèle → `/planification` → joindre ce fichier (+ le plan).
- Prérequis session cloud neuve (comme le projet armes) : `apt-get install -y dotnet-sdk-8.0`, redéposer les DLL du jeu dans `lib/` (jamais commitées), BepInEx 5.4.23.2 pour `BepInEx.dll` et `0Harmony.dll`.

## Récapitulatif de cadrage (validé le 2026-10-12)

| Point | Réponse |
|---|---|
| Objectif | Une fiche « wiki » en jeu pour **chaque avion et hélicoptère** (vanilla + moddés), afin de comparer les appareils et de choisir sa monture en connaissance de cause. Même esprit que le wiki d'armes. |
| Livrable | **Extension du mod existant `NuclearOptionWiki`** (BepInEx 5, C#, DLL unique, données intégrées) : une **fiche appareil** de style MFD, plus un Excel enrichi et les scripts de mise à jour. Pas de second mod. |
| Où / déclenchement | Au **hangar, à la sélection d'appareil** : la fiche s'affiche à gauche quand on choisit ou survole un appareil. **Même touche bascule (F6)**, réglable en F1. Au loadout, la fiche arme reste inchangée. |
| Mise en page | **Fiche unique, strictement identique** pour tous les appareils (avions, hélicos, VTOL, convertibles). Une valeur sans objet (ex. décrochage d'un hélico) = voyant gris « N/A ». |
| Contenu demandé | 1) vitesse · 2) coût · 3) agilité · 4) rôle(s) · 5) taille S/M/H · 6) nom et description en français · 7) capacités d'emport (nombre, tailles, nombre de baies) · 8) rayonnement IR · 9) RCS et indice de furtivité · 10) texte personnel de Florian · 11) autres infos pertinentes. |
| Taille S/M/H | **Relative** : petit / moyen / grand par rapport aux plus petits et aux plus gros appareils (donc calculée sur l'ensemble des appareils, moddés compris). Critère et seuils à fixer en phase 2 (masse max., dimensions, ou combinaison). |
| Furtivité | **Notes séparées seulement, pas d'indice combiné** : RCS (valeur en m² + note /5, échelle log comme les armes) et IR (valeur + note /5), couleurs cohérentes avec les armes (haute = grande signature = mauvais). |
| Emports | **Résumé + détail par emplacement** : ligne de synthèse (nombre de points d'emport, soutes internes), puis une rangée de pastilles par emplacement (taille max S/M/H, interne ou externe, nombre d'armes). La **compatibilité d'armes** (par emplacement) est demandée aussi. Forme exacte à concevoir en phase 2 (la fiche doit tenir dans la même zone que celle des armes). |
| Textes | **Nom officiel conservé** (ex. CI-22 Cricket). **Description traduite en français** par Claude, dans l'Excel, à relire par Florian. Nouvelle colonne **« Texte perso »**, vide, remplie par Florian, affichée en fin de fiche. |
| Autres infos retenues | **Perfs de vol** : rang requis, masses (vide / max), carburant, poussée ou puissance, décrochage, facteur G, décollage / atterrissage, décollage vertical. **Défenses et capteurs** : leurres IR, paillettes, brouilleur radar, portée du radar embarqué, palier de blindage. **Armement fixe** : canons fixes (nombre, calibre), cargaison / élingue / troupes. **Compatibilité d'armes.** |
| Données | Excel `NO-stats.xlsx` (feuille « Avions » : 25 appareils, 14 vanilla + 11 moddés : MiG-15, OA-27, RAH-72, F-99, F-16M, KR-33, F-117A, FS-3, MC-260, F-22E, FS-41) **plus un export en jeu** produit par le mod (comme pour les armes) : hardpoints (tailles, interne / externe, armes compatibles), IR des moteurs, textes localisés, valeurs manquantes. Florian lance le jeu avec ses mods et renvoie le fichier. |
| Environnement | Claude Code (cloud). Pas d'accès au jeu : Florian teste en jeu et renvoie captures, `LogOutput.log` et exports. DLL du jeu fournies par Florian en début de phase 3, jamais commitées. |
| Critères de réussite (proposés) | 1) fiche de structure identique pour les 25 appareils (tests Core) et sans erreur dans le journal BepInEx ; 2) valeurs de la fiche = Excel + export en jeu, sans trou non justifié (N/A seulement si sans objet) ; 3) pas de conflit avec le mod d'armes ni avec le modpack ; 4) pas d'impact FPS ; 5) Florian peut ajouter son texte perso dans l'Excel et régénérer sans recompiler ; 6) un appareil absent de l'Excel (nouveau mod) donne une fiche en N/A avec les valeurs lues en jeu, comme pour les armes. |
| Hors périmètre (proposé) | Édition des notes en jeu · fiches navires / véhicules · synchro multijoueur · rendu 3D · comparateur côte à côte de deux appareils · icônes de catégories (reste le point F6 du wiki d'armes). |
| Préférences | Maquette HTML interactive pour valider la mise en page avant le code (comme pour les armes) ; libellés en français ; thèmes A / B / C réutilisés. |
| Risques et inconnues | Accroche à l'écran de sélection d'appareil sans casser les autres mods (point d'ancrage à trouver par décompilation) · le jeu stocke la taille et le type des emplacements de façon peut-être différente selon l'appareil (à lire dans l'export) · fiche très dense (emports + compatibilité) à faire tenir dans la mise en page · appareils moddés dont les fiches sont incomplètes (masse à vide à 0, vitesse déclarée par le mod) · les descriptions du jeu sont en anglais et la localisation française n'existe peut-être pas dans les fichiers. |

### À trancher en phase 2
- Règle exacte de la taille S/M/H et seuils ; sur quelle liste d'appareils elle est calculée (vanilla seulement ou tout).
- Notes /10 retenues : vitesse (rang parmi les appareils ?), coût, agilité (colonne Maniabilité 0 à 10 de l'Excel prioritaire ?), et comment l'agilité est notée pour les hélicos.
- Forme de la zone d'emport et de la compatibilité d'armes dans une fiche de taille fixe (pastilles, défilement, onglets).
- Découpage des rôles : les quatre indices de l'Excel (anti-surface, anti-air, anti-missile, anti-radar, valeurs 0 à 1) → voyants /5 ou libellés.
- Cas particuliers : l'OVNI événementiel « ??? » (à garder ou exclure), les doublons de variantes moddées.
- Où tombe la fiche à l'écran quand les deux fiches (appareil, arme) existent dans le même hangar.

### Tranché en phase 2 (2026-10-12)
- **D-A Format du plan** : Markdown dans le dépôt (`docs/planification/plan-wiki-avions-no.md`), comme pour le wiki d'armes.
- **D-B Cohabitation** : **panneau unique** à gauche, qui montre le **dernier élément** survolé ou sélectionné : l'appareil (changement ou survol d'appareil) ou l'arme (survol ou sélection d'arme). Même touche F6.
- **D-C Taille S/M/H** : **encombrement = longueur × envergure**, échelle log coupée en trois parts égales entre le plus petit et le plus gros appareil (hors OVNI). Avec les données actuelles : bornes 74 m² (RAH-72) → 1 391 m² (MC-260) ; S ≤ 199 m² (Cricket, OA-27, MiG-15, RAH-72, F-99, VT-7, Chicane, T/A-30, Ibis, Revoker, Vortex, F-16M) ; M ≤ 528 m² (KR-33, Brawler, F-117A, Medusa, Ifrit, FS-41, F-22E, FS-3) ; H au-delà (Tarantula, AB-4, Darkreach, MC-260).
- **D-D Rôles** : **libellés de rôle définis par Claude d'après la description de l'appareil**, dans une colonne de l'Excel ; laissés **vides** quand la description ne permet pas de trancher (Florian complète). Pas de note dérivée des indices d'IA.
- **D-E OVNI « ??? »** : **exclu totalement** (pas de fiche ; s'il apparaît, fiche « appareil non référencé »), et exclu des classements.

## Pistes évoquées (non retenues comme décisions)
- Réutiliser le Core du wiki d'armes (barres 10 niveaux, notes au rang, N/A, thèmes) et l'outillage `tools/` (conversion Excel → JSON, mise à jour autonome par Florian).
- Étendre le mode « export » du mod pour qu'il écrive aussi un CSV des appareils (AircraftDefinition, AircraftParameters, hardpoints, moteurs, textes).
- Mini-clé de correspondance Excel ↔ identifiants internes des appareils (25 lignes seulement), déduite de l'export.

## Plan
`docs/planification/plan-wiki-avions-no.md` — pas encore écrit (phase 2).

## Avancement de l'exécution
- [x] Phase 1 — cadrage validé (2026-10-12)
- [ ] Phase 2 — plan validé
- [ ] Phase 3 — exécution
- [ ] Phase 4 — peaufinage

## Fichiers produits
| Fichier | Rôle | Statut |
|---|---|---|
| `docs/planification/etat-wiki-avions-no.md` | État du projet | à jour |
| `data/sources/NO-stats.xlsx` | Excel source (déjà au dépôt, identique au fichier joint) | existant |

## Écarts au plan et remontées
- (aucun)

## Écarts de modèle assumés
- (aucun) — phase 1 menée sur Sonnet 5.5, modèle conforme.

## Questions ouvertes
- DLL du jeu (`Assembly-CSharp.dll`) et capture d'écran du hangar avec la fiche arme affichée : nécessaires pour la partie technique du plan.
