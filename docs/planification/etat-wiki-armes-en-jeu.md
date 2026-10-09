# État du projet — Wiki d'armes en jeu (Nuclear Option)

Slug : wiki-armes-en-jeu · Mis à jour : 2026-10-09 · Statut : en cours

## Reprise
- Phase en cours : 2 Plan
- Modèle conseillé : Opus 5.5, effort high
- Prochaine action exacte : choisir le format de travail du plan (Markdown ou Claude Docs), trancher les points ouverts ci-dessous, analyser la DLL du jeu (écran de loadout, `WeaponInfo`), produire les maquettes HTML de style, puis rédiger `plan-wiki-armes-en-jeu.md`.
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

### À trancher en phase 2
1. Formules : note de portée /10 et note globale (Claude propose, Florian valide).
2. Colonnes vides (laser, F&F, agilité) : masquer ou afficher « N/A ».
3. Armes absentes de l'Excel : fiche « N/A » complète ou pas de fiche.
4. DLL `UnityEngine.UI` et `Unity.TextMeshPro` (dossier `Managed` du jeu) à fournir.
5. Version du jeu visée.

## Pistes évoquées (non retenues comme décisions)
- Couche d'intégration au jeu isolée (patchs Harmony) + mode « désactivé » sûr en cas d'échec d'accroche.
- Mode « dump » qui liste dans un fichier les identifiants d'armes présents en jeu, pour fiabiliser la correspondance.

## Plan
À rédiger : `docs/planification/plan-wiki-armes-en-jeu.md`.

## Avancement de l'exécution
(phase 3 non commencée)

## Fichiers produits
| Fichier | Rôle | Statut |
|---|---|---|
| `docs/planification/etat-wiki-armes-en-jeu.md` | État du projet | à jour |

## Écarts au plan et remontées
- (aucun)

## Écarts de modèle assumés
- (aucun)

## Questions ouvertes
- Voir « À trancher en phase 2 ».
