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
    /// État de la fiche : mode actif (touche bascule), arme affichée (dernière survolée ou sélectionnée,
    /// affichage « collant »), visibilité liée à l'écran hangar. Vit sur son propre GameObject persistant.
    /// </summary>
    internal sealed class WikiController : MonoBehaviour
    {
        private const float WarningSeconds = 4f;

        private static WikiController _instance;
        private static readonly HashSet<string> LoggedUnknown = new HashSet<string>();

        private AircraftSelectionMenu _menu;
        private bool _active;
        private bool _dirty = true;
        private bool _demo;
        private WeaponMount _mount;
        private float _warningUntil;
        private string _warning = "";

        /// <summary>Appelé à l'ouverture de l'écran hangar (patch de AircraftSelectionMenu.Initialize).</summary>
        public static void Attach(AircraftSelectionMenu menu)
        {
            if (Plugin.Database == null) return;
            if (_instance == null)
            {
                var go = new GameObject("NuclearOptionWiki.Controller") { hideFlags = HideFlags.HideAndDontSave };
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<WikiController>();
                _instance.SubscribeSettings();
            }
            _instance.OnMenuOpened(menu);
        }

        /// <summary>Survol ou sélection d'un emport. Null (« Empty », emplacement vide) : rien ne change.</summary>
        public static void ShowWeapon(WeaponMount mount)
        {
            if (_instance == null || mount == null || mount.info == null) return;
            if (_instance._mount == mount && !_instance._demo) return;
            _instance._mount = mount;
            _instance._demo = false;
            _instance._dirty = true;
        }

        private void OnMenuOpened(AircraftSelectionMenu menu)
        {
            var s = Plugin.Settings;
            _menu = menu;
            _mount = null;
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

        private void Update()
        {
            var panel = WikiPanel.Instance;
            if (_menu == null || !_menu.isActiveAndEnabled)
            {
                if (panel.Visible) panel.SetVisible(false);
                if (_menu == null) _mount = null; // menu détruit : on repart de zéro à la prochaine ouverture
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

            panel.SetVisible(_active);
            if (_active && _dirty) Refresh();
        }

        private void Refresh()
        {
            _dirty = false;
            try
            {
                var mode = Plugin.Settings.RangeNote.Value == RangeScale.Rang ? RangeNoteMode.Rank : RangeNoteMode.Logarithmic;
                FicheModel model;
                if (_mount != null)
                {
                    var info = _mount.info;
                    var record = Plugin.Database.Find(info.name, _mount.jsonKey, info.weaponName);
                    if (record == null) LogUnknownOnce(info, _mount);
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
