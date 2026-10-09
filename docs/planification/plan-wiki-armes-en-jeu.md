# Plan — Wiki d'armes en jeu (Nuclear Option)

Version : 1 · Date : 2026-10-09 · Statut : validé (2026-10-09)

## 1. Objectif et livrable

Ajouter à Nuclear Option une **fiche « wiki » d'arme** affichée dans l'écran de **loadout / hangar** : un panneau fixe à gauche, superposé à la scène 3D. Il présente les stats et les notes d'une arme avec des voyants de style MFD.

Livrable final :
- un **mod BepInEx 5** (`NuclearOptionWiki.dll`) installable via NOMM (dossier `BepInEx/plugins/NuclearOptionWiki/`) ;
- un fichier de données **`weapons.json`** placé à côté de la DLL et généré depuis l'Excel de Florian (`data/Armes_regroupees.xlsx`) par un script Python du dépôt ;
- un **README en français** (installation, touches, réglages, mise à jour des données).

Dépôt : `zeviper-gaming/nuclear-option-wiki`, branche de travail `claude/practical-bohr-lg5opp`.

## 2. Contexte nécessaire à l'exécutant

- **Jeu** : Nuclear Option (Unity, Mono, .NET Framework 4.x), version de la DLL fournie par Florian le 2026-10-09. Le jeu utilise uGUI + TextMeshPro, Rewired pour ses entrées, et l'entrée Unity « legacy » (`Input.GetKeyDown`) reste active.
- **Modpack de Florian** (BepInEx 5 via NOMM), 23 mods dont Configuration Manager (**F1**), Loadout Presets (champ texte dans le hangar), EditorPlus, NOXMFD, armes Aryx et Ternion. Touches déjà prises : F1, F7, F8, F9, Home, End, Pavé 5, Shift+Q, Ctrl+C / V / D.
- **Ce que Claude ne peut pas faire** : lancer le jeu. Tout test en jeu est fait par Florian, qui renvoie captures d'écran et journal `BepInEx/LogOutput.log`.
- **Outils dans la session cloud** :
  - SDK .NET 8 : `apt-get install -y dotnet-sdk-8.0` (dépôts Ubuntu accessibles). Le site officiel est bloqué.
  - NuGet (nuget.org) accessible ; le flux NuGet de BepInEx est bloqué.
  - BepInEx 5 : `https://github.com/BepInEx/BepInEx/releases/download/v5.4.23.2/BepInEx_win_x64_5.4.23.2.zip` (téléchargement vérifié). On en extrait `BepInEx/core/BepInEx.dll` et `0Harmony.dll`.
  - Décompilateur : `dotnet tool install -g ilspycmd --version 8.2.0.7535`, lancé avec `DOTNET_ROLL_FORWARD=Major`.
  - Playwright + Chromium préinstallés (`/opt/pw-browsers/chromium`), pour les captures de la maquette.
- **DLL du jeu** : elles ne sont **jamais commitées** (droits de l'éditeur) ; elles vont dans `lib/`, qui est ignoré par git. Florian les dépose au début de la session d'exécution (voir étape E0).
- **Référence visuelle** : `docs/maquettes/fiche-arme-mfd.html`, publiée sur https://claude.ai/artifact/8JAaLwuogtTfQFgh2XWjGP. La variante **A** est la cible par défaut ; B et C sont les thèmes optionnels.
- **Points d'accroche dans le code du jeu** (vérifiés par décompilation) :

| Classe | Membre | Rôle |
|---|---|---|
| `AircraftSelectionMenu` | `Initialize(Player, Airbase)` (public) ; champ privé `loadoutSelector` | Ouverture de l'écran hangar. Détruit à la sortie (`OnDestroy`). |
| `LoadoutSelector` | `ShowMountInfo(WeaponMount mount)` (public) | Appelée au **survol d'une option** de la liste déroulante ouverte (mount = null si option « Empty »). |
| `WeaponSelector` | `DropdownChanged(int)` (public), `GetValue()` → `WeaponMount` | **Sélection** d'une arme dans un emplacement. |
| `WeaponSelector` | `Initialize(Aircraft, HardpointSet, FactionHQ, Airbase)` (public) | Création d'un emplacement : on y greffe le survol de l'**emplacement fermé**. |
| `WeaponMount` | `jsonKey`, `mountName`, `info` (`WeaponInfo`) | Option d'emport (« AGM-48 x3 ») ; `info` = l'arme. |
| `WeaponInfo` | `name` (nom d'asset), `weaponName`, `shortName`, `weaponPrefab`, `targetRequirements.maxRange`, flags `missile/bomb/gun/…` | L'arme. |
| `Missile` (sur `weaponPrefab`) | `definition.radarSize` (RCS), moteurs : `IR_intensity` ; privés : `maxTurnRate`, `gLimit` | Valeurs réelles lues en jeu. |
| `Encyclopedia` | `i.weaponMounts`, `WeaponLookup` | Liste des emports connus. |

## 3. Décisions figées

| # | Décision | Raison |
|---|---|---|
| D1 | Mod BepInEx 5, C#, `netstandard2.1` (repli `net472` si le chargement échoue en jeu). | Build test réussi en `netstandard2.1` contre la DLL du jeu. |
| D2 | Écran ciblé : **loadout / hangar uniquement**. | Cadrage. |
| D3 | **Touche bascule** on/off, défaut **F6**, **modifiable par Florian** (détail en D23). La touche n'est lue que si l'écran hangar est ouvert **et** qu'aucun champ texte n'a le focus. | Pas de conflit avec le modpack ni avec la saisie d'un nom de preset. |
| D23 | **Choix de la touche** : entrée de config de type `KeyboardShortcut`, donc n'importe quelle touche ou combinaison avec Ctrl, Shift ou Alt. Trois façons de la modifier :<br>1. dans le menu **F1** (Configuration Manager), section « Général » : bouton de capture, puis appui sur la touche voulue ;<br>2. dans le fichier `.cfg` (ex. `Touche bascule = LeftControl + W`) ;<br>3. remise à F6 par le bouton « Reset » de F1.<br>La nouvelle touche s'applique **immédiatement** (`SettingChanged`), sans redémarrage. Si elle est vide ou fait partie des touches réservées connues (F1, F7, F8, F9, Home, End, Keypad5, Shift+Q, Escape), un **avertissement** est écrit dans le journal et affiché 4 s en bas de la fiche, mais le choix est respecté (Escape, qui ferme le hangar, est refusé et remplacé par F6). La touche active est rappelée dans le **pied de la fiche** (« F6 · masquer »), dans la description de l'entrée F1 et dans le README. | Demande de Florian : pouvoir choisir et modifier la touche. |
| D4 | Mode actif : la fiche affiche **la dernière arme survolée ou sélectionnée** (affichage « collant »). Sources : survol d'une option de la liste ouverte, survol d'un emplacement fermé (arme équipée), sélection. Survoler « Empty » ou un emplacement vide **ne change rien**. Avant toute interaction : message « Survolez ou sélectionnez une arme ». | Évite le clignotement ; comportement prévisible. |
| D5 | Panneau **fixe à gauche** : 600 × 840 px en référence 1920×1080, position (12, 12) depuis le coin haut-gauche. Mis à l'échelle par `CanvasScaler` (référence 1920×1080, `matchWidthOrHeight = 1`, donc selon la hauteur) × réglage « Échelle ». | Fidèle à l'ébauche ; adaptable à la résolution. |
| D6 | **Mise en page strictement identique** pour toutes les armes : mêmes zones, mêmes tuiles, même ordre. Valeur absente → voyant gris + « N/A ». | Exigence de Florian. |
| D7 | Rendu en **uGUI** construit par code, sur un **Canvas dédié** (`ScreenSpaceOverlay`, `sortingOrder` 500) **sans `GraphicRaycaster`**, et `raycastTarget = false` partout. | Ne bloque aucun clic ; isolé de l'interface du jeu et des autres mods. |
| D8 | Police : la police TMP du jeu, récupérée sur un `TMP_Text` existant de l'écran hangar ; repli sur la police TMP par défaut. | Cohérence visuelle, aucune ressource à embarquer. |
| D9 | **Thème** réglable (menu F1) : `MfdPhosphore` (A, **défaut**), `Annonciateur` (B), `HangarNO` (C), tels que dans la maquette. | Choix de Florian. |
| D10 | Note de **portée /10** : deux calculs dans le JSON ; choix dans le menu F1, **défaut Logarithmique**. Log : `1 + 9 × (log10(portée_m) − 2) / 4`, bornée à [1 ; 10], 1 décimale (100 m → 1, 1 000 km → 10). Rang : `1 + 9 × (nb de portées ≤ p − 1) / (n − 1)` sur les armes qui ont une portée, ex æquo en haut du groupe. | Florian a écrit « logarithmique » ; le rang reste disponible. |
| D11 | Note de **vitesse /10** : colonne « Vitesse indicielle (/10) » de l'Excel, telle quelle. | Déjà calculée par Florian. |
| D12 | **Signatures** (radar, IR) : note haute = grande signature = **mauvais** pour l'arme → couleurs **inversées** (0 vert, 5 rouge). Les autres notes : haute = bonne. | Réponse de Florian. |
| D13 | Note de **signature radar /5** calculée depuis la RCS de l'Excel : `clamp(round(5 × (log10(rcs) + 3) / 3), 0, 5)` (0,001 m² → 0 ; 1 m² → 5). La tuile affiche **la note et la valeur réelle** (m²). | « Valeurs réelles quand possible, avec les notes. » |
| D14 | **Valeurs réelles lues en jeu** quand l'arme a un `Missile` sur `weaponPrefab` : RCS (`definition.radarSize`), intensité IR (somme des `IR_intensity` des moteurs), agilité (`maxTurnRate` en °/s et `gLimit` en G). Lecture par réflexion, mise en cache par `WeaponInfo`, sans exception propagée ; échec → valeur « — ». Les notes viennent **toujours** du JSON. | Valeurs réelles demandées ; l'Excel n'a ni IR réel ni agilité. |
| D15 | Colonnes vides de l'Excel (Besoin laser, Fire and Forget, Virage / Agilité) : voyants présents, « N/A » tant qu'elles sont vides. | Choix de Florian. |
| D16 | Arme absente du JSON : même fiche, tout en N/A, nom de l'arme en titre, mention « ARME NON RÉFÉRENCÉE », et une ligne dans le journal BepInEx (une seule fois par arme). Les valeurs lues en jeu (D14) s'affichent quand même. | Choix de Florian. |
| D17 | Pas de note globale. | Mélanger air-sol, AA et interception dans un seul chiffre serait arbitraire ; chaque zone a ses voyants. |
| D18 | Clé d'arme = **nom d'asset `WeaponInfo.name`**. Ordre de recherche : `WeaponInfo.name` → alias `WeaponMount.jsonKey` → `WeaponInfo.weaponName` s'il est unique dans le JSON → fiche non référencée. | Plusieurs emports partagent une même arme ; les noms affichés sont ambigus. |
| D19 | Correspondance Excel ↔ jeu établie depuis un **export des armes chargées en jeu** (CSV produit par le mod, étape E3), puis complétée à la main pour les cas ambigus, validés par Florian. | Seule source fiable des noms d'assets, y compris ceux des mods. |
| D20 | Code séparé en **Core sans Unity** (données, notes, formatage, modèle de fiche) et **Plugin** (Unity, Harmony, UI). Core est testé par un projet console .NET 8. | Permet de vérifier ici, sans le jeu, que les 133 fiches se construisent sans erreur. |
| D21 | Libellés de l'interface **en français**. | Cadrage. |
| D22 | Le mod est **purement local** (client), sans réseau ni modification des données du jeu. | Hors périmètre multijoueur. |

## 4. Laissé libre à l'exécutant

- Le nom exact des classes internes, tant que l'arborescence de la section 5 est respectée.
- La construction interne des widgets uGUI (positionnement absolu ou `LayoutGroup`), à condition de respecter les cotes de la section 7.3.
- Le format des messages de journal (préfixe `[Wiki]` obligatoire).
- Le GUID du plugin : `zeviper.nuclearoption.wiki` est suggéré, l'exécutant peut le confirmer avec Florian.

## 5. Architecture / structure du livrable

```
nuclear-option-wiki/
├─ README.md                         installation, touches, réglages, mise à jour des données (FR)
├─ .gitignore                        lib/, bin/, obj/, *.user, dist/
├─ data/
│  ├─ Armes_regroupees.xlsx          source de Florian (commitée)
│  ├─ correspondance.csv             clé Excel « Arme|Source » ↔ WeaponInfo.name (+ statut)
│  ├─ dump/                          CSV exportés par le mod en jeu (commités)
│  └─ weapons.json                   GÉNÉRÉ — ne pas éditer à la main
├─ tools/
│  ├─ xlsx_vers_json.py              Excel + correspondance → weapons.json (calcule D10, D13)
│  └─ correspondance.py              dump CSV + Excel → proposition de correspondance.csv
├─ src/
│  ├─ Core/                          C# pur, sans référence Unity
│  │  ├─ WeaponRecord.cs             modèle d'une arme (schéma JSON v1)
│  │  ├─ WeaponDatabase.cs           chargement JSON + recherche (D18)
│  │  ├─ NoteScale.cs                couleurs, sens inversé, segments /10
│  │  ├─ Format.cs                   formatage FR (m, km, m/s, M$, kg, m²)
│  │  └─ FicheModel.cs               modèle de fiche : liste FIXE des champs (section 7.2)
│  └─ Plugin/
│     ├─ NuclearOptionWiki.csproj    compile aussi ../Core/**/*.cs
│     ├─ Plugin.cs                   BaseUnityPlugin : config, Harmony, chargement JSON
│     ├─ WikiConfig.cs               entrées de config (section 7.4)
│     ├─ Integration/
│     │  ├─ LoadoutHooks.cs          patchs Harmony Postfix (section 7.5)
│     │  ├─ SlotHoverRelay.cs        IPointerEnterHandler sur chaque WeaponSelector
│     │  └─ WikiController.cs        état (actif, arme courante), touche, visibilité
│     ├─ GameData/LiveWeaponStats.cs lecture en jeu (D14), cache par WeaponInfo
│     ├─ Diagnostics/WeaponDump.cs   export CSV des armes chargées (section 7.6)
│     └─ UI/
│        ├─ WikiPanel.cs             construit le Canvas et les 3 zones, applique un FicheModel
│        ├─ Themes.cs                palettes A / B / C
│        └─ Widgets/ (Tile.cs, Bar10.cs, StatCell.cs, ZoneTitle.cs)
├─ tests/Core.Tests/                 console net8 : charge weapons.json, construit les 133 fiches
├─ lib/                              (ignoré) DLL du jeu + BepInEx + Harmony
└─ docs/
   ├─ planification/                 plan + état
   └─ maquettes/fiche-arme-mfd.html  référence visuelle
```

**Flux à l'exécution :**
1. `Plugin.Awake` lit la config, charge `weapons.json` (échec → journal d'erreur, mod inactif sans planter) et applique les patchs Harmony.
2. `AircraftSelectionMenu.Initialize` (Postfix) crée ou réutilise le `WikiController` et le `WikiPanel` (caché), puis récupère la police TMP du jeu.
3. Les événements de survol et de sélection appellent `WikiController.ShowWeapon(WeaponMount)`, qui met l'arme à jour seulement si `mount?.info != null`.
4. `WikiController.Update` :
   - si le menu n'existe plus ou est inactif, cacher le panneau ;
   - sinon, gérer la touche bascule (D3) ;
   - si l'arme a changé, construire le `FicheModel` (Core) avec les valeurs en jeu et l'appliquer au panneau.
   - Aucune allocation par image en dehors d'un changement d'arme.

## 6. Étapes

| # | Étape | Dépend de | Critère d'acceptation (« fini quand… ») |
|---|---|---|---|
| E0 | **Environnement** : installer le SDK .NET 8. Récupérer BepInEx 5.4.23.2 (`BepInEx.dll`, `0Harmony.dll`) dans `lib/`. Demander à Florian ses DLL : `Assembly-CSharp.dll` et, depuis `NuclearOption_Data/Managed/`, `UnityEngine.dll`, `UnityEngine.CoreModule.dll`, `UnityEngine.UI.dll`, `UnityEngine.UIModule.dll`, `UnityEngine.TextRenderingModule.dll`, `UnityEngine.InputLegacyModule.dll`, `UnityEngine.IMGUIModule.dll`, `Unity.TextMeshPro.dll`, `Newtonsoft.Json.dll`. | — | `ls lib/` montre ces fichiers ; `git status` ne les liste pas. |
| E1 | **Squelette** : arborescence, `.gitignore`, csproj (références `lib/` avec `Private=false`), `Plugin.cs` minimal (journal « [Wiki] chargé »), `WikiConfig.cs` complet. | E0 | `dotnet build -c Release` réussit sans avertissement d'assembly manquant ; la DLL est produite. |
| E2 | **Données** : `tools/xlsx_vers_json.py` (openpyxl) → `data/weapons.json` au format 7.1. Calcul de D10 et D13 ; vides, « N/A » et texte → `null`. Tant que E4 n'est pas fait, la clé est provisoire (`weaponName`), avec le champ `cle_provisoire: true`. | E1 | 133 entrées ; contrôle ponctuel : AGM-48 (vitesse 2,1 ; portée log 5,5 / rang 6,7 ; CAS 5,5,3,1,0,3,1,0 ; RCS 0,02 → note 2 ; IR 1) ; le script est idempotent (2 exécutions = fichier identique). |
| E3 | **Export diagnostic** : `WeaponDump` écrit `BepInEx/config/NuclearOptionWiki_armes.csv` (format 7.6) à l'ouverture du hangar si l'option est active. Livrer **v0.1** (zip) à Florian. | E1 | Florian renvoie le CSV, déposé dans `data/dump/`. ⚠️ Étape bloquante côté Florian. |
| E4 | **Correspondance** : `tools/correspondance.py` rapproche le dump et l'Excel (nom exact, puis nom normalisé, puis par source de mod) et produit `data/correspondance.csv` avec un statut `auto` / `ambigu` / `absent_excel` / `hors_hangar`. Soumettre les `ambigu` à Florian, intégrer ses réponses, régénérer `weapons.json` (clés définitives). | E2, E3 | Aucun statut `ambigu` restant ; chaque arme du dump montable en hangar est mappée ou validée « non référencée » par Florian. |
| E5 | **Core** : `WeaponRecord`, `WeaponDatabase`, `NoteScale`, `Format`, `FicheModel`. **Tests** `tests/Core.Tests` : chargement du JSON réel, construction des fiches de toutes les entrées et d'une arme inconnue, vérification du nombre fixe de champs, des couleurs inversées, des formats FR et de la recherche D18. | E2 | `dotnet run --project tests/Core.Tests` → « OK » et code 0 ; toutes les fiches ont exactement le même nombre de champs dans le même ordre. |
| E6 | **UI** : `WikiPanel` + widgets + 3 thèmes, cotes de la section 7.3. Ajouter un réglage de diagnostic « Afficher une fiche de démonstration » qui affiche AGM-48 dès l'ouverture du hangar. | E1, E5 | Build OK. Florian envoie 3 captures (un thème chacune) : la fiche correspond à la maquette (zones, ordre, couleurs, N/A) et ne bloque aucun clic. |
| E7 | **Intégration** : `LoadoutHooks`, `SlotHoverRelay`, `WikiController` (D3, D4, D5, D23 : touche modifiable à chaud, avertissement, pied de fiche). | E6 | Recette en jeu de Florian (section 8, checklist R1–R8 et R7b) OK. |
| E8 | **Valeurs en jeu** : `LiveWeaponStats` (D14), branché sur les tuiles RCS, IR et Agilité et sur les fiches non référencées. | E7 | Missile (AGM-48) : RCS, IR et agilité affichés ; canon : « — » ; aucune exception dans le journal. |
| E9 | **Empaquetage et doc** : cible `dotnet build` ou script `tools/package.sh` → `dist/NuclearOptionWiki-<version>.zip` contenant `BepInEx/plugins/NuclearOptionWiki/{NuclearOptionWiki.dll, weapons.json}`. README FR. | E8 | Zip installé par Florian depuis zéro → recette R1–R8 OK ; README relu par Florian. |

Ordre : E0 → E1 → (E2 ∥ E3) → E4 ; E5 après E2 ; E6 après E5 ; E7 → E8 → E9. Pendant que Florian teste E3, avancer sur E5 et E6.

## 7. Formats exacts et exemples

### 7.1 `weapons.json` (schéma v1)

```json
{
  "schema": 1,
  "genere_le": "2026-10-09T14:00:00Z",
  "source": "Armes_regroupees.xlsx",
  "armes": {
    "AGM48_Info": {
      "nom": "AGM-48", "source": "Jeu de base", "type": "Missile",
      "guidage": "Optique/TV (opérateur)", "approche": "datalink; loft 0.2; évasion zigzag 0.1; évite le relief",
      "penetration": 1550, "explosif_kg": 9, "cout_m": 0.09, "explosif_par_m": 100,
      "portee_min_m": 1000, "portee_max_m": 10000, "vitesse_ms": 357.5,
      "note_vitesse": 2.1, "note_portee_log": 5.5, "note_portee_rang": 6.7,
      "laser_requis": null, "tire_et_oublie": null,
      "air_sol": { "vehicules_legers": 5, "chars": 5, "batiments_legers": 3, "gros_batiments": 1,
                   "bunker": 0, "navire_leger": 3, "navire_moyen": 1, "porte_avions": 0 },
      "air_air": { "petit_avion": null, "moyen_avion": null, "gros_avion": null },
      "defense": { "agilite": null, "rcs_m2": 0.02, "note_rcs": 2, "note_ir": 1,
                   "resistance_laser": 1, "difficulte_toucher": 0 }
    }
  },
  "alias": { "AGM48_x3": "AGM48_Info" }
}
```

- Les clés de `armes` sont des `WeaponInfo.name` (ici `AGM48_Info` est un **exemple fictif** : les vrais noms viennent du dump E3).
- Toute valeur inconnue vaut `null`.
- `laser_requis` et `tire_et_oublie` valent `true`, `false` ou `null`.
- Les valeurs ci-dessus sont celles de l'Excel ; le nom de clé et l'alias sont des exemples.

### 7.2 Champs de la fiche (liste fixe, dans cet ordre)

| Zone | Champ | Source | Affichage |
|---|---|---|---|
| 1 Stats globales | Titre | `nom` (ou nom en jeu si non référencée) | texte 30 px |
| | Puces | `type`, `source` (ou « HORS EXCEL ») | 2 puces |
| | Ligne | « Guidage : … · Approche : … » (ou « ARME NON RÉFÉRENCÉE ») | 1 ligne, coupée par « … » |
| | Barre VITESSE | `note_vitesse` /10 · `vitesse_ms` « 358 m/s » | Bar10 |
| | Barre PORTÉE | `note_portee_log` ou `note_portee_rang` selon config · `portee_max_m` « 10 km » | Bar10 |
| | 7 cellules | Pénétration · Explosif (kg) · Coût (M$) · Explosif / M$ · Portée min · Laser requis · Tire et oublie (sur 2 colonnes) | StatCell |
| 2 Capacité air-sol (CAS) | 8 tuiles /5 | Véh. légers · Chars · Bât. légers · Gros bât. · Bunker · Navire léger · Navire moyen · Porte-avions | Tile |
| 2 Capacité air-air (AA) | 3 tuiles /5 | Petit avion · Moyen avion · Gros avion | Tile |
| 3 Interception · défenses | 5 tuiles | Agilité (note `agilite` + réel « 35 °/s · 20 G ») · Sign. radar (`note_rcs` **inversée** + « 0,02 m² ») · Sign. IR (`note_ir` **inversée** + réel IR) · Rés. laser · Diff. toucher | Tile (variante note + valeur) |

**Couleurs des notes /5** : 0 `#FF3B3B` · 1 `#FF6A2B` · 2 `#FFA31F` · 3 `#FFD523` · 4 `#A9FF2F` · 5 `#3DFF57` · N/A `#5D6670`.
- Note inversée : couleur de `5 − note`.
- Barres /10 : couleur de `round(note / 2)` ; segments allumés `clamp(round(note), 1, 10)` ; note `null` → 0 segment et « N/A ».

**Formats FR** :
- distance : < 1000 m → « 800 m », sinon « 7,5 km » (1 décimale sous 10 km), « 1 000 km » ;
- vitesse : « 850 m/s » ; coût : « 0,09 M$ » ; explosif : « 9 kg » ; RCS : « 0,02 m² » (3 décimales max, zéros finaux retirés) ;
- note /10 : « 5,5/10 » ; note /5 : « 3/5 ».

### 7.3 Cotes du panneau (px en 1920×1080, variante A)

- Panneau 600 × 840, marge interne 14, espace entre blocs 10.
- Fond `rgba(3,7,5,0.9)`, bord 1 px `#1F2B24`, rayon 10.
- Titres de zone 13 px, interlettrage large, `#46FF5A`.
- Barre : libellé 88 · segments (10, écart 3, hauteur 16) · note 64 · valeur 96.
- Cellules de stats : grille 4 colonnes, écart 6.
- Tuiles : hauteur 78 ; grilles de 4 (CAS), 3 (AA) et 5 (défenses), écart 8.
- Lampe : 42 % de la largeur × 4 px, à 8 px du haut, avec halo.
- Libellé de tuile 11 px, **toujours vert** `#46FF5A` en thème A ; note 28 px dans la couleur de la note, suivie de « /5 » en 12 px.
- Les thèmes B et C reprennent les valeurs CSS des classes `.vb` et `.vc` de la maquette. La maquette fait foi pour tout détail non coté ici.

### 7.4 Configuration (`BepInEx/config/zeviper.nuclearoption.wiki.cfg`, visible dans F1)

| Section | Clé | Type | Défaut |
|---|---|---|---|
| Général | Touche bascule | KeyboardShortcut (touche + Ctrl/Shift/Alt possibles, capture dans F1) — voir D23 | F6 |
| Général | Affichage automatique au hangar (pas d'apostrophe : interdite par BepInEx dans un nom de clé) | bool | false |
| Affichage | Thème | enum MfdPhosphore / Annonciateur / HangarNO | MfdPhosphore |
| Affichage | Note de portée | enum Logarithmique / Rang | Logarithmique |
| Affichage | Échelle | float 0,75–1,5 | 1,0 |
| Affichage | Opacité du fond | float 0,5–1 | 0,9 |
| Diagnostic | Exporter la liste des armes | bool | false |
| Diagnostic | Fiche de démonstration | bool | false |
| Diagnostic | Journal détaillé | bool | false |

Un changement de thème, d'échelle ou de note de portée s'applique immédiatement, via l'événement `SettingChanged`.

### 7.5 Patchs Harmony (tous en Postfix, chacun dans un try/catch qui journalise et n'interrompt jamais le jeu)

```csharp
[HarmonyPatch(typeof(AircraftSelectionMenu), nameof(AircraftSelectionMenu.Initialize))]
static void Postfix(AircraftSelectionMenu __instance) => WikiController.Attach(__instance);

[HarmonyPatch(typeof(LoadoutSelector), nameof(LoadoutSelector.ShowMountInfo))]
static void Postfix(WeaponMount mount) => WikiController.ShowWeapon(mount);   // ignore null

[HarmonyPatch(typeof(WeaponSelector), nameof(WeaponSelector.DropdownChanged))]
static void Postfix(WeaponSelector __instance) => WikiController.ShowWeapon(__instance.GetValue());

[HarmonyPatch(typeof(WeaponSelector), nameof(WeaponSelector.Initialize),
    new[] { typeof(Aircraft), typeof(HardpointSet), typeof(FactionHQ), typeof(Airbase) })]
static void Postfix(WeaponSelector __instance) => SlotHoverRelay.AttachTo(__instance);
```

`Initialize` a deux surcharges : il faut préciser les types.

### 7.6 CSV du dump (UTF-8, séparateur `;`)

```
info_name;weapon_name;short_name;mount_jsonkey;mount_name;missile;bomb;gun;energy;cargo;troops;dans_encyclopedie
AGM48_Info;AGM-48;AGM48;AGM48_x3;AGM-48 x3;1;0;0;0;0;0;1
```

Source : `Resources.FindObjectsOfTypeAll<WeaponMount>()`, en joignant les `WeaponInfo` sans emport via `FindObjectsOfTypeAll<WeaponInfo>()` (colonnes `mount_*` vides). La colonne `dans_encyclopedie` est déduite de `Encyclopedia.i.weaponMounts`.

## 8. Pièges connus et risques

| Risque / piège | Parade |
|---|---|
| Un patch casse après une mise à jour du jeu. | Patchs Postfix uniquement, try/catch, vérification au démarrage que les méthodes existent (sinon journal + mod inactif). |
| Conflit avec un autre mod du hangar (Loadout Presets, EditorPlus). | Canvas dédié sans raycaster, aucun objet du jeu modifié, aucun Prefix. Recette R6. |
| Touche tapée dans un champ texte (nom de preset). | Ignorer la touche si `EventSystem.current.currentSelectedGameObject` porte un `TMP_InputField` ou un `InputField`. |
| `IPointerEnterHandler` sur l'emplacement ne reçoit rien. | Unity propage l'entrée de pointeur aux parents. Si la recette R3 échoue, ajouter le relais sur l'objet du `TMP_Dropdown` enfant. |
| Le survol natif ne passe que par les options de la liste ouverte. | Couvert par `ShowMountInfo` + `SlotHoverRelay`. |
| Champs privés (`maxTurnRate`, `gLimit`, moteurs) renommés. | Réflexion protégée (AccessTools), résultat « — », un seul avertissement par session. |
| Noms ambigus dans l'Excel (« 30mm Autocannon » ×5…). | Dump + statut `hors_hangar` pour les armes de véhicules et navires, absentes de l'écran hangar. |
| Excel basé sur un wiki possiblement antérieur à 0.34. | Les valeurs en jeu (D14) servent de contrôle ; écarts signalés à Florian, sans correction automatique. |
| Pas de test en jeu possible depuis le cloud. | Core testé ici (E5) ; « fiche de démonstration » (E6) ; recette R1–R8 par Florian, avec captures et journal. |
| Version de Unity inconnue. | Compiler contre les DLL `Managed/` de Florian (E0), pas contre NuGet. |

**Recette en jeu (Florian)** :
- R1 : F6 dans le hangar → la fiche apparaît et disparaît.
- R2 : survol des options de la liste ouverte → la fiche suit.
- R3 : survol d'un emplacement fermé → fiche de l'arme équipée.
- R4 : sélection d'une arme → fiche à jour.
- R5 : fiche identique sur 5 armes variées (missile, bombe, canon, arme Aryx, arme nucléaire), avec les N/A aux bons endroits.
- R6 : Loadout Presets fonctionne normalement (édition et saisie d'un nom de preset), tous les clics du hangar passent ; aucun clic bloqué par la fiche.
- R7 : changement de thème, d'échelle et de note de portée dans F1 appliqué immédiatement.
- R7b : dans F1, changer la touche (ex. `Ctrl + W`) → l'ancienne touche ne fait plus rien, la nouvelle bascule la fiche sans redémarrage, le pied de fiche affiche la nouvelle touche ; choisir F8 → avertissement visible ; « Reset » → retour à F6.
- R8 : FPS du hangar inchangés (compteur Steam ou Graphy), aucune erreur `[Wiki]` dans `LogOutput.log`.

## 9. Modèle d'exécution

**Opus 5.5, effort medium.** Plusieurs critères le justifient :
- exécution **longue et agentique** (décompilation, compilation, aller-retour avec les tests de Florian) ;
- **multi-fichiers** (une vingtaine) ;
- accroche à du **code de jeu non documenté**, sans test en jeu possible ici ;
- zones de jugement restantes sur la construction uGUI.

Sonnet 5.5 conviendra au peaufinage (phase 4).

## 10. Hors périmètre

- Édition des notes en jeu.
- Fiches avions, véhicules, navires (l'architecture Core / Plugin et le schéma JSON permettront d'ajouter une fiche véhicules plus tard).
- Synchronisation multijoueur.
- Rendu 3D ou image de l'arme.
- Écran de vol, éditeur de mission.
- Note globale (D17).

## Consigne à l'exécutant

Suivre les étapes dans l'ordre et vérifier chaque critère d'acceptation. En cas d'étape impossible, ambiguë ou contredite par la réalité (méthode du jeu absente, DLL manquante, comportement en jeu différent), **s'arrêter et le signaler à Florian plutôt qu'improviser**. Un problème de structure renvoie en phase 2.
