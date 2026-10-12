using System;
using BepInEx.Configuration;
using NuclearOptionWiki.Core;
using UnityEngine;

namespace NuclearOptionWiki.Integration
{
    /// <summary>
    /// A24 : éditeur des tags de l'appareil affiché, dessiné dans le menu F1 (Configuration Manager).
    /// Champ prérempli (tags séparés par « ; »), bouton « Enregistrer » : la fiche est mise à jour aussitôt.
    /// </summary>
    internal static class TagsEditor
    {
        private static string _key;      // appareil dont le champ est chargé
        private static string _buffer = "";
        private static string _status = "";

        public static object Tag() => new ConfigurationManagerAttributes { HideDefaultButton = true, CustomDrawer = Draw };

        private static void Draw(ConfigEntryBase entry)
        {
            string key = WikiController.CurrentAircraftKey;
            if (key == null)
            {
                GUILayout.Label("Ouvre le hangar et affiche la fiche d'un appareil (F6).");
                return;
            }
            if (key != _key)
            {
                _key = key;
                _buffer = TagsFile.Join(Plugin.Tags.Get(key) ?? WikiController.CurrentAircraftSheetTags());
                _status = "";
            }
            GUILayout.BeginVertical();
            GUILayout.Label(WikiController.CurrentAircraftName ?? key);
            GUILayout.BeginHorizontal();
            _buffer = GUILayout.TextField(_buffer ?? "", GUILayout.ExpandWidth(true));
            if (GUILayout.Button("Enregistrer", GUILayout.ExpandWidth(false)))
            {
                try
                {
                    Plugin.Tags.Set(key, _buffer);
                    _status = Plugin.SaveTags()
                        ? (Plugin.Tags.Get(key) == null ? "Tags du classeur rétablis" : "Enregistré")
                        : "Échec : voir LogOutput.log";
                    _buffer = TagsFile.Join(Plugin.Tags.Get(key) ?? WikiController.CurrentAircraftSheetTags());
                    WikiController.RefreshAircraft();
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError($"[Wiki] Enregistrement des tags impossible : {e}");
                    _status = "Échec : voir LogOutput.log";
                }
            }
            GUILayout.EndHorizontal();
            if (_status.Length > 0) GUILayout.Label(_status);
            GUILayout.EndVertical();
        }
    }
}
