using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using NuclearOptionWiki.Core;
using NuclearOptionWiki.GameData;
using NuclearOptionWiki.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace NuclearOptionWiki.Integration
{
    /// <summary>
    /// État de la fiche : mode actif (touche bascule), élément affiché, visibilité liée à l'écran hangar.
    /// Règle d'affichage (plan avions A2, amendé v3.5) : l'appareil ou l'arme SURVOLÉ par la souris ; quand la souris
    /// ne survole rien, l'appareil SÉLECTIONNÉ dans le hangar. Vit sur son propre GameObject persistant.
    /// </summary>
    internal sealed class WikiController : MonoBehaviour
    {
        private const float WarningSeconds = 4f;

        private static WikiController _instance;
        private static readonly HashSet<string> LoggedUnknown = new HashSet<string>();
        private static readonly HashSet<string> LoggedUnknownAircraft = new HashSet<string>();

        private enum Hover { None, Aircraft, Weapon }

        private AircraftSelectionMenu _menu;
        private bool _active;
        private bool _dirty = true;
        private bool _demo;
        private Hover _hover;
        private AircraftDefinition _hoverAircraft;
        private WeaponMount _hoverMount;
        private Component _hoverSource;      // relais qui a signalé le survol (null : survol signalé par le jeu)
        private object _shown;               // élément actuellement affiché (appareil ou emport)
        private bool _hidden;                // A22 : navire ou véhicule, fiche masquée
        private int _aircraftFrame = -1;
        private int _hoverFrame = -1;
        private float _warningUntil;
        private string _warning = "";

        /// <summary>Appelé à l'ouverture de l'écran hangar (patch de AircraftSelectionMenu.Initialize).</summary>
        public static void Attach(AircraftSelectionMenu menu)
        {
            if (Plugin.Database == null && Plugin.Aircraft == null) return;
            if (_instance == null)
            {
                var go = new GameObject("NuclearOptionWiki.Controller") { hideFlags = HideFlags.HideAndDontSave };
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<WikiController>();
                _instance.SubscribeSettings();
            }
            _instance.OnMenuOpened(menu);
        }

        /// <summary>
        /// Survol (ou sélection dans la liste) d'un emport. Null (option « Empty ») : plus d'arme survolée.
        /// <paramref name="source"/> : relais de l'emplacement, null quand c'est le jeu qui signale le survol.
        /// </summary>
        public static void ShowWeapon(WeaponMount mount, Component source = null)
        {
            if (_instance == null) return;
            if (mount == null || mount.info == null || Plugin.Database == null) { EndHover(source); return; }
            // Piège 8 : AssignAircraft lève des DropdownChanged pendant le changement d'appareil : ignorés.
            if (Time.frameCount == _instance._aircraftFrame) return;
            _instance._hover = Hover.Weapon;
            _instance._hoverFrame = Time.frameCount;
            _instance._hoverMount = mount;
            _instance._hoverAircraft = null;
            _instance._hoverSource = source;
            _instance._demo = false;
        }

        /// <summary>Survol d'un bouton de la grille d'appareils (grisés compris).</summary>
        public static void ShowAircraft(AircraftDefinition def, Component source = null)
        {
            if (_instance == null || def == null || Plugin.Aircraft == null) return;
            _instance._hover = Hover.Aircraft;
            _instance._hoverAircraft = def;
            _instance._hoverMount = null;
            _instance._hoverSource = source;
            _instance._demo = false;
        }

        /// <summary>La souris quitte un élément : retour à l'appareil sélectionné (sauf si un autre élément a pris le relais).</summary>
        public static void EndHover(Component source)
        {
            if (_instance == null || _instance._hover == Hover.None) return;
            if (source != null && _instance._hoverSource != null && _instance._hoverSource != source) return;
            _instance._hover = Hover.None;
            _instance._hoverAircraft = null;
            _instance._hoverMount = null;
            _instance._hoverSource = null;
        }

        /// <summary>
        /// A24 (amendé v3.5) : l'éditeur de tags vise l'appareil (ou le véhicule) SÉLECTIONNÉ dans le hangar,
        /// pas celui survolé par la souris : la sélection ne bouge pas quand on parcourt la grille.
        /// </summary>
        private static AircraftDefinition SelectedAircraft
        {
            get
            {
                try { return _instance?._menu != null ? _instance._menu.GetSelectedType() : null; }
                catch (Exception) { return null; }
            }
        }

        public static string CurrentAircraftKey => SelectedAircraft == null ? null : ResolveKey(SelectedAircraft);
        public static string CurrentAircraftName => SelectedAircraft?.unitName;

        public static List<string> CurrentAircraftSheetTags()
        {
            var def = SelectedAircraft;
            if (def == null || Plugin.Aircraft == null) return new List<string>();
            return Plugin.Aircraft.Find(def.jsonKey, def.unitName).Value?.Tags ?? new List<string>();
        }

        public static void RefreshAircraft()
        {
            if (_instance != null) _instance._dirty = true;
        }

        private static string ResolveKey(AircraftDefinition def)
        {
            var found = Plugin.Aircraft?.Find(def.jsonKey, def.unitName).Key;
            return found ?? def.jsonKey;
        }

        private void OnMenuOpened(AircraftSelectionMenu menu)
        {
            var s = Plugin.Settings;
            // Un seul abonnement par menu (piège : Initialize peut être rappelé ; Refresh relance l'événement de lui-même).
            if (_menu != null) _menu.OnSelectedAircraftChange -= OnAircraftChanged;
            _menu = menu;
            _menu.OnSelectedAircraftChange -= OnAircraftChanged;
            _menu.OnSelectedAircraftChange += OnAircraftChanged;
            EndHover(null);
            _shown = null;
            _hidden = false;
            _demo = s.DemoSheet.Value;
            _active = s.ActiveOnOpen.Value || _demo;
            _dirty = true;

            var panel = WikiPanel.Instance;
            panel.Configure(s.Theme.Value, FindGameFont(menu));
            panel.SetScale(s.Scale.Value);
            panel.SetOpacity(s.BackgroundOpacity.Value);
            UpdateFooter();
            panel.SetVisible(false);
        }

        /// <summary>Changement d'appareil sélectionné : la fiche le suit dès que la souris ne survole rien.</summary>
        private static void OnAircraftChanged(AircraftDefinition def)
        {
            if (_instance == null) return;
            _instance._aircraftFrame = Time.frameCount;
            // Piège 8 : AssignAircraft (juste avant cet événement) lève des DropdownChanged qui ne sont pas des survols.
            if (_instance._hover == Hover.Weapon && _instance._hoverSource == null && _instance._hoverFrame == Time.frameCount) EndHover(null);
            _instance._dirty = true;
        }

        private void OnDestroy()
        {
            if (_menu != null) _menu.OnSelectedAircraftChange -= OnAircraftChanged;
        }

        /// <summary>Élément à afficher : survolé, sinon fiche de démonstration, sinon appareil sélectionné.</summary>
        private object Target()
        {
            if (_hover == Hover.Weapon && _hoverMount != null) return _hoverMount;
            if (_hover == Hover.Aircraft && _hoverAircraft != null) return _hoverAircraft;
            if (_demo && Plugin.Aircraft != null)
            {
                var demo = FindDefinition("COIN");
                if (demo != null) return demo;
            }
            return Plugin.Aircraft != null ? SelectedAircraft : null;
        }

        private void Update()
        {
            var panel = WikiPanel.Instance;
            if (_menu == null || !_menu.isActiveAndEnabled)
            {
                if (panel.Visible) panel.SetVisible(false);
                if (_menu == null) { EndHover(null); _shown = null; } // menu détruit : on repart de zéro à la prochaine ouverture
                return;
            }

            if (Plugin.Settings.ToggleKey.Value.IsDown() && !IsTypingInTextField())
                _active = !_active;

            if (_warningUntil > 0f && Time.unscaledTime > _warningUntil)
            {
                _warningUntil = 0f;
                _warning = "";
                UpdateFooter();
            }

            // Relais détruit (objet Unity nul mais référence C# conservée) ou masqué, sans événement de sortie : fin du survol.
            bool sourceDestroyed = !ReferenceEquals(_hoverSource, null) && _hoverSource == null;
            if (sourceDestroyed || (_hoverSource != null && !_hoverSource.gameObject.activeInHierarchy)) EndHover(null);
            // Souris hors de toute interface (vue 3D du hangar) : plus rien n'est survolé, même sans événement de sortie.
            else if (_hover != Hover.None && EventSystem.current != null && !EventSystem.current.IsPointerOverGameObject()) EndHover(null);

            var target = Target();
            if (!ReferenceEquals(target, _shown)) _dirty = true;
            if (_dirty)
                _hidden = target is AircraftDefinition def && !LiveAircraftStats.IsAircraft(def);

            panel.SetVisible(_active && !_hidden && target != null);
            if (_active && _dirty) Refresh(target);
        }

        private void Refresh(object target)
        {
            _dirty = false;
            _shown = target;
            try
            {
                if (target is AircraftDefinition def)
                {
                    if (!_hidden) WikiPanel.Instance.ShowAircraft(BuildAircraft(def));
                    return;
                }
                if (Plugin.Database == null) return;
                var mode = Plugin.Settings.RangeNote.Value == RangeScale.Rang ? RangeNoteMode.Rank : RangeNoteMode.Logarithmic;
                FicheModel model;
                if (target is WeaponMount mount)
                {
                    var info = mount.info;
                    var record = Plugin.Database.Find(info.name, mount.jsonKey, info.weaponName);
                    if (record == null) LogUnknownOnce(info, mount);
                    model = FicheModel.Build(record, info.weaponName, LiveWeaponStats.Get(info), mode);
                }
                else if (_demo)
                {
                    model = FicheModel.Build(Plugin.Database.Find(null, null, "AGM-48"), "AGM-48", LiveStats.None, mode);
                }
                else
                {
                    model = FicheModel.Build(null, "Wiki armes", LiveStats.None, mode);
                    model.Unreferenced = false;
                    model.Chip1 = "";
                    model.Subtitle = "Survolez ou sélectionnez une arme";
                }
                WikiPanel.Instance.Show(model);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"[Wiki] Erreur d'affichage de la fiche : {e}");
            }
        }

        private static AircraftFicheModel BuildAircraft(AircraftDefinition def)
        {
            var db = Plugin.Aircraft;
            bool excluded = AircraftDatabase.IsExcluded(def.jsonKey, GameData.AircraftReader.IsEventContent(def));
            var found = excluded ? new KeyValuePair<string, AircraftRecord>(null, null) : db.Find(def.jsonKey, def.unitName);
            if (found.Value == null && LoggedUnknownAircraft.Add(def.jsonKey ?? def.name))
                Plugin.Log.LogInfo($"[Wiki] Appareil non référencé dans aircraft.json : jsonKey={def.jsonKey}, nom={def.unitName}");
            var live = LiveAircraftStats.Get(def);
            live.TagsOverride = Plugin.Tags.Get(found.Key ?? def.jsonKey);
            return AircraftFicheModel.Build(db, found.Value, found.Key, live);
        }

        private static AircraftDefinition FindDefinition(string jsonKey)
        {
            try
            {
                var list = Encyclopedia.i?.aircraft;
                if (list != null) foreach (var d in list) if (d != null && d.jsonKey == jsonKey) return d;
            }
            catch (Exception e) { Plugin.Log.LogDebug($"[Wiki] Encyclopédie illisible : {e.Message}"); }
            return null;
        }

        private void SubscribeSettings()
        {
            var s = Plugin.Settings;
            s.Theme.SettingChanged += (_, __) =>
            {
                var panel = WikiPanel.Instance;
                panel.Configure(s.Theme.Value, FindGameFont(_menu));
                panel.SetOpacity(s.BackgroundOpacity.Value);
                _dirty = true;
            };
            s.Scale.SettingChanged += (_, __) => WikiPanel.Instance.SetScale(s.Scale.Value);
            s.BackgroundOpacity.SettingChanged += (_, __) => WikiPanel.Instance.SetOpacity(s.BackgroundOpacity.Value);
            s.RangeNote.SettingChanged += (_, __) => _dirty = true;
            s.ToggleKey.SettingChanged += (_, __) => OnToggleKeyChanged();
        }

        /// <summary>Décision D23 : nouvelle touche appliquée à chaud, avertissement si elle est déjà prise.</summary>
        private void OnToggleKeyChanged()
        {
            var key = Plugin.Settings.ToggleKey.Value;
            string reservedBy = KeyNames.ReservedBy(key);
            if (key.MainKey == KeyCode.None)
            {
                _warning = "Attention : aucune touche définie";
                Plugin.Log.LogWarning("[Wiki] Touche bascule vide : la fiche ne peut plus être affichée au clavier.");
            }
            else if (reservedBy != null)
            {
                _warning = $"Attention : {KeyNames.Describe(key)} déjà utilisée ({reservedBy})";
                Plugin.Log.LogWarning($"[Wiki] La touche {KeyNames.Describe(key)} est déjà utilisée par {reservedBy}.");
            }
            else
            {
                _warning = "";
            }
            _warningUntil = _warning.Length > 0 ? Time.unscaledTime + WarningSeconds : 0f;
            UpdateFooter();
        }

        private void UpdateFooter()
        {
            var key = Plugin.Settings.ToggleKey.Value;
            string keyText = key.MainKey == KeyCode.None ? "" : KeyNames.Describe(key) + " · masquer";
            WikiPanel.Instance.SetFooter(keyText, _warning);
        }

        private static bool IsTypingInTextField()
        {
            var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (selected == null) return false;
            var tmpInput = selected.GetComponent<TMP_InputField>();
            if (tmpInput != null && tmpInput.isFocused) return true;
            var input = selected.GetComponent<UnityEngine.UI.InputField>();
            return input != null && input.isFocused;
        }

        private static TMP_FontAsset FindGameFont(AircraftSelectionMenu menu)
        {
            if (menu == null) return null;
            return menu.GetComponentsInChildren<TMP_Text>(true).Select(t => t.font).FirstOrDefault(f => f != null);
        }

        private static void LogUnknownOnce(WeaponInfo info, WeaponMount mount)
        {
            string key = info.name + "|" + mount.jsonKey;
            if (!LoggedUnknown.Add(key)) return;
            Plugin.Log.LogInfo($"[Wiki] Arme non référencée dans weapons.json : info={info.name}, emport={mount.jsonKey}, nom={info.weaponName}");
        }
    }

    /// <summary>Noms lisibles des touches et touches déjà prises par le modpack (décision D23).</summary>
    internal static class KeyNames
    {
        private static readonly Dictionary<KeyCode, string> Reserved = new Dictionary<KeyCode, string>
        {
            { KeyCode.F1, "Configuration Manager" },
            { KeyCode.F7, "un autre mod" },
            { KeyCode.F8, "un autre mod" },
            { KeyCode.F9, "un autre mod" },
            { KeyCode.Home, "un autre mod" },
            { KeyCode.End, "un autre mod" },
            { KeyCode.Keypad5, "un autre mod" },
        };

        public static string ReservedBy(KeyboardShortcut key)
        {
            var mods = key.Modifiers.ToList();
            if (mods.Count == 0 && Reserved.TryGetValue(key.MainKey, out var by)) return by;
            if (key.MainKey == KeyCode.Q && mods.Count == 1 && (mods[0] == KeyCode.LeftShift || mods[0] == KeyCode.RightShift)) return "un autre mod";
            return null;
        }

        /// <summary>« Ctrl + W », « F6 », « Pavé 5 »…</summary>
        public static string Describe(KeyboardShortcut key)
        {
            var parts = key.Modifiers.Select(Name).ToList();
            parts.Add(Name(key.MainKey));
            return string.Join(" + ", parts);
        }

        private static string Name(KeyCode k)
        {
            switch (k)
            {
                case KeyCode.LeftControl: case KeyCode.RightControl: return "Ctrl";
                case KeyCode.LeftShift: case KeyCode.RightShift: return "Shift";
                case KeyCode.LeftAlt: case KeyCode.RightAlt: return "Alt";
                case KeyCode.Return: return "Entrée";
                case KeyCode.Space: return "Espace";
                case KeyCode.Tab: return "Tab";
            }
            string s = k.ToString();
            if (s.StartsWith("Alpha") && s.Length == 6) return s.Substring(5);
            if (s.StartsWith("Keypad")) return "Pavé " + s.Substring(6);
            return s;
        }
    }
}
