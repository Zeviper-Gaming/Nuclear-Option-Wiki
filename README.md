# Nuclear Option Wiki

Mod BepInEx 5 pour **Nuclear Option** : une fiche « wiki » d'arme, de style MFD, affichée à gauche de l'écran de **loadout / hangar**.

Pour chaque arme, la fiche présente :
- vitesse et portée en barres à 10 niveaux, avec la note et la valeur réelle ;
- stats globales ;
- capacités air-sol (CAS) et air-air (AA) en voyants notés sur 5 ;
- interception et défenses (agilité, signatures radar et IR, résistance laser, difficulté à toucher).

La mise en page est identique pour toutes les armes. Une valeur absente s'affiche en gris avec « N/A ».

## Installation

Le mod tient en **un seul fichier**, `NuclearOptionWiki.dll`, qui contient déjà les données des armes. Il faut BepInEx 5, déjà installé si tu utilises NOMM.

- **Avec NOMM** : utilise l'ajout de mod depuis un fichier et choisis `NuclearOptionWiki.dll`. Si NOMM demande une archive, donne-lui `NuclearOptionWiki-<version>.zip`.
- **À la main** : extrais `NuclearOptionWiki-<version>.zip` dans le dossier du jeu, ou copie la DLL dans `BepInEx/plugins/`.

Ne garde qu'**un seul exemplaire** de la DLL dans `BepInEx/plugins/`. Supprime une ancienne installation manuelle avant d'importer avec NOMM.

Lance ensuite le jeu. Ces lignes doivent apparaître dans `BepInEx/LogOutput.log` :
```
[Wiki] Données intégrées à la DLL utilisées.
[Wiki] chargé v… : 133 armes, 4 patch(s) appliqué(s).
```

## Utilisation

- Dans l'écran de loadout du hangar, appuie sur **F6** pour afficher ou masquer la fiche.
- Quand la fiche est affichée, elle montre la **dernière arme survolée ou sélectionnée** :
  - survol d'une option dans la liste déroulante ouverte ;
  - survol d'un emplacement fermé, qui affiche l'arme équipée ;
  - sélection d'une arme.
- La fiche ne bloque aucun clic.

### Changer la touche

Ouvre le menu **F1** (Configuration Manager), puis **Nuclear Option Wiki → Général → Touche bascule** :
- clique sur le bouton, puis appuie sur la touche voulue. Les combinaisons avec Ctrl, Shift ou Alt sont acceptées ;
- la nouvelle touche marche tout de suite, sans redémarrer ;
- si elle est déjà utilisée par un autre mod de ton modpack (F1, F7, F8, F9, Origine, Fin, Pavé 5, Shift+Q), un avertissement s'affiche 4 s en bas de la fiche ;
- **Échap** est refusée, parce qu'elle ferme le hangar ;
- **Reset** remet F6.

Tu peux aussi éditer `BepInEx/config/zeviper.nuclearoption.wiki.cfg`, par exemple `Touche bascule = W + LeftControl`.

### Réglages (menu F1)

| Section | Réglage | Défaut | Rôle |
|---|---|---|---|
| Général | Touche bascule | F6 | Afficher / masquer la fiche |
| Général | Affichage automatique au hangar | non | Fiche visible sans appuyer sur la touche |
| Affichage | Thème | MfdPhosphore | MfdPhosphore (voyants verts), Annonciateur (tuiles allumées), HangarNO (bleu du hangar) |
| Affichage | Note de portée | Logarithmique | Logarithmique (100 m = 1, 1 000 km = 10) ou Rang (comme la vitesse) |
| Affichage | Échelle | 1,0 | Taille de la fiche (0,75 à 1,5) |
| Affichage | Opacité du fond | 0,9 | 0,5 à 1 |
| Diagnostic | Exporter la liste des armes | non | Écrit `BepInEx/config/NuclearOptionWiki_armes.csv` à l'ouverture du hangar |
| Diagnostic | Fiche de démonstration | non | Affiche l'AGM-48 dès l'ouverture du hangar |
| Diagnostic | Journal détaillé | non | Plus de détails dans le journal |

### Lire la fiche

- **Couleurs des notes** : 0 rouge, 1 rouge-orange, 2 orange, 3 jaune, 4 vert-jaune, 5 vert, N/A gris. Les barres /10 utilisent la même échelle (note ÷ 2).
- **« inv. »** (signature radar, signature IR) : une note haute veut dire une grande signature, donc une note mauvaise pour l'arme. Les couleurs sont inversées : 0 = invisible = vert.
- **Valeurs réelles** sous les voyants :
  - signature radar : lue en jeu, sinon tirée de l'Excel ;
  - IR (premier moteur) et agilité (°/s et G) : lues en jeu sur les missiles ;
  - « — » si la valeur est inconnue.
- **Arme non référencée** : arme du jeu absente de `weapons.json`. La fiche est identique, tout est en N/A, et le nom de l'arme est noté dans le journal.

## Mettre à jour les notes

Les notes viennent de `data/Armes_regroupees.xlsx`. Après une modification de l'Excel :

```bash
python3 tools/xlsx_vers_json.py      # régénère data/weapons.json (nécessite openpyxl)
```

Copie ensuite `data/weapons.json` **dans le même dossier que la DLL**. Ce fichier est prioritaire sur les données intégrées, donc il n'y a rien à recompiler. Le journal indique alors `Données lues depuis …`.

`data/correspondance.csv` relie chaque ligne de l'Excel (« Arme|Source ») au nom interne de l'arme dans le jeu. Il est établi à partir de l'export de diagnostic.

## Développement

```
src/Core/      C# sans Unity : base d'armes, notes, formats, modèle de fiche (testé)
src/Plugin/    mod BepInEx : configuration, patchs Harmony, valeurs lues en jeu, interface uGUI
tests/         dotnet run --project tests/Core.Tests
tools/         xlsx_vers_json.py, package.sh
docs/          plan, état du projet, maquette (docs/maquettes/fiche-arme-mfd.html)
```

**Compiler** (SDK .NET 8) :
1. Dépose dans `lib/` les DLL du jeu :
   - `Assembly-CSharp`, `Newtonsoft.Json`, `UnityEngine`, `UnityEngine.CoreModule`, `UnityEngine.UI`, `UnityEngine.UIModule`, `UnityEngine.TextRenderingModule`, `UnityEngine.InputLegacyModule`, `UnityEngine.IMGUIModule`, `Unity.TextMeshPro`, tirées de `NuclearOption_Data/Managed/` ;
   - `BepInEx.dll` et `0Harmony.dll` de BepInEx 5.4.23.2.

   Ces DLL ne sont **jamais commitées**.
2. Lance `tools/package.sh`, qui produit `dist/NuclearOptionWiki-<version>.zip`.
