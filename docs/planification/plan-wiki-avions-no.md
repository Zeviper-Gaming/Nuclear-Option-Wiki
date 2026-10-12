# Plan — Wiki avions et hélicos (Nuclear Option)

Version : 3 · Date : 2026-10-12 · Statut : **validé** le 2026-10-12, amendement v3.1 (voyant VISEURS IR, A21) le 2026-10-12 (v2 : corrections manuelles A20 ; v3 : branche `zv/plane_update`, IR à 30 % de gaz et plein gaz, description en bas de fiche)

## 1. Objectif et livrable

Ajouter au mod existant **NuclearOptionWiki** une **fiche « appareil »** (avions, hélicoptères, VTOL, convertibles ; vanilla et moddés), dans le même panneau MFD que la fiche arme, à l'écran hangar. But : comparer les appareils et choisir sa monture en connaissance de cause.

Livrables :
1. **Mod v0.5.0** : `NuclearOptionWiki.dll` unique (données des armes **et** des appareils intégrées), dans `release/` et en zip, importable par NOMM.
2. **Classeur `data/Avions_wiki.xlsx`** : toutes les stats consolidées des appareils (Excel de Florian + export en jeu), le détail des emports, trois colonnes rédactionnelles (**Rôle(s)**, **Description (FR)**, **Texte perso** ; les deux premières préremplies par Claude, la troisième vide pour Florian) et une feuille **« Corrections »** où Florian peut forcer n'importe quelle valeur affichée (A20).
3. **`data/aircraft.json`** généré depuis ce classeur, et la chaîne de mise à jour sans recompiler (`mettre_a_jour.bat`).
4. **README** mis à jour (FR).

Dépôt : `zeviper-gaming/nuclear-option-wiki`, branche **`zv/plane_update`** (partie de `main` = v0.4.1). Ne pousser que sur cette branche.

## 2. Contexte nécessaire à l'exécutant

### 2.1 Le mod existant (à lire avant de coder)
- `README.md`, `docs/planification/plan-wiki-armes-en-jeu.md` (v1.1) et `etat-wiki-armes-en-jeu.md` : architecture **Core** (C# pur, testable en console) / **Plugin** (Unity, Harmony, uGUI), thèmes A/B/C, règles N/A, touche F6 modifiable, données JSON intégrées comme ressource avec surcharge par un fichier posé à côté de la DLL.
- Code clé : `src/Core/FicheModel.cs` (modèle de fiche à champs fixes), `src/Core/NoteScale.cs`, `src/Core/Format.cs`, `src/Plugin/UI/WikiPanel.cs` (panneau 600 × 840), `src/Plugin/UI/Widgets/*`, `src/Plugin/Integration/{LoadoutHooks,WikiController,SlotHoverRelay}.cs`, `src/Plugin/Diagnostics/WeaponDump.cs`, `src/Plugin/GameData/LiveWeaponStats.cs`, `tools/{xlsx_vers_json,mettre_a_jour}.py`.
- **Règle du dépôt** : les DLL du jeu ne sont jamais commitées (`lib/` ignoré).

### 2.2 Environnement cloud
- `apt-get update && apt-get install -y dotnet-sdk-8.0` (le paquet n'est trouvé qu'après `apt-get update`).
- Décompilateur : `dotnet tool install -g ilspycmd --version 8.2.0.7535`, lancé avec `DOTNET_ROLL_FORWARD=Major` et `PATH=$PATH:/root/.dotnet/tools`.
- BepInEx 5.4.23.2 : `https://github.com/BepInEx/BepInEx/releases/download/v5.4.23.2/BepInEx_win_x64_5.4.23.2.zip` → `BepInEx.dll`, `0Harmony.dll` dans `lib/`.
- DLL du jeu à demander à Florian (liste en étape V0). `Assembly-CSharp.dll` a été fourni le 2026-10-12 (jeu 0.34.1) : en session neuve, le redemander s'il n'est plus là.
- Playwright + Chromium préinstallés (`/opt/pw-browsers/chromium`) pour capturer la maquette.
- Claude **ne peut pas lancer le jeu** : Florian teste et renvoie captures, `BepInEx/LogOutput.log` et exports CSV.

### 2.3 Ce que dit le code du jeu (décompilation du 2026-10-12, `Assembly-CSharp.dll` 0.34.1)

| Élément | Constat | Usage |
|---|---|---|
| `AircraftSelectionMenu` | Un **seul écran** contient la grille d'appareils (`AircraftInventoryMenu`, en haut au centre) **et** le loadout (en bas). Événement **public** `event Action<AircraftDefinition> OnSelectedAircraftChange`, levé à la fin de `SpawnPreview()` (ouverture du hangar, puis chaque changement d'appareil via `SetSelectedType`). `GetSelectedType()` renvoie l'appareil courant. | Sélection d'appareil. |
| Ordre des événements | `SpawnPreview` appelle `loadoutSelector.AssignAircraft(...)` (qui peut lever `WeaponSelector.DropdownChanged`, via `dropdown.value = 0`) **avant** de lever `OnSelectedAircraftChange`. | L'appareil arrive en dernier : il gagne. Voir les pièges. |
| `Initialize(Player, Airbase)` | Appelle `Refresh` → `SpawnPreview` **avant** le Postfix déjà patché par le mod : le premier `OnSelectedAircraftChange` est manqué. | Dans le Postfix, lire `GetSelectedType()`. |
| `AircraftSelectionButton` | Un bouton par appareil dans la grille, avec un champ **public** `AircraftDefinition definition` ; `Initialize(AircraftInventoryMenu, Player, HoverText)` public. Les appareils grisés (indisponibles ici) ont aussi un bouton. | Survol d'un bouton → fiche de l'appareil. |
| `UnitDefinition` (base de `AircraftDefinition`) | `jsonKey`, `unitName`, `code`, `description` (anglais), `radarSize` (RCS), `length`, `width`, `height`, `value` (coût), `mass`, `armorTier`, `roleIdentity` (`antiSurface`, `antiAir`, `antiMissile`, `antiRadar`), `unitPrefab`, `mapIcon` ; champ **privé** `isEventContent`. | Clé, valeurs de repli, exclusion de l'OVNI. |
| `AircraftDefinition` | `aircraftParameters` (`rankRequired`, `maxSpeed`, `aircraftGLimit`, `takeoffSpeed`, `takeoffDistance`, `verticalLanding`, `cornerSpeed`, `landingSpeed`, `StandardLoadouts`), `aircraftInfo` (`emptyWeight`, `maxSpeed`, `stallSpeed`, `maneuverability`, `maxWeight`). | Valeurs de repli. |
| Emports | `Aircraft.weaponManager` (public) → `WeaponManager.hardpointSets : HardpointSet[]`. `HardpointSet` : `name` (anglais, ex. « Inner Wing Pylons »), `hardpoints : List<Hardpoint>`, `weaponOptions : List<WeaponMount>` (armes compatibles), `precludingHardpointSets : List<byte>` (exclusions mutuelles, par index), `SymmetryName`. `Hardpoint` : `bayDoors : BayDoor[]` (non vide = **soute**), `BuiltInWeapons`, `BuiltInTurrets`. `WeaponMount` : `jsonKey`, `mountName`, `info`, `mass`, `ammo`, `Cargo`, `Troops`, `missileBay`, `radar`, `countermeasure`, `tailHook`, `slingloadHook`. | Zone EMPORTS. |
| **Taille d'emplacement** | **N'existe pas dans le jeu.** | Remplacée par la classe de charge (A9). |
| `WeaponInfo` | Drapeaux `gun`, `missile`, `bomb`, `glideBomb`, `nuclear`, `strategic`, `energy`, `jammer`, `cargo`, `troops`, `sling` ; `effectiveness : RoleIdentity`. | Catégories de compatibilité (A10). |
| IR | Signature **par moteur** : intensité de `IRMin` (ralenti) à `IRMax` (plein gaz, postcombustion comprise) selon la poussée. `JetNozzle.GetIRMin()` / `GetIRMax()` (public, `GetIRMax` ajoute les postcombustions), `TurbineEngine.GetIRMin()` / `GetIRMax()` (public), `DuctedFan` : champs **privés** `IRMin`, `IRMax`. L'autodirecteur IR vise **une** source au hasard (`Unit.GetIRSource`). | Valeurs IR (A11). |
| Viseurs | `Aircraft.EOTS` (public, `TargetDetector`) = viseur électro-optique/IR principal ; liste privée `targetDetectors`. Un viseur = composant `TargetDetector` **de type exact** `TargetDetector` (les `Radar` en dérivent et sont exclus). `GetVisualRange()` (portée, m) et `GetVisualMagnification()` (grossissement) publics. Les nacelles sont des `WeaponMount` montées en jeu, absentes du prefab de l'appareil. | Voyant VISEURS IR (A21). |
| Localisation | **Aucune** : textes du jeu en anglais seulement. | Traductions dans le classeur (A13). |
| Écran (capture de Florian, 1920×1080) | Fiche à gauche (12 → 612 px), grille d'appareils de x ≈ 670 à 1250, liste d'emplacements en bas (y ≥ 860), description anglaise du jeu en bas au centre, et à droite les valeurs en direct du jeu (Value, Payload, Fuel, P/W, RCS du chargement). | La fiche reste à gauche, 600 × 840. |

### 2.4 Données de départ
- `data/sources/NO-stats.xlsx`, feuille « Avions » : 25 appareils (14 vanilla dont l'OVNI « ??? », 11 moddés). Colonnes A à AJ : Source, Appareil, Code, Type, Rang requis, Coût (M$), 4 rôles d'IA (0 à 1), Motorisation, Poussée (kN), Puissance (kW), Masse à vide, Masse max., Carburant interne, Vitesse max. (km/h), Décrochage (km/h), Maniabilité (0 à 10), Facteur G max, Décollage vertical, Vitesse et distance de décollage, Vitesse d'atterrissage, Vitesse de virage optimale, Dimensions « L × l × h (m) », Emplacements d'armes, Canons fixes, Portée radar (m), Brouilleur radar, Leurres IR, Paillettes, Palier de blindage, Signature radar, Description (jeu), Notes.
- Ce fichier est **produit ailleurs** (par Florian ou une autre session) et peut être remplacé : on ne l'édite jamais, on le lit.
- Trous connus : masse à vide nulle (OA-27, RAH-72, F-22E) ; emplacements d'armes vides pour 11 appareils ; décrochage vide pour les hélicos ; vitesses max. des mods « déclarées par l'auteur ».

## 3. Décisions figées

| # | Décision | Raison |
|---|---|---|
| A1 | **Extension du mod existant**, même DLL, même configuration F1, même touche F6. Version **0.5.0**. Aucune régression de la fiche arme (recette R1–R12 du plan armes). | Cadrage. |
| A2 | **Panneau unique** à gauche (600 × 840, mêmes thèmes, échelle, opacité). Il montre le **dernier élément** survolé ou sélectionné : **appareil** (changement d'appareil, survol d'un bouton de la grille) ou **arme** (règles inchangées). Avant toute interaction, à l'ouverture du hangar, il montre l'**appareil courant**. | Décision D-B. |
| A3 | Fiche appareil **strictement identique** pour tous les appareils (mêmes zones, champs, ordre) ; valeur sans objet ou inconnue = gris + « N/A ». | Cadrage. |
| A4 | Sources de données, par ordre de priorité, **champ par champ** : 0) **correction manuelle** de la feuille « Corrections » (A20), toujours prioritaire ; 1) `aircraft.json` (issu du classeur, donc de l'Excel de Florian + l'export) ; 2) **valeurs lues en jeu** sur la définition et le prefab (repli). Les **emports** et l'**IR** sont lus en jeu (exacts pour la version installée, mods compris), sauf correction manuelle ; le JSON fournit pour eux les traductions, les seuils et les corrections. | Comme D14 du wiki d'armes ; marche aussi pour un appareil moddé ajouté plus tard ; Florian garde la main sur chaque chiffre (décision du 2026-10-12). |
| A5 | **Clé d'appareil = `UnitDefinition.jsonKey`**. Recherche : `jsonKey` → `unitName` (unique dans le JSON) → fiche « APPAREIL NON RÉFÉRENCÉ » (tout en N/A sauf les valeurs lues en jeu, une ligne de journal par appareil). | `unitName` peut changer ; `jsonKey` est la clé réseau du jeu. |
| A6 | **OVNI exclu** : tout appareil dont `isEventContent` est vrai, ou de clé `UFO`, est absent du classeur et du JSON, ignoré des classements, et affiché comme non référencé s'il apparaît. | Décision D-E. |
| A7 | **Taille S/M/H de l'appareil** = encombrement `longueur × envergure` (m²), en échelle log : `t = (log10(e) − log10(e_min)) / (log10(e_max) − log10(e_min))` sur les appareils référencés ; S si `t ≤ 1/3`, M si `t ≤ 2/3`, sinon H. Les seuils en m² (`e_min`, `e_max` et les deux bornes) sont écrits dans le JSON, et le jeu les applique aux appareils non référencés. Contrôle sur NO-stats du 2026-10-11 : S ≤ 197 m², M ≤ 524 m² (12 S, 8 M, 4 H). | Décision D-C. |
| A8 | **Rôles** : colonne **Rôle(s)** du classeur, rédigée par Claude **d'après la description** de l'appareil, avec le vocabulaire fermé de la section 7.4. Laissée **vide** si la description ne permet pas de trancher. Affichage en pastilles ; vide → une pastille « N/A ». Les indices d'IA ne sont **pas** affichés (ils restent dans le classeur). | Décision D-D. |
| A9 | **Classe de charge S/M/H d'un emplacement** : charge max d'un point = masse (`WeaponMount.mass`) de l'option compatible la plus lourde, hors options `Cargo`/`Troops`/crochets. Classe par tiers log entre la plus petite et la plus grande charge max de **tous** les emplacements des appareils référencés (même formule que A7). Affichage : lettre + « 900 kg ». Emplacement sans option pesée → classe N/A. | Le jeu n'a pas de taille d'emplacement (décision du 2026-10-12). |
| A10 | **Compatibilité à l'écran = pastilles de catégories** + nombre d'armes compatibles. Catégories (règle en 7.5) : CANON, MISSILE AA, MISSILE AS, BOMBE, ROQUETTE, NUC, BROUILLEUR, LASER, CARGO. La **liste complète** des armes compatibles va dans la feuille « Emports » du classeur. | Décision du 2026-10-12. |
| A11 | **IR** : deux valeurs, calculées par moteur puis en prenant le **max des moteurs** (pas la somme), parce que l'autodirecteur vise une seule source. **IR à ≈ 30 % de gaz** = `IRMin + 0,3 × (IRMax_sec − IRMin)`, où `IRMax_sec` est l'IR plein gaz **sans** postcombustion (la postcombustion ne s'allume qu'au-delà de 85 % du régime, `JetNozzle.Thrust`) : champ privé `IRMax` de `JetNozzle`, `GetIRMax()` de `TurbineEngine`, champ privé `IRMax` de `DuctedFan`. C'est une **estimation** linéaire (le jeu passe par la montée en régime du moteur), signalée dans le Lisez-moi. **IR plein gaz** = max des `IRMax` **avec** postcombustion (`JetNozzle.GetIRMax()`, `TurbineEngine.GetIRMax()`, `DuctedFan.IRMax`). Chaque valeur a sa note /5 **inversée** = rang centile de cette valeur parmi les appareils de l'encyclopédie (hors contenu événementiel), calculée une fois par session, comme D30 du wiki d'armes. | Données du jeu ; demande de Florian (2026-10-12) : un voyant à ≈ 30 % de gaz et un voyant plein gaz. |
| A12 | **RCS** : valeur en m² (classeur, sinon `radarSize`) + note /5 **inversée**, échelle propre aux appareils : `clamp(round_half_up(5 × (log10(rcs) + 4) / 5), 0, 5)` (10⁻⁴ m² → 0 ; 10 m² → 5). Contrôle : F-117A 5·10⁻⁷ → 0 ; Vortex 0,001 → 1 ; Chicane 0,02 → 2 ; Revoker 0,08 → 3 ; Cricket 0,5 → 4 ; MiG-15 5 → 5. **Pas d'indice de furtivité combiné.** | Décision du cadrage (« notes séparées seulement ») ; l'échelle des armes sature à 1 m². |
| A13 | **Textes** : nom officiel du jeu conservé. Colonne **Description (FR)** = traduction fidèle et complète de la description du jeu, faite par Claude, relue par Florian. Colonne **Texte perso** vide, remplie par Florian. Les noms d'emplacements sont traduits dans la feuille « Emports » (colonne **Nom (FR)**, préremplie par Claude via le glossaire 7.6). Une traduction vide fait afficher le texte anglais du jeu. | Cadrage ; pas de localisation dans le jeu. |
| A14 | **Notes /10** (barres), calculées par le script Python sur les appareils référencés, au rang `1 + 9 × (nb ≤ v − 1)/(n − 1)` (ex æquo en haut), une décimale : VITESSE (vitesse max. km/h), CHARGE UTILE (masse max. − masse à vide), COÛT (M$, **couleur inversée** : cher = rouge). AGILITÉ = colonne **Maniabilité (0 à 10)** telle quelle. Valeur absente → N/A. | Même logique que les barres du wiki d'armes. |
| A15 | **Masse à vide manquante** (OA-27, RAH-72, F-22E) : l'export fournit `masse_parts` (somme des `UnitPart.mass` du prefab). Le script l'utilise seulement si, sur les appareils qui ont les deux valeurs, `masse_parts` reste à ±15 % de la masse à vide pour au moins 80 % d'entre eux. Sinon : N/A, et charge utile N/A. Le choix est écrit dans la colonne Notes du classeur. | Valeur fiable ou rien. |
| A16 | **Classeur dédié** `data/Avions_wiki.xlsx`, régénéré par script depuis `NO-stats.xlsx` + l'export. Les colonnes **éditables** (Rôle(s), Description (FR), Texte perso, Nom (FR) des emplacements) sont **conservées par clé** à chaque régénération ; elles sont surlignées en jaune clair. La feuille **« Corrections »** est conservée **intégralement**, ligne à ligne. Toute autre cellule est régénérée : une stat corrigée à la main ailleurs serait écrasée (le Lisez-moi le dit en tête). | `NO-stats.xlsx` peut être remplacé par Florian ; ses ajouts ne doivent jamais se perdre. |
| A17 | **Export diagnostic** des appareils : nouvelle entrée F1 « Diagnostic → Exporter la liste des appareils » (défaut non), qui écrit deux CSV à l'ouverture du hangar (format 7.2). | Seule source des emports, de l'IR et des clés. |
| A18 | Données appareils dans `aircraft.json`, **ressource intégrée** `NuclearOptionWiki.aircraft.json`, surchargeable par un `aircraft.json` posé à côté de la DLL (même mécanisme que `weapons.json`). Échec de lecture → fiche appareil désactivée, fiche arme intacte. | A1 ; une seule DLL. |
| A20 | **Corrections manuelles** : feuille « Corrections » du classeur, une ligne par correction : **Clé appareil** (liste déroulante des clés) · **Emplacement (jeu)** (vide pour un champ de l'appareil ; sinon nom anglais exact de l'emplacement, liste déroulante) · **Champ** (liste déroulante fermée, section 7.7) · **Valeur forcée** · **Commentaire** (libre). Effets : 1) la valeur forcée remplace la valeur de NO-stats, de l'export **et** la valeur lue en jeu ; 2) les **notes et classes sont recalculées avec les valeurs corrigées** (par le script pour les notes /10, la taille, la note RCS et les seuils ; en jeu pour la note IR, qui utilise l'IR corrigé quand il existe) ; 3) sur la fiche, une valeur forcée est suivie d'un **astérisque** « * » ; 4) une ligne invalide (clé inconnue, champ hors liste, valeur non numérique pour un champ numérique) **n'est pas appliquée** : le script l'affiche en avertissement, et la colonne « État » de la feuille indique « ignorée : <raison> » ou « appliquée ». | Décision du 2026-10-12 (option 2) : garder la lecture en jeu tout en laissant Florian corriger n'importe quel chiffre. |
| A21 | **Voyant VISEURS IR** : nombre de viseurs **intégrés** à l'appareil = composants `TargetDetector` de type exact `TargetDetector` (radars exclus) trouvés sur le prefab de l'appareil (`GetComponentsInChildren<TargetDetector>(true)`), **sans les nacelles** (qui sont des emports, absentes du prefab). Valeur réelle sous le nombre : portée max. et grossissement max. des viseurs (« 8 km · ×12 »). Couleur neutre, pas de note. 0 viseur → « 0 » (pas N/A). Lu en jeu, exporté (7.2), corrigeable (7.7). | Demande de Florian (2026-10-12). |
| A19 | Tout nouveau patch est un **Postfix** dans un try/catch qui journalise ; une méthode absente désactive seulement ce patch. L'événement `OnSelectedAircraftChange` est abonné dans `WikiController.Attach` et désabonné quand le menu est détruit ou change. | Robustesse (D-plan armes 7.5). |

## 4. Laissé libre à l'exécutant

- Noms internes des classes, tant que la séparation Core / Plugin est respectée.
- La factorisation entre la fiche arme et la fiche appareil (classe de base, ou deux modèles distincts partageant les widgets).
- La construction exacte des rangées d'emport (widgets uGUI), à condition de respecter les cotes de la section 7.3.
- La formulation des descriptions françaises, à condition qu'elles soient fidèles et complètes (pas de résumé).
- L'ordre des colonnes de stats dans la feuille « Avions » du classeur, à condition que les colonnes éditables soient en tête, juste après l'identification.

## 5. Architecture / structure du livrable

```
data/
├─ sources/NO-stats.xlsx            (Florian, lu seulement)
├─ dump/                            exports CSV du mod (armes ET appareils), commités
├─ Avions_wiki.xlsx                 NOUVEAU — classeur consolidé, colonnes éditables jaunes
├─ aircraft.json                    NOUVEAU — GÉNÉRÉ, ne pas éditer
└─ weapons.json                     (inchangé)
tools/
├─ avions_classeur.py               NOUVEAU — NO-stats + export → Avions_wiki.xlsx (conserve les colonnes éditables et la feuille Corrections)
├─ avions_vers_json.py              NOUVEAU — Avions_wiki.xlsx → aircraft.json (applique les corrections, puis notes, tailles, seuils, traductions)
└─ mettre_a_jour.py                 ÉTENDU — enchaîne aussi les deux scripts avions, copie aircraft.json
src/Core/
├─ AircraftRecord.cs, AircraftDatabase.cs   NOUVEAUX — schéma 7.1, recherche A5
├─ AircraftFicheModel.cs            NOUVEAU — champs fixes 7.3
├─ LoadClass.cs                     NOUVEAU — A7 / A9 (tiers log), testable
├─ WeaponCategory.cs                NOUVEAU — règle 7.5, testable sur des drapeaux
└─ (NoteScale, Format : ajouts km/h, kg, kN, kW, m²)
src/Plugin/
├─ Integration/AircraftHooks.cs     NOUVEAU — patch AircraftSelectionButton.Initialize (relais de survol)
├─ Integration/AircraftHoverRelay.cs NOUVEAU — IPointerEnterHandler sur chaque bouton d'appareil
├─ Integration/WikiController.cs    ÉTENDU — état « dernier élément » (appareil | arme), abonnement OnSelectedAircraftChange
├─ GameData/LiveAircraftStats.cs    NOUVEAU — emports, IR, valeurs de repli, classement IR, cache par définition
├─ Diagnostics/AircraftDump.cs      NOUVEAU — export 7.2
├─ UI/WikiPanel.cs                  ÉTENDU — deux contenus (arme, appareil) dans le même cadre
└─ UI/Widgets/HardpointRow.cs, ChipRow.cs   NOUVEAUX
tests/Core.Tests/                   ÉTENDU — fiches appareils, LoadClass, WeaponCategory, RCS
docs/maquettes/fiche-appareil-mfd.html   NOUVEAU — maquette validée par Florian
```

**Flux à l'exécution :**
1. `Plugin.Awake` charge `weapons.json` puis `aircraft.json` (indépendamment) et applique les patchs.
2. Postfix de `AircraftSelectionMenu.Initialize` (déjà présent) : `WikiController.Attach(menu)` abonne `OnSelectedAircraftChange` et pose l'appareil courant (`GetSelectedType()`) comme élément affiché.
3. `OnSelectedAircraftChange(def)` et le survol d'un `AircraftSelectionButton` → `WikiController.ShowAircraft(def)`. Les événements d'arme existants → `ShowWeapon(mount)`. Chacun remplace l'élément affiché (A2).
4. `Refresh` construit `AircraftFicheModel` (Core) à partir du `AircraftRecord` et de `LiveAircraftStats.Get(def)` (mis en cache par définition, aucune allocation par image). Le Core applique l'ordre A4 champ par champ : correction > JSON > valeur lue en jeu, et pose l'astérisque des valeurs forcées.

## 6. Étapes

| # | Étape | Dépend de | Critère d'acceptation (« fini quand… ») |
|---|---|---|---|
| V0 | **Environnement** (2.2) : SDK, ilspycmd, BepInEx dans `lib/`. Demander à Florian les DLL `Managed/` listées à l'étape E0 du plan armes (dont `Assembly-CSharp.dll`). Décompiler et **revérifier chaque ligne du tableau 2.3** (signatures exactes). | — | `dotnet build -c Release` de la v0.4.1 réussit ; tableau 2.3 confirmé ou écarts signalés à Florian **avant** de continuer. |
| V1 | **Export** `AircraftDump` (A17, format 7.2) + entrée F1. Livrer une **v0.5.0-export** (DLL) à Florian, qui ouvre le hangar avec **tous** ses mods d'avions, puis renvoie les deux CSV. | V0 | Les deux CSV sont déposés dans `data/dump/` ; ils contiennent les 24 appareils référencés (OVNI à part) avec ≥ 1 emplacement pour chaque appareil armé. ⚠️ Bloquant côté Florian : avancer V3, V4 et V5 en attendant. |
| V2 | **Classeur** `tools/avions_classeur.py` → `data/Avions_wiki.xlsx` (format 7.4) : fusion NO-stats + export, clé `jsonKey` (correspondance par `unitName` = colonne « Appareil »), OVNI exclu, A15, feuille « Emports », conservation des colonnes éditables et de la feuille « Corrections » (A16, A20), listes déroulantes de cette feuille (clés, emplacements, champs 7.7). | V1 | 24 lignes « Avions », 0 appareil de NO-stats sans clé ; deux exécutions donnent un classeur au contenu identique ; une valeur saisie dans « Texte perso » **et** une ligne de « Corrections » survivent à la régénération (test automatisé). |
| V3 | **Rédaction** (Claude) dans le classeur : Description (FR) des 24 appareils, Rôle(s) (vocabulaire 7.4, vide si incertain), Nom (FR) de chaque emplacement (glossaire 7.6). Lister à Florian les rôles laissés vides. | V2 (V3 peut commencer sur NO-stats avant V1, puis être recopiée) | 24 descriptions FR complètes ; aucun rôle hors vocabulaire ; tous les noms d'emplacement traduits ou signalés. |
| V4 | **JSON** `tools/avions_vers_json.py` → `data/aircraft.json` (schéma 7.1) : **corrections A20 appliquées en premier**, puis notes A14, taille A7, seuils A7/A9, RCS A12, traductions. Colonne « État » de la feuille Corrections remplie. Extension de `mettre_a_jour.py` et du `.bat`. | V2 | Contrôles chiffrés : CI-22 taille S, note RCS 4, agilité 6,5 ; MC-260 taille H ; F-117A RCS 0 ; vitesse du FS-41 = 10,0 ; sortie identique sur deux exécutions. **Test des corrections** : forcer `vitesse_kmh` du CI-22 à 3000 → sa note vitesse devient 10,0, celle du FS-41 passe à 9,6 ou moins, et `corriges` contient `vitesse_kmh` ; une ligne au champ inconnu est marquée « ignorée ». |
| V5 | **Maquette** `docs/maquettes/fiche-appareil-mfd.html` (cotes 7.3, 3 thèmes, données réelles de **CI-22**, **SAH-46** et **MC-260**, ce dernier pour 9 emplacements), publiée en artefact ; captures Playwright. **Validation de Florian.** | V4 (données provisoires de NO-stats acceptées avant V1) | Florian valide la mise en page ; ses retours sont intégrés dans 7.3 (amendement noté dans l'état). |
| V6 | **Core** : `AircraftRecord`, `AircraftDatabase`, `AircraftFicheModel`, `LoadClass`, `WeaponCategory`, ajouts `Format`/`NoteScale`. **Tests** : construction des 24 fiches + 1 inconnue, même nombre de champs dans le même ordre, cas A7/A9/A12/7.5, formats FR, **priorité A4** (correction > JSON > en jeu) et astérisque sur un champ forcé, emport forcé (points, soute, charge, catégories, masqué). | V4, V5 | `dotnet run --project tests/Core.Tests` → OK, code 0 ; les anciens tests (armes) passent toujours. |
| V7 | **Plugin données** : chargement de `aircraft.json` (A18), `LiveAircraftStats` (emports, IR via `GetIRMin`/`GetIRMax` et réflexion pour `DuctedFan`, valeurs de repli, classement IR A11 calculé sur l'IR **corrigé** quand une correction existe, cache). | V6 | Build sans avertissement ; toute lecture est sous try/catch ; journal `[Wiki] chargé v0.5.0 : N armes, M appareils, P patch(s)`. |
| V8 | **UI** : contenu appareil dans `WikiPanel` (cotes 7.3), widgets `HardpointRow` et `ChipRow`, 3 thèmes, bascule arme/appareil sans reconstruire le Canvas. Réglage « Fiche de démonstration » : affiche aussi le CI-22. | V6, V5 | Build OK ; aucun élément au-delà de y = 798 ; maquette et code concordent (relecture des cotes). |
| V9 | **Intégration** : abonnement à `OnSelectedAircraftChange`, appareil courant au Postfix d'`Initialize`, relais de survol sur `AircraftSelectionButton` (patch Postfix de `Initialize`), logique « dernier élément » (A2). Livrer **v0.5.0** (`tools/package.sh`, `release/`). | V7, V8 | Recette RA1–RA9 (section 8) de Florian OK, et R1–R12 du wiki d'armes sans régression. |
| V10 | **Docs** : README (fiche appareil, classeur, colonnes à remplir, export appareils), état du projet, `Lisez-moi` du classeur. | V9 | README relu par Florian. |

Ordre : V0 → V1 → V2 → (V3 ∥ V4) → V5 → V6 → (V7 ∥ V8) → V9 → V10. Pendant l'attente de l'export (V1), avancer V3 sur NO-stats, et V4, V5, V6 sur les données NO-stats seules (emports et IR factices, marqués « provisoire »).

## 7. Formats exacts et exemples

### 7.1 `aircraft.json` (schéma 1)

```json
{
  "schema": 1,
  "genere_le": "2026-10-12T14:00:00Z",
  "source": "Avions_wiki.xlsx",
  "seuils": {
    "encombrement_m2": { "min": 74.2, "max": 1390.9, "s_max": 197.2, "m_max": 523.7 },
    "charge_kg":       { "min": 25,   "max": 11000,  "s_max": 190,   "m_max": 1450 }
  },
  "appareils": {
    "CI-22": {
      "nom": "CI-22 Cricket", "code": "CI-22", "source": "Jeu de base",
      "type": "Avion à hélice (turbopropulseur)", "taille": "S", "encombrement_m2": 120.6,
      "longueur_m": 10.4, "envergure_m": 11.6,
      "roles": ["Contre-insurrection", "Appui aérien rapproché"],
      "description_fr": "Conçu pour la contre-insurrection et les conflits de faible intensité, …",
      "texte_perso": null,
      "rang": 0, "cout_m": 11.5, "vitesse_kmh": 510, "decrochage_kmh": 140,
      "maniabilite": 6.5, "g_max": 6.5, "masse_vide_kg": 3850, "masse_max_kg": 4800,
      "charge_utile_kg": 950, "carburant_kg": 500,
      "poussee_kn": null, "puissance_kw": 1500, "decollage_vertical": false,
      "decollage_m": 100, "decollage_ms": 50,
      "radar_m": null, "brouilleur": false, "canons_fixes": null,
      "leurres": 48, "paillettes": null, "blindage": 1, "rcs_m2": 0.5,
      "note_vitesse": 1.4, "note_charge": 1.0, "note_cout": 2.1, "note_rcs": 4,
      "corriges": ["decrochage_kmh"],
      "ir_force": { "ir_30": null, "ir_max": 1.2 },
      "emports_forces": {
        "Inner Wing Pylons": { "points": null, "soute": null, "charge_kg": 250, "categories": null, "nb_armes": null, "masquer": false }
      }
    }
  },
  "emplacements_fr": {
    "CI-22": { "Inner Wing Pylons": "Pylônes d'aile intérieurs", "Ventral Hardpoint": "Point d'emport ventral" }
  },
  "alias": { "CI-22 Cricket": "CI-22" }
}
```

- Clés d'`appareils` = `jsonKey` réels, issus de l'export (« CI-22 » est un **exemple**). Valeur inconnue → `null`. Les notes ci-dessus sont des **exemples de forme**, pas des valeurs de contrôle.
- `alias` : `unitName` → `jsonKey` (repli A5).
- `corriges` : noms des champs de l'appareil dont la valeur vient d'une correction (A20) ; leur valeur est **déjà** la valeur corrigée, et la fiche y ajoute « * ».
- `ir_force` et `emports_forces` : corrections des valeurs lues en jeu (A20) ; `null` = pas de correction pour ce sous-champ. Un emplacement forcé avec `masquer: true` n'est pas affiché. Un emplacement présent dans `emports_forces` mais absent en jeu est ignoré, avec une ligne de journal.
- Les seuils `charge_kg` ci-dessus sont **fictifs** : ils sont calculés à l'étape V4.

### 7.2 Export des appareils (UTF-8, séparateur `;`, `Clean()` comme `WeaponDump`)

Fichiers `BepInEx/config/NuclearOptionWiki_avions_v<version>.csv` et `NuclearOptionWiki_emports_v<version>.csv`. Appareils = union de `Encyclopedia.i.aircraft` et de `Resources.FindObjectsOfTypeAll<AircraftDefinition>()`, dédoublonnée par `jsonKey`. Lecture sur le prefab : `def.unitPrefab.GetComponentInChildren<Aircraft>(true)`, puis `GetComponentsInChildren<…>(true)`. Une erreur sur un appareil donne une ligne partielle, jamais un échec de l'export.

```
json_key;unit_name;code;evenementiel;encyclopedie;rang;cout;vmax_info;vmax_param;decrochage;maniabilite;g_max;masse_vide;masse_max;masse_parts;longueur;largeur;hauteur;rcs;blindage;vertical;decollage_ms;decollage_m;moteurs;ir_min;ir_max_sec;ir_max;viseurs;viseur_portee;viseur_grossissement;eots;nb_emplacements;nb_points;nb_soutes;canons_fixes;role_surface;role_air;role_missile;role_radar;description
CI-22;CI-22 Cricket;CI-22;0;1;0;11.5;510;…;140;6.5;6.5;3850;4800;3790;10.4;11.6;3.4;0.5;1;0;50;100;TurbineEngine;0.2;1.4;1.4;1;8000;12;1;5;9;0;1;0.56;0.44;0;0;Designed for counter insurgency…
```

```
json_key;set_index;set_name;points;soute;symetrie;exclut;integres;mount_jsonkey;mount_name;info_name;masse;munitions;categorie
CI-22;3;Inner Wing Pylons;2;0;;4;0;AGR18_x7;AGR-18 Lynchpin x7;AGR18_Info;180;7;ROQUETTE
```

- `moteurs` : types de composants moteurs trouvés, séparés par `|`. `ir_min`, `ir_max_sec` (plein gaz sans postcombustion), `ir_max` (avec) : max des moteurs, règle A11 ; l'IR à 30 % est calculé par le script et en jeu, pas exporté. `viseurs` : nombre de viseurs (A21) ; `viseur_portee` (m) et `viseur_grossissement` : max. sur ces viseurs ; `eots` = 1 si `Aircraft.EOTS` est renseigné. `soute` = 1 si au moins un `Hardpoint.bayDoors` est non vide. `exclut` : indices de `precludingHardpointSets`, séparés par `|`. `integres` : nombre de `BuiltInWeapons` + `BuiltInTurrets` de l'emplacement. Un emplacement sans option a une ligne avec les colonnes `mount_*` vides. `categorie` : règle 7.5.
- Les valeurs des lignes d'exemple sont **illustratives**.

### 7.3 Fiche appareil — champs fixes et cotes (px, 1920×1080, panneau 600 × 840, marge 14)

Pas de séparateur horizontal (gain de place) : les zones sont séparées par leur titre. Pied de fiche à y = 798 (inchangé).

| y de fin | Élément | Contenu |
|---|---|---|
| 28 | Titre de zone « APPAREIL » | 13 px, comme les armes |
| 72 | Nom (30 px) + 3 puces | `nom` ; puces : type court (7.8), source (« JEU DE BASE » ou nom du mod), **taille** « S » / « M » / « H » |
| 98 | Ligne RÔLES | pastilles des `roles` ; vide → « N/A » |
| 210 | 4 barres /10 (`Bar10`) | VITESSE (« 1 060 km/h ») · AGILITÉ (`maniabilite`, valeur « 9 G ») · CHARGE UTILE (« 5 600 kg ») · COÛT (**inversée**, « 21,8 M$ ») |
| 358 | Grille 3 × 3 (`StatCell`, 40 px, écart 6) | RANG REQUIS · MASSE VIDE / MAX (« 3,9 / 4,8 t ») · CARBURANT (« 500 kg ») ; MOTEUR (« 63 kN » ou « 1 500 kW ») · DÉCROCHAGE (« 140 km/h ») · DÉCOLLAGE (« VERTICAL » ou « 100 m · 50 m/s ») ; RADAR (« 35 km » ou « NON ») · BROUILLEUR (« OUI » / « NON ») · CANONS FIXES (« 2 » ou « NON ») |
| 386 | Titre « EMPORTS » | |
| 409 | Ligne de synthèse (17 px) | « 5 emplacements · 9 points · 1 soute · charge max 2 400 kg » |
| 589 | **9 rangées d'emplacement** (20 px chacune) | voir ci-dessous ; au-delà de 9 emplacements, la 9ᵉ affiche « +n emplacements » ; rangées non utilisées vides |
| 625 | Titre « SIGNATURES · DÉFENSES » | |
| 689 | 7 tuiles (64 px, écart 6, largeur (572 − 6 × 6) / 7 ≈ 76 px ; libellés et valeurs réelles en auto-size 9–11 px) | SIGN. RADAR (note /5 **inversée** + « 0,5 m² ») · IR 30 % GAZ (note /5 **inversée** + valeur « 0,56 ») · IR PLEIN GAZ (note /5 **inversée** + valeur « 1,4 ») · LEURRES IR (nombre, couleur neutre) · PAILLETTES (nombre, couleur neutre) · BLINDAGE (palier, couleur neutre) · VISEURS IR (nombre, couleur neutre, valeur « 8 km · ×12 ») |
| 725 | Texte perso | rectangle 572 × 28, auto-size 10–12 px, couleur ambre `#FFC24A` ; vide → « — » gris |
| 785 | **Description FR, tout en bas de la fiche** | rectangle 572 × 56 (y 729 → 785), TMP auto-size 10–12 px, « … » si trop long ; vide → description anglaise du jeu |

**Rangée d'emplacement** (largeur 572) : nom FR (190, tronqué « … ») · « ×2 » points (34) · puce SOUTE ou EXT (52) · puce classe S/M/H (24) · charge « 900 kg » (62) · pastilles de catégories (reste, 3 max + « +n ») · « (12) » armes compatibles, aligné à droite. Un emplacement qui en exclut d'autres porte un « ⊘ » devant son nom.

**Couleur neutre** (compteurs) : couleur d'accent du thème (vert phosphore en A), sans échelle de note, avec l'unité vide.

La maquette (V5) fait foi pour tout détail non coté ici, une fois validée.

### 7.4 Classeur `data/Avions_wiki.xlsx`

**Feuille « Avions »** (une ligne par appareil référencé, triée par rang puis coût) :
- Identification : Clé (`jsonKey`) · Appareil · Code · Source · Type.
- **Éditables (jaune)** : Rôle(s) · Description (FR) · Texte perso.
- Calculées : Taille · Encombrement (m²) · Note vitesse · Note charge · Note coût · Note RCS.
- Stats : toutes les colonnes utiles de NO-stats, plus Charge utile, Viseurs IR (nombre, portée, grossissement), IR 30 % gaz, IR plein gaz (et IR ralenti, IR plein gaz sans postcombustion pour contrôle), Nb emplacements, Nb points, Nb soutes, Description (jeu, EN), Notes.

**Feuille « Emports »** (une ligne par appareil × emplacement) : Clé · Appareil · Index · Nom (jeu) · **Nom (FR) (jaune)** · Points · Soute · Exclut (noms) · Charge max / point (kg) · Classe · Nb armes compatibles · Catégories · Armes compatibles (`mountName`, séparés par « , »).

**Feuille « Corrections »** (conservée intégralement, A20) : Clé appareil · Emplacement (jeu) · Champ · Valeur forcée · Commentaire · État (rempli par le script). Listes déroulantes sur les trois premières colonnes.

**Feuille « Lisez-moi »** : en tête, l'avertissement « seules les colonnes jaunes et la feuille Corrections sont conservées ; pour corriger une stat, utilise la feuille Corrections » ; puis origine des données, règles A7, A9, A11, A12, A14, A15, A20, colonnes à remplir, procédure de mise à jour.

**Vocabulaire fermé des rôles** (A8) : Chasseur · Intercepteur · Supériorité aérienne · Multirôle · Attaque au sol · Appui aérien rapproché · Contre-insurrection · Bombardier · Bombardier furtif · Frappe furtive · Guerre électronique · SEAD · Hélicoptère d'attaque · Hélicoptère utilitaire · Transport · Entraînement · Reconnaissance · Lutte antinavire · Embarqué (porte-avions). Rôles séparés par « · », 3 au plus.

### 7.5 Catégories de compatibilité (C# `WeaponCategory`, appliquée à chaque option, dans cet ordre)

| Condition sur l'option (`WeaponMount` m, `WeaponInfo` i) | Catégorie |
|---|---|
| `m.Cargo` ou `m.Troops` ou `i.cargo` ou `i.troops` ou `i.sling` | CARGO |
| `i.jammer` | BROUILLEUR |
| `i.energy` | LASER |
| `i.gun` | CANON |
| `i.bomb` ou `i.glideBomb` | BOMBE |
| `i.missile` et l'arme n'a pas d'autodirecteur (`MissileSeeker` absent du prefab) et n'est pas guidée laser | ROQUETTE |
| `i.missile` et `i.effectiveness.antiAir > i.effectiveness.antiSurface` | MISSILE AA |
| `i.missile` | MISSILE AS |
| sinon (réservoir, radar, crochet, leurres, nacelle…) | aucune (non affichée) |

En plus : `i.nuclear` ajoute la pastille **NUC**. Ordre d'affichage : CANON, MISSILE AA, MISSILE AS, ROQUETTE, BOMBE, NUC, LASER, BROUILLEUR, CARGO.

### 7.6 Glossaire des emplacements (point de départ, à compléter avec l'export)

| Jeu | FR |
|---|---|
| Internal guns | Canons internes |
| Internal Bay / Weapon Bay | Soute interne |
| Ventral Hardpoint | Point d'emport ventral |
| Fuselage Pylons | Pylônes de fuselage |
| Inner Wing Pylons | Pylônes d'aile intérieurs |
| Outer Wing Pylons | Pylônes d'aile extérieurs |
| Wingtip(s) | Saumons d'aile |
| Chin Turret | Tourelle de nez |
| Cargo Bay / Cargo | Soute cargo |
| Sling | Élingue |

### 7.7 Champs corrigeables (liste fermée de la colonne « Champ »)

Libellé affiché dans la liste déroulante → champ JSON. Type N = nombre, T = texte, B = OUI/NON, L = liste de catégories 7.5 séparées par « , ».

**Appareil** (colonne Emplacement vide) : Coût (M$) → `cout_m` N · Rang requis → `rang` N · Vitesse max. (km/h) → `vitesse_kmh` N · Décrochage (km/h) → `decrochage_kmh` N · Maniabilité (0 à 10) → `maniabilite` N · Facteur G max → `g_max` N · Masse à vide (kg) → `masse_vide_kg` N · Masse max. (kg) → `masse_max_kg` N · Charge utile (kg) → `charge_utile_kg` N · Carburant (kg) → `carburant_kg` N · Poussée (kN) → `poussee_kn` N · Puissance (kW) → `puissance_kw` N · Décollage vertical → `decollage_vertical` B · Distance de décollage (m) → `decollage_m` N · Vitesse de décollage (m/s) → `decollage_ms` N · Portée radar (m) → `radar_m` N · Brouilleur → `brouilleur` B · Canons fixes → `canons_fixes` N · Leurres IR → `leurres` N · Paillettes → `paillettes` N · Palier de blindage → `blindage` N · Signature radar (m²) → `rcs_m2` N · Viseurs IR → `viseurs` N · IR à 30 % de gaz → `ir_force.ir_30` N · IR plein gaz → `ir_force.ir_max` N · Taille (S/M/H) → `taille` T · Type → `type` T · Longueur (m) → `longueur_m` N · Envergure (m) → `envergure_m` N.

**Emplacement** (colonne Emplacement renseignée) : Points → `points` N · Soute → `soute` B · Charge max / point (kg) → `charge_kg` N (la classe S/M/H suit) · Catégories → `categories` L · Nb armes compatibles → `nb_armes` N · Masquer l'emplacement → `masquer` B.

Corriger Longueur ou Envergure fait recalculer l'encombrement et la taille, sauf si Taille est aussi forcée. Corriger un champ qui entre dans une note (vitesse, coût, charge utile, masses, RCS) fait recalculer cette note et le classement de **tous** les appareils.

### 7.8 Type court (puce)

Avion à hélice → « HÉLICE » ; Hélicoptère → « HÉLICO » ; Avion à réaction → « RÉACTION » ; Avion à décollage vertical → « VTOL » ; Convertible à rotors basculants → « CONVERTIBLE » ; ailes à géométrie variable → « GÉOM. VARIABLE ». Correspondance par début de la colonne Type ; inconnu → le texte en capitales, tronqué à 14 caractères.

## 8. Pièges connus et risques

| Risque / piège | Parade |
|---|---|
| `AssignAircraft` lève des `DropdownChanged` d'arme pendant le changement d'appareil : la fiche arme écraserait la fiche appareil. | `OnSelectedAircraftChange` est levé **après** : l'appareil gagne naturellement. En plus, ignorer tout `ShowWeapon` reçu dans la même image qu'un `ShowAircraft` (comparer `Time.frameCount`). |
| Premier `OnSelectedAircraftChange` manqué (levé avant le Postfix d'`Initialize`). | Lire `GetSelectedType()` dans le Postfix (2.3). |
| `Initialize` rappelé (changement de base via `GoToNextAirbase`, qui passe par `Refresh` et non par `Initialize`). | S'abonner une seule fois par instance de menu (garder la référence, se désabonner avant de se réabonner). `Refresh` relance `SpawnPreview`, donc l'événement. |
| Lire `weaponManager` / `hardpointSets` sur un **prefab** non instancié. | Champs sérialisés : lisibles sur le prefab. Si `weaponManager` est nul sur le prefab, se rabattre sur `GetComponentInChildren<WeaponManager>(true)`. En dernier recours, à l'ouverture du hangar, lire `menu.previewAircraft` (champ privé) pour l'appareil courant seulement. |
| `DuctedFan.IRMin` / `IRMax` privés renommés. | `AccessTools.Field`, résultat N/A, un seul avertissement par session. |
| Appareil moddé chargé tardivement (absent de l'encyclopédie). | Union encyclopédie + `FindObjectsOfTypeAll<AircraftDefinition>()` (7.2). |
| `NO-stats.xlsx` remplacé par une version aux colonnes renommées. | `avions_classeur.py` cherche les colonnes **par en-tête** et s'arrête avec un message clair si une colonne attendue manque. |
| Colonnes éditables perdues à la régénération. | A16 + test automatisé de V2. |
| Florian corrige une stat directement dans la feuille « Avions » (écrasée à la régénération). | Avertissement en tête du Lisez-moi et en commentaire de cellule sur l'en-tête de la feuille « Avions » ; le script signale les cellules de stats qui diffèrent de leur source avant de les écraser, et propose la ligne de correction équivalente dans son affichage. |
| Correction devenue obsolète (emplacement renommé par une mise à jour du jeu ou d'un mod). | Ligne marquée « ignorée : emplacement inconnu » par le script, et une ligne de journal en jeu. Jamais d'erreur bloquante. |
| Fiche trop dense (9 emplacements + description). | Cotes 7.3 calculées sur le budget de 812 px ; auto-size TMP ; maquette validée (V5) avant le code. |
| Masse à vide incohérente pour les mods. | A15 (seuil de concordance chiffré). |
| Régression de la fiche arme. | Recette R1–R12 rejouée à V9 ; tests Core existants conservés. |
| Police du jeu sans certains caractères (⊘, →). | Remplacement ASCII existant (« (x) », « -> ») si le glyphe manque, comme pour la fiche arme. |

**Recette en jeu (Florian), en plus de R1–R12 :**
- RA1 : à l'ouverture du hangar avec F6 actif, la fiche montre l'appareil courant.
- RA2 : clic sur un autre appareil de la grille → fiche de cet appareil.
- RA3 : survol d'un bouton d'appareil (y compris grisé) → fiche de cet appareil ; survol ou sélection d'une arme ensuite → fiche arme.
- RA4 : changer d'appareil ne laisse jamais la fiche arme affichée à la place de l'appareil.
- RA5b : le nombre de VISEURS IR correspond aux viseurs intégrés connus de l'appareil (nacelles exclues) sur 5 appareils choisis par Florian.
- RA5 : fiches identiques en structure pour un avion à hélice (CI-22), un hélico (SAH-46), un VTOL (VT-7), un furtif moddé (F-117A) et le MC-260 (9 emplacements) ; N/A aux bons endroits.
- RA6 : soutes, classes S/M/H, catégories et nombre d'armes cohérents avec la liste des emplacements du jeu (bas de l'écran).
- RA7 : un texte saisi dans « Texte perso », puis `mettre_a_jour.bat`, apparaît en jeu sans recompiler.
- RA7b : une correction (ex. Vitesse max. du CI-22 = 600, et Charge max / point d'un emplacement), puis `mettre_a_jour.bat` → les valeurs forcées s'affichent avec « * » et la note de vitesse change.
- RA8 : thèmes, échelle et opacité appliqués aussi à la fiche appareil.
- RA9 : FPS inchangés, aucune erreur `[Wiki]` dans le journal.

## 9. Modèle d'exécution

**Opus 5.5, effort medium.** Plusieurs critères le justifient :
- exécution **longue et agentique** (décompilation, build, aller-retour avec les exports et les tests de Florian) ;
- **multi-fichiers** (une quinzaine de fichiers créés ou modifiés, dans du code existant) ;
- accroche à du code de jeu non documenté, sans test en jeu possible ici ;
- rédaction de 24 descriptions françaises et choix des rôles (jugement).

Sonnet 5.5 conviendra au peaufinage (phase 4).

## 10. Hors périmètre

- Fiches navires et véhicules.
- Comparateur côte à côte de deux appareils.
- Indice de furtivité combiné (A12).
- Affichage des indices d'IA (A8).
- Valeurs du chargement en cours (masse totale, P/W, RCS chargé) : le jeu les affiche déjà à droite.
- Édition des textes en jeu ; synchronisation multijoueur ; rendu 3D.
- Icônes de catégories (point F6 du wiki d'armes, inchangé).

## Consigne à l'exécutant

Suivre les étapes dans l'ordre et vérifier chaque critère d'acceptation. En cas d'étape impossible, ambiguë ou contredite par la réalité (méthode du jeu absente, export incomplet, comportement en jeu différent), **s'arrêter et le signaler à Florian plutôt qu'improviser**. Un problème de structure renvoie en phase 2.
