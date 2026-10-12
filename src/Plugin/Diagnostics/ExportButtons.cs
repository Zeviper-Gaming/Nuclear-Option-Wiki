using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;

namespace NuclearOptionWiki.Diagnostics
{
    /// <summary>
    /// Boutons « Exporter maintenant » du menu F1, à la place des cases à cocher d'export.
    /// Chaque appui écrit le fichier, ou le met à jour (réécriture complète) s'il existe déjà,
    /// puis affiche le résultat à côté du bouton.
    /// </summary>
    internal static class ExportButtons
    {
        private static readonly Dictionary<string, string> Status = new Dictionary<string, string>();

        /// <summary>Attribut lu par le Configuration Manager pour l'entrée « armes » ou « appareils ».</summary>
        public static object Tag(string kind)
        {
            Func<string> export = kind == "armes" ? (Func<string>)WeaponDump.Export : AircraftDump.Export;
            return new ConfigurationManagerAttributes
            {
                HideDefaultButton = true,
                CustomDrawer = _ => Draw(kind, export),
            };
        }

        private static void Draw(string kind, Func<string> export)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Exporter maintenant", GUILayout.ExpandWidth(false)))
            {
                try { Status[kind] = export(); }
                catch (Exception e)
                {
                    Plugin.Log.LogError($"[Wiki] Échec de l'export ({kind}) : {e}");
                    Status[kind] = "Échec : voir LogOutput.log";
                }
            }
            if (Status.TryGetValue(kind, out var text)) GUILayout.Label(text);
            GUILayout.EndHorizontal();
        }
    }
}

#pragma warning disable 0169, 0414, 0649
/// <summary>
/// Copie partielle de la classe reconnue par nom par BepInEx.ConfigurationManager (menu F1) :
/// seuls les champs utilisés ici sont déclarés, le gestionnaire ignore les autres.
/// </summary>
internal sealed class ConfigurationManagerAttributes
{
    public Action<ConfigEntryBase> CustomDrawer;
    public bool? HideDefaultButton;
}
#pragma warning restore 0169, 0414, 0649
