# Nuclear Option Wiki

Mod BepInEx 5 pour **Nuclear Option** : une fiche « wiki » d'arme, de style MFD, affichée à gauche de l'écran de **loadout / hangar**.

Pour chaque arme, la fiche présente :
- 5 barres à 10 niveaux, avec la note et la valeur réelle : vitesse, portée, dégâts (explosif), pénétration, explosif par M$ ;
- le profil d'attaque en pastilles (DATALINK, LOFT, ZIGZAG, SUIVI RELIEF…) ;
- coût, portée min, autodirecteur, IR, tire et oublie, laser requis (lus en jeu quand l'Excel ne les donne pas) ;
- capacités air-sol (CAS) et air-air (AA) en voyants notés sur 5 ;
- interception et défenses : agilité (notée en jeu d'après la limite en G des missiles), signatures radar et IR, résistance laser, difficulté à toucher.

La mise en page est identique pour toutes les armes. Une valeur absente s'affiche en gris avec « N/A ».

## Installation

Le mod tient en **un seul fichier**, `NuclearOptionWiki.dll`, qui contient déjà les données des armes. La dernière version se trouve dans [`release/`](release/). Il faut BepInEx 5, déjà installé si tu utilises NOMM.

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
| Diagnostic | Exporter la liste des armes | non | Écrit `BepInEx/config/NuclearOptionWiki_armes_v<version>.csv` à l'ouverture du hangar |
| Diagnostic | Fiche de démonstration | non | Affiche l'AGM-48 dès l'ouverture du hangar |
| Diagnostic | Journal détaillé | non | Plus de détails dans le journal |

### Lire la fiche

- **Couleurs des notes** : 0 rouge, 1 rouge-orange, 2 orange, 3 jaune, 4 vert-jaune, 5 vert, N/A gris. Les barres /10 utilisent la même échelle (note ÷ 2). Vitesse, dégâts, pénétration et explosif/M$ sont notés au rang parmi les armes de l'Excel ; une valeur nulle donne 0 (barre éteinte).
- **Tire et oublie** ne concerne que les missiles : N/A pour les bombes, canons et armes à énergie.
- **« inv. »** (signature radar, signature IR) : une note haute veut dire une grande signature, donc une note mauvaise pour l'arme. Les couleurs sont inversées : 0 = invisible = vert.
- **Valeurs réelles** sous les voyants :
  - signature radar : lue en jeu, sinon tirée de l'Excel ;
  - IR (premier moteur) et agilité (°/s et G) : lues en jeu sur les missiles ;
  - « — » si la valeur est inconnue.
- **Arme non référencée** : arme du jeu absente de `weapons.json`. La fiche est identique, tout est en N/A, et le nom de l'arme est noté dans le journal.

## Mettre à jour les données soi-même (sans recompiler)

Les données viennent de trois classeurs :

| Fichier | Rôle |
|---|---|
| `data/Armes_regroupees.xlsx` | Fiche fusionnée : stats, notes air-sol/air-air, interception. Prioritaire pour les armes qu'il contient. |
| `data/sources/NO-stats.xlsx` | Stats brutes. Une arme qui n'est pas encore dans `Armes_regroupees` y est reprise automatiquement. |
| `data/sources/NO_efficacite_armes.xlsx` | Notes d'efficacité (dont la cible « Hélico ») et vulnérabilité aux défenses. |

**Procédure (Windows)**, après une modification des classeurs :

1. Installe une fois Python 3, puis lance `pip install openpyxl`.
2. Récupère le dépôt (bouton « Code → Download ZIP » sur GitHub, ou `git pull`) et remplace les fichiers dans `data/` et `data/sources/`.
3. *Facultatif, pour les armes nouvelles en jeu* : lance le jeu, active F1 → Diagnostic → **Exporter la liste des armes**, ouvre le hangar avec les avions concernés, puis quitte. L'export sert à rattacher les nouvelles armes à leur nom interne.
4. Double-clique sur **`mettre_a_jour.bat`**. Si le jeu est installé ailleurs que dans `G:\SteamLibrary\steamapps\common\Nuclear Option`, corrige la ligne `JEU=` au début du fichier.
5. Relance le jeu. Le journal BepInEx doit afficher `[Wiki] Données lues depuis … weapons.json`.

`mettre_a_jour.bat` lance `python tools/mettre_a_jour.py --jeu "<dossier du jeu>"`, qui :
- prend l'export le plus récent ;
- met à jour `data/correspondance.csv` ;
- régénère `data/weapons.json` ;
- le copie à côté de `NuclearOptionWiki.dll`.

Le `weapons.json` placé à côté de la DLL est **prioritaire** sur les données intégrées à la DLL. Quand tu installes une nouvelle version du mod, relance `mettre_a_jour.bat`, ou supprime ce `weapons.json`, pour ne pas garder d'anciennes données.

**Ce qui demande une nouvelle version du mod** : ajouter une nouvelle **catégorie** de note, c'est-à-dire une nouvelle colonne de cible ou de défense. De nouvelles armes ou de nouvelles valeurs n'en ont pas besoin.

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
2. Lance `tools/package.sh`, qui produit `dist/NuclearOptionWiki-<version>.zip` et copie la DLL dans `release/`. Chaque version terminée y est commitée.
