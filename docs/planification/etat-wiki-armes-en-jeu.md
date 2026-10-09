# État du projet — Wiki d'armes en jeu (Nuclear Option)

Slug : wiki-armes-en-jeu · Mis à jour : 2026-10-09 · Statut : en cours

## Reprise
- Phase en cours : 2 Plan (version 1 du plan rédigée, en attente de validation par Florian)
- Modèle conseillé : Opus 5.5, effort high
- Prochaine action exacte : recueillir la validation ou les corrections de Florian sur `plan-wiki-armes-en-jeu.md` v1 ; intégrer les retours ; passer le statut du plan à « validé » ; puis phase 3 sur Opus 5.5, effort medium, en commençant par l'étape E0 (Florian fournit les DLL du dossier `Managed`).
- Pour reprendre : nouvelle conversation sur le bon modèle → `/planification` → joindre ce fichier (+ le plan s'il existe).

## Récapitulatif de cadrage (validé le 2026-10-09)

| Point | Réponse |
|---|---|
| Objectif | Fiche « wiki » en jeu présentant stats et notes d'une arme, pour choisir son équipement en connaissance de cause. |
| Livrable | Mod **BepInEx 5** (C#), distribué via NOMM. Données dans un **JSON à côté de la DLL**, généré depuis l'Excel de Florian par un script. |
| Où | Écran **loadout / hangar**, **panneau fixe à gauche** superposé à la scène 3D, adaptable à la résolution. |
| Déclenchement | **Touche bascule** on/off ; en mode actif, la fiche s'affiche à la sélection **et** au survol d'une arme. Touche réglable via Configuration Manager. |
| Mise en page | **Strictement identique pour chaque arme**. Trois zones (ébauche de Florian) : *Stats globales et notes* (haut) · *Capacité AA et CAS* (milieu) · *Interception / défenses* (bas). |
| Indicateurs | Voyants **style MFD** (barre lumineuse au-dessus, libellé police mono, comme la capture AIRCRAFT/MISSILES/… « FULL »). Note en gros, schéma de couleur cohérent avec la note. Vitesse et portée : barre horizontale **10 niveaux** + note + valeur réelle. Domaine sans note → voyant affiché avec **« N/A »**. |
| Langue | Français. |
| Données | Excel `Armes_regroupees.xlsx` : 133 armes (80 jeu de base, 53 mods Aryx / Ternion). Correspondance Excel ↔ identifiants internes du jeu faite par Claude ; cas ambigus soumis à Florian. |
| Environnement | Claude Code (cloud). SDK .NET 8 installable par apt ; build test (Unity 2021.3.33 NuGet + `Assembly-CSharp.dll`) **réussi**. `BepInEx.dll` récupérable depuis la release GitHub 5.4.23.2. Tests en jeu faits par Florian. |
| Critères de réussite | 1) fiche identique pour les 133 armes, sans erreur dans le journal BepInEx ; 2) touche réglable (Configuration Manager) ; 3) aucun conflit avec le modpack (Loadout Presets, EditorPlus, NOXMFD…) ; 4) pas d'impact sur les FPS. |
| Hors périmètre | Édition des notes en jeu ; fiches avions / navires / véhicules (une fiche véhicules pourra venir plus tard → architecture extensible) ; synchro multijoueur ; rendu 3D de l'arme. |
| Préférences | Propositions d'architecture et de style au fil du projet, sous forme de **maquettes HTML interactives**. |
| Risques | Accroche à l'écran de loadout sans casser les autres mods ; correspondance des noms d'armes ; version du jeu (Excel basé sur un wiki possiblement antérieur à 0.34). |

### Constats sur les données (Excel, feuille « Armes »)
- Notes air-sol (/5, 8 cibles) : 83/133 armes · AA (/5, 3 tailles d'avion) : 45 à 52 · Interception/défenses : 29.
- Vitesse indicielle /10 existante pour 99 armes (rang centile, feuille « Vitesse (calcul) »).
- **Pas** de note de portée /10, **pas** de note globale.
- Colonnes vides pour toutes les armes : « Besoin laser », « Fire and Forget », « Virage / Agilité ».
- Noms ambigus : « 30mm Autocannon » ×5, « 20mm Autocannon » ×3, « Radome » ×2, « ??? (variante 1-4) ».

### Tranché en phase 2 (2026-10-09)
1. Plan en Markdown dans le dépôt.
2. Style : variante **A · MFD phosphore** par défaut ; thèmes A / B / C au choix dans le menu F1.
3. Note de portée /10 : **logarithmique par défaut**, rang disponible dans F1. Florian avait écrit « logarithmique » en commentaire et coché « Rang » : les deux sont gardées, à confirmer.
4. Pas de note globale (proposition de Claude, à confirmer par la validation du plan).
5. Signatures radar et IR : note haute = grande signature = mauvais pour l'arme (couleurs inversées) ; afficher les valeurs réelles quand possible, avec les notes. Note RCS /5 calculée depuis la RCS de l'Excel.
6. Valeurs réelles lues en jeu (RCS, IR, taux de virage / G) sur les missiles.
7. Colonnes vides : voyants présents en N/A.
8. Arme absente de l'Excel : fiche complète en N/A + « arme non référencée » + journal.
9. Version du jeu : celle de la DLL fournie.
10. Survol : liste déroulante ouverte **et** emplacement fermé ; affichage « collant » de la dernière arme.
11. Touche : F6 par défaut, **modifiable** (capture dans F1, combinaisons Ctrl/Shift/Alt, fichier .cfg, Reset), appliquée à chaud, avertissement si la touche est déjà prise, rappel en pied de fiche.
12. DLL `Managed` du jeu à fournir par Florian au début de la phase 3 (jamais commitées).

### Constats techniques (décompilation de la DLL, phase 2)
- Points d'accroche : `AircraftSelectionMenu.Initialize`, `LoadoutSelector.ShowMountInfo(WeaponMount)` (survol de la liste ouverte), `WeaponSelector.DropdownChanged` / `GetValue()` (sélection), `WeaponSelector.Initialize` (emplacement).
- `WeaponMount.jsonKey`, `WeaponInfo.name` / `weaponName` ; `Encyclopedia.i.weaponMounts`.
- Valeurs réelles : `Missile.definition.radarSize`, `IR_intensity` des moteurs, `maxTurnRate` / `gLimit` (privés).
- Outils : SDK .NET 8 (apt), ilspycmd 8.2.0.7535 (`DOTNET_ROLL_FORWARD=Major`), BepInEx 5.4.23.2 depuis la release GitHub.

## Pistes évoquées (non retenues comme décisions)
- Couche d'intégration au jeu isolée (patchs Harmony) + mode « désactivé » sûr en cas d'échec d'accroche.
- Mode « dump » qui liste dans un fichier les identifiants d'armes présents en jeu, pour fiabiliser la correspondance.

## Plan
`docs/planification/plan-wiki-armes-en-jeu.md` — version 1, brouillon en attente de validation.
Maquette de référence : `docs/maquettes/fiche-arme-mfd.html` (en ligne : https://claude.ai/artifact/8JAaLwuogtTfQFgh2XWjGP, version 2).

## Avancement de l'exécution
(phase 3 non commencée)

## Fichiers produits
| Fichier | Rôle | Statut |
|---|---|---|
| `docs/planification/etat-wiki-armes-en-jeu.md` | État du projet | à jour |
| `docs/planification/plan-wiki-armes-en-jeu.md` | Plan / cahier des charges | v1 brouillon |
| `docs/maquettes/fiche-arme-mfd.html` | Maquette interactive (3 thèmes, données réelles) | v2 |

## Écarts au plan et remontées
- (aucun)

## Écarts de modèle assumés
- (aucun)

## Questions ouvertes
- Validation du plan v1 (dont : portée logarithmique par défaut, absence de note globale, touche F6 par défaut, GUID `zeviper.nuclearoption.wiki`).
